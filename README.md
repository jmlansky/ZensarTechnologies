# WebDownloader

Asynchronously downloads and persists web pages to disk along with their static
assets and first-level same-origin links. Page metadata is stored in SQLite.

The solution ships two front ends that share the same domain layer:

- **WebDownloader.Console** — command-line tool that takes URLs as arguments.
- **WebDownloader.Api** — ASP.NET Core Web API with a Swagger UI.

## Project structure

```
WebDownloader/
├── WebDownloader.Domain/        Entities, services, contracts, options
├── WebDownloader.Repositories/  EF Core (SQLite), filesystem storage,
│                                HTTP downloader, HTML asset extractor
├── WebDownloader.Api/           ASP.NET Core Web API + Swagger
├── WebDownloader.Console/       Console application
└── WebDownloader.Tests/         xUnit integration tests
```

Files are organized by feature inside each project
(`Features/PageDownloads/...`) following the SCREAMING / Clean architecture
guideline.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional, only if you need to add new migrations) `dotnet ef` tool:
  ```bash
  dotnet tool install --global dotnet-ef --version 8.0.8
  ```

## Build

From the solution root:

```bash
dotnet build
```

## Run the Console

```bash
cd WebDownloader.Console
dotnet run -- https://example.com https://en.wikipedia.org/wiki/HTTP
```

Each argument is treated as a URL. Output:

```
[Succeeded] https://example.com -> <path-to-page.html>
[Succeeded] https://en.wikipedia.org/wiki/HTTP -> <path-to-page.html>
```

## Run the API

```bash
cd WebDownloader.Api
dotnet run
```

Open `https://localhost:<port>/swagger`. Endpoints:

| Method | Route                          | Description                  |
| ------ | ------------------------------ | ---------------------------- |
| POST   | `/api/page-downloads`          | Download a list of URLs.     |
| GET    | `/api/page-downloads/{id}`     | Get a single result by id.   |
| GET    | `/api/page-downloads`          | List all stored results.     |

Sample request body:

```json
{
  "urls": [
    "https://example.com",
    "https://en.wikipedia.org/wiki/HTTP"
  ]
}
```

## Configuration

Both `WebDownloader.Api/appsettings.json` and
`WebDownloader.Console/appsettings.json` expose the same section:

```json
"ConnectionStrings": {
  "WebDownloader": "Data Source=webdownloader.db"
},
"PageDownload": {
  "DownloadsRoot": "Downloads",
  "MaxConcurrency": 3,
  "MaxAttempts": 3,
  "MaxLinkedPages": 10,
  "UserAgent": "Mozilla/5.0 (compatible; WebDownloader/1.0)",
  "AcceptHeader": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
}
```

| Key              | Meaning                                                       |
| ---------------- | ------------------------------------------------------------- |
| `DownloadsRoot`  | Folder where downloaded pages are written.                    |
| `MaxConcurrency` | Maximum parallel page downloads per request.                  |
| `MaxAttempts`    | Total attempts per page (initial + retries).                  |
| `MaxLinkedPages` | First-level same-origin links followed from each root page.   |
| `UserAgent`      | User-Agent header sent with every HTTP request.               |
| `AcceptHeader`   | Accept header sent with every HTTP request.                   |

## Storage layout

Each downloaded URL produces a self-contained bundle:

```
Downloads/
└── <host>/
    └── <yyyyMMddHHmmss>_<guid>/
        ├── page.html              Root page with rewritten asset/link paths
        ├── page_<hash>.html       First-level linked pages (same-origin)
        └── asset_<hash>.<ext>     CSS, JS, images, fonts
```

Metadata for every download (URL, status, HTTP code, timestamp, content path,
error message) is persisted in `webdownloader.db` (SQLite).

## Tests

```bash
dotnet test
```

Integration tests cover happy paths, retry exhaustion, asset extraction,
internal-link crawling, external-link skipping, invalid URLs and duplicates.

## Limitations

- Single Page Applications (React / Vue / Angular) won't render offline because
  their JavaScript depends on a live backend.
- CSS `@import` rules and `url(...)` references inside CSS files are not
  followed.
- The `srcset` attribute is not processed.
- Crawl depth is fixed to 1 (only direct same-origin links from the root page).
