using System.ComponentModel.DataAnnotations;

namespace WebDownloader.Api.Features.PageDownloads;

public class DownloadPagesRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> Urls { get; set; } = [];
}
