using System;
using System.Collections.Generic;
using System.IO;

namespace FileCollector.Services.Settings;

public class IoService
{
    public IEnumerable<string> GetFileSystemEntriesRecursive(
        string directoryPath,
        string searchPattern = "*",
        bool ignoreInaccessible = true)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentNullException(nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            return ignoreInaccessible
                ? []
                : throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = ignoreInaccessible,
            RecurseSubdirectories = true,
        };

        try
        {
            return Directory.EnumerateFileSystemEntries(directoryPath, searchPattern, enumerationOptions);
        }
        catch (UnauthorizedAccessException) when (ignoreInaccessible)
        {
            return [];
        }
    }
}