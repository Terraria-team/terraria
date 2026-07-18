using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Data;

public class PlayerGoogleLoginEntityConfiguration : IEntityTypeConfiguration<PlayerGoogleLoginEntity>
{
    public void Configure(EntityTypeBuilder<PlayerGoogleLoginEntity> builder)
    {
        builder.ToTable("player_google_logins");

        builder.HasKey(pp => pp.PlayerId);
        builder.Property(pp => pp.PlayerId)
            .HasColumnName("player_id");

        builder.Property(g => g.GoogleId)
            .HasColumnName("google_login")
            .HasMaxLength(21);

        builder.HasIndex(g => g.GoogleId)
            .IsUnique();

        builder.HasOne(g => g.Player)
            .WithOne(p => p.GoogleLogin)
            .HasForeignKey<PlayerGoogleLoginEntity>(g => g.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}