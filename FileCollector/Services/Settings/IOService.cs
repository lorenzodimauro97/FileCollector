using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileCollector.Services.Settings;

public class IoService
{
    public static IEnumerable<FileSystemInfo> GetFileSystemInfosRecursive(
        string directoryPath,
        string searchPattern = "*",
        bool ignoreInaccessible = true)
    {
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
            return ignoreInaccessible
                ? []
                : throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var allInfos = new ConcurrentBag<FileSystemInfo>();
        var directories = new BlockingCollection<string>();
        directories.Add(directoryPath);
        long pendingDirectories = 1;

        int maxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount * 2);
        var tasks = new Task[maxDegreeOfParallelism];

        for (int i = 0; i < maxDegreeOfParallelism; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                while (true)
                {
                    string currentDir;
                    try
                    {
                        currentDir = directories.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }

                    try
                    {
                        var dirInfo = new DirectoryInfo(currentDir);
                        FileSystemInfo[] infos = [];
                        try
                        {
                            infos = dirInfo.GetFileSystemInfos(
                                searchPattern,
                                new EnumerationOptions
                                {
                                    IgnoreInaccessible = ignoreInaccessible,
                                    RecurseSubdirectories = false,
                                    AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                                    MatchType = MatchType.Simple,
                                    ReturnSpecialDirectories = false
                                });
                        }
                        catch (UnauthorizedAccessException) when (ignoreInaccessible) { }
                        catch (DirectoryNotFoundException) when (ignoreInaccessible) { }

                        foreach (var info in infos)
                        {
                            allInfos.Add(info);
                            if (info.Attributes.HasFlag(FileAttributes.Directory))
                            {
                                Interlocked.Increment(ref pendingDirectories);
                                directories.Add(info.FullName);
                            }
                        }
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref pendingDirectories) == 0)
                        {
                            directories.CompleteAdding();
                        }
                    }
                }
            });
        }

        Task.WaitAll(tasks);
        return allInfos;
    }
}