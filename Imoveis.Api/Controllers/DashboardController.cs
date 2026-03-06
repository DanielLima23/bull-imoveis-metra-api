using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet("imobiliario")]
    public async Task<ActionResult<ApiResponse<RealEstateDashboardDto>>> Get([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var month = mes ?? now.Month;
        var year = ano ?? now.Year;

        var data = await _service.GetAsync(month, year, cancellationToken);
        return OkResponse(data);
    }
}
