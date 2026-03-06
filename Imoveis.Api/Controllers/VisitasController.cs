using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Visits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/visitas")]
public sealed class VisitasController : ApiControllerBase
{
    private readonly IVisitService _service;

    public VisitasController(IVisitService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] Guid? propertyId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new VisitQueryRequest(propertyId, status, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VisitDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VisitDto>>> Create([FromBody] VisitCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<VisitDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VisitDto>>> Update(Guid id, [FromBody] VisitUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<VisitDto>>> UpdateStatus(Guid id, [FromBody] VisitStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateStatusAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }
}
