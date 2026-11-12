namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Resolves Mobile BFF (/api/mobile/*) and root server (/api/tts/speak) URIs consistently.
/// </summary>
public static class MobileEndpointResolver
{
    public const string DefaultMobileApiBase = "http://10.0.2.2:5000/api/mobile/";

    public static Uri NormalizeMobileApiUri(string? configuredBaseUrl)
    {
        var raw = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? DefaultMobileApiBase
            : configuredBaseUrl.Trim();

        if (!raw.EndsWith('/'))
        {
            raw += "/";
        }

        return new Uri(raw, UriKind.Absolute);
    }

    public static Uri ResolveServerRootUri(string? configuredBaseUrl)
    {
        var apiUri = NormalizeMobileApiUri(configuredBaseUrl);
        var path = apiUri.AbsolutePath.TrimEnd('/');

        if (path.EndsWith("/api/mobile", StringComparison.OrdinalIgnoreCase))
        {
            var rootPath = path[..^"/api/mobile".Length];
            var builder = new UriBuilder(apiUri)
            {
                Path = string.IsNullOrEmpty(rootPath) ? "/" : rootPath + "/"
            };
            return builder.Uri;
        }

        return new Uri(apiUri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
    }
}
