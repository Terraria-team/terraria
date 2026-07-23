using Lobby.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Persistence;

public class TerrariaWorldStorageEntityConfiguration : IEntityTypeConfiguration<TerrariaWorldStorageEntity>
{
    public void Configure(EntityTypeBuilder<TerrariaWorldStorageEntity> builder)
    {
        builder.ToTable("terraria_world_storages");

        builder.HasKey(en => en.Id);

        builder.Property(en => en.AbsolutePathOnTheDisk)
            .IsRequired()
            .HasMaxLength(1000);
    }
}