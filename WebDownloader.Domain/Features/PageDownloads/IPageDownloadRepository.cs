namespace WebDownloader.Domain.Features.PageDownloads;

public interface IPageDownloadRepository
{
    Task<DownloadedPage?> GetByIdAsync(DownloadedPageId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DownloadedPage>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveAsync(DownloadedPage page, CancellationToken cancellationToken);
}
