using Lobby.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lobby.Infrastructure.Data;

public class PlayerEntityConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.ToTable("players");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(255);
        
        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(250);
        
        builder.Property(p => p.Role)
            .HasColumnName("role")
            .HasMaxLength(128);

    }
}