namespace GodsWar.LoginServer;

public sealed class LoginServerOptions
{
    public const string SectionName = "LoginServer";

    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5999;
    public string AdvertiseIp { get; set; } = "127.0.0.1";
    public int GamePort { get; set; } = 6001;
    public string ServerName { get; set; } = "GodsWar";
    public byte ServerId { get; set; } = 1;

    /// <summary>
    /// If greater than 0, <c>MSG_LOGIN.Version</c> must match this value or the server
    /// replies with <c>LoginResultCode.VersionMismatch (0xFF)</c>.
    /// </summary>
    public int ExpectedVersion { get; set; } = 0;

    /// <summary>
    /// When true, logging in with an unknown account name automatically creates it in
    /// <c>accounts.gamearea_info</c> (convenient for local dev and playtesting).
    /// </summary>
    public bool AutoCreateAccount { get; set; } = false;
}
