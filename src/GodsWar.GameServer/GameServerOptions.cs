namespace GodsWar.GameServer;

public sealed class GameServerOptions
{
    public const string SectionName = "GameServer";

    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 6001;
    public byte ServerId { get; set; } = 1;
    public string ServerName { get; set; } = "GodsWar";
    public string AdvertiseIp { get; set; } = "127.0.0.1";
    public int MaxOnline { get; set; } = 200;

    public string LoginServerHost { get; set; } = "127.0.0.1";
    public int LoginServerPort { get; set; } = 5999;

    /// <summary>
    /// Path to <c>GW 2.0/Server Files (Compiled)/gameserver/Scene</c>.
    /// When empty, <see cref="World.WorldDataLoader"/> searches upward from the working directory.
    /// </summary>
    public string SceneDataPath { get; set; } = string.Empty;
}
