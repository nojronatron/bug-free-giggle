using System;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Machine-readable status for OperationResult outcomes.
/// </summary>
public enum ResponseStatus
{
    Success,
    NotFound,
    BadFormat,
    OutOfRange,
    Cancelled,
    Error,
    Other
}

/// <summary>
/// Unit type used for void-like OperationResult payloads.
/// </summary>
public readonly struct Unit
{
    /// <summary>Single shared instance representing no value.</summary>
    public static readonly Unit Value = new();
}

/// <summary>
/// Immutable result object representing success or failure of an operation.
/// Use OperationResult{T}.Success(...) and OperationResult{T}.Failure(...)
/// or the non-generic <see cref="OperationResult"/> helper to construct instances.
/// </summary>
/// <typeparam name="T">Payload type returned on success.</typeparam>
public sealed record OperationResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public ResponseStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Diagnostic { get; init; }

    private OperationResult() { }

    private OperationResult(bool isSuccess, T? value, ResponseStatus status, string? errorMessage, Exception? diagnostic)
        : this()
    {
        IsSuccess = isSuccess;
        Value = value;
        Status = status;
        ErrorMessage = errorMessage;
        Diagnostic = diagnostic;
    }

    public static OperationResult<T> Success(T value, ResponseStatus status = ResponseStatus.Success)
        => new OperationResult<T>(true, value, status, null, null);

    public static OperationResult<T> Failure(string errorMessage, ResponseStatus status = ResponseStatus.Error, Exception? diagnostic = null)
        => new OperationResult<T>(false, default, status, errorMessage, diagnostic);
}

/// <summary>
/// Non-generic helper that provides concise factory methods for OperationResult{T} construction.
/// This type improves call-site ergonomics and supports the common Unit (void) case.
/// </summary>
public static class OperationResult
{
    public static OperationResult<T> Success<T>(T value, ResponseStatus status = ResponseStatus.Success)
        => OperationResult<T>.Success(value, status);

    public static OperationResult<T> Failure<T>(string errorMessage, ResponseStatus status = ResponseStatus.Error, Exception? diagnostic = null)
        => OperationResult<T>.Failure(errorMessage, status, diagnostic);

    public static OperationResult<Unit> Success()
        => OperationResult<Unit>.Success(Unit.Value);

    public static OperationResult<Unit> Failure(string errorMessage, ResponseStatus status = ResponseStatus.Error, Exception? diagnostic = null)
        => OperationResult<Unit>.Failure(errorMessage, status, diagnostic);
}
