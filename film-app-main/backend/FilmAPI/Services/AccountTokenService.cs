using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IAccountTokenService
{
    Task<string> CreateTokenAsync(int userId, AccountActionTokenPurpose purpose, TimeSpan ttl, int? createdByUserId = null, string? requestIp = null, string? userAgent = null);
    Task<(int UserId, AccountActionToken Token)> ValidateTokenAsync(string rawToken, AccountActionTokenPurpose purpose);
    Task ConsumeTokenAsync(int tokenId);
    Task RevokeActiveTokensAsync(int userId, AccountActionTokenPurpose purpose);
    string HashToken(string rawToken);
}

public class AccountTokenService : IAccountTokenService
{
    private readonly FilmDbContext _context;

    public AccountTokenService(FilmDbContext context)
    {
        _context = context;
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    public async Task<string> CreateTokenAsync(int userId, AccountActionTokenPurpose purpose, TimeSpan ttl, int? createdByUserId = null, string? requestIp = null, string? userAgent = null)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('/', '-').Replace('+', '_').TrimEnd('=');

        var tokenHash = HashToken(rawToken);

        await RevokeActiveTokensAsync(userId, purpose);

        var token = new AccountActionToken
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            RequestIp = requestIp,
            UserAgent = userAgent
        };

        _context.AccountActionTokens.Add(token);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<(int UserId, AccountActionToken Token)> ValidateTokenAsync(string rawToken, AccountActionTokenPurpose purpose)
    {
        var tokenHash = HashToken(rawToken);
        var token = await _context.AccountActionTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Purpose == purpose);

        if (token is null)
            throw new UnauthorizedAccessException("Token non valido.");

        if (!token.IsValid)
            throw new UnauthorizedAccessException("Token scaduto o gia utilizzato.");

        return (token.UserId, token);
    }

    public async Task ConsumeTokenAsync(int tokenId)
    {
        var token = await _context.AccountActionTokens.FindAsync(tokenId);
        if (token is not null)
        {
            token.UsedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeActiveTokensAsync(int userId, AccountActionTokenPurpose purpose)
    {
        var activeTokens = await _context.AccountActionTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && !t.IsConsumed && !t.IsExpired)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        if (activeTokens.Count > 0)
            await _context.SaveChangesAsync();
    }
}
