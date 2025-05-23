namespace FileCollector.Utils;

public static class StringExtensions
{
    public static string Shorten(this string? str, int maxLength, string truncationSuffix = "...")
    {
        if (string.IsNullOrEmpty(str) || maxLength <= 0) return string.Empty;

        if (str.Length <= maxLength) return str;

        if (maxLength <= truncationSuffix.Length)
        {
            return str[..maxLength];
        }
        return str[..(maxLength - truncationSuffix.Length)] + truncationSuffix;
    }
}