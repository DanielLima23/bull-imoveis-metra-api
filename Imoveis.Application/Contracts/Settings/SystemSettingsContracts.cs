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
    DateTime UpdatedAtUtc)
{
    public bool EnableGuidedFlows { get; init; }
}

public sealed record SystemSettingsUpdateRequest(
    string BrandName,
    string BrandShortName,
    string ThemePreset,
    bool EnableAnimations)
{
    public bool EnableGuidedFlows { get; init; }
}
