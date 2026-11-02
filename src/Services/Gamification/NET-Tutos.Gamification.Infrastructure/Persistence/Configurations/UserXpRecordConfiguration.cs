using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Infrastructure.Persistence.Configurations;

public class UserXpRecordConfiguration : IEntityTypeConfiguration<UserXpRecord>
{
    public void Configure(EntityTypeBuilder<UserXpRecord> builder)
    {
        builder.ToTable("UserXpRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.TotalXp).HasDefaultValue(0);
        builder.Property(x => x.CurrentLevel).HasDefaultValue(1);
    }
}
