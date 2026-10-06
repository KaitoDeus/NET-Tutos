using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public interface IMobileAuthService
{
    string GenerateToken(ApplicationUser user);
    (bool IsValid, string? UserId) ValidateToken(string token);
}

public class MobileAuthService : IMobileAuthService
{
    private readonly byte[] _secretKey;

    public MobileAuthService(IConfiguration configuration)
    {
        var secret = configuration["MobileAuth:SecretKey"] ?? "NET_Tutos_Secure_Mobile_API_Token_Key_2026_DotNet10_DefaultSecret";
        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    private class TokenPayload
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long ExpiresAt { get; set; }
    }

    public string GenerateToken(ApplicationUser user)
    {
        var payload = new TokenPayload
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
        };

        var json = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadBase64 = Convert.ToBase64String(payloadBytes);

        using var hmac = new HMACSHA256(_secretKey);
        var signatureBytes = hmac.ComputeHash(payloadBytes);
        var signatureBase64 = Convert.ToBase64String(signatureBytes);

        return $"{payloadBase64}.{signatureBase64}";
    }

    public (bool IsValid, string? UserId) ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, null);

        var parts = token.Split('.');
        if (parts.Length != 2)
            return (false, null);

        try
        {
            var payloadBytes = Convert.FromBase64String(parts[0]);
            var signatureBytes = Convert.FromBase64String(parts[1]);

            using var hmac = new HMACSHA256(_secretKey);
            var computedSignature = hmac.ComputeHash(payloadBytes);

            if (!CryptographicOperations.FixedTimeEquals(computedSignature, signatureBytes))
                return (false, null);

            var json = Encoding.UTF8.GetString(payloadBytes);
            var payload = JsonSerializer.Deserialize<TokenPayload>(json);

            if (payload == null || string.IsNullOrEmpty(payload.UserId))
                return (false, null);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.ExpiresAt < now)
                return (false, null); // Expired

            return (true, payload.UserId);
        }
        catch
        {
            return (false, null);
        }
    }
}
