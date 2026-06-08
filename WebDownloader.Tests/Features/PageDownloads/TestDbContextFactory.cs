using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebDownloader.Repositories.Features.PageDownloads;

namespace WebDownloader.Tests.Features.PageDownloads;

public class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<WebDownloaderDbContext>
{
    private readonly SqliteConnection _connection = connection;

    public WebDownloaderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WebDownloaderDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new WebDownloaderDbContext(options);
    }
}
