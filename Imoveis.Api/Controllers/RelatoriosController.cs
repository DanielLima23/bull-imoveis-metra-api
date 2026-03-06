using Imoveis.Application.Abstractions.Services;
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

    [HttpGet("financeiro")]
    public async Task<IActionResult> Financial([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var month = mes ?? now.Month;
        var year = ano ?? now.Year;

        var file = await _service.BuildFinancialCsvAsync(month, year, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("vacancia")]
    public async Task<IActionResult> Vacancy([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var month = mes ?? now.Month;
        var year = ano ?? now.Year;

        var file = await _service.BuildVacancyCsvAsync(month, year, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("pendencias")]
    public async Task<IActionResult> Pendencies(CancellationToken cancellationToken)
    {
        var file = await _service.BuildPendenciesCsvAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
