using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class DownloadedPageConfiguration : IEntityTypeConfiguration<DownloadedPage>
{
    public void Configure(EntityTypeBuilder<DownloadedPage> builder)
    {
        builder.ToTable("DownloadedPages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new DownloadedPageId(value));

        builder.Property(p => p.Url).IsRequired().HasMaxLength(2048);
        builder.Property(p => p.SiteName).IsRequired().HasMaxLength(512);
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.ContentPath).HasMaxLength(1024);
        builder.Property(p => p.ErrorMessage).HasMaxLength(2048);
    }
}
