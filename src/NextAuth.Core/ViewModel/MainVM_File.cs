using SteamLib.Utility;
using CommunityToolkit.Mvvm.Input;
using NextAuth.Core.Services;
using NextAuth.Core.Model;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Exceptions;
using NextAuth.Core.Model.Mafiles;
using NextAuth.Core.Model.MafilesLegacy;
using NextAuth.Core.Utility;
using NextAuth.Core.ViewModel.Other;
using SteamLib.Exceptions.Authorization;

namespace NextAuth.Core.ViewModel;

public partial class MainVM // File operations - TODO: Extract import workflow into a dedicated service
{
    private record MafileReadResult(
        Mafile? Mafile,
        bool SdaPasswordPrompted,
        SDAEncryptionHelper.Context? SdaContext);

    private record MafileImportPlanItem(Mafile Mafile, bool RequiresRelogin, bool HasConflict);

    public Settings Settings => Settings.Instance;


    [RelayCommand]
    private void OpenMafileFolder()
    {
        var mafile = SelectedMafile;

        var path = Storage.MafilesDirectory;
        string? mafilePath = null;
        if (mafile != null)
        {
            mafilePath = Storage.TryGetMafilePath(mafile);
        }

        try
        {
            AppServices.App.OpenFolder(mafilePath != null
                ? Path.GetDirectoryName(mafilePath) ?? path
                : path);
        }
        catch (Exception ex)
        {
            SnackbarController.SendError(ex.Message);
        }
    }


    [RelayCommand]
    private async Task AddMafile()
    {
        var files = await AppServices.FileDialog.OpenFilesAsync("AddMafile", "Mafile", ["mafile", "maFile", "json"]);
        if (files == null || files.Length == 0) return;

        await AddMafile(files);
    }

    // TODO: Extract import workflow into a dedicated service (currently combines I/O, SDA decryption, validation, and UI prompts)
    public async Task AddMafile(string[] path)
    {
        var summary = new MafileImportSummary();
        var items = new List<MafileImportPlanItem>();
        SDAEncryptionHelper.Context? sdaContext = null;
        var sdaPasswordPrompted = false;
        foreach (var str in path)
        {
            try
            {
                var (mafile, passwordPrompted, context) = await TryReadMafile(str, sdaContext, sdaPasswordPrompted);
                sdaContext = context;
                sdaPasswordPrompted = passwordPrompted;
                if (mafile == null)
                {
                    summary.ErrorOne();
                    continue;
                }

                items.Add(new MafileImportPlanItem(mafile, false, HasImportConflict(mafile)));
            }
            catch (FormatException)
            {
                summary.ErrorOne();
            }
            catch (MafileNeedReloginException ex)
            {
                if (path.Length == 1 && ex.Mafile != null)
                {
                    items.Add(new MafileImportPlanItem(ex.Mafile, true, HasImportConflict(ex.Mafile)));
                }
                else
                {
                    summary.ErrorOne();
                    SnackbarController.SendError(
                        $"{GetLocalization("MafileImportError")} {Path.GetFileName(str)}{GetLocalization("MissingSessionInMafile")}",
                        TimeSpan.FromSeconds(4));
                }
            }
        }

        if (items.Count == 0)
        {
            ShowImportSummary(summary);
            return;
        }

        var conflictCount = items.Count(i => i.HasConflict);
        var showImportDialog = Settings.ConfirmMafileImport || conflictCount > 0;
        MafileImportDialogResult? importOptions = null;
        if (showImportDialog)
        {
            importOptions = await DialogsController.ShowMafileImportDialog(items.Count, conflictCount);
            if (importOptions == null)
            {
                SnackbarController.SendSnackbar(LocManager.GetCommonOrDefault("Canceled", "Canceled"));
                return;
            }
        }

        var overwriteConflicts = importOptions?.OverwriteConflicts ?? false;
        foreach (var item in items)
        {
            if (item.RequiresRelogin)
            {
                if (item.HasConflict && !overwriteConflicts)
                {
                    summary.SkippedOne();
                    continue;
                }

                if (await HandleAddMafileWithoutSession(item.Mafile))
                {
                    summary.AddedOne();
                }
                else
                {
                    summary.ErrorOne();
                }

                continue;
            }

            var res = await Storage.AddNewMafileFromData(item.Mafile, overwriteConflicts);
            summary.Apply(res);
        }

        ShowImportSummary(summary);
    }

    private static bool HasImportConflict(Mafile mafile)
    {
        var fileName = MafilesStorageHelper.CreateMafileFileName(mafile, Settings.Instance.UseAccountNameAsMafileName);
        return File.Exists(Path.Combine(Storage.MafilesDirectory, fileName));
    }


    private async Task<MafileReadResult> TryReadMafile(string path, SDAEncryptionHelper.Context? sdaContext,
        bool sdaPasswordPrompted)
    {
        try
        {
            var content = await File.ReadAllTextAsync(path);
            Mafile mafile;
            if (!SDAEncryptionHelper.LooksLikeSdaEncryptedBlob(content))
            {
                mafile = NextSerializer.Deserialize(content, path);
                return new MafileReadResult(mafile, sdaPasswordPrompted, sdaContext);
            }

            // Looks like encrypted, let's see if we have manifest
            sdaContext ??= SDAEncryptionHelper.TryDetect(path, sdaContext?.SdaManifest);
            if (sdaContext != null)
            {
                // We have manifest, but we must ensure we have password
                if (sdaContext.Password == null && !sdaPasswordPrompted)
                {
                    sdaPasswordPrompted = true;
                    var password = await DialogsController.ShowSdaPasswordDialog();
                    sdaContext = sdaContext.WithPassword(password);
                }

                var decrypted = SDAEncryptionHelper.TryDecrypt(content, path, sdaContext);
                if (decrypted == null) return new MafileReadResult(null, sdaPasswordPrompted, sdaContext);
                content = decrypted;
            }

            // If we are here, it means that either file is not encrypted, or we successfully decrypted it
            mafile = NextSerializer.Deserialize(content, path);
            return new MafileReadResult(mafile, sdaPasswordPrompted, sdaContext);
        }
        catch (Exception ex)
            when (ex is not MafileNeedReloginException)
        {
            throw new FormatException($"Failed to read mafile {Path.GetFileName(path)}", ex);
        }
    }

    private async Task<bool> HandleAddMafileWithoutSession(Mafile data)
    {
        var loginAgainVm = await DialogsController.ShowLoginAgainOnImportDialog(data);
        if (loginAgainVm == null)
        {
            return false;
        }

        var password = loginAgainVm.Password;

        var wait = DialogsController.ShowWaitLoginDialog();
        try
        {
            await MaClient.LoginAgain(data, password, loginAgainVm.SavePassword);
            SnackbarController.SendSuccess(GetLocalization("SuccessfulLogin"));
        }
        catch (LoginException ex)
        {
            SnackbarController.SendError(ErrorTranslatorHelper.TranslateLoginError(ex.Error),
                TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
            when (ExceptionHandler.Handle(ex))
        {
        }
        finally
        {
            wait.Dispose();
        }

        var result = data.SessionData != null;
        if (!result) return result;
        data.Filename = null;
        await Storage.SaveMafileAsync(data);
        SelectedMafile = data;
        return result;
    }

    private static void ShowImportSummary(MafileImportSummary summary)
    {
        var msg = GetLocalization("Import");
        if (summary.Added > 0)
        {
            msg += $" {GetLocalization("ImportAdded")} {summary.Added}.";
        }

        if (summary.NotAdded > 0)
        {
            msg += $" {GetLocalization("ImportSkipped")} {summary.NotAdded}.";
        }

        if (summary.Errors > 0)
        {
            msg += $" {GetLocalization("ImportErrors")} {summary.Errors}.";
        }

        if (summary.Errors > 0)
        {
            SnackbarController.SendWarning(msg, TimeSpan.FromSeconds(3));
        }
        else
        {
            SnackbarController.SendSuccess(msg, TimeSpan.FromSeconds(2.5));
        }
    }


    [RelayCommand]
    private async Task RemoveMafile()
    {
        if (SelectedMafile == null) return;
        var confirm =
            await DialogsController.ShowConfirmCancelDialog(GetLocalization("RemoveMafileConfirmation"));

        if (!confirm) return;
        try
        {
            Storage.MoveToRemoved(SelectedMafile);
        }
        catch (UnauthorizedAccessException)
        {
            SnackbarController.SendError(GetLocalization("CantRemoveAlreadyExist"));
        }
        catch (Exception ex)
        {
            SnackbarController.SendError(
                $"{GetLocalization("CantRemoveMafile")} {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenSettingsDialog()
    {
        await DialogsController.ShowSettingsDialog();
    }

    [RelayCommand]
    private async Task PasteMafilesFromClipboard()
    {
        string[] files;
        try
        {
            files = await AppServices.Clipboard.GetFilesAsync() ?? [];
        }
        catch
        {
            return;
        }

        if (files.Length == 0 || files.All(p => !p.ContainsIgnoreCase("mafile"))) return;


        await AddMafile(files);
    }

    [RelayCommand]
    private async Task CopyLogin(object? mafile)
    {
        if (mafile is not Mafile maf) return;
        if (await ClipboardHelper.Set(maf.AccountName))
            SnackbarController.SendSuccess(GetLocalization("LoginCopied"));
    }

    [RelayCommand]
    private async Task CopySteamId(object? mafile)
    {
        if (mafile is not Mafile maf) return;

        if (await ClipboardHelper.Set(maf.SteamId.ToString()))
            SnackbarController.SendSuccess(GetLocalization("SteamIdCopied"));
    }

    [RelayCommand]
    private async Task CopyMafile(object? mafile)
    {
        if (mafile is not Mafile maf) return;
        var path = Storage.TryGetMafilePath(maf);
        if (string.IsNullOrEmpty(path)) return;
        if (await ClipboardHelper.SetFiles([path]))
            SnackbarController.SendSuccess(GetLocalization("MafileCopied"));
    }

    [RelayCommand(CanExecute = nameof(CanCopyPassword))]
    private async Task CopyPassword(object? mafile)
    {
        if (mafile is not Mafile maf) return;
        if (maf.Password == null) return;
        try
        {
            var pass = PHandler.Decrypt(maf.Password);
            if (await ClipboardHelper.Set(pass))
                SnackbarController.SendSuccess(GetLocalization("PasswordCopied"));
        }
        catch
        {
            SnackbarController.SendError(GetLocalization("CantDecryptPassword"));
        }
    }

    private bool CanCopyPassword(object? mafile)
    {
        if (mafile is not Mafile maf) return false;
        return maf.Password != null && PHandler.IsPasswordSet;
    }
}