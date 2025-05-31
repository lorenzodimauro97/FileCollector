using System;
using System.Collections.Generic;
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
            List<string> allEntries;
            try
            {
                allEntries = await Task.Run(() =>
                    IoService.GetFileSystemEntriesRecursive(currentProcessingPath).ToList());
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

            if (allEntries.Count == 0)
            {
                _logger.LogInformation("No files or directories found in {Path}", currentProcessingPath);
                return new FileTreeLoadResult { Message = "No files or directories found in the selected folder." };
            }

            await reportProgress($"Filtering {allEntries.Count:N0} entries...");
            var filteredFullPaths =
                await Task.Run(() => _gitIgnoreFilterService.FilterPaths(allEntries, exclusions).ToList());

            if (filteredFullPaths.Count == 0)
            {
                _logger.LogInformation("No items remain after filtering in {Path}", currentProcessingPath);
                return new FileTreeLoadResult { Message = "No items remain after filtering." };
            }

            await reportProgress($"Building tree from {filteredFullPaths.Count:N0} paths...");
            var newTrueRootItems = await Task.Run(() => PathConverterService.BuildTree(filteredFullPaths));

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
                     filteredFullPaths.Count != 0)
            {
                message = "Items were found and filtered, but tree construction resulted in no displayable items.";
                _logger.LogWarning("{MessageContent} Path: {Path}", message, currentProcessingPath);
            }

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