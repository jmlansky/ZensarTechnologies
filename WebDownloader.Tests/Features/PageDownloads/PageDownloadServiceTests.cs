using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebDownloader.Domain.Features.PageDownloads;
using WebDownloader.Repositories.Features.PageDownloads;

namespace WebDownloader.Tests.Features.PageDownloads;

public class PageDownloadServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebDownloaderDbContext _dbContext;
    private readonly string _tempRoot;
    private readonly FakePageDownloader _downloader;
    private readonly PageDownloadService _service;

    public PageDownloadServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var factory = new TestDbContextFactory(_connection);
        _dbContext = factory.CreateDbContext();
        _dbContext.Database.EnsureCreated();

        var repository = new PageDownloadRepository(factory);

        _tempRoot = Path.Combine(Path.GetTempPath(), "WebDownloader.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var storage = new FileSystemPageContentStorage(_tempRoot);
        var extractor = new HtmlAssetExtractor();
        _downloader = new FakePageDownloader();
        var options = new PageDownloadOptions { MaxConcurrency = 1 };

        _service = new PageDownloadService(_downloader, repository, storage, extractor, options, TimeProvider.System, NullLogger<PageDownloadService>.Instance);
    }

    [Fact]
    public async Task GivenNoUrls_WhenDownloading_ThenFailureResponseIsReturned()
    {
        var response = await _service.DownloadAsync(new PageDownload(Array.Empty<string>()), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.NotNull(response.Error);
    }

    [Fact]
    public async Task GivenValidUrl_WhenDownloading_ThenPageIsPersisted()
    {
        const string url = "https://example.com/";
        _downloader.SetSuccess(url, "<html><body>hello</body></html>");

        var response = await _service.DownloadAsync(new PageDownload(new[] { url }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var page = Assert.Single(response.Value!);
        Assert.Equal(DownloadStatus.Succeeded, page.Status);
        Assert.NotNull(page.ContentPath);
        Assert.True(File.Exists(page.ContentPath));

        var stored = await _dbContext.DownloadedPages.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(stored);
        Assert.Equal(DownloadStatus.Succeeded, stored!.Status);
    }

    [Fact]
    public async Task GivenAllAttemptsFail_WhenDownloading_ThenPageIsMarkedAsFailed()
    {
        const string url = "https://example.com/";
        _downloader.SetFailure(url, 404, "Not Found");

        var response = await _service.DownloadAsync(new PageDownload(new[] { url }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var page = Assert.Single(response.Value!);
        Assert.Equal(DownloadStatus.Failed, page.Status);
        Assert.Equal(404, page.HttpStatusCode);
        Assert.Equal(3, _downloader.CallCounts[url]);
    }

    [Fact]
    public async Task GivenPageWithStylesheetAndImage_WhenDownloading_ThenAssetsArePersisted()
    {
        const string pageUrl = "https://example.com/";
        const string cssUrl = "https://example.com/style.css";
        const string imgUrl = "https://example.com/logo.png";

        var html = "<html><head><link rel='stylesheet' href='style.css'></head><body><img src='/logo.png'></body></html>";
        _downloader.SetSuccess(pageUrl, html);
        _downloader.SetSuccess(cssUrl, "body { color: red; }");
        _downloader.SetSuccess(imgUrl, "fake-image-bytes");

        var response = await _service.DownloadAsync(new PageDownload(new[] { pageUrl }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var page = response.Value!.Single();
        Assert.Equal(DownloadStatus.Succeeded, page.Status);

        var pageFolder = Path.GetDirectoryName(page.ContentPath)!;
        var assetFiles = Directory.GetFiles(pageFolder).Where(f => Path.GetFileName(f).StartsWith("asset_")).ToList();
        Assert.Equal(2, assetFiles.Count);
    }

    [Fact]
    public async Task GivenSameOriginAnchor_WhenDownloading_ThenLinkedPageIsAlsoDownloaded()
    {
        const string pageUrl = "https://example.com/";
        const string linkedUrl = "https://example.com/about";

        _downloader.SetSuccess(pageUrl, "<html><body><a href='/about'>about</a></body></html>");
        _downloader.SetSuccess(linkedUrl, "<html><body>about page</body></html>");

        var response = await _service.DownloadAsync(new PageDownload(new[] { pageUrl }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var page = response.Value!.Single();

        var pageFolder = Path.GetDirectoryName(page.ContentPath)!;
        var linkedPageFiles = Directory.GetFiles(pageFolder).Where(f => Path.GetFileName(f).StartsWith("page_")).ToList();
        Assert.Single(linkedPageFiles);
        Assert.Equal(1, _downloader.CallCounts[linkedUrl]);
    }

    [Fact]
    public async Task GivenExternalAnchor_WhenDownloading_ThenExternalLinkIsNotCrawled()
    {
        const string pageUrl = "https://example.com/";
        const string externalUrl = "https://other.com/foo";

        _downloader.SetSuccess(pageUrl, "<html><body><a href='https://other.com/foo'>foo</a></body></html>");

        var response = await _service.DownloadAsync(new PageDownload(new[] { pageUrl }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.False(_downloader.CallCounts.ContainsKey(externalUrl));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("/relative/path")]
    public async Task GivenInvalidUrl_WhenDownloading_ThenPageIsMarkedAsFailedWithoutNetworkCall(string invalidUrl)
    {
        var response = await _service.DownloadAsync(new PageDownload(new[] { invalidUrl }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var page = Assert.Single(response.Value!);
        Assert.Equal(DownloadStatus.Failed, page.Status);
        Assert.NotNull(page.ErrorMessage);
        Assert.Empty(_downloader.CallCounts);
    }

    [Fact]
    public async Task GivenMixOfValidAndInvalidUrls_WhenDownloading_ThenValidOnesAreProcessedAndInvalidOnesAreFailed()
    {
        const string validUrl = "https://example.com/";
        const string invalidUrl = "not-a-url";

        _downloader.SetSuccess(validUrl, "<html><body>hello</body></html>");

        var response = await _service.DownloadAsync(new PageDownload(new[] { validUrl, invalidUrl }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(2, response.Value!.Count);

        var succeeded = response.Value!.Single(p => p.Status == DownloadStatus.Succeeded);
        var failed = response.Value!.Single(p => p.Status == DownloadStatus.Failed);

        Assert.Equal(validUrl, succeeded.Url);
        Assert.Equal(invalidUrl, failed.Url);
    }

    [Fact]
    public async Task GivenDuplicateUrls_WhenDownloading_ThenEachOccurrenceIsDownloaded()
    {
        const string url = "https://example.com/";
        _downloader.SetSuccess(url, "<html><body>hello</body></html>");

        var response = await _service.DownloadAsync(new PageDownload(new[] { url, url }), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(2, response.Value!.Count);
        Assert.All(response.Value!, page => Assert.Equal(DownloadStatus.Succeeded, page.Status));

        var stored = await _dbContext.DownloadedPages.AsNoTracking().ToListAsync();
        Assert.Equal(2, stored.Count);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();

        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
