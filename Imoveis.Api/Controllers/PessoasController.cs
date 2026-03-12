using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Parties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/pessoas")]
public sealed class PessoasController : ApiControllerBase
{
    private readonly IPartyService _service;

    public PessoasController(IPartyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query(
        [FromQuery] string? search,
        [FromQuery] string? kind,
        [FromQuery] bool? active,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new PartyQueryRequest(search, kind, active, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PartyDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PartyDto>>> Create([FromBody] PartyCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<PartyDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PartyDto>>> Update(Guid id, [FromBody] PartyUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted
            ? OkResponse<object>(new { message = "Party deleted." })
            : NotFoundResponse();
    }
}
