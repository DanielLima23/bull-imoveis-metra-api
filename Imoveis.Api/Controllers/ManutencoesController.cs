using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/manutencoes")]
public sealed class ManutencoesController : ApiControllerBase
{
    private readonly IMaintenanceService _service;

    public ManutencoesController(IMaintenanceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] Guid? propertyId, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new MaintenanceQueryRequest(propertyId, status, priority, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Create([FromBody] MaintenanceCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<MaintenanceDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Update(Guid id, [FromBody] MaintenanceUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> UpdateStatus(Guid id, [FromBody] MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateStatusAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }
}
