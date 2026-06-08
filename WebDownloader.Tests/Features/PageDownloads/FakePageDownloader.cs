using System.Text;
using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Tests.Features.PageDownloads;

public class FakePageDownloader : IPageDownloader
{
    private readonly Dictionary<string, PageDownloadResult> _responses = new();

    public Dictionary<string, int> CallCounts { get; } = new();

    public void SetSuccess(string url, string content, int statusCode = 200)
    {
        _responses[url] = PageDownloadResult.Success(statusCode, Encoding.UTF8.GetBytes(content));
    }

    public void SetSuccess(string url, byte[] content, int statusCode = 200)
    {
        _responses[url] = PageDownloadResult.Success(statusCode, content);
    }

    public void SetFailure(string url, int? statusCode, string error)
    {
        _responses[url] = PageDownloadResult.Failure(statusCode, error);
    }

    public Task<PageDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        CallCounts[url] = CallCounts.GetValueOrDefault(url, 0) + 1;

        return Task.FromResult(_responses.TryGetValue(url, out var response)
            ? response
            : PageDownloadResult.Failure(httpStatusCode: null, errorMessage: $"No response configured for {url}"));
    }
}
