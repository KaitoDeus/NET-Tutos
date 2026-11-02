using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IDeveloperPortfolioService
{
    Task<PublicPortfolioViewModel?> GetPortfolioByUsernameAsync(string username, string? currentVisitorUserId, bool isEnglish);
    Task<DeveloperProfile> GetOrCreateProfileAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, UpdateDeveloperProfileRequest request);
}
