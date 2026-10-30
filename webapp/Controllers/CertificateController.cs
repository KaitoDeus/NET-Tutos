using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class CertificateController : Controller
{
    private readonly ICertificateService _certificateService;

    public CertificateController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    // GET: /verify-certificate/{code} (Public verification URL)
    [AllowAnonymous]
    [Route("verify-certificate/{code}")]
    public async Task<IActionResult> Verify(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return View(new CertificateVerifyViewModel { IsFound = false });
        }

        var cert = await _certificateService.GetCertificateByCodeAsync(code);
        var viewModel = new CertificateVerifyViewModel
        {
            IsFound = cert != null,
            Certificate = cert
        };

        return View(viewModel);
    }

    // GET: /Certificate/DownloadPdf/{code}
    [AllowAnonymous]
    public async Task<IActionResult> DownloadPdf(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return NotFound();

        var cert = await _certificateService.GetCertificateByCodeAsync(code);
        if (cert == null) return NotFound();

        var hostUrl = $"{Request.Scheme}://{Request.Host}";
        var pdfBytes = _certificateService.GenerateCertificatePdf(cert, hostUrl);

        return File(pdfBytes, "application/pdf", $"Certificate_{cert.CertificateCode}.pdf");
    }
}
