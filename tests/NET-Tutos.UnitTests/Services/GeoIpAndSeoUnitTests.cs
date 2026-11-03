using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class GeoIpAndSeoUnitTests
{
    [Theory]
    [InlineData("14.160.0.1", true)]      // VNPT Vietnam
    [InlineData("27.72.100.50", true)]    // Viettel Vietnam
    [InlineData("113.161.10.20", true)]   // VNPT Vietnam
    [InlineData("118.70.12.34", true)]    // FPT Telecom Vietnam
    [InlineData("171.244.10.1", true)]    // Viettel Vietnam
    [InlineData("203.113.130.1", true)]   // VNNIC Vietnam
    [InlineData("2001:ee0:4001::1", true)] // VNNIC IPv6 Vietnam
    [InlineData("8.8.8.8", false)]        // Google US
    [InlineData("1.1.1.1", false)]        // Cloudflare International
    [InlineData("54.230.10.15", false)]   // AWS International
    [InlineData("185.199.108.153", false)] // GitHub Fastly US/EU
    public void IsVietnamPublicIp_CorrectlyClassifiesVietnamAndInternationalIps(string ipString, bool expectedIsVietnam)
    {
        var ip = IPAddress.Parse(ipString);
        GeoIpService.IsVietnamPublicIp(ip).Should().Be(expectedIsVietnam);
    }

    [Fact]
    public void SeoMetadataService_BuildsTutorialTechArticleAndBilingualAlternates()
    {
        // Arrange
        var service = new SeoMetadataService();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("nettutos.dev");
        context.Request.Path = "/bai-hoc/gioi-thieu-dot-net";
        context.Request.RouteValues["controller"] = "Tutorials";
        context.Request.RouteValues["action"] = "Details";
        context.Request.RouteValues["slug"] = "gioi-thieu-dot-net";
        context.Request.Headers["CF-IPCountry"] = "VN";

        var tutorial = new Tutorial
        {
            Id = 1,
            Title = "Giới thiệu .NET 10",
            Slug = "gioi-thieu-dot-net",
            Summary = "Tổng quan về nền tảng .NET 10 và C# 13 hiện đại.",
            EstimatedReadingMinutes = 12,
            Difficulty = DifficultyLevel.Beginner,
            Category = new Category { Name = "Nền tảng .NET", Slug = "dot-net-co-ban" }
        };

        var viewData = new Dictionary<string, object?>
        {
            ["Title"] = tutorial.Title,
            ["SeoTutorial"] = tutorial
        };

        // Act
        var metadata = service.BuildMetadata(context, viewData);

        // Assert
        metadata.DetectedCountry.Should().Be("VN");
        metadata.LanguageCode.Should().Be("vi");
        metadata.OgType.Should().Be("article");
        metadata.CanonicalUrl.Should().Be("https://nettutos.dev/bai-hoc/gioi-thieu-dot-net");
        metadata.AlternateViUrl.Should().Be("https://nettutos.dev/bai-hoc/gioi-thieu-dot-net");
        metadata.AlternateEnUrl.Should().StartWith("https://nettutos.dev/lessons/");
        metadata.JsonLd.Should().Contain("\"@type\":\"TechArticle\"");
        metadata.JsonLd.Should().Contain("\"timeRequired\":\"PT12M\"");
    }
}
