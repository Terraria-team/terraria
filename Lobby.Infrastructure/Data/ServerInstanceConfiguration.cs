using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Data;

public class ServerInstanceConfiguration : IEntityTypeConfiguration<ServerInstanceEntity>
{
    public void Configure(EntityTypeBuilder<ServerInstanceEntity> builder)
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

        builder.Property(en => en.PlayerCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(en => en.EmptySince);
        builder.Property(en => en.PendingSince);
        builder.Property(en => en.CreatedAt).IsRequired();
        builder.Property(en => en.UpdatedAt);

        builder.HasIndex(en => en.Port).IsUnique();
        builder.HasIndex(en => en.Status);
        builder.HasIndex(en => en.EmptySince);

        builder.Property(en => en.WorldId).IsRequired();
        
        builder.HasOne(en => en.World)
            .WithMany(tw => tw.ServerInstances)
            .HasForeignKey(en => en.WorldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}