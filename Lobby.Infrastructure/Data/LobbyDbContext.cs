using System.Reflection;
using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Data;

public class LobbyDbContext : DbContext
{
    public DbSet<ServerInstanceEntity> ServerInstances { get; set; }
    
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    public DbSet<PlayerGoogleLoginEntity> PlayerGoogleLogins { get; set; }

    public LobbyDbContext(DbContextOptions<LobbyDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}