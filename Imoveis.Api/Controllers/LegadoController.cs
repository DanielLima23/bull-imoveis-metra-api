using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Legacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("api/legado")]
public sealed class LegadoController : ApiControllerBase
{
    private readonly ILegacyImportService _service;

    public LegadoController(ILegacyImportService service)
    {
        _service = service;
    }

    [HttpPost("importacao")]
    public async Task<ActionResult<ApiResponse<LegacyImportResultDto>>> Import([FromBody] LegacyImportRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.ImportAsync(request, cancellationToken);
        return OkResponse(data);
    }
}
