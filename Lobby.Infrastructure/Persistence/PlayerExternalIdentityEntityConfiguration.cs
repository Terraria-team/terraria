using Lobby.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Persistence;

public class PlayerExternalIdentityEntityConfiguration : IEntityTypeConfiguration<PlayerExternalIdentityEntity>
{
    public void Configure(EntityTypeBuilder<PlayerExternalIdentityEntity> builder)
    {
        builder.ToTable("player_external_identities");

        builder.HasKey(pp => new { pp.PlayerId, pp.Provider });

        builder.Property(pp => pp.PlayerId)
            .HasColumnName("player_id");

        builder.Property(g => g.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50);

        builder.Property(g => g.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(128);

        builder.HasIndex(g => new { g.Provider, g.ExternalId })
            .IsUnique();

        builder.HasOne(g => g.Player)
            .WithMany(p => p.ExternalIdentities)
            .HasForeignKey(g => g.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
