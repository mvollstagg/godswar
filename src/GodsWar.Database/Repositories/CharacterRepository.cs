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
    Task UpdateUserMagicAsync(int userId, string magicInfo, CancellationToken cancellationToken = default);
    Task UpdateUserSkillAsync(int userId, string skillInfo, CancellationToken cancellationToken = default);
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
                 @Map, @PosX, @PosZ, 100, 0, 0, 0, @MaxHp, @MaxMp, NOW(), NOW())
            RETURNING *;
            """;

        CharacterBase created = await conn.QuerySingleAsync<CharacterBase>(
            new CommandDefinition(insertSql, request, tx, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        const string starterBag = "[4000,,,,,,0,10,1,1,0]#[4030,,,,,,0,10,1,1,0]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#";
        (string starterEquip, string starterMagic, string starterSkill) = GetStarterEquipmentAndSkills(request.Profession, request.Camp);

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO godswar.character_kitbag (user_id, kitbag_1, kitbag_2, kitbag_3, kitbag_4, storage, equip)
                VALUES (@UserId, @StarterBag, NULL, NULL, NULL, NULL, @StarterEquip)
                ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_altar (user_id) VALUES (@UserId) ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_magic (user_id, magic_info) VALUES (@UserId, @StarterMagic) ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_skill (user_id, skill_info) VALUES (@UserId, @StarterSkill) ON CONFLICT (user_id) DO NOTHING;
                INSERT INTO godswar.user_quest (id, allqueststatus, activestatus, timequest) VALUES (@UserId, '', '', '') ON CONFLICT (id) DO NOTHING;
                """,
                new { UserId = created.Id, StarterBag = starterBag, StarterEquip = starterEquip, StarterMagic = starterMagic, StarterSkill = starterSkill },
                tx,
                cancellationToken: cancellationToken)
        ).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new CreateCharacterResult(CreateCharacterStatus.Success, created);
    }

    private static (string Equip, string Magic, string Skill) GetStarterEquipmentAndSkills(short profession, short camp)
    {
        int recallMagic = camp == 0 ? 3063 : 3061;
        return profession switch
        {
            0 => (
                "[]#[]#[]#[2100,,,,,,1,1,1,1,0]#[]#[]#[2900,,,,,,1,1,1,1,0]#[]#[]#[]#[1000,,,,,,1,1,1,1,0]#[2000,,,,,,1,1,1,1,0]#",
                $"[0,0]#[{recallMagic},0]#",
                "[0,0]#[1,0]#[2,0]#[3,0]#[4,0]#[5,0]#[6,0]#[7,0]#[8,0]#[9,0]#[10,0]#[11,0]#[12,0]#[13,0]#[14,0]#[15,0]#[16,0]#[17,0]#"
            ),
            1 => (
                "[]#[]#[]#[2100,,,,,,1,1,1,1,0]#[]#[]#[2900,,,,,,1,1,1,1,0]#[]#[]#[]#[1400,,,,,,1,1,1,1,0]#[]#",
                $"[250,0]#[{recallMagic},0]#",
                "[50,0]#[51,0]#[52,0]#[53,0]#[54,0]#[55,0]#[56,0]#[57,0]#[58,0]#[59,0]#[60,0]#[61,0]#[62,0]#[63,0]#[64,0]#[65,0]#[66,0]#[67,0]#"
            ),
            2 => (
                "[]#[]#[]#[2200,,,,,,1,1,1,1,0]#[]#[]#[2900,,,,,,1,1,1,1,0]#[]#[]#[]#[1700,,,,,,1,1,1,1,0]#[2000,,,,,,1,1,1,1,0]#",
                $"[800,0]#[{recallMagic},0]#",
                "[150,0]#[151,0]#[152,0]#[153,0]#[154,0]#[155,0]#[156,0]#[157,0]#[158,0]#[159,0]#[160,0]#[161,0]#[162,0]#[163,0]#[164,0]#[165,0]#[166,0]#[167,0]#"
            ),
            _ => (
                "[]#[]#[]#[2200,,,,,,1,1,1,1,0]#[]#[]#[2900,,,,,,1,1,1,1,0]#[]#[]#[]#[1800,,,,,,1,1,1,1,0]#[]#",
                $"[500,0]#[{recallMagic},0]#",
                "[100,0]#[101,0]#[102,0]#[103,0]#[104,0]#[105,0]#[106,0]#[107,0]#[108,0]#[109,0]#[110,0]#[111,0]#[112,0]#[113,0]#[114,0]#[115,0]#[116,0]#[117,0]#"
            ),
        };
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

    public async Task UpdateUserMagicAsync(int userId, string magicInfo, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_magic (user_id, magic_info)
            VALUES (@UserId, @MagicInfo)
            ON CONFLICT (user_id) DO UPDATE SET
                magic_info = EXCLUDED.magic_info;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, MagicInfo = magicInfo }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task UpdateUserSkillAsync(int userId, string skillInfo, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_skill (user_id, skill_info)
            VALUES (@UserId, @SkillInfo)
            ON CONFLICT (user_id) DO UPDATE SET
                skill_info = EXCLUDED.skill_info;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, SkillInfo = skillInfo }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }
}
