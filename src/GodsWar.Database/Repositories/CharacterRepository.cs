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
    Task<string?> GetUserMagicAsync(int userId, CancellationToken cancellationToken = default);
    Task<string?> GetUserSkillAsync(int userId, CancellationToken cancellationToken = default);
    Task<CreateCharacterResult> CreateCharacterAsync(CreateCharacterParams request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCharacterAsync(string account, string name, CancellationToken cancellationToken = default);
    Task UpdateCharacterPositionAsync(int id, short map, float posX, float posZ, CancellationToken cancellationToken = default);
    Task UpdateCharacterStateAsync(CharacterBase character, CancellationToken cancellationToken = default);
    Task UpdateKitbagAsync(int userId, string kitbag1, string? kitbag2, string? kitbag3, string? kitbag4, string equip, CancellationToken cancellationToken = default);
    Task UpdateStorageAsync(int userId, string storage, CancellationToken cancellationToken = default);
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

    public async Task<string?> GetUserMagicAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT magic_info
            FROM godswar.user_magic
            WHERE user_id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<string?> GetUserSkillAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT skill_info
            FROM godswar.user_skill
            WHERE user_id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task<CreateCharacterResult> CreateCharacterAsync(CreateCharacterParams request, CancellationToken cancellationToken = default)
    {
        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbTransaction tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        int existingCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM godswar.character_base WHERE accounts = @Account;",
                new { request.Account },
                tx,
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        if (existingCount >= 4)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new CreateCharacterResult(CreateCharacterStatus.LimitReached, null);
        }

        int nameExists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM godswar.character_base WHERE name = @Name;",
                new { request.Name },
                tx,
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        if (nameExists > 0)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new CreateCharacterResult(CreateCharacterStatus.DuplicateName, null);
        }

        const string insertSql = """
            INSERT INTO godswar.character_base
                (accounts, name, gender, gm, camp, profession, fighter_job_lv, scholar_job_lv,
                 fighter_job_exp, scholar_job_exp, cur_hp, cur_mp, status, belief, prestige, earl_rank,
                 consortia, consortia_job, consortia_contribute, store_num, bag_num, hair_style, face_shap,
                 map, pos_x, pos_z, money, stone, skill_point, skill_exp, max_hp, max_mp, register_time, last_login_time)
            VALUES
                (@Account, @Name, @Gender, 0, @Camp, @Profession, 1, 1,
                 0, 0, @MaxHp, @MaxMp, 0, @Belief, 0, 0,
                 0, 0, 0, 10, 1, @HairStyle, @FaceShap,
                 @Map, @PosX, @PosZ, 0, 0, 0, 0, @MaxHp, @MaxMp, NOW(), NOW())
            RETURNING *;
            """;

        CharacterBase created = await conn.QuerySingleAsync<CharacterBase>(
            new CommandDefinition(insertSql, request, tx, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        const string emptyBag = "[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#";
        const string emptyEquip = "[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#";

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO godswar.character_kitbag (user_id, kitbag_1, kitbag_2, kitbag_3, kitbag_4, storage, equip)
                VALUES (@UserId, @EmptyBag, NULL, NULL, NULL, NULL, @EmptyEquip)
                ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_altar (user_id) VALUES (@UserId) ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_magic (user_id, magic_info) VALUES (@UserId, '') ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_skill (user_id, skill_info) VALUES (@UserId, '') ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_quest (id, allqueststatus, activestatus, timequest) VALUES (@UserId, '', '', '') ON CONFLICT (id) DO NOTHING;
                """,
                new { UserId = created.Id, EmptyBag = emptyBag, EmptyEquip = emptyEquip },
                tx,
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new CreateCharacterResult(CreateCharacterStatus.Success, created);
    }

    public async Task<bool> DeleteCharacterAsync(string account, string name, CancellationToken cancellationToken = default)
    {
        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbTransaction tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        int? id = await conn.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM godswar.character_base WHERE accounts = @Account AND name = @Name LIMIT 1;",
                new { Account = account, Name = name },
                tx,
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        if (id is null)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE FROM godswar.character_kitbag WHERE user_id = @Id;
                DELETE FROM godswar.user_altar WHERE user_id = @Id;
                DELETE FROM godswar.user_magic WHERE user_id = @Id;
                DELETE FROM godswar.user_skill WHERE user_id = @Id;
                DELETE FROM godswar.user_quest WHERE id = @Id;
                DELETE FROM godswar.character_base WHERE id = @Id;
                """,
                new { Id = id.Value },
                tx,
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task UpdateCharacterPositionAsync(int id, short map, float posX, float posZ, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.character_base
            SET map = @Map,
                pos_x = @PosX,
                pos_z = @PosZ,
                last_login_time = NOW()
            WHERE id = @Id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, Map = map, PosX = posX, PosZ = posZ }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task UpdateCharacterStateAsync(CharacterBase character, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.character_base
            SET fighter_job_lv = @FighterJobLv,
                scholar_job_lv = @ScholarJobLv,
                fighter_job_exp = @FighterJobExp,
                scholar_job_exp = @ScholarJobExp,
                cur_hp = @CurHp,
                cur_mp = @CurMp,
                max_hp = @MaxHp,
                max_mp = @MaxMp,
                map = @Map,
                pos_x = @PosX,
                pos_z = @PosZ,
                money = @Money,
                stone = @Stone,
                prestige = @Prestige,
                belief = @Belief,
                consortia = @Consortia,
                consortia_job = @ConsortiaJob,
                consortia_contribute = @ConsortiaContribute,
                skill_point = @SkillPoint,
                skill_exp = @SkillExp,
                last_login_time = NOW()
            WHERE id = @Id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, character, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task UpdateStorageAsync(int userId, string storage, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.character_kitbag
            SET storage = @Storage
            WHERE user_id = @UserId;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, Storage = storage }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task UpdateKitbagAsync(
        int userId,
        string kitbag1,
        string? kitbag2,
        string? kitbag3,
        string? kitbag4,
        string equip,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.character_kitbag
            SET kitbag_1 = @Kitbag1,
                kitbag_2 = @Kitbag2,
                kitbag_3 = @Kitbag3,
                kitbag_4 = @Kitbag4,
                equip = @Equip
            WHERE user_id = @UserId;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { UserId = userId, Kitbag1 = kitbag1, Kitbag2 = kitbag2, Kitbag3 = kitbag3, Kitbag4 = kitbag4, Equip = equip },
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }
}
