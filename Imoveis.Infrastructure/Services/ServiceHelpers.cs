using Imoveis.Application.Common;

namespace Imoveis.Infrastructure.Services;

internal static class ServiceHelpers
{
    public static TEnum ParseEnum<TEnum>(string? value, string fieldName) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            throw new AppException($"Invalid value for {fieldName}.", 400, "validation_error");
        }

        return parsed;
    }

    public static int NormalizePage(int page) => page <= 0 ? 1 : page;

    public static int NormalizePageSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 200);

    public static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
