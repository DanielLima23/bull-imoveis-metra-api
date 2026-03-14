using Imoveis.Application.Common;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

internal static class LeaseBusinessRules
{
    internal static async Task EnsurePropertyStatusChangeAllowedAsync(
        AppDbContext dbContext,
        Guid propertyId,
        string targetStatus,
        CancellationToken cancellationToken)
    {
        var canonicalStatus = PropertyStatusContract.ParseStatus(targetStatus);
        var activeLease = await FindActiveLeaseAsync(dbContext, propertyId, null, cancellationToken);

        if (canonicalStatus == PropertyStatusContract.Alugado)
        {
            if (activeLease is not null)
            {
                return;
            }

            throw new AppException(
                "O imovel so pode ser marcado como alugado quando existir uma locacao ativa vinculada.",
                409,
                PropertyLeaseErrorCodes.PropertyRequiresActiveLease);
        }

        if (activeLease is not null)
        {
            throw new AppException(
                "Nao e possivel alterar manualmente o status do imovel enquanto existir uma locacao ativa vinculada.",
                409,
                PropertyLeaseErrorCodes.PropertyHasActiveLease);
        }
    }

    internal static async Task EnsureSingleActiveLeaseAsync(
        AppDbContext dbContext,
        Guid propertyId,
        Guid? ignoreLeaseId,
        CancellationToken cancellationToken)
    {
        var activeLease = await FindActiveLeaseAsync(dbContext, propertyId, ignoreLeaseId, cancellationToken);
        if (activeLease is null)
        {
            return;
        }

        throw new AppException(
            "Este imovel ja possui uma locacao ativa vinculada.",
            409,
            PropertyLeaseErrorCodes.PropertyAlreadyHasActiveLease);
    }

    internal static async Task<Guid?> GetActiveLeaseIdAsync(
        AppDbContext dbContext,
        Guid propertyId,
        Guid? ignoreLeaseId,
        CancellationToken cancellationToken)
        => (await FindActiveLeaseAsync(dbContext, propertyId, ignoreLeaseId, cancellationToken))?.Id;

    private static Task<ActiveLeaseSnapshot?> FindActiveLeaseAsync(
        AppDbContext dbContext,
        Guid propertyId,
        Guid? ignoreLeaseId,
        CancellationToken cancellationToken)
        => dbContext.LeaseContracts
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Status == Imoveis.Domain.Enums.LeaseStatus.ACTIVE)
            .Where(x => !ignoreLeaseId.HasValue || x.Id != ignoreLeaseId.Value)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new ActiveLeaseSnapshot(x.Id))
            .FirstOrDefaultAsync(cancellationToken);

    private sealed record ActiveLeaseSnapshot(Guid Id);
}
