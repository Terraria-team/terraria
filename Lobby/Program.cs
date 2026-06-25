using System.Runtime.InteropServices;
using Docker.DotNet;
using Lobby.Application.Repositories;
using Lobby.Application.Services;
using Lobby.Data;
using Lobby.Repositories;
using Lobby.Services;
using Lobby.Settings;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IServerInstanceService, ServerInstanceService>();
builder.Services.AddScoped<IServerInstanceRepository, EfServerInstanceRepository>();
builder.Services.AddScoped<IServerInstanseManagementService, DockerServerInstanceService>();

builder.Services.Configure<DockerServerSettings>(builder.Configuration.GetSection(DockerServerSettings.SettingsName));

builder.Services.AddDbContext<LobbyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LobbyDb"))
);

builder.Services.AddSingleton<IDockerClient>(provider =>
{
    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    Uri dockerUri = isWindows 
        ? new Uri("npipe://./pipe/docker_engine") 
        : new Uri("unix:///var/run/docker.sock");

    return new DockerClientConfiguration(dockerUri).CreateClient();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LobbyDbContext>();
    await db.Database.MigrateAsync();
}

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
