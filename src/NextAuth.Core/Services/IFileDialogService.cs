namespace NextAuth.Core.Services;

public interface IFileDialogService
{
    Task<string[]?> OpenFilesAsync(string title, string? filterName = null, string[]? extensions = null, bool multiselect = false);
    Task<string?> OpenFolderAsync(string title);
    Task<string?> SaveFileAsync(string title, string defaultFileName, string? extension = null);
}
