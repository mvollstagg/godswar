using GodsWar.GameServer.World;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodsWar.GameServer.Game;

/// <summary>
/// Periodic 200ms world simulation loop driving monster AI, respawns, buffs, and regen.
/// </summary>
public sealed class WorldSimulationService : BackgroundService
{
    private readonly WorldManager _worldManager;
    private readonly ILogger<WorldSimulationService> _logger;

    public WorldSimulationService(WorldManager worldManager, ILogger<WorldSimulationService> logger)
    {
        _worldManager = worldManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            DateTime nowUtc = DateTime.UtcNow;
            foreach (WorldMap map in _worldManager.Maps.Values)
            {
                try
                {
                    await map.TickAsync(nowUtc, _worldManager.Catalog, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Simulation tick error on map {MapId}", map.Scene.MapId);
                }
            }
        }
    }
}
