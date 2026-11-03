using System.Net;
using FluentAssertions;
using Xunit;

namespace NET_Tutos.IntegrationTests.Controllers;

public class SeoAndGeoIpTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeoAndGeoIpTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("CF-IPCountry", "VN", "14.160.10.25")]
    [InlineData("X-Country-Code", "VN", "113.160.5.10")]
    public async Task Get_Home_FromVietnamIpOrHeader_DefaultsToVietnamese(string headerName, string headerValue, string forwardedIp)
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(headerName, headerValue);
        request.Headers.Add("X-Forwarded-For", forwardedIp);

        // Act
        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Detected-Country").First().Should().Be("VN");
        response.Content.Headers.ContentLanguage.Should().Contain("vi-VN");
        html.Should().Contain("<html lang=\"vi\"");
        html.Should().Contain("<meta property=\"og:locale\" content=\"vi_VN\"");
    }

    [Theory]
    [InlineData("CF-IPCountry", "US", "8.8.8.8")]
    [InlineData("X-Country-Code", "SG", "1.1.1.1")]
    [InlineData("X-Geo-Country", "JP", "208.67.222.222")]
    public async Task Get_Home_FromOutsideVietnamHeader_DefaultsToEnglish(string headerName, string headerValue, string forwardedIp)
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(headerName, headerValue);
        request.Headers.Add("X-Forwarded-For", forwardedIp);

        // Act
        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Detected-Country").First().Should().Be(headerValue);
        response.Content.Headers.ContentLanguage.Should().Contain("en-US");
        html.Should().Contain("<html lang=\"en\"");
        html.Should().Contain("<meta property=\"og:locale\" content=\"en_US\"");
    }

    [Fact]
    public async Task Get_Home_FromPublicInternationalIpWithoutCountryHeader_DefaultsToEnglish()
    {
        // Arrange: Google Public DNS IP (8.8.8.8 - Outside Vietnam)
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Forwarded-For", "8.8.8.8");

        // Act
        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Detected-Country").First().Should().Be("INTL");
        response.Content.Headers.ContentLanguage.Should().Contain("en-US");
        html.Should().Contain("<html lang=\"en\"");
    }

    [Fact]
    public async Task Get_Home_FromPublicVietnamIpWithoutCountryHeader_DefaultsToVietnamese()
    {
        // Arrange: VNPT Vietnam CIDR block IP (14.160.20.30)
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Forwarded-For", "14.160.20.30");

        // Act
        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Detected-Country").First().Should().Be("VN");
        response.Content.Headers.ContentLanguage.Should().Contain("vi-VN");
        html.Should().Contain("<html lang=\"vi\"");
    }

    [Fact]
    public async Task Get_Home_WithExplicitCultureQuery_OverridesGeoIpRegion()
    {
        // Arrange: US IP but explicit ?culture=vi
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/?culture=vi");
        request.Headers.Add("CF-IPCountry", "US");

        // Act
        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentLanguage.Should().Contain("vi-VN");
        html.Should().Contain("<html lang=\"vi\"");
    }

    [Fact]
    public async Task Get_RobotsTxt_ReturnsValidSeoDirectivesAndSitemap()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/robots.txt");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Contain("User-agent: *");
        content.Should().Contain("Allow: /bai-hoc/");
        content.Should().Contain("Allow: /lessons/");
        content.Should().Contain("Disallow: /Admin/");
        content.Should().Contain("Sitemap:");
        content.Should().Contain("/sitemap.xml");
    }

    [Fact]
    public async Task Get_SitemapXml_ReturnsBilingualUrlsWithHreflangAlternates()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/xml");
        xml.Should().Contain("<urlset");
        xml.Should().Contain("hreflang=\"vi\"");
        xml.Should().Contain("hreflang=\"en\"");
        xml.Should().Contain("hreflang=\"x-default\"");
        xml.Should().Contain("/chuong-trinh-csharp-toan-dien");
        xml.Should().Contain("/csharp-full-curriculum");
    }

    [Fact]
    public async Task Get_HtmlPages_IncludeCanonicalHreflangOpenGraphAndJsonLd()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Tutorials");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("<meta name=\"description\"");
        html.Should().Contain("<meta name=\"keywords\"");
        html.Should().Contain("<link rel=\"canonical\"");
        html.Should().Contain("<link rel=\"alternate\" hreflang=\"vi\"");
        html.Should().Contain("<link rel=\"alternate\" hreflang=\"en\"");
        html.Should().Contain("<link rel=\"alternate\" hreflang=\"x-default\"");
        html.Should().Contain("<meta property=\"og:title\"");
        html.Should().Contain("<meta name=\"twitter:card\" content=\"summary_large_image\"");
        html.Should().Contain("<script type=\"application/ld+json\">");
        html.Should().Contain("\"@type\":\"EducationalOrganization\"");
        html.Should().Contain("\"@type\":\"BreadcrumbList\"");
    }

    [Fact]
    public async Task Get_DynamicOgImages_And_IndexNow_ReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act & Assert 1: Dynamic Tutorial OG Image
        var ogTutResponse = await client.GetAsync("/seo/og-image/gioi-thieu-dot-net.svg?lang=vi");
        ogTutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ogTutResponse.Content.Headers.ContentType?.MediaType.Should().Be("image/svg+xml");
        var svgContent = await ogTutResponse.Content.ReadAsStringAsync();
        svgContent.Should().Contain("<svg");
        svgContent.Should().Contain("NET-Tutos");

        // Act & Assert 2: Dynamic Project OG Image
        var ogProjResponse = await client.GetAsync("/seo/og-project/xay-dung-restful-web-api-quan-ly-thu-vien-sach.svg?lang=en");
        ogProjResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ogProjResponse.Content.Headers.ContentType?.MediaType.Should().Be("image/svg+xml");

        // Act & Assert 3: IndexNow verification key
        var indexNowResponse = await client.GetAsync("/nettutos-indexnow-2026.txt");
        indexNowResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var keyText = await indexNowResponse.Content.ReadAsStringAsync();
        keyText.Should().Be("nettutos-indexnow-2026");

        // Act & Assert 4: SEO & Geo-IP Diagnostics API
        var diagResponse = await client.GetAsync("/api/seo/diagnostics");
        diagResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var diagJson = await diagResponse.Content.ReadAsStringAsync();
        diagJson.Should().Contain("\"status\":\"Healthy\"");
        diagJson.Should().Contain("\"FAQPage\"");
    }
}
