using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FileCollector.Models;
using Microsoft.Extensions.Logging;

namespace FileCollector.Services;

public static class PathConverterService
{
    public static List<FileSystemItem> BuildTree(IEnumerable<FileSystemInfo> infos, ILogger logger)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var stepStopwatch = Stopwatch.StartNew();

        var infoList = infos.AsParallel().ToList();
        
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.ToList completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        stepStopwatch.Restart();

        if (infoList.Count == 0)
        {
            return [];
        }

        var allNodes = new ConcurrentDictionary<string, FileSystemItem>(StringComparer.OrdinalIgnoreCase);

        infoList.AsParallel().ForAll(info =>
        {
            var isDirectory = info.Attributes.HasFlag(FileAttributes.Directory);
            allNodes.TryAdd(info.FullName, new FileSystemItem(info.FullName, isDirectory));
        });
        
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.CreateNodes completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        stepStopwatch.Restart();

        allNodes.Values.AsParallel().ForAll(node =>
        {
            var parentPath = Path.GetDirectoryName(node.FullPath);
            if (!string.IsNullOrEmpty(parentPath) && allNodes.TryGetValue(parentPath, out var parentNode))
            {
                node.Parent = parentNode;
            }
        });
        
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.AssignParents completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        stepStopwatch.Restart();
        
        var childrenByParent = allNodes.Values.AsParallel()
            .Where(n => n.Parent != null)
            .GroupBy(n => n.Parent!);

        childrenByParent.ForAll(group =>
        {
            var parentNode = group.Key;
            parentNode.Children = group.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (!parentNode.IsDirectory && parentNode.Children.Count > 0)
            {
                parentNode.IsDirectory = true;
            }
        });
        
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.GroupAndAssignChildren completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        stepStopwatch.Restart();

        var rootItems = allNodes.Values.AsParallel()
            .Where(n => n.Parent == null)
            .OrderBy(c => !c.IsDirectory)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
            
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.FinalizeRoots completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        
        overallStopwatch.Stop();
        logger.LogInformation("Perf: PathConverterService.BuildTree completed in {ElapsedMilliseconds}ms for {Count} paths.", overallStopwatch.ElapsedMilliseconds, infoList.Count);

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
}