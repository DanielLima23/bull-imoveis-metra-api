namespace Imoveis.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Imoveis.Api";
    public string Audience { get; set; } = "Imoveis.Web";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshExpiryDays { get; set; } = 30;
}
