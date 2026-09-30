using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NET_Tutos.Services;

public class CertificateService : ICertificateService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CertificateService(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Certificate?> GetCertificateByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var cleanCode = code.Trim().ToUpperInvariant();
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.CertificateCode == cleanCode);
    }

    public async Task<IEnumerable<Certificate>> GetUserCertificatesAsync(string userId)
    {
        return await _context.Certificates
            .Include(c => c.Category)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
    }

    public async Task<Certificate?> GetUserCertificateForCategoryAsync(string userId, int? categoryId)
    {
        return await _context.Certificates
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CategoryId == categoryId);
    }

    public async Task<Certificate> IssueCertificateAsync(string userId, int? categoryId, double score, string hostUrl)
    {
        var existing = await GetUserCertificateForCategoryAsync(userId, categoryId);
        if (existing != null)
        {
            // If already issued, update score if higher
            if (score > existing.FinalScore)
            {
                existing.FinalScore = score;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        var user = await _userManager.FindByIdAsync(userId);
        string studentName = !string.IsNullOrWhiteSpace(user?.FullName) 
            ? user.FullName 
            : (user?.UserName?.Split('@')[0] ?? "Học viên .NET");

        string courseTitle = "Chương trình Lập trình viên .NET Toàn diện";
        if (categoryId.HasValue)
        {
            var cat = await _context.Categories.FindAsync(categoryId.Value);
            if (cat != null)
            {
                courseTitle = $"Khóa học: {cat.Name}";
            }
        }

        // Generate unique code: NET-2026-XXXXXX
        string randomSuffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        string certCode = $"NET-{DateTime.UtcNow.Year}-{randomSuffix}";
        string verificationUrl = $"{hostUrl.TrimEnd('/')}/verify-certificate/{certCode}";

        var cert = new Certificate
        {
            CertificateCode = certCode,
            UserId = userId,
            CategoryId = categoryId,
            CourseTitle = courseTitle,
            StudentFullName = studentName,
            FinalScore = score,
            IssuedAt = DateTime.UtcNow,
            VerificationUrl = verificationUrl
        };

        _context.Certificates.Add(cert);

        // Reward extra XP for certification (+100 XP)
        if (user != null)
        {
            user.ExperiencePoints += 100;
        }

        await _context.SaveChangesAsync();
        return cert;
    }

    public byte[] GenerateCertificatePdf(Certificate certificate, string hostUrl)
    {
        string verificationUrl = string.IsNullOrWhiteSpace(certificate.VerificationUrl) 
            ? $"{hostUrl.TrimEnd('/')}/verify-certificate/{certificate.CertificateCode}" 
            : certificate.VerificationUrl;

        // Generate QR code bytes using QRCoder
        byte[] qrCodeBytes;
        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            qrCodeBytes = qrCode.GetGraphic(10);
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11).FontColor("#1f2937"));

                page.Content().Border(3).BorderColor("#512bd4").Padding(12).Border(1).BorderColor("#0d6efd").Padding(24).Column(col =>
                {
                    // Top Brand Header
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("NET-Tutos | NỀN TẢNG ĐÀO TẠO .NET").FontSize(11).Bold().FontColor("#512bd4").LetterSpacing(1.5f);
                            c.Item().Text("ACCREDITED .NET LEARNING MANAGEMENT SYSTEM").FontSize(8).FontColor("#6b7280");
                        });

                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text($"MÃ SỐ: {certificate.CertificateCode}").FontSize(9).Bold().FontColor("#374151");
                            c.Item().Text($"NGÀY CẤP: {certificate.IssuedAt:dd/MM/yyyy}").FontSize(8).FontColor("#6b7280");
                        });
                    });

                    col.Item().PaddingTop(25).AlignCenter().Column(c =>
                    {
                        c.Item().Text("CHỨNG NHẬN HOÀN THÀNH").FontSize(28).Bold().FontColor("#1e1b4b").LetterSpacing(2f);
                        c.Item().Text("CERTIFICATE OF COMPLETION").FontSize(13).SemiBold().FontColor("#512bd4").LetterSpacing(3f);
                    });

                    col.Item().PaddingTop(20).AlignCenter().Text("Chứng nhận học viên").FontSize(12).Italic().FontColor("#4b5563");

                    col.Item().PaddingTop(6).AlignCenter().Text(certificate.StudentFullName).FontSize(26).Bold().FontColor("#512bd4");

                    col.Item().PaddingTop(12).AlignCenter().Text("Đã hoàn thành xuất sắc chương trình học và vượt qua kỳ thi đánh giá năng lực:")
                        .FontSize(11).FontColor("#4b5563");

                    col.Item().PaddingTop(6).AlignCenter().Text(certificate.CourseTitle)
                        .FontSize(18).Bold().FontColor("#111827");

                    col.Item().PaddingTop(10).AlignCenter().Row(r =>
                    {
                        r.AutoItem().Text("Điểm số bài thi: ").FontSize(11).FontColor("#4b5563");
                        r.AutoItem().Text($"{certificate.FinalScore:F1}%").FontSize(12).Bold().FontColor("#16a34a");
                        r.AutoItem().Text("  •  Xếp loại: ").FontSize(11).FontColor("#4b5563");
                        r.AutoItem().Text("ĐẠT CHUẨN XUẤT SẮC (PASSED)").FontSize(11).Bold().FontColor("#16a34a");
                    });

                    col.Item().PaddingTop(25).Row(row =>
                    {
                        // Left: Verification QR & Info
                        row.RelativeItem().Row(r =>
                        {
                            r.ConstantItem(68).Image(qrCodeBytes);
                            r.RelativeItem().PaddingLeft(10).Column(c =>
                            {
                                c.Item().Text("Xác thực chứng chỉ:").FontSize(9).Bold().FontColor("#374151");
                                c.Item().Text("Quét mã QR hoặc truy cập đường dẫn công khai để kiểm tra tính hợp lệ.").FontSize(7.5f).FontColor("#6b7280");
                                c.Item().Text(verificationUrl).FontSize(7.5f).FontColor("#2563eb").Underline();
                            });
                        });

                        // Right: Signatory & Stamp
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().AlignCenter().Text("NET-Tutos Academic Council").FontSize(10).Bold().FontColor("#1f2937");
                            c.Item().AlignCenter().PaddingTop(25).Text("ĐÃ XÁC THỰC KÝ DUYỆT").FontSize(9).Bold().FontColor("#059669");
                            c.Item().AlignCenter().Text("Hệ thống Khảo thí & Cấp bằng Tự động").FontSize(8).FontColor("#6b7280");
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
