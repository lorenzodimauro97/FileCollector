using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileCollector.Models;
using FileCollector.Services.Settings;
using FileCollector.Utils;
using Microsoft.Extensions.Logging;

namespace FileCollector.Services;

public class FileTreeService(
    IoService ioService,
    GitIgnoreFilterService gitIgnoreFilterService,
    ILogger<FileTreeService> logger)
{
    private readonly IoService _ioService = ioService ?? throw new ArgumentNullException(nameof(ioService));
    private readonly GitIgnoreFilterService _gitIgnoreFilterService = gitIgnoreFilterService ?? throw new ArgumentNullException(nameof(gitIgnoreFilterService));
    private readonly ILogger<FileTreeService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public class FileTreeLoadResult
    {
        public List<FileSystemItem> TrueRootItems { get; init; } = [];
        public List<FileSystemItem> DisplayRootItems { get; init; } = [];
        public string Message { get; init; } = "";
        public bool IsError { get; init; }
    }

    public async Task<FileTreeLoadResult> LoadTreeAsync(
        string currentProcessingPath,
        List<string> exclusions,
        Func<string, Task> reportProgress)
    {
        var overallStopwatch = Stopwatch.StartNew();
        try
        {
            await reportProgress($"Accessing folder: {currentProcessingPath.Shorten(100)}...");

            if (!await Task.Run(() => Directory.Exists(currentProcessingPath)))
            {
                _logger.LogWarning("Selected folder '{Path}' does not exist.", currentProcessingPath);
                return new FileTreeLoadResult
                {
                    Message = $"Error: Selected folder '{currentProcessingPath.Shorten(100)}' does not exist.",
                    IsError = true
                };
            }

            await reportProgress($"Scanning file system entries in {currentProcessingPath.Shorten(100)}...");
            List<FileSystemInfo> allInfos;
            var stepStopwatch = Stopwatch.StartNew();
            try
            {
                allInfos = await Task.Run(() =>
                    IoService.GetFileSystemInfosRecursive(currentProcessingPath).ToList());
                stepStopwatch.Stop();
                _logger.LogInformation("Perf: Scanned {Count} file system entries in {ElapsedMilliseconds}ms.", allInfos.Count, stepStopwatch.ElapsedMilliseconds);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while enumerating files in {Path}", currentProcessingPath);
                return new FileTreeLoadResult
                {
                    Message = $"Error: Access denied to '{ex.Message.Shorten(60)}'. Cannot enumerate files.",
                    IsError = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating files in {Path}", currentProcessingPath);
                return new FileTreeLoadResult
                    { Message = $"Error enumerating files: {ex.Message.Shorten(100)}", IsError = true };
            }

            if (allInfos.Count == 0)
            {
                _logger.LogInformation("No files or directories found in {Path}", currentProcessingPath);
                return new FileTreeLoadResult { Message = "No files or directories found in the selected folder." };
            }

            await reportProgress($"Filtering {allInfos.Count:N0} entries...");
            stepStopwatch.Restart();
            var filteredInfos =
                await Task.Run(() => _gitIgnoreFilterService.FilterInfos(allInfos, exclusions).ToList());
            stepStopwatch.Stop();
            _logger.LogInformation("Perf: Filtered {InitialCount} entries down to {FinalCount} in {ElapsedMilliseconds}ms.", allInfos.Count, filteredInfos.Count, stepStopwatch.ElapsedMilliseconds);

            if (filteredInfos.Count == 0)
            {
                _logger.LogInformation("No items remain after filtering in {Path}", currentProcessingPath);
                return new FileTreeLoadResult { Message = "No items remain after filtering." };
            }

            await reportProgress($"Building tree from {filteredInfos.Count:N0} paths...");
            stepStopwatch.Restart();
            var newTrueRootItems = await Task.Run(() => PathConverterService.BuildTree(filteredInfos, _logger));
            stepStopwatch.Stop();
            _logger.LogInformation("Perf: Built tree from {Count} paths in {ElapsedMilliseconds}ms.", filteredInfos.Count, stepStopwatch.ElapsedMilliseconds);

            var message = "";
            List<FileSystemItem> newDisplayRootItems;
            var displayRootNode = newTrueRootItems.FirstOrDefault(item =>
                string.Equals(item.FullPath, currentProcessingPath, StringComparison.OrdinalIgnoreCase));

            if (displayRootNode == null)
            {
                foreach (var trueRoot in newTrueRootItems)
                {
                    displayRootNode = FindNodeRecursive(trueRoot, currentProcessingPath);
                    if (displayRootNode != null) break;
                }
            }

            if (displayRootNode != null)
            {
                newDisplayRootItems = displayRootNode.Children.ToList();
            }
            else
            {
                var allTrueRootsAreDirectChildren = newTrueRootItems.Count != 0 && newTrueRootItems.All(item =>
                    Path.GetDirectoryName(item.FullPath)
                        ?.Equals(
                            currentProcessingPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                            StringComparison.OrdinalIgnoreCase) == true);

                if (allTrueRootsAreDirectChildren)
                {
                    newDisplayRootItems = newTrueRootItems;
                }
                else
                {
                    message =
                        "Selected folder content structure does not directly map to a displayable root. Displaying all found top-level items.";
                    _logger.LogWarning("{MessageContent} Path: {Path}", message, currentProcessingPath);
                    newDisplayRootItems = newTrueRootItems;
                }
            }

            if (string.IsNullOrEmpty(message) && newDisplayRootItems.Count == 0 && newTrueRootItems.Count != 0)
            {
                message =
                    "Selected folder has content, but none to display at the root level after processing (e.g., all are children of a filtered-out subfolder).";
                _logger.LogInformation("{MessageContent} Path: {Path}", message, currentProcessingPath);
            }
            else if (string.IsNullOrEmpty(message) && newDisplayRootItems.Count == 0 && newTrueRootItems.Count == 0 &&
                     filteredInfos.Count != 0)
            {
                message = "Items were found and filtered, but tree construction resulted in no displayable items.";
                _logger.LogWarning("{MessageContent} Path: {Path}", message, currentProcessingPath);
            }
            
            overallStopwatch.Stop();
            _logger.LogInformation("Perf: Total file tree loading process completed in {ElapsedMilliseconds}ms for {Path}.", overallStopwatch.ElapsedMilliseconds, currentProcessingPath);

            return new FileTreeLoadResult
            {
                TrueRootItems = newTrueRootItems,
                DisplayRootItems = newDisplayRootItems,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file tree processing for path {Path}", currentProcessingPath);
            return new FileTreeLoadResult
                { Message = $"Error during background processing: {ex.Message.Shorten(100)}", IsError = true };
        }
    }

    private static FileSystemItem? FindNodeRecursive(FileSystemItem currentNode, string targetFullPath)
    {
        if (string.Equals(currentNode.FullPath, targetFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return currentNode;
        }

        if (currentNode.IsDirectory)
        {
            foreach (var child in currentNode.Children)
            {
                var foundInChild = FindNodeRecursive(child, targetFullPath);
                if (foundInChild != null)
                {
                    return foundInChild;
                }
            }
        }

        return null;
    }
}