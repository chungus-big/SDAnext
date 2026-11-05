using System.Numerics;
using System.Text.Json.Nodes;
using SteamLib.Models.SteamIds;

namespace SteamLib.Utility.MafileSerialization;

public partial class MafileSerializer //Utility
{
    private static JsonNode? GetToken(JsonObject j, params string[] aliases)
    {
        foreach (var name in aliases)
        {
            foreach (var kv in j)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Value;
                }
            }
        }

        return null;
    }

    private static JsonNode? GetToken(JsonObject j, Dictionary<string, JsonNode?> removeFrom, params string[] aliases)
    {
        foreach (var name in aliases)
        {
            foreach (var kv in j)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    removeFrom.Remove(kv.Key);
                    return kv.Value;
                }
            }
        }

        return null;
    }

    private static JsonNode GetTokenOrThrow(JsonObject j, Dictionary<string, JsonNode?> removeFrom, string propertyName,
        params string[] aliases)
    {
        foreach (var name in aliases)
        {
            foreach (var kv in j)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (kv.Value is null)
                    {
                        throw new ArgumentException($"Required property {propertyName} is null");
                    }

                    removeFrom.Remove(kv.Key);
                    return kv.Value;
                }
            }
        }

        throw new ArgumentException($"Required property {propertyName} not found");
    }

    private static string? GetString(JsonObject j, params string[] aliases)
    {
        var token = GetToken(j, aliases);
        if (token is null)
            return null;

        if (token is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s))
                return s;
            return val.ToString();
        }

        return null;
    }

    private static string? GetString(JsonObject j, Dictionary<string, JsonNode?> removeFrom,
        params string[] aliases)
    {
        var token = GetToken(j, removeFrom, aliases);
        if (token is null)
            return null;

        if (token is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s))
                return s;
            return val.ToString();
        }
        return null;
    }

    private static long? GetLong(JsonObject j, Dictionary<string, JsonNode?> removeFrom,
        params string[] aliases)
    {
        var token = GetToken(j, removeFrom, aliases);
        if (token is null)
            return null;

        if (token is JsonValue val)
        {
            if (val.TryGetValue<long>(out var l))
                return l;
            if (val.TryGetValue<string>(out var s) && long.TryParse(s, out var parsed))
                return parsed;
        }
        return null;
    }

    private static string GetBase64(string propertyName, JsonNode token)
    {
        if (token is null) throw new ArgumentNullException(propertyName, $"{propertyName} is null");
        if (token is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s))
            {
                Validate.IsValidBase64(propertyName, s);
                return s;
            }
        }

        if (token is JsonArray arr)
        {
            try
            {
                var bytes = new byte[arr.Count];
                for (var i = 0; i < arr.Count; i++)
                {
                    bytes[i] = arr[i]!.GetValue<byte>();
                }
                var base64 = Convert.ToBase64String(bytes);
                return base64;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"{propertyName}'s Array is not valid base64", e);
            }
        }

        throw new ArgumentOutOfRangeException(nameof(token),
            $"Not valid token type for base64 property '{propertyName}'. Node: {token.GetType().Name}");
    }

    private static string GetDeviceId(JsonObject j, MafileSerializerSettings settings,
        Dictionary<string, JsonNode?> removeFrom, string propertyName,
        params string[] aliases)
    {
        var deviceId = GetString(j, removeFrom, aliases);
        if (string.IsNullOrWhiteSpace(deviceId) && !settings.DeserializationOptions.AllowDeviceIdGeneration)
        {
            throw new ArgumentException($"Required property {propertyName} not found");
        }

        return deviceId ?? GenerateDeviceId();

        static string GenerateDeviceId()
        {
            return "android:" + Guid.NewGuid();
        }
    }

    private static ulong? GetSerialNumber(JsonObject j, MafileSerializerSettings settings, string propertyName,
        Dictionary<string, JsonNode?> removeFrom,
        params string[] aliases)
    {
        var token = GetToken(j, removeFrom, aliases);
        if (token is null)
            return null;

        if (token is JsonValue val)
        {
            var strVal = val.ToString();
            if (BigInteger.TryParse(strVal, out var bigInt))
            {
                ulong res;
                if (bigInt < ulong.MinValue && bigInt > long.MinValue) // Negative overflow from signed 64-bit int in SDA, e.g. -2260921916482386064
                {
                    res = settings.DeserializationOptions.RestrictOverflowSerialNumberRecovery
                        ? 0
                        : GetFromOverflow((long)bigInt);
                }
                else if (bigInt >= ulong.MinValue && bigInt <= ulong.MaxValue) //Valid range
                {
                    res = (ulong)bigInt;
                }
                else
                {
                    res = 0;
                }

                if (res == 0 && settings.DeserializationOptions.ThrowIfInvalidSerialNumber)
                    throw new ArgumentException(
                        $"SerialNumber has invalid value. Value: '{val}'. Property: '{propertyName}'");
                return res;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(token),
            $"Not valid token type for serial number property '{propertyName}'. Node: {token.GetType().Name}");

        static ulong GetFromOverflow(long overflow)
        {
            ulong originalValue;
            unchecked
            {
                originalValue = (ulong)overflow + ulong.MaxValue + 1;
            }

            return originalValue;
        }
    }

    private static SteamId? GetSteamId(JsonObject j, Dictionary<string, JsonNode?> removeFrom,
        params string[] aliases)
    {
        var id = GetLong(j, removeFrom, aliases);
        return id switch
        {
            null or < SteamId64.SEED => null,
            _ => SteamId.FromSteam64(id.Value)
        };
    }
}