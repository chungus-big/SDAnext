using Microsoft.Extensions.DependencyInjection;
using SteamLib.Abstractions;
using SteamLib.Exceptions.Authorization;
using SteamLib.Services;
using SteamLib.SteamMobile;
using SteamLib.Web;
using Xunit;

namespace SteamLib.Tests;

public class SteamServicesTests
{
    [Fact]
    public void SteamTimeAligner_SetTimeDifference_UpdatesOffsetAndAlignedFlag()
    {
        var aligner = new SteamTimeAligner();
        Assert.False(aligner.IsAligned);

        aligner.SetTimeDifference(120);

        Assert.True(aligner.IsAligned);
        Assert.Equal(120, aligner.TimeDifference);
    }

    [Fact]
    public void TimeAligner_StaticFacade_DelegatesToDefault()
    {
        var customAligner = new SteamTimeAligner();
        customAligner.SetTimeDifference(45);

        var original = TimeAligner.Default;
        try
        {
            TimeAligner.Default = customAligner;
            Assert.True(TimeAligner.IsAligned);
            Assert.Equal(45, TimeAligner.TimeDifference);
        }
        finally
        {
            TimeAligner.Default = original;
        }
    }

    [Fact]
    public void SteamSessionContext_EnsureSessionId_ThrowsWhenAbsent()
    {
        var context = new SteamSessionContext();
        Assert.Throws<SessionInvalidException>(context.EnsureSessionId);
    }

    [Fact]
    public void SteamSessionContext_EnsureSessionId_ReturnsValueWhenPresent()
    {
        var context = new SteamSessionContext
        {
            SessionId = "session_test_123"
        };
        Assert.Equal("session_test_123", context.EnsureSessionId());
    }

    [Fact]
    public void SteamSessionContext_EnsureAccessToken_ThrowsWhenAbsent()
    {
        var context = new SteamSessionContext();
        Assert.Throws<SessionInvalidException>(context.EnsureAccessToken);
    }

    [Fact]
    public void AddSteamLib_RegistersAllRequiredServicesInContainer()
    {
        var services = new ServiceCollection();
        services.AddSteamLib();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ISteamHttpClientProvider>());
        Assert.NotNull(provider.GetService<ISteamTimeAligner>());
        Assert.NotNull(provider.GetService<ISteamAuthenticationService>());
        Assert.NotNull(provider.GetService<ISteamConfirmationsService>());
        Assert.NotNull(provider.GetService<ISteamLinkerService>());

        Assert.IsType<SteamTimeAligner>(provider.GetService<ISteamTimeAligner>());
        Assert.IsType<SteamAuthenticationService>(provider.GetService<ISteamAuthenticationService>());
        Assert.IsType<SteamConfirmationsService>(provider.GetService<ISteamConfirmationsService>());
        Assert.IsType<SteamLinkerService>(provider.GetService<ISteamLinkerService>());
    }

    [Fact]
    public async Task SteamConfirmationsService_UsesInjectedTimeAligner()
    {
        var fixedTime = 1712345678L;
        var fakeAligner = new FakeTimeAligner(fixedTime);
        var service = new SteamConfirmationsService(fakeAligner);

        var steamId = SteamId.FromSteam64(76561198000000001);
        var mobileData = new MobileData
        {
            DeviceId = "device_test",
            SharedSecret = "dGVzdF9zaGFyZWQ=",
            IdentitySecret = "dGVzdF9pZGVudGl0eQ=="
        };

        string? requestedUri = null;
        var handler = new TestHttpMessageHandler((req, ct) =>
        {
            requestedUri = req.RequestUri?.ToString();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"need_auth\":false}")
            };
            return Task.FromResult(response);
        });
        using var client = new HttpClient(handler);

        await service.GetConfirmationsResultAsync(client, mobileData, steamId);

        Assert.True(fakeAligner.WasCalled);
        Assert.NotNull(requestedUri);
        Assert.Contains($"t={fixedTime}", requestedUri);
    }

    private sealed class FakeTimeAligner(long fixedTime) : ISteamTimeAligner
    {
        public bool WasCalled { get; private set; }
        public bool IsAligned => true;
        public int TimeDifference => 0;

        public ValueTask<long> GetSteamTimeAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return ValueTask.FromResult(fixedTime);
        }

        public Task AlignTimeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public long GetSteamTime() => fixedTime;
        public void AlignTime() { }
        public void SetTimeDifference(int timeDifference) { }
    }

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
