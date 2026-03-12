using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Expenses;

namespace Imoveis.Application.Abstractions.Services;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseTypeDto>> ListTypesAsync(CancellationToken cancellationToken);
    Task<ExpenseTypeDto> CreateTypeAsync(ExpenseTypeCreateRequest request, CancellationToken cancellationToken);
    Task<ExpenseTypeDto?> UpdateTypeAsync(Guid id, ExpenseTypeUpdateRequest request, CancellationToken cancellationToken);

    Task<PagedResult<ExpenseDto>> QueryAsync(ExpenseQueryRequest request, CancellationToken cancellationToken);
    Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ExpenseDto> CreateAsync(ExpenseCreateRequest request, CancellationToken cancellationToken);
    Task<ExpenseDto?> UpdateAsync(Guid id, ExpenseUpdateRequest request, CancellationToken cancellationToken);
    Task<ExpenseDto?> MarkPaidAsync(Guid id, ExpenseMarkPaidRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExpenseDto>> GetOverdueAsync(CancellationToken cancellationToken);
}
