using System.Globalization;
using System.Text;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;

namespace Imoveis.Application.Common;

public static class PropertyStatusContract
{
    public const string Disponivel = "disponível";
    public const string Alugado = "alugado";
    public const string Inativo = "inativo";
    public const string A_Venda = "à venda";
    public const string Demandas = "demandas";
    public const string Ocioso = "ocioso";

    public const string Reforma = "reforma";
    public const string Rescisao = "rescisão";
    public const string PendenciaJuridica = "pendência jurídica";

    public static readonly IReadOnlyList<string> StatusValues =
    [
        Disponivel,
        Alugado,
        Inativo,
        A_Venda,
        Demandas,
        Ocioso
    ];

    public static readonly IReadOnlyList<string> IdleReasonValues =
    [
        Reforma,
        Rescisao,
        PendenciaJuridica
    ];

    public static void Apply(Property property, string status, string? motivoOciosidade)
    {
        var canonicalStatus = ParseStatus(status);
        var canonicalReason = ParseIdleReason(motivoOciosidade);

        if (canonicalStatus == Ocioso && canonicalReason is null)
        {
            throw new AppException("motivoOciosidade is required when status is 'ocioso'.", 400, "validation_error");
        }

        if (canonicalStatus != Ocioso && canonicalReason is not null)
        {
            throw new AppException("motivoOciosidade must be null unless status is 'ocioso'.", 400, "validation_error");
        }

        property.IdleReason = canonicalStatus == Ocioso ? canonicalReason : null;

        switch (canonicalStatus)
        {
            case Disponivel:
                property.OccupancyStatus = PropertyOccupancyStatus.VACANT;
                property.AssetState = PropertyAssetState.READY;
                break;
            case Alugado:
                property.OccupancyStatus = PropertyOccupancyStatus.OCCUPIED;
                property.AssetState = PropertyAssetState.READY;
                break;
            case Inativo:
                property.OccupancyStatus = PropertyOccupancyStatus.VACANT;
                property.AssetState = PropertyAssetState.NEW;
                break;
            case A_Venda:
                property.OccupancyStatus = PropertyOccupancyStatus.VACANT;
                property.AssetState = PropertyAssetState.FOR_SALE;
                break;
            case Demandas:
                property.OccupancyStatus = PropertyOccupancyStatus.VACANT;
                property.AssetState = PropertyAssetState.CONSTRUCTION;
                break;
            case Ocioso:
                property.OccupancyStatus = PropertyOccupancyStatus.VACANT;
                property.AssetState = canonicalReason switch
                {
                    Reforma => PropertyAssetState.RENOVATION,
                    Rescisao => PropertyAssetState.PREPARATION,
                    PendenciaJuridica => PropertyAssetState.CONSTRUCTION,
                    _ => PropertyAssetState.READY
                };
                break;
        }
    }

    public static string GetStatus(Property property)
    {
        if (property.OccupancyStatus == PropertyOccupancyStatus.OCCUPIED)
        {
            return Alugado;
        }

        var idleReason = ParseIdleReasonOrNull(property.IdleReason);
        if (idleReason is not null)
        {
            return Ocioso;
        }

        return property.AssetState switch
        {
            PropertyAssetState.FOR_SALE => A_Venda,
            PropertyAssetState.NEW => Inativo,
            PropertyAssetState.CONSTRUCTION or PropertyAssetState.PREPARATION or PropertyAssetState.RENOVATION => Demandas,
            _ => Disponivel
        };
    }

    public static string? GetIdleReason(Property property)
        => GetStatus(property) == Ocioso
            ? ParseIdleReasonOrNull(property.IdleReason)
            : null;

    public static string ParseStatus(string value)
    {
        var normalized = NormalizeKey(value);
        return normalized switch
        {
            "disponivel" => Disponivel,
            "alugado" => Alugado,
            "inativo" => Inativo,
            "a venda" => A_Venda,
            "demandas" => Demandas,
            "ocioso" => Ocioso,
            _ => throw new AppException("Invalid property status.", 400, "validation_error")
        };
    }

    public static string? ParseIdleReason(string? value)
    {
        var parsed = ParseIdleReasonOrNull(value);
        if (value is null || parsed is not null)
        {
            return parsed;
        }

        throw new AppException("Invalid motivoOciosidade.", 400, "validation_error");
    }

    public static string? TryMapLegacyStatus(string? occupancyStatus, string? assetState)
    {
        var occupancy = NormalizeKey(occupancyStatus);
        var asset = NormalizeKey(assetState);

        if (occupancy == "occupied")
        {
            return Alugado;
        }

        return asset switch
        {
            "for sale" => A_Venda,
            "new" => Inativo,
            "construction" or "preparation" or "renovation" => Demandas,
            "ready" => Disponivel,
            _ => null
        };
    }

    private static string? ParseIdleReasonOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeKey(value) switch
        {
            "reforma" => Reforma,
            "rescisao" => Rescisao,
            "pendencia juridica" => PendenciaJuridica,
            _ => null
        };
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Replace('_', ' ')
            .Replace('-', ' ');
    }
}
