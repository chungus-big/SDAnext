using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using NextAuth.Core.Services;

namespace NextAuth.Desktop.Services;

public class AvaloniaClipboardService(IClipboard? clipboard, IStorageProvider? storageProvider = null) : IClipboardService
{
    public Task<bool> SetTextAsync(string text) =>
        clipboard == null ? Task.FromResult(false) : Wrap(clipboard.SetTextAsync(text));

    public async Task<string?> GetTextAsync()
    {
        return clipboard == null ? null : await clipboard.TryGetTextAsync();
    }

    public async Task<bool> SetFilesAsync(IEnumerable<string> files)
    {
        if (clipboard == null) return false;
        try
        {
            var storageItems = new List<IStorageItem>();
            if (storageProvider != null)
            {
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        var storageFile = await storageProvider.TryGetFileFromPathAsync(file);
                        if (storageFile != null) storageItems.Add(storageFile);
                    }
                    else if (Directory.Exists(file))
                    {
                        var storageFolder = await storageProvider.TryGetFolderFromPathAsync(file);
                        if (storageFolder != null) storageItems.Add(storageFolder);
                    }
                }
            }

            if (storageItems.Count > 0)
            {
                await clipboard.SetFilesAsync(storageItems);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string[]?> GetFilesAsync()
    {
        if (clipboard == null) return null;
        try
        {
            var files = await clipboard.TryGetFilesAsync();
            if (files == null) return null;

            return [.. files
                .Select(f => f.TryGetLocalPath() ?? (f.Path.IsAbsoluteUri ? f.Path.LocalPath : f.Path.ToString()))
                .Where(p => !string.IsNullOrEmpty(p))];
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> Wrap(Task task)
    {
        try
        {
            await task;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
