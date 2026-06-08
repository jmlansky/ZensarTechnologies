using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Repositories.Features.PageDownloads;

public class FileSystemPageContentStorage : IPageContentStorage
{
    private const string FileName = "page.html";

    private readonly string _rootPath;

    public FileSystemPageContentStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<string> SaveAsync(DownloadedPageId id, string siteName, DateTimeOffset downloadedAt, PageContent content, CancellationToken cancellationToken)
    {
        var siteFolder = Sanitize(siteName);
        var idFolder = $"{downloadedAt.UtcDateTime:yyyyMMddHHmmss}_{id.Value}";

        var directory = Path.Combine(_rootPath, siteFolder, idFolder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, FileName);
        await File.WriteAllBytesAsync(fullPath, content.Html, cancellationToken);

        foreach (var (relativePath, bytes) in content.Assets)
        {
            var assetPath = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var assetDirectory = Path.GetDirectoryName(assetPath);

            if (!string.IsNullOrEmpty(assetDirectory))
            {
                Directory.CreateDirectory(assetDirectory);
            }

            await File.WriteAllBytesAsync(assetPath, bytes, cancellationToken);
        }

        return fullPath.Replace('\\', '/');
    }

    public async Task<byte[]?> ReadAsync(string contentPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(contentPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(contentPath, cancellationToken);
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
