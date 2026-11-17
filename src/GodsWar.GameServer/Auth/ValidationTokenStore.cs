using System.Collections.Concurrent;

namespace GodsWar.GameServer.Auth;

/// <summary>
/// Stores short-lived validation tokens pushed by <c>GodsWar.LoginServer</c> via <c>MSG_VALIDATE_GAMESERVER</c> (300).
/// </summary>
public sealed class ValidationTokenStore
{
    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new(StringComparer.OrdinalIgnoreCase);

    public sealed record TokenEntry(string Account, string ValidateCode, DateTimeOffset ExpiresAtUtc);

    public void Store(string account, string validateCode, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(validateCode))
        {
            return;
        }

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow + (ttl ?? TimeSpan.FromMinutes(5));
        _tokens[account] = new TokenEntry(account, validateCode, expiresAt);
    }

    /// <summary>
    /// Verifies that <paramref name="validateCode"/> matches the stored token for <paramref name="account"/>
    /// and has not expired. Does not consume the token immediately because the client sends it in both
    /// <c>MSG_LOGIN_GAMESERVER</c> (10000) and <c>MSG_ENTER_GAME</c> (10006) / <c>MSG_CLIENT_READY</c> (10007).
    /// </summary>
    public bool Validate(string account, string validateCode)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(validateCode))
        {
            return false;
        }

        if (!_tokens.TryGetValue(account, out TokenEntry? entry))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow > entry.ExpiresAtUtc)
        {
            _tokens.TryRemove(account, out _);
            return false;
        }

        return string.Equals(entry.ValidateCode, validateCode, StringComparison.Ordinal);
    }

    public void Remove(string account)
    {
        if (!string.IsNullOrWhiteSpace(account))
        {
            _tokens.TryRemove(account, out _);
        }
    }
}
