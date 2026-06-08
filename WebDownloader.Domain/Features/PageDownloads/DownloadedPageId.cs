namespace WebDownloader.Domain.Features.PageDownloads;

public readonly record struct DownloadedPageId(Guid Value)
{
    public static DownloadedPageId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
