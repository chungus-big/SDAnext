using NextAuth.Core.Model.Entities;
using NextAuth.Core.ViewModel.Other;

namespace NextAuth.Core.Services;

public interface IDialogService
{
    Task<LoginAgainVM?> ShowLoginAgainDialogAsync(string username, string? currentPassword = null);
    Task<LoginAgainOnImportVM?> ShowLoginAgainOnImportDialogAsync(Mafile mafile);
    Task<string?> ShowSdaPasswordDialogAsync();
    Task<MafileImportDialogResult?> ShowMafileImportDialogAsync(int totalCount, int conflictCount);
    Task<bool> ShowSetPasswordsAsync(IEnumerable<Mafile> mafiles);
    Task<bool> ShowSettingsDialogAsync();
    Task<bool> ShowLinkAccountDialogAsync();
    Task<bool> ShowMafileMoverDialogAsync();
    Task<bool> ShowExporterDialogAsync();
    Task<bool> ShowLinksDialogAsync();
    Task<bool> ShowConfirmCancelDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");
    Task<string?> ShowTextFieldDialogAsync(string title, string hint, string defaultText = "");
    Task<string?> ShowSetPasswordDialogAsync();
    IDisposable ShowWaitLoginDialog();
    bool IsDialogOpen();
}
