using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class SystemSettings : BaseEntity
{
    public string BrandName { get; set; } = "Imoveis Hub";
    public string BrandShortName { get; set; } = "IH";
    public string ThemePreset { get; set; } = "SAND_LIGHT";
    public string PrimaryColor { get; set; } = "#8F6A3A";
    public string SecondaryColor { get; set; } = "#5E4525";
    public string AccentColor { get; set; } = "#C69A5D";
    public bool EnableAnimations { get; set; } = true;
    public bool EnableGuidedFlows { get; set; }
}
