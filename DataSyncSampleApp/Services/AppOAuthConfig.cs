using System;

namespace DataSyncSampleApp.Services;

/// <summary>
/// OAuth + Connect API. Confidential client (client secret) disables PKCE — use a registered confidential app in Trimble Developer Console.
/// </summary>
public static class AppOAuthConfig
{
    /// <summary>Trimble Developer Console application Client ID.</summary>
    public const string ClientId = "4438bfff-847d-11e6-904c-02f285fc0101";

    /// <summary>
    /// Confidential client secret. When non-empty, sign-in uses authorization code + secret (no PKCE).
    /// Prefer setting here for builds; see also <see cref="EffectiveClientSecret"/> for env override.
    /// </summary>
    /// <remarks>
    /// Embedding secrets in a mobile app is a security risk (APK can be inspected). Use only where acceptable (e.g. internal builds).
    /// </remarks>
    public const string ClientSecret = "zldT0Op46kXwVp6_fBJzjEQ1VPga";

    public const string AppName = "TC.SDK.Example";

    /// <summary>Must match Android IntentFilter DataScheme + registered redirect (e.g. tcps://datasyncsample/oauth).</summary>
    public const string RedirectUri = "tcps://localhost";

    public const string AuthorityUrl = "https://id.trimble.com/oauth/";

    public const string ConnectServiceUrl = "https://app.connect.trimble.com/tc/api/2.0/";

    /// <summary>
    /// NOTE: No hardcoded encryption key needed!
    /// The SDK automatically uses a certificate-based default encryption key when StorageOptions.EncryptionKey is not provided.
    /// This is more secure than hardcoding keys in source code.
    /// See: Trimble.Connect.Data.Security.CertificateKeyProvider and EncryptionKeyResolver
    /// </summary>

    /// <summary>
    /// Optional: set environment variable <c>DATASYNC_OAUTH_CLIENT_SECRET</c> (e.g. CI) without editing code.
    /// </summary>
    public static string EffectiveClientSecret()
    {
        var env = Environment.GetEnvironmentVariable("DATASYNC_OAUTH_CLIENT_SECRET");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();
        if (!string.IsNullOrWhiteSpace(ClientSecret))
            return ClientSecret.Trim();
        return "";
    }
}
