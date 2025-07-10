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
        
        if (infoList.Count == 0) return [];

        var allNodes = new ConcurrentDictionary<string, FileSystemItem>(StringComparer.OrdinalIgnoreCase);
        var childrenLookup = new ConcurrentDictionary<string, ConcurrentBag<FileSystemItem>>(StringComparer.OrdinalIgnoreCase);

        stepStopwatch.Restart();
        infoList.AsParallel().ForAll(info =>
        {
            var isDirectory = info.Attributes.HasFlag(FileAttributes.Directory);
            var node = new FileSystemItem(info.FullName, isDirectory);
            allNodes.TryAdd(info.FullName, node);

            var parentPath = Path.GetDirectoryName(info.FullName);
            if (!string.IsNullOrEmpty(parentPath))
            {
                childrenLookup.AddOrUpdate(
                    parentPath,
                    key => new ConcurrentBag<FileSystemItem> { node },
                    (key, bag) => { bag.Add(node); return bag; }
                );
            }
        });
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.CreateNodesAndChildrenLookup completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        stepStopwatch.Restart();

        allNodes.Values.AsParallel().ForAll(node =>
        {
            if (childrenLookup.TryGetValue(node.FullPath, out var children))
            {
                node.Children = children
                    .OrderBy(c => !c.IsDirectory)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var parentPath = Path.GetDirectoryName(node.FullPath);
            if (!string.IsNullOrEmpty(parentPath))
            {
                if (allNodes.TryGetValue(parentPath, out var parentNode))
                {
                    node.Parent = parentNode;
                }
            }
        });
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.AssignParentsAndChildren completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        stepStopwatch.Restart();

        var rootItems = allNodes.Values.AsParallel()
            .Where(n => n.Parent == null)
            .OrderBy(c => !c.IsDirectory)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
            
        stepStopwatch.Stop();
        logger.LogDebug("Perf: PathConverter.FinalizeRoots completed in {ElapsedMilliseconds}ms.", stepStopwatch.ElapsedMilliseconds);
        
        overallStopwatch.Stop();
        logger.LogInformation("Perf: PathConverterService.BuildTree completed in {ElapsedMilliseconds}ms for {Count} paths.", 
            overallStopwatch.ElapsedMilliseconds, infoList.Count);

        return rootItems;
    }
}