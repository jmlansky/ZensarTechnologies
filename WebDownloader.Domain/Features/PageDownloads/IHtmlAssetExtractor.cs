namespace WebDownloader.Domain.Features.PageDownloads;

public interface IHtmlAssetExtractor
{
    AssetExtractionResult Extract(byte[] html, Uri baseUri, bool rewriteSameOriginLinks);
}
