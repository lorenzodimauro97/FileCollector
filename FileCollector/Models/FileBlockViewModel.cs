using System;

namespace FileCollector.Models; // Or FileCollector.ViewModels

public class FileBlockViewModel
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // Raw content
    public string LanguageClass { get; set; } = "language-none";
    public bool IsError { get; set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;
    public Guid UniqueId { get; } = Guid.NewGuid(); // For unique element IDs if needed
}