using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebDownloader.Domain.Features.PageDownloads;
using WebDownloader.Repositories.Features.PageDownloads;

if (args.Length == 0)
{
    Console.WriteLine("Usage: WebDownloader.Console <url> [<url> ...]");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

var connectionString = configuration.GetConnectionString("WebDownloader")
    ?? throw new InvalidOperationException("Connection string 'WebDownloader' is not configured.");
services.AddDbContextFactory<WebDownloaderDbContext>(options => options.UseSqlite(connectionString));

var pageDownloadOptions = configuration.GetSection("PageDownload").Get<PageDownloadOptions>() ?? new PageDownloadOptions();
services.AddSingleton(pageDownloadOptions);

var absoluteDownloadsRoot = Path.IsPathRooted(pageDownloadOptions.DownloadsRoot)
    ? pageDownloadOptions.DownloadsRoot
    : Path.Combine(AppContext.BaseDirectory, pageDownloadOptions.DownloadsRoot);

services.AddSingleton<IPageContentStorage>(_ => new FileSystemPageContentStorage(absoluteDownloadsRoot));
services.AddSingleton<IHtmlAssetExtractor, HtmlAssetExtractor>();
services.AddHttpClient<IPageDownloader, HttpPageDownloader>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(pageDownloadOptions.UserAgent);
    client.DefaultRequestHeaders.Accept.ParseAdd(pageDownloadOptions.AcceptHeader);
});
services.AddSingleton<IPageDownloadRepository, PageDownloadRepository>();
services.AddScoped<IPageDownloadService, PageDownloadService>();
services.AddSingleton(TimeProvider.System);

var provider = services.BuildServiceProvider();

await using (var dbContext = await provider.GetRequiredService<IDbContextFactory<WebDownloaderDbContext>>().CreateDbContextAsync())
{
    await dbContext.Database.MigrateAsync();
}

using (var scope = provider.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<IPageDownloadService>();
    var request = new PageDownload(args);

    var response = await service.DownloadAsync(request, CancellationToken.None);

    if (!response.IsSuccess)
    {
        Console.Error.WriteLine($"Error: {response.Error}");
        return 1;
    }

    foreach (var page in response.Value!)
    {
        Console.WriteLine($"[{page.Status}] {page.Url} -> {page.ContentPath ?? page.ErrorMessage}");
    }
}

return 0;
