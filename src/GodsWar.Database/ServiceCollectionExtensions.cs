using GodsWar.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodsWar.Database;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGodsWarDatabase(
        this IServiceCollection services,
        Action<DatabaseOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<DatabaseOptions>();
        }

        services.AddSingleton<IDbConnectionFactory>(sp =>
        {
            DatabaseOptions options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return new NpgsqlConnectionFactory(options.ConnectionString);
        });

        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<ICharacterRepository, CharacterRepository>();
        return services;
    }
}
