using System.Collections.Generic;

namespace FileCollector.Models;

public class AppSettings
{
    public List<string> IgnorePatterns { get; set; } = [];
}
    
public class AppConfiguration
{
    public AppSettings AppSettings { get; set; } = new();
}