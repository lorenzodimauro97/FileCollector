using System.Diagnostics;
using Serilog;
using Serilog.Events;

namespace FileCollector.Updater;

internal class Program
{
    private const int DelayBeforeStartOperationsMs = 5000;
    private const string LogFolderName = "logs";

    private static void Main(string[] args)
    {
        var updaterProcess = Process.GetCurrentProcess();
        var updaterExePath = updaterProcess.MainModule?.FileName ?? "FileCollector.Updater.exe";
        var updaterDirectory = Path.GetDirectoryName(updaterExePath) ?? AppContext.BaseDirectory;
        var logDirectory = Path.Combine(updaterDirectory, LogFolderName); // Ensure logs are in a subfolder of updater's location
        
        // Ensure log directory exists
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        var logFilePath = Path.Combine(logDirectory, "FileCollector.Updater-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ProcessId}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 5, // Increased slightly
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            )
            .CreateLogger();

        try
        {
            Log.Information("--------------------------------------------------");
            Log.Information("FileCollector Updater ({ProcessId}) started.", updaterProcess.Id);
            Log.Verbose("Updater Version: {Version}", typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown");
            Log.Verbose("Updater Exe Path: {UpdaterExePath}", updaterExePath);
            Log.Verbose("Updater Working Directory: {UpdaterDirectory}", Environment.CurrentDirectory);
            Log.Verbose("Updater AppContext.BaseDirectory: {BaseDirectory}", AppContext.BaseDirectory);
            Log.Verbose("Effective Updater Directory for Logs: {EffectiveLogDir}", updaterDirectory);
            Log.Verbose("Log File Path: {LogFilePath}", logFilePath);
            Log.Verbose("Command line arguments: {ArgsCount} arguments", args.Length);
            for (var i = 0; i < args.Length; i++)
            {
                Log.Verbose("Arg[{Index}]: {Argument}", i, args[i]);
            }

            if (args.Length < 4)
            {
                Log.Error(
                    "Insufficient arguments. Expected: <targetDir> <sourceDir> <mainAppExeName> <settingsBackupPath>");
                Log.Information("Updater exiting with code 1 (Insufficient Arguments).");
                Environment.ExitCode = 1;
                return;
            }

            var targetDir = args[0];
            var sourceDir = args[1];
            var mainAppExeName = args[2];
            var settingsBackupPath = args[3];

            Log.Information("Target Directory (Main App): {TargetDir}", targetDir);
            Log.Information("Source Directory (Extracted Update): {SourceDir}", sourceDir);
            Log.Information("Main App Executable Name: {MainAppExeName}", mainAppExeName);
            Log.Information("Settings Backup Path: {SettingsBackupPath}", settingsBackupPath);

            if (!Directory.Exists(sourceDir))
            {
                Log.Error("Source directory '{SourceDir}' does not exist.", sourceDir);
                Log.Information("Updater exiting with code 2 (Source Directory Not Found).");
                Environment.ExitCode = 2;
                return;
            }
            Log.Verbose("Source directory '{SourceDir}' confirmed to exist.", sourceDir);

            if (!File.Exists(settingsBackupPath))
            {
                Log.Warning(
                    "Settings backup file '{SettingsBackupPath}' does not exist. Settings might not be restored if this is an initial setup or an error occurred before backup.",
                    settingsBackupPath);
            }
            else
            {
                Log.Verbose("Settings backup file '{SettingsBackupPath}' confirmed to exist.", settingsBackupPath);
            }

            if (!Directory.Exists(targetDir))
            {
                Log.Warning("Target directory '{TargetDir}' does not exist. Will attempt to create it.", targetDir);
                try
                {
                    Directory.CreateDirectory(targetDir);
                    Log.Information("Target directory '{TargetDir}' created successfully.", targetDir);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to create target directory '{TargetDir}'.", targetDir);
                    Log.Information("Updater exiting with code 3 (Failed to Create Target Directory).");
                    Environment.ExitCode = 3;
                    return;
                }
            }
            else
            {
                Log.Verbose("Target directory '{TargetDir}' confirmed to exist.", targetDir);
            }
            
            Log.Information("Waiting for {DelaySeconds} seconds for the main application to close...",
                DelayBeforeStartOperationsMs / 1000);
            Log.Verbose("Current time before sleep: {Time}", DateTime.Now.ToString("O"));
            Thread.Sleep(DelayBeforeStartOperationsMs);
            Log.Verbose("Current time after sleep: {Time}", DateTime.Now.ToString("O"));

            try
            {
                Log.Information("Starting update process execution...");

                Log.Information("Clearing target directory: {TargetDir}", targetDir);
                ClearDirectory(targetDir, updaterDirectory); // Pass updaterDirectory to avoid deleting its logs

                Log.Information("Copying new files from {SourceDir} to {TargetDir}", sourceDir, targetDir);
                CopyDirectory(sourceDir, targetDir, true);

                if (File.Exists(settingsBackupPath))
                {
                    var targetSettingsPath = Path.Combine(targetDir, "appsettings.json");
                    Log.Information("Restoring appsettings.json from {SettingsBackupPath} to {TargetSettingsPath}",
                        settingsBackupPath, targetSettingsPath);
                    try
                    {
                        File.Copy(settingsBackupPath, targetSettingsPath, true);
                        Log.Verbose("Successfully copied settings backup to target.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to copy settings backup from {SettingsBackupPath} to {TargetSettingsPath}.", settingsBackupPath, targetSettingsPath);
                    }
                }
                else
                {
                    Log.Information(
                        "No settings backup file found at {SettingsBackupPath} to restore. The new version might include default settings or this is an initial setup.", settingsBackupPath);
                }

                Log.Information("Update process core operations completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "FATAL ERROR during update process execution block.");
                Log.Information("Updater exiting with code 4 (Fatal Error in Update Execution).");
                Environment.ExitCode = 4;
                return;
            }
            finally
            {
                Log.Information("Starting cleanup of temporary files and directories...");
                try
                {
                    if (Directory.Exists(sourceDir))
                    {
                        Log.Information("Cleaning up source directory: {SourceDir}", sourceDir);
                        Directory.Delete(sourceDir, true);
                        Log.Verbose("Successfully deleted source directory: {SourceDir}", sourceDir);
                    } else {
                        Log.Verbose("Source directory {SourceDir} does not exist, no need to clean up.", sourceDir);
                    }

                    if (File.Exists(settingsBackupPath))
                    {
                        Log.Information("Cleaning up settings backup: {SettingsBackupPath}", settingsBackupPath);
                        File.Delete(settingsBackupPath);
                        Log.Verbose("Successfully deleted settings backup: {SettingsBackupPath}", settingsBackupPath);
                    } else {
                        Log.Verbose("Settings backup {SettingsBackupPath} does not exist, no need to clean up.", settingsBackupPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    Log.Warning(cleanupEx, "Error during cleanup of temporary files. This is non-fatal.");
                }
                Log.Information("Cleanup finished.");
            }

            var mainAppPath = Path.Combine(targetDir, mainAppExeName);
            Log.Information("Attempting to relaunch main application. Path: {MainAppPath}", mainAppPath);
            if (File.Exists(mainAppPath))
            {
                Log.Verbose("Main application executable found at: {MainAppPath}", mainAppPath);
                try
                {
                    var startInfo = new ProcessStartInfo(mainAppPath)
                    {
                        WorkingDirectory = targetDir,
                        UseShellExecute = true // Important for launching GUI apps correctly, especially if not in PATH
                    };
                    Log.Verbose("ProcessStartInfo.FileName: {FileName}", startInfo.FileName);
                    Log.Verbose("ProcessStartInfo.WorkingDirectory: {WorkingDirectory}", startInfo.WorkingDirectory);
                    Log.Verbose("ProcessStartInfo.UseShellExecute: {UseShellExecute}", startInfo.UseShellExecute);
                    Log.Verbose("ProcessStartInfo.Arguments: {Arguments}", string.IsNullOrEmpty(startInfo.Arguments) ? "<none>" : startInfo.Arguments);

                    Process.Start(startInfo);
                    Log.Information("Main application relaunch command issued successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error relaunching main application from {MainAppPath} with WorkingDirectory {WorkingDirectory}.", mainAppPath, targetDir);
                    Log.Information("Updater exiting with code 5 (Error Relaunching Main App).");
                    Environment.ExitCode = 5;
                    return;
                }
            }
            else
            {
                Log.Error("Main application executable not found after update: {MainAppPath}", mainAppPath);
                Log.Information("Updater exiting with code 6 (Main App Not Found After Update).");
                Environment.ExitCode = 6;
                return;
            }

            Log.Information("Updater finished successfully.");
            Log.Information("Updater exiting with code 0 (Success).");
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception in updater Main method. This is outside the primary operational try-catch.");
            Log.Information("Updater exiting with code 99 (Unhandled Exception in Main).");
            Environment.ExitCode = 99;
        }
        finally
        {
            Log.Information("FileCollector Updater ({ProcessId}) shutting down.", updaterProcess.Id);
            Log.Information("--------------------------------------------------");
            Log.CloseAndFlush();
        }
    }

    private static void ClearDirectory(string directoryPath, string updaterBaseDirectory)
    {
        Log.Verbose("Entering ClearDirectory for path: {DirectoryPath}", directoryPath);
        var dirInfo = new DirectoryInfo(directoryPath);
        if (!dirInfo.Exists)
        {
            Log.Warning("ClearDirectory: Directory {DirectoryPath} does not exist. Nothing to clear.", directoryPath);
            return;
        }

        var files = dirInfo.GetFiles();
        Log.Verbose("Found {Count} files in {DirectoryPath}", files.Length, directoryPath);
        foreach (var file in files)
        {
            try
            {
                Log.Verbose("Attempting to delete file: {FilePath}", file.FullName);
                file.Delete();
                Log.Verbose("Deleted file: {FilePath}", file.FullName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error deleting file {FilePath}. Skipping.", file.FullName);
            }
        }

        var subDirs = dirInfo.GetDirectories();
        Log.Verbose("Found {Count} subdirectories in {DirectoryPath}", subDirs.Length, directoryPath);
        foreach (var subDir in subDirs)
        {
            // Critical: Do not delete the updater's own log directory or the updater's directory itself if it's somehow inside targetDir
            var fullSubDirPathNormalized = Path.GetFullPath(subDir.FullName).TrimEnd(Path.DirectorySeparatorChar);
            var updaterLogDirNormalized = Path.GetFullPath(Path.Combine(updaterBaseDirectory, LogFolderName)).TrimEnd(Path.DirectorySeparatorChar);
            var updaterDirNormalized = Path.GetFullPath(updaterBaseDirectory).TrimEnd(Path.DirectorySeparatorChar);

            if (fullSubDirPathNormalized.Equals(updaterLogDirNormalized, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Skipping deletion of updater's log folder: {FolderPath}", subDir.FullName);
                continue;
            }
            if (fullSubDirPathNormalized.Equals(updaterDirNormalized, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Skipping deletion of updater's own base directory: {FolderPath}", subDir.FullName);
                continue;
            }
            // Legacy check, just in case LogFolderName is directly under targetDir (though it shouldn't be with new log path logic)
            if (subDir.Name.Equals(LogFolderName, StringComparison.OrdinalIgnoreCase) && 
                Path.GetFullPath(subDir.Parent.FullName).Equals(Path.GetFullPath(updaterBaseDirectory), StringComparison.OrdinalIgnoreCase) )
            {
                 Log.Information("Skipping log folder by name comparison (legacy safety): {FolderPath}", subDir.FullName);
                 continue;
            }


            try
            {
                Log.Verbose("Attempting to delete directory recursively: {FolderPath}", subDir.FullName);
                subDir.Delete(true);
                Log.Verbose("Deleted directory: {FolderPath}", subDir.FullName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error deleting directory {FolderPath}. Skipping.", subDir.FullName);
            }
        }
        Log.Verbose("Exiting ClearDirectory for path: {DirectoryPath}", directoryPath);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        Log.Verbose("Entering CopyDirectory. Source: {SourceDir}, Destination: {DestinationDir}, Recursive: {IsRecursive}", sourceDir, destinationDir, recursive);
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
        {
            Log.Error("CopyDirectory: Source directory not found: {SourcePath}", dir.FullName);
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        var dirs = dir.GetDirectories();
        Log.Verbose("Found {Count} subdirectories in source {SourceDir}", dirs.Length, sourceDir);

        if (!Directory.Exists(destinationDir))
        {
            Log.Verbose("Destination directory {DestinationDir} does not exist. Creating it.", destinationDir);
            Directory.CreateDirectory(destinationDir);
            Log.Verbose("Created destination directory: {DestinationDir}", destinationDir);
        } else {
            Log.Verbose("Destination directory {DestinationDir} already exists.", destinationDir);
        }

        var files = dir.GetFiles();
        Log.Verbose("Found {Count} files in source {SourceDir}", files.Length, sourceDir);
        foreach (var file in files)
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            Log.Verbose("Copying file: {SourceFilePath} to {TargetFilePath}", file.FullName, targetFilePath);
            try
            {
                file.CopyTo(targetFilePath, true); // Overwrite if exists
                Log.Verbose("Successfully copied file: {SourceFilePath} to {TargetFilePath}", file.FullName, targetFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy file {SourceFilePath} to {TargetFilePath}", file.FullName, targetFilePath);
                throw; // Re-throw to let the main update process fail if a copy fails
            }
        }

        if (recursive)
        {
            Log.Verbose("Recursive copy enabled. Processing subdirectories...");
            foreach (var subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                Log.Verbose("Recursively calling CopyDirectory for subdir: {SubDirName} to {NewDestinationDir}", subDir.Name, newDestinationDir);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
        Log.Verbose("Exiting CopyDirectory for Source: {SourceDir}, Destination: {DestinationDir}", sourceDir, destinationDir);
    }
}