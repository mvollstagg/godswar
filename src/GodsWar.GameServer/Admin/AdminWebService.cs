using System.Net;
using System.Text;
using System.Text.Json;
using GodsWar.GameServer.Game;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodsWar.GameServer.Admin;

/// <summary>
/// Built-in HTTP Admin UI &amp; JSON API on port 8080 for managing welcome messages,
/// server announcements, XP rates, events, and online players.
/// </summary>
public sealed class AdminWebService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly WorldManager _world;
    private readonly ChatAndPortalService _chat;
    private readonly GameplayHandlers _gameplay;
    private readonly ILogger<AdminWebService> _logger;

    public AdminWebService(
        WorldManager world,
        ChatAndPortalService chat,
        GameplayHandlers gameplay,
        ILogger<AdminWebService> logger)
    {
        _world = world;
        _chat = chat;
        _gameplay = gameplay;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new HttpListener();
        string prefix = "http://+:8080/";
        try
        {
            listener.Prefixes.Add(prefix);
            listener.Start();
        }
        catch (HttpListenerException)
        {
            prefix = "http://localhost:8080/";
            listener.Prefixes.Clear();
            listener.Prefixes.Add(prefix);
            listener.Start();
        }

        _logger.LogInformation("Admin Web UI listening on {Prefix}", prefix);

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync().WaitAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin Web UI accept error");
                continue;
            }

            _ = Task.Run(() => HandleRequestAsync(ctx, stoppingToken), stoppingToken);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            string path = ctx.Request.Url?.AbsolutePath ?? "/";
            string method = ctx.Request.HttpMethod;

            if (method == "GET" && path == "/")
            {
                await WriteHtmlAsync(ctx.Response, DashboardHtml, ct).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path == "/api/items")
            {
                string q = (ctx.Request.QueryString["q"] ?? string.Empty).Trim();
                IEnumerable<ItemBaseDef> items = _world.Catalog.Items.Values;
                if (!string.IsNullOrEmpty(q))
                {
                    items = items.Where(i =>
                        i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        i.Id.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));
                }

                var result = items
                    .OrderBy(i => i.Id)
                    .Take(50)
                    .Select(i => new
                    {
                        id = i.Id,
                        name = i.Name,
                        level = i.RequiredLevel,
                        slot = i.SlotType.ToString(),
                    })
                    .ToList();

                await WriteJsonAsync(ctx.Response, 200, result, ct).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path == "/api/state")
            {
                var players = _world.OnlinePlayers.Select(p => new
                {
                    name = p.Character.Name,
                    level = p.EffectiveLevel,
                    profession = p.Character.Profession,
                    camp = p.Character.Camp == 0 ? "Sparta" : "Athens",
                    mapId = p.MapId,
                    hp = p.Character.CurHp,
                    maxHp = p.Character.MaxHp,
                    mp = p.Character.CurMp,
                    maxMp = p.Character.MaxMp,
                    money = p.Character.Money,
                    gm = p.Character.Gm,
                }).ToList();

                await WriteJsonAsync(ctx.Response, 200, new
                {
                    xpRatePercent = _world.XpRatePercent,
                    welcomeMessage = _world.WelcomeMessage,
                    players,
                }, ct).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path == "/api/settings")
            {
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                SettingsRequest? req = JsonSerializer.Deserialize<SettingsRequest>(body, JsonOpts);
                if (req is null)
                {
                    await WriteJsonAsync(ctx.Response, 400, new { error = "Invalid JSON" }, ct).ConfigureAwait(false);
                    return;
                }

                if (req.XpRatePercent is int rate && rate >= 1 && rate <= 1_000_000)
                {
                    _world.XpRatePercent = rate;
                }

                if (req.WelcomeMessage is not null)
                {
                    _world.WelcomeMessage = req.WelcomeMessage.Trim();
                }

                await WriteJsonAsync(ctx.Response, 200, new
                {
                    ok = true,
                    xpRatePercent = _world.XpRatePercent,
                    welcomeMessage = _world.WelcomeMessage,
                }, ct).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path == "/api/announce")
            {
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                AnnounceRequest? req = JsonSerializer.Deserialize<AnnounceRequest>(body, JsonOpts);
                string message = req?.Message?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(message))
                {
                    await WriteJsonAsync(ctx.Response, 400, new { error = "Message is required" }, ct).ConfigureAwait(false);
                    return;
                }

                ChatChannel channel = string.Equals(req?.Channel, "world", StringComparison.OrdinalIgnoreCase)
                    ? ChatChannel.World
                    : ChatChannel.System;

                byte[] payload = new MsgTalk(0, channel, 0, 0, 0, message, "GM", string.Empty).ToClientPayload();
                int sent = 0;
                foreach (PlayerSession p in _world.OnlinePlayers)
                {
                    if (p.InWorld)
                    {
                        await p.SendAsync(GameOpcode.MsgTalk, payload, ct).ConfigureAwait(false);
                        sent++;
                    }
                }

                await WriteJsonAsync(ctx.Response, 200, new { ok = true, sent }, ct).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path == "/api/player-action")
            {
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                PlayerActionRequest? req = JsonSerializer.Deserialize<PlayerActionRequest>(body, JsonOpts);
                PlayerSession? target = string.IsNullOrWhiteSpace(req?.Name)
                    ? null
                    : _world.GetPlayerByName(req.Name.Trim());
                if (target is null)
                {
                    await WriteJsonAsync(ctx.Response, 404, new { error = "Player not online" }, ct).ConfigureAwait(false);
                    return;
                }

                bool wasGm = target.Character.Gm != 0;
                if (!wasGm)
                {
                    target.Character.Gm = 1;
                }

                try
                {
                    string cmd = req!.Action?.ToLowerInvariant() switch
                    {
                        "heal" => "/heal",
                        "level" => $"/level {Math.Clamp(req.Value ?? (target.EffectiveLevel + 1), 1, 200)}",
                        "money" => $"/money {req.Value ?? 100000}",
                        "exp" => $"/exp {req.Value ?? 50000}",
                        "item" => $"/item {req.ItemId ?? 4000} {req.Count ?? 1}",
                        "tp" => $"/tp {req.MapId ?? target.MapId}",
                        _ => string.Empty,
                    };

                    if (string.IsNullOrEmpty(cmd))
                    {
                        await WriteJsonAsync(ctx.Response, 400, new { error = "Unknown action" }, ct).ConfigureAwait(false);
                        return;
                    }

                    await _chat.HandleCommandAsync(target, cmd, ct).ConfigureAwait(false);
                    await WriteJsonAsync(ctx.Response, 200, new { ok = true, command = cmd }, ct).ConfigureAwait(false);
                }
                finally
                {
                    if (!wasGm)
                    {
                        target.Character.Gm = 0;
                    }
                }
                return;
            }

            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin Web UI request error");
            try
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task WriteHtmlAsync(HttpListenerResponse resp, string html, CancellationToken ct)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        resp.StatusCode = 200;
        resp.ContentType = "text/html; charset=utf-8";
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        resp.Close();
    }

    private static async Task WriteJsonAsync(HttpListenerResponse resp, int status, object data, CancellationToken ct)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOpts);
        resp.StatusCode = status;
        resp.ContentType = "application/json; charset=utf-8";
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        resp.Close();
    }

    private sealed record SettingsRequest(int? XpRatePercent, string? WelcomeMessage);
    private sealed record AnnounceRequest(string? Channel, string? Message);
    private sealed record PlayerActionRequest(string? Name, string? Action, int? Value, uint? ItemId, int? Count, ushort? MapId);

    private const string DashboardHtml = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1" />
