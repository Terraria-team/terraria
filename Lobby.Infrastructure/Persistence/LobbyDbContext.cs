using System.Reflection;
using Lobby.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Persistence;

public class LobbyDbContext : DbContext
{
    public DbSet<ServerInstanceEntity> ServerInstances { get; set; }
    
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    public DbSet<PlayerExternalIdentityEntity> PlayerExternalIdentities { get; set; }
    public DbSet<TerrariaWorldEntity> TerrariaWorlds { get; set; }
    public DbSet<TerrariaWorldStorageEntity> TerrariaWorldStorages { get; set; }

    public LobbyDbContext(DbContextOptions<LobbyDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
