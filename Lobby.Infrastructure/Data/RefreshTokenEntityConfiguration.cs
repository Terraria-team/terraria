using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Data;

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id)
            .HasColumnName("id");

        builder.Property(rt => rt.PlayerId)
            .HasColumnName("player_id");

        builder.Property(rt => rt.IsRevoked)
            .HasColumnName("is_revoked");

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(rt => rt.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45);

        builder.Property(rt => rt.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255);

        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        
        builder.HasOne(rt => rt.Player)
            .WithMany(p => p.RefreshTokens)
            .HasForeignKey(rt => rt.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        
    }
}