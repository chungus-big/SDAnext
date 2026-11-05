using NextAuth.Core.Model.Entities;
using SteamLib.Api.Mobile;
using SteamLib.Api.Services;
using SteamLib.Api.Trade;
using SteamLib.Authentication;
using SteamLib.Exceptions.Authorization;
using SteamLib.ProtoCore.Services;
using SteamLib.Results;
using SteamLib.SteamMobile.Confirmations;
using SteamLib.SteamMobile.Confirmations.Concrete;
using SteamLib.Web;

namespace NextAuth.Core.Model;

public static class MaClient
{
    private static HttpClientHandler ClientHandler { get; }
    private static HttpClient Client { get; }


    static MaClient()
    {
        var pair = ClientBuilder.BuildMobileClient();
        Client = pair.Client;
        ClientHandler = pair.Handler;
    }


    public static void SetAccount(Mafile? account)
    {
        ClientHandler.CookieContainer.ClearAllCookies();
        if (account == null) return;
        ClientHandler.CookieContainer.SetSteamMobileCookies(account.SessionData);
    }

    public static Task<IEnumerable<Confirmation>> GetConfirmations(Mafile mafile, CancellationToken cancellationToken = default)
    {
        ValidateMafile(mafile);
        return SteamMobileConfirmationsApi.GetConfirmations(Client, mafile, mafile.SessionData!.SteamId, cancellationToken);
    }

    public static Task LoginAgain(Mafile mafile, string password, bool savePassword)
    {
        return SessionHandler.LoginAgain(new HttpClientHandlerPair(Client, ClientHandler), mafile, password,
            savePassword);
    }


    public static Task RefreshSession(Mafile mafile)
    {
        ValidateMafile(mafile, true);
        return SessionHandler.RefreshMobileToken(new HttpClientHandlerPair(Client, ClientHandler), mafile);
    }

