using System.Net;
using System.Net.Http.Headers;

namespace SteamLib.Web;

/// <summary>
/// Delegating HTTP handler that implements exponential backoff retry policy with jitter,
/// HTTP 429 Retry-After handling, and transient failure recovery (502, 503, 504) for Steam API calls.
/// </summary>
public class SteamRetryHandler : DelegatingHandler
{
    public SteamRetryOptions Options { get; }

    public SteamRetryHandler(HttpMessageHandler innerHandler, SteamRetryOptions? options = null)
        : base(innerHandler)
    {
        Options = options ?? SteamRetryOptions.Default;
    }

    public SteamRetryHandler(SteamRetryOptions? options = null)
    {
        Options = options ?? SteamRetryOptions.Default;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);
        }

        var currentRequest = request;
        var attempt = 0;
        var uri = request.RequestUri?.GetLeftPart(UriPartial.Path) ?? "unknown";

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage? response;
            try
            {
                response = await base.SendAsync(currentRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (
                (ex is HttpRequestException or TimeoutException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                && ShouldRetryNetworkError(request, attempt, cancellationToken))
            {
                attempt++;
                var delay = CalculateBackoff(attempt, null);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                if (!ReferenceEquals(currentRequest, request))
                {
                    currentRequest.Dispose();
                }
                currentRequest = await CloneRequestAsync(request).ConfigureAwait(false);
                continue;
            }

            if (response != null && IsTransientStatusCode(response.StatusCode) && attempt < Options.MaxRetries && !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                var delay = CalculateBackoff(attempt, response.Headers.RetryAfter);

                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                if (!ReferenceEquals(currentRequest, request))
                {
                    currentRequest.Dispose();
                }
                currentRequest = await CloneRequestAsync(request).ConfigureAwait(false);
                continue;
            }

            return response!;
        }
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Content?.LoadIntoBufferAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        var currentRequest = request;
        var attempt = 0;
        var uri = request.RequestUri?.GetLeftPart(UriPartial.Path) ?? "unknown";

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage? response;
            try
            {
                response = base.Send(currentRequest, cancellationToken);
            }
            catch (Exception ex) when (
                (ex is HttpRequestException or TimeoutException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                && ShouldRetryNetworkError(request, attempt, cancellationToken))
            {
                attempt++;
                var delay = CalculateBackoff(attempt, null);

                cancellationToken.WaitHandle.WaitOne(delay);
                cancellationToken.ThrowIfCancellationRequested();
                if (!ReferenceEquals(currentRequest, request))
                {
                    currentRequest.Dispose();
                }
                currentRequest = CloneRequestSync(request);
                continue;
            }

            if (response != null && IsTransientStatusCode(response.StatusCode) && attempt < Options.MaxRetries && !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                var delay = CalculateBackoff(attempt, response.Headers.RetryAfter);

                response.Dispose();
                cancellationToken.WaitHandle.WaitOne(delay);
                cancellationToken.ThrowIfCancellationRequested();
                if (!ReferenceEquals(currentRequest, request))
                {
                    currentRequest.Dispose();
                }
                currentRequest = CloneRequestSync(request);
                continue;
            }

            return response!;
        }
    }

    private bool ShouldRetryNetworkError(HttpRequestMessage request, int attempt, CancellationToken cancellationToken)
    {
        if (attempt >= Options.MaxRetries || cancellationToken.IsCancellationRequested)
            return false;

        if (!Options.RetryNetworkErrorsOnNonIdempotentMethods && IsNonIdempotentMethod(request.Method))
            return false;

        return true;
    }

    private static bool IsNonIdempotentMethod(HttpMethod method)
    {
        return method == HttpMethod.Post || method.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.TooManyRequests => true, // 429
            HttpStatusCode.BadGateway => true,      // 502
            HttpStatusCode.ServiceUnavailable => true, // 503
            HttpStatusCode.GatewayTimeout => true,  // 504
            (HttpStatusCode)520 => true,            // Cloudflare unknown
            (HttpStatusCode)521 => true,            // Cloudflare server down
            (HttpStatusCode)522 => true,            // Cloudflare connection timed out
            (HttpStatusCode)524 => true,            // Cloudflare timeout
            _ => false
        };
    }

    private TimeSpan CalculateBackoff(int attempt, RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter != null)
        {
            if (retryAfter.Delta.HasValue)
            {
                var delta = retryAfter.Delta.Value;
                if (delta > TimeSpan.Zero && delta <= Options.MaxBackoff)
                    return delta;
                if (delta > Options.MaxBackoff)
                    return Options.MaxBackoff;
            }
            else if (retryAfter.Date.HasValue)
            {
                var delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero && delay <= Options.MaxBackoff)
                    return delay;
                if (delay > Options.MaxBackoff)
                    return Options.MaxBackoff;
            }
        }

        var factor = Math.Pow(2, attempt - 1);
        var baseDelayMs = Options.InitialBackoff.TotalMilliseconds * factor;
        if (baseDelayMs > Options.MaxBackoff.TotalMilliseconds)
        {
            baseDelayMs = Options.MaxBackoff.TotalMilliseconds;
        }

        var jitterMs = Random.Shared.Next(0, (int)Math.Max(50, baseDelayMs * 0.2));
        return TimeSpan.FromMilliseconds(baseDelayMs + jitterMs);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;
            var streamContent = new StreamContent(ms);
            foreach (var header in request.Content.Headers)
            {
                streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = streamContent;
        }

        return clone;
    }

    private static HttpRequestMessage CloneRequestSync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content != null)
        {
            var ms = new MemoryStream();
            request.Content.CopyTo(ms, null, CancellationToken.None);
            ms.Position = 0;
            var streamContent = new StreamContent(ms);
            foreach (var header in request.Content.Headers)
            {
                streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = streamContent;
        }

        return clone;
    }
}
