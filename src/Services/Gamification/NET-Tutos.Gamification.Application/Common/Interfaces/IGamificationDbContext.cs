using Microsoft.EntityFrameworkCore;
using NET_Tutos.Gamification.Domain.Entities;

namespace NET_Tutos.Gamification.Application.Common.Interfaces;

public interface IGamificationDbContext
{
    DbSet<UserStreak> Streaks { get; }
    DbSet<DailyCheckIn> CheckIns { get; }
    DbSet<Badge> Badges { get; }
    DbSet<UserBadge> UserBadges { get; }
    DbSet<UserXpRecord> XpRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
