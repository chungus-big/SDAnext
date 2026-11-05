using System.Diagnostics;
using System.Text.Json;
using SteamLib.Models;
using SteamLib.Abstractions;
using SteamLib.Core;
using SteamLib.Web;
using SteamLib.Web.Serialization;

namespace SteamLib.SteamMobile;

/// <summary>
/// Service implementation for synchronizing local system time with Steam server time.
/// </summary>
public class SteamTimeAligner(ISteamHttpClientProvider? httpClientProvider = null) : ISteamTimeAligner
{
    private const string TIME_ALIGN_ENDPOINT = "/ITwoFactorService/QueryTime/v0001";
    private readonly SemaphoreSlim _alignLock = new(1, 1);
    private readonly TimeSpan _retryCooldown = TimeSpan.FromSeconds(60);
    private readonly ISteamHttpClientProvider _httpClientProvider = httpClientProvider ?? SteamHttpClientFactory.Provider;

    private volatile bool _aligned;
    private volatile int _timeDifference;
    private DateTime _lastSyncAttempt = DateTime.MinValue;

    public bool IsAligned => _aligned;
    public int TimeDifference => _timeDifference;
    public static UnixTimeStamp UtcNow => UnixTimeStamp.FromDateTime(DateTime.UtcNow);

    public ValueTask<long> GetSteamTimeAsync(CancellationToken ct = default)
    {
        if (_aligned)
        {
            return ValueTask.FromResult(UtcNow.ToLong() + _timeDifference);
        }

        if (DateTime.UtcNow - _lastSyncAttempt < _retryCooldown)
        {
            return ValueTask.FromResult(UtcNow.ToLong() + _timeDifference);
        }

        return AlignAndGetTimeAsync(ct);
    }

    private async ValueTask<long> AlignAndGetTimeAsync(CancellationToken ct)
    {
        try
        {
            await AlignTimeAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }

        return UtcNow.ToLong() + _timeDifference;
    }

    [Obsolete("Use GetSteamTimeAsync instead to prevent synchronous blocking of threads.")]
    public long GetSteamTime()
    {
        if (!_aligned && DateTime.UtcNow - _lastSyncAttempt >= _retryCooldown)
        {
            try
            {
                AlignTime();
            }
            catch
            {
            }
        }

        return UtcNow.ToLong() + _timeDifference;
    }

    [Obsolete("Use AlignTimeAsync instead to prevent synchronous blocking of threads.")]
    public void AlignTime()
    {
        _alignLock.Wait();
        try
        {
            if (_aligned) return;

            if (DateTime.UtcNow - _lastSyncAttempt < _retryCooldown) return;
            _lastSyncAttempt = DateTime.UtcNow;

            var client = _httpClientProvider.SharedClient;
            var sw = Stopwatch.StartNew();
            var req = new HttpRequestMessage(HttpMethod.Post, SteamConstants.STEAM_API + TIME_ALIGN_ENDPOINT);
            using var response = client.Send(req);
            response.EnsureSuccessStatusCode();
            sw.Stop();
            using var stream = response.Content.ReadAsStream();
            var timeQueryResp = JsonSerializer.Deserialize(stream, SteamJsonSerializerContext.Default.TimeQueryResponse);
            var time = timeQueryResp?.Response?.ServerTime ?? 0;
            if (time <= 0)
                throw new InvalidOperationException("Failed to parse valid server_time from Steam API");
            var now = UtcNow - sw.Elapsed;
            _timeDifference = (int)(time - now.ToLong());
            _aligned = true;
        }
        catch
        {
        }
        finally
        {
            _alignLock.Release();
        }
    }

    public async Task AlignTimeAsync(CancellationToken ct = default)
    {
        await _alignLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_aligned) return;

            if (DateTime.UtcNow - _lastSyncAttempt < _retryCooldown) return;
            _lastSyncAttempt = DateTime.UtcNow;

            var client = _httpClientProvider.SharedClient;
            var sw = Stopwatch.StartNew();
            using var response = await client.PostAsync(SteamConstants.STEAM_API + TIME_ALIGN_ENDPOINT, null, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            sw.Stop();
            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var timeQueryResp = await JsonSerializer.DeserializeAsync(stream, SteamJsonSerializerContext.Default.TimeQueryResponse, ct).ConfigureAwait(false);
            var time = timeQueryResp?.Response?.ServerTime ?? 0;
            if (time <= 0)
                throw new InvalidOperationException("Failed to parse valid server_time from Steam API");
            var now = UtcNow - sw.Elapsed;
            _timeDifference = (int)(time - now.ToLong());
            _aligned = true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }
        finally
        {
            _alignLock.Release();
        }
    }

    public void SetTimeDifference(int timeDifference)
    {
        _timeDifference = timeDifference;
        _aligned = true;
    }
}
