using SteamLib.Abstractions;
using SteamLib.Api.Mobile;
using SteamLib.Results;
using SteamLib.SteamMobile;
using SteamLib.SteamMobile.Confirmations.Concrete;
using SteamLib.Web;

namespace SteamLib.Services;

/// <summary>
/// Default implementation of <see cref="ISteamConfirmationsService"/> for managing Steam Guard confirmations.
/// </summary>
public class SteamConfirmationsService(ISteamTimeAligner? timeAligner = null) : ISteamConfirmationsService
{
    private readonly ISteamTimeAligner _timeAligner = timeAligner ?? TimeAligner.Default;

    public Task<IEnumerable<Confirmation>> GetConfirmationsAsync(HttpClient client, MobileData data,
        SteamId steamId, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.GetConfirmations(client, data, steamId, _timeAligner, cancellationToken);
    }

    public Task<IEnumerable<Confirmation>> GetConfirmationsAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.GetConfirmations(context, _timeAligner, cancellationToken);
    }

    public Task<bool> SendConfirmationAsync(HttpClient client, Confirmation confirmation, SteamId steamId,
        MobileData data, bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendConfirmation(client, confirmation, steamId, data, confirm, _timeAligner, cancellationToken);
    }

    public Task<bool> SendConfirmationAsync(SteamSessionContext context, Confirmation confirmation,
        bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendConfirmation(context, confirmation, confirm, _timeAligner, cancellationToken);
    }

    public Task<bool> SendMultipleConfirmationsAsync(HttpClient client, IEnumerable<Confirmation> confirmations,
        SteamId steamId, MobileData data, bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendMultipleConfirmations(client, confirmations, steamId, data, confirm, _timeAligner, cancellationToken);
    }

    public Task<bool> SendMultipleConfirmationsAsync(SteamSessionContext context, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendMultipleConfirmations(context, confirmations, confirm, _timeAligner, cancellationToken);
    }

    public Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(HttpClient client, MobileData data,
        SteamId steamId, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.GetConfirmationsResultAsync(client, data, steamId, _timeAligner, cancellationToken);
    }

    public Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.GetConfirmationsResultAsync(context, _timeAligner, cancellationToken);
    }

    public Task<SteamResult<bool>> SendConfirmationResultAsync(HttpClient client, Confirmation confirmation,
        SteamId steamId, MobileData data, bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendConfirmationResultAsync(client, confirmation, steamId, data, confirm, _timeAligner, cancellationToken);
    }

    public Task<SteamResult<bool>> SendConfirmationResultAsync(SteamSessionContext context, Confirmation confirmation,
        bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendConfirmationResultAsync(context, confirmation, confirm, _timeAligner, cancellationToken);
    }

    public Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(HttpClient client,
        IEnumerable<Confirmation> confirmations, SteamId steamId, MobileData data, bool confirm,
        CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendMultipleConfirmationsResultAsync(client, confirmations, steamId, data, confirm, _timeAligner, cancellationToken);
    }

    public Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(SteamSessionContext context,
        IEnumerable<Confirmation> confirmations, bool confirm, CancellationToken cancellationToken = default)
    {
        return SteamMobileConfirmationsApi.SendMultipleConfirmationsResultAsync(context, confirmations, confirm, _timeAligner, cancellationToken);
    }
}
