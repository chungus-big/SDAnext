namespace NextAuth.Core.Services;

public static class AppServices
{
    public static IClipboardService Clipboard { get; set; } = null!;
    public static IFileDialogService FileDialog { get; set; } = null!;
    public static IDialogService Dialog { get; set; } = null!;
    public static ISnackbarService Snackbar { get; set; } = null!;
    public static IAppService App { get; set; } = null!;
}
