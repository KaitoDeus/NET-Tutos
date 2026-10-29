using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public class NavMenuService : INavMenuService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private const string CategoriesCacheKey = "NavCategories_CacheKey";
    private const string SearchJsonViCacheKey = "SearchDataJson_VI_CacheKey";
    private const string SearchJsonEnCacheKey = "SearchDataJson_EN_CacheKey";

    public NavMenuService(IServiceProvider serviceProvider, IMemoryCache cache)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
    }

    public List<Category> GetNavCategories()
    {
        return _cache.GetOrCreate(CategoriesCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return db.Categories
                .AsNoTracking()
                .OrderBy(c => c.OrderIndex)
                .ToList();
        }) ?? new List<Category>();
    }

    public string GetSearchDataJson(bool isEn)
    {
        string cacheKey = isEn ? SearchJsonEnCacheKey : SearchJsonViCacheKey;
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tutorials = db.Tutorials
                .AsNoTracking()
                .Include(t => t.Category)
                .OrderBy(t => t.OrderIndex)
                .Take(150)
                .ToList();

            return System.Text.Json.JsonSerializer.Serialize(tutorials.Select(t => new
            {
                id = t.Id,
                title = t.GetTitle(isEn),
                slug = t.GetSlug(isEn),
                category = t.Category != null ? t.Category.GetTitle(isEn) : "",
                readingMinutes = t.EstimatedReadingMinutes,
                difficulty = LocalizationHelper.GetDifficultyName(t.Difficulty, isEn)
            }));
        }) ?? "[]";
    }

    public void InvalidateCache()
    {
        _cache.Remove(CategoriesCacheKey);
        _cache.Remove(SearchJsonViCacheKey);
        _cache.Remove(SearchJsonEnCacheKey);
    }
}
