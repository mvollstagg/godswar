using GodsWar.Database;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGodsWarDatabase(opts =>
{
    string? connStr = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["Database:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(connStr))
    {
        opts.ConnectionString = connStr;
    }
});

builder.Services.AddHostedService<GameServerWorker>();

using IHost host = builder.Build();
await host.RunAsync();

internal sealed class GameServerWorker(
    ILogger<GameServerWorker> logger,
    ICharacterRepository characterRepository,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int port = configuration.GetValue("GameServer:Port", 6001);
        logger.LogInformation("GodsWar.GameServer (Phase 1 scaffold) starting on port {Port}...", port);

        CharacterBase? seeded = await characterRepository.GetCharacterByIdAsync(3795, stoppingToken);
        logger.LogInformation(
            "PostgreSQL connected: seeded character 3795 = {Name} (Account={Account}, Map={Map}).",
            seeded?.Name ?? "<none>",
            seeded?.Accounts ?? "<none>",
            seeded?.Map ?? 0);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
