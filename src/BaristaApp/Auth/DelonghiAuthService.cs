using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BaristaApp.Auth;

public class DelonghiAuthException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>
/// Handles authentication against Gigya SSO and Ayla Networks IoT cloud.
/// Tokens are cached in memory only; refresh is performed automatically.
/// Credentials are read from DELONGHI_EMAIL / DELONGHI_PASSWORD environment variables.
/// </summary>
public sealed class DelonghiAuthService
{
    private readonly HttpClient _http;
    private readonly ILogger<DelonghiAuthService> _logger;
    private readonly string _email;
    private readonly string _password;

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DelonghiAuthService(HttpClient http, ILogger<DelonghiAuthService> logger)
    {
        _http = http;
        _logger = logger;

        _email = Environment.GetEnvironmentVariable("DELONGHI_EMAIL")
            ?? throw new InvalidOperationException(
                "DELONGHI_EMAIL environment variable is not set.");
        _password = Environment.GetEnvironmentVariable("DELONGHI_PASSWORD")
            ?? throw new InvalidOperationException(
                "DELONGHI_PASSWORD environment variable is not set.");
    }

    /// <summary>Returns a valid Ayla access token, refreshing or re-authenticating as needed.</summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry - TimeSpan.FromMinutes(5))
            return _accessToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry - TimeSpan.FromMinutes(5))
                return _accessToken;

            if (_refreshToken is not null)
            {
                try
                {
                    await RefreshTokenAsync(ct);
                    return _accessToken!;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Token refresh failed, falling back to full re-auth");
                }
            }

            await AuthenticateAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        _logger.LogInformation("Authenticating {Email} against Gigya + Ayla", _email);

        // ── Step 1: Gigya login ────────────────────────────────────────────
        var loginForm = new Dictionary<string, string>
        {
            ["loginID"]           = _email,
            ["password"]          = _password,
            ["apiKey"]            = Constants.GigyaApiKey,
            ["targetEnv"]         = "mobile",
            ["include"]           = "id_token,profile,data,preferences",
            ["sessionExpiration"] = "7776000",
            ["httpStatusCodes"]   = "true",
        };

        using var loginResp = await _http.PostAsync(
            $"{Constants.GigyaBaseUrl}/accounts.login",
            new FormUrlEncodedContent(loginForm), ct);

        loginResp.EnsureSuccessStatusCode();

        using var loginDoc = await JsonDocument.ParseAsync(
            await loginResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var loginRoot = loginDoc.RootElement;

        if (loginRoot.TryGetProperty("errorCode", out var ec) && ec.GetInt32() != 0)
        {
            var msg = loginRoot.TryGetProperty("errorMessage", out var em) ? em.GetString() : "Unknown";
            throw new DelonghiAuthException($"Gigya login failed: {msg}");
        }

        string? idToken = loginRoot.TryGetProperty("id_token", out var idEl)
            ? idEl.GetString() : null;

        string? sessionToken = null, sessionSecret = null;
        if (loginRoot.TryGetProperty("sessionInfo", out var sessionInfo))
        {
            sessionToken = sessionInfo.TryGetProperty("sessionToken", out var st) ? st.GetString() : null;
            sessionSecret = sessionInfo.TryGetProperty("sessionSecret", out var ss) ? ss.GetString() : null;
            idToken ??= sessionToken;
        }

        if (idToken is null)
            throw new DelonghiAuthException("No id_token in Gigya login response");

        // ── Step 2: Get long-lived JWT ─────────────────────────────────────
        string jwt = idToken;
        if (sessionToken is not null && sessionSecret is not null)
        {
            var jwtForm = new Dictionary<string, string>
            {
                ["oauth_token"]     = sessionToken,
                ["secret"]          = sessionSecret,
                ["apiKey"]          = Constants.GigyaApiKey,
                ["fields"]          = "data.favoriteStoreId",
                ["expiration"]      = "7776000",
                ["httpStatusCodes"] = "true",
            };

            using var jwtResp = await _http.PostAsync(
                $"{Constants.GigyaBaseUrl}/accounts.getJWT",
                new FormUrlEncodedContent(jwtForm), ct);
            jwtResp.EnsureSuccessStatusCode();

            using var jwtDoc = await JsonDocument.ParseAsync(
                await jwtResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            jwt = jwtDoc.RootElement.TryGetProperty("id_token", out var jt)
                ? jt.GetString() ?? jwt : jwt;
        }

        // ── Step 3: Ayla token_sign_in ─────────────────────────────────────
        var aylaBody = new
        {
            app_id     = Constants.AylaAppId,
            app_secret = Constants.AylaAppSecret,
            provider   = "gigya_eu1_field",
            token      = jwt,
        };

        using var aylaResp = await _http.PostAsJsonAsync(
            $"{Constants.AylaUserUrl}/api/v1/token_sign_in", aylaBody, ct);
        aylaResp.EnsureSuccessStatusCode();

        using var aylaDoc = await JsonDocument.ParseAsync(
            await aylaResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var aylaRoot = aylaDoc.RootElement;

        _accessToken = aylaRoot.GetProperty("access_token").GetString()
            ?? throw new DelonghiAuthException("Missing access_token in Ayla response");
        _refreshToken = aylaRoot.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = aylaRoot.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 86400;
        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        _logger.LogInformation("Authentication successful (token valid for {Hours}h)", expiresIn / 3600);
    }

    private async Task RefreshTokenAsync(CancellationToken ct)
    {
        _logger.LogDebug("Refreshing Ayla access token");
        var body = new { user = new { refresh_token = _refreshToken } };

        using var resp = await _http.PostAsJsonAsync(
            $"{Constants.AylaUserUrl}/users/refresh_token.json", body, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        _accessToken = root.GetProperty("access_token").GetString()
            ?? throw new DelonghiAuthException("Missing access_token in refresh response");
        _refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : _refreshToken;
        var expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 86400;
        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
    }
}
