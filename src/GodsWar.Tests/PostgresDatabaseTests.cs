using Dapper;
using GodsWar.Database;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;

namespace GodsWar.Tests;

public sealed class PostgresDatabaseTests
{
    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("GODSWAR_PG_CONNSTR")
        ?? "Host=localhost;Port=5432;Database=godswar;Username=godswar;Password=godswar";

    [Fact]
    public async Task AccountsSchema_ReadsSeededGameAreaAccounts()
    {
        using var factory = new NpgsqlConnectionFactory(GetConnectionString());
        var repo = new AccountRepository(factory);

        IReadOnlyList<GameAreaInfo> accounts = await repo.ListGameAreaAccountsAsync();
        Assert.Contains(accounts, a => a.PassName == "test" && a.PassPwd == "test");
        Assert.Contains(accounts, a => a.PassName == "test2" && a.PassPwd == "123456");
        Assert.Contains(accounts, a => a.PassName == "updays");
    }

    [Fact]
    public async Task AccountsSchema_ValidateAccount_ReturnsExpectedStatuses()
    {
        using var factory = new NpgsqlConnectionFactory(GetConnectionString());
        var repo = new AccountRepository(factory);

        AccountAuthResult ok = await repo.ValidateAccountAsync("test", "test", "127.0.0.1");
        Assert.Equal(AccountAuthStatus.Success, ok.Status);
        Assert.NotNull(ok.Account);
        Assert.Equal("test", ok.Account.PassName);

        AccountAuthResult badPwd = await repo.ValidateAccountAsync("test", "wrong", "127.0.0.1");
        Assert.Equal(AccountAuthStatus.InvalidCredentials, badPwd.Status);
        Assert.Null(badPwd.Account);
    }

    [Fact]
    public async Task GodsWarSchema_ReadsSeededCharacterAndCrossSchemaView()
    {
        using var factory = new NpgsqlConnectionFactory(GetConnectionString());
        var chars = new CharacterRepository(factory);

        CharacterBase? pangu = await chars.GetCharacterByIdAsync(3795);
        Assert.NotNull(pangu);
        Assert.Equal("updays", pangu.Accounts);
        Assert.Equal((short)51, pangu.Map);

        CharacterKitbag? kitbag = await chars.GetKitbagAsync(3795);
        Assert.NotNull(kitbag);
        Assert.False(string.IsNullOrEmpty(kitbag.Kitbag1));

        await using var conn = await factory.OpenConnectionAsync();
        int viewRows = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM accounts.view_web_select_lv;");
        Assert.True(viewRows >= 1);
    }
}
