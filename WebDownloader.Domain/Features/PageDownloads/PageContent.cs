namespace WebDownloader.Domain.Features.PageDownloads;

public class PageContent
{
    public byte[] Html { get; }
    public IReadOnlyDictionary<string, byte[]> Assets { get; }

    public PageContent(byte[] html, IReadOnlyDictionary<string, byte[]> assets)
    {
        Html = html;
        Assets = assets;
    }
}
