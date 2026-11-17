namespace GodsWar.Database.Models;

public sealed class AccountInfo
{
    public int Id { get; set; }
    public string Accounts { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public short LoginStatus { get; set; }
    public DateTime LastLoginTime { get; set; }
    public DateTime LastLogoutTime { get; set; }
    public string LastLoginIp { get; set; } = string.Empty;
    public string LastLoginMac { get; set; } = string.Empty;
    public long TotalOnlineTime { get; set; }
    public short AccountsStatus { get; set; }
}

public sealed class GameAreaInfo
{
    public int Gid { get; set; }
    public int GameArea { get; set; }
    public int AccId { get; set; }
    public string PassName { get; set; } = string.Empty;
    public string PassPwd { get; set; } = string.Empty;
    public string? Balance { get; set; }
    public short IsDel { get; set; }
    public short? IsDraw { get; set; }
}

public enum AccountAuthStatus
{
    Success = 0,
    InvalidCredentials = 1,
    AccountBanned = 2,
    IpBanned = 3,
}

public sealed record AccountAuthResult(AccountAuthStatus Status, GameAreaInfo? Account);
