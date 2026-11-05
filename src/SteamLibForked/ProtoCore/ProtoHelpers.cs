using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Interfaces;

namespace SteamLib.ProtoCore;

public static class ProtoHelpers
{
    public static byte[] ProtoToBytes(IProtoMsg obj)
    {
        using var stream = new PooledBufferStream();
        Serializer.Serialize(stream, obj);
        return stream.WrittenSpan.ToArray();
    }

    [return: NotNullIfNotNull(nameof(obj))]
    public static string? ProtoToString(IProtoMsg? obj)
    {
        if (obj == null) return null;
        using var stream = new PooledBufferStream();
        Serializer.Serialize(stream, obj);
        return Convert.ToBase64String(stream.WrittenSpan);
    }

    public static EResult GetEResult(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-eresult", out var val))
        {
            var eResultInt = Convert.ToInt32(val.Single());
            return (EResult)eResultInt;
        }

        return EResult.Invalid;
    }
}