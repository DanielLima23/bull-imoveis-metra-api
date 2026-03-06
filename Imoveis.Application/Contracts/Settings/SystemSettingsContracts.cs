namespace Imoveis.Application.Contracts.Settings;

public sealed record SystemSettingsDto(
    Guid Id,
    string BrandName,
    string BrandShortName,
    string ThemePreset,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    bool EnableAnimations,
    DateTime UpdatedAtUtc);

public sealed record SystemSettingsUpdateRequest(
    string BrandName,
    string BrandShortName,
    string ThemePreset,
    bool EnableAnimations);
