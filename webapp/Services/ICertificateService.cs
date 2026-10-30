using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public interface ICertificateService
{
    Task<Certificate?> GetCertificateByCodeAsync(string code);
    Task<IEnumerable<Certificate>> GetUserCertificatesAsync(string userId);
    Task<Certificate?> GetUserCertificateForCategoryAsync(string userId, int? categoryId);
    Task<Certificate> IssueCertificateAsync(string userId, int? categoryId, double score, string hostUrl);
    byte[] GenerateCertificatePdf(Certificate certificate, string hostUrl);
}