    public static async Task<bool> SendConfirmation(Mafile mafile, Confirmation confirmation, bool confirm,
        CancellationToken cancellationToken = default)
    {
        ValidateMafile(mafile);
        var res = await SteamMobileConfirmationsApi.SendConfirmation(Client, confirmation, mafile.SessionData!.SteamId,
            mafile,
            confirm, cancellationToken).ConfigureAwait(false);

        if (!res && confirmation.ConfType == ConfirmationType.Trade)
        {
            await SteamTradeApi.Acknowledge(Client, mafile.SessionData.SessionId, cancellationToken).ConfigureAwait(false);
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return res;
        }

        return await SteamMobileConfirmationsApi.SendConfirmation(Client, confirmation, mafile.SessionData!.SteamId,
            mafile,
            confirm, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<bool> SendMultipleConfirmation(Mafile mafile, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default)
    {
        var enumerable = confirmations.ToList();
        if (enumerable.Count == 0)
        {
            return false;
        }

        ValidateMafile(mafile);

        var res = await SteamMobileConfirmationsApi.SendMultipleConfirmations(Client, enumerable,
            mafile.SessionData!.SteamId,
            mafile, confirm, cancellationToken).ConfigureAwait(false);
        if (!res && enumerable.Any(c => c.ConfType == ConfirmationType.Trade))
        {
            await SteamTradeApi.Acknowledge(Client, mafile.SessionData.SessionId, cancellationToken).ConfigureAwait(false);
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return res;
        }

        return await SteamMobileConfirmationsApi.SendMultipleConfirmations(Client, enumerable,
            mafile.SessionData!.SteamId,
            mafile, confirm, cancellationToken).ConfigureAwait(false);
    }

    public static Task<RemoveAuthenticator_Response> RemoveAuthenticator(Mafile mafile,
        CancellationToken cancellationToken = default)
    {
        ValidateMafile(mafile);
        if (mafile.RevocationCode == null)
        {
            throw new InvalidOperationException("This mafile does not have R-Code");
        }

        var token = mafile.SessionData!.GetMobileToken()!;
        return SteamMobileApi.RemoveAuthenticator(Client, token.Value.Token, mafile.RevocationCode, cancellationToken);
    }

    private static void ValidateMafile(Mafile mafile, bool ignoreAccessToken = false)
    {
        if (mafile.SessionData == null) throw new SessionInvalidException();
        if (mafile.SessionData.RefreshToken.IsExpired)
            throw new SessionPermanentlyExpiredException();

        if (!ignoreAccessToken)
        {
            var access = mafile.SessionData.GetMobileToken();
            if (access == null || access.Value.IsExpired)
                throw new SessionPermanentlyExpiredException();
        }
    }

    public static async Task<LoginConfirmationResult> ConfirmLoginRequest(Mafile mafile)
    {
        ValidateMafile(mafile);
        var token = mafile.SessionData!.GetMobileToken()!.Value;
        var sessions = await AuthenticationServiceApi.GetAuthSessionsForAccount(Client, token.Token);

        if (sessions.ClientIds.Count == 0)
        {
            return new LoginConfirmationResult
            {
                Error = LoginConfirmationError.NoRequests
            };
        }

        if (sessions.ClientIds.Count > 1)
        {
            return new LoginConfirmationResult
            {
                Error = LoginConfirmationError.MoreThanOneRequest
            };
        }

        var clientId = sessions.ClientIds.Single();
        var clientInfo = await AuthenticationServiceApi.GetAuthSessionInfo(Client, token.Token, clientId);
        var updateReq =
            AuthRequestHelper.CreateMobileConfirmationRequest(1, clientId, mafile.SessionData.SteamId.Steam64,
                mafile.SharedSecret);
        await AuthenticationServiceApi.UpdateAuthSessionWithMobileConfirmation(Client, token.Token, updateReq);
        return new LoginConfirmationResult
        {
            Country = clientInfo.Country,
            IP = clientInfo.IP,
            Success = true
        };
    }

    public static HttpClientHandlerPair GetHttpClientHandlerPair(Mafile? _ = null)
    {
        return new HttpClientHandlerPair(Client, ClientHandler);
    }

    public static SteamSessionContext GetSessionContext(Mafile? account = null)
    {
        if (account == null)
            return new SteamSessionContext(ClientHandler.CookieContainer) { Client = Client };
        return account.ToSessionContext(ClientHandler.CookieContainer, Client);
    }

    public static Task<IEnumerable<Confirmation>> GetConfirmations(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SteamMobileConfirmationsApi.GetConfirmations(Client, context, cancellationToken);
    }

    public static Task<bool> SendConfirmation(SteamSessionContext context, Confirmation confirmation, bool confirm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SteamMobileConfirmationsApi.SendConfirmation(Client, context, confirmation, confirm, cancellationToken);
    }

    public static Task<bool> SendMultipleConfirmation(SteamSessionContext context, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SteamMobileConfirmationsApi.SendMultipleConfirmations(Client, context, confirmations, confirm, cancellationToken);
    }

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResult(Mafile mafile,
        CancellationToken cancellationToken = default)
    {
        ValidateMafile(mafile);
        return SteamMobileConfirmationsApi.GetConfirmationsResultAsync(Client, mafile, mafile.SessionData!.SteamId, cancellationToken);
    }

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResult(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SteamMobileConfirmationsApi.GetConfirmationsResultAsync(Client, context, cancellationToken);
    }

    public static async Task<SteamResult<bool>> SendConfirmationResult(Mafile mafile, Confirmation confirmation, bool confirm,
        CancellationToken cancellationToken = default)
    {
        ValidateMafile(mafile);
        var res = await SteamMobileConfirmationsApi.SendConfirmationResultAsync(Client, confirmation, mafile.SessionData!.SteamId,
            mafile, confirm, cancellationToken).ConfigureAwait(false);

        if (res.IsFailure && confirmation.ConfType == ConfirmationType.Trade)
        {
            await SteamTradeApi.Acknowledge(Client, mafile.SessionData.SessionId, cancellationToken).ConfigureAwait(false);
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            return await SteamMobileConfirmationsApi.SendConfirmationResultAsync(Client, confirmation, mafile.SessionData!.SteamId,
                mafile, confirm, cancellationToken).ConfigureAwait(false);
        }

        return res;
    }

    public static Task<SteamResult<bool>> SendConfirmationResult(SteamSessionContext context, Confirmation confirmation, bool confirm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SteamMobileConfirmationsApi.SendConfirmationResultAsync(Client, context, confirmation, confirm, cancellationToken);
    }

    public static async Task<SteamResult<bool>> SendMultipleConfirmationResult(Mafile mafile, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default)
    {
        var enumerable = confirmations.ToList();
        if (enumerable.Count == 0)
        {
            return SteamResult<bool>.Success(true);
        }

        ValidateMafile(mafile);

        var res = await SteamMobileConfirmationsApi.SendMultipleConfirmationsResultAsync(Client, enumerable,
            mafile.SessionData!.SteamId, mafile, confirm, cancellationToken).ConfigureAwait(false);

        if (res.IsFailure && enumerable.Any(c => c.ConfType == ConfirmationType.Trade))
        {
            await SteamTradeApi.Acknowledge(Client, mafile.SessionData.SessionId, cancellationToken).ConfigureAwait(false);
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            return await SteamMobileConfirmationsApi.SendMultipleConfirmationsResultAsync(Client, enumerable,
                mafile.SessionData!.SteamId, mafile, confirm, cancellationToken).ConfigureAwait(false);
        }

        return res;
    }

    public static Task<SteamResult<bool>> SendMultipleConfirmationResult(SteamSessionContext context, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SteamMobileConfirmationsApi.SendMultipleConfirmationsResultAsync(Client, context, confirmations, confirm, cancellationToken);
    }
}