using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Settings;
using Imoveis.Domain.Entities;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class SystemSettingsService : ISystemSettingsService
{
    private static readonly IReadOnlyDictionary<string, ThemePalette> ThemePresets =
        new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["AURORA_LIGHT"] = new("#1176EE", "#0A58BA", "#06B6D4"),
            ["EMERALD_LIGHT"] = new("#0F766E", "#115E59", "#22C55E"),
            ["MIDNIGHT_DARK"] = new("#38BDF8", "#0EA5E9", "#22D3EE"),
            ["GRAPHITE_DARK"] = new("#8B5CF6", "#6366F1", "#14B8A6")
        };

    private readonly AppDbContext _dbContext;

    public SystemSettingsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemSettingsDto> GetPublicAsync(CancellationToken cancellationToken)
        => await GetOrCreateDtoAsync(cancellationToken);

    public async Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken)
        => await GetOrCreateDtoAsync(cancellationToken);

    public async Task<SystemSettingsDto> UpdateAsync(SystemSettingsUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await GetOrCreateEntityAsync(cancellationToken);
        var themePreset = NormalizeThemePreset(request.ThemePreset);
        var palette = ThemePresets[themePreset];

        entity.BrandName = NormalizeBrandName(request.BrandName);
        entity.BrandShortName = NormalizeBrandShortName(request.BrandShortName);
        entity.ThemePreset = themePreset;
        entity.PrimaryColor = palette.PrimaryColor;
        entity.SecondaryColor = palette.SecondaryColor;
        entity.AccentColor = palette.AccentColor;
        entity.EnableAnimations = request.EnableAnimations;
        entity.EnableGuidedFlows = request.EnableGuidedFlows;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    private async Task<SystemSettingsDto> GetOrCreateDtoAsync(CancellationToken cancellationToken)
    {
        var entity = await GetOrCreateEntityAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task<SystemSettings> GetOrCreateEntityAsync(CancellationToken cancellationToken)
    {
        var entity = await _dbContext.SystemSettings
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is not null)
        {
            var changed = EnsureThemePreset(entity);
            if (changed)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return entity;
        }

        entity = new SystemSettings();
        EnsureThemePreset(entity);
        _dbContext.SystemSettings.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    private static bool EnsureThemePreset(SystemSettings entity)
    {
        var themePreset = NormalizeThemePresetOrDefault(entity.ThemePreset);
        var palette = ThemePresets[themePreset];
        var changed = false;

        if (!string.Equals(entity.ThemePreset, themePreset, StringComparison.Ordinal))
        {
            entity.ThemePreset = themePreset;
            changed = true;
        }

        if (!string.Equals(entity.PrimaryColor, palette.PrimaryColor, StringComparison.Ordinal))
        {
            entity.PrimaryColor = palette.PrimaryColor;
            changed = true;
        }

        if (!string.Equals(entity.SecondaryColor, palette.SecondaryColor, StringComparison.Ordinal))
        {
            entity.SecondaryColor = palette.SecondaryColor;
            changed = true;
        }

        if (!string.Equals(entity.AccentColor, palette.AccentColor, StringComparison.Ordinal))
        {
            entity.AccentColor = palette.AccentColor;
            changed = true;
        }

        return changed;
    }

    private static string NormalizeBrandName(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AppException("BrandName is required.", 400, "validation_error");
        }

        return normalized;
    }

    private static string NormalizeBrandShortName(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AppException("BrandShortName is required.", 400, "validation_error");
        }

        if (normalized.Length > 8)
        {
            throw new AppException("BrandShortName must have up to 8 characters.", 400, "validation_error");
        }

        return normalized.ToUpperInvariant();
    }

    private static string NormalizeThemePreset(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ThemePresets.ContainsKey(normalized))
        {
            throw new AppException("ThemePreset is invalid.", 400, "validation_error");
        }

        return normalized;
    }

    private static string NormalizeThemePresetOrDefault(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return ThemePresets.ContainsKey(normalized) ? normalized : "AURORA_LIGHT";
    }

    private static SystemSettingsDto ToDto(SystemSettings entity)
        => new(
            entity.Id,
            entity.BrandName,
            entity.BrandShortName,
            entity.ThemePreset,
            entity.PrimaryColor,
            entity.SecondaryColor,
            entity.AccentColor,
            entity.EnableAnimations,
            entity.UpdatedAtUtc ?? entity.CreatedAtUtc)
        {
            EnableGuidedFlows = entity.EnableGuidedFlows
        };

    private sealed record ThemePalette(string PrimaryColor, string SecondaryColor, string AccentColor);
}
