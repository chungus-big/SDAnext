using System.Text;

namespace SteamLib.Web;

public static class HttpExtensions
{
    /// <summary>
    /// Converts a collection of key-value pairs to a URL query string starting with '?' (e.g., "?key1=val1&amp;key2=val2").
    /// Returns an empty string if the collection is null or empty.
    /// </summary>
    public static string ToQueryString(this IEnumerable<KeyValuePair<string, string>>? parameters)
    {
        if (parameters == null) return string.Empty;

        StringBuilder? sb = null;
        foreach (var kvp in parameters)
        {
            if (sb == null)
            {
                sb = new StringBuilder("?");
            }
            else
            {
                sb.Append('&');
            }

            sb.Append(Uri.EscapeDataString(kvp.Key ?? string.Empty));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(kvp.Value ?? string.Empty));
        }

        return sb?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Ensures the response has a success status code and reads the content as a string asynchronously.
    /// </summary>
    public static async Task<string> ReadAsStringEnsureSuccessAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
