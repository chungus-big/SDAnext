using System.Text.Json;
using System.Text.Json.Serialization;
using SteamLib.Utility;

namespace SteamLib.Web.Serialization.Converters;

/// <summary>
/// High-performance System.Text.Json converter for <see cref="SteamId"/>.
/// </summary>
public class SteamIdJsonConverter : JsonConverter<SteamId>
{
    public override SteamId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return SteamId.FromSteam64(reader.GetInt64());
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str)) return default;
            return SteamIdParser.Parse(str);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SteamId value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Steam64.ToLong());
    }
}
