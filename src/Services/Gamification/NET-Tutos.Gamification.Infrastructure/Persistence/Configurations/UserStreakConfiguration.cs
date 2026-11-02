using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Infrastructure.Persistence.Configurations;

public class UserStreakConfiguration : IEntityTypeConfiguration<UserStreak>
{
    public void Configure(EntityTypeBuilder<UserStreak> builder)
    {
        builder.ToTable("UserStreaks");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserId).IsRequired().HasMaxLength(128);
        builder.HasIndex(s => s.UserId).IsUnique();
        builder.Property(s => s.CurrentStreak).HasDefaultValue(0);
        builder.Property(s => s.LongestStreak).HasDefaultValue(0);
    }
}
