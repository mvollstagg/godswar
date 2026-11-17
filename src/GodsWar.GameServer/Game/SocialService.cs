using System.Collections.Concurrent;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public sealed class SocialService
{
    private readonly WorldManager _world;
    private readonly ICharacterRepository _characters;
    private readonly IWorldSocialRepository _socialRepo;

    private readonly ConcurrentDictionary<int, TeamGroup> _teams = new();
    private readonly ConcurrentDictionary<uint, TradeSession> _tradesByPlayer = new();
    private int _nextTeamId;

    public SocialService(
        WorldManager world,
        ICharacterRepository characters,
        IWorldSocialRepository socialRepo)
    {
        _world = world;
        _characters = characters;
        _socialRepo = socialRepo;
    }

    // ========================================================================
    // Relations (Friends / Blacklist)
    // ========================================================================

    public async Task LoadRelationsAsync(PlayerSession player, CancellationToken ct)
    {
        UserRelationRecord? rec = await _socialRepo.GetRelationsAsync(player.Character.Id, ct).ConfigureAwait(false);
        if (rec is null)
        {
            return;
        }

        foreach (string name in rec.Friend.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            player.Friends.Add(name);
        }

        foreach (string name in rec.Blacklist.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            player.Blacklist.Add(name);
        }
    }

    public async Task SendRelationsOnLoginAsync(PlayerSession player, CancellationToken ct)
    {
        if (player.Friends.Count > 0)
        {
            var entries = player.Friends.Select(n =>
            {
                PlayerSession? p = _world.GetPlayerByName(n);
                return new RelationEntry(n, p is not null ? (byte)p.Character.Camp : (byte)0, p is not null ? (byte)p.Character.Profession : (byte)0, p?.EffectiveLevel ?? 1, (byte)(p is not null ? 1 : 0));
            }).ToArray();

            await player.SendAsync(GameOpcode.MsgRelationAll, new MsgRelationAll(player.UiId, 0, (byte)entries.Length, entries).ToPayload(), ct).ConfigureAwait(false);
        }

        if (player.Blacklist.Count > 0)
        {
            var entries = player.Blacklist.Select(n => new RelationEntry(n, 0, 0, 1, 0)).ToArray();
            await player.SendAsync(GameOpcode.MsgRelationAll, new MsgRelationAll(player.UiId, 1, (byte)entries.Length, entries).ToPayload(), ct).ConfigureAwait(false);
        }
    }

    public async Task HandleRelationRequestAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgRelationRequest req = MsgRelationRequest.Parse(payload.Span);
        if (string.IsNullOrWhiteSpace(req.ResponseName) || req.ResponseName.Equals(player.Character.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (req.Type == 1)
        {
            player.Blacklist.Add(req.ResponseName);
            await SaveRelationsAsync(player, ct).ConfigureAwait(false);
            await SendRelationsOnLoginAsync(player, ct).ConfigureAwait(false);
            return;
        }

        PlayerSession? target = _world.GetPlayerByName(req.ResponseName);
        if (target is null)
        {
            await player.SendAsync(
                GameOpcode.MsgRelationRequest,
                new MsgRelationRequest(player.Character.Name, req.ResponseName, req.Type, 1).ToPayload(),
                ct).ConfigureAwait(false);
            return;
        }

        await target.SendAsync(
            GameOpcode.MsgRelationRequest,
            new MsgRelationRequest(player.Character.Name, target.Character.Name, req.Type, 0).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandleRelationResponseAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgRelationResponse resp = MsgRelationResponse.Parse(payload.Span);
        PlayerSession? requester = _world.GetPlayerByName(resp.RequestName);
        if (!resp.IsAgree || requester is null)
        {
            return;
        }

        player.Friends.Add(requester.Character.Name);
        requester.Friends.Add(player.Character.Name);

        await SaveRelationsAsync(player, ct).ConfigureAwait(false);
        await SaveRelationsAsync(requester, ct).ConfigureAwait(false);

        await SendRelationsOnLoginAsync(player, ct).ConfigureAwait(false);
        await SendRelationsOnLoginAsync(requester, ct).ConfigureAwait(false);
    }

    public async Task HandleRelationDeleteAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgRelationDelete del = MsgRelationDelete.Parse(payload.Span);
        if (del.Style == 1)
        {
            player.Blacklist.Remove(del.RequestName);
        }
        else
        {
            player.Friends.Remove(del.RequestName);
        }

        await SaveRelationsAsync(player, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgRelationDelete, del.ToPayload(), ct).ConfigureAwait(false);
    }

    private Task SaveRelationsAsync(PlayerSession player, CancellationToken ct)
        => _socialRepo.SaveRelationsAsync(
            new UserRelationRecord
            {
                UserId = player.Character.Id,
                Friend = string.Join(',', player.Friends),
                Blacklist = string.Join(',', player.Blacklist),
            },
            ct);

    // ========================================================================
    // Trade (10098..10102)
    // ========================================================================

    public async Task HandleTradeAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTrade msg = MsgTrade.Parse(payload.Span);

        switch (msg.State)
        {
            case 0: // Request
            {
                PlayerSession? target = !string.IsNullOrWhiteSpace(msg.ObjectName)
                    ? _world.GetPlayerByName(msg.ObjectName)
                    : _world.GetPlayerByUiId(msg.ObjectId);
                if (target is null || target.UiId == player.UiId || _tradesByPlayer.ContainsKey(target.UiId))
                {
                    return;
                }

                await target.SendAsync(
                    GameOpcode.MsgTrade,
                    new MsgTrade(player.UiId, target.UiId, 0, 0, player.Character.Name, target.Character.Name).ToPayload(),
                    ct).ConfigureAwait(false);
                break;
            }
            case 1: // Accept / Open
            {
                PlayerSession? partner = !string.IsNullOrWhiteSpace(msg.ObjectName)
                    ? _world.GetPlayerByName(msg.ObjectName)
                    : _world.GetPlayerByUiId(msg.ObjectId);
                if (partner is null || partner.UiId == player.UiId)
                {
                    return;
                }

                var session = new TradeSession(partner, player);
                _tradesByPlayer[partner.UiId] = session;
                _tradesByPlayer[player.UiId] = session;

                var openPkt = new MsgTrade(partner.UiId, player.UiId, 1, 1, partner.Character.Name, player.Character.Name).ToPayload();
                await partner.SendAsync(GameOpcode.MsgTrade, openPkt, ct).ConfigureAwait(false);
                await player.SendAsync(GameOpcode.MsgTrade, openPkt, ct).ConfigureAwait(false);
                break;
            }
            case 2: // Lock
            {
                if (!_tradesByPlayer.TryGetValue(player.UiId, out TradeSession? session))
                {
                    return;
                }

                bool bothLocked;
                lock (session.Sync)
                {
                    if (session.PlayerA.UiId == player.UiId) session.StateA = 2;
                    else if (session.PlayerB.UiId == player.UiId) session.StateB = 2;
                    bothLocked = session.StateA == 2 && session.StateB == 2;
                }

                if (!bothLocked)
                {
                    byte[] lockPkt = new MsgTrade(
                        session.PlayerA.UiId,
                        session.PlayerB.UiId,
                        session.StateA,
                        session.StateB,
                        session.PlayerA.Character.Name,
                        session.PlayerB.Character.Name).ToPayload();
                    await session.PlayerA.SendAsync(GameOpcode.MsgTrade, lockPkt, ct).ConfigureAwait(false);
                    await session.PlayerB.SendAsync(GameOpcode.MsgTrade, lockPkt, ct).ConfigureAwait(false);
                    return;
                }

                await CompleteTradeAsync(session, ct).ConfigureAwait(false);
                break;
            }
            case 4: // Cancel
            {
                await CancelTradeAsync(player, ct).ConfigureAwait(false);
                break;
            }
        }
    }

    public async Task HandleTradeMoneyAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (!_tradesByPlayer.TryGetValue(player.UiId, out TradeSession? session))
        {
            return;
        }

        MsgTradeMoney msg = MsgTradeMoney.Parse(payload.Span);
        int money = Math.Clamp(msg.Money, 0, player.Character.Money);
        int bijou = Math.Clamp(msg.Bijou, 0, player.Character.Stone);

        lock (session.Sync)
        {
            if (session.PlayerA.UiId == player.UiId)
            {
                session.MoneyA = money;
                session.BijouA = bijou;
            }
            else
            {
                session.MoneyB = money;
                session.BijouB = bijou;
            }
        }

        byte[] outPkt = new MsgTradeMoney(player.UiId, money, bijou, player.Character.Name).ToPayload();
        await session.PlayerA.SendAsync(GameOpcode.MsgTradeMoney, outPkt, ct).ConfigureAwait(false);
        await session.PlayerB.SendAsync(GameOpcode.MsgTradeMoney, outPkt, ct).ConfigureAwait(false);
    }

    public async Task HandleTradeAddItemAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (!_tradesByPlayer.TryGetValue(player.UiId, out TradeSession? session))
        {
            return;
        }

        MsgTradeAddItem msg = MsgTradeAddItem.Parse(payload.Span);
        if (msg.Bag < 0 || msg.Bag >= 4 || msg.Grid < 0 || msg.Grid >= 24)
        {
            return;
        }

        ItemEquipSlot item = player.Kitbags[msg.Bag][msg.Grid];
        if (item.IsEmpty || item.Binding > 0)
        {
            return;
        }

        lock (session.Sync)
        {
            Dictionary<int, (int Bag, int Grid, ItemEquipSlot Item)> map =
                session.PlayerA.UiId == player.UiId ? session.ItemsA : session.ItemsB;
            map[msg.Index] = (msg.Bag, msg.Grid, item);
        }

        byte[] outPkt = new MsgTradeItem(player.UiId, msg.Index, item.Base, player.Character.Name, item).ToPayload();
        await session.PlayerA.SendAsync(GameOpcode.MsgTradeItem, outPkt, ct).ConfigureAwait(false);
        await session.PlayerB.SendAsync(GameOpcode.MsgTradeItem, outPkt, ct).ConfigureAwait(false);
    }

    public async Task HandleTradeRemoveItemAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (!_tradesByPlayer.TryGetValue(player.UiId, out TradeSession? session))
        {
            return;
        }

        MsgTradeRemoveItem msg = MsgTradeRemoveItem.Parse(payload.Span);
        lock (session.Sync)
        {
            Dictionary<int, (int Bag, int Grid, ItemEquipSlot Item)> map =
                session.PlayerA.UiId == player.UiId ? session.ItemsA : session.ItemsB;
            map.Remove(msg.Index);
        }

        byte[] outPkt = new MsgTradeRemoveItem(player.UiId, msg.Index, player.Character.Name).ToPayload();
        await session.PlayerA.SendAsync(GameOpcode.MsgTradeRemoveItem, outPkt, ct).ConfigureAwait(false);
        await session.PlayerB.SendAsync(GameOpcode.MsgTradeRemoveItem, outPkt, ct).ConfigureAwait(false);
    }

    public async Task CancelTradeAsync(PlayerSession player, CancellationToken ct)
    {
        if (!_tradesByPlayer.TryRemove(player.UiId, out TradeSession? session))
        {
            return;
        }

        PlayerSession partner = session.PlayerA.UiId == player.UiId ? session.PlayerB : session.PlayerA;
        _tradesByPlayer.TryRemove(partner.UiId, out _);

        byte[] cancelPkt = new MsgTrade(
            session.PlayerA.UiId,
            session.PlayerB.UiId,
            4,
            4,
            session.PlayerA.Character.Name,
            session.PlayerB.Character.Name).ToPayload();
        await session.PlayerA.SendAsync(GameOpcode.MsgTrade, cancelPkt, ct).ConfigureAwait(false);
        await session.PlayerB.SendAsync(GameOpcode.MsgTrade, cancelPkt, ct).ConfigureAwait(false);
    }

    private async Task CompleteTradeAsync(TradeSession session, CancellationToken ct)
    {
        _tradesByPlayer.TryRemove(session.PlayerA.UiId, out _);
        _tradesByPlayer.TryRemove(session.PlayerB.UiId, out _);

        PlayerSession a = session.PlayerA;
        PlayerSession b = session.PlayerB;

        if (a.Character.Money < session.MoneyA || b.Character.Money < session.MoneyB
            || a.Character.Stone < session.BijouA || b.Character.Stone < session.BijouB)
        {
            byte[] failPkt = new MsgTrade(a.UiId, b.UiId, 4, 4, a.Character.Name, b.Character.Name).ToPayload();
            await a.SendAsync(GameOpcode.MsgTrade, failPkt, ct).ConfigureAwait(false);
            await b.SendAsync(GameOpcode.MsgTrade, failPkt, ct).ConfigureAwait(false);
            return;
        }

        // Remove items from source bags and add to partner bags
        foreach ((int bag, int grid, ItemEquipSlot item) in session.ItemsA.Values)
        {
            a.Kitbags[bag][grid] = ItemEquipSlot.Empty;
            b.TryAddItemToBag(item, out _, out _);
        }

        foreach ((int bag, int grid, ItemEquipSlot item) in session.ItemsB.Values)
        {
            b.Kitbags[bag][grid] = ItemEquipSlot.Empty;
            a.TryAddItemToBag(item, out _, out _);
        }

        a.Character.Money = a.Character.Money - session.MoneyA + session.MoneyB;
        b.Character.Money = b.Character.Money - session.MoneyB + session.MoneyA;
        a.Character.Stone = a.Character.Stone - session.BijouA + session.BijouB;
        b.Character.Stone = b.Character.Stone - session.BijouB + session.BijouA;

        byte[] donePkt = new MsgTrade(a.UiId, b.UiId, 3, 3, a.Character.Name, b.Character.Name).ToPayload();
        await a.SendAsync(GameOpcode.MsgTrade, donePkt, ct).ConfigureAwait(false);
        await b.SendAsync(GameOpcode.MsgTrade, donePkt, ct).ConfigureAwait(false);

        await SendFullBagSyncAsync(a, ct).ConfigureAwait(false);
        await SendFullBagSyncAsync(b, ct).ConfigureAwait(false);

        await a.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(a.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
        await b.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(b.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        await PersistPlayerInventoryAndStateAsync(a, ct).ConfigureAwait(false);
        await PersistPlayerInventoryAndStateAsync(b, ct).ConfigureAwait(false);
    }

    // ========================================================================
    // Party / Team (10117..10128)
    // ========================================================================

    public IReadOnlyList<PlayerSession> GetTeamMembers(PlayerSession player)
    {
        if (player.TeamId is not int tid || !_teams.TryGetValue(tid, out TeamGroup? team))
        {
            return [player];
        }

        lock (team.Sync)
        {
            return team.Members.ToArray();
        }
    }

    public Task HandleTeamInfoAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTeamInfo req = MsgTeamInfo.Parse(payload.Span);
        PlayerSession? aim = _world.GetPlayerByName(req.AimName);
        bool aimInTeam = aim?.TeamId is not null;
        bool selfInTeam = player.TeamId is not null;
        return player.SendAsync(
            GameOpcode.MsgTeamInfo,
            new MsgTeamInfo(req.Mode, req.AimName, aimInTeam, selfInTeam).ToPayload(),
            ct).AsTask();
    }

    public async Task HandleTeamInviteAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTeam msg = MsgTeam.Parse(payload.Span);
        PlayerSession? target = _world.GetPlayerByName(msg.AimPlayerName);
        if (target is null || target.UiId == player.UiId || target.TeamId is not null)
        {
            return;
        }

        await target.SendAsync(
            GameOpcode.MsgTeam,
            new MsgTeam(player.UiId, player.Character.Name, target.Character.Name).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandleTeamAcceptAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTeam msg = MsgTeam.Parse(payload.Span);
        PlayerSession? leader = !string.IsNullOrWhiteSpace(msg.ResPlayerName)
            ? _world.GetPlayerByName(msg.ResPlayerName)
            : _world.GetPlayerByUiId(msg.ResPlayerId);
        if (leader is null || player.TeamId is not null)
        {
            return;
        }

        TeamGroup team;
        if (leader.TeamId is int existingId && _teams.TryGetValue(existingId, out TeamGroup? existing))
        {
            team = existing;
        }
        else
        {
            int newId = Interlocked.Increment(ref _nextTeamId);
            team = new TeamGroup(newId, leader);
            _teams[newId] = team;
            leader.TeamId = newId;
        }

        lock (team.Sync)
        {
            if (team.Members.Count >= 5)
            {
                return;
            }
            if (!team.Members.Contains(player))
            {
                team.Members.Add(player);
            }
            player.TeamId = team.TeamId;
        }

        await BroadcastTeamRefreshAsync(team, ct).ConfigureAwait(false);
    }

    public Task HandleTeamRejectAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTeam msg = MsgTeam.Parse(payload.Span);
        PlayerSession? requester = _world.GetPlayerByName(msg.ResPlayerName);
        if (requester is null)
        {
            return Task.CompletedTask;
        }

        return requester.SendAsync(
            GameOpcode.MsgTeamTip,
            new MsgTeamTip(player.Character.Name, $"{player.Character.Name} declined the party invite.").ToPayload(),
            ct).AsTask();
    }

    public async Task HandleTeamLeaveAsync(PlayerSession player, CancellationToken ct)
    {
        if (player.TeamId is not int tid || !_teams.TryGetValue(tid, out TeamGroup? team))
        {
            return;
        }

        bool dissolve;
        lock (team.Sync)
        {
            team.Members.Remove(player);
            player.TeamId = null;
            if (team.Leader.UiId == player.UiId && team.Members.Count > 0)
            {
                team.Leader = team.Members[0];
            }
            dissolve = team.Members.Count <= 1;
        }

        await player.SendAsync(GameOpcode.MsgTeamDestroy, Array.Empty<byte>(), ct).ConfigureAwait(false);

        if (dissolve)
        {
            await DissolveTeamInternalAsync(team, ct).ConfigureAwait(false);
        }
        else
        {
            await BroadcastTeamRefreshAsync(team, ct).ConfigureAwait(false);
        }
    }

    public async Task HandleTeamDeleteAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTeam msg = MsgTeam.Parse(payload.Span);
        if (player.TeamId is not int tid || !_teams.TryGetValue(tid, out TeamGroup? team) || team.Leader.UiId != player.UiId)
        {
            return;
        }

        PlayerSession? target = _world.GetPlayerByName(msg.AimPlayerName);
        if (target is not null && target.TeamId == tid)
        {
            await HandleTeamLeaveAsync(target, ct).ConfigureAwait(false);
        }
    }

    public async Task HandleTeamReplaceLeaderAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTeam msg = MsgTeam.Parse(payload.Span);
        if (player.TeamId is not int tid || !_teams.TryGetValue(tid, out TeamGroup? team) || team.Leader.UiId != player.UiId)
        {
            return;
        }

        PlayerSession? nextLeader = _world.GetPlayerByName(msg.AimPlayerName);
        if (nextLeader is null)
        {
            return;
        }

        lock (team.Sync)
        {
            if (team.Members.Contains(nextLeader))
            {
                team.Leader = nextLeader;
            }
        }

        await BroadcastTeamRefreshAsync(team, ct).ConfigureAwait(false);
    }

    public async Task HandleTeamDissolveAsync(PlayerSession player, CancellationToken ct)
    {
        if (player.TeamId is not int tid || !_teams.TryGetValue(tid, out TeamGroup? team) || team.Leader.UiId != player.UiId)
        {
            return;
        }

        await DissolveTeamInternalAsync(team, ct).ConfigureAwait(false);
    }

    private async Task DissolveTeamInternalAsync(TeamGroup team, CancellationToken ct)
    {
        _teams.TryRemove(team.TeamId, out _);
        PlayerSession[] members;
        lock (team.Sync)
        {
            members = team.Members.ToArray();
            team.Members.Clear();
        }

        foreach (PlayerSession m in members)
        {
            m.TeamId = null;
            await m.SendAsync(GameOpcode.MsgTeamDestroy, Array.Empty<byte>(), ct).ConfigureAwait(false);
        }
    }

    private static async Task BroadcastTeamRefreshAsync(TeamGroup team, CancellationToken ct)
    {
        PlayerSession leader;
        PlayerSession[] mates;
        PlayerSession[] all;
        lock (team.Sync)
        {
            leader = team.Leader;
            mates = team.Members.Where(m => m.UiId != leader.UiId).Take(4).ToArray();
            all = team.Members.ToArray();
        }

        static TeamMemberWire ToWire(PlayerSession p) => new(
            (int)p.UiId,
            p.EffectiveCurHp,
            p.EffectiveMaxHp,
            p.EffectiveLevel,
            (byte)p.Character.Profession,
            p.Character.Name,
            (short)p.MapId,
            p.X,
            p.Y,
            p.Z);

        byte[] payload = new MsgTeamRefresh(ToWire(leader), mates.Select(ToWire).ToArray(), true).ToPayload();
        foreach (PlayerSession m in all)
        {
            await m.SendAsync(GameOpcode.MsgTeamRefresh, payload, ct).ConfigureAwait(false);
        }
    }

    // ========================================================================
    // Guild / Consortia (10131..10146)
    // ========================================================================

    public async Task SendGuildOnLoginAsync(PlayerSession player, CancellationToken ct)
    {
        if (player.Character.Consortia <= 0)
        {
            return;
        }

        ConsortiaRecord? guild = await _socialRepo.GetConsortiaByIdAsync(player.Character.Consortia, ct).ConfigureAwait(false);
        if (guild is null)
        {
            return;
        }

        await SendGuildInfoAndMembersAsync(player, guild, ct).ConfigureAwait(false);
    }

    public async Task CreateGuildAsync(PlayerSession player, string guildName, CancellationToken ct)
    {
        if (player.Character.Consortia > 0)
        {
            await player.SendAsync(GameOpcode.MsgConsortiaCreateResponse, new MsgConsortiaCreateResponse(player.UiId, guildName, player.Character.Name, 1, 3).ToPayload(), ct).ConfigureAwait(false);
            return;
        }

        ConsortiaRecord? created = await _socialRepo.CreateConsortiaAsync(guildName, $"Welcome to {guildName}!", ct).ConfigureAwait(false);
        if (created is null)
        {
            await player.SendAsync(GameOpcode.MsgConsortiaCreateResponse, new MsgConsortiaCreateResponse(player.UiId, guildName, player.Character.Name, 1, 4).ToPayload(), ct).ConfigureAwait(false);
            return;
        }

        player.Character.Consortia = created.Id;
        player.Character.ConsortiaJob = 6; // Master
        player.Character.ConsortiaContribute = 0;

        await _socialRepo.UpdateMemberConsortiaAsync(player.Character.Id, created.Id, 6, 0, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgConsortiaCreateResponse, new MsgConsortiaCreateResponse(player.UiId, created.Name, player.Character.Name, 1, 0).ToPayload(), ct).ConfigureAwait(false);
        await SendGuildInfoAndMembersAsync(player, created, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleConsortiaInviteAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgConsortiaInvite req = MsgConsortiaInvite.Parse(payload.Span);
        if (player.Character.Consortia <= 0 || player.Character.ConsortiaJob < 4)
        {
            return;
        }

        ConsortiaRecord? guild = await _socialRepo.GetConsortiaByIdAsync(player.Character.Consortia, ct).ConfigureAwait(false);
        PlayerSession? target = _world.GetPlayerByName(req.InvitedName);
        if (guild is null || target is null || target.Character.Consortia > 0)
        {
            return;
        }

        await target.SendAsync(
            GameOpcode.MsgConsortiaInvite,
            new MsgConsortiaInvite(player.UiId, target.Character.Name, guild.Name).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandleConsortiaResponseAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgConsortiaResponse resp = MsgConsortiaResponse.Parse(payload.Span);
        PlayerSession? inviter = _world.GetPlayerByName(resp.ReceiveName);
        if (inviter is null || inviter.Character.Consortia <= 0 || player.Character.Consortia > 0)
        {
            return;
        }

        ConsortiaRecord? guild = await _socialRepo.GetConsortiaByIdAsync(inviter.Character.Consortia, ct).ConfigureAwait(false);
        if (guild is null)
        {
            return;
        }

        player.Character.Consortia = guild.Id;
        player.Character.ConsortiaJob = 2; // Member
        player.Character.ConsortiaContribute = 0;

        await _socialRepo.UpdateMemberConsortiaAsync(player.Character.Id, guild.Id, 2, 0, ct).ConfigureAwait(false);
        await SendGuildInfoAndMembersAsync(player, guild, ct).ConfigureAwait(false);
        await SendGuildInfoAndMembersAsync(inviter, guild, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleConsortiaExitAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (player.Character.Consortia <= 0)
        {
            return;
        }

        int guildId = player.Character.Consortia;
        bool isMaster = player.Character.ConsortiaJob >= 6;
        player.Character.Consortia = 0;
        player.Character.ConsortiaJob = 0;
        player.Character.ConsortiaContribute = 0;

        await _socialRepo.UpdateMemberConsortiaAsync(player.Character.Id, 0, 0, 0, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgConsortiaExit, new MsgConsortiaExit(player.Character.Name, string.Empty, 0).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        if (isMaster)
        {
            await _socialRepo.DeleteConsortiaAsync(guildId, ct).ConfigureAwait(false);
        }
    }

    public async Task HandleConsortiaTextAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgConsortiaText msg = MsgConsortiaText.Parse(payload.Span);
        if (player.Character.Consortia <= 0 || player.Character.ConsortiaJob < 5)
        {
            return;
        }

        ConsortiaRecord? guild = await _socialRepo.GetConsortiaByIdAsync(player.Character.Consortia, ct).ConfigureAwait(false);
        if (guild is null)
        {
            return;
        }

        guild.Placard = msg.Placard;
        await _socialRepo.UpdateConsortiaAsync(guild, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgConsortiaText, msg.ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleConsortiaDutyAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgConsortiaDuty msg = MsgConsortiaDuty.Parse(payload.Span);
        if (player.Character.Consortia <= 0 || player.Character.ConsortiaJob < 6)
        {
            return;
        }

        CharacterBase? target = await _characters.GetCharacterByNameAsync(msg.NewName, ct).ConfigureAwait(false);
        if (target is null || target.Consortia != player.Character.Consortia)
        {
            return;
        }

        await _socialRepo.UpdateMemberConsortiaAsync(target.Id, target.Consortia, msg.NewDuty, target.ConsortiaContribute, ct).ConfigureAwait(false);
        PlayerSession? online = _world.GetPlayerByName(msg.NewName);
        if (online is not null)
        {
            online.Character.ConsortiaJob = msg.NewDuty;
        }

        await player.SendAsync(GameOpcode.MsgConsortiaDuty, msg.ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleConsortiaMemberDelAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgConsortiaMemberDel msg = MsgConsortiaMemberDel.Parse(payload.Span);
        if (player.Character.Consortia <= 0 || player.Character.ConsortiaJob < 5)
        {
            return;
        }

        CharacterBase? target = await _characters.GetCharacterByNameAsync(msg.Name, ct).ConfigureAwait(false);
        if (target is null || target.Consortia != player.Character.Consortia)
        {
            return;
        }

        await _socialRepo.UpdateMemberConsortiaAsync(target.Id, 0, 0, 0, ct).ConfigureAwait(false);
        PlayerSession? online = _world.GetPlayerByName(msg.Name);
        if (online is not null)
        {
            online.Character.Consortia = 0;
            online.Character.ConsortiaJob = 0;
            await online.SendAsync(GameOpcode.MsgConsortiaExit, new MsgConsortiaExit(online.Character.Name, string.Empty, 0).ToPayload(), ct).ConfigureAwait(false);
        }

        await player.SendAsync(GameOpcode.MsgConsortiaMemberDel, msg.ToPayload(), ct).ConfigureAwait(false);
    }

    private async Task SendGuildInfoAndMembersAsync(PlayerSession player, ConsortiaRecord guild, CancellationToken ct)
    {
        int maxMembers = 20 + Math.Max(0, guild.Lv - 1) * 10;
        await player.SendAsync(
            GameOpcode.MsgConsortiaBaseInfo,
            new MsgConsortiaBaseInfo(guild.Name, (byte)Math.Clamp(guild.Lv, 1, 255), guild.Found, guild.Bijou, guild.Honor, guild.Placard ?? string.Empty, maxMembers).ToPayload(),
            ct).ConfigureAwait(false);

        IReadOnlyList<ConsortiaMemberView> members = await _socialRepo.GetConsortiaMembersAsync(guild.Id, ct).ConfigureAwait(false);
        var wires = members.Select(m =>
        {
            PlayerSession? online = _world.GetPlayerByName(m.Name);
            sbyte mapId = online is not null ? (sbyte)online.MapId : (sbyte)-1;
            byte lv = (byte)Math.Clamp(Math.Max((int)m.FighterJobLv, (int)m.ScholarJobLv), 1, 255);
            return new ConsortiaMemberWire(m.Name, mapId, (byte)m.ConsortiaJob, lv, (byte)m.Profession, (uint)Math.Max(0, m.ConsortiaContribute));
        }).ToArray();

        await player.SendAsync(
            GameOpcode.MsgConsortiaMemberList,
            new MsgConsortiaMemberList(player.UiId, wires, true).ToPayload(),
            ct).ConfigureAwait(false);
    }

    private static async Task SendFullBagSyncAsync(PlayerSession player, CancellationToken ct)
    {
        int bags = Math.Clamp(player.Character.BagNum, 1, 4);
        for (int b = 0; b < bags; b++)
        {
            await GameplayHandlers.SendKitbagPageForSlotAsync(player, b, 0, ct).ConfigureAwait(false);
            await GameplayHandlers.SendKitbagPageForSlotAsync(player, b, 12, ct).ConfigureAwait(false);
        }
    }

    private async Task PersistPlayerInventoryAndStateAsync(PlayerSession player, CancellationToken ct)
    {
        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await _characters.UpdateKitbagAsync(
            player.Character.Id,
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[0]),
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[1]),
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[2]),
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[3]),
            ItemEquipSlot.FormatDbKitbagString(player.Equips),
            ct).ConfigureAwait(false);
    }

    private sealed class TeamGroup
    {
        public object Sync { get; } = new();
        public int TeamId { get; }
        public PlayerSession Leader { get; set; }
        public List<PlayerSession> Members { get; } = [];

        public TeamGroup(int teamId, PlayerSession leader)
        {
            TeamId = teamId;
            Leader = leader;
            Members.Add(leader);
        }
    }

    private sealed class TradeSession
    {
        public object Sync { get; } = new();
        public PlayerSession PlayerA { get; }
        public PlayerSession PlayerB { get; }
        public int StateA { get; set; } = 1;
        public int StateB { get; set; } = 1;
        public int MoneyA { get; set; }
        public int MoneyB { get; set; }
        public int BijouA { get; set; }
        public int BijouB { get; set; }
        public Dictionary<int, (int Bag, int Grid, ItemEquipSlot Item)> ItemsA { get; } = [];
        public Dictionary<int, (int Bag, int Grid, ItemEquipSlot Item)> ItemsB { get; } = [];

        public TradeSession(PlayerSession a, PlayerSession b)
        {
            PlayerA = a;
            PlayerB = b;
        }
    }
}
