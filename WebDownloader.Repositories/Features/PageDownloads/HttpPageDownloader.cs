using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class HttpPageDownloader : IPageDownloader
{
    private readonly HttpClient _httpClient;

    public HttpPageDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PageDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            return PageDownloadResult.Failure(statusCode, $"HTTP {statusCode} {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return PageDownloadResult.Success(statusCode, content);
    }
}
