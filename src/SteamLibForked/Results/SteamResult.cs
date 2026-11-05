using SteamLib.Core.StatusCodes;

namespace SteamLib.Results;

/// <summary>
/// Represents the result of a typed Steam operation with an optional payload.
/// </summary>
/// <typeparam name="T">The type of the result payload.</typeparam>
public readonly struct SteamResult<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public SteamStatusCode? StatusCode { get; }
    public Exception? Exception { get; }

    private SteamResult(bool isSuccess, T? value, string? error, SteamStatusCode? statusCode, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
        Exception = exception;
    }

    public static SteamResult<T> Success(T value) =>
        new(true, value, null, SteamStatusCode.Ok, null);

    public static SteamResult<T> Failure(string error, SteamStatusCode? statusCode = null, Exception? exception = null) =>
        new(false, default, error, statusCode ?? SteamStatusCode.Undefined, exception);

    public static SteamResult<T> Failure(SteamStatusCode statusCode, string? error = null, Exception? exception = null) =>
        new(false, default, error ?? statusCode.Name, statusCode, exception);

    public static implicit operator SteamResult<T>(T value) => Success(value);

    public T ValueOr(T fallback) => IsSuccess ? Value! : fallback;

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error ?? "Unknown error");

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error}, StatusCode={StatusCode?.Name ?? "None"})";
}

/// <summary>
/// Represents the result of an operation that does not return a payload.
/// </summary>
public readonly struct SteamResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public SteamStatusCode? StatusCode { get; }
    public Exception? Exception { get; }

    private SteamResult(bool isSuccess, string? error, SteamStatusCode? statusCode, Exception? exception)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
        Exception = exception;
    }

    public static SteamResult Success() =>
        new(true, null, SteamStatusCode.Ok, null);

    public static SteamResult Failure(string error, SteamStatusCode? statusCode = null, Exception? exception = null) =>
        new(false, error, statusCode ?? SteamStatusCode.Undefined, exception);

    public static SteamResult Failure(SteamStatusCode statusCode, string? error = null, Exception? exception = null) =>
        new(false, error ?? statusCode.Name, statusCode, exception);

    public static SteamResult<T> Ok<T>(T value) => SteamResult<T>.Success(value);

    public static SteamResult<T> Fail<T>(string error, SteamStatusCode? statusCode = null, Exception? exception = null) =>
        SteamResult<T>.Failure(error, statusCode, exception);

    public static SteamResult<T> Fail<T>(SteamStatusCode statusCode, string? error = null, Exception? exception = null) =>
        SteamResult<T>.Failure(statusCode, error, exception);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error ?? "Unknown error");

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error}, StatusCode={StatusCode?.Name ?? "None"})";
}
