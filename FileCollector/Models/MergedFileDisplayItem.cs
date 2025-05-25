using Prism.Blazor;

namespace FileCollector.Models;

public class MergedFileDisplayItem
{
    public required string FilePath { get; set; }
    public required string RelativePath { get; set; }
    public required string Content { get; set; }
    public required ILanguageDefinition Language { get; set; }
    public string? ErrorMessage { get; set; }
}