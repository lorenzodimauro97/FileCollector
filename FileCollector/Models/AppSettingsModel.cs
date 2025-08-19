using System;
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
    public string UpdaterExecutableName { get; set; } = "FileCollector.Updater.exe";
}

public class Prompt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AppSettings
{
    public List<string> IgnorePatterns { get; set; } = [];
    
    [Obsolete("Use PrePrompts instead. This will be removed in a future version.", false)]
    public string PrePrompt { get; set; } = string.Empty;

    [Obsolete("Use PostPrompts instead. This will be removed in a future version.", false)]
    public string PostPrompt { get; set; } = string.Empty;
    
    public List<Prompt> PrePrompts { get; set; } = [];
    public List<Prompt> PostPrompts { get; set; } = [];
    public Guid? ActivePrePromptId { get; set; }
    public Guid? ActivePostPromptId { get; set; }
    
    public List<SavedContext> SavedContexts { get; set; } = [];
    public UpdateSettings Update { get; set; } = new();
    public bool PrivatizeDataInOutput { get; set; }
}
    
public class AppConfiguration
{
    public AppSettings AppSettings { get; set; } = new();
}