using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Data;

public class ServerInstanseConfiguration : IEntityTypeConfiguration<ServerInstance>
{
    public void Configure(EntityTypeBuilder<ServerInstance> builder)
    {
        builder.ToTable("server_instances");

        builder.HasKey(en => en.Id);
        
        builder.Property(en => en.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(en => en.ContainerId)
            .HasMaxLength(100);

        builder.Property(en => en.Image)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(en => en.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(en => en.Port)
            .IsUnique();

        builder.HasIndex(en => en.Status);
        builder.HasIndex(en => en.EmptySince);
    }
}