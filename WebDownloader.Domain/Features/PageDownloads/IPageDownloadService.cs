using WebDownloader.Domain.Shared;

namespace WebDownloader.Domain.Features.PageDownloads;

public interface IPageDownloadService
{
    Task<ServiceResponse<IReadOnlyList<DownloadedPage>>> DownloadAsync(PageDownload pageDownload, CancellationToken cancellationToken);

    Task<ServiceResponse<DownloadedPage>> GetByIdAsync(DownloadedPageId id, CancellationToken cancellationToken);

    Task<ServiceResponse<IReadOnlyList<DownloadedPage>>> GetAllAsync(CancellationToken cancellationToken);
}
