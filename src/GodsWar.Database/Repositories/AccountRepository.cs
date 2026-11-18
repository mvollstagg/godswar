using System.Data.Common;
using Dapper;
using GodsWar.Database.Models;

namespace GodsWar.Database.Repositories;

public interface IAccountRepository
{
    Task<GameAreaInfo?> GetGameAreaAccountAsync(string passName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameAreaInfo>> ListGameAreaAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountAuthResult> ValidateAccountAsync(string passName, string password, string? clientIp = null, CancellationToken cancellationToken = default);
    Task<bool> CreateAccountIfMissingAsync(string passName, string password, CancellationToken cancellationToken = default);
}

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AccountRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<GameAreaInfo?> GetGameAreaAccountAsync(string passName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT gid, game_area, acc_id, pass_name, pass_pwd, balance, is_del, is_draw
            FROM accounts.gamearea_info
            WHERE pass_name = @PassName
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<GameAreaInfo>(
            new CommandDefinition(sql, new { PassName = passName }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GameAreaInfo>> ListGameAreaAccountsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT gid, game_area, acc_id, pass_name, pass_pwd, balance, is_del, is_draw
            FROM accounts.gamearea_info
            ORDER BY gid;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<GameAreaInfo> rows = await conn.QueryAsync<GameAreaInfo>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<AccountAuthResult> ValidateAccountAsync(
        string passName,
        string password,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        const string accountSql = """
            SELECT gid, game_area, acc_id, pass_name, pass_pwd, balance, is_del, is_draw
            FROM accounts.gamearea_info
            WHERE pass_name = @PassName
              AND (pass_pwd = @Password OR MD5(pass_pwd) = LOWER(@Password))
              AND is_del = 0
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        GameAreaInfo? account = await conn.QuerySingleOrDefaultAsync<GameAreaInfo>(
            new CommandDefinition(accountSql, new { PassName = passName, Password = password }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        if (account is null)
        {
            return new AccountAuthResult(AccountAuthStatus.InvalidCredentials, null);
        }

        const string banSql = """
            SELECT EXISTS (
                SELECT 1
                FROM accounts.accounts_ban
                WHERE id = @Gid AND NOW() < unbandate
            );
            """;

        bool accountBanned = await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(banSql, new { account.Gid }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        if (accountBanned)
        {
            return new AccountAuthResult(AccountAuthStatus.AccountBanned, null);
        }

        if (!string.IsNullOrWhiteSpace(clientIp))
        {
            const string ipBanSql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM accounts.ip_ban
                    WHERE ip = @ClientIp AND NOW() < unbandate
                );
                """;

            bool ipBanned = await conn.ExecuteScalarAsync<bool>(
                new CommandDefinition(ipBanSql, new { ClientIp = clientIp }, cancellationToken: cancellationToken)
            ).ConfigureAwait(false);

            if (ipBanned)
            {
                return new AccountAuthResult(AccountAuthStatus.IpBanned, null);
            }
        }

        return new AccountAuthResult(AccountAuthStatus.Success, account);
    }

    public async Task<bool> CreateAccountIfMissingAsync(string passName, string password, CancellationToken cancellationToken = default)
    {
        const string checkSql = "SELECT 1 FROM accounts.gamearea_info WHERE pass_name = @PassName LIMIT 1;";
        const string insertSql = """
            INSERT INTO accounts.gamearea_info (game_area, acc_id, pass_name, pass_pwd, balance, is_del, is_draw)
            VALUES (1, 0, @PassName, @Password, 0, 0, 0);
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        int? exists = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(checkSql, new { PassName = passName }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        if (exists.HasValue)
        {
            return false;
        }

        await conn.ExecuteAsync(
            new CommandDefinition(insertSql, new { PassName = passName, Password = password }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return true;
    }
}
