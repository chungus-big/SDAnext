using ProtoBuf;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Interfaces;

namespace SteamLib.ProtoCore;

public class ProtoResponse
{
    public EResult Result { get; }
    public IProtoMsg? ProtoObject { get; protected set; }

    protected ProtoResponse(EResult result, IProtoMsg? protoObject)
    {
        Result = result;
        ProtoObject = protoObject;
    }
}

public class ProtoResponse<TProtoResponse>(EResult result, TProtoResponse? protoObject)
    : ProtoResponse(result, protoObject)
    where TProtoResponse : IProtoMsg
{
    public TProtoResponse? ResponseMsg
    {
        get => ProtoObject == null ? default : (TProtoResponse)ProtoObject;
        set => ProtoObject = value;
    }

    public static ProtoResponse<TProtoResponse> FromHttpResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var eResult = EResult.Invalid;
        if (response.Headers.TryGetValues("x-eresult", out var val))
        {
            var eResultInt = Convert.ToInt32(val.Single());
            eResult = (EResult)eResultInt;
        }

        var msg = default(TProtoResponse);
        Stream? stream = null;
        try
        {
            stream = response.Content.ReadAsStream();
            msg = Serializer.Deserialize<TProtoResponse>(stream);
        }
        catch
        {
            //Ignored
        }
        finally
        {
            stream?.Dispose();
        }

        return new ProtoResponse<TProtoResponse>(eResult, msg);
    }

    public static async Task<ProtoResponse<TProtoResponse>> FromHttpResponseAsync(HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        response.EnsureSuccessStatusCode();
        var eResult = EResult.Invalid;
        if (response.Headers.TryGetValues("x-eresult", out var val))
        {
            var eResultInt = Convert.ToInt32(val.Single());
            eResult = (EResult)eResultInt;
        }

        var msg = default(TProtoResponse);
        Stream? stream = null;
        try
        {
            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            msg = Serializer.Deserialize<TProtoResponse>(stream);
        }
        catch
        {
            //Ignored
        }
        finally
        {
            if (stream != null) await stream.DisposeAsync();
        }

        return new ProtoResponse<TProtoResponse>(eResult, msg);
    }

    /// <summary>
    ///     Ensures that <see cref="ResponseMsg" /> is not <see langword="null" /> and returns it; otherwise throws
    ///     <see cref="InvalidOperationException" />.
    /// </summary>
    /// <returns>The deserialized response message.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ResponseMsg" /> is null.</exception>
    public TProtoResponse GetResponse()
    {
        return ResponseMsg ?? throw new InvalidOperationException("ResponseMsg in response was null");
    }
}