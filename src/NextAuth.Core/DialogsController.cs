using NextAuth.Core.Services;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Mafiles;
using NextAuth.Core.ViewModel.Other;

namespace NextAuth.Core;

public static class DialogsController
{
    public static Task<LoginAgainVM?> ShowLoginAgainDialog(string username, string? currentPassword = null)
    {
        return AppServices.Dialog.ShowLoginAgainDialogAsync(username, currentPassword);
    }

    public static Task<LoginAgainOnImportVM?> ShowLoginAgainOnImportDialog(Mafile mafile)
    {
        return AppServices.Dialog.ShowLoginAgainOnImportDialogAsync(mafile);
    }

    public static Task<string?> ShowSdaPasswordDialog()
    {
        return AppServices.Dialog.ShowSdaPasswordDialogAsync();
    }

    public static Task<MafileImportDialogResult?> ShowMafileImportDialog(int totalCount, int conflictCount)
    {
        return AppServices.Dialog.ShowMafileImportDialogAsync(totalCount, conflictCount);
    }

    public static Task ShowLinkerDialog()
    {
        return AppServices.Dialog.ShowLinkAccountDialogAsync();
    }

    public static Task ShowMafileMoverDialog()
    {
        return AppServices.Dialog.ShowMafileMoverDialogAsync();
    }

    public static Task ShowSetAccountsPasswordDialog()
    {
        return AppServices.Dialog.ShowSetPasswordsAsync(Storage.MaFiles);
    }

    public static Task ShowExportDialog()
    {
        return AppServices.Dialog.ShowExporterDialogAsync();
    }

    public static Task ShowSettingsDialog()
    {
        return AppServices.Dialog.ShowSettingsDialogAsync();
    }

    public static Task ShowLinksDialog()
    {
        return AppServices.Dialog.ShowLinksDialogAsync();
    }


    public static Task<bool> ShowConfirmCancelDialog(string? msg = null)
    {
        var title = LocManager.GetCommonOrDefault("Confirmation", "Confirmation");
        return AppServices.Dialog.ShowConfirmCancelDialogAsync(title, msg ?? string.Empty);
    }

    public static Task<string?> ShowTextFieldDialog(string? title = null, string? msg = null)
    {
        return AppServices.Dialog.ShowTextFieldDialogAsync(title ?? string.Empty, msg ?? string.Empty);
    }

    public static Task<string?> ShowSetPasswordDialog()
    {
        return AppServices.Dialog.ShowSetPasswordDialogAsync();
    }

    public static IDisposable ShowWaitLoginDialog()
    {
        return AppServices.Dialog.ShowWaitLoginDialog();
    }

    public static bool IsDialogOpen()
    {
        return AppServices.Dialog.IsDialogOpen();
    }
}