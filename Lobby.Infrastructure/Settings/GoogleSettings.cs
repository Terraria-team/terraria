namespace Lobby.Infrastructure.Settings;

public class GoogleSettings
{
    public const string SettingsName = "Authentication:Google";
    public string ClientId { get; set; } = string.Empty;
    // Store via dotnet user-secrets or env var AUTHENTICATION__GOOGLE__CLIENTSECRET — never in appsettings.json
    public string ClientSecret { get; set; } = string.Empty;
}