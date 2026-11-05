using SteamLib.Web;
using NextAuth.Core.Services;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Utility;
using SteamLib.Exceptions.Authorization;

namespace NextAuth.Core.Model;

public static partial class SessionHandler
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static IDisposable? _waitDialog;

    public static async Task<T> Handle<T>(Func<Task<T>> func, Mafile mafile,
        HttpClientHandlerPair? chp = null, string? snackbarPrefix = null, bool allowInteractiveLogin = true)
    {
        chp ??= MaClient.GetHttpClientHandlerPair(mafile);
        await Semaphore.WaitAsync();
        try
        {
            return await HandleInternal(func, chp.Value, mafile, snackbarPrefix, allowInteractiveLogin);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task<T> HandleInternal<T>(Func<Task<T>> func, HttpClientHandlerPair chp, Mafile mafile,
        string? snackbarPrefix = null, bool allowInteractiveLogin = true)
    {
        Exception? currentException = null;

        if (!mafile.MobileTokenExpired())
        {
            try
            {
                return await func();
            }
            catch (SessionInvalidException ex)
            {
                currentException = ex;
            }
        }

        if (!mafile.RefreshTokenExpired())
        {
            var refreshed = await RefreshInternal(chp, mafile);
            if (refreshed)
            {
                SnackbarController.SendSuccess(snackbarPrefix +
                LocManager.GetCodeBehindOrDefault("SessionWasRefreshedAutomatically",
                "SessionHandler", "SessionWasRefreshedAutomatically"));
                try
                {
                    return await func();
                }
                catch (SessionInvalidException ex)
                {
                    currentException = ex;
                }
            }
        }

        if (mafile.HasPassword(out var password))
        {
            var logged = await LoginAgainInternal(chp, mafile, password, true);
            if (logged)
            {
                return await func();
            }
        }

        if (allowInteractiveLogin && !DialogsController.IsDialogOpen())
        {
            var currentPassword = GetPassword(mafile);

            var vm = await DialogsController.ShowLoginAgainDialog(mafile.AccountName, currentPassword);
            if (vm != null)
            {
                var logged = await LoginAgainInternal(chp, mafile, vm.Password, vm.SavePassword);
                if (logged)
                {
                    return await func();
                }
            }
        }

        throw new SessionPermanentlyExpiredException(SessionPermanentlyExpiredException.SESSION_EXPIRED_MSG,
            currentException);
    }

    private static async Task<bool> RefreshInternal(HttpClientHandlerPair chp, Mafile mafile)
    {
        try
        {
            await RefreshMobileToken(chp, mafile);
            return true;
        }
        catch (SessionInvalidException)
        {
            return false;
        }
    }

    private static async Task<bool> LoginAgainInternal(HttpClientHandlerPair chp, Mafile mafile, string password,
        bool savePassword)
    {
        var t = Task.Run(OnLoginStarted);
        try
        {
            await LoginAgain(chp, mafile, password, savePassword);
            return true;
        }
        catch (Exception ex) // TODO: Refine exception handling (distinguish network/proxy/socket errors from credential errors)
        {
            ExceptionHandler.Handle(ex);
            return false;
        }
        finally
        {
            OnLoginCompleted();
            await t;
        }
    }

    private static async Task OnLoginStarted()
    {
        if (DialogsController.IsDialogOpen()) return;
        var tcs = new TaskCompletionSource();
        AppServices.App.PostToUI(() =>
        {
            try
            {
                _waitDialog = DialogsController.ShowWaitLoginDialog();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        await tcs.Task;
    }

    private static void OnLoginCompleted()
    {
        var waitDialog = _waitDialog;
        _waitDialog = null;
        waitDialog?.Dispose();
    }
}