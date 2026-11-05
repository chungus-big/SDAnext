using System.Text.Json;
using System.Text.Json.Serialization;
using SteamLib.Models.SteamIds;
using SteamLib.Utility;

namespace SteamLib.Web.Converters;

public class SteamIdToSteam64Converter : JsonConverter<SteamId>
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

public class SteamIdToSteam2Converter : JsonConverter<SteamId>
{
    public override SteamId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str))
                return new SteamId(SteamId2.Parse(str));
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SteamId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Steam2.ToString());
    }
}

public class SteamIdToSteam3Converter : JsonConverter<SteamId>
{
    public override SteamId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str))
                return new SteamId(SteamId3.Parse(str));
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SteamId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Steam3.ToString());
    }
}

public class Steam64ToLongConverter : JsonConverter<SteamId64>
{
    public override SteamId64 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new SteamId64(reader.GetInt64());
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str))
                return SteamId64.Parse(str);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SteamId64 value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToLong());
    }
}

public class SteamId2ToStringConverter : JsonConverter<SteamId2>
{
    public override SteamId2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str))
                return SteamId2.Parse(str);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SteamId2 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class SteamId3ToStringConverter : JsonConverter<SteamId3>
{
    public override SteamId3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str))
                return SteamId3.Parse(str);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SteamId3 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}