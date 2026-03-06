using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/locatarios")]
public sealed class LocatariosController : ApiControllerBase
{
    private readonly ITenantService _service;

    public LocatariosController(ITenantService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] string? search, [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new TenantQueryRequest(search, active, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Create([FromBody] TenantCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<TenantDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Update(Guid id, [FromBody] TenantUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }
}
