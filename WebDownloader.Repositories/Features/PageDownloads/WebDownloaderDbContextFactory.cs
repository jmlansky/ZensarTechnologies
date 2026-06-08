using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class WebDownloaderDbContextFactory : IDesignTimeDbContextFactory<WebDownloaderDbContext>
{
    public WebDownloaderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WebDownloaderDbContext>()
            .UseSqlite("Data Source=webdownloader.db")
            .Options;

        return new WebDownloaderDbContext(options);
    }
}
