using System.Diagnostics;
using Serilog;
using Serilog.Events;

namespace FileCollector.Updater;

internal abstract class Program
{
    private const int DelayBeforeStartOperationsMs = 5000;
    private const string LogFolderName = "logs";

    private static void Main(string[] args)
    {
        var updaterExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "FileCollector.Updater.exe";
        var logDirectory = Path.GetDirectoryName(updaterExePath) ?? AppContext.BaseDirectory;
        var logFilePath = Path.Combine(logDirectory, "FileCollector.Updater-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 3,
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            )
            .CreateLogger();

        try
        {
            Log.Information("FileCollector Updater started.");
            Log.Verbose("Updater process initiated with Serilog.");

            if (args.Length < 4)
            {
                Log.Error(
                    "Insufficient arguments. Expected: <targetDir> <sourceDir> <mainAppExeName> <settingsBackupPath>");
                Environment.ExitCode = 1;
                return;
            }

            var targetDir = args[0];
            var sourceDir = args[1];
            var mainAppExeName = args[2];
            var settingsBackupPath = args[3];

            Log.Information("Target Directory: {TargetDir}", targetDir);
            Log.Information("Source Directory (Extracted Update): {SourceDir}", sourceDir);
            Log.Information("Main App Executable: {MainAppExeName}", mainAppExeName);
            Log.Information("Settings Backup Path: {SettingsBackupPath}", settingsBackupPath);

            if (!Directory.Exists(sourceDir))
            {
                Log.Error("Source directory '{SourceDir}' does not exist.", sourceDir);
                Environment.ExitCode = 2;
                return;
            }

            if (!File.Exists(settingsBackupPath))
            {
                Log.Warning(
                    "Settings backup file '{SettingsBackupPath}' does not exist. Settings might not be restored if this is an initial setup or an error occurred before backup.",
                    settingsBackupPath);
            }

            if (!Directory.Exists(targetDir))
            {
                Log.Warning("Target directory '{TargetDir}' does not exist. Will attempt to create it.", targetDir);
                try
                {
                    Directory.CreateDirectory(targetDir);
                    Log.Information("Target directory '{TargetDir}' created.", targetDir);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to create target directory '{TargetDir}'.", targetDir);
                    Environment.ExitCode = 3;
                    return;
                }
            }

            Log.Information("Waiting for {DelayMs} seconds for the main application to close...",
                DelayBeforeStartOperationsMs / 1000);
            Thread.Sleep(DelayBeforeStartOperationsMs);

            try
            {
                Log.Information("Starting update process...");

                Log.Information("Clearing target directory: {TargetDir}", targetDir);
                ClearDirectory(targetDir);

                Log.Information("Copying new files from {SourceDir} to {TargetDir}", sourceDir, targetDir);
                CopyDirectory(sourceDir, targetDir, true);

                if (File.Exists(settingsBackupPath))
                {
                    var targetSettingsPath = Path.Combine(targetDir, "appsettings.json");
                    Log.Information("Restoring appsettings.json from {SettingsBackupPath} to {TargetSettingsPath}",
                        settingsBackupPath, targetSettingsPath);
                    File.Copy(settingsBackupPath, targetSettingsPath, true);
                }
                else
                {
                    Log.Information(
                        "No settings backup file found to restore. The new version might include default settings.");
                }

                Log.Information("Update process completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "FATAL ERROR during update process.");
                Environment.ExitCode = 4;
                return;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(sourceDir))
                    {
                        Log.Information("Cleaning up source directory: {SourceDir}", sourceDir);
                        Directory.Delete(sourceDir, true);
                    }

                    if (File.Exists(settingsBackupPath))
                    {
                        Log.Information("Cleaning up settings backup: {SettingsBackupPath}", settingsBackupPath);
                        File.Delete(settingsBackupPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    Log.Warning(cleanupEx, "Error during cleanup of temporary files.");
                }
            }

            var mainAppPath = Path.Combine(targetDir, mainAppExeName);
            Log.Information("Attempting to relaunch main application: {MainAppPath}", mainAppPath);
            if (File.Exists(mainAppPath))
            {
                try
                {
                    var startInfo = new ProcessStartInfo(mainAppPath)
                    {
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    Log.Information("Main application relaunch command issued.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error relaunching main application.");
                    Environment.ExitCode = 5;
                    return;
                }
            }
            else
            {
                Log.Error("Main application executable not found after update: {MainAppPath}", mainAppPath);
                Environment.ExitCode = 6;
                return;
            }

            Log.Information("Updater finished successfully.");
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception in updater Main method.");
            Environment.ExitCode = 99;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ClearDirectory(string directoryPath)
    {
        var dirInfo = new DirectoryInfo(directoryPath);

        foreach (var file in dirInfo.GetFiles())
        {
            try
            {
                file.Delete();
                Log.Verbose("Deleted file: {FilePath}", file.FullName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error deleting file {FilePath}. Skipping.", file.FullName);
            }
        }

        foreach (var subDir in dirInfo.GetDirectories())
        {
            if (subDir.Name.Equals(LogFolderName, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Skipping log folder: {FolderPath}", subDir.FullName);
                continue;
            }

            try
            {
                subDir.Delete(true);
                Log.Verbose("Deleted directory: {FolderPath}", subDir.FullName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error deleting directory {FolderPath}. Skipping.", subDir.FullName);
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        var dirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
            Log.Verbose("Copied file: {SourceFilePath} to {TargetFilePath}", file.FullName, targetFilePath);
        }

        if (recursive)
        {
            foreach (var subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}