using Microsoft.Extensions.Logging;
using WebDownloader.Domain.Shared;

namespace WebDownloader.Domain.Features.PageDownloads;

public class PageDownloadService(IPageDownloader downloader, IPageDownloadRepository repository, IPageContentStorage contentStorage, IHtmlAssetExtractor assetExtractor, PageDownloadOptions options, TimeProvider timeProvider, ILogger<PageDownloadService> logger) : IPageDownloadService
{
    private readonly IPageDownloader _downloader = downloader;
    private readonly IPageDownloadRepository _repository = repository;
    private readonly IPageContentStorage _contentStorage = contentStorage;
    private readonly IHtmlAssetExtractor _assetExtractor = assetExtractor;
    private readonly PageDownloadOptions _options = options;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<PageDownloadService> _logger = logger;

    public async Task<ServiceResponse<IReadOnlyList<DownloadedPage>>> DownloadAsync(PageDownload pageDownload, CancellationToken cancellationToken)
    {
        if (pageDownload.Urls.Count == 0)
        {
            return ServiceResponse<IReadOnlyList<DownloadedPage>>.Failure("No URLs were provided.");
        }

        var maxConcurrency = _options.MaxConcurrency > 0 ? _options.MaxConcurrency : 1;
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = pageDownload.Urls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await DownloadSingleAsync(url, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);
        return ServiceResponse<IReadOnlyList<DownloadedPage>>.Success(results);
    }

    public async Task<ServiceResponse<DownloadedPage>> GetByIdAsync(DownloadedPageId id, CancellationToken cancellationToken)
    {
        var page = await _repository.GetByIdAsync(id, cancellationToken);

        return page is null
            ? ServiceResponse<DownloadedPage>.Failure($"Downloaded page '{id}' was not found.")
            : ServiceResponse<DownloadedPage>.Success(page);
    }

    public async Task<ServiceResponse<IReadOnlyList<DownloadedPage>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var pages = await _repository.GetAllAsync(cancellationToken);
        return ServiceResponse<IReadOnlyList<DownloadedPage>>.Success(pages);
    }

    private async Task<DownloadedPage> DownloadSingleAsync(string url, CancellationToken cancellationToken)
    {
        if (!IsValidAbsoluteHttpUrl(url))
        {
            var invalidPage = new DownloadedPage
            {
                Id = DownloadedPageId.New(),
                Url = url ?? string.Empty,
                SiteName = string.IsNullOrWhiteSpace(url) ? "(empty)" : url,
                Status = DownloadStatus.Failed,
                DownloadedAt = _timeProvider.GetUtcNow(),
                ErrorMessage = "URL is invalid. Expected an absolute http or https URL."
            };

            await _repository.SaveAsync(invalidPage, cancellationToken);
            _logger.LogWarning("Rejected invalid URL: {Url}", url);
            return invalidPage;
        }

        var page = new DownloadedPage
        {
            Id = DownloadedPageId.New(),
            Url = url,
            SiteName = ExtractSiteName(url),
            Status = DownloadStatus.Pending,
            DownloadedAt = _timeProvider.GetUtcNow()
        };

        await _repository.SaveAsync(page, cancellationToken);

        PageDownloadResult? lastFailure = null;

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            var attemptResult = await TryDownloadAsync(url, attempt, cancellationToken);

            if (attemptResult.IsSuccess)
            {
                var pageContent = await BuildPageContentAsync(url, attemptResult.Content!, cancellationToken);

                var contentPath = await _contentStorage.SaveAsync(page.Id, page.SiteName, page.DownloadedAt, pageContent, cancellationToken);

                page.Status = DownloadStatus.Succeeded;
                page.HttpStatusCode = attemptResult.HttpStatusCode;
                page.ContentLength = attemptResult.Content!.LongLength;
                page.ContentPath = contentPath;

                await _repository.SaveAsync(page, cancellationToken);
                return page;
            }

            lastFailure = attemptResult;
        }

        var finalErrorMessage = lastFailure?.ErrorMessage ?? "Unknown error.";
        page.Status = DownloadStatus.Failed;
        page.HttpStatusCode = lastFailure?.HttpStatusCode;
        page.ErrorMessage = finalErrorMessage;
        await _repository.SaveAsync(page, cancellationToken);

        _logger.LogWarning("Failed to download {Url} after {Attempts} attempts. Last error: {Error}", url, _options.MaxAttempts, finalErrorMessage);

        return page;
    }

    private async Task<PageContent> BuildPageContentAsync(string url, byte[] html, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri))
        {
            return new PageContent(html, new Dictionary<string, byte[]>());
        }

        var files = new Dictionary<string, byte[]>();
        var rootExtraction = _assetExtractor.Extract(html, baseUri, rewriteSameOriginLinks: true);

        await DownloadAssetsAsync(rootExtraction.Assets, files, cancellationToken);

        foreach (var link in rootExtraction.InternalLinks.Take(_options.MaxLinkedPages))
        {
            if (files.ContainsKey(link.LocalPath))
            {
                continue;
            }

            var linkedHtml = await TryDownloadBytesAsync(link.Url.ToString(), cancellationToken);
            if (linkedHtml is null)
            {
                continue;
            }

            var linkedExtraction = _assetExtractor.Extract(linkedHtml, link.Url, rewriteSameOriginLinks: false);
            files[link.LocalPath] = linkedExtraction.RewrittenHtml;

            await DownloadAssetsAsync(linkedExtraction.Assets, files, cancellationToken);
        }

        return new PageContent(rootExtraction.RewrittenHtml, files);
    }

    private async Task DownloadAssetsAsync(IReadOnlyList<AssetReference> assets, IDictionary<string, byte[]> files, CancellationToken cancellationToken)
    {
        foreach (var asset in assets)
        {
            if (files.ContainsKey(asset.LocalPath))
            {
                continue;
            }

            var bytes = await TryDownloadBytesAsync(asset.Url.ToString(), cancellationToken);
            if (bytes is not null)
            {
                files[asset.LocalPath] = bytes;
            }
        }
    }

    private async Task<byte[]?> TryDownloadBytesAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _downloader.DownloadAsync(url, cancellationToken);

            if (result.IsSuccess && result.Content is not null)
            {
                return result.Content;
            }

            _logger.LogWarning("Skipped {Url}: {Error}", url, result.ErrorMessage);
            return null;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Download for {Url} threw an exception.", url);
            return null;
        }
    }

    private async Task<PageDownloadResult> TryDownloadAsync(string url, int attempt, CancellationToken cancellationToken)
    {
        try
        {
            return await _downloader.DownloadAsync(url, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Download attempt {Attempt} for {Url} threw an exception.", attempt, url);
            return PageDownloadResult.Failure(httpStatusCode: null, errorMessage: ex.Message);
        }
    }

    private static string ExtractSiteName(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        return url;
    }

    private static bool IsValidAbsoluteHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
