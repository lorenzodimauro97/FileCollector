using System.Collections.Generic;

namespace FileCollector.Models;

public class NavigationState
{
    public string? RootPath { get; init; }
    public List<string> SelectedFilePaths { get; init; } = [];
}