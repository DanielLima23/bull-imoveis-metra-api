using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Pendencies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/pendencias")]
public sealed class PendenciasController : ApiControllerBase
{
    private readonly IPendencyService _service;

    public PendenciasController(IPendencyService service)
    {
        _service = service;
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendencyTypeDto>>>> Types(CancellationToken cancellationToken)
    {
        var data = await _service.ListTypesAsync(cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("tipos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PendencyTypeDto>>> CreateType([FromBody] PendencyTypeCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateTypeAsync(request, cancellationToken);
        return OkResponse(data);
    }

    [HttpPut("tipos/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PendencyTypeDto>>> UpdateType(Guid id, [FromBody] PendencyTypeUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateTypeAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] Guid? propertyId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new PendencyQueryRequest(propertyId, status, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PendencyDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PendencyDto>>> Create([FromBody] PendencyCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<PendencyDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PendencyDto>>> Update(Guid id, [FromBody] PendencyUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPatch("{id:guid}/resolver")]
    public async Task<ActionResult<ApiResponse<PendencyDto>>> Resolve(Guid id, [FromBody] PendencyResolveRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.ResolveAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }
}
