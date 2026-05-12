using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace FilmAPI.Services;

public class GoogleExternalAuthProvider : IExternalAuthProvider
{
    private static readonly OidcDiscovery _discovery = new()
    {
        Issuer = "https://accounts.google.com",
        AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
        TokenEndpoint = "https://oauth2.googleapis.com/token",
        JwksUri = "https://www.googleapis.com/oauth2/v3/certs"
    };

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly HttpClient _http;

    public GoogleExternalAuthProvider(HttpClient http)
    {
        _clientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID") ?? string.Empty;
        _clientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET") ?? string.Empty;
        _http = http;
    }

    public string Provider => "Google";

    public string GetAuthorizationUrl(string state, string codeChallenge, string redirectUri)
    {
        return $"{_discovery.AuthorizationEndpoint}?response_type=code&client_id={_clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=openid%20email%20profile&state={state}&code_challenge={codeChallenge}&code_challenge_method=S256";
    }

    public Task<OidcDiscovery> GetDiscoveryAsync() => Task.FromResult(_discovery);

    public async Task<ExternalUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        });

        var response = await _http.PostAsync(_discovery.TokenEndpoint, body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var idToken = doc.RootElement.GetProperty("id_token").GetString()!;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
        var emailVerified = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;

        return new ExternalUserInfo
        {
            ProviderUserId = sub,
            Email = email,
            Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
            EmailVerified = emailVerified
        };
    }
}
