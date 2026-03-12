using Imoveis.Domain.Enums;

namespace Imoveis.Application.Common;

public static class PartyKindContract
{
    public static readonly IReadOnlyList<string> KindValues =
    [
        nameof(PartyKind.PROPRIETARIO),
        nameof(PartyKind.ADMINISTRADOR),
        nameof(PartyKind.FIADOR),
        nameof(PartyKind.ADVOGADO),
        nameof(PartyKind.CORRETOR),
        nameof(PartyKind.SINDICO),
        nameof(PartyKind.REPRESENTANTE_LEGAL),
        nameof(PartyKind.PRESTADOR_DE_SERVICO),
        nameof(PartyKind.PERSON),
        nameof(PartyKind.COMPANY),
        nameof(PartyKind.OUTRO)
    ];

    public static PartyKind Parse(string? value, string fieldName = "kind")
    {
        if (TryParse(value, out var kind))
        {
            return kind;
        }

        throw new AppException($"Invalid value for {fieldName}.", 400, "validation_error");
    }

    public static PartyKind ParseStoredValue(string? value)
    {
        if (TryParse(value, out var kind))
        {
            return kind;
        }

        return PartyKind.OUTRO;
    }

    public static string GetCode(PartyKind kind) => kind.ToString();

    private static bool TryParse(string? value, out PartyKind kind)
    {
        kind = default;
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        switch (normalized)
        {
            case "PROPRIETARIO":
            case "OWNER":
                kind = PartyKind.PROPRIETARIO;
                return true;

            case "ADMINISTRADOR":
            case "ADMINISTRATOR":
                kind = PartyKind.ADMINISTRADOR;
                return true;

            case "FIADOR":
            case "GUARANTOR":
                kind = PartyKind.FIADOR;
                return true;

            case "ADVOGADO":
            case "LAWYER":
                kind = PartyKind.ADVOGADO;
                return true;

            case "CORRETOR":
            case "BROKER":
                kind = PartyKind.CORRETOR;
                return true;

            case "SINDICO":
            case "SYNDIC":
                kind = PartyKind.SINDICO;
                return true;

            case "REPRESENTANTE_LEGAL":
            case "LEGAL_REPRESENTATIVE":
                kind = PartyKind.REPRESENTANTE_LEGAL;
                return true;

            case "PRESTADOR_DE_SERVICO":
            case "SERVICE_PROVIDER":
            case "PROVIDER":
                kind = PartyKind.PRESTADOR_DE_SERVICO;
                return true;

            case "PERSON":
                kind = PartyKind.PERSON;
                return true;

            case "COMPANY":
                kind = PartyKind.COMPANY;
                return true;

            case "OUTRO":
            case "OTHER":
                kind = PartyKind.OUTRO;
                return true;

            default:
                return false;
        }
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace('-', '_')
            .Replace(' ', '_');
    }
}
