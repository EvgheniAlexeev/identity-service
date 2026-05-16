// FILE: Result.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WRITER
// PURPOSE: M-IDENTITY-WRITER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WRITER

namespace IdentityService.Api.WriterService.Models;

/// <summary>
/// Generic result wrapper for handler responses.
/// </summary>
public class Result<T> where T : class
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }

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
}
