using System.Runtime.InteropServices;
using System.Text;
using Docker.DotNet;
using Lobby.Application.Contracts;
using Lobby.Application.Services;
using Lobby.Application.Settings;
using Lobby.HostedServices;
using Lobby.Infrastructure.Data;
using Lobby.Infrastructure.Repositories;
using Lobby.Infrastructure.Services;
using Lobby.Infrastructure.Settings;
using Lobby.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IServerInstanceService, ServerInstanceService>();
builder.Services.AddScoped<IServerInstanceSpawner, DockerServerInstanceService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// repos
builder.Services.AddScoped<IPlayerGoogleLoginRepository, EfPlayerGoogleLoginRepository>();
builder.Services.AddScoped<IPlayerRepository, EfPlayerRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
builder.Services.AddScoped<IServerInstanceRepository, EfServerInstanceRepository>();

// settings
var dockerSettings = builder.Configuration.GetSection(DockerServerSettings.SettingsName).Get<DockerServerSettings>()!;
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SettingsName).Get<JwtSettings>()!;
var googleSettings = builder.Configuration.GetSection(GoogleSettings.SettingsName).Get<GoogleSettings>()!;
var serverToLobbyAuthSettings = builder.Configuration.GetSection(ServerToLobbyAuthSettings.SettingsName).Get<ServerToLobbyAuthSettings>()!;
var serverInstanceServiceSettings = builder.Configuration.GetSection(ServerInstanceServiceSettings.SettingsName).Get<ServerInstanceServiceSettings>()!;

builder.Services.AddSingleton(dockerSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(googleSettings);
builder.Services.AddSingleton(serverToLobbyAuthSettings);
builder.Services.AddSingleton(serverInstanceServiceSettings);

builder.Services.Configure<ServerInstanceCleanupSettings>(builder.Configuration.GetSection(ServerInstanceCleanupSettings.SettingsName));
builder.Services.Configure<TokenCleanupSettings>(builder.Configuration.GetSection(TokenCleanupSettings.SettingsName));

// db
builder.Services.AddDbContext<LobbyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LobbyDb"))
);

// docker
builder.Services.AddSingleton<IDockerClient>(provider =>
{
    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    Uri dockerUri = isWindows 
        ? new Uri("npipe://./pipe/docker_engine") 
        : new Uri("unix:///var/run/docker.sock");

    return new DockerClientConfiguration(dockerUri).CreateClient();
});

// jwt
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("access_token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});



// hosted services
builder.Services.AddHostedService<ServerInstanceCleanupService>();
builder.Services.AddHostedService<TokenCleanupBackgroundService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy"))
    .WithName("Health")
    .AllowAnonymous();

// db migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LobbyDbContext>();
    await db.Database.MigrateAsync();
}

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.Run();

// Для видимості у WebApplicationFactory<Program> в інтеграційних тестах.
public partial class Program { }
