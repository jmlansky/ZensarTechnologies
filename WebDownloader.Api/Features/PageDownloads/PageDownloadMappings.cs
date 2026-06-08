using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Api.Features.PageDownloads;

public static class PageDownloadMappings
{
    public static PageDownload ToDomain(this DownloadPagesRequest request)
    {
        return new PageDownload(request.Urls);
    }

    public static DownloadedPageResponse ToResponse(this DownloadedPage page)
    {
        return new DownloadedPageResponse
        {
            Id = page.Id.ToString(),
            Url = page.Url,
            SiteName = page.SiteName,
            Status = page.Status.ToString(),
            HttpStatusCode = page.HttpStatusCode,
            ContentLength = page.ContentLength,
            ContentPath = page.ContentPath,
            ErrorMessage = page.ErrorMessage,
            DownloadedAt = page.DownloadedAt
        };
    }

    public static IReadOnlyList<DownloadedPageResponse> ToResponse(this IEnumerable<DownloadedPage> pages)
    {
        return pages.Select(ToResponse).ToList();
    }
}
