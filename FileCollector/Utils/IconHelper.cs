
using System;
using System.IO;
using BlazorTablerIcons;

namespace FileCollector.Utils;

public static class IconHelper
{
    public static (TablerIconOutlineName Icon, string Color) GetIconForItem(string itemName, bool isDirectory, bool isExpanded = false)
    {
        if (isDirectory)
        {
            return (isExpanded ? TablerIconOutlineName.FolderOpen : TablerIconOutlineName.Folder, "#FFCA28");
        }

        var extension = string.IsNullOrEmpty(itemName)
            ? string.Empty
            : Path.GetExtension(itemName).ToLowerInvariant();

        return extension switch
        {
            ".cs" => (TablerIconOutlineName.FileTypeTsx, "#68217A"),
            ".razor" => (TablerIconOutlineName.BrandRadixUi, "#512BD4"),
            ".html" => (TablerIconOutlineName.FileTypeHtml, "#E44D26"),
            ".css" => (TablerIconOutlineName.FileTypeCss, "#264DE4"),
            ".js" => (TablerIconOutlineName.FileTypeJs, "#F0DB4F"),
            ".ts" => (TablerIconOutlineName.FileTypeTs, "#3178C6"),
            ".txt" => (TablerIconOutlineName.FileText, "#555555"),
            ".csproj" => (TablerIconOutlineName.FileCode, "#9B4F96"),
            ".sln" => (TablerIconOutlineName.FileCode, "#9B4F96"),
            ".json" => (TablerIconOutlineName.FileCode2, "#AAAAAA"),
            ".xml" => (TablerIconOutlineName.FileCode, "#FF6600"),
            ".md" => (TablerIconOutlineName.Markdown, "#333333"),
            ".png" => (TablerIconOutlineName.Photo, "#4CAF50"),
            ".jpg" => (TablerIconOutlineName.Photo, "#4CAF50"),
            ".jpeg" => (TablerIconOutlineName.Photo, "#4CAF50"),
            ".gif" => (TablerIconOutlineName.Photo, "#4CAF50"),
            ".ico" => (TablerIconOutlineName.Photo, "#4CAF50"),
            ".zip" => (TablerIconOutlineName.Zip, "#FFC107"),
            ".pdf" => (TablerIconOutlineName.FileTypePdf, "#B30B00"),
            ".doc" => (TablerIconOutlineName.FileTypeDoc, "#2B579A"),
            ".docx" => (TablerIconOutlineName.FileTypeDocx, "#2B579A"),
            ".xls" => (TablerIconOutlineName.FileTypeXls, "#1D6F42"),
            ".xlsx" => (TablerIconOutlineName.FileTypeXls, "#1D6F42"),
            ".sql" => (TablerIconOutlineName.FileTypeSql, "#00758F"),
            _ => (TablerIconOutlineName.FileDescription, "var(--text-color)"),
        };
    }
}