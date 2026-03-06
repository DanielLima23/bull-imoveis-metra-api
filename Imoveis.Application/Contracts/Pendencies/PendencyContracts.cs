namespace Imoveis.Application.Contracts.Pendencies;

public sealed record PendencyTypeCreateRequest(string Name, int DefaultSlaDays);

public sealed record PendencyTypeUpdateRequest(string Name, int DefaultSlaDays);

public sealed record PendencyTypeDto(Guid Id, string Name, int DefaultSlaDays);

public sealed record PendencyQueryRequest(Guid? PropertyId, string? Status, int Page = 1, int PageSize = 20);

public sealed record PendencyCreateRequest(Guid PropertyId, Guid PendencyTypeId, string Title, string? Description, DateTime DueAtUtc);

public sealed record PendencyUpdateRequest(string Title, string? Description, DateTime DueAtUtc);

public sealed record PendencyResolveRequest(DateTime? ResolvedAtUtc);

public sealed record PendencyDto(
    Guid Id,
    Guid PropertyId,
    Guid PendencyTypeId,
    string PropertyTitle,
    string PendencyTypeName,
    string Title,
    string? Description,
    DateTime OpenedAtUtc,
    DateTime DueAtUtc,
    DateTime? ResolvedAtUtc,
    string Status,
    string Severity,
    int SlaDays,
    int ElapsedDays,
    DateTime CreatedAtUtc);
