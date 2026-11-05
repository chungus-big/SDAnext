using DialogHostAvalonia;
using NextAuth.Core;
using NextAuth.Core.Services;
using NextAuth.Desktop.Views.Dialogs;
using NextAuth.Core.Model;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.ViewModel.Linker;
using NextAuth.Core.ViewModel.MafileMover;
using NextAuth.Core.ViewModel.Other;

namespace NextAuth.Desktop.Services;

public class AvaloniaDialogService : IDialogService
{
    public const string MAIN_HOST_ID = "MainDialogHost";

    public static void SetDialogHost(DialogHost _)
    {
    }

    public async Task<LoginAgainVM?> ShowLoginAgainDialogAsync(string username, string? currentPassword = null)
    {
        var vm = new LoginAgainVM
        {
            UserName = username,
            Password = currentPassword ?? string.Empty,
            SavePassword = PHandler.IsPasswordSet
        };
        var view = new LoginAgainView { DataContext = vm };
        var result = await DialogHost.Show(view, MAIN_HOST_ID);
        return result is true ? vm : null;
    }

    public async Task<LoginAgainOnImportVM?> ShowLoginAgainOnImportDialogAsync(Mafile mafile)
    {
        var vm = new LoginAgainOnImportVM(mafile);
        var view = new LoginAgainOnImportView { DataContext = vm };
        var result = await DialogHost.Show(view, MAIN_HOST_ID);
        return result is true ? vm : null;
    }

    public async Task<string?> ShowSdaPasswordDialogAsync()
    {
        var view = new PromptDialogView
        {
            Title = LocManager.GetOrDefault("SdaPasswordDialog", "Title", "SDA Password"),
            Hint = LocManager.GetOrDefault("SdaPasswordDialog", "PasswordHint", string.Empty),
            IsPassword = true
        };
        return await ShowPromptDialogAsync(view);
    }

    public async Task<MafileImportDialogResult?> ShowMafileImportDialogAsync(int totalCount, int conflictCount)
    {
        var vm = new MafileImportDialogVM(totalCount, conflictCount);
        var view = new MafileImportDialogView { DataContext = vm };
        var result = await DialogHost.Show(view, MAIN_HOST_ID);
        return result as MafileImportDialogResult;
    }

    public async Task<bool> ShowSetPasswordsAsync(IEnumerable<Mafile> mafiles)
    {
        var vm = new SetAccountPasswordsVM();
        var view = new SetAccountPasswordsView { DataContext = vm };
        await DialogHost.Show(view, MAIN_HOST_ID);
        return true;
    }

    public async Task<bool> ShowSettingsDialogAsync()
    {
        var vm = new SettingsVM();
        var view = new SettingsView { DataContext = vm };
        await DialogHost.Show(view, MAIN_HOST_ID);
        return true;
    }

    public async Task<bool> ShowLinkAccountDialogAsync()
    {
        using var vm = new LinkAccountVM();
        var view = new LinkerView { DataContext = vm };
        await DialogHost.Show(view, MAIN_HOST_ID);
        return true;
    }

    public async Task<bool> ShowMafileMoverDialogAsync()
    {
        using var vm = new MafileMoverVM();
        var view = new MafileMoverView { DataContext = vm };
        await DialogHost.Show(view, MAIN_HOST_ID);
        return true;
    }

    public async Task<bool> ShowExporterDialogAsync()
    {
        var vm = new MafileExporterVM();
        var view = new MafileExporterView { DataContext = vm };
        await DialogHost.Show(view, MAIN_HOST_ID);
        return true;
    }

    public async Task<bool> ShowLinksDialogAsync()
    {
        var view = new LinksView();
        await DialogHost.Show(view, MAIN_HOST_ID);
        return true;
    }

    public Task<string?> ShowTextFieldDialogAsync(string title, string hint, string defaultText = "")
    {
        var view = new PromptDialogView
        {
            Title = string.IsNullOrWhiteSpace(title) ? LocManager.GetOrDefault("TextFieldDialog", "Title", "Enter value") : title,
            Hint = hint,
            Watermark = hint,
            IsPassword = false
        };
        if (!string.IsNullOrEmpty(defaultText)) view.SetDefaultText(defaultText);
        return ShowPromptDialogAsync(view);
    }

    public Task<string?> ShowSetPasswordDialogAsync()
    {
        var view = new PromptDialogView
        {
            Title = LocManager.GetCommonOrDefault("MasterPassword", "Master password"),
            Hint = string.Empty,
            IsPassword = true
        };
        return ShowPromptDialogAsync(view);
    }

    public IDisposable ShowWaitLoginDialog()
    {
        var view = new WaitLoginView();
        try
        {
            _ = DialogHost.Show(view, MAIN_HOST_ID);
        }
        catch
        {
        }
        return new WaitLoginDisposable();
    }

    public bool IsDialogOpen()
    {
        return DialogHost.IsDialogOpen(MAIN_HOST_ID);
    }

    public async Task<bool> ShowConfirmCancelDialogAsync(string title, string message, string? confirmText = null,
        string? cancelText = null)
    {
        var view = new ConfirmCancelDialogView
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText ?? LocManager.GetCommonOrDefault("OK", "OK"),
            CancelText = cancelText ?? LocManager.GetCommonOrDefault("Cancel", "Cancel")
        };
        var result = await DialogHost.Show(view, MAIN_HOST_ID);
        return result is true;
    }

    private async Task<string?> ShowPromptDialogAsync(PromptDialogView view)
    {
        var result = await DialogHost.Show(view, MAIN_HOST_ID);
        return result as string;
    }

    private sealed class WaitLoginDisposable : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (DialogHost.IsDialogOpen(MAIN_HOST_ID))
                {
                    DialogHost.Close(MAIN_HOST_ID);
                }
            }
            catch
            {
                // Ignored
            }
        }
    }
}