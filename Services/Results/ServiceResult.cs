namespace laoyu_blog_backend.Services.Results;

public sealed record ServiceError(
    string Code,
    string Message);

public sealed record ServiceResult<T>(
    T? Value,
    ServiceError? Error)
{
    public bool IsSuccess => Error is null;

    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>(
            Value: value,
            Error: null);
    }

    public static ServiceResult<T> Failure(
        string code,
        string message)
    {
        return new ServiceResult<T>(
            Value: default,
            Error: new ServiceError(code, message));
    }
}