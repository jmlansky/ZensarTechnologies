namespace WebDownloader.Domain.Features.PageDownloads;

public interface IPageContentStorage
{
    Task<string> SaveAsync(DownloadedPageId id, string siteName, DateTimeOffset downloadedAt, PageContent content, CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(string contentPath, CancellationToken cancellationToken);
}
