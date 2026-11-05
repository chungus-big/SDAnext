using Avalonia.Controls;
using Avalonia.Platform.Storage;
using NextAuth.Core.Services;

namespace NextAuth.Desktop.Services;

public class AvaloniaFileDialogService(TopLevel topLevel) : IFileDialogService
{
    public async Task<string[]?> OpenFilesAsync(string title, string? filterName = null,
        string[]? extensions = null, bool multiselect = false)
    {
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = multiselect
        };
        if (extensions != null && extensions.Length > 0)
        {
            var patterns = extensions.Select(e => $"*.{e}").ToList();
            options.FileTypeFilter =
            [
                new FilePickerFileType(filterName ?? "Files")
                {
                    Patterns = patterns
                },
                FilePickerFileTypes.All
            ];
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0) return null;
        return files.Select(GetLocalPath).Where(p => !string.IsNullOrEmpty(p)).ToArray()!;
    }

    public async Task<string?> OpenFolderAsync(string title)
    {
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title
        });
        if (folders.Count == 0) return null;
        return GetLocalPath(folders[0]);
    }

    public async Task<string?> SaveFileAsync(string title, string defaultFileName, string? extension = null)
    {
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            DefaultExtension = extension
        });
        return file != null ? GetLocalPath(file) : null;
    }

    private static string? GetLocalPath(IStorageItem item)
    {
        var local = item.TryGetLocalPath();
        if (!string.IsNullOrEmpty(local)) return local;
        if (item.Path.IsAbsoluteUri) return item.Path.LocalPath;
        return item.Path.ToString();
    }
}