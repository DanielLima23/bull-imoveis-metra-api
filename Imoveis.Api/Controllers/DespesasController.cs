using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Authorize(Policy = "AdminOrOperator")]
[Route("api/despesas")]
public sealed class DespesasController : ApiControllerBase
{
    private readonly IExpenseService _service;

    public DespesasController(IExpenseService service)
    {
        _service = service;
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenseTypeDto>>>> Types(CancellationToken cancellationToken)
    {
        var data = await _service.ListTypesAsync(cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("tipos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<ExpenseTypeDto>>> CreateType([FromBody] ExpenseTypeCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateTypeAsync(request, cancellationToken);
        return OkResponse(data);
    }

    [HttpPut("tipos/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<ExpenseTypeDto>>> UpdateType(Guid id, [FromBody] ExpenseTypeUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateTypeAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Query([FromQuery] Guid? propertyId, [FromQuery] Guid? expenseTypeId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _service.QueryAsync(new ExpenseQueryRequest(propertyId, expenseTypeId, status, page, pageSize), cancellationToken);
        return OkResponse<object>(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Create([FromBody] ExpenseCreateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<ExpenseDto>.Ok(data, RequestId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Update(Guid id, [FromBody] ExpenseUpdateRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpPatch("{id:guid}/pagar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> MarkPaid(Guid id, [FromBody] ExpenseMarkPaidRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.MarkPaidAsync(id, request, cancellationToken);
        return data is null ? NotFoundResponse() : OkResponse(data);
    }

    [HttpGet("atrasadas")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenseDto>>>> Overdue(CancellationToken cancellationToken)
    {
        var data = await _service.GetOverdueAsync(cancellationToken);
        return OkResponse(data);
    }
}
