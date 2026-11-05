using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SteamLib.Models.Session;
using SteamLib.Models.SteamIds;
using SteamLib.Web.Converters;

namespace SteamLib.Utility.MafileSerialization;

public partial class MafileSerializer(MafileSerializerSettings? settings = null)
{
    public const int MAFILE_VERSION = 3;
    public const string SIGNATURE_PROPERTY_NAME = "@SLSV";
    private const string CREDITS_PROPERTY_NAME = "Credits";

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters =
        {
            new SteamIdToSteam64Converter(),
            new UnixTimeStampConverter()
        }
    };

    private static readonly HashSet<string> ActualProperties =
        new(typeof(MobileDataExtended).GetProperties().Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

    public MafileSerializerSettings Settings { get; } = settings ?? new MafileSerializerSettings();


    public DeserializedMafileData Deserialize(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject j)
        {
            throw new JsonException("Invalid JSON root: expected object");
        }

        var unusedProperties = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in j)
        {
            unusedProperties[kv.Key] = kv.Value?.DeepClone();
        }

        var versionToken = GetToken(j, SIGNATURE_PROPERTY_NAME);
        unusedProperties.Remove(CREDITS_PROPERTY_NAME);
        int? version = null;
        if (versionToken is JsonValue vVal)
        {
            if (vVal.TryGetValue<int>(out var v))
                version = v;
            else if (int.TryParse(vVal.ToString(), out var vParsed))
                version = vParsed;
        }

        unusedProperties.Remove(SIGNATURE_PROPERTY_NAME);
        if (version == MAFILE_VERSION)
        {
            try
            {
                var unused = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (key, prop) in unusedProperties)
                {
                    if (ActualProperties.Contains(key))
                        continue;
                    unused.Add(key, prop);
                }

                var data = JsonSerializer.Deserialize<MobileDataExtended>(json, SerializerOptions)!;
                data.SessionData = Validate.ValidateMobileData(data, out var sessionResult);

                return DeserializedMafileData.CreateActual(data, unused, sessionResult);
            }
            catch (Exception ex)
            {
                throw new JsonException(
                    "MafileSerializer found actual 'Version' signature but some error occurred during deserialization",
                    ex);
            }
        }

        var sharedSecretToken = GetTokenOrThrow(j, unusedProperties, nameof(MobileData.SharedSecret), "sharedsecret",
            "shared_secret", "shared");
        var identitySecretToken = GetTokenOrThrow(j, unusedProperties, nameof(MobileData.IdentitySecret),
            "identitysecret", "identity_secret", "identity");
        var deviceId = GetDeviceId(j, Settings, unusedProperties, nameof(MobileData.DeviceId), "deviceid", "device_id",
            "device");

        var sharedSecret = GetBase64(nameof(MobileData.SharedSecret), sharedSecretToken);
        var identitySecret = GetBase64(nameof(MobileData.IdentitySecret), identitySecretToken);
        Validate.NotNullOrEmpty(nameof(MobileData.DeviceId), deviceId);

        var accountName = GetString(j, unusedProperties, nameof(MobileDataExtended.AccountName), "account_name",
            "accountname");
        var revocationCode = GetString(j, unusedProperties, nameof(MobileDataExtended.RevocationCode),
            "revocation_code", "rcode", "r_code", "revocationcode");
        var sessionDataToken = GetToken(j, unusedProperties, nameof(MobileDataExtended.SessionData), "session_data",
            "sessiondata", "session");

        // Determine whether this mafile is in minified form (contains only secrets, no account or session data)
        var minified
            = string.IsNullOrWhiteSpace(accountName)
              && string.IsNullOrWhiteSpace(revocationCode)
              && sessionDataToken == null;

        if (minified)
        {
            var data = new MobileData
            {
                DeviceId = deviceId,
                IdentitySecret = identitySecret,
                SharedSecret = sharedSecret
            };
            return DeserializedMafileData.Create(data, version, unusedProperties);
        }

        var missingProperties = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(accountName)) missingProperties.Add(nameof(MobileDataExtended.AccountName));
        if (string.IsNullOrWhiteSpace(revocationCode)) missingProperties.Add(nameof(MobileDataExtended.RevocationCode));

        var serverTime = GetLong(j, unusedProperties, nameof(MobileDataExtended.ServerTime), "server_time",
            "servertime");
        var serialNumber = GetSerialNumber(j, Settings, nameof(MobileDataExtended.SerialNumber), unusedProperties,
            "serial_number", "serialnumber");
        var uri = GetString(j, unusedProperties, nameof(MobileDataExtended.Uri), "url", "uri");
        var tokenGid = GetString(j, unusedProperties, nameof(MobileDataExtended.TokenGid), "token_gid", "tokengid");
        var secret1 = GetString(j, unusedProperties, nameof(MobileDataExtended.Secret1), "secret_1", "seecret1");
        var steamId = GetSteamId(j, unusedProperties, nameof(MobileDataExtended.SteamId), "steam_id", "id");

        if (serverTime == null) missingProperties.Add(nameof(MobileDataExtended.ServerTime));
        if (serialNumber == null) missingProperties.Add(nameof(MobileDataExtended.SerialNumber));
        if (string.IsNullOrWhiteSpace(uri)) missingProperties.Add(nameof(MobileDataExtended.Uri));
        if (string.IsNullOrWhiteSpace(tokenGid)) missingProperties.Add(nameof(MobileDataExtended.TokenGid));
        if (string.IsNullOrWhiteSpace(secret1)) missingProperties.Add(nameof(MobileDataExtended.Secret1));

        MobileSessionData? sessionData = null;
        var sResult = DeserializedMafileSessionResult.Missing;
        if (sessionDataToken is JsonObject sessionObj)
        {
            sessionData = DeserializeMobileSessionData(sessionObj, out sResult, out steamId);
        }

        if ((steamId == null || steamId.Value.Steam64.Id < SteamId64.SEED) &&
            Settings.DeserializationOptions.ThrowIfInvalidSteamId)
        {
            throw new ArgumentException("Can't retrieve SteamId from Mafile", nameof(MobileDataExtended.SteamId));
        }

        if (steamId == null) missingProperties.Add(nameof(MobileDataExtended.SteamId));

        var mobileData = new MobileDataExtended
        {
            DeviceId = deviceId,
            IdentitySecret = identitySecret,
            SharedSecret = sharedSecret,
            AccountName = accountName ?? string.Empty,
            RevocationCode = revocationCode,
            ServerTime = serverTime.GetValueOrDefault(),
            SerialNumber = serialNumber.GetValueOrDefault(),
            Uri = uri ?? string.Empty,
            TokenGid = tokenGid ?? string.Empty,
            Secret1 = secret1 ?? string.Empty,
            SessionData = sessionData,
            SteamId = steamId.GetValueOrDefault() // Assigned after SessionData to ensure it is not overwritten
        };
        return DeserializedMafileData.Create(mobileData, version, unusedProperties, missingProperties,
            sResult);
    }
}