using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILearningProgressService _progressService;
    private readonly INotificationService _notificationService;
    private readonly ExternalOAuthConfig _oauthConfig;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILearningProgressService progressService,
        INotificationService notificationService,
        ExternalOAuthConfig? oauthConfig = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _progressService = progressService;
        _notificationService = notificationService;
        _oauthConfig = oauthConfig ?? new ExternalOAuthConfig();
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Profile");
        }
        return View(new RegisterViewModel());
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        bool isEn = HttpContext.IsEnglish();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", isEn ? "This email address is already in use." : "Địa chỉ email này đã được sử dụng.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true,
            ExperiencePoints = 50, // Welcome gift XP
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Student");
            await _signInManager.SignInAsync(user, isPersistent: true);

            // Send welcoming notification to newly registered student
            try
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    isEn ? "Welcome to NET-Tutos! 🚀" : "Chào mừng bạn đến với NET-Tutos! 🚀",
                    isEn
                        ? $"Hi {user.FullName}, your learner account is activated with +50 XP bonus! Start your journey by exploring the tutorials."
                        : $"Xin chào {user.FullName}, tài khoản học viên của bạn đã sẵn sàng cùng phần thưởng +50 XP khởi đầu! Hãy bắt đầu khám phá ngay.",
                    NotificationType.BonusXpAwarded,
                    "/Tutorials",
                    "bi-rocket-takeoff-fill",
                    "primary");
            }
            catch
            {
                // Non-critical fallback
            }

            TempData["SuccessMessage"] = isEn
                ? "Account registered successfully! Welcome to NET-Tutos."
                : "Tạo tài khoản học viên thành công! Chào mừng bạn gia nhập cộng đồng NET-Tutos.";

            return RedirectToAction("Profile");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Profile");
        }
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        bool isEn = HttpContext.IsEnglish();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, isEn ? "Incorrect email or password." : "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = isEn
                ? $"Welcome back, {user.FullName}!"
                : $"Chào mừng {user.FullName} quay trở lại học tập!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Profile");
        }

        ModelState.AddModelError(string.Empty, isEn ? "Incorrect email or password." : "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    // GET: /Account/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Profile");
        }
        return View(new ForgotPasswordViewModel());
    }

    // POST: /Account/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        bool isEn = HttpContext.IsEnglish();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);
            ViewBag.ResetLink = resetLink;
            ViewBag.UserFound = true;
        }
        else
        {
            ViewBag.UserFound = false;
        }

        ViewBag.SubmittedEmail = model.Email;
        return View("ForgotPasswordConfirmation");
    }

    // GET: /Account/ResetPassword
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login");
        }
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    // POST: /Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        bool isEn = HttpContext.IsEnglish();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = isEn
                ? "Password has been reset successfully. Please log in with your new password."
                : "Mật khẩu đã được đặt lại thành công! Bạn có thể đăng nhập ngay với mật khẩu mới.";
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Profile (Student Dashboard)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }

        var viewModel = await _progressService.GetStudentProfileAsync(userId);
        if (viewModel.User != null)
        {
            var logins = await _userManager.GetLoginsAsync(viewModel.User);
            viewModel.LinkedExternalProviders = logins.Select(l => l.LoginProvider).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        return View(viewModel);
    }

    // POST: /Account/ToggleLesson
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLesson(int tutorialId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Chưa đăng nhập" });
        }

        int targetId = tutorialId;
        if (targetId <= 0)
        {
            if (Request.HasFormContentType && int.TryParse(Request.Form["tutorialId"], out int fId))
            {
                targetId = fId;
            }
            else if (int.TryParse(Request.Query["tutorialId"], out int qId))
            {
                targetId = qId;
            }
        }

        if (targetId <= 0)
        {
            return Json(new { success = false, message = "Mã bài học không hợp lệ." });
        }

        bool isCompleted = await _progressService.ToggleLessonCompletedAsync(userId, targetId);
        return Json(new { success = true, isCompleted = isCompleted });
    }

    // POST: /Account/ExternalLogin
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var normalizedProvider = NormalizeProviderName(provider);
        if (normalizedProvider == null)
        {
            return RedirectToAction("Login", new { returnUrl });
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(normalizedProvider, redirectUrl);
        return Challenge(properties, normalizedProvider);
    }

    // GET: /Account/ExternalLoginCallback
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        bool isEn = HttpContext.IsEnglish();
        returnUrl ??= Url.Content("~/Account/Profile");

        if (!string.IsNullOrEmpty(remoteError))
        {
            TempData["ErrorMessage"] = isEn
                ? $"External OAuth provider error: {remoteError}"
                : $"Lỗi xác thực từ nhà cung cấp OAuth: {remoteError}";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = isEn
                ? "Unable to load external login information."
                : "Không thể tải thông tin xác thực từ nhà cung cấp OAuth.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var provider = NormalizeProviderName(info.LoginProvider) ?? info.LoginProvider;
        var providerKey = info.ProviderKey;
        var email = info.Principal.FindFirstValue(ClaimTypes.Email)
            ?? info.Principal.FindFirstValue("urn:github:login") + "@users.noreply.github.com";
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name)
            ?? info.Principal.FindFirstValue("urn:github:login")
            ?? email.Split('@')[0];
        var avatarUrl = info.Principal.FindFirstValue("urn:oauth:avatar");

        return await ProvisionAndSignInExternalUserAsync(provider, providerKey, email, fullName, avatarUrl, returnUrl);
    }

    private async Task<IActionResult> ProvisionAndSignInExternalUserAsync(
        string provider,
        string providerKey,
        string email,
        string fullName,
        string? avatarUrl,
        string? returnUrl)
    {
        bool isEn = HttpContext.IsEnglish();

        // 1. Check if this external login is already linked to a user
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            provider,
            providerKey,
            isPersistent: true,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var existingLinkedUser = await _userManager.FindByLoginAsync(provider, providerKey);
            TempData["SuccessMessage"] = isEn
                ? $"Signed in via {provider} as {existingLinkedUser?.FullName ?? fullName}!"
                : $"Đăng nhập qua {provider} thành công! Chào mừng {existingLinkedUser?.FullName ?? fullName} quay trở lại.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Profile));
        }

        // 2. Find existing user by email or create a new ApplicationUser
        var user = await _userManager.FindByEmailAsync(email);
        bool isNewUser = false;

        if (user == null)
        {
            isNewUser = true;
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                AvatarUrl = avatarUrl,
                Bio = provider.Equals("GitHub", StringComparison.OrdinalIgnoreCase)
                    ? "Full-Stack C# & .NET 10 Developer connected via GitHub OAuth."
                    : "Modern .NET 10 Learner connected via Google SSO.",
                ExperiencePoints = 50, // +50 XP welcome bonus
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            await _userManager.AddToRoleAsync(user, "Student");
        }
        else if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
            await _userManager.UpdateAsync(user);
        }

        // 3. Link external login in AspNetUserLogins if not already linked
        var currentLogins = await _userManager.GetLoginsAsync(user);
        if (!currentLogins.Any(l => l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        }

        // 4. Sign in user
        await _signInManager.SignInAsync(user, isPersistent: true);

        if (isNewUser)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    isEn ? $"Welcome via {provider} SSO! 🚀" : $"Chào mừng bạn đăng nhập qua {provider} SSO! 🚀",
                    isEn
                        ? $"Hi {user.FullName}, your {provider} account is linked and you received +50 XP bonus!"
                        : $"Xin chào {user.FullName}, tài khoản {provider} của bạn đã được kết nối thành công kèm +50 XP khởi đầu!",
                    NotificationType.BonusXpAwarded,
                    "/Tutorials",
                    provider.Equals("GitHub", StringComparison.OrdinalIgnoreCase) ? "bi-github" : "bi-google",
                    "primary");
            }
            catch
            {
                // Ignore non-critical notification error
            }
        }

        TempData["SuccessMessage"] = isNewUser
            ? (isEn
                ? $"Account created & linked with {provider} (+50 XP bonus)! Welcome, {user.FullName}!"
                : $"Kết nối {provider} thành công (+50 XP khởi đầu)! Chào mừng {user.FullName} gia nhập NET-Tutos.")
            : (isEn
                ? $"Signed in via {provider}! Welcome back, {user.FullName}!"
                : $"Đăng nhập qua {provider} thành công! Chào mừng {user.FullName} quay trở lại.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Profile));
    }

    private static string? NormalizeProviderName(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider)) return null;
        if (provider.Equals("GitHub", StringComparison.OrdinalIgnoreCase)) return "GitHub";
        if (provider.Equals("Google", StringComparison.OrdinalIgnoreCase)) return "Google";
        return null;
    }
}
