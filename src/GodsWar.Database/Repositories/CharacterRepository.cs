using System.Data.Common;
using Dapper;
using GodsWar.Database.Models;

namespace GodsWar.Database.Repositories;

public interface ICharacterRepository
{
    Task<IReadOnlyList<CharacterBase>> GetCharactersByAccountAsync(string account, CancellationToken cancellationToken = default);
    Task<CharacterBase?> GetCharacterByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CharacterBase?> GetCharacterByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<CharacterKitbag?> GetKitbagAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class CharacterRepository : ICharacterRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CharacterRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<CharacterBase>> GetCharactersByAccountAsync(string account, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM godswar.character_base
            WHERE accounts = @Account
            ORDER BY id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<CharacterBase> rows = await conn.QueryAsync<CharacterBase>(
            new CommandDefinition(sql, new { Account = account }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<CharacterBase?> GetCharacterByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM godswar.character_base
            WHERE id = @Id
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<CharacterBase>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<CharacterBase?> GetCharacterByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM godswar.character_base
            WHERE name = @Name
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<CharacterBase>(
            new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<CharacterKitbag?> GetKitbagAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, kitbag_1, kitbag_2, kitbag_3, kitbag_4, storage, equip
            FROM godswar.character_kitbag
            WHERE user_id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<CharacterKitbag>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }
}
