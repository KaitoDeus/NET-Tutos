using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class LanguageController : Controller
{
    [HttpPost]
    [HttpGet]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(culture) || (culture != "vi" && culture != "en"))
        {
            culture = "vi";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = false
            }
        );

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
