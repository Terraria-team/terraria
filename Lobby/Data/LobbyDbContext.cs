using System.Reflection;
using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Data;

public class LobbyDbContext : DbContext
{
    public DbSet<ServerInstance> ServerInstances { get; set; }

    public LobbyDbContext(DbContextOptions<LobbyDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}