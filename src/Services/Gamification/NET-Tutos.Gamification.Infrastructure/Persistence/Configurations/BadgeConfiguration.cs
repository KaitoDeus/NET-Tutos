using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Infrastructure.Persistence.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(64);
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.Name).IsRequired().HasMaxLength(128);
        builder.Property(b => b.Description).HasMaxLength(512);
        builder.Property(b => b.IconClass).HasMaxLength(64);
        builder.Property(b => b.BadgeColor).HasMaxLength(32);
    }
}
