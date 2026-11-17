using System.Globalization;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public sealed class ChatAndPortalService
{
    private readonly WorldManager _world;
    private readonly ICharacterRepository _characters;
    private readonly SocialService _social;

    public ChatAndPortalService(
        WorldManager world,
        ICharacterRepository characters,
        SocialService social)
    {
        _world = world;
        _characters = characters;
        _social = social;
    }

    public async Task HandleTalkAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTalk msg = MsgTalk.Parse(payload.Span);
        string text = msg.Content.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (text.StartsWith('/'))
        {
            await HandleCommandAsync(player, text, ct).ConfigureAwait(false);
            return;
        }

        var outgoing = new MsgTalk(
            player.UiId,
            msg.Channel,
            0,
            0,
            msg.ChatBubbleStyle,
            text,
            player.Character.Name,
            msg.AimName);
        byte[] outPayload = outgoing.ToPayload();

        switch (msg.Channel)
        {
            case ChatChannel.Whisper:
            {
                PlayerSession? target = _world.GetPlayerByName(msg.AimName);
                if (target is not null && !target.Blacklist.Contains(player.Character.Name))
                {
                    await target.SendAsync(GameOpcode.MsgTalk, outPayload, ct).ConfigureAwait(false);
                }
                await player.SendAsync(GameOpcode.MsgTalk, outPayload, ct).ConfigureAwait(false);
                break;
            }
            case ChatChannel.Team:
            {
                foreach (PlayerSession member in _social.GetTeamMembers(player))
                {
                    await member.SendAsync(GameOpcode.MsgTalk, outPayload, ct).ConfigureAwait(false);
                }
                break;
            }
            case ChatChannel.Guild:
            {
                if (player.Character.Consortia > 0)
                {
                    foreach (PlayerSession p in _world.GetAllPlayers())
                    {
                        if (p.InWorld && p.Character.Consortia == player.Character.Consortia)
                        {
                            await p.SendAsync(GameOpcode.MsgTalk, outPayload, ct).ConfigureAwait(false);
                        }
                    }
                }
                break;
            }
            case ChatChannel.Camp:
            {
                foreach (PlayerSession p in _world.GetAllPlayers())
                {
                    if (p.InWorld && p.Character.Camp == player.Character.Camp)
                    {
                        await p.SendAsync(GameOpcode.MsgTalk, outPayload, ct).ConfigureAwait(false);
                    }
                }
                break;
            }
            case ChatChannel.World:
            {
                foreach (PlayerSession p in _world.GetAllPlayers())
                {
                    if (p.InWorld)
                    {
                        await p.SendAsync(GameOpcode.MsgTalk, outPayload, ct).ConfigureAwait(false);
                    }
                }
                break;
            }
            default:
            {
                WorldMap? map = _world.GetMap(player.MapId);
                if (map is not null)
                {
                    await map.BroadcastToAoiAsync(player.CellX, player.CellZ, GameOpcode.MsgTalk, outPayload, cancellationToken: ct).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    public Task HandleTalkChannelAsync(PlayerSession player, ReadOnlyMemory<byte> payload)
    {
        MsgTalkChannel ch = MsgTalkChannel.Parse(payload.Span);
        player.ActiveChatChannel = ch.Channel;
        return Task.CompletedTask;
    }

    public async Task TransitionMapAsync(
        PlayerSession player,
        ushort targetMapId,
        float targetX,
        float targetZ,
        CancellationToken ct)
    {
        WorldMap? destMap = _world.GetMap(targetMapId);
        if (destMap is null)
        {
            return;
        }

        ushort oldMapId = player.MapId;
        WorldMap? oldMap = _world.GetMap(oldMapId);
        if (oldMap is not null)
        {
            await oldMap.LeaveAsync(player, ct).ConfigureAwait(false);
        }

        player.MapId = targetMapId;
        player.X = targetX;
        player.Z = targetZ;
        player.Character.Map = (short)targetMapId;
        player.Character.PosX = targetX;
        player.Character.PosZ = targetZ;

        await player.SendAsync(
            GameOpcode.MsgSceneChange,
            new MsgSceneChange(player.UiId, targetX, 0f, targetZ, oldMapId, targetMapId).ToPayload(),
            ct).ConfigureAwait(false);

        await destMap.JoinAsync(player, ct).ConfigureAwait(false);
        await _characters.UpdateCharacterPositionAsync(player.Character.Id, (short)targetMapId, targetX, targetZ, ct).ConfigureAwait(false);
    }

    private async Task HandleCommandAsync(PlayerSession player, string commandText, CancellationToken ct)
    {
        string[] parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        string cmd = parts[0].ToLowerInvariant();
        if (cmd == "/help")
        {
            await SendSystemMessageAsync(player, "Commands: /tp <mapId|name> [x z], /item <id> [count], /money <amount>, /level <lv>, /exp <amount>, /heal, /guild create <name>", ct).ConfigureAwait(false);
            return;
        }

        if (cmd == "/guild" && parts.Length >= 3 && parts[1].Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            await _social.CreateGuildAsync(player, parts[2], ct).ConfigureAwait(false);
            return;
        }

        // GM commands (require GM flag > 0 or local dev)
        if (player.Character.Gm <= 0)
        {
            await SendSystemMessageAsync(player, "Unknown command or insufficient GM privileges.", ct).ConfigureAwait(false);
            return;
        }

        switch (cmd)
        {
            case "/heal":
                player.Character.CurHp = (int)player.EffectiveMaxHp;
                player.Character.CurMp = (int)player.EffectiveMaxMp;
                await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
                await SendSystemMessageAsync(player, "Restored HP and MP.", ct).ConfigureAwait(false);
                break;

            case "/money" when parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int money):
                player.Character.Money = Math.Max(0, player.Character.Money + money);
                await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
                await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
                await SendSystemMessageAsync(player, $"Added {money} gold.", ct).ConfigureAwait(false);
                break;

            case "/exp" when parts.Length >= 2 && uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint exp):
                player.AddExp(exp, _world.Catalog, out bool leveled);
                await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
                if (leveled)
                {
                    await player.SendAsync(GameOpcode.MsgUpgrade, GameplayHandlers.BuildUpgradePacket(player).ToPayload(), ct).ConfigureAwait(false);
                }
                await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
                break;

            case "/level" when parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int lv):
            {
                short clamped = (short)Math.Clamp(lv, 1, GameTables.PlayerMaxLevel);
                player.Character.FighterJobLv = clamped;
                player.Character.ScholarJobLv = clamped;
                player.Character.MaxHp = GameTables.GetPlayerMaxHp(clamped, player.Character.Profession);
                player.Character.MaxMp = GameTables.GetPlayerMaxMp(clamped, player.Character.Profession);
                player.Character.CurHp = player.Character.MaxHp;
                player.Character.CurMp = player.Character.MaxMp;
                await player.SendAsync(GameOpcode.MsgUpgrade, GameplayHandlers.BuildUpgradePacket(player).ToPayload(), ct).ConfigureAwait(false);
                await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
                await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
                break;
            }

            case "/item" when parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int itemId):
            {
                byte count = parts.Length >= 3 && byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte c)
                    ? Math.Max((byte)1, c)
                    : (byte)1;
                var item = new ItemEquipSlot(itemId, -1, -1, -1, -1, -1, 1, 1, 0, count);
                if (player.TryAddItemToBag(item, out int b, out int s))
                {
                    await GameplayHandlers.SendKitbagPageForSlotAsync(player, b, s, ct).ConfigureAwait(false);
                    await _characters.UpdateKitbagAsync(
                        player.Character.Id,
                        ItemEquipSlot.FormatDbKitbagString(player.Kitbags[0]),
                        ItemEquipSlot.FormatDbKitbagString(player.Kitbags[1]),
                        ItemEquipSlot.FormatDbKitbagString(player.Kitbags[2]),
                        ItemEquipSlot.FormatDbKitbagString(player.Kitbags[3]),
                        ItemEquipSlot.FormatDbKitbagString(player.Equips),
                        ct).ConfigureAwait(false);
                    await SendSystemMessageAsync(player, $"Spawned item {itemId} x{count}.", ct).ConfigureAwait(false);
                }
                break;
            }

            case "/tp" when parts.Length >= 2:
            {
                WorldMap? dest = ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort mapId)
                    ? _world.GetMap(mapId)
                    : _world.GetMapByName(parts[1]);
                if (dest is not null)
                {
                    float tx = parts.Length >= 4 && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px)
                        ? px
                        : (player.Character.Camp == 0 ? dest.Scene.AthensX : dest.Scene.SpartaX);
                    float tz = parts.Length >= 4 && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float pz)
                        ? pz
                        : (player.Character.Camp == 0 ? dest.Scene.AthensZ : dest.Scene.SpartaZ);
                    await TransitionMapAsync(player, dest.Scene.MapId, tx, tz, ct).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    public static Task SendSystemMessageAsync(PlayerSession player, string message, CancellationToken ct)
        => player.SendAsync(
            GameOpcode.MsgTalk,
            new MsgTalk(0, ChatChannel.System, 0, 0, 0, message, "[System]", string.Empty).ToPayload(),
            ct).AsTask();
}
