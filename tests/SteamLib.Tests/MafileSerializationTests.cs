using System.Text.Json.Nodes;
using SteamLib.Utility.MafileSerialization;
using Xunit;

namespace SteamLib.Tests;

public class MafileSerializationTests
{
    private readonly MafileSerializer _serializer = new();

    [Fact]
    public void Deserialize_V3Format_SuccessfullyParsesAllFields()
    {
        var json = """
        {
          "@SLSV": 3,
          "SharedSecret": "nQ/u8cK2k2YwHk+j/b/15oA9g4Q=",
          "IdentitySecret": "ABCDEFGHIJ1234567890KLMNOPQ=",
          "DeviceId": "android:00000000-0000-0000-0000-000000000000",
          "AccountName": "steam_user_test",
          "RevocationCode": "R12345",
          "SteamId": 76561197960287930
        }
        """;

        var result = _serializer.Deserialize(json);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        var ext = Assert.IsType<MobileDataExtended>(result.Data);
        Assert.Equal("steam_user_test", ext.AccountName);
        Assert.Equal("R12345", ext.RevocationCode);
        Assert.Equal(76561197960287930L, ext.SteamId.Steam64.Id);
        Assert.Equal("nQ/u8cK2k2YwHk+j/b/15oA9g4Q=", ext.SharedSecret);
        Assert.Equal("ABCDEFGHIJ1234567890KLMNOPQ=", ext.IdentitySecret);
        Assert.Equal("android:00000000-0000-0000-0000-000000000000", ext.DeviceId);
    }

    [Fact]
    public void Deserialize_V3Format_WithCamelCaseProperties_DoesNotTreatThemAsUnused()
    {
        var json = """
        {
          "@SLSV": 3,
          "sharedSecret": "nQ/u8cK2k2YwHk+j/b/15oA9g4Q=",
          "identitySecret": "ABCDEFGHIJ1234567890KLMNOPQ=",
          "deviceId": "android:00000000-0000-0000-0000-000000000000",
          "accountName": "steam_user_test",
          "revocationCode": "R12345",
          "steamId": 76561197960287930,
          "customExtraField": "extra_value"
        }
        """;

        var result = _serializer.Deserialize(json);

        Assert.NotNull(result);
        var ext = Assert.IsType<MobileDataExtended>(result.Data);
        Assert.Equal("steam_user_test", ext.AccountName);
        Assert.NotNull(result.Info.UnusedProperties);
        Assert.Single(result.Info.UnusedProperties);
        Assert.Contains("customExtraField", result.Info.UnusedProperties.Keys);
    }

    [Fact]
    public void Deserialize_LegacySdaFormat_SuccessfullyParsesSnakeCaseAndSession()
    {
        var json = """
        {
          "shared_secret": "nQ/u8cK2k2YwHk+j/b/15oA9g4Q=",
          "identity_secret": "ABCDEFGHIJ1234567890KLMNOPQ=",
          "device_id": "android:00000000-0000-0000-0000-000000000000",
          "account_name": "sda_legacy_user",
          "revocation_code": "R54321",
          "steamid": "76561197960287930",
          "fully_enrolled": true,
          "Session": {
            "SessionID": "test_session_id_123",
            "SteamLogin": "76561197960287930%7C%7Clogin_token",
            "SteamLoginSecure": "76561197960287930%7C%7Csecure_token",
            "WebCookie": "cookie_val",
            "OAuthToken": "oauth_token_val",
            "SteamID": 76561197960287930
          }
        }
        """;

        var result = _serializer.Deserialize(json);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        var ext = Assert.IsType<MobileDataExtended>(result.Data);
        Assert.Equal("sda_legacy_user", ext.AccountName);
        Assert.Equal("R54321", ext.RevocationCode);
        Assert.Equal(76561197960287930L, ext.SteamId.Steam64.Id);
        Assert.Equal("nQ/u8cK2k2YwHk+j/b/15oA9g4Q=", ext.SharedSecret);
        Assert.Equal("ABCDEFGHIJ1234567890KLMNOPQ=", ext.IdentitySecret);

        // Unknown extra fields like fully_enrolled should be captured in UnusedProperties
        Assert.NotNull(result.Info.UnusedProperties);
        Assert.Contains("fully_enrolled", result.Info.UnusedProperties.Keys);
    }

    [Fact]
    public void Deserialize_MinifiedFormat_ParsesEssentialSecretsOnly()
    {
        var json = """
        {
          "shared_secret": "nQ/u8cK2k2YwHk+j/b/15oA9g4Q=",
          "identity_secret": "ABCDEFGHIJ1234567890KLMNOPQ=",
          "device_id": "android:11111111-2222-3333-4444-555555555555"
        }
        """;

        var result = _serializer.Deserialize(json);

        Assert.NotNull(result);
        Assert.False(result.Info.IsExtended);
        Assert.Equal("nQ/u8cK2k2YwHk+j/b/15oA9g4Q=", result.Data.SharedSecret);
        Assert.Equal("ABCDEFGHIJ1234567890KLMNOPQ=", result.Data.IdentitySecret);
        Assert.Equal("android:11111111-2222-3333-4444-555555555555", result.Data.DeviceId);
    }

    [Fact]
    public void Serialize_Roundtrip_ProducesValidMafileJson()
    {
        var originalJson = """
        {
          "@SLSV": 3,
          "SharedSecret": "nQ/u8cK2k2YwHk+j/b/15oA9g4Q=",
          "IdentitySecret": "ABCDEFGHIJ1234567890KLMNOPQ=",
          "DeviceId": "android:00000000-0000-0000-0000-000000000000",
          "AccountName": "roundtrip_user",
          "RevocationCode": "R99999",
          "SteamId": 76561197960287930
        }
        """;

        var deserialized = _serializer.Deserialize(originalJson);
        var serializedJson = _serializer.Serialize(deserialized.Data);

        var jObj = JsonNode.Parse(serializedJson) as JsonObject;
        Assert.NotNull(jObj);
        Assert.Equal(3, (int)jObj["@SLSV"]!);
        Assert.Equal("roundtrip_user", (string)jObj["AccountName"]!);
        Assert.Equal("R99999", (string)jObj["RevocationCode"]!);
        Assert.Equal("nQ/u8cK2k2YwHk+j/b/15oA9g4Q=", (string)jObj["SharedSecret"]!);

        // Deserialize again and verify equivalence
        var roundtrip = _serializer.Deserialize(serializedJson);
        var origExt = (MobileDataExtended)deserialized.Data;
        var roundExt = (MobileDataExtended)roundtrip.Data;
        Assert.Equal(origExt.AccountName, roundExt.AccountName);
        Assert.Equal(origExt.SharedSecret, roundExt.SharedSecret);
        Assert.Equal(origExt.IdentitySecret, roundExt.IdentitySecret);
        Assert.Equal(origExt.DeviceId, roundExt.DeviceId);
        Assert.Equal(origExt.SteamId, roundExt.SteamId);
    }
}
