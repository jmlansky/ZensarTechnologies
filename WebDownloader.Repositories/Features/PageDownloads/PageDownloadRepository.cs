using Microsoft.EntityFrameworkCore;
using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class PageDownloadRepository(IDbContextFactory<WebDownloaderDbContext> factory) : IPageDownloadRepository
{
    private readonly IDbContextFactory<WebDownloaderDbContext> _factory = factory;

    public async Task<DownloadedPage?> GetByIdAsync(DownloadedPageId id, CancellationToken cancellationToken)
    {
        await using var dbContext = await _factory.CreateDbContextAsync(cancellationToken);

        return await dbContext.DownloadedPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DownloadedPage>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _factory.CreateDbContextAsync(cancellationToken);

        return await dbContext.DownloadedPages
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(DownloadedPage page, CancellationToken cancellationToken)
    {
        await using var dbContext = await _factory.CreateDbContextAsync(cancellationToken);

        var exists = await dbContext.DownloadedPages.AsNoTracking().AnyAsync(p => p.Id == page.Id, cancellationToken);

        if (exists)
        {
            dbContext.Update(page);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        dbContext.Add(page);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
