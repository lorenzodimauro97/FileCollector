using System;
using System.Collections.Generic;
using FileCollector.Models;

namespace FileCollector.Services;

public class GitIgnoreFilterService
{
    public IEnumerable<string> FilterPaths(IEnumerable<string> absolutePaths, IEnumerable<string> rawGitIgnorePatterns)
    {
        var patterns = PreprocessRawPatterns(rawGitIgnorePatterns);
        var includedPaths = new List<string>();

        foreach (var absPath in absolutePaths)
        {
            if (string.IsNullOrWhiteSpace(absPath)) continue;


            var hasInvalidChars = false;
            try
            {
                if (absPath.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1)
                {
                    hasInvalidChars = true;
                }
            }
            catch (ArgumentException)
            {
                hasInvalidChars = true;
            }


            if (hasInvalidChars || !(absPath.Contains(":/") || absPath.Contains(":\\")))
            {
                continue;
            }

            var normalizedPath = absPath.Replace('\\', '/');

            if (!IsPathExcluded(normalizedPath, patterns))
            {
                includedPaths.Add(absPath);
            }
        }

        return includedPaths;
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

    private bool IsPathEffectivelyDirectory(string normalizedPath)
    {
        var fileName = System.IO.Path.GetFileName(normalizedPath.TrimEnd('/'));
        return string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(System.IO.Path.GetExtension(fileName));
    }

    private bool IsPathExcluded(string normalizedPath, List<ProcessedPattern> patterns)
    {
        var pathOutcome =
            GetMatchOutcomeForSpecificPath(normalizedPath, patterns, IsPathEffectivelyDirectory(normalizedPath));

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
            if (pPattern.IsMatch(path, pathIsKnownToBeDirectory, IsPathEffectivelyDirectory))
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