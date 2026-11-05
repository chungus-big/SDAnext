using System.Text.Json;
using System.Text.Json.Nodes;

namespace SteamLib.Utility.MafileSerialization;

public partial class MafileSerializer //Write
{
    #region Instance

    public string Serialize(MobileData mobileData)
    {
        return Serialize(mobileData, Settings.SerializationOptions);
    }

    public Task<string> SerializeAsync(MobileData mobileData)
    {
        return SerializeAsync(mobileData, Settings.SerializationOptions);
    }

    public string SerializeLegacy(MobileData mobileData, IDictionary<string, object?>? additionalProperties = null)
    {
        return SerializeLegacy(mobileData, Settings.SerializationOptions, additionalProperties);
    }

    public Task<string> SerializeLegacyAsync(MobileData mobileData,
        IDictionary<string, object?>? additionalProperties = null)
    {
        return SerializeLegacyAsync(mobileData, Settings.SerializationOptions, additionalProperties);
    }

    #endregion

    #region Default

    public static string Serialize(MobileData mobileData, MafileSerializationOptions? options)
    {
        options ??= new MafileSerializationOptions();
        var j = CreateJsonObject(mobileData, options);
        var jsonOptions = new JsonSerializerOptions(SerializerOptions)
        {
            WriteIndented = options.WriteIndented
        };
        return j.ToJsonString(jsonOptions);
    }

    public static Task<string> SerializeAsync(MobileData mobileData, MafileSerializationOptions? options)
    {
        return Task.FromResult(Serialize(mobileData, options));
    }

    private static JsonObject CreateJsonObject(MobileData mobileData, MafileSerializationOptions options)
    {
        var node = JsonSerializer.SerializeToNode(mobileData, mobileData.GetType(), SerializerOptions) as JsonObject
            ?? [];
        node[SIGNATURE_PROPERTY_NAME] = MAFILE_VERSION;
        return node;
    }

    #endregion

    #region Legacy

    public static string SerializeLegacy(MobileData mobileData, MafileSerializationOptions? options,
        IDictionary<string, object?>? additionalProperties = null)
    {
        options ??= new MafileSerializationOptions();
        var j = CreateLegacyJsonObject(mobileData, options, additionalProperties);
        var jsonOptions = new JsonSerializerOptions(SerializerOptions)
        {
            WriteIndented = options.WriteIndented
        };
        return j.ToJsonString(jsonOptions);
    }

    public static Task<string> SerializeLegacyAsync(MobileData mobileData, MafileSerializationOptions? options,
        IDictionary<string, object?>? additionalProperties = null)
    {
        return Task.FromResult(SerializeLegacy(mobileData, options, additionalProperties));
    }

    private static JsonObject CreateLegacyJsonObject(MobileData mobileData, MafileSerializationOptions options,
        IDictionary<string, object?>? additionalProperties = null)
    {
        var result = new LegacyMafile
        {
            SharedSecret = mobileData.SharedSecret,
            IdentitySecret = mobileData.IdentitySecret,
            DeviceId = mobileData.DeviceId
        };

        if (mobileData is MobileDataExtended ext)
        {
            result.RevocationCode = ext.RevocationCode ?? string.Empty;
            result.AccountName = ext.AccountName;
            result.SessionData = ext.SessionData == null
                ? null
                : new LegacySessionData
                {
                    AccessToken = ext.SessionData?.MobileToken?.Token,
                    SteamLoginSecure = ext.SessionData?.MobileToken?.SignedToken,
                    RefreshToken = ext.SessionData?.RefreshToken.Token,
                    SteamId = ext.SessionData?.SteamId.Steam64.Id,
                    SessionId = ext.SessionData?.SessionId
                };
            result.ServerTime = ext.ServerTime;
            result.SerialNumber = ext.SerialNumber.ToString();
            result.Uri = ext.Uri;
            result.TokenGid = ext.TokenGid;
            result.Secret1 = ext.Secret1;
            result.SteamId = ext.SteamId.Steam64.Id;
        }

        var j = JsonSerializer.SerializeToNode(result, SerializerOptions)?.AsObject() ?? [];
        if (additionalProperties != null)
        {
            foreach (var (name, value) in additionalProperties)
            {
                j[name] = JsonSerializer.SerializeToNode(value, SerializerOptions);
            }
        }

        return j;
    }

    #endregion

    #region Obsolete

    [Obsolete("Use Serialize(MobileData, MafileSerializationOptions) instead.")]
    public static string Serialize(MobileData mobileData, Formatting formatting)
    {
        return Serialize(mobileData, new MafileSerializationOptions { Formatting = formatting });
    }

    [Obsolete("Use SerializeAsync(MobileData, MafileSerializationOptions) instead.")]
    public static Task<string> SerializeAsync(MobileData mobileData, Formatting formatting)
    {
        return SerializeAsync(mobileData, new MafileSerializationOptions { Formatting = formatting });
    }

    [Obsolete("Use SerializeLegacy(MobileData, MafileSerializationOptions, Dictionary<string, object?>) instead.")]
    public static string SerializeLegacy(MobileData mobileData, Formatting formatting,
        Dictionary<string, object?>? additionalProperties = null)
    {
        return SerializeLegacy(mobileData, new MafileSerializationOptions { Formatting = formatting }, additionalProperties);
    }

    #endregion
}