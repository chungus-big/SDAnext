using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ProtoBuf;
using SteamLib.ProtoCore;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using Xunit;

namespace SteamLib.Tests;

public class ProtobufOptimizationTests
{
    [Fact]
    public void ProtoHelpers_SerializeToBytesAndString_ProducesValidProtobuf()
    {
        var req = new GetPasswordRSAPublicKey_Request
        {
            AccountName = "test_steam_account"
        };

        var bytes = ProtoHelpers.ProtoToBytes(req);
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        var base64 = ProtoHelpers.ProtoToString(req);
        Assert.NotNull(base64);
        Assert.Equal(Convert.ToBase64String(bytes), base64);

        using var ms = new MemoryStream(bytes);
        var deserialized = Serializer.Deserialize<GetPasswordRSAPublicKey_Request>(ms);
        Assert.NotNull(deserialized);
        Assert.Equal("test_steam_account", deserialized.AccountName);
    }

    [Fact]
    public void ComputeSignature_ProducesExactHmacSha256Hash()
    {
        var req = new UpdateAuthSessionWithMobileConfirmation_Request
        {
            Version = 1,
            ClientId = 12345678901234567890UL,
            Steamid = 76561197960287930UL
        };

        var secretBytes = Encoding.UTF8.GetBytes("SuperSecretPrivateKey32BytesLong");
        var privateKeyBase64 = Convert.ToBase64String(secretBytes);

        req.ComputeSignature(privateKeyBase64);

        Assert.NotNull(req.Signature);
        Assert.Equal(32, req.Signature.Length); // HMAC-SHA256 produces 256 bits (32 bytes)

        // Verify with manual calculation
        var expectedData = new byte[18];
        BinaryPrimitives.WriteInt16LittleEndian(expectedData.AsSpan(0, 2), 1);
        BinaryPrimitives.WriteUInt64LittleEndian(expectedData.AsSpan(2, 8), 12345678901234567890UL);
        BinaryPrimitives.WriteUInt64LittleEndian(expectedData.AsSpan(10, 8), 76561197960287930UL);

        var expectedSignature = HMACSHA256.HashData(secretBytes, expectedData);
        Assert.Equal(expectedSignature, req.Signature);
    }

    [Fact]
    public void ETokenRenewalType_PreservesBinaryCompatibility()
    {
        var req = new GenerateAccessTokenForApp_Request
        {
            RefreshToken = "sample_refresh_token",
            SteamId = 76561197960287930L,
            TokenRenewalType = ETokenRenewalType.Allow
        };

        var bytes = ProtoHelpers.ProtoToBytes(req);
        Assert.NotNull(bytes);

        using var ms = new MemoryStream(bytes);
        var deserialized = Serializer.Deserialize<GenerateAccessTokenForApp_Request>(ms);

        Assert.NotNull(deserialized);
        Assert.Equal("sample_refresh_token", deserialized.RefreshToken);
        Assert.Equal(76561197960287930L, deserialized.SteamId);
        Assert.Equal(ETokenRenewalType.Allow, deserialized.TokenRenewalType);
        Assert.Equal(1, (int)deserialized.TokenRenewalType);
    }

    [Fact]
    public void AuthHelper_CreateMobileConfirmationRequest_UsesOptimizedSignature()
    {
        var secretBytes = Encoding.UTF8.GetBytes("SuperSecretPrivateKey32BytesLong");
        var privateKeyBase64 = Convert.ToBase64String(secretBytes);

        var req = Authentication.AuthRequestHelper.CreateMobileConfirmationRequest(
            1, 12345678901234567890UL, SteamId.FromSteam64(76561197960287930L), privateKeyBase64);

        Assert.NotNull(req.Signature);
        Assert.Equal(32, req.Signature.Length);

        var directSig = Authentication.AuthRequestHelper.ComputeConfirmationSignature(
            1, 12345678901234567890UL, 76561197960287930UL, privateKeyBase64);

        Assert.Equal(directSig, req.Signature);
    }

    [Fact]
    public void PooledBufferStream_Lifecycle_ResetsCanWriteAndAllowsMultipleDisposes()
    {
        var stream = new PooledBufferStream(16);
        Assert.True(stream.CanWrite);

        stream.Write([1, 2, 3, 4], 0, 4);
        Assert.Equal(4, stream.Length);
        Assert.Equal(4, stream.WrittenSpan.Length);

        stream.Dispose();
        Assert.False(stream.CanWrite);
        Assert.True(stream.WrittenSpan.IsEmpty);

        // Multiple dispose calls must be safe
        stream.Dispose();
    }
}
