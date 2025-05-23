using System.Linq;

namespace FileCollector.Models;

internal static class PathPatternMatcher
{
    public static bool Match(string[] pathSegments, ProcessedPattern pPattern)
    {
        switch (pPattern.PatternSegments.Length)
        {
            case 0 when pPattern.PatternCore == "/":
                return pathSegments.Length == 0;
            case 0 when
                !string.IsNullOrEmpty(pPattern.PatternCore):
                return false;
            case 0 when
                string.IsNullOrEmpty(pPattern.PatternCore):
                return pathSegments.Length == 0;
        }


        if (pPattern.IsAnchoredToRoot)
        {
            return MatchRecursive(pathSegments, 0, pPattern.PatternSegments, 0);
        }

        if (pPattern.CoreContainsNoSlash)
        {
            var singlePatternSegment = pPattern.PatternSegments[0];


            if (pathSegments.Length > 0 && MatchSingleSegment(pathSegments.Last(), singlePatternSegment))
            {
                return true;
            }


            for (var i = 0; i < pathSegments.Length - 1; i++)
            {
                if (MatchSingleSegment(pathSegments[i], singlePatternSegment))
                {
                    return true;
                }
            }

            return false;
        }

        if (pathSegments.Where((_, i) => MatchRecursive(pathSegments, i, pPattern.PatternSegments, 0)).Any())
        {
            return true;
        }


        return pPattern.StartsWithDoubleStar && MatchRecursive(pathSegments, 0, pPattern.PatternSegments, 0);
    }

    private static bool MatchRecursive(string[] pathSegs, int pIdx, string[] patternSegs, int patIdx)
    {
        while (patIdx < patternSegs.Length)
        {
            var currentPatternSeg = patternSegs[patIdx];

            if (currentPatternSeg == "**")
            {
                patIdx++;
                if (patIdx == patternSegs.Length) return true;


                for (var i = pIdx; i < pathSegs.Length; i++)
                {
                    if (MatchRecursive(pathSegs, i, patternSegs, patIdx))
                        return true;
                }


                return MatchRecursive(pathSegs, pIdx, patternSegs, patIdx);
            }


            if (pIdx >= pathSegs.Length) return false;

            if (!MatchSingleSegment(pathSegs[pIdx], currentPatternSeg))
            {
                return false;
            }

            pIdx++;
            patIdx++;
        }


        return pIdx == pathSegs.Length;
    }


    public static bool MatchSingleSegment(string text, string pattern)
    {
        var n = text.Length;
        var m = pattern.Length;
        int i = 0, j = 0;
        var starIndex = -1;
        var textMatchIndex = -1;

        while (i < n)
        {
            if (j < m)
            {
                var pChar = pattern[j];
                switch (pChar)
                {
                    case '*':
                        starIndex = j;
                        textMatchIndex = i;
                        j++;
                        continue;
                    case '?':
                        i++;
                        j++;
                        continue;
                    case '[':
                    {
                        var closingBracket = pattern.IndexOf(']', j + 1);
                        if (closingBracket == -1) return false;

                        var setContent = pattern.Substring(j + 1, closingBracket - (j + 1));
                        j = closingBracket + 1;

                        var negate = false;
                        if (setContent.StartsWith("!"))
                        {
                            negate = true;
                            setContent = setContent[1..];
                        }

                        if (setContent.Length == 0)
                        {
                            if (starIndex != -1)
                            {
                                j = starIndex + 1;
                                i = ++textMatchIndex;
                            }
                            else return false;

                            continue;
                        }


                        var matchedInSet = false;
                        for (var k = 0; k < setContent.Length; k++)
                        {
                            if (k + 2 < setContent.Length && setContent[k + 1] == '-')
                            {
                                if (text[i] >= setContent[k] && text[i] <= setContent[k + 2])
                                {
                                    matchedInSet = true;
                                    break;
                                }

                                k += 2;
                            }
                            else
                            {
                                if (text[i] == setContent[k])
                                {
                                    matchedInSet = true;
                                    break;
                                }
                            }
                        }

                        if (negate ? matchedInSet : !matchedInSet)
                        {
                            if (starIndex != -1)
                            {
                                j = starIndex + 1;
                                i = ++textMatchIndex;
                            }
                            else return false;

                            continue;
                        }

                        i++;
                        continue;
                    }
                }
            }


            if (j < m && text[i] == pattern[j])
            {
                i++;
                j++;
                continue;
            }


            if (starIndex != -1)
            {
                j = starIndex + 1;
                i = ++textMatchIndex;
            }
            else
            {
                return false;
            }
        }


        while (j < m && pattern[j] == '*')
        {
            j++;
        }

        return j == m;
    }
}