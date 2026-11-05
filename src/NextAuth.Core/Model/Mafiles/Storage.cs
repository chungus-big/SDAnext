using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Compression;
using SteamLib.Utility;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Exceptions;
using SteamLib;
using SteamLib.Exceptions.Authorization;
using SteamLib.SteamMobile;

namespace NextAuth.Core.Model.Mafiles;

public static class Storage
{
    public const string DIR_MAFILES = "maFiles";
    public const string DIR_REMOVED_MAFILES = "maFiles_removed";
    public const string DIR_BACKUP_MAFILES = "maFiles_backup";

    public static readonly string[] MafilesDirectories;
    public static string MafilesDirectory { get; } = Path.GetFullPath(DIR_MAFILES);
    public static string RemovedMafilesDirectory { get; } = Path.GetFullPath(DIR_REMOVED_MAFILES);
    public static string BackupMafilesDirectory { get; } = Path.GetFullPath(DIR_BACKUP_MAFILES);

    public static ObservableCollection<Mafile> MaFiles { get; private set; } = [];

    static Storage()
    {
        MafilesDirectories = [MafilesDirectory, RemovedMafilesDirectory, BackupMafilesDirectory];
    }

    public static async Task Initialize(int threadCount, CancellationToken token = default)
    {
        Directory.CreateDirectory(MafilesDirectory);
        Directory.CreateDirectory(RemovedMafilesDirectory);
        Directory.CreateDirectory(BackupMafilesDirectory);

        var files = Directory
            .GetFiles(MafilesDirectory)
            .Where(file => Path.GetExtension(file).EqualsIgnoreCase(".mafile"))
            .ToList();


        var localList = new ConcurrentBag<Mafile>();
        await Task.Run(() =>
        {
            return Parallel.ForEachAsync(files,
                new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = threadCount },
                async (file, ct) =>
                {
                    try
                    {
                        var data = await ReadMafileAsync(file);
                        localList.Add(data);
                    }
                    catch
                    {
                    }
                });
        }, token);

        MaFiles = [.. localList.OrderBy(m => m.AccountName)];
    }

    /// <summary>
    /// </summary>
    /// <param name="path"></param>
    /// <param name="overwrite"></param>
    /// <returns> Result of adding mafile</returns>
    /// <exception cref="SessionInvalidException"></exception>
    public static async Task<AddMafileResult> AddNewMafile(string path, bool overwrite)
    {
        Mafile data;

        try
        {
            data = await ReadMafileAsync(path);
        }
        catch (Exception ex) when (ex is not MafileNeedReloginException)
        {
            throw new FormatException("File data is not valid", ex);
        }

        await AddNewMafileInternal(data, overwrite);
        return AddMafileResult.Added;
    }

    /// <summary>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="overwrite"></param>
    /// <returns> Result of adding mafile</returns>
    /// <exception cref="SessionInvalidException"></exception>
    public static Task<AddMafileResult> AddNewMafileFromData(Mafile data, bool overwrite)
    {
        return AddNewMafileInternal(data, overwrite);
    }

    private static async Task<AddMafileResult> AddNewMafileInternal(Mafile data, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (string.IsNullOrWhiteSpace(data.AccountName))
        {
            return AddMafileResult.Error;
        }

        try
        {
            _ = await SteamGuardCodeGenerator.GenerateCodeAsync(data.SharedSecret).ConfigureAwait(false);
        }
        catch
        {
            return AddMafileResult.Error;
        }

        data.Filename = null;

        if (!overwrite && File.Exists(MafilesStorageHelper.GetOrUpdateMafilePath(data)))
        {
            return AddMafileResult.AlreadyExist;
        }

        await SaveMafileAsync(data);
        return AddMafileResult.Added;
    }

    public static async Task<Mafile> ReadMafileAsync(string path)
    {
        var str = await File.ReadAllTextAsync(path);
        return NextSerializer.Deserialize(str, path);
    }

    public static async Task SaveMafileAsync(Mafile data)
    {
        var path = MafilesStorageHelper.GetOrUpdateMafilePath(data);
        var str = NextSerializer.SerializeMafile(data);
        await File.WriteAllTextAsync(Path.GetFullPath(path), str);

        // Replace if exist
        for (var i = 0; i < MaFiles.Count; i++)
        {
            var existed = MaFiles[i];
            if (ReferenceEquals(existed, data) || (existed.Filename != null && existed.Filename == data.Filename))
            {
                MaFiles[i] = data;
                return;
            }
        }

        MaFiles.Add(data);
    }

    public static void UpdateMafile(Mafile data)
    {
        var path = MafilesStorageHelper.GetOrUpdateMafilePath(data);
        var str = NextSerializer.SerializeMafile(data);
        File.WriteAllText(Path.GetFullPath(path), str);
    }

    public static async Task UpdateMafileAsync(Mafile data)
    {
        var path = MafilesStorageHelper.GetOrUpdateMafilePath(data);
        var str = NextSerializer.SerializeMafile(data);
        await File.WriteAllTextAsync(Path.GetFullPath(path), str);
    }

    public static void MoveToRemoved(Mafile data)
    {
        var sourcePath = MafilesStorageHelper.GetOrUpdateMafilePath(data);
        var fileName = Path.GetFileNameWithoutExtension(data.Filename!);
        var destinationPathBase = Path.Combine(DIR_REMOVED_MAFILES, fileName);
        var destinationPathFinal = Path.ChangeExtension(destinationPathBase, MafileNamingStrategy.DEF_EXTENSION);
        var i = 0;
        while (File.Exists(destinationPathFinal))
        {
            i++;
            destinationPathFinal = destinationPathBase + $" ({i})";
            destinationPathFinal = Path.ChangeExtension(destinationPathFinal, MafileNamingStrategy.DEF_EXTENSION);
        }

        File.Copy(sourcePath, destinationPathFinal, false);
        File.Delete(sourcePath);
        MaFiles.Remove(data);
    }


    public static string? TryGetMafilePath(Mafile data)
    {
        if (data.Filename == null) return null;
        return Path.Combine(MafilesDirectory, data.Filename);
    }

    public static void WriteBackup(MobileDataExtended data)
    {
        var json = NextSerializer.SerializeMafile(data, null);
        WriteBackup(data.AccountName, json);
    }

    public static void WriteBackup(string accountName, string data)
    {
        Directory.CreateDirectory(DIR_BACKUP_MAFILES);
        File.WriteAllText(Path.Combine(DIR_BACKUP_MAFILES, accountName + MafileNamingStrategy.DEF_EXTENSION), data);
    }

    public static async Task<MafilesBulkRenameResult> RenameMafiles(bool loginAsFileName,
        IProgress<double>? progress = null)
    {
        if (MaFiles.Count == 0) return new MafilesBulkRenameResult();
        var now = DateTime.Now;
        var backupFileName = $"rename_backup_{now:yyyy-MM-dd_HH-mm-ss}.zip";
        var zipPath = Path.Combine(BackupMafilesDirectory, backupFileName);
        await using (var zipStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, false);
            var files = Directory
                .EnumerateFiles(MafilesDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => Path.GetExtension(f).Equals(".mafile", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var counter = 0;
            foreach (var file in files)
            {
                counter++;
                var entry = archive.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);

                await using var entryStream = entry.Open();
                await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                    true);
                await fileStream.CopyToAsync(entryStream);

                if (counter % 5 == 0)
                {
                    progress?.Report(counter / (double)files.Count);
                    await Task.Delay(10);
                }
            }
        }

        var mafiles = MaFiles.ToList();
        var res = new MafilesBulkRenameResult
        {
            Total = mafiles.Count,
            BackupFileName = backupFileName
        };

        var dic = new Dictionary<string, string>();
        foreach (var mafile in mafiles)
        {
            try
            {
                var targetFileName = MafilesStorageHelper.CreateMafileFileName(mafile, loginAsFileName);
                if (mafile.Filename == targetFileName || mafile.Filename == null)
                {
                    res.Renamed += 1;
                    continue;
                }

                var sourceFileName = mafile.Filename;
                var fullSourcePath = Path.Combine(MafilesDirectory, sourceFileName);
                var fullTargetPath = Path.Combine(MafilesDirectory, targetFileName);

                if (!File.Exists(fullSourcePath))
                {
                    IncErrors();
                    continue;
                }

                if (File.Exists(fullTargetPath))
                {
                    res.Conflict += 1;
                    continue;
                }

                File.Move(fullSourcePath, fullTargetPath);
                dic[sourceFileName] = targetFileName;
                res.Renamed += 1;
                mafile.Filename = targetFileName;
            }
            catch
            {
                IncErrors();
            }
        }

        return res;

        void IncErrors()
        {
            res.Errors += 1;
        }
    }
}