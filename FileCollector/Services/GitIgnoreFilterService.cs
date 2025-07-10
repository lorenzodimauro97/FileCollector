using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FileCollector.Models;
using Microsoft.Extensions.Logging;

namespace FileCollector.Services;

public class GitIgnoreFilterService(ILogger<GitIgnoreFilterService> logger)
{
    private readonly ILogger<GitIgnoreFilterService> _logger = logger;

    public IEnumerable<FileSystemInfo> FilterInfos(IEnumerable<FileSystemInfo> infos, IEnumerable<string> rawGitIgnorePatterns)
    {
        var stopwatch = Stopwatch.StartNew();
        var patterns = PreprocessRawPatterns(rawGitIgnorePatterns);
        var includedInfos = new ConcurrentBag<FileSystemInfo>();

        var infoList = infos as ICollection<FileSystemInfo> ?? infos.ToList();
        
        infoList.AsParallel().ForAll(info =>
        {
            if (string.IsNullOrWhiteSpace(info.FullName)) return;

            var normalizedPath = info.FullName.Replace('\\', '/');
            var isDirectory = info.Attributes.HasFlag(FileAttributes.Directory);

            if (!IsPathExcluded(normalizedPath, patterns, isDirectory))
            {
                includedInfos.Add(info);
            }
        });

        stopwatch.Stop();
        _logger.LogDebug("Perf: GitIgnoreFilterService.FilterInfos completed in {ElapsedMilliseconds}ms. Input: {InputCount}, Output: {OutputCount}", stopwatch.ElapsedMilliseconds, infoList.Count, includedInfos.Count);
        
        return includedInfos;
    }

    private static List<ProcessedPattern> PreprocessRawPatterns(IEnumerable<string> rawPatterns)
    {
        var processed = new List<ProcessedPattern>();
        foreach (var rawLine in rawPatterns)
        {
            var line = rawLine;


            line = line.TrimEnd();

            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("\\#"))
            {
                line = line[1..];
            }
            else if (line.StartsWith("#"))
            {
                continue;
            }


            processed.Add(new ProcessedPattern(line));
        }

        return processed;
    }
    
    private bool IsPathExcluded(string normalizedPath, List<ProcessedPattern> patterns, bool isDirectory)
    {
        var pathOutcome = GetMatchOutcomeForSpecificPath(normalizedPath, patterns, isDirectory);

        switch (pathOutcome)
        {
            case MatchOutcome.Excluded:
                return true;
            case MatchOutcome.Included:
            {
                var parent = GetParent(normalizedPath);
                while (!string.IsNullOrEmpty(parent))
                {
                    var parentOutcome = GetMatchOutcomeForSpecificPath(parent, patterns, true);
                    if (parentOutcome == MatchOutcome.Excluded)
                    {
                        return true;
                    }

                    parent = GetParent(parent);
                }

                return false;
            }
        }


        var currentParent = GetParent(normalizedPath);
        while (!string.IsNullOrEmpty(currentParent))
        {
            var parentOutcome = GetMatchOutcomeForSpecificPath(currentParent, patterns, true);
            if (parentOutcome == MatchOutcome.Excluded)
            {
                return true;
            }

            currentParent = GetParent(currentParent);
        }

        return false;
    }

    private MatchOutcome GetMatchOutcomeForSpecificPath(string path, List<ProcessedPattern> patterns,
        bool pathIsKnownToBeDirectory)
    {
        var currentOutcome = MatchOutcome.NotMatched;
        foreach (var pPattern in patterns)
        {
            if (pPattern.IsMatch(path, pathIsKnownToBeDirectory, (s) => true))
            {
                currentOutcome = pPattern.IsNegation ? MatchOutcome.Included : MatchOutcome.Excluded;
            }
        }

        return currentOutcome;
    }

    private static string GetParent(string normalizedPath)
    {
        if (string.IsNullOrEmpty(normalizedPath)) return null;


        var pathForParentCalc = normalizedPath.TrimEnd('/');
        if (string.IsNullOrEmpty(pathForParentCalc)) return null;


        if (pathForParentCalc is [_, ':']) return null;

        var lastSlash = pathForParentCalc.LastIndexOf('/');

        if (lastSlash < 0) return null;


        if (pathForParentCalc.Length > 2 && pathForParentCalc[1] == ':' && lastSlash == 2)
        {
            return pathForParentCalc[..(lastSlash + 1)];
        }


        if (lastSlash == 0 && pathForParentCalc.Length > 1) return null;


        return pathForParentCalc[..lastSlash];
    }

    private enum MatchOutcome
    {
        NotMatched,
        Excluded,
        Included
    }
}