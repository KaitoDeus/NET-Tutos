using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Infrastructure.Persistence.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("UserBadges");
        builder.HasKey(ub => ub.Id);
        builder.Property(ub => ub.UserId).IsRequired().HasMaxLength(128);
        builder.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();
        builder.HasOne(ub => ub.Badge).WithMany().HasForeignKey(ub => ub.BadgeId).OnDelete(DeleteBehavior.Cascade);
    }
}
