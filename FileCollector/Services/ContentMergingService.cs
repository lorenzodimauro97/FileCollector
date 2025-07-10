using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    private const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> ConfigFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "appsettings.json", "secrets.json", "launchSettings.json", "web.config", "app.config", ".env"
    };

    private static readonly HashSet<string> ConfigFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json", ".config", ".env", ".xml", ".yaml", ".yml", ".ini", ".properties"
    };
    private static readonly string[] Separator = ["\r\n", "\r", "\n"];

    private string RedactSensitiveData(string originalContent, string fileName)
    {
        _logger.LogInformation(
            "[RedactSensitiveData] Called for fileName: '{FileName}'. Original content length: {Length}", fileName,
            originalContent.Length);
        var currentContent = originalContent;
        var lowerFileName = fileName.ToLowerInvariant();
        var extension = Path.GetExtension(lowerFileName);
        _logger.LogDebug("[RedactSensitiveData] lowerFileName: '{LowerFileName}', extension: '{Extension}'",
            lowerFileName, extension);


        var actualFileNameOnly = Path.GetFileName(lowerFileName);


        if (actualFileNameOnly.StartsWith(".env", StringComparison.OrdinalIgnoreCase) ||
            ((extension.Equals(".properties", StringComparison.OrdinalIgnoreCase) ||
              extension.Equals(".ini", StringComparison.OrdinalIgnoreCase))))
        {
            _logger.LogInformation(
                "[RedactSensitiveData] Processing as .env, .properties, or .ini file type for: '{FileName}'", fileName);
            var lines = originalContent.Split(Separator, StringSplitOptions.None);
            var newLines = new List<string>(lines.Length);
            _logger.LogDebug("[RedactSensitiveData] Split content into {LineCount} lines for .env processing.",
                lines.Length);
            var lineNum = 0;
            foreach (var line in lines)
            {
                lineNum++;
                _logger.LogDebug("[RedactSensitiveData] .env Line {LineNum}: Raw: '{LineContent}'", lineNum, line);
                if (line.TrimStart().StartsWith("#") || line.TrimStart().StartsWith(";") ||
                    string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogDebug("[RedactSensitiveData] .env Line {LineNum}: Keeping comment or empty line.",
                        lineNum);
                    newLines.Add(line);
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var keyPartOriginal = parts[0];
                    var key = parts[0].Trim();
                    var rawValue = parts[1].Trim();
                    _logger.LogDebug("[RedactSensitiveData] .env Line {LineNum}: Key='{Key}', RawValue='{RawValue}'",
                        lineNum, key, rawValue);

                    string redactedLine;
                    if (rawValue.Length >= 2 && rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
                    {
                        redactedLine = $"{keyPartOriginal.TrimEnd()}=" + $"\"{RedactedValue}\"";
                        _logger.LogDebug(
                            "[RedactSensitiveData] .env Line {LineNum}: Redacted double-quoted value. New line: '{RedactedLineContent}'",
                            lineNum, redactedLine);
                    }
                    else if (rawValue.Length >= 2 && rawValue.StartsWith("'") && rawValue.EndsWith("'"))
                    {
                        redactedLine = $"{keyPartOriginal.TrimEnd()}=" + $"'{RedactedValue}'";
                        _logger.LogDebug(
                            "[RedactSensitiveData] .env Line {LineNum}: Redacted single-quoted value. New line: '{RedactedLineContent}'",
                            lineNum, redactedLine);
                    }
                    else
                    {
                        redactedLine = $"{keyPartOriginal.TrimEnd()}=" + $"{RedactedValue}";
                        _logger.LogDebug(
                            "[RedactSensitiveData] .env Line {LineNum}: Redacted unquoted value. New line: '{RedactedLineContent}'",
                            lineNum, redactedLine);
                    }

                    newLines.Add(redactedLine);
                }
                else
                {
                    _logger.LogDebug(
                        "[RedactSensitiveData] .env Line {LineNum}: Line does not contain '=' or is malformed, keeping as is.",
                        lineNum);
                    newLines.Add(line);
                }
            }

            currentContent = string.Join(Environment.NewLine, newLines);
            _logger.LogDebug(
                "[RedactSensitiveData] Finished .env processing for: '{FileName}'. New content length: {Length}",
                fileName, currentContent.Length);
        }

        else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[RedactSensitiveData] Processing as .json file type for: '{FileName}'", fileName);

            currentContent = Regex.Replace(currentContent, @":\s*""(?:\\""|[^""])*""", $": \"{RedactedValue}\"");

            currentContent = Regex.Replace(currentContent, @":\s*(-?\d+(\.\d+)?([eE][+-]?\d+)?)\b",
                $": \"{RedactedValue}\"");

            currentContent = Regex.Replace(currentContent, @":\s*(true|false)\b", $": \"{RedactedValue}\"");

            currentContent = Regex.Replace(currentContent, @":\s*(null)\b", $": \"{RedactedValue}\"");
            _logger.LogDebug(
                "[RedactSensitiveData] Finished .json processing for: '{FileName}'. New content length: {Length}",
                fileName, currentContent.Length);
        }

        else if ((extension.Equals(".xml", StringComparison.OrdinalIgnoreCase) ||
                  extension.Equals(".config", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("[RedactSensitiveData] Processing as .xml or .config file type for: '{FileName}'",
                fileName);

            currentContent = Regex.Replace(currentContent, @"(value\s*=\s*"")((?:\\.|[^""\\])*)(""\s*(?:/>|>))",
                match => $"{match.Groups[1].Value}{RedactedValue}{match.Groups[3].Value}",
                RegexOptions.IgnoreCase);

            currentContent = Regex.Replace(currentContent,
                @"(connectionString\s*=\s*"")((?:\\.|[^""\\])*)(""\s*(?:/>|>))",
                match => $"{match.Groups[1].Value}{RedactedValue}{match.Groups[3].Value}",
                RegexOptions.IgnoreCase);
            _logger.LogDebug(
                "[RedactSensitiveData] Finished .xml/.config processing for: '{FileName}'. New content length: {Length}",
                fileName, currentContent.Length);
        }

        else if ((extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) ||
                  extension.Equals(".yml", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("[RedactSensitiveData] Processing as .yaml or .yml file type for: '{FileName}'",
                fileName);
            var lines = originalContent.Split(Separator, StringSplitOptions.None);
            var newLines = new List<string>(lines.Length);
            var yamlKeyValueRegex = new Regex(@"^(\s*[a-zA-Z0-9_.-]+\s*:\s*)(.*)$");
            var lineNum = 0;
            foreach (var line in lines)
            {
                lineNum++;
                _logger.LogDebug("[RedactSensitiveData] .yaml Line {LineNum}: Raw: '{LineContent}'", lineNum, line);
                if (line.TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    newLines.Add(line);
                    continue;
                }

                var match = yamlKeyValueRegex.Match(line);
                if (match.Success)
                {
                    var keyPart = match.Groups[1].Value;
                    var valuePart = match.Groups[2].Value.TrimEnd();
                    _logger.LogDebug(
                        "[RedactSensitiveData] .yaml Line {LineNum}: KeyPart='{KeyPart}', ValuePart='{ValuePart}'",
                        lineNum, keyPart, valuePart);

                    if ((valuePart.StartsWith("\"") && valuePart.EndsWith("\"")) ||
                        (valuePart.StartsWith("'") && valuePart.EndsWith("'")))
                    {
                        newLines.Add($"{keyPart}\"{RedactedValue}\"");
                    }
                    else
                    {
                        newLines.Add($"{keyPart}{RedactedValue}");
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }

            currentContent = string.Join(Environment.NewLine, newLines);
            _logger.LogDebug(
                "[RedactSensitiveData] Finished .yaml/.yml processing for: '{FileName}'. New content length: {Length}",
                fileName, currentContent.Length);
        }
        else
        {
            _logger.LogInformation(
                "[RedactSensitiveData] File '{FileName}' did not match any specific config types for full redaction. No changes made by this method.",
                fileName);
        }

        return currentContent;
    }


    public class MergedContentResult
    {
        public List<MergedFileDisplayItem> MergedFilesToDisplay { get; init; } = [];
        public string MergedFileContentPlainText { get; init; } = "";
        public int EstimatedTokenCount { get; init; }
        public string ErrorMessage { get; init; } = "";
    }

    private static bool IsConfigurationFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        var lowerFileName = fileName.ToLowerInvariant();
        var actualFileNameOnly = Path.GetFileName(lowerFileName);


        if (actualFileNameOnly.StartsWith(".env"))
        {
            return true;
        }


        if (ConfigFileNames.Contains(actualFileNameOnly))
        {
            return true;
        }


        var extension = Path.GetExtension(lowerFileName);
        if (!string.IsNullOrEmpty(extension) && ConfigFileExtensions.Contains(extension))
        {
            return true;
        }


        return actualFileNameOnly.StartsWith("appsettings.") && actualFileNameOnly.EndsWith(".json");
    }

    public async Task<MergedContentResult> GenerateMergedContentAsync(
        IEnumerable<FileSystemItem> filesToMerge,
        AppSettings appSettings,
        string userPrompt,
        string? currentDisplayRootPath,
        bool includeFileTreeInOutput,
        bool privatizeDataInOutput,
        IEnumerable<FileSystemItem> allDisplayRootItems)
    {
        _logger.LogInformation("[GenerateMergedContentAsync] Starting. privatizeDataInOutput: {PrivatizeFlag}",
            privatizeDataInOutput);
        var mergedFilesToDisplay = new List<MergedFileDisplayItem>();
        var sbPlainText = new StringBuilder();
        var plainTextLanguage = new PlainTextLanguageDefinition();
        var overallErrorMessage = "";

        if (!string.IsNullOrWhiteSpace(appSettings.PrePrompt))
        {
            _logger.LogDebug("[GenerateMergedContentAsync] Adding Pre-Prompt.");
            mergedFilesToDisplay.Add(new MergedFileDisplayItem
            {
                FilePath = "SYSTEM_PRE_PROMPT",
                RelativePath = "Pre-Prompt",
                Content = appSettings.PrePrompt,
                Language = plainTextLanguage,
                TokenCount = 0
            });
            AppendSectionToPlainText(sbPlainText, "Pre-Prompt", appSettings.PrePrompt);
        }

        if (includeFileTreeInOutput && !string.IsNullOrEmpty(currentDisplayRootPath) && allDisplayRootItems.Any())
        {
            _logger.LogDebug("[GenerateMergedContentAsync] Adding File Tree.");
            try
            {
                var treeStructureString = GenerateTreeStructureString(allDisplayRootItems, currentDisplayRootPath);
                mergedFilesToDisplay.Add(new MergedFileDisplayItem
                {
                    FilePath = "SYSTEM_FILE_TREE",
                    RelativePath = "Project File Tree",
                    Content = treeStructureString,
                    Language = plainTextLanguage,
                    TokenCount = 0
                });
                AppendSectionToPlainText(sbPlainText, "Project File Tree", treeStructureString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GenerateMergedContentAsync] Error generating file tree structure for display.");
                var treeErrorMessage = $"ERROR generating file tree: {ex.Message.Shorten(100)}";
                mergedFilesToDisplay.Add(new MergedFileDisplayItem
                {
                    FilePath = "SYSTEM_FILE_TREE_ERROR",
                    RelativePath = "Project File Tree (Error)",
                    Content = treeErrorMessage,
                    Language = plainTextLanguage,
                    ErrorMessage = treeErrorMessage,
                    TokenCount = 0
                });
                AppendSectionToPlainText(sbPlainText, "Project File Tree (Error)", treeErrorMessage, isError: true);
                if (string.IsNullOrEmpty(overallErrorMessage)) overallErrorMessage = "Error generating file tree.";
            }
        }


        var fileNodesToProcess = filesToMerge.ToList();
        if (fileNodesToProcess.Count != 0)
        {
            _logger.LogInformation("[GenerateMergedContentAsync] Processing {FileCount} files.",
                fileNodesToProcess.Count);
            foreach (var fileNode in fileNodesToProcess)
            {
                _logger.LogDebug("[GenerateMergedContentAsync] Processing file: '{FilePath}', Name: '{FileName}'",
                    fileNode.FullPath, fileNode.Name);
                string fileContent;
                string? errorMessage = null;
                var langDef = GetLanguageDefinitionByFileName(fileNode.Name);
                var tokenCount = 0;

                try
                {
                    fileContent = await File.ReadAllTextAsync(fileNode.FullPath);
                    fileContent = fileContent.TrimEnd('\r', '\n');
                    _logger.LogDebug("[GenerateMergedContentAsync] Read file '{FileName}'. Content length: {Length}",
                        fileNode.Name, fileContent.Length);

                    var isConfigFile = IsConfigurationFile(fileNode.Name);
                    _logger.LogDebug(
                        "[GenerateMergedContentAsync] File '{FileName}': IsConfigurationFile returned {IsConfig}. privatizeDataInOutput is {PrivatizeFlag}",
                        fileNode.Name, isConfigFile, privatizeDataInOutput);

                    if (privatizeDataInOutput && isConfigFile)
                    {
                        _logger.LogInformation(
                            "[GenerateMergedContentAsync] Redacting data for config file: '{FileName}'", fileNode.Name);
                        fileContent = RedactSensitiveData(fileContent, fileNode.Name);
                        _logger.LogDebug(
                            "[GenerateMergedContentAsync] Finished redacting for '{FileName}'. New content length: {Length}",
                            fileNode.Name, fileContent.Length);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "[GenerateMergedContentAsync] No redaction performed for '{FileName}'. (privatizeDataInOutput: {PrivatizeFlag}, isConfigFile: {IsConfig})",
                            fileNode.Name, privatizeDataInOutput, isConfigFile);
                    }
                    
                    tokenCount = fileContent.Length / 4;

                    AppendSectionToPlainText(sbPlainText, fileNode.FullPath, fileContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GenerateMergedContentAsync] Error reading file for merging: {FilePath}",
                        fileNode.FullPath);
                    errorMessage = $"ERROR reading file: {ex.Message.Shorten(150)}";
                    fileContent = $"// {errorMessage}";
                    langDef = plainTextLanguage;
                    tokenCount = 0;

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
                    ErrorMessage = errorMessage,
                    TokenCount = tokenCount
                });
            }
        }
        else
        {
            _logger.LogInformation("[GenerateMergedContentAsync] No files selected to merge.");
        }

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            _logger.LogDebug("[GenerateMergedContentAsync] Adding User Prompt.");
            mergedFilesToDisplay.Add(new MergedFileDisplayItem
            {
                FilePath = "SYSTEM_USER_PROMPT",
                RelativePath = "User Prompt",
                Content = userPrompt,
                Language = plainTextLanguage,
                TokenCount = 0
            });
            AppendSectionToPlainText(sbPlainText, "User Prompt", userPrompt);
        }

        if (!string.IsNullOrWhiteSpace(appSettings.PostPrompt))
        {
            _logger.LogDebug("[GenerateMergedContentAsync] Adding Post-Prompt.");
            mergedFilesToDisplay.Add(new MergedFileDisplayItem
            {
                FilePath = "SYSTEM_POST_PROMPT",
                RelativePath = "Post-Prompt",
                Content = appSettings.PostPrompt,
                Language = plainTextLanguage,
                TokenCount = 0
            });
            AppendSectionToPlainText(sbPlainText, "Post-Prompt", appSettings.PostPrompt);
        }

        var finalPlainTextContent = sbPlainText.ToString().TrimEnd();
        var estimatedTokenCount = finalPlainTextContent.Length / 4;
        _logger.LogInformation(
            "[GenerateMergedContentAsync] Finished. Plain text length: {Length}, Estimated tokens: {Tokens}. ErrorMessage: '{ErrorMsg}'",
            finalPlainTextContent.Length, estimatedTokenCount, overallErrorMessage);


        if (fileNodesToProcess.Count == 0 && mergedFilesToDisplay.All(mfd => mfd.FilePath != "SYSTEM_FILE_TREE") &&
            string.IsNullOrWhiteSpace(appSettings.PrePrompt) &&
            string.IsNullOrWhiteSpace(userPrompt) &&
            string.IsNullOrWhiteSpace(appSettings.PostPrompt))
        {
            return new MergedContentResult
            {
                MergedFileContentPlainText = "", EstimatedTokenCount = 0, MergedFilesToDisplay = [],
                ErrorMessage = overallErrorMessage
            };
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
        var displayName = filePathOrSectionName;
        if (!filePathOrSectionName.StartsWith("SYSTEM_") && !filePathOrSectionName.StartsWith("Pre-Prompt") &&
            !filePathOrSectionName.StartsWith("Post-Prompt") && !filePathOrSectionName.StartsWith("User Prompt") &&
            !filePathOrSectionName.StartsWith("Project File Tree"))
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
            sb.AppendLine(content.TrimEnd('\r', '\n'));
            sb.AppendLine("//--------------------------------------------------");
            sb.AppendLine($"// End of file: {displayName}");
        }

        sb.AppendLine().AppendLine();
    }

    private static string GenerateTreeStructureString(IEnumerable<FileSystemItem> displayRootItems,
        string currentDisplayRootPath)
    {
        var sb = new StringBuilder();
        var rootDirName =
            Path.GetFileName(
                currentDisplayRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(rootDirName) && !string.IsNullOrEmpty(currentDisplayRootPath))
        {
            rootDirName = currentDisplayRootPath;
        }

        sb.AppendLine($"{rootDirName}{Path.DirectorySeparatorChar}");

        var sortedRootItems = displayRootItems.OrderBy(c => !c.IsDirectory)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        for (var i = 0; i < sortedRootItems.Count; i++)
        {
            BuildTreeStringRecursive(sb, sortedRootItems[i], "", i == sortedRootItems.Count - 1);
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    private static void BuildTreeStringRecursive(StringBuilder sb, FileSystemItem item, string indent, bool isLast)
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

        if (!item.IsDirectory || item.Children.Count == 0) return;
        var sortedChildren = item.Children.OrderBy(c => !c.IsDirectory)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        for (var i = 0; i < sortedChildren.Count; i++)
        {
            BuildTreeStringRecursive(sb, sortedChildren[i], indent, i == sortedChildren.Count - 1);
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