using System.IO;
using BlazorTablerIcons;

namespace FileCollector.Utils;

public static class IconHelper
{
    public static TablerIconOutlineName GetIconForItem(string itemName, bool isDirectory, bool isExpanded = false)
    {
        if (isDirectory)
        {
            return isExpanded ? TablerIconOutlineName.FolderOpen : TablerIconOutlineName.Folder;
        }


        var extension = string.IsNullOrEmpty(itemName)
            ? string.Empty
            : Path.GetExtension(itemName).ToLowerInvariant();

        return extension switch
        {
            ".cs" => TablerIconOutlineName.FileTypeTsx,
            ".razor" => TablerIconOutlineName.BrandWindows,
            ".html" => TablerIconOutlineName.FileTypeHtml,
            ".css" => TablerIconOutlineName.FileTypeCss,
            ".js" => TablerIconOutlineName.FileTypeJs,
            ".txt" => TablerIconOutlineName.FileText,
            ".csproj" => TablerIconOutlineName.FileCode,
            ".sln" => TablerIconOutlineName.FileCode,
            ".json" => TablerIconOutlineName.FileCode2,
            ".xml" => TablerIconOutlineName.FileCode,
            ".md" => TablerIconOutlineName.Markdown,
            ".png" => TablerIconOutlineName.Photo,
            ".jpg" => TablerIconOutlineName.Photo,
            ".jpeg" => TablerIconOutlineName.Photo,
            ".gif" => TablerIconOutlineName.Photo,
            ".zip" => TablerIconOutlineName.Zip,
            ".pdf" => TablerIconOutlineName.FileTypePdf,
            ".doc" => TablerIconOutlineName.FileTypeDoc,
            ".docx" => TablerIconOutlineName.FileTypeDocx,
            ".xls" => TablerIconOutlineName.FileTypeXls,
            ".xlsx" => TablerIconOutlineName.FileTypeXls,
            ".ico" => TablerIconOutlineName.Photo,
            ".sql" => TablerIconOutlineName.FileTypeSql,
            _ => TablerIconOutlineName.FileDescription,
        };
    }
}