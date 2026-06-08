namespace WebDownloader.Api.Features.PageDownloads;

public class DownloadedPageResponse
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public long? ContentLength { get; set; }
    public string? ContentPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset DownloadedAt { get; set; }
}
