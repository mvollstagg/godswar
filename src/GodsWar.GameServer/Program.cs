using GodsWar.Database;
using GodsWar.GameServer;
using GodsWar.GameServer.Admin;
using GodsWar.GameServer.Auth;
using GodsWar.GameServer.Game;
using GodsWar.GameServer.Network;
using GodsWar.GameServer.World;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GameServerOptions>(
    builder.Configuration.GetSection(GameServerOptions.SectionName));

builder.Services.AddGodsWarDatabase(opts =>
{
    string? connStr = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["Database:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(connStr))
    {
        opts.ConnectionString = connStr;
    }
});

builder.Services.AddSingleton<ValidationTokenStore>();
builder.Services.AddSingleton<WorldManager>();
builder.Services.AddSingleton<QuestService>();
builder.Services.AddSingleton<NpcShopAndForgeService>();
builder.Services.AddSingleton<SocialService>();
builder.Services.AddSingleton<ChatAndPortalService>();
builder.Services.AddSingleton<GameplayHandlers>();
builder.Services.AddSingleton<ClientFeaturesService>();
builder.Services.AddSingleton<GameSessionHandler>();
builder.Services.AddHostedService<LoginServerClient>();
builder.Services.AddHostedService<GameServerListener>();
builder.Services.AddHostedService<WorldSimulationService>();
builder.Services.AddHostedService<AdminWebService>();

using IHost host = builder.Build();
await host.RunAsync();
