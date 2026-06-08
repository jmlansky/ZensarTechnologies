namespace WebDownloader.Domain.Features.PageDownloads;

public interface IPageDownloader
{
    Task<PageDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken);
}
