using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/imoveis")]
public sealed class ImoveisController : ApiControllerBase
{
    private readonly IPropertyService _service;

    public ImoveisController(IPropertyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new PropertyQueryRequest(search, status, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> Create([FromBody] PropertyCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<PropertyDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> Update(Guid id, [FromBody] PropertyUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> UpdateStatus(Guid id, [FromBody] PropertyStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateStatusAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost("{id:guid}/valor-base")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyRentReferenceDto>>> AddRentReference(Guid id, [FromBody] PropertyRentReferenceCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.AddRentReferenceAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("{id:guid}/valor-base/historico")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyRentReferenceDto>>>> RentHistory(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetRentHistoryAsync(id, cancellationToken);
        return OkResponse(data);
    }
}
