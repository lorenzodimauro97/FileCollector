// File: C:\Users\Administrator\RiderProjects\FileCollector\FileCollector\Models\LargeVersion.cs
using System;
using System.Text;

namespace FileCollector.Models;

public class LargeVersion : IComparable<LargeVersion>
{
    public long Major { get; }
    public long Minor { get; }
    public long Build { get; }
    public long Revision { get; }

    public LargeVersion(long major, long minor = 0, long build = 0, long revision = 0)
    {
        Major = major < 0 ? 0 : major;
        Minor = minor < 0 ? 0 : minor;
        Build = build < 0 ? 0 : build;
        Revision = revision < 0 ? 0 : revision;
    }

    public static bool TryParse(string? versionString, out LargeVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(versionString)) return false;

        var parts = versionString.Split('.');
        if (parts.Length == 0 || parts.Length > 4) return false; 

        long[] numParts = new long[4] { 0, 0, 0, 0 };

        for (int i = 0; i < parts.Length; i++)
        {
            if (!long.TryParse(parts[i], out long val) || val < 0)
            {
                return false; 
            }
            numParts[i] = val;
        }
        
        version = new LargeVersion(numParts[0], numParts[1], numParts[2], numParts[3]);
        return true;
    }

    public int CompareTo(LargeVersion? other)
    {
        if (other == null) return 1; 

        int majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        int minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;

        int buildCompare = Build.CompareTo(other.Build);
        if (buildCompare != 0) return buildCompare;

        return Revision.CompareTo(other.Revision);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Major);

        if (Minor != 0 || Build != 0 || Revision != 0 || (partsOriginallyParsed > 1) )
        {
            sb.Append('.');
            sb.Append(Minor);
        }
        if (Build != 0 || Revision != 0 || (partsOriginallyParsed > 2) )
        {
            sb.Append('.');
            sb.Append(Build);
        }
        if (Revision != 0 || (partsOriginallyParsed > 3) )
        {
            sb.Append('.');
            sb.Append(Revision);
        }
        return sb.ToString();
    }
    
    // Store how many parts were originally parsed to help ToString be more accurate
    // This requires TryParse to set it.
    private int partsOriginallyParsed = 1; // Default to 1 for a single number version

    public static bool TryParse(string? versionString, out LargeVersion? version, out int parsedPartsCount)
    {
        parsedPartsCount = 0;
        version = null;
        if (string.IsNullOrWhiteSpace(versionString)) return false;

        var parts = versionString.Split('.');
        if (parts.Length == 0 || parts.Length > 4) return false;

        long[] numParts = new long[4] { 0, 0, 0, 0 };

        for (int i = 0; i < parts.Length; i++)
        {
            if (!long.TryParse(parts[i], out long val) || val < 0)
            {
                return false;
            }
            numParts[i] = val;
        }
        
        version = new LargeVersion(numParts[0], numParts[1], numParts[2], numParts[3]);
        version.partsOriginallyParsed = parts.Length; // Store how many parts were in the string
        parsedPartsCount = parts.Length;
        return true;
    }
    
    public static LargeVersion FromSystemVersion(System.Version sysVersion)
    {
        var lv = new LargeVersion(
            sysVersion.Major, 
            sysVersion.Minor,
            sysVersion.Build == -1 ? 0 : sysVersion.Build, 
            sysVersion.Revision == -1 ? 0 : sysVersion.Revision
        );
        
        if (sysVersion.Revision != -1) lv.partsOriginallyParsed = 4;
        else if (sysVersion.Build != -1) lv.partsOriginallyParsed = 3;
        else lv.partsOriginallyParsed = 2; // Major and Minor always exist (default to 0 if not specified)

        return lv;
    }
}