namespace WebDownloader.Domain.Features.PageDownloads;

public class DownloadedPage
{
    public DownloadedPageId Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public DownloadStatus Status { get; set; }
    public int? HttpStatusCode { get; set; }
    public long? ContentLength { get; set; }
    public string? ContentPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset DownloadedAt { get; set; }
}
