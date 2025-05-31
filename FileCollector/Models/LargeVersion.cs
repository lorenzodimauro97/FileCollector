
using System;
using System.Text;

namespace FileCollector.Models;

public class LargeVersion : IComparable<LargeVersion>
{
    public long Major { get; }
    public long Minor { get; }
    public long Build { get; }
    public long Revision { get; }


    private int _partsOriginallyParsed;

    public LargeVersion(long major, long minor = 0, long build = 0, long revision = 0)
    {
        Major = major < 0 ? 0 : major;
        Minor = minor < 0 ? 0 : minor;
        Build = build < 0 ? 0 : build;
        Revision = revision < 0 ? 0 : revision;


        if (revision != 0) _partsOriginallyParsed = 4;
        else if (build != 0) _partsOriginallyParsed = 3;
        else if (minor != 0) _partsOriginallyParsed = 2;
        else _partsOriginallyParsed = 1;


    }

    public static bool TryParse(string? versionString, out LargeVersion? version)
    {

        return TryParse(versionString, out version, out _);
    }

    public static bool TryParse(string? versionString, out LargeVersion? version, out int parsedPartsCount)
    {
        parsedPartsCount = 0;
        version = null;
        if (string.IsNullOrWhiteSpace(versionString)) return false;

        var parts = versionString.Split('.');
        if (parts.Length == 0 || parts.Length > 4) return false;

        long[] numParts = [0, 0, 0, 0];

        for (var i = 0; i < parts.Length; i++)
        {
            if (!long.TryParse(parts[i], out var val) || val < 0)
            {
                return false;
            }
            numParts[i] = val;
        }
        
        version = new LargeVersion(numParts[0], numParts[1], numParts[2], numParts[3]);
        version._partsOriginallyParsed = parts.Length;
        parsedPartsCount = parts.Length;
        return true;
    }

    public int CompareTo(LargeVersion? other)
    {
        if (other == null) return 1; 

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;

        var buildCompare = Build.CompareTo(other.Build);
        if (buildCompare != 0) return buildCompare;

        return Revision.CompareTo(other.Revision);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Major);

        if (_partsOriginallyParsed > 1 || Minor != 0 || Build != 0 || Revision != 0)
        {
            sb.Append('.');
            sb.Append(Minor);
        }
        if (_partsOriginallyParsed > 2 || Build != 0 || Revision != 0)
        {
            sb.Append('.');
            sb.Append(Build);
        }
        if (_partsOriginallyParsed > 3 || Revision != 0)
        {
            sb.Append('.');
            sb.Append(Revision);
        }
        return sb.ToString();
    }
    
    public static LargeVersion FromSystemVersion(System.Version sysVersion)
    {
        var lv = new LargeVersion(
            sysVersion.Major, 
            sysVersion.Minor,
            sysVersion.Build == -1 ? 0 : sysVersion.Build, 
            sysVersion.Revision == -1 ? 0 : sysVersion.Revision
        );
        
        if (sysVersion.Revision != -1) lv._partsOriginallyParsed = 4;
        else if (sysVersion.Build != -1) lv._partsOriginallyParsed = 3;


        else lv._partsOriginallyParsed = 2; 

        return lv;
    }
}