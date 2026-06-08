using Microsoft.EntityFrameworkCore;
using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class WebDownloaderDbContext : DbContext
{
    public WebDownloaderDbContext(DbContextOptions<WebDownloaderDbContext> options) : base(options) { }

    public DbSet<DownloadedPage> DownloadedPages => Set<DownloadedPage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DownloadedPageConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
