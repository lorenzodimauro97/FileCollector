using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileCollector.Models;

namespace FileCollector.Services;

public static class PathConverterService
{
    public static List<FileSystemItem> BuildTree(IEnumerable<string> rawPaths)
    {
        var cleanedPaths = rawPaths
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(ExtractActualPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        var rootItems = new List<FileSystemItem>();
        var allNodes = new Dictionary<string, FileSystemItem>();

        foreach (var path in cleanedPaths)
        {
            if(path == null) throw new NullReferenceException("path is null");
            
            AddPathToTree(path, rootItems, allNodes);
        }


        var allPathsCopy = new List<string>(allNodes.Keys);
        foreach (var currentPath in allPathsCopy)
        {
            if (!allNodes.TryGetValue(currentPath, out var node)) continue;
            if (!node.IsDirectory && node.Children.Count != 0)
            {
                node.IsDirectory = true;
            }

            var parentPath = Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrEmpty(parentPath) && !allNodes.ContainsKey(parentPath))
            {
                AddPathToTree(currentPath, rootItems, allNodes);
            }
        }


        return rootItems;
    }

    private static string? ExtractActualPath(string rawLine)
    {
        var driveIndex = rawLine.IndexOf(":\\", StringComparison.InvariantCultureIgnoreCase);
        if (driveIndex > 0 && driveIndex - 1 < rawLine.Length)
        {
            if (char.IsLetter(rawLine[driveIndex - 1]))
            {
                return rawLine[(driveIndex - 1)..].Trim();
            }
        }


        var slashIndex = rawLine.IndexOf('/');
        if (slashIndex != -1)
        {
            string[] commonPrefixes = ["Path: ", "File: ", "Directory: "];
            foreach (var prefix in commonPrefixes)
            {
                var prefixIndex = rawLine.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (prefixIndex != -1)
                {
                    return rawLine[(prefixIndex + prefix.Length)..].Trim();
                }
            }

            if (rawLine.TrimStart().StartsWith('/')) return rawLine.Trim();
        }


        var parts = rawLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var potentialPath = parts.Last();

            if (potentialPath.Contains(Path.DirectorySeparatorChar) ||
                potentialPath.Contains(Path.AltDirectorySeparatorChar))
            {
                return potentialPath;
            }
        }


        string[] commonExtensions =
            [".cs", ".razor", ".txt", ".csproj", ".css", ".js", ".html", ".ico", ".xml", ".map"];
        if (commonExtensions.Any(ext => rawLine.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            var segments = rawLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments.Reverse())
            {
                if (commonExtensions.Any(ext => segment.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                    segment.Contains(Path.DirectorySeparatorChar) || segment.Contains(Path.AltDirectorySeparatorChar))
                    return segment;
            }
        }


        var lastWord = rawLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (lastWord != null && (lastWord.Contains(Path.DirectorySeparatorChar) ||
                                 lastWord.Contains(Path.AltDirectorySeparatorChar) || lastWord.Contains('.')))
        {
            return lastWord;
        }


        if (rawLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length == 1)
        {
            return rawLine.Trim();
        }

        Console.WriteLine($"Warning: Could not reliably extract path from: '{rawLine}'");
        return null;
    }

    private static void AddPathToTree(string path, List<FileSystemItem> rootItems,
        Dictionary<string, FileSystemItem> allNodes)
    {
        if (string.IsNullOrEmpty(path) || allNodes.ContainsKey(path))
        {
            if (allNodes.TryGetValue(path, out var existingNode) && !existingNode.IsDirectory)
            {
            }

            return;
        }


        var isLikelyDirectoryInitially = string.IsNullOrEmpty(Path.GetExtension(path));

        FileSystemItem? parentNode = null;
        var parentPath = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(parentPath))
        {
            if (!allNodes.TryGetValue(parentPath, out parentNode))
            {
                AddPathToTree(parentPath, rootItems, allNodes);
                parentNode = allNodes[parentPath];
            }

            if (parentNode is { IsDirectory: false })
            {
                parentNode.IsDirectory = true;
            }
        }


        var newNode = new FileSystemItem(path, isLikelyDirectoryInitially, parentNode);
        allNodes[path] = newNode;

        if (parentNode != null)
        {
            parentNode.Children.Add(newNode);

            if (!parentNode.IsDirectory) parentNode.IsDirectory = true;
        }
        else
        {
            rootItems.Add(newNode);
        }
    }
}