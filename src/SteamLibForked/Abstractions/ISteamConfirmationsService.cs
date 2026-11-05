using SteamLib.Results;
using SteamLib.SteamMobile.Confirmations.Concrete;
using SteamLib.Web;

namespace SteamLib.Abstractions;

/// <summary>
/// Service for managing Steam Guard mobile confirmations (trades, market listings, recovery).
/// </summary>
public interface ISteamConfirmationsService
{
    /// <summary>
    /// Gets the list of pending confirmations for the specified account using an explicit <see cref="HttpClient"/>.
    /// </summary>
    Task<IEnumerable<Confirmation>> GetConfirmationsAsync(HttpClient client, MobileData data,
        SteamId steamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of pending confirmations using the session context.
    /// </summary>
    Task<IEnumerable<Confirmation>> GetConfirmationsAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a single confirmation (allow or cancel) using an explicit <see cref="HttpClient"/>.
    /// </summary>
    Task<bool> SendConfirmationAsync(HttpClient client, Confirmation confirmation, SteamId steamId,
        MobileData data, bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a single confirmation (allow or cancel) using the session context.
    /// </summary>
    Task<bool> SendConfirmationAsync(SteamSessionContext context, Confirmation confirmation,
        bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers multiple confirmations in a single batch operation using an explicit <see cref="HttpClient"/>.
    /// </summary>
    Task<bool> SendMultipleConfirmationsAsync(HttpClient client, IEnumerable<Confirmation> confirmations,
        SteamId steamId, MobileData data, bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers multiple confirmations in a single batch operation using the session context.
    /// </summary>
    Task<bool> SendMultipleConfirmationsAsync(SteamSessionContext context, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending confirmations returning a typed <see cref="SteamResult{T}"/>.
    /// </summary>
    Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(HttpClient client, MobileData data,
        SteamId steamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending confirmations using the session context returning a typed <see cref="SteamResult{T}"/>.
    /// </summary>
    Task<SteamResult<IEnumerable<Confirmation>>> GetConfirmationsResultAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a single confirmation returning a typed <see cref="SteamResult{T}"/>.
    /// </summary>
    Task<SteamResult<bool>> SendConfirmationResultAsync(HttpClient client, Confirmation confirmation, SteamId steamId,
        MobileData data, bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a single confirmation using the session context returning a typed <see cref="SteamResult{T}"/>.
    /// </summary>
    Task<SteamResult<bool>> SendConfirmationResultAsync(SteamSessionContext context, Confirmation confirmation,
        bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers multiple confirmations returning a typed <see cref="SteamResult{T}"/>.
    /// </summary>
    Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(HttpClient client, IEnumerable<Confirmation> confirmations,
        SteamId steamId, MobileData data, bool confirm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers multiple confirmations using the session context returning a typed <see cref="SteamResult{T}"/>.
    /// </summary>
    Task<SteamResult<bool>> SendMultipleConfirmationsResultAsync(SteamSessionContext context, IEnumerable<Confirmation> confirmations,
        bool confirm, CancellationToken cancellationToken = default);
}
