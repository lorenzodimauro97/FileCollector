using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileCollector.Services.Settings;

public class IoService
{
    public static IEnumerable<string> GetFileSystemEntriesRecursive(
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
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
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
    
    public static IEnumerable<FileSystemInfo> GetFileSystemInfosRecursive(
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
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            return directoryInfo.EnumerateFileSystemInfos(searchPattern, enumerationOptions);
        }
        catch (UnauthorizedAccessException) when (ignoreInaccessible)
        {
            return [];
        }
    }
}