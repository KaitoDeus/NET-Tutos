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

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILearningProgressService progressService,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _progressService = progressService;
        _notificationService = notificationService;
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

        bool isCompleted = await _progressService.ToggleLessonCompletedAsync(userId, tutorialId);
        return Json(new { success = true, isCompleted = isCompleted });
    }
}
