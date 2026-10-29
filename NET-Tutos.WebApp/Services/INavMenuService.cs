using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public interface INavMenuService
{
    List<Category> GetNavCategories();
    string GetSearchDataJson(bool isEn);
    void InvalidateCache();
}
