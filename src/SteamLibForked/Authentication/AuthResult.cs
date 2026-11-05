using SteamLib.Models.Session;

namespace SteamLib.Authentication;

/// <summary>
/// Discriminated union representing all possible outcomes of a Steam Login V2 authentication flow.
/// </summary>
public abstract record AuthResult
{
    /// <summary>
    /// Indicates whether the authentication succeeded.
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// Authentication completed successfully, yielding session tokens and cookies.
    /// </summary>
    public sealed record Success(SessionData Session) : AuthResult
    {
        public MobileSessionData? MobileSession => Session as MobileSessionData;
    }

    /// <summary>
    /// Steam Guard 2FA mobile authenticator code (device code) is required to proceed.
    /// </summary>
    public sealed record NeedDeviceCode(ulong ClientId, SteamId SteamId) : AuthResult;

    /// <summary>
    /// Steam Guard email confirmation code is required to proceed.
    /// </summary>
    public sealed record NeedEmailCode(ulong ClientId, SteamId SteamId, string EmailDomain = "") : AuthResult;

    /// <summary>
    /// Invalid username or password supplied.
    /// </summary>
    public sealed record InvalidCredentials(string Message = "Invalid username or password") : AuthResult;

    /// <summary>
    /// Rate limit reached (HTTP 429 or Steam rate limit status code).
    /// </summary>
    public sealed record RateLimited(TimeSpan? RetryAfter = null) : AuthResult;

    /// <summary>
    /// Authentication failed due to an error.
    /// </summary>
    public sealed record Failed(string Error, Exception? Exception = null) : AuthResult;

    /// <summary>
    /// Matches the current result against all possible outcomes.
    /// </summary>
    public TResult Match<TResult>(
        Func<Success, TResult> onSuccess,
        Func<NeedDeviceCode, TResult> onNeedDeviceCode,
        Func<NeedEmailCode, TResult> onNeedEmailCode,
        Func<InvalidCredentials, TResult> onInvalidCredentials,
        Func<RateLimited, TResult> onRateLimited,
        Func<Failed, TResult> onFailed)
    {
        return this switch
        {
            Success s => onSuccess(s),
            NeedDeviceCode d => onNeedDeviceCode(d),
            NeedEmailCode e => onNeedEmailCode(e),
            InvalidCredentials i => onInvalidCredentials(i),
            RateLimited r => onRateLimited(r),
            Failed f => onFailed(f),
            _ => throw new InvalidOperationException($"Unknown AuthResult: {GetType().Name}")
        };
    }
}
