using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[ApiController]
[Route("api/relatorios")]
public sealed class RelatoriosController : ControllerBase
{
    private readonly IReportService _service;

    public RelatoriosController(IReportService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReportCatalogItemDto>>> Catalog(CancellationToken cancellationToken)
    {
        var data = await _service.ListCatalogAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> DownloadBySlug(string slug, [FromQuery] int? mes, [FromQuery] int? ano, CancellationToken cancellationToken)
    {
        var file = await _service.BuildBySlugAsync(slug, mes, ano, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("financeiro")]
    public async Task<IActionResult> Financial([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var file = await _service.BuildFinancialCsvAsync(mes ?? now.Month, ano ?? now.Year, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("vacancia")]
    public async Task<IActionResult> Vacancy([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var file = await _service.BuildVacancyCsvAsync(mes ?? now.Month, ano ?? now.Year, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("pendencias")]
    public async Task<IActionResult> Pendencies(CancellationToken cancellationToken)
    {
        var file = await _service.BuildPendenciesCsvAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
