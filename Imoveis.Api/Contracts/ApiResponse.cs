namespace Imoveis.Api.Contracts;

public sealed record ApiError(string Code, string Message, object? Detail = null);

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    IReadOnlyList<ApiError> Errors,
    string RequestId)
{
    public static ApiResponse<T> Ok(T data, string requestId) => new(true, data, Array.Empty<ApiError>(), requestId);

    public static ApiResponse<T> Fail(string requestId, params ApiError[] errors) => new(false, default, errors, requestId);
}