<title>GodsWar Server Manager</title>
<style>
  :root { color-scheme: dark; --bg:#0f1419; --card:#171f27; --border:#2a3542; --accent:#4ea1ff; --ok:#38c172; --text:#e6edf3; --muted:#8b9bb0; }
  body { margin:0; font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif; background:var(--bg); color:var(--text); padding:24px; }
  .wrap { max-width:1040px; margin:0 auto; display:grid; gap:18px; }
  h1 { margin:0; font-size:22px; display:flex; align-items:center; justify-content:space-between; }
  .grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(320px,1fr)); gap:18px; }
  .card { background:var(--card); border:1px solid var(--border); border-radius:10px; padding:16px; }
  h2 { margin:0 0 12px; font-size:15px; color:var(--accent); text-transform:uppercase; letter-spacing:.04em; }
  label { display:block; font-size:12px; color:var(--muted); margin:8px 0 4px; }
  input, select, textarea { width:100%; box-sizing:border-box; background:#0d1217; color:var(--text); border:1px solid var(--border); border-radius:6px; padding:8px 10px; font-size:14px; }
  .row { display:flex; gap:8px; flex-wrap:wrap; margin-top:10px; }
  button { background:var(--accent); color:#07121e; font-weight:600; border:0; border-radius:6px; padding:8px 12px; cursor:pointer; font-size:13px; }
  button.sec { background:#232f3d; color:var(--text); }
  table { width:100%; border-collapse:collapse; font-size:13px; }
  th, td { text-align:left; padding:8px 6px; border-bottom:1px solid var(--border); }
  th { color:var(--muted); font-weight:600; }
  #toast { font-size:13px; color:var(--ok); min-height:18px; }
</style>
</head>
<body>
<div class="wrap">
  <h1>GodsWar Server Manager <span id="toast"></span></h1>
  <div class="grid">
    <div class="card">
      <h2>Rates &amp; Welcome Message</h2>
      <label>XP Rate (% — 100 = 1x, 10000 = 100x)</label>
      <input id="xpRate" type="number" min="1" max="1000000" />
      <div class="row">
        <button class="sec" onclick="setRate(100)">1x (100%)</button>
        <button class="sec" onclick="setRate(1000)">10x (1000%)</button>
        <button class="sec" onclick="setRate(10000)">100x (10000%)</button>
        <button class="sec" onclick="setRate(50000)">500x (50000%)</button>
      </div>
      <label>Welcome Message (sent on login)</label>
      <textarea id="welcomeMsg" rows="2"></textarea>
      <div class="row">
        <button onclick="saveSettings()">Save Settings</button>
      </div>
    </div>

    <div class="card">
      <h2>Server Announcement / Event</h2>
      <label>Channel</label>
      <select id="annChannel">
        <option value="system">System (Yellow Banner)</option>
        <option value="world">World Chat</option>
      </select>
      <label>Message</label>
      <textarea id="annMsg" rows="3" placeholder="e.g. 100x XP Event is live!"></textarea>
      <div class="row">
        <button onclick="sendAnnounce()">Broadcast to All Online</button>
      </div>
    </div>
  </div>

  <div class="card">
    <h2>Online Players <button class="sec" style="float:right;padding:4px 10px" onclick="refresh()">Refresh</button></h2>
    <table>
      <thead><tr><th>Name</th><th>Lv</th><th>Camp</th><th>Map</th><th>HP</th><th>Gold</th><th>Actions</th></tr></thead>
      <tbody id="playersBody"><tr><td colspan="7" style="color:var(--muted)">Loading...</td></tr></tbody>
    </table>
  </div>
  <div id="itemModal" style="display:none;position:fixed;inset:0;background:rgba(0,0,0,.72);align-items:center;justify-content:center;padding:16px;z-index:50">
    <div class="card" style="width:100%;max-width:620px;max-height:85vh;display:flex;flex-direction:column;gap:10px">
      <div style="display:flex;justify-content:space-between;align-items:center">
        <h2 style="margin:0">Give Item to <span id="modalPlayer"></span></h2>
        <button class="sec" onclick="closeItemModal()">Close</button>
      </div>
      <div style="display:flex;gap:8px">
        <input id="itemSearch" placeholder="Search item name or ID (e.g. sword, potion, 4000)..." oninput="searchItems()" />
        <input id="itemCount" type="number" value="1" min="1" max="999" style="width:90px" title="Count" />
      </div>
      <div style="overflow:auto;max-height:360px;border:1px solid var(--border);border-radius:6px">
        <table>
          <thead><tr><th>ID</th><th>Name</th><th>Slot</th><th>Lv</th><th></th></tr></thead>
          <tbody id="itemResults"></tbody>
        </table>
      </div>
    </div>
  </div>
</div>
<script>
const toast = (m) => { document.getElementById('toast').textContent = m; setTimeout(() => document.getElementById('toast').textContent = '', 3500); };
let itemTargetPlayer = '';
let itemSearchTimer = null;
async function refresh() {
  const r = await fetch('/api/state');
  const d = await r.json();
  document.getElementById('xpRate').value = d.xpRatePercent;
  document.getElementById('welcomeMsg').value = d.welcomeMessage || '';
  const tb = document.getElementById('playersBody');
  if (!d.players.length) { tb.innerHTML = '<tr><td colspan="7" style="color:var(--muted)">No players online</td></tr>'; return; }
  tb.innerHTML = d.players.map(p => `
    <tr>
      <td><b>${p.name}</b></td><td>${p.level}</td><td>${p.camp}</td><td>${p.mapId}</td>
      <td>${p.hp}/${p.maxHp}</td><td>${p.money}</td>
      <td>
        <button class="sec" onclick="act('${p.name}','heal')">Heal</button>
        <button class="sec" onclick="act('${p.name}','level',${p.level+1})">+1 Lv</button>
        <button class="sec" onclick="act('${p.name}','money',100000)">+100k</button>
        <button class="sec" onclick="openItemModal('${p.name}')">Item</button>
      </td>
    </tr>`).join('');
}
function setRate(v) { document.getElementById('xpRate').value = v; saveSettings(); }
async function saveSettings() {
  const xpRatePercent = parseInt(document.getElementById('xpRate').value, 10);
  const welcomeMessage = document.getElementById('welcomeMsg').value;
  await fetch('/api/settings', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ xpRatePercent, welcomeMessage }) });
  toast('Settings saved');
}
async function sendAnnounce() {
  const channel = document.getElementById('annChannel').value;
  const message = document.getElementById('annMsg').value;
  const r = await fetch('/api/announce', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ channel, message }) });
  const d = await r.json();
  toast(d.ok ? `Sent to ${d.sent} player(s)` : (d.error || 'Error'));
}
async function act(name, action, value) {
  await fetch('/api/player-action', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ name, action, value }) });
  toast(`${action} -> ${name}`);
  setTimeout(refresh, 300);
}
function openItemModal(name) {
  itemTargetPlayer = name;
  document.getElementById('modalPlayer').textContent = name;
  document.getElementById('itemModal').style.display = 'flex';
  document.getElementById('itemSearch').focus();
  loadItems('');
}
function closeItemModal() {
  document.getElementById('itemModal').style.display = 'none';
}
function searchItems() {
  clearTimeout(itemSearchTimer);
  itemSearchTimer = setTimeout(() => loadItems(document.getElementById('itemSearch').value), 180);
}
async function loadItems(q) {
  const r = await fetch('/api/items?q=' + encodeURIComponent(q));
  const list = await r.json();
  const tb = document.getElementById('itemResults');
  tb.innerHTML = list.map(i => `
    <tr>
      <td>${i.id}</td><td>${i.name}</td><td>${i.slot}</td><td>${i.level}</td>
      <td><button onclick="giveSelectedItem(${i.id})">Give</button></td>
    </tr>`).join('');
}
async function giveSelectedItem(id) {
  const count = parseInt(document.getElementById('itemCount').value || '1', 10);
  await fetch('/api/player-action', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ name: itemTargetPlayer, action:'item', itemId: id, count }) });
  toast(`Gave item ${id} x${count} to ${itemTargetPlayer}`);
}
refresh();
setInterval(refresh, 5000);
</script>
</body>
</html>
""";
}
