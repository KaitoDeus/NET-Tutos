using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Localization;

namespace NET_Tutos.Services;

public interface IGeoIpService
{
    string ResolveCountryCode(HttpContext? context);
    bool IsVietnamRequest(HttpContext? context);
}

/// <summary>
/// Automatically resolves visitor geographic region from IP or reverse-proxy headers:
/// - Vietnam region (VN) -> Vietnamese ("vi")
/// - Outside Vietnam (International) -> English ("en")
/// </summary>
public class GeoIpService : IGeoIpService
{
    public const string DetectedCountryItemKey = "NetTutos.DetectedCountry";

    private static readonly string[] CountryHeaderNames =
    [
        "CF-IPCountry",
        "X-Country-Code",
        "X-Geo-Country",
        "CloudFront-Viewer-Country",
        "X-AppEngine-Country",
        "X-Vercel-IP-Country"
    ];

    // APNIC/VNNIC IPv4 CIDR allocations for Vietnam ISPs (VNPT, Viettel, FPT, Mobifone, CMC, NetNam, VNG...)
    private static readonly (uint Network, uint Mask)[] VietnamIpv4Ranges = BuildVietnamIpv4Ranges(
    [
        "1.52.0.0/14",
        "14.160.0.0/11",
        "14.224.0.0/11",
        "27.64.0.0/12",
        "42.112.0.0/13",
        "45.117.80.0/22",
        "45.122.220.0/22",
        "45.124.92.0/22",
        "49.156.52.0/22",
        "58.186.0.0/15",
        "61.28.224.0/19",
        "101.53.0.0/18",
        "101.96.64.0/18",
        "103.1.208.0/22",
        "103.7.36.0/22",
        "103.27.236.0/22",
        "103.28.36.0/22",
        "103.63.108.0/22",
        "103.199.0.0/22",
        "112.78.0.0/19",
        "112.197.0.0/18",
        "113.22.0.0/15",
        "113.160.0.0/11",
        "115.72.0.0/13",
        "116.96.0.0/12",
        "117.0.0.0/13",
        "118.68.0.0/14",
        "123.16.0.0/12",
        "125.212.128.0/17",
        "125.234.0.0/15",
        "171.224.0.0/11",
        "183.80.0.0/14",
        "183.91.0.0/19",
        "202.78.224.0/20",
        "202.151.160.0/20",
        "203.113.128.0/18",
        "203.128.240.0/21",
        "203.160.0.0/23",
        "203.162.0.0/16",
        "203.205.0.0/19",
        "203.210.128.0/17",
        "210.86.224.0/19",
        "210.211.96.0/19",
        "210.245.0.0/17",
        "221.132.0.0/18",
        "221.133.0.0/19",
        "222.252.0.0/14"
    ]);

    // Common VNNIC IPv6 prefixes allocated to Vietnam networks
    private static readonly string[] VietnamIpv6Prefixes =
    [
        "2001:ee0:",
        "2401:d800:",
        "2402:800:",
        "2402:9d80:",
        "2405:4800:",
        "2406:d00:"
    ];

    private static readonly ConcurrentDictionary<string, string> IpCountryCache = new(StringComparer.OrdinalIgnoreCase);

    public string ResolveCountryCode(HttpContext? context)
    {
        return ResolveCountryCodeStatic(context);
    }

