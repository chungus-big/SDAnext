using System.Text.Json;
using System.Text.Json.Serialization;
using SteamLib.Models;

namespace SteamLib.Web.Serialization.Converters;

/// <summary>
/// High-performance System.Text.Json converter for <see cref="UnixTimeStamp"/>.
/// </summary>
public class UnixTimeStampJsonConverter : JsonConverter<UnixTimeStamp>
{
    public override UnixTimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new UnixTimeStamp(reader.GetInt64(), UnixFormat.Seconds);
        }

        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var seconds))
        {
            return new UnixTimeStamp(seconds, UnixFormat.Seconds);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, UnixTimeStamp value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToLong());
    }
}
