using Microsoft.EntityFrameworkCore;
using NET_Tutos.Gamification.Application.Common.Interfaces;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Infrastructure.Data;

public class GamificationDbContext : DbContext, IGamificationDbContext
{
    public GamificationDbContext(DbContextOptions<GamificationDbContext> options) : base(options)
    {
    }

    public DbSet<UserStreak> Streaks => Set<UserStreak>();
    public DbSet<DailyCheckIn> DailyCheckIns => Set<DailyCheckIn>();
    public DbSet<DailyCheckIn> CheckIns => DailyCheckIns;
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<UserXpRecord> UserXpRecords => Set<UserXpRecord>();
    public DbSet<UserXpRecord> XpRecords => UserXpRecords;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations defined in Configurations/
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GamificationDbContext).Assembly);

        // Seed default system achievement badges
        modelBuilder.Entity<Badge>().HasData(
            new Badge { Id = 1, Code = "streak-3", Name = "Khởi đầu kiên trì", Description = "Duy trì chuỗi học 3 ngày liên tiếp", IconClass = "bi-fire", BadgeColor = "warning", RequiredCount = 3 },
            new Badge { Id = 2, Code = "streak-7", Name = "Chiến binh 1 tuần", Description = "Duy trì chuỗi học 7 ngày liên tiếp", IconClass = "bi-lightning-charge-fill", BadgeColor = "danger", RequiredCount = 7 },
            new Badge { Id = 3, Code = "streak-14", Name = "Bất khả chiến bại", Description = "Duy trì chuỗi học 14 ngày liên tiếp", IconClass = "bi-trophy-fill", BadgeColor = "primary", RequiredCount = 14 },
            new Badge { Id = 4, Code = "streak-30", Name = "Huyền thoại .NET", Description = "Duy trì chuỗi học 30 ngày liên tiếp", IconClass = "bi-star-fill", BadgeColor = "success", RequiredCount = 30 },
            new Badge { Id = 5, Code = "code-master", Name = "Bậc thầy Thuật toán", Description = "Giải thành công 10 thử thách lập trình", IconClass = "bi-terminal-fill", BadgeColor = "info", RequiredCount = 10 },
            new Badge { Id = 6, Code = "scholar", Name = "Học giả .NET", Description = "Hoàn thành 20 bài học giáo trình", IconClass = "bi-book-fill", BadgeColor = "secondary", RequiredCount = 20 }
        );
    }
}
