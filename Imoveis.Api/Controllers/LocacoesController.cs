using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Leases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/locacoes")]
public sealed class LocacoesController : ApiControllerBase
{
    private readonly ILeaseService _service;

    public LocacoesController(ILeaseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] Guid? propertyId, [FromQuery] Guid? tenantId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new LeaseQueryRequest(propertyId, tenantId, status, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeaseDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<LeaseDto>>> Create([FromBody] LeaseCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<LeaseDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<LeaseDto>>> Update(Guid id, [FromBody] LeaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPatch("{id:guid}/encerrar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<LeaseDto>>> Close(Guid id, [FromBody] LeaseCloseRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CloseAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("/api/imoveis/{propertyId:guid}/locacoes/historico")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaseDto>>>> PropertyHistory(Guid propertyId, CancellationToken cancellationToken)
    {
        var data = await _service.HistoryByPropertyAsync(propertyId, cancellationToken);
        return OkResponse(data);
    }
}
