using System.Net;
using SteamLib.Abstractions;
using SteamLib.Core;
using SteamLib.Core.StatusCodes;
using SteamLib.Exceptions;
using SteamLib.Exceptions.Authorization;
using SteamLib.SteamMobile;
using SteamLib.SteamMobile.Confirmations.Concrete;
using SteamLib.Results;
using SteamLib.Utility;
using SteamLib.Web;
using SteamLib.Web.Scrappers.JSON;

namespace SteamLib.Api.Mobile;

public static class SteamMobileConfirmationsApi
{
    private static ISteamConfirmationsService _default = new SteamLib.Services.SteamConfirmationsService();

    /// <summary>
    /// Gets or sets the default <see cref="ISteamConfirmationsService"/> instance.
    /// </summary>
    public static ISteamConfirmationsService Default
    {
        get => _default;
        set => _default = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static Task<IEnumerable<Confirmation>> GetConfirmations(HttpClient client, MobileData data,
        SteamId steamId, CancellationToken cancellationToken = default)
        => GetConfirmations(client, data, steamId, null, cancellationToken);

    public static async Task<IEnumerable<Confirmation>> GetConfirmations(HttpClient client, MobileData data,
        SteamId steamId, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        var nvc = await GetConfirmationKvpAsync(steamId, data.DeviceId, data.IdentitySecret, "list", timeAligner, cancellationToken).ConfigureAwait(false);

        var req = new Uri(Routes.CONF_LIST + nvc.ToQueryString());
        var reqMsg = new HttpRequestMessage(HttpMethod.Get, req);
        var resp = await client.SendAsync(reqMsg, cancellationToken).ConfigureAwait(false);


        if (resp.StatusCode == HttpStatusCode.Redirect)
        {
            throw new SessionInvalidException("Mobile session expired");
        }

        var respStr = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        return MobileConfirmationScrapper.Scrap(respStr);
    }

    public static Task<bool> SendConfirmation(HttpClient client, Confirmation confirmation, SteamId steamId,
        MobileData data, bool confirm, CancellationToken cancellationToken = default)
        => SendConfirmation(client, confirmation, steamId, data, confirm, null, cancellationToken);

    public static async Task<bool> SendConfirmation(HttpClient client, Confirmation confirmation, SteamId steamId,
        MobileData data, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        var op = confirm ? "allow" : "cancel";

        var kvp = await GetConfirmationKvpAsync(steamId, data.DeviceId, data.IdentitySecret, op, timeAligner, cancellationToken).ConfigureAwait(false);
        var query = kvp.ToList();

        var id = confirmation.Id.ToString();
        var key = confirmation.Nonce.ToString();

        query.Insert(0, new KeyValuePair<string, string>("op", op));
        query.Add(new KeyValuePair<string, string>("cid", id));
        query.Add(new KeyValuePair<string, string>("ck", key));

        var req = Routes.CONF_OP + query.ToQueryString();
        var resp = await client.GetStringAsync(req, cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618
        var successCode =
            SteamLibErrorMonitor.HandleResponse(resp, () => SteamStatusCode.Translate(resp));
#pragma warning restore CS0618

        return successCode.Equals(SteamStatusCode.Ok);
    }

    public static Task<bool> SendMultipleConfirmations(HttpClient client, IEnumerable<Confirmation> confirmations,
        SteamId steamId, MobileData data, bool confirm, CancellationToken cancellationToken = default)
        => SendMultipleConfirmations(client, confirmations, steamId, data, confirm, null, cancellationToken);

    public static async Task<bool> SendMultipleConfirmations(HttpClient client, IEnumerable<Confirmation> confirmations,
        SteamId steamId, MobileData data, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        var op = confirm ? "allow" : "cancel";
        var kvp = await GetConfirmationKvpAsync(steamId, data.DeviceId, data.IdentitySecret, op, timeAligner, cancellationToken).ConfigureAwait(false);
        var query = kvp.ToList();
        query.Insert(0, new KeyValuePair<string, string>("op", op));
        var list = confirmations.ToList();
        foreach (var confirmation in list)
        {
            query.Add(new KeyValuePair<string, string>("cid[]", confirmation.Id.ToString()));
            query.Add(new KeyValuePair<string, string>("ck[]", confirmation.Nonce.ToString()));
        }

        var content = new FormUrlEncodedContent(query);
        var resp = await client.PostAsync(Routes.MULTI_CONF_OP, content, cancellationToken).ConfigureAwait(false);
        var respStr = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618
        var successCode = SteamLibErrorMonitor.HandleResponse(respStr,
            () => SteamStatusCode.Translate(respStr));
#pragma warning restore CS0618
        return successCode.Equals(SteamStatusCode.Ok);
    }

    public static Task<IEnumerable<Confirmation>> GetConfirmations(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
        => GetConfirmations(client, context, null, cancellationToken);

    public static Task<IEnumerable<Confirmation>> GetConfirmations(HttpClient client, SteamSessionContext context,
        ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SteamId == null) throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        var mobileData = new MobileData
        {
            DeviceId = context.DeviceId ?? throw new InvalidOperationException("DeviceId is required"),
            IdentitySecret = context.IdentitySecret ?? throw new InvalidOperationException("IdentitySecret is required"),
            SharedSecret = context.SharedSecret ?? string.Empty
        };
        return GetConfirmations(client, mobileData, context.SteamId.Value, timeAligner, cancellationToken);
    }

    public static Task<IEnumerable<Confirmation>> GetConfirmations(SteamSessionContext context,
        CancellationToken cancellationToken = default)
        => GetConfirmations(context, null, cancellationToken);

    public static Task<IEnumerable<Confirmation>> GetConfirmations(SteamSessionContext context,
        ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return GetConfirmations(client, context, timeAligner, cancellationToken);
    }

    public static Task<bool> SendConfirmation(HttpClient client, SteamSessionContext context,
        Confirmation confirmation, bool confirm, CancellationToken cancellationToken = default)
        => SendConfirmation(client, context, confirmation, confirm, null, cancellationToken);

    public static Task<bool> SendConfirmation(HttpClient client, SteamSessionContext context,
        Confirmation confirmation, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SteamId == null) throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        var mobileData = new MobileData
        {
            DeviceId = context.DeviceId ?? throw new InvalidOperationException("DeviceId is required"),
            IdentitySecret = context.IdentitySecret ?? throw new InvalidOperationException("IdentitySecret is required"),
            SharedSecret = context.SharedSecret ?? string.Empty
        };
        return SendConfirmation(client, confirmation, context.SteamId.Value, mobileData, confirm, timeAligner, cancellationToken);
    }

    public static Task<bool> SendConfirmation(SteamSessionContext context,
        Confirmation confirmation, bool confirm, CancellationToken cancellationToken = default)
        => SendConfirmation(context, confirmation, confirm, null, cancellationToken);

    public static Task<bool> SendConfirmation(SteamSessionContext context,
        Confirmation confirmation, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return SendConfirmation(client, context, confirmation, confirm, timeAligner, cancellationToken);
    }

    public static Task<bool> SendMultipleConfirmations(HttpClient client, SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm, CancellationToken cancellationToken = default)
        => SendMultipleConfirmations(client, context, confirmations, confirm, null, cancellationToken);

    public static Task<bool> SendMultipleConfirmations(HttpClient client, SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SteamId == null) throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        var mobileData = new MobileData
        {
            DeviceId = context.DeviceId ?? throw new InvalidOperationException("DeviceId is required"),
            IdentitySecret = context.IdentitySecret ?? throw new InvalidOperationException("IdentitySecret is required"),
            SharedSecret = context.SharedSecret ?? string.Empty
        };
        return SendMultipleConfirmations(client, confirmations, context.SteamId.Value, mobileData, confirm, timeAligner, cancellationToken);
    }

    public static Task<bool> SendMultipleConfirmations(SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm, CancellationToken cancellationToken = default)
        => SendMultipleConfirmations(context, confirmations, confirm, null, cancellationToken);

    public static Task<bool> SendMultipleConfirmations(SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return SendMultipleConfirmations(client, context, confirmations, confirm, timeAligner, cancellationToken);
    }

    public static Task<SteamResult<bool>> SendConfirmationResultAsync(HttpClient client, Confirmation confirmation,
        SteamId steamId, MobileData data, bool confirm, CancellationToken cancellationToken = default)
        => SendConfirmationResultAsync(client, confirmation, steamId, data, confirm, null, cancellationToken);

    public static async Task<SteamResult<bool>> SendConfirmationResultAsync(HttpClient client, Confirmation confirmation,
        SteamId steamId, MobileData data, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        try
        {
            var op = confirm ? "allow" : "cancel";
            var kvp = await GetConfirmationKvpAsync(steamId, data.DeviceId, data.IdentitySecret, op, timeAligner, cancellationToken).ConfigureAwait(false);
            var query = kvp.ToList();

            var id = confirmation.Id.ToString();
            var key = confirmation.Nonce.ToString();

            query.Insert(0, new KeyValuePair<string, string>("op", op));
            query.Add(new KeyValuePair<string, string>("cid", id));
            query.Add(new KeyValuePair<string, string>("ck", key));

            var req = Routes.CONF_OP + query.ToQueryString();
            var resp = await client.GetStringAsync(req, cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618
            var successCode = SteamLibErrorMonitor.HandleResponse(resp, () => SteamStatusCode.Translate(resp));
#pragma warning restore CS0618

            if (successCode.Equals(SteamStatusCode.Ok))
            {
                return SteamResult<bool>.Success(true);
            }

            return SteamResult<bool>.Failure(successCode, $"Confirmation failed with status {successCode.Name}");
        }
        catch (SteamStatusCodeException ex)
        {
            return SteamResult<bool>.Failure(ex.StatusCode, ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SteamResult<bool>.Failure(ex.Message, SteamStatusCode.Undefined, ex);
        }
    }

    public static Task<SteamResult<bool>> SendConfirmationResultAsync(HttpClient client, SteamSessionContext context,
        Confirmation confirmation, bool confirm, CancellationToken cancellationToken = default)
        => SendConfirmationResultAsync(client, context, confirmation, confirm, null, cancellationToken);

    public static Task<SteamResult<bool>> SendConfirmationResultAsync(HttpClient client, SteamSessionContext context,
        Confirmation confirmation, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SteamId == null) throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        var mobileData = new MobileData
        {
            DeviceId = context.DeviceId ?? throw new InvalidOperationException("DeviceId is required"),
            IdentitySecret = context.IdentitySecret ?? throw new InvalidOperationException("IdentitySecret is required"),
            SharedSecret = context.SharedSecret ?? string.Empty
        };
        return SendConfirmationResultAsync(client, confirmation, context.SteamId.Value, mobileData, confirm, timeAligner, cancellationToken);
    }

    public static Task<SteamResult<bool>> SendConfirmationResultAsync(SteamSessionContext context,
        Confirmation confirmation, bool confirm, CancellationToken cancellationToken = default)
        => SendConfirmationResultAsync(context, confirmation, confirm, null, cancellationToken);

    public static Task<SteamResult<bool>> SendConfirmationResultAsync(SteamSessionContext context,
        Confirmation confirmation, bool confirm, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return SendConfirmationResultAsync(client, context, confirmation, confirm, timeAligner, cancellationToken);
    }

    public static Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(HttpClient client,
        IEnumerable<Confirmation> confirmations, SteamId steamId, MobileData data, bool confirm,
        CancellationToken cancellationToken = default)
        => SendMultipleConfirmationsResultAsync(client, confirmations, steamId, data, confirm, null, cancellationToken);

    public static async Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(HttpClient client,
        IEnumerable<Confirmation> confirmations, SteamId steamId, MobileData data, bool confirm,
        ISteamTimeAligner? timeAligner,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var list = confirmations.ToList();
            if (list.Count == 0)
            {
                return SteamResult<bool>.Success(true);
            }

            var op = confirm ? "allow" : "cancel";
            var kvp = await GetConfirmationKvpAsync(steamId, data.DeviceId, data.IdentitySecret, op, timeAligner, cancellationToken).ConfigureAwait(false);
            var query = kvp.ToList();
            query.Insert(0, new KeyValuePair<string, string>("op", op));
            foreach (var confirmation in list)
            {
                query.Add(new KeyValuePair<string, string>("cid[]", confirmation.Id.ToString()));
                query.Add(new KeyValuePair<string, string>("ck[]", confirmation.Nonce.ToString()));
            }

            var content = new FormUrlEncodedContent(query);
            var resp = await client.PostAsync(Routes.MULTI_CONF_OP, content, cancellationToken).ConfigureAwait(false);
            var respStr = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618
            var successCode = SteamLibErrorMonitor.HandleResponse(respStr,
                () => SteamStatusCode.Translate(respStr));
#pragma warning restore CS0618

            if (successCode.Equals(SteamStatusCode.Ok))
            {
                return SteamResult<bool>.Success(true);
            }

            return SteamResult<bool>.Failure(successCode, $"Multiple confirmations failed with status {successCode.Name}");
        }
        catch (SteamStatusCodeException ex)
        {
            return SteamResult<bool>.Failure(ex.StatusCode, ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SteamResult<bool>.Failure(ex.Message, SteamStatusCode.Undefined, ex);
        }
    }

    public static Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(HttpClient client,
        SteamSessionContext context, IEnumerable<Confirmation> confirmations, bool confirm,
        CancellationToken cancellationToken = default)
        => SendMultipleConfirmationsResultAsync(client, context, confirmations, confirm, null, cancellationToken);

    public static Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(HttpClient client,
        SteamSessionContext context, IEnumerable<Confirmation> confirmations, bool confirm,
        ISteamTimeAligner? timeAligner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SteamId == null) throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        var mobileData = new MobileData
        {
            DeviceId = context.DeviceId ?? throw new InvalidOperationException("DeviceId is required"),
            IdentitySecret = context.IdentitySecret ?? throw new InvalidOperationException("IdentitySecret is required"),
            SharedSecret = context.SharedSecret ?? string.Empty
        };
        return SendMultipleConfirmationsResultAsync(client, confirmations, context.SteamId.Value, mobileData, confirm, timeAligner, cancellationToken);
    }

    public static Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm, CancellationToken cancellationToken = default)
        => SendMultipleConfirmationsResultAsync(context, confirmations, confirm, null, cancellationToken);

    public static Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm,
        ISteamTimeAligner? timeAligner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return SendMultipleConfirmationsResultAsync(client, context, confirmations, confirm, timeAligner, cancellationToken);
    }

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(HttpClient client,
        MobileData data, SteamId steamId, CancellationToken cancellationToken = default)
        => GetConfirmationsResultAsync(client, data, steamId, null, cancellationToken);

    public static async Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(HttpClient client,
        MobileData data, SteamId steamId, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        try
        {
            var confs = await GetConfirmations(client, data, steamId, timeAligner, cancellationToken).ConfigureAwait(false);
            return SteamResult<IEnumerable<Confirmation>>.Success(confs);
        }
        catch (SteamStatusCodeException ex)
        {
            return SteamResult<IEnumerable<Confirmation>>.Failure(ex.StatusCode, ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SteamResult<IEnumerable<Confirmation>>.Failure(ex.Message, SteamStatusCode.Undefined, ex);
        }
    }

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(HttpClient client,
        SteamSessionContext context, CancellationToken cancellationToken = default)
        => GetConfirmationsResultAsync(client, context, null, cancellationToken);

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(HttpClient client,
        SteamSessionContext context, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SteamId == null) throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        var mobileData = new MobileData
        {
            DeviceId = context.DeviceId ?? throw new InvalidOperationException("DeviceId is required"),
            IdentitySecret = context.IdentitySecret ?? throw new InvalidOperationException("IdentitySecret is required"),
            SharedSecret = context.SharedSecret ?? string.Empty
        };
        return GetConfirmationsResultAsync(client, mobileData, context.SteamId.Value, timeAligner, cancellationToken);
    }

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default)
        => GetConfirmationsResultAsync(context, null, cancellationToken);

    public static Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(SteamSessionContext context,
        ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return GetConfirmationsResultAsync(client, context, timeAligner, cancellationToken);
    }

    internal static async ValueTask<IEnumerable<KeyValuePair<string, string>>> GetConfirmationKvpAsync(
        SteamId steamId, string deviceId, string identitySecret, string tag,
        ISteamTimeAligner? timeAligner,
        CancellationToken cancellationToken = default)
    {
        var aligner = timeAligner ?? TimeAligner.Default;
        var time = await aligner.GetSteamTimeAsync(cancellationToken).ConfigureAwait(false);
        var hash = EncryptionHelper.GenerateConfirmationHash(time, identitySecret, tag);
        return
        [
            KeyValuePair.Create("p", deviceId),
            KeyValuePair.Create("a", steamId.Steam64.ToString()),
            KeyValuePair.Create("k", hash),
            KeyValuePair.Create("t", time.ToString()),
            KeyValuePair.Create("m", "react"),
            KeyValuePair.Create("tag", tag)
        ];
    }

    internal static ValueTask<IEnumerable<KeyValuePair<string, string>>> GetConfirmationKvpAsync(
        SteamId steamId, string deviceId, string identitySecret, string tag = "conf",
        CancellationToken cancellationToken = default)
        => GetConfirmationKvpAsync(steamId, deviceId, identitySecret, tag, null, cancellationToken);

    [Obsolete("Use GetConfirmationKvpAsync instead.")]
    internal static IEnumerable<KeyValuePair<string, string>> GetConfirmationKvp(SteamId steamId, string deviceId,
        string identitySecret, string tag = "conf")
    {

        var time = TimeAligner.GetSteamTime();

        var hash = EncryptionHelper.GenerateConfirmationHash(time, identitySecret, tag);
        return
        [
            KeyValuePair.Create("p", deviceId),
            KeyValuePair.Create("a", steamId.Steam64.ToString()),
            KeyValuePair.Create("k", hash),
            KeyValuePair.Create("t", time.ToString()),
            KeyValuePair.Create("m", "react"),
            KeyValuePair.Create("tag", tag)
        ];
    }

    public static class Routes
    {
        public const string CONF = SteamConstants.STEAM_COMMUNITY + "/mobileconf";
        public const string CONF_LIST = SteamConstants.STEAM_COMMUNITY + "/mobileconf/getlist";
        public const string CONF_OP = SteamConstants.STEAM_COMMUNITY + "/mobileconf/ajaxop";
        public const string MULTI_CONF_OP = SteamConstants.STEAM_COMMUNITY + "/mobileconf/multiajaxop";
    }
}