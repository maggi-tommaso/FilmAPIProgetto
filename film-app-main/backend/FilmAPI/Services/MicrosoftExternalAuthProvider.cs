using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace FilmAPI.Services;

public class MicrosoftExternalAuthProvider : IExternalAuthProvider
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _authority;
    private readonly HttpClient _http;
    private OidcDiscovery? _cachedDiscovery;

    public MicrosoftExternalAuthProvider(HttpClient http)
    {
        _clientId = Environment.GetEnvironmentVariable("MICROSOFT_OAUTH_CLIENT_ID") ?? string.Empty;
        _clientSecret = Environment.GetEnvironmentVariable("MICROSOFT_OAUTH_CLIENT_SECRET") ?? string.Empty;
        _authority = Environment.GetEnvironmentVariable("MICROSOFT_AUTHORITY") ?? "common";
        _http = http;
    }

    public string Provider => "Microsoft";

    public string GetAuthorizationUrl(string state, string codeChallenge, string redirectUri)
    {
        return $"https://login.microsoftonline.com/{_authority}/oauth2/v2.0/authorize?response_type=code&client_id={_clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=openid%20profile%20email&state={state}&code_challenge={codeChallenge}&code_challenge_method=S256";
    }

    public async Task<OidcDiscovery> GetDiscoveryAsync()
    {
        if (_cachedDiscovery is not null) return _cachedDiscovery;

        var url = $"https://login.microsoftonline.com/{_authority}/v2.0/.well-known/openid-configuration";
        var response = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);
        _cachedDiscovery = new OidcDiscovery
        {
            Issuer = doc.RootElement.GetProperty("issuer").GetString()!,
            AuthorizationEndpoint = doc.RootElement.GetProperty("authorization_endpoint").GetString()!,
            TokenEndpoint = doc.RootElement.GetProperty("token_endpoint").GetString()!,
            JwksUri = doc.RootElement.GetProperty("jwks_uri").GetString()!
        };
        return _cachedDiscovery;
    }

    public async Task<ExternalUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var discovery = await GetDiscoveryAsync();

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        });

        var response = await _http.PostAsync(discovery.TokenEndpoint, body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var idToken = doc.RootElement.GetProperty("id_token").GetString()!;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        var oid = jwt.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var tid = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

        return new ExternalUserInfo
        {
            ProviderUserId = oid ?? sub ?? string.Empty,
            TenantId = tid,
            Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? string.Empty,
            Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
            EmailVerified = true
        };
    }
}
