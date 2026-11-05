using System.Text.Json;
using System.Text.Json.Serialization;

namespace SteamLib.Web.Converters;

/// <summary>
/// System.Text.Json converter for serializing <see cref="int"/> to and from string or number representations.
/// </summary>
public class IntToStringConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var parsed))
        {
            return parsed;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
