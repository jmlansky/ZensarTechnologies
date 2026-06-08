using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class HtmlAssetExtractor : IHtmlAssetExtractor
{
    private static readonly (string Tag, string Attribute)[] AssetSelectors = new[]
    {
        ("link", "href"),
        ("script", "src"),
        ("img", "src"),
        ("source", "src")
    };

    public AssetExtractionResult Extract(byte[] html, Uri baseUri, bool rewriteSameOriginLinks)
    {
        var document = new HtmlDocument();
        document.LoadHtml(Encoding.UTF8.GetString(html));

        var assets = new List<AssetReference>();
        var assetsSeen = new Dictionary<Uri, string>();

        foreach (var (tag, attribute) in AssetSelectors)
        {
            var nodes = document.DocumentNode.SelectNodes($"//{tag}[@{attribute}]");
            if (nodes is null)
            {
                continue;
            }

            foreach (var node in nodes)
            {
                var rawValue = node.GetAttributeValue(attribute, string.Empty);

                if (!TryResolveUri(rawValue, baseUri, out var absoluteUri))
                {
                    continue;
                }

                if (!assetsSeen.TryGetValue(absoluteUri, out var localPath))
                {
                    localPath = BuildAssetLocalPath(absoluteUri);
                    assetsSeen[absoluteUri] = localPath;
                    assets.Add(new AssetReference(absoluteUri, localPath));
                }

                node.SetAttributeValue(attribute, localPath);
            }
        }

        var internalLinks = new List<AssetReference>();
        var linksSeen = new Dictionary<Uri, string>();

        var anchorNodes = document.DocumentNode.SelectNodes("//a[@href]");
        if (anchorNodes is not null)
        {
            foreach (var node in anchorNodes)
            {
                var rawValue = node.GetAttributeValue("href", string.Empty);

                if (!TryResolveUri(rawValue, baseUri, out var absoluteUri))
                {
                    continue;
                }

                if (!IsSameOrigin(absoluteUri, baseUri))
                {
                    continue;
                }

                var canonical = StripFragment(absoluteUri);

                if (!linksSeen.TryGetValue(canonical, out var localPath))
                {
                    localPath = BuildLinkLocalPath(canonical);
                    linksSeen[canonical] = localPath;
                    internalLinks.Add(new AssetReference(canonical, localPath));
                }

                if (rewriteSameOriginLinks)
                {
                    var rewrittenHref = string.IsNullOrEmpty(absoluteUri.Fragment) ? localPath : localPath + absoluteUri.Fragment;
                    node.SetAttributeValue("href", rewrittenHref);
                }
            }
        }

        var rewrittenHtml = Encoding.UTF8.GetBytes(document.DocumentNode.OuterHtml);
        return new AssetExtractionResult(rewrittenHtml, assets, internalLinks);
    }

    private static bool TryResolveUri(string rawValue, Uri baseUri, out Uri absoluteUri)
    {
        absoluteUri = null!;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var trimmed = rawValue.Trim();

        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("#"))
        {
            return false;
        }

        if (!Uri.TryCreate(baseUri, trimmed, out var resolved))
        {
            return false;
        }

        if (resolved.Scheme != Uri.UriSchemeHttp && resolved.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        absoluteUri = resolved;
        return true;
    }

    private static bool IsSameOrigin(Uri candidate, Uri baseUri)
    {
        return string.Equals(candidate.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase);
    }

    private static Uri StripFragment(Uri uri)
    {
        if (string.IsNullOrEmpty(uri.Fragment))
        {
            return uri;
        }

        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri;
    }

    private static string BuildAssetLocalPath(Uri absoluteUri)
    {
        var extension = Path.GetExtension(absoluteUri.AbsolutePath);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".bin";
        }

        return $"asset_{ShortHash(absoluteUri.ToString())}{extension}";
    }

    private static string BuildLinkLocalPath(Uri absoluteUri)
    {
        return $"page_{ShortHash(absoluteUri.ToString())}.html";
    }

    private static string ShortHash(string value)
    {
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)))[..8].ToLowerInvariant();
    }
}
