using GodsWar.Database;
using GodsWar.GameServer;
using GodsWar.GameServer.Auth;
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
builder.Services.AddSingleton<GameSessionHandler>();
builder.Services.AddHostedService<LoginServerClient>();
builder.Services.AddHostedService<GameServerListener>();

using IHost host = builder.Build();
await host.RunAsync();
