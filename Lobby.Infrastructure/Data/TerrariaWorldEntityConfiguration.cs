using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Data;

public class TerrariaWorldEntityConfiguration : IEntityTypeConfiguration<TerrariaWorldEntity>
{
    public void Configure(EntityTypeBuilder<TerrariaWorldEntity> builder)
    {
        builder.ToTable("terraria_worlds");

        builder.HasKey(en => en.Id);

        builder.Property(en => en.OwnerId)
            .IsRequired();

        builder.Property(en => en.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(en => en.OwnerId);

        builder.HasOne(en => en.Owner)
            .WithMany(player => player.OwnedWorlds)
            .HasForeignKey(en => en.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(en => en.StorageId);
        
        builder.HasOne(en => en.Storage)
            .WithOne(s => s.World)
            .HasForeignKey<TerrariaWorldEntity>(t => t.StorageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}