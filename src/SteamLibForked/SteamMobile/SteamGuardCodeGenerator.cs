using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SteamLib.Api.Services;
using SteamLib.Authentication;
using SteamLib.ProtoCore.Enums;
using SteamLib.Abstractions.Auth;

namespace SteamLib.SteamMobile;

public class SteamGuardCodeGenerator(string sharedSecret) : ISteamGuardProvider
{
    private static readonly byte[] SteamGuardCodeTranslations =
        "23456789BCDFGHJKMNPQRTVWXY"u8.ToArray();

    private string SharedSecret { get; } = sharedSecret;


    public ValueTask<string> GetSteamGuardCode() => GenerateCodeAsync();

    public ValueTask<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        return GenerateCodeAsync(SharedSecret, ct);
    }

    public static async ValueTask<string> GenerateCodeAsync(string sharedSecret, CancellationToken ct = default)
    {
        var time = await TimeAligner.GetSteamTimeAsync(ct).ConfigureAwait(false);
        return GenerateCode(sharedSecret, time);
    }

    [Obsolete("Use GenerateCodeAsync instead to avoid blocking on time alignment.")]
    public string GenerateCode()
    {
        return GenerateCode(SharedSecret);
    }

    [Obsolete("Use GenerateCodeAsync instead to avoid blocking on time alignment.")]
    public static string GenerateCode(string sharedSecret)
    {

        return GenerateCode(sharedSecret, TimeAligner.GetSteamTime());

    }

    public static string GenerateCode(string sharedSecret, long time)
    {
        ArgumentNullException.ThrowIfNull(sharedSecret);
        var sharedSecretUnescaped = sharedSecret.Contains('\\')
            ? Regex.Unescape(sharedSecret)
            : sharedSecret;

        Span<byte> secretBuffer = stackalloc byte[64];
        if (Convert.TryFromBase64Chars(sharedSecretUnescaped.AsSpan(), secretBuffer, out var bytesWritten))
        {
            try
            {
                return GenerateCode(secretBuffer[..bytesWritten], time);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretBuffer[..bytesWritten]);
            }
        }

        var sharedSecretArray = Convert.FromBase64String(sharedSecretUnescaped);
        try
        {
            return GenerateCode((ReadOnlySpan<byte>)sharedSecretArray, time);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecretArray);
        }
    }

    public static string GenerateCode(byte[] sharedSecret, long time)
    {
        ArgumentNullException.ThrowIfNull(sharedSecret);
        return GenerateCode((ReadOnlySpan<byte>)sharedSecret, time);
    }

    public static string GenerateCode(ReadOnlySpan<byte> sharedSecret, long time)
    {
        Span<byte> timeBuffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(timeBuffer, time / 30L);

        Span<byte> hashBuffer = stackalloc byte[20];
        try
        {
            HMACSHA1.HashData(sharedSecret, timeBuffer, hashBuffer);

            var b = (byte)(hashBuffer[19] & 0x0F);
            var codePoint = ((hashBuffer[b] & 0x7F) << 24) | ((hashBuffer[b + 1] & 0xFF) << 16) |
                            ((hashBuffer[b + 2] & 0xFF) << 8) | (hashBuffer[b + 3] & 0xFF);

            Span<char> codeChars = stackalloc char[5];
            for (var i = 0; i < 5; ++i)
            {
                codeChars[i] = (char)SteamGuardCodeTranslations[codePoint % SteamGuardCodeTranslations.Length];
                codePoint /= SteamGuardCodeTranslations.Length;
            }

            return new string(codeChars);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hashBuffer);
            CryptographicOperations.ZeroMemory(timeBuffer);
        }
    }

    public bool IsSupportedGuardType(ILoginConsumer consumer, EAuthSessionGuardType type)
    {
        return type == EAuthSessionGuardType.DeviceCode;
    }

    public async Task UpdateAuthSession(HttpClient authClient, ILoginConsumer loginConsumer, UpdateAuthSessionModel model,
        CancellationToken cancellationToken = default)
    {
        var code = await GenerateCodeAsync(cancellationToken).ConfigureAwait(false);
        var req = AuthRequestHelper.CreateMobileCodeRequest(code, model.ClientId, model.SteamId);
        await AuthenticationServiceApi.UpdateAuthSessionWithSteamGuardCode(authClient, req, cancellationToken).ConfigureAwait(false);
    }
}