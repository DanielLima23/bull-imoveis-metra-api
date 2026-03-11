namespace Imoveis.Application.Contracts.Parties;

public sealed record PartyQueryRequest(string? Search, string? Kind, bool? Active, int Page = 1, int PageSize = 20);

public sealed record PartyCreateRequest(string Kind, string Name, string? DocumentNumber, string? Email, string? Phone, string? Notes);

public sealed record PartyUpdateRequest(string Kind, string Name, string? DocumentNumber, string? Email, string? Phone, string? Notes, bool IsActive);

public sealed record PartyDto(
    Guid Id,
    string Kind,
    string Name,
    string? DocumentNumber,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc);
