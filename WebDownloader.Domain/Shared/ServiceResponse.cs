namespace WebDownloader.Domain.Shared;

public class ServiceResponse<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private ServiceResponse(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static ServiceResponse<T> Success(T value) => new(true, value, null);

    public static ServiceResponse<T> Failure(string error) => new(false, default, error);
}
