using NET_Tutos.Gamification.Application.Common.Interfaces;

namespace NET_Tutos.Gamification.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
