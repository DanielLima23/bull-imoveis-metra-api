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

    [HttpGet("{id:guid}/detalhe")]
    public async Task<ActionResult<ApiResponse<PropertyDetailDto>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetDetailAsync(id, cancellationToken);
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

    [HttpGet("{id:guid}/historico")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyHistoryEntryDto>>>> History(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetHistoryAsync(id, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("{id:guid}/historico")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyHistoryEntryDto>>> AddHistory(Guid id, [FromBody] PropertyHistoryEntryCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.AddHistoryAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("{id:guid}/documentos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyDocumentDto>>>> Documents(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetDocumentsAsync(id, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("{id:guid}/documentos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyDocumentDto>>> AddDocument(Guid id, [FromBody] PropertyDocumentCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.AddDocumentAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("{id:guid}/relacionamentos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyPartyLinkDto>>>> Relationships(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetRelationshipsAsync(id, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("{id:guid}/relacionamentos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyPartyLinkDto>>> LinkParty(Guid id, [FromBody] PropertyPartyLinkCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.LinkPartyAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }
}
