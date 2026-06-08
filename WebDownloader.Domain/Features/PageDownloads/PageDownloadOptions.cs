namespace WebDownloader.Domain.Features.PageDownloads;

public class PageDownloadOptions
{
    public int MaxConcurrency { get; set; } = 3;
    public int MaxAttempts { get; set; } = 3;
    public int MaxLinkedPages { get; set; } = 10;
    public string DownloadsRoot { get; set; } = "Downloads";
    public string UserAgent { get; set; } = "Mozilla/5.0 (compatible; WebDownloader/1.0)";
    public string AcceptHeader { get; set; } = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
}
