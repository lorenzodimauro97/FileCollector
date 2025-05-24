using System.Collections.Generic;

namespace FileCollector.Models;

public class AppSettings
{
    public List<string> IgnorePatterns { get; set; } = [];
    public string PrePrompt { get; set; } = string.Empty;
    public string PostPrompt { get; set; } = string.Empty;
}
    
public class AppConfiguration
{
    public AppSettings AppSettings { get; set; } = new();
}