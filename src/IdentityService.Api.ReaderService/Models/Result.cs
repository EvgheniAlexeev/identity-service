namespace IdentityService.Api.ReaderService.Models;

/// <summary>
/// Generic result wrapper for handler responses.
/// Transparently wraps success/failure/not-found outcomes.
/// </summary>
public class Result<T> where T : class
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }
    public bool IsNotFound { get; private init; }

    public static Result<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static Result<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public static Result<T> NotFound(string error) => new()
    {
        IsSuccess = false,
        Error = error,
        IsNotFound = true
    };
}
