using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
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
    public async Task<ActionResult<ApiResponse<object>>> Query(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? motivoOciosidade,
        [FromQuery] string? propertyType,
        [FromQuery] string? city,
        [FromQuery] string? proprietary,
        [FromQuery] string? administrator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var occupancyStatus = Request.Query.TryGetValue("occupancyStatus", out var occupancyValue)
            ? occupancyValue.ToString()
            : null;

        var assetState = Request.Query.TryGetValue("assetState", out var assetValue)
            ? assetValue.ToString()
            : null;

        NormalizeLegacyStatus(ref status, occupancyStatus, assetState);
        var data = await _service.QueryAsync(
            new PropertyQueryRequest(search, status, motivoOciosidade, propertyType, city, proprietary, administrator, page, pageSize),
            cancellationToken);
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

    [HttpGet("{id:guid}/contas-modelo")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyChargeTemplateDto>>>> ChargeTemplates(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.ListChargeTemplatesAsync(id, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("{id:guid}/contas-modelo")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyChargeTemplateDto>>> AddChargeTemplate(Guid id, [FromBody] PropertyChargeTemplateCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.AddChargeTemplateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPut("{id:guid}/contas-modelo/{templateId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyChargeTemplateDto>>> UpdateChargeTemplate(Guid id, Guid templateId, [FromBody] PropertyChargeTemplateUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateChargeTemplateAsync(id, templateId, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("{id:guid}/historico")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyHistoryEntryDto>>>> History(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.ListHistoryAsync(id, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("{id:guid}/historico")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyHistoryEntryDto>>> AddHistory(Guid id, [FromBody] PropertyHistoryEntryCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.AddHistoryAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("{id:guid}/anexos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PropertyAttachmentDto>>>> Attachments(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.ListAttachmentsAsync(id, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("{id:guid}/anexos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PropertyAttachmentDto>>> AddAttachment(Guid id, [FromBody] PropertyAttachmentCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.AddAttachmentAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("{id:guid}/extrato-mensal")]
    public async Task<ActionResult<ApiResponse<PropertyMonthlyStatementDto>>> MonthlyStatement(Guid id, [FromQuery] int year, [FromQuery] int? month, CancellationToken cancellationToken)
    {
        var data = await _service.GetMonthlyStatementAsync(id, year, month, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    private static void NormalizeLegacyStatus(ref string? status, string? occupancyStatus, string? assetState)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        status = PropertyStatusContract.TryMapLegacyStatus(occupancyStatus, assetState);
    }
}
