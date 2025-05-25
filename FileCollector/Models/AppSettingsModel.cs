using System.Collections.Generic;

namespace FileCollector.Models;

public class SavedContext
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public List<string> SelectedFilePaths { get; set; } = [];
}

public class AppSettings
{
    public List<string> IgnorePatterns { get; set; } = [];
    public string PrePrompt { get; set; } = string.Empty;
    public string PostPrompt { get; set; } = string.Empty;
    public List<SavedContext> SavedContexts { get; set; } = [];
}
    
public class AppConfiguration
{
    public AppSettings AppSettings { get; set; } = new();
}