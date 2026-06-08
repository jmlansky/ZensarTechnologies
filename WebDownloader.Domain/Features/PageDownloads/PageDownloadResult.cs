namespace WebDownloader.Domain.Features.PageDownloads;

public class PageDownloadResult
{
    public bool IsSuccess { get; }
    public int? HttpStatusCode { get; }
    public byte[]? Content { get; }
    public string? ErrorMessage { get; }

    private PageDownloadResult(bool isSuccess, int? httpStatusCode, byte[]? content, string? errorMessage)
    {
        IsSuccess = isSuccess;
        HttpStatusCode = httpStatusCode;
        Content = content;
        ErrorMessage = errorMessage;
    }

    public static PageDownloadResult Success(int httpStatusCode, byte[] content) =>
        new(true, httpStatusCode, content, null);

    public static PageDownloadResult Failure(int? httpStatusCode, string errorMessage) =>
        new(false, httpStatusCode, null, errorMessage);
}
