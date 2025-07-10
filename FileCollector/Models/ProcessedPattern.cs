using System;
using System.Linq;

namespace FileCollector.Models;

internal class ProcessedPattern
{
    public bool IsNegation { get; }
    private readonly bool _patternExpectsDirectory;
    public readonly string PatternCore;
    public readonly bool IsAnchoredToRoot;


    internal readonly string[] PatternSegments;
    internal readonly bool StartsWithDoubleStar;
    internal readonly bool CoreContainsNoSlash;


    public ProcessedPattern(string rawPatternWithPotentialNegation)
    {
        var pattern = rawPatternWithPotentialNegation;

        if (pattern.StartsWith('!'))
        {
            IsNegation = true;
            pattern = pattern[1..];
        }

        if (pattern.StartsWith('/'))
        {
            IsAnchoredToRoot = true;
            pattern = pattern[1..];
        }

        if (pattern.EndsWith('/') && pattern.Length > 1)
        {
            _patternExpectsDirectory = true;
            pattern = pattern[..^1];
        }
        else if (pattern == "/")
        {
            _patternExpectsDirectory = true;
        }


        PatternCore = pattern;
        CoreContainsNoSlash = !PatternCore.Contains('/');


        PatternSegments = string.IsNullOrEmpty(PatternCore)
            ? []
            : PatternCore.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        StartsWithDoubleStar = !IsAnchoredToRoot && PatternSegments.Length > 0 && PatternSegments[0] == "**";
    }

    public bool IsMatch(string? path, bool pathIsActuallyDirectory, Func<string, bool> isPathSegmentDirectoryHeuristic)
    {
        var pathForMatching = path;


        if (path == null) return false;
        var driveColonSlash = path.IndexOf(":/", StringComparison.InvariantCultureIgnoreCase);
        if (driveColonSlash != -1 && path.Length > driveColonSlash + 2)
        {
            pathForMatching = path[(driveColonSlash + 2)..];
        }
        else if (path.StartsWith('/'))
        {
            pathForMatching = path[1..];
        }

        var pathSegments = string.IsNullOrEmpty(pathForMatching) ? [] : pathForMatching.Split('/');


        var matched = PathPatternMatcher.Match(pathSegments, this);

        if (!matched) return false;

        if (!_patternExpectsDirectory) return true;
        if (pathIsActuallyDirectory) return true;


        if (!CoreContainsNoSlash || PatternSegments.Length != 1) return false;
        var dirNamePattern = PatternSegments[0];

        for (var i = pathSegments.Length - 1; i >= 0; i--)
        {
            if (!PathPatternMatcher.MatchSingleSegment(pathSegments[i], dirNamePattern)) continue;
            var originalPathPrefix = "";
            if (driveColonSlash != -1) originalPathPrefix = path[..(driveColonSlash + 2)];
            else if (path.StartsWith('/')) originalPathPrefix = "/";

            var dirPathToCheck = originalPathPrefix + string.Join("/", pathSegments.Take(i + 1));
            if (isPathSegmentDirectoryHeuristic(dirPathToCheck)) return true;
        }

        return false;


    }
}