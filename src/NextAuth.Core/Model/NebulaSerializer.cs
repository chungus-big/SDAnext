using System.Text.Json;
using System.Text.Json.Nodes;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Exceptions;
using SteamLib;
using SteamLib.Utility.MafileSerialization;

namespace NextAuth.Core.Model;

public static class NextSerializer
{
    public static MafileSerializer Serializer { get; }

    static NextSerializer()
    {
        Serializer = new MafileSerializer(new MafileSerializerSettings
        {
            DeserializationOptions =
            {
                AllowDeviceIdGeneration = true,
                AllowSessionIdGeneration = true,
                ThrowIfInvalidSteamId = false
            },
            SerializationOptions =
            {
                Formatting = Formatting.Indented
            }
        });
    }


    public static Mafile Deserialize(string cont, string path)
    {
        var data = Serializer.Deserialize(cont);
        var mobileData = data.Data;
        var info = data.Info;
        if (!info.IsExtended)
            throw new FormatException("Mafile is not extended data");


        var props = info.UnusedProperties ?? [];
        var group = GetPropertyValue<string>("Group", props);
        var password = GetPropertyValue<string>("Password", props);
        var mafile = Mafile.FromMobileDataExtended((MobileDataExtended)mobileData, group, password);

        mafile.Filename = Path.GetFileName(path);

        if (!info.SteamIdValid)
            throw new MafileNeedReloginException(mafile);

        return mafile;
    }

    private static T? GetPropertyValue<T>(string name, Dictionary<string, JsonNode?> dictionary)
    {
        if (!dictionary.TryGetValue(name, out var node) || node is null) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(node);
        }
        catch
        {
            return default;
        }
    }

    public static string SerializeMafile(Mafile data)
    {
        var props = new Dictionary<string, object?>
        {
            {nameof(Mafile.Group), data.Group},
            {nameof(Mafile.Password), data.Password}
        };
        return SerializeMafile(data, props);
    }


    public static string SerializeMafile(MobileDataExtended data, Dictionary<string, object?>? properties)
    {
        return MafileSerializer.SerializeLegacy(data, Serializer.Settings.SerializationOptions, properties);
    }
}