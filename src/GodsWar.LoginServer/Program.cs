using GodsWar.Database;
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

builder.Services.AddHostedService<LoginServerWorker>();

using IHost host = builder.Build();
await host.RunAsync();

internal sealed class LoginServerWorker(
    ILogger<LoginServerWorker> logger,
    IAccountRepository accountRepository,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int port = configuration.GetValue("LoginServer:Port", 5999);
        logger.LogInformation("GodsWar.LoginServer (Phase 1 scaffold) starting on port {Port}...", port);

        var accounts = await accountRepository.ListGameAreaAccountsAsync(stoppingToken);
        logger.LogInformation("PostgreSQL connected: {AccountCount} accounts in accounts.gamearea_info.", accounts.Count);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
