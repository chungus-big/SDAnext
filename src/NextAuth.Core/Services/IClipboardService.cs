namespace NextAuth.Core.Services;

public interface IClipboardService
{
    Task<bool> SetTextAsync(string text);
    Task<string?> GetTextAsync();
    Task<bool> SetFilesAsync(IEnumerable<string> files);
    Task<string[]?> GetFilesAsync();
}
