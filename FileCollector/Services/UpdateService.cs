using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FileCollector.Models;
using FileCollector.Services.Settings;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace FileCollector.Services;

public class UpdateService
{
    private readonly ILogger<UpdateService> _logger;
    public readonly SettingsService SettingsService;
    private readonly HttpClient _httpClient;
    private readonly UpdateStateService _updateStateService;
    private readonly Func<PhotinoWindow> _photinoWindowFactory;

    private const string UpdateTempSubFolder = "FileCollectorUpdate";
    private const string AppNamePrefix = "FileCollector";

    public UpdateService(
        ILogger<UpdateService> logger,
        SettingsService settingsService,
        HttpClient httpClient,
        UpdateStateService updateStateService,
        Func<PhotinoWindow> photinoWindowFactory)
    {
        _logger = logger;
        SettingsService = settingsService;
        _httpClient = httpClient;
        _updateStateService = updateStateService;
        _photinoWindowFactory = photinoWindowFactory;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FileCollectorApp/1.0");
    }

    private string GetPlatformSpecificAssetFileName()
    {
        string osPart;
        string archPart;
        string extension;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            osPart = "win";
            extension = ".zip";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            osPart = "linux";
            extension = ".tar.gz";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osPart = "osx";
            extension = ".tar.gz";
        }
        else
        {
            _logger.LogWarning("Unsupported OS platform for update asset determination: {OSPlatform}",
                RuntimeInformation.OSDescription);
            return $"{AppNamePrefix}-unknown-platform{".zip"}";
        }

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                archPart = "x64";
                break;
            case Architecture.Arm64:
                archPart = "arm64";
                break;
            default:
                _logger.LogWarning(
                    "Unsupported process architecture for update asset determination: {ProcessArchitecture}",
                    RuntimeInformation.ProcessArchitecture);
                archPart = "unknownarch";
                break;
        }

        return $"{AppNamePrefix}-{osPart}-{archPart}{extension}";
    }

    public async Task CheckForUpdatesAsync(bool initiatedByUser = false)
    {
        _logger.LogInformation("Checking for updates...");
        _updateStateService.SetState(UpdateProcessState.Checking, "Checking for updates...");

        try
        {
            var appSettings = await SettingsService.GetAppSettingsAsync();
            if (string.IsNullOrWhiteSpace(appSettings.Update.GitHubRepoOwner) ||
                string.IsNullOrWhiteSpace(appSettings.Update.GitHubRepoName))
            {
                var msg = "Update settings (RepoOwner, RepoName) are not configured.";
                _logger.LogWarning(msg);
                _updateStateService.SetError(msg);
                return;
            }

            var requestUrl =
                $"https://api.github.com/repos/{appSettings.Update.GitHubRepoOwner}/{appSettings.Update.GitHubRepoName}/releases";
            var allReleases = await _httpClient.GetFromJsonAsync<List<GitHubReleaseInfo>>(requestUrl);

            if (allReleases == null || allReleases.Count == 0)
            {
                _logger.LogInformation("No releases found on GitHub.");
                _updateStateService.SetState(UpdateProcessState.Idle,
                    initiatedByUser ? "You are on the latest version." : null, null);
                return;
            }

            LargeVersion? currentVersion;
            var systemVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            if (systemVersion != null)
            {
                currentVersion = LargeVersion.FromSystemVersion(systemVersion);
            }
            else
            {
                if (!LargeVersion.TryParse("0.0.0.0", out currentVersion))
                {
                    _logger.LogWarning("Could not parse fallback current application version '0.0.0.0'.");
                    _updateStateService.SetError("Could not determine current app version.");
                    return;
                }
            }

            if (currentVersion == null)
            {
                _logger.LogWarning("Could not determine current application version.");
                _updateStateService.SetError("Could not determine current app version.");
                return;
            }

            var availableUpdates = new List<GitHubReleaseInfo>();
            foreach (var release in allReleases)
            {
                if (release.Prerelease || release.Draft) continue;

                var releaseVersionString = release.TagName.TrimStart('v');
                if (LargeVersion.TryParse(releaseVersionString, out var releaseVersion) && releaseVersion != null)
                {
                    if (releaseVersion.CompareTo(currentVersion) > 0)
                    {
                        availableUpdates.Add(release);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse release version from tag: {TagName}", release.TagName);
                }
            }


            if (availableUpdates.Any())
            {
                var sortedUpdates = availableUpdates.OrderByDescending(r =>
                {
                    LargeVersion.TryParse(r.TagName.TrimStart('v'), out var v);
                    return v;
                }).ToList();

                var latestVersion = sortedUpdates.First();
                _logger.LogInformation("{UpdateCount} new version(s) available. Latest is {LatestVersion}", sortedUpdates.Count, latestVersion.TagName);
                _updateStateService.SetState(UpdateProcessState.UpdateAvailable,
                    $"{sortedUpdates.Count} new update(s) available.",
                    sortedUpdates);
            }
            else
            {
                _logger.LogInformation("Application is up to date.");
                _updateStateService.SetState(UpdateProcessState.Idle,
                    initiatedByUser ? "You are on the latest version." : null, null);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error checking for updates (HTTP Request).");
            _updateStateService.SetError($"Network error checking for updates: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generic error checking for updates.");
            _updateStateService.SetError($"Error checking for updates: {ex.Message}");
        }
    }

    public async Task DownloadAndApplyUpdateAsync()
    {
        if (_updateStateService.SelectedUpdate == null)
        {
            _updateStateService.SetError("No update information available to download.");
            return;
        }

        var appSettings = await SettingsService.GetAppSettingsAsync();
        var expectedAssetName = GetPlatformSpecificAssetFileName();
        var asset = _updateStateService.SelectedUpdate.Assets.FirstOrDefault(a =>
            a.Name.Equals(expectedAssetName, StringComparison.OrdinalIgnoreCase));

        if (asset == null)
        {
            _updateStateService.SetError($"Update asset '{expectedAssetName}' for your platform not found in release.");
            return;
        }

        var tempUpdateDir = Path.Combine(Path.GetTempPath(), UpdateTempSubFolder);
        var downloadedZipPath = Path.Combine(tempUpdateDir, asset.Name);
        var extractionPath = Path.Combine(tempUpdateDir, "extracted");

        try
        {
            _logger.LogInformation("Starting update download from {DownloadUrl}", asset.BrowserDownloadUrl);
            _updateStateService.SetState(UpdateProcessState.Downloading, $"Downloading {asset.Name}...");

            if (Directory.Exists(tempUpdateDir)) Directory.Delete(tempUpdateDir, true);
            Directory.CreateDirectory(tempUpdateDir);
            Directory.CreateDirectory(extractionPath);

            using var response =
                await _httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            long bytesRead = 0;

            await using (var contentStream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(downloadedZipPath, FileMode.Create, FileAccess.Write, FileShare.None,
                             8192, true))
            {
                var buffer = new byte[8192];
                int bytesReadFromStream;
                while ((bytesReadFromStream = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesReadFromStream));
                    bytesRead += bytesReadFromStream;
                    if (totalBytes > 0)
                    {
                        _updateStateService.SetProgress((double)bytesRead / totalBytes * 100,
                            $"Downloading... {bytesRead / (1024 * 1024)}MB / {totalBytes / (1024 * 1024)}MB");
                    }
                }
            }

            _logger.LogInformation("Download complete: {DownloadedZipPath}", downloadedZipPath);
            _updateStateService.SetProgress(100, "Download complete.");

            _updateStateService.SetState(UpdateProcessState.Extracting, "Extracting update...");

            if (asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(downloadedZipPath, extractionPath, true));
            }
            else if (asset.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Extraction of .tar.gz is not fully implemented in this example using System.Formats.Tar. The downloaded file is at {DownloadedFilePath}. Copying archive to extraction path.",
                    downloadedZipPath);
                File.Copy(downloadedZipPath, Path.Combine(extractionPath, Path.GetFileName(downloadedZipPath)), true);
                _logger.LogInformation(
                    "Non-zip asset {AssetName} copied to extraction path. Updater will need to handle actual extraction if this is an archive.",
                    asset.Name);
            }
            else
            {
                _logger.LogError("Unknown archive type for asset: {AssetName}", asset.Name);
                _updateStateService.SetError($"Unknown archive type: {asset.Name}");
                return;
            }

            _logger.LogInformation("Extraction/preparation complete to: {ExtractionPath}", extractionPath);
            _updateStateService.SetState(UpdateProcessState.Extracting, "Extraction complete.");


            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var appSettingsBackupPath = Path.Combine(tempUpdateDir, "appsettings.backup.json");

            if (File.Exists(appSettingsPath))
            {
                File.Copy(appSettingsPath, appSettingsBackupPath, true);
                _logger.LogInformation("Backed up appsettings.json to {AppSettingsBackupPath}", appSettingsBackupPath);
            }

            _updateStateService.SetState(UpdateProcessState.ReadyToApply,
                $"Update downloaded and prepared in: {extractionPath}. Ready to apply.");

            var baseUpdaterExeNameFromSettings = appSettings.Update.UpdaterExecutableName;
            string actualUpdaterFileName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                actualUpdaterFileName = Path.GetFileNameWithoutExtension(baseUpdaterExeNameFromSettings);
            }
            else
            {
                actualUpdaterFileName = baseUpdaterExeNameFromSettings;
            }

            var updaterSourcePath = Path.Combine(AppContext.BaseDirectory, actualUpdaterFileName);

            if (File.Exists(updaterSourcePath))
            {
                var updaterTempPath = Path.Combine(tempUpdateDir, actualUpdaterFileName);
                File.Copy(updaterSourcePath, updaterTempPath, true);
                _logger.LogInformation("Copied updater {UpdaterFileName} to {UpdaterTempPath}", actualUpdaterFileName,
                    updaterTempPath);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    File.SetUnixFileMode(updaterTempPath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                    _logger.LogInformation("Set execute permissions for {UpdaterTempPath} on Unix-like system.",
                        updaterTempPath);
                }

                var mainModuleFileName = Process.GetCurrentProcess().MainModule?.FileName;
                string mainAppExeNameToPass;

                if (string.IsNullOrEmpty(mainModuleFileName))
                {
                    _logger.LogError("Could not determine main module file name for the current process. Updater might not be able to restart the main application. Using fallback 'FileCollector.exe'.");
                    mainAppExeNameToPass = "FileCollector.exe"; 
                }
                else
                {
                    mainAppExeNameToPass = Path.GetFileName(mainModuleFileName);
                }
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = updaterTempPath,
                    WorkingDirectory = tempUpdateDir,
                    UseShellExecute = false 
                };

                processStartInfo.ArgumentList.Add(AppContext.BaseDirectory);
                processStartInfo.ArgumentList.Add(extractionPath);
                processStartInfo.ArgumentList.Add(mainAppExeNameToPass);
                processStartInfo.ArgumentList.Add(appSettingsBackupPath);
                
                _logger.LogInformation("Launching updater: {FileName} with {ArgCount} arguments.", processStartInfo.FileName, processStartInfo.ArgumentList.Count);
                for(var i = 0; i < processStartInfo.ArgumentList.Count; i++)
                {
                    _logger.LogInformation("Arg[{Index}]: {ArgumentValue}", i, processStartInfo.ArgumentList[i]);
                }
                _logger.LogInformation("Updater UseShellExecute: {UseShellExecute}", processStartInfo.UseShellExecute);

                Process.Start(processStartInfo);
                _updateStateService.SetState(UpdateProcessState.Applying,
                    "Updater launched. Application will now close.");

                await Task.Delay(1000);
                _photinoWindowFactory().Close();
            }
            else
            {
                var manualUpdateMessage =
                    $"Updater '{actualUpdaterFileName}' not found at '{updaterSourcePath}'. Update downloaded to '{extractionPath}'. Please close the application and manually copy files. Your settings have been backed up to '{appSettingsBackupPath}'.";
                _logger.LogWarning(manualUpdateMessage);
                _updateStateService.SetState(UpdateProcessState.ReadyToApply, manualUpdateMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update download/application process.");
            _updateStateService.SetError($"Error applying update: {ex.Message}");
            if (Directory.Exists(tempUpdateDir))
            {
                try
                {
                    Directory.Delete(tempUpdateDir, true);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to clean up temporary update directory: {TempUpdateDir}", tempUpdateDir);
                }
            }
        }
    }
}