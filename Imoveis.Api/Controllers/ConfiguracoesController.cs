using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Route("api/configuracoes")]
public sealed class ConfiguracoesController : ApiControllerBase
{
    private readonly ISystemSettingsService _service;

    public ConfiguracoesController(ISystemSettingsService service)
    {
        _service = service;
    }

    [HttpGet("publico")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> GetPublic(CancellationToken cancellationToken)
    {
        var data = await _service.GetPublicAsync(cancellationToken);
        return OkResponse(data);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOrOperator")]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> Get(CancellationToken cancellationToken)
    {
        var data = await _service.GetAsync(cancellationToken);
        return OkResponse(data);
    }

    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> Update([FromBody] SystemSettingsUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(request, cancellationToken);
        return OkResponse(data);
    }
}