    public bool IsVietnamRequest(HttpContext? context)
    {
        return ResolveCountryCodeStatic(context).Equals("VN", StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveCountryCodeStatic(HttpContext? context)
    {
        if (context == null)
        {
            return "VN";
        }

        if (context.Items.TryGetValue(DetectedCountryItemKey, out var cachedItem) && cachedItem is string cachedCountry && !string.IsNullOrEmpty(cachedCountry))
        {
            return cachedCountry;
        }

        // 1. Support explicit ?country=VN / ?country=US query parameter for testing or direct region preview
        if (context.Request.Query.TryGetValue("country", out var queryCountry))
        {
            var qc = queryCountry.ToString().Trim().ToUpperInvariant();
            if (qc.Length == 2 && char.IsLetter(qc[0]) && char.IsLetter(qc[1]))
            {
                context.Items[DetectedCountryItemKey] = qc;
                return qc;
            }
        }

        // 2. Inspect standard Geo-IP headers from Cloudflare, CloudFront, Nginx, Envoy, or Reverse Proxy
        foreach (var headerName in CountryHeaderNames)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerVal))
            {
                var raw = headerVal.ToString().Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(raw))
                {
                    var code = raw.Split(',')[0].Trim();
                    if (code.Length == 2 && code != "XX" && code != "T1" && char.IsLetter(code[0]) && char.IsLetter(code[1]))
                    {
                        context.Items[DetectedCountryItemKey] = code;
                        return code;
                    }
                }
            }
        }

        // 3. Extract client IP address (supports CF-Connecting-IP, X-Forwarded-For, X-Real-IP)
        var clientIp = ExtractClientIp(context);
        if (clientIp == null || IsPrivateOrLoopbackIp(clientIp))
        {
            // Local/loopback/Docker bridge environment:
            // If browser sends an English primary Accept-Language (without 'vi'), treat as US/International;
            // otherwise default to VN for local development in Vietnam.
            if (context.Request.Headers.TryGetValue("Accept-Language", out var acceptLang))
            {
                var langStr = acceptLang.ToString().Trim();
                if (!string.IsNullOrEmpty(langStr))
                {
                    var primaryLang = langStr.Split(',')[0].Trim();
                    if (primaryLang.StartsWith("en", StringComparison.OrdinalIgnoreCase) &&
                        !langStr.Contains("vi", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Items[DetectedCountryItemKey] = "US";
                        return "US";
                    }
                }
            }

            context.Items[DetectedCountryItemKey] = "VN";
            return "VN";
        }

        var ipKey = clientIp.ToString();
        if (IpCountryCache.TryGetValue(ipKey, out var cachedIpCountry))
        {
            context.Items[DetectedCountryItemKey] = cachedIpCountry;
            return cachedIpCountry;
        }

        var detected = IsVietnamPublicIp(clientIp) ? "VN" : "INTL";
        IpCountryCache.TryAdd(ipKey, detected);
        context.Items[DetectedCountryItemKey] = detected;
        return detected;
    }

    public static IPAddress? ExtractClientIp(HttpContext context)
    {
        string[] forwardedHeaders = ["CF-Connecting-IP", "X-Forwarded-For", "X-Real-IP", "X-Client-IP"];
        foreach (var header in forwardedHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var rawVal))
            {
                var firstIp = rawVal.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(firstIp) && IPAddress.TryParse(firstIp, out var parsedIp))
                {
                    return parsedIp.IsIPv4MappedToIPv6 ? parsedIp.MapToIPv4() : parsedIp;
                }
            }
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            return remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;
        }

        return null;
    }

    public static bool IsPrivateOrLoopbackIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.IPv6None))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 127.0.0.0/8
            if (bytes[0] == 127) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 169.254.0.0/16 (Link-local)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 100.64.0.0/10 (Carrier-grade NAT / Docker)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;
            var str = ip.ToString().ToLowerInvariant();
            if (str.StartsWith("fc") || str.StartsWith("fd") || str == "::1") return true;
        }

        return false;
    }

    public static bool IsVietnamPublicIp(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            uint ipUint = IpToUint32(ip);
            foreach (var (network, mask) in VietnamIpv4Ranges)
            {
                if ((ipUint & mask) == network)
                {
                    return true;
                }
            }
            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var ipStr = ip.ToString().ToLowerInvariant();
            foreach (var prefix in VietnamIpv6Prefixes)
            {
                if (ipStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static uint IpToUint32(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    private static (uint Network, uint Mask)[] BuildVietnamIpv4Ranges(string[] cidrs)
    {
        var list = new List<(uint Network, uint Mask)>(cidrs.Length);
        foreach (var cidr in cidrs)
        {
            var parts = cidr.Split('/');
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ip) && int.TryParse(parts[1], out var prefixLen))
            {
                uint mask = prefixLen == 0 ? 0 : uint.MaxValue << (32 - prefixLen);
                uint network = IpToUint32(ip) & mask;
                list.Add((network, mask));
            }
        }
        return list.ToArray();
    }
}

/// <summary>
/// RequestCultureProvider that automatically selects culture based on IP geo-region:
/// - Vietnam IP (VN) -> "vi"
/// - International IP (outside VN) -> "en"
/// Activated only when no explicit ?culture= query or language cookie is present.
/// </summary>
public class GeoIpRequestCultureProvider : RequestCultureProvider
{
    private static readonly Task<ProviderCultureResult?> ViCultureResult =
        Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("vi", "vi"));

    private static readonly Task<ProviderCultureResult?> EnCultureResult =
        Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("en", "en"));

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var countryCode = GeoIpService.ResolveCountryCodeStatic(httpContext);
        return countryCode.Equals("VN", StringComparison.OrdinalIgnoreCase)
            ? ViCultureResult
            : EnCultureResult;
    }
}
