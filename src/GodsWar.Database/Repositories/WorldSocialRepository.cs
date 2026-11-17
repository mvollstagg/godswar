using System.Data.Common;
using Dapper;
using GodsWar.Database.Models;

namespace GodsWar.Database.Repositories;

public interface IWorldSocialRepository
{
    Task<UserQuestRecord?> GetQuestRecordAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveQuestRecordAsync(UserQuestRecord record, CancellationToken cancellationToken = default);

    Task<UserRelationRecord?> GetRelationsAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveRelationsAsync(UserRelationRecord record, CancellationToken cancellationToken = default);

    Task<UserAltarRecord?> GetAltarsAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveAltarsAsync(UserAltarRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsortiaRecord>> GetAllConsortiaAsync(CancellationToken cancellationToken = default);
    Task<ConsortiaRecord?> GetConsortiaByIdAsync(int consortiaId, CancellationToken cancellationToken = default);
    Task<ConsortiaRecord?> GetConsortiaByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<ConsortiaRecord?> CreateConsortiaAsync(string name, string placard, CancellationToken cancellationToken = default);
    Task UpdateConsortiaAsync(ConsortiaRecord record, CancellationToken cancellationToken = default);
    Task DeleteConsortiaAsync(int consortiaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsortiaMemberView>> GetConsortiaMembersAsync(int consortiaId, CancellationToken cancellationToken = default);
    Task UpdateMemberConsortiaAsync(int userId, int consortiaId, short job, int contribute, CancellationToken cancellationToken = default);
}

public sealed class WorldSocialRepository : IWorldSocialRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WorldSocialRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<UserQuestRecord?> GetQuestRecordAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, allqueststatus, activestatus, timequest
            FROM godswar.user_quest
            WHERE id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<UserQuestRecord>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task SaveQuestRecordAsync(UserQuestRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_quest (id, allqueststatus, activestatus, timequest)
            VALUES (@Id, @AllQuestStatus, @ActiveStatus, @TimeQuest)
            ON CONFLICT (id) DO UPDATE SET
                allqueststatus = EXCLUDED.allqueststatus,
                activestatus = EXCLUDED.activestatus,
                timequest = EXCLUDED.timequest;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<UserRelationRecord?> GetRelationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, friend, blacklist
            FROM godswar.user_relation
            WHERE user_id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<UserRelationRecord>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task SaveRelationsAsync(UserRelationRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_relation (user_id, friend, blacklist)
            VALUES (@UserId, @Friend, @Blacklist)
            ON CONFLICT (user_id) DO UPDATE SET
                friend = EXCLUDED.friend,
                blacklist = EXCLUDED.blacklist;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<UserAltarRecord?> GetAltarsAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, altar1, altar2, altar3, altar4, altar5, altar6,
                   altar7, altar8, altar9, altar10, altar11, altar12
            FROM godswar.user_altar
            WHERE user_id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<UserAltarRecord>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task SaveAltarsAsync(UserAltarRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_altar (
                user_id, altar1, altar2, altar3, altar4, altar5, altar6,
                altar7, altar8, altar9, altar10, altar11, altar12)
            VALUES (
                @UserId, @Altar1, @Altar2, @Altar3, @Altar4, @Altar5, @Altar6,
                @Altar7, @Altar8, @Altar9, @Altar10, @Altar11, @Altar12)
            ON CONFLICT (user_id) DO UPDATE SET
                altar1 = EXCLUDED.altar1, altar2 = EXCLUDED.altar2, altar3 = EXCLUDED.altar3,
                altar4 = EXCLUDED.altar4, altar5 = EXCLUDED.altar5, altar6 = EXCLUDED.altar6,
                altar7 = EXCLUDED.altar7, altar8 = EXCLUDED.altar8, altar9 = EXCLUDED.altar9,
                altar10 = EXCLUDED.altar10, altar11 = EXCLUDED.altar11, altar12 = EXCLUDED.altar12;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ConsortiaRecord>> GetAllConsortiaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, placard, found, bijou, lv, honor, altar_info
            FROM godswar.consortia
            ORDER BY id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<ConsortiaRecord> rows = await conn.QueryAsync<ConsortiaRecord>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<ConsortiaRecord?> GetConsortiaByIdAsync(int consortiaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, placard, found, bijou, lv, honor, altar_info
            FROM godswar.consortia
            WHERE id = @Id
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<ConsortiaRecord>(
            new CommandDefinition(sql, new { Id = consortiaId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<ConsortiaRecord?> GetConsortiaByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, placard, found, bijou, lv, honor, altar_info
            FROM godswar.consortia
            WHERE name = @Name
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<ConsortiaRecord>(
            new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<ConsortiaRecord?> CreateConsortiaAsync(string name, string placard, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.consortia (name, placard, found, bijou, lv, honor, altar_info)
            VALUES (@Name, @Placard, 0, 0, 1, 0, '')
            ON CONFLICT (name) DO NOTHING
            RETURNING id, name, placard, found, bijou, lv, honor, altar_info;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<ConsortiaRecord>(
            new CommandDefinition(sql, new { Name = name, Placard = placard }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task UpdateConsortiaAsync(ConsortiaRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.consortia
            SET placard = @Placard,
                found = @Found,
                bijou = @Bijou,
                lv = @Lv,
                honor = @Honor,
                altar_info = @AltarInfo
            WHERE id = @Id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteConsortiaAsync(int consortiaId, CancellationToken cancellationToken = default)
    {
        const string clearMembersSql = """
            UPDATE godswar.character_base
            SET consortia = 0, consortia_job = 0, consortia_contribute = 0
            WHERE consortia = @Id;
            """;
        const string deleteGuildSql = """
            DELETE FROM godswar.consortia
            WHERE id = @Id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(clearMembersSql, new { Id = consortiaId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(deleteGuildSql, new { Id = consortiaId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ConsortiaMemberView>> GetConsortiaMembersAsync(int consortiaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, fighter_job_lv, scholar_job_lv, profession, map, consortia_job, consortia_contribute
            FROM godswar.character_base
            WHERE consortia = @Id
            ORDER BY consortia_job DESC, id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<ConsortiaMemberView> rows = await conn.QueryAsync<ConsortiaMemberView>(
            new CommandDefinition(sql, new { Id = consortiaId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task UpdateMemberConsortiaAsync(int userId, int consortiaId, short job, int contribute, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.character_base
            SET consortia = @ConsortiaId,
                consortia_job = @Job,
                consortia_contribute = @Contribute
            WHERE id = @UserId;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, ConsortiaId = consortiaId, Job = job, Contribute = contribute }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }
}
