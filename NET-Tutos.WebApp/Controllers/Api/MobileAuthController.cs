using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.DTOs.Mobile;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/mobile/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMobileAuthService _mobileAuthService;
    private readonly AppDbContext _context;

    public MobileAuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMobileAuthService mobileAuthService,
        AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mobileAuthService = mobileAuthService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = "Tên đăng nhập hoặc mật khẩu không được để trống."
            });
        }

        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail) 
                   ?? await _userManager.FindByNameAsync(request.UsernameOrEmail);

        if (user == null)
        {
            return Unauthorized(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = "Tài khoản hoặc mật khẩu không chính xác."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = "Tài khoản hoặc mật khẩu không chính xác."
            });
        }

        var token = _mobileAuthService.GenerateToken(user);
        var completedCount = await _context.UserLessonProgresses
            .CountAsync(p => p.UserId == user.Id && p.IsCompleted);

        var userDto = new MobileUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : (user.UserName ?? "Học viên"),
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            TotalXp = user.ExperiencePoints,
            Level = Math.Max(1, (user.ExperiencePoints / 100) + 1),
            StreakDays = user.CurrentStreak,
            CompletedLessonsCount = completedCount
        };

        return Ok(new MobileAuthResponse
        {
            IsSuccess = true,
            Message = "Đăng nhập thành công!",
            Token = token,
            User = userDto
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] MobileRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || 
            string.IsNullOrWhiteSpace(request.Email) || 
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = "Vui lòng nhập đầy đủ thông tin bắt buộc."
            });
        }

        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return BadRequest(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = "Tên tài khoản này đã được sử dụng."
            });
        }

        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
        {
            return BadRequest(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = "Email này đã được đăng ký tài khoản."
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName : request.Username,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Đăng ký không thành công.";
            return BadRequest(new MobileAuthResponse
            {
                IsSuccess = false,
                Message = firstError
            });
        }

        var token = _mobileAuthService.GenerateToken(user);

        var userDto = new MobileUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = string.Empty,
            TotalXp = 0,
            Level = 1,
            StreakDays = 0,
            CompletedLessonsCount = 0
        };

        return Ok(new MobileAuthResponse
        {
            IsSuccess = true,
            Message = "Đăng ký tài khoản thành công!",
            Token = token,
            User = userDto
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập." });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var (isValid, userId) = _mobileAuthService.ValidateToken(token);
        if (!isValid || string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var completedCount = await _context.UserLessonProgresses
            .CountAsync(p => p.UserId == user.Id && p.IsCompleted);

        var userDto = new MobileUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : (user.UserName ?? "Học viên"),
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            TotalXp = user.ExperiencePoints,
            Level = Math.Max(1, (user.ExperiencePoints / 100) + 1),
            StreakDays = user.CurrentStreak,
            CompletedLessonsCount = completedCount
        };

        return Ok(userDto);
    }
}
