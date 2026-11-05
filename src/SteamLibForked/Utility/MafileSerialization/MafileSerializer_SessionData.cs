using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SteamLib.Authentication;
using SteamLib.Models.Session;
using SteamLib.Models.SteamIds;

namespace SteamLib.Utility.MafileSerialization;

public partial class MafileSerializer //SessionData
{
    private MobileSessionData? DeserializeMobileSessionData(JsonObject j, out DeserializedMafileSessionResult result,
        out SteamId? steamId)
    {
        steamId = GetSessionSteamId(j);
        result = DeserializedMafileSessionResult.Invalid;
        var refreshToken = GetAuthToken(j, nameof(MobileSessionData.RefreshToken), "refreshtoken", "refresh_token",
            "refresh", "OAuthToken");

        if (refreshToken is not { Type: SteamAccessTokenType.MobileRefresh })
        {
            result = DeserializedMafileSessionResult.Invalid;
            return null;
        }

        var accessToken = GetAuthToken(j, "accesstoken", "access_token", "access", "steamLoginSecure");
        if (accessToken is not { Type: SteamAccessTokenType.Mobile })
        {
            accessToken = null;
        }

        steamId = refreshToken.Value.SteamId;


        var sessionId = GetSessionId(j, Settings, "sessionid", "session_id", "session");
        if (sessionId == null)
        {
            result = DeserializedMafileSessionResult.Invalid;
            return null;
        }

        var sessionData =
            new MobileSessionData(sessionId, steamId.Value, refreshToken.Value, accessToken, tokens: null);
        if (!SessionDataValidator.Validate(null, sessionData).Succeeded)
        {
            return null;
        }

        result = DeserializedMafileSessionResult.Valid;
        return sessionData;
    }

    private static SteamAuthToken? GetAuthToken(JsonObject j, params string[] aliases)
    {
        var jAuthToken = GetToken(j, aliases);

        SteamAuthToken? token = null;
        if (jAuthToken is null) return null;
        if (jAuthToken is JsonValue val)
        {
            var strVal = val.ToString();
            if (!string.IsNullOrEmpty(strVal) && SteamTokenHelper.TryParse(strVal, out var parsed))
            {
                token = parsed;
            }
        }
        else if (jAuthToken is JsonObject obj)
        {
            try
            {
                token = JsonSerializer.Deserialize<SteamAuthToken>(obj, SerializerOptions);
            }
            catch
            {
                //Ignored
            }
        }

        return token;
    }

    private static SteamId? GetSessionSteamId(JsonObject j)
    {
        var token = GetToken(j, "steamid");
        if (token is null)
            return null;

        if (token is JsonValue val)
        {
            if (val.TryGetValue<long>(out var l))
                return SteamId.FromSteam64(l);

            var str = val.ToString();
            if (SteamId64.TryParse(str, out var steamId))
            {
                return new SteamId(steamId);
            }
        }

        return null;
    }

    private static string? GetSessionId(JsonObject j, MafileSerializerSettings settings, params string[] aliases)
    {
        var sessionId = GetString(j, aliases);
        if (sessionId == null && settings.DeserializationOptions.AllowSessionIdGeneration)
        {
            return GenerateRandomHex();
        }

        return sessionId;

        static string GenerateRandomHex(int byteLength = 12)
        {
            var randomBytes = new byte[byteLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var hex = new StringBuilder(byteLength * 2);
            foreach (var b in randomBytes)
                hex.Append($"{b:x2}");

            return hex.ToString();
        }
    }
}