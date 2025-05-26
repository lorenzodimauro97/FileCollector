
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileCollector.Models;
using FileCollector.Utils;
using Microsoft.Extensions.Logging;
using Prism.Blazor;
using Prism.Blazor.PresetDefinitions;

namespace FileCollector.Services;

public class ContentMergingService(ILogger<ContentMergingService> logger)
{
    private readonly ILogger<ContentMergingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public class MergedContentResult
    {
        public List<MergedFileDisplayItem> MergedFilesToDisplay { get; init; } = [];
        public string MergedFileContentPlainText { get; init; } = "";
        public int EstimatedTokenCount { get; init; }
        public string ErrorMessage { get; init; } = "";
    }

    public async Task<MergedContentResult> GenerateMergedContentAsync(
        IEnumerable<FileSystemItem> filesToMerge,
        AppSettings appSettings,
        string userPrompt,
        string? currentDisplayRootPath,
        bool includeFileTreeInOutput,
        IEnumerable<FileSystemItem> allDisplayRootItems)
    {
        var mergedFilesToDisplay = new List<MergedFileDisplayItem>();
        var sbPlainText = new StringBuilder();
        var plainTextLanguage = new PlainTextLanguageDefinition();
        var overallErrorMessage = "";

        if (!string.IsNullOrWhiteSpace(appSettings.PrePrompt))
        {
            mergedFilesToDisplay.Add(new MergedFileDisplayItem
            {
                FilePath = "SYSTEM_PRE_PROMPT",
                RelativePath = "Pre-Prompt",
                Content = appSettings.PrePrompt,
                Language = plainTextLanguage
            });
            AppendSectionToPlainText(sbPlainText, "Pre-Prompt", appSettings.PrePrompt);
        }

        if (includeFileTreeInOutput && !string.IsNullOrEmpty(currentDisplayRootPath) && allDisplayRootItems.Any())
        {
            try
            {
                var treeStructureString = GenerateTreeStructureString(allDisplayRootItems, currentDisplayRootPath);
                mergedFilesToDisplay.Add(new MergedFileDisplayItem
                {
                    FilePath = "SYSTEM_FILE_TREE",
                    RelativePath = "Project File Tree",
                    Content = treeStructureString,
                    Language = plainTextLanguage
                });
                AppendSectionToPlainText(sbPlainText, "Project File Tree", treeStructureString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating file tree structure for display.");
                var treeErrorMessage = $"ERROR generating file tree: {ex.Message.Shorten(100)}";
                 mergedFilesToDisplay.Add(new MergedFileDisplayItem
                {
                    FilePath = "SYSTEM_FILE_TREE_ERROR",
                    RelativePath = "Project File Tree (Error)",
                    Content = treeErrorMessage,
                    Language = plainTextLanguage,
                    ErrorMessage = treeErrorMessage
                });
                AppendSectionToPlainText(sbPlainText, "Project File Tree (Error)", treeErrorMessage, isError: true);
                if(string.IsNullOrEmpty(overallErrorMessage)) overallErrorMessage = "Error generating file tree.";
            }
        }


        var fileNodesToProcess = filesToMerge.ToList();
        if (fileNodesToProcess.Count != 0)
        {
            foreach (var fileNode in fileNodesToProcess)
            {
                string fileContent;
                string? errorMessage = null;
                var langDef = GetLanguageDefinitionByFileName(fileNode.Name);

                try
                {
                    fileContent = await File.ReadAllTextAsync(fileNode.FullPath);
                    fileContent = fileContent.TrimEnd('\r', '\n');
                    AppendSectionToPlainText(sbPlainText, fileNode.FullPath, fileContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading file for merging: {FilePath}", fileNode.FullPath);
                    errorMessage = $"ERROR reading file: {ex.Message.Shorten(150)}";
                    fileContent = $"// {errorMessage}";
                    langDef = plainTextLanguage;

                    AppendSectionToPlainText(sbPlainText, fileNode.FullPath, errorMessage, isError: true);

                    if (string.IsNullOrEmpty(overallErrorMessage))
                    {
                        overallErrorMessage = "One or more files could not be read. See content for details.";
                    }
                }

                var relativePathForDisplay = (currentDisplayRootPath != null &&
                                              fileNode.FullPath.StartsWith(currentDisplayRootPath,
                                                  StringComparison.OrdinalIgnoreCase))
                    ? Path.GetRelativePath(currentDisplayRootPath, fileNode.FullPath)
                    : fileNode.Name;

                mergedFilesToDisplay.Add(new MergedFileDisplayItem
                {
                    FilePath = fileNode.FullPath,
                    RelativePath = relativePathForDisplay,
                    Content = fileContent,
                    Language = langDef,
                    ErrorMessage = errorMessage
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            mergedFilesToDisplay.Add(new MergedFileDisplayItem
            {
                FilePath = "SYSTEM_USER_PROMPT",
                RelativePath = "User Prompt",
                Content = userPrompt,
                Language = plainTextLanguage
            });
            AppendSectionToPlainText(sbPlainText, "User Prompt", userPrompt);
        }

        if (!string.IsNullOrWhiteSpace(appSettings.PostPrompt))
        {
            mergedFilesToDisplay.Add(new MergedFileDisplayItem
            {
                FilePath = "SYSTEM_POST_PROMPT",
                RelativePath = "Post-Prompt",
                Content = appSettings.PostPrompt,
                Language = plainTextLanguage
            });
            AppendSectionToPlainText(sbPlainText, "Post-Prompt", appSettings.PostPrompt);
        }
        
        string finalPlainTextContent = sbPlainText.ToString().TrimEnd();
        int estimatedTokenCount = finalPlainTextContent.Length / 4;


        if (fileNodesToProcess.Count == 0 &&
            !mergedFilesToDisplay.Any(mfd => mfd.FilePath == "SYSTEM_FILE_TREE") &&
            string.IsNullOrWhiteSpace(appSettings.PrePrompt) &&
            string.IsNullOrWhiteSpace(userPrompt) &&
            string.IsNullOrWhiteSpace(appSettings.PostPrompt))
        {
            return new MergedContentResult
                { MergedFileContentPlainText = "", EstimatedTokenCount = 0, MergedFilesToDisplay = [], ErrorMessage = overallErrorMessage };
        }

        return new MergedContentResult
        {
            MergedFilesToDisplay = mergedFilesToDisplay,
            MergedFileContentPlainText = finalPlainTextContent,
            EstimatedTokenCount = estimatedTokenCount,
            ErrorMessage = overallErrorMessage
        };
    }

    private static void AppendSectionToPlainText(StringBuilder sb, string filePathOrSectionName, string content,
        bool isError = false)
    {

        string displayName = filePathOrSectionName;
        if (!filePathOrSectionName.StartsWith("SYSTEM_") && !filePathOrSectionName.StartsWith("Pre-Prompt") && !filePathOrSectionName.StartsWith("Post-Prompt") && !filePathOrSectionName.StartsWith("User Prompt") && !filePathOrSectionName.StartsWith("Project File Tree"))
        {
             displayName = filePathOrSectionName.Replace('\\', Path.DirectorySeparatorChar);
        }


        sb.AppendLine($"// File: {displayName}");
        if (isError)
        {
            sb.AppendLine($"// {content}");
        }
        else
        {
            sb.AppendLine("//--------------------------------------------------");
            sb.AppendLine(content.TrimEnd('\r','\n'));
            sb.AppendLine("//--------------------------------------------------");
            sb.AppendLine($"// End of file: {displayName}");
        }

        sb.AppendLine().AppendLine();
    }
    
    private string GenerateTreeStructureString(IEnumerable<FileSystemItem> displayRootItems, string currentDisplayRootPath)
    {
        var sb = new StringBuilder();
        var rootDirName = Path.GetFileName(currentDisplayRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(rootDirName) && !string.IsNullOrEmpty(currentDisplayRootPath))
        {
            rootDirName = currentDisplayRootPath;
        }
        sb.AppendLine($"{rootDirName}{Path.DirectorySeparatorChar}");

        var sortedRootItems = displayRootItems.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        for (int i = 0; i < sortedRootItems.Count; i++)
        {
            BuildTreeStringRecursive(sb, sortedRootItems[i], "", i == sortedRootItems.Count - 1);
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    private void BuildTreeStringRecursive(StringBuilder sb, FileSystemItem item, string indent, bool isLast)
    {
        sb.Append(indent);
        if (isLast)
        {
            sb.Append("└── ");
            indent += "    ";
        }
        else
        {
            sb.Append("├── ");
            indent += "│   ";
        }
        sb.Append(item.Name);
        if (item.IsDirectory)
        {
            sb.Append(Path.DirectorySeparatorChar);
        }
        sb.AppendLine();

        if (item.IsDirectory && item.Children.Any())
        {
            var sortedChildren = item.Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                BuildTreeStringRecursive(sb, sortedChildren[i], indent, i == sortedChildren.Count - 1);
            }
        }
    }


    private static ILanguageDefinition GetLanguageDefinitionByFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".cs" => new CSharpLanguageDefinition(),
            ".css" => new CssLanguageDefinition(),
            ".js" or ".mjs" => new JavaScriptLanguageDefinition(),
            ".ts" => new TypeScriptLanguageDefinition(),
            ".jsx" => new JsxLanguageDefinition(),
            ".tsx" => new TsxLanguageDefinition(),
            ".razor" => new RazorLanguageDefinition(),
            ".sql" => new SqlLanguageDefinition(),
            _ => new PlainTextLanguageDefinition()
        };
    }
}