using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Infrastructure.Persistence.Configurations;

public class DailyCheckInConfiguration : IEntityTypeConfiguration<DailyCheckIn>
{
    public void Configure(EntityTypeBuilder<DailyCheckIn> builder)
    {
        builder.ToTable("DailyCheckIns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.UserId).IsRequired().HasMaxLength(128);
        builder.HasIndex(c => new { c.UserId, c.CheckInDate });
    }
}
