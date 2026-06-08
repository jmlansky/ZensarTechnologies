namespace WebDownloader.Domain.Features.PageDownloads;

public class AssetExtractionResult
{
    public byte[] RewrittenHtml { get; }
    public IReadOnlyList<AssetReference> Assets { get; }
    public IReadOnlyList<AssetReference> InternalLinks { get; }

    public AssetExtractionResult(byte[] rewrittenHtml, IReadOnlyList<AssetReference> assets, IReadOnlyList<AssetReference> internalLinks)
    {
        RewrittenHtml = rewrittenHtml;
        Assets = assets;
        InternalLinks = internalLinks;
    }
}
