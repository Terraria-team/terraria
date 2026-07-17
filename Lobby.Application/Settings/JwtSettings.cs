namespace Lobby.Application.Settings;


public class JwtSettings
{
    public const string SettingsName = "Authentication:Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int RefreshTokenExpiryDays { get; set; }
    
}