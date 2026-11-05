using System.Text.Json;
using SteamLib.Models;
using SteamLib.Models.Account;
using SteamLib.Web.Serialization;
using Xunit;

namespace SteamLib.Tests;

public class JsonOptimizationTests
{
    [Fact]
    public void TimeQueryResponse_DeserializesCorrectly()
    {
        var json = """
        {
          "response": {
            "server_time": 1712345678
          }
        }
        """;

        var result = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.TimeQueryResponse);

        Assert.NotNull(result);
        Assert.NotNull(result.Response);
        Assert.Equal(1712345678L, result.Response.ServerTime);
    }

    [Fact]
    public void FinalizeLoginJson_DeserializesCorrectly()
    {
        var json = """
        {
          "steamID": "76561197960287930",
          "transfer_info": [
            {
              "url": "https://store.steampowered.com/login/transfer",
              "params": {
                "nonce": "test_nonce_123",
                "auth": "test_auth_token_456"
              }
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.FinalizeLoginJson);

        Assert.NotNull(result);
        Assert.Equal(76561197960287930UL, result.SteamId);
        Assert.Single(result.TransferInfo);
        Assert.Equal("https://store.steampowered.com/login/transfer", result.TransferInfo[0].Url);
        Assert.Equal("test_nonce_123", result.TransferInfo[0].TransferInfoParams.Nonce);
        Assert.Equal("test_auth_token_456", result.TransferInfo[0].TransferInfoParams.Auth);
    }

    [Fact]
    public void TransferResult_DeserializesCorrectly()
    {
        var json = """{"result": 1}""";
        var result = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.TransferResult);

        Assert.NotNull(result);
        Assert.Equal(1, result.Result);
    }

    [Fact]
    public void PhoneValidationResponse_DeserializesCorrectly()
    {
        var json = """{"success": true, "is_voip": false, "is_valid": true}""";
        var result = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.PhoneValidationResponse);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(result.IsVoip);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void HasPhoneResponse_DeserializesCorrectly()
    {
        var json = """{"has_phone": true}""";
        var result = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.HasPhoneResponse);

        Assert.NotNull(result);
        Assert.True(result.HasPhone);
    }

    [Fact]
    public void WalletInfoSchema_DeserializesCorrectly()
    {
        var json = """
        {
          "success": 1,
          "currency": "USD",
          "country_code": "US",
          "alternate_min_amount": false,
          "amounts": [500, 1000, 2500],
          "related_trans_type": false,
          "related_trainsid": false,
          "user_wallet": {
            "amount": "1500",
            "currency": "USD"
          }
        }
        """;

        var result = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.WalletInfoSchema);

        Assert.NotNull(result);
        Assert.Equal(1, result.Success);
        Assert.Equal("USD", result.Currency);
        Assert.NotNull(result.Amounts);
        Assert.Equal(3, result.Amounts.Count);
        Assert.NotNull(result.UserWalletSchema);
        Assert.Equal(1500, result.UserWalletSchema.Amount);
        Assert.Equal("USD", result.UserWalletSchema.Currency);
    }

    [Fact]
    public void WebTradeEligibility_SerializesAndDeserializesRoundtrip()
    {
        var original = new WebTradeEligibility
        {
            Allowed = 1,
            Reason = 16416,
            AllowedAtTime = 1710000000,
            SteamGuardRequiredDays = 15,
            NewDeviceCooldownDays = 0,
            Expiration = new UnixTimeStamp(1715000000, UnixFormat.Seconds),
            TimeChecked = new UnixTimeStamp(1710000000, UnixFormat.Seconds)
        };

        var json = JsonSerializer.Serialize(original, SteamJsonSerializerContext.Default.WebTradeEligibility);
        var deserialized = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.WebTradeEligibility);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Allowed, deserialized.Allowed);
        Assert.Equal(original.Reason, deserialized.Reason);
        Assert.Equal(original.SteamGuardRequiredDays, deserialized.SteamGuardRequiredDays);
        Assert.Equal(original.Expiration.ToLong(), deserialized.Expiration.ToLong());
        Assert.Equal(original.TimeChecked.ToLong(), deserialized.TimeChecked.ToLong());
    }

    [Fact]
    public void ScriptHeaderModel_SerializesAndDeserializesCorrectly()
    {
        var model = new ScriptHeaderModel
        {
            SessionId = "session_xyz_789",
            SteamId = SteamId.FromSteam64(76561197960287930L)
        };

        var json = JsonSerializer.Serialize(model, SteamJsonSerializerContext.Default.ScriptHeaderModel);
        var deserialized = JsonSerializer.Deserialize(json, SteamJsonSerializerContext.Default.ScriptHeaderModel);

        Assert.NotNull(deserialized);
        Assert.Equal("session_xyz_789", deserialized.SessionId);
        Assert.NotNull(deserialized.SteamId);
        Assert.Equal(76561197960287930L, deserialized.SteamId.Value.Steam64.Id);
    }
}
