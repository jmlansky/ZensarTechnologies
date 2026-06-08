using Microsoft.EntityFrameworkCore;
using WebDownloader.Domain.Features.PageDownloads;
using WebDownloader.Repositories.Features.PageDownloads;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("WebDownloader")
    ?? throw new InvalidOperationException("Connection string 'WebDownloader' is not configured.");
builder.Services.AddDbContextFactory<WebDownloaderDbContext>(options => options.UseSqlite(connectionString));

var pageDownloadOptions = builder.Configuration.GetSection("PageDownload").Get<PageDownloadOptions>() ?? new PageDownloadOptions();
builder.Services.AddSingleton(pageDownloadOptions);

var absoluteDownloadsRoot = Path.IsPathRooted(pageDownloadOptions.DownloadsRoot)
    ? pageDownloadOptions.DownloadsRoot
    : Path.Combine(builder.Environment.ContentRootPath, pageDownloadOptions.DownloadsRoot);

builder.Services.AddSingleton<IPageContentStorage>(_ => new FileSystemPageContentStorage(absoluteDownloadsRoot));
builder.Services.AddSingleton<IHtmlAssetExtractor, HtmlAssetExtractor>();
builder.Services.AddHttpClient<IPageDownloader, HttpPageDownloader>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(pageDownloadOptions.UserAgent);
    client.DefaultRequestHeaders.Accept.ParseAdd(pageDownloadOptions.AcceptHeader);
});
builder.Services.AddSingleton<IPageDownloadRepository, PageDownloadRepository>();
builder.Services.AddScoped<IPageDownloadService, PageDownloadService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

await using (var dbContext = await app.Services.GetRequiredService<IDbContextFactory<WebDownloaderDbContext>>().CreateDbContextAsync())
{
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
