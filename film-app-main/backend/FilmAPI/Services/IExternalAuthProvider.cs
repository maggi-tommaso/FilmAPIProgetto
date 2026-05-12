namespace FilmAPI.Services;

public interface IExternalAuthProvider
{
    string Provider { get; }
    string GetAuthorizationUrl(string state, string codeChallenge, string redirectUri);
    Task<ExternalUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri);
    Task<OidcDiscovery> GetDiscoveryAsync();
}

public class ExternalUserInfo
{
    public string ProviderUserId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool EmailVerified { get; set; }
}

public class OidcDiscovery
{
    public string Issuer { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string JwksUri { get; set; } = string.Empty;
}
