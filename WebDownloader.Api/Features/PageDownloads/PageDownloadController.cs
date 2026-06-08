using Microsoft.AspNetCore.Mvc;
using WebDownloader.Domain.Features.PageDownloads;

namespace WebDownloader.Api.Features.PageDownloads;

[ApiController]
[Route("api/page-downloads")]
[Produces("application/json")]
public class PageDownloadController(IPageDownloadService service) : ControllerBase
{
    private readonly IPageDownloadService _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<DownloadedPageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Download([FromBody] DownloadPagesRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.DownloadAsync(request.ToDomain(), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value!.ToResponse());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DownloadedPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return NotFound();
        }

        var result = await _service.GetByIdAsync(new DownloadedPageId(guid), cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Value!.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DownloadedPageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return Ok(result.Value!.ToResponse());
    }
}
