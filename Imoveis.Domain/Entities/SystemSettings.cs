using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class SystemSettings : BaseEntity
{
    public string BrandName { get; set; } = "Imoveis Hub";
    public string BrandShortName { get; set; } = "IH";
    public string ThemePreset { get; set; } = "AURORA_LIGHT";
    public string PrimaryColor { get; set; } = "#1176EE";
    public string SecondaryColor { get; set; } = "#0A58BA";
    public string AccentColor { get; set; } = "#06B6D4";
    public bool EnableAnimations { get; set; } = true;
    public bool EnableGuidedFlows { get; set; }
}
