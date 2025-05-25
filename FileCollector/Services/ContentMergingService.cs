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
        public string ErrorMessage { get; init; } = "";
    }

    public async Task<MergedContentResult> GenerateMergedContentAsync(
        IEnumerable<FileSystemItem> filesToMerge,
        AppSettings appSettings,
        string userPrompt,
        string? currentDisplayRootPath)
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

        if (fileNodesToProcess.Count == 0 &&
            string.IsNullOrWhiteSpace(appSettings.PrePrompt) &&
            string.IsNullOrWhiteSpace(userPrompt) &&
            string.IsNullOrWhiteSpace(appSettings.PostPrompt))
        {
            return new MergedContentResult
                { MergedFileContentPlainText = "", MergedFilesToDisplay = [], ErrorMessage = overallErrorMessage };
        }

        return new MergedContentResult
        {
            MergedFilesToDisplay = mergedFilesToDisplay,
            MergedFileContentPlainText = sbPlainText.ToString().TrimEnd(),
            ErrorMessage = overallErrorMessage
        };
    }

    private static void AppendSectionToPlainText(StringBuilder sb, string filePath, string content,
        bool isError = false)
    {
        sb.AppendLine($"// File: {filePath}");
        if (isError)
        {
            sb.AppendLine($"// {content}");
        }
        else
        {
            sb.AppendLine("//--------------------------------------------------");
            sb.AppendLine(content);
            sb.AppendLine("//--------------------------------------------------");
            sb.AppendLine($"// End of file: {filePath}");
        }

        sb.AppendLine().AppendLine();
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