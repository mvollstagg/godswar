using GodsWar.Database;
using GodsWar.LoginServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<LoginServerOptions>(
    builder.Configuration.GetSection(LoginServerOptions.SectionName));

builder.Services.AddGodsWarDatabase(opts =>
{
    string? connStr = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["Database:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(connStr))
    {
        opts.ConnectionString = connStr;
    }
});

builder.Services.AddSingleton<GameServerRegistry>();
builder.Services.AddSingleton<LoginSessionHandler>();
builder.Services.AddSingleton<LoginServerListener>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LoginServerListener>());

using IHost host = builder.Build();
await host.RunAsync();
