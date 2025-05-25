using System.Collections.Generic;

namespace FileCollector.Models;

public class SavedContext
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public List<string> SelectedFilePaths { get; set; } = [];
}

public class UpdateSettings
{
    public string GitHubRepoOwner { get; set; } = "lorenzodimauro97";
    public string GitHubRepoName { get; set; } = "FileCollector";
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public string UpdaterExecutableName { get; set; } = "FileCollector.Updater.exe"; // Or similar
}

public class AppSettings
{
    public List<string> IgnorePatterns { get; set; } = [];
    public string PrePrompt { get; set; } = string.Empty;
    public string PostPrompt { get; set; } = string.Empty;
    public List<SavedContext> SavedContexts { get; set; } = [];
    public UpdateSettings Update { get; set; } = new();
}
    
public class AppConfiguration
{
    public AppSettings AppSettings { get; set; } = new();
}