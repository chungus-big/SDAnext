using SteamLib.Authentication;
using SteamLib.Core.StatusCodes;
using SteamLib.Models;
using SteamLib.Models.Session;
using SteamLib.Results;
using Xunit;

namespace SteamLib.Tests;

public class TypedResultsTests
{
    [Fact]
    public void SteamResult_Success_PropertiesAreSetCorrectly()
    {
        var result = SteamResult<string>.Success("test-token");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("test-token", result.Value);
        Assert.Null(result.Error);
        Assert.Equal(SteamStatusCode.Ok, result.StatusCode);
    }

    [Fact]
    public void SteamResult_Failure_PropertiesAreSetCorrectly()
    {
        var result = SteamResult<string>.Failure("Network error occurred", SteamStatusCode.Fail);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal("Network error occurred", result.Error);
        Assert.Equal(SteamStatusCode.Fail, result.StatusCode);
    }

    [Fact]
    public void SteamResult_ImplicitConversionFromValue()
    {
        SteamResult<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void SteamResult_ValueOr_ReturnsFallbackOnFailure()
    {
        var success = SteamResult<string>.Success("real-value");
        var failure = SteamResult<string>.Failure("error");

        Assert.Equal("real-value", success.ValueOr("fallback"));
        Assert.Equal("fallback", failure.ValueOr("fallback"));
    }

    [Fact]
    public void SteamResult_Match_ExecutesCorrectBranch()
    {
        var success = SteamResult<int>.Success(100);
        var failure = SteamResult<int>.Failure("Fail");

        var successMessage = success.Match(v => $"Success: {v}", err => $"Error: {err}");
        var failureMessage = failure.Match(v => $"Success: {v}", err => $"Error: {err}");

        Assert.Equal("Success: 100", successMessage);
        Assert.Equal("Error: Fail", failureMessage);
    }

    [Fact]
    public void NonGenericSteamResult_WorksAsExpected()
    {
        var ok = SteamResult.Success();
        var fail = SteamResult.Failure("Something went wrong", SteamStatusCode.AccessDenied);

        Assert.True(ok.IsSuccess);
        Assert.False(fail.IsSuccess);
        Assert.Equal("Something went wrong", fail.Error);
        Assert.Equal(SteamStatusCode.AccessDenied, fail.StatusCode);
    }

    [Fact]
    public void AuthResult_Success_PatternMatching()
    {
        var steamId = SteamId.FromSteam64(76561198000000010);
        var refToken = new SteamAuthToken("token", steamId, UnixTimeStamp.FromDateTime(DateTime.UtcNow.AddHours(1)),
            SteamDomain.Community, SteamAccessTokenType.Refresh);
        var session = new SessionData("sess_123", steamId, refToken, []);

        AuthResult result = new AuthResult.Success(session);

        Assert.True(result.IsSuccess);

        var matched = result switch
        {
            AuthResult.Success s => s.Session.SessionId,
            _ => null
        };

        Assert.Equal("sess_123", matched);
    }

    [Fact]
    public void AuthResult_NeedDeviceCode_PatternMatching()
    {
        var steamId = SteamId.FromSteam64(76561198000000020);
        AuthResult result = new AuthResult.NeedDeviceCode(123456789UL, steamId);

        Assert.False(result.IsSuccess);

        var isNeedDeviceCode = result is AuthResult.NeedDeviceCode { ClientId: 123456789UL, SteamId: var sid } && sid == steamId;
        Assert.True(isNeedDeviceCode);
    }

    [Fact]
    public void AuthResult_NeedEmailCode_CarriesDomainHint()
    {
        var steamId = SteamId.FromSteam64(76561198000000030);
        AuthResult result = new AuthResult.NeedEmailCode(987654321UL, steamId, "g****@gmail.com");

        Assert.False(result.IsSuccess);

        if (result is AuthResult.NeedEmailCode email)
        {
            Assert.Equal("g****@gmail.com", email.EmailDomain);
            Assert.Equal(987654321UL, email.ClientId);
        }
        else
        {
            Assert.Fail("Result was not NeedEmailCode");
        }
    }

    [Fact]
    public void AuthResult_InvalidCredentials_ReportsMessage()
    {
        AuthResult result = new AuthResult.InvalidCredentials("Incorrect password entered");

        Assert.False(result.IsSuccess);
        var invalid = Assert.IsType<AuthResult.InvalidCredentials>(result);
        Assert.Equal("Incorrect password entered", invalid.Message);
    }

    [Fact]
    public void AuthResult_RateLimited_ReportsRetryAfter()
    {
        var retryAfter = TimeSpan.FromSeconds(60);
        AuthResult result = new AuthResult.RateLimited(retryAfter);

        Assert.False(result.IsSuccess);
        var limited = Assert.IsType<AuthResult.RateLimited>(result);
        Assert.Equal(retryAfter, limited.RetryAfter);
    }

    [Fact]
    public void AuthResult_Failed_CarriesErrorAndException()
    {
        var ex = new InvalidOperationException("Inner connection failed");
        AuthResult result = new AuthResult.Failed("Connection dropped", ex);

        Assert.False(result.IsSuccess);
        var failed = Assert.IsType<AuthResult.Failed>(result);
        Assert.Equal("Connection dropped", failed.Error);
        Assert.Same(ex, failed.Exception);
    }

    [Fact]
    public void AuthResult_Match_VisitsAllBranches()
    {
        var steamId = SteamId.FromSteam64(76561198000000040);
        AuthResult devCodeResult = new AuthResult.NeedDeviceCode(42UL, steamId);

        var branchName = devCodeResult.Match(
            onSuccess: _ => "Success",
            onNeedDeviceCode: _ => "DeviceCode",
            onNeedEmailCode: _ => "EmailCode",
            onInvalidCredentials: _ => "InvalidCreds",
            onRateLimited: _ => "RateLimited",
            onFailed: _ => "Failed"
        );

        Assert.Equal("DeviceCode", branchName);
    }
}
