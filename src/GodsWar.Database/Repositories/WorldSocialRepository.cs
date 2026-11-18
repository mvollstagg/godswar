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

    Task EnsurePhase6SchemaAsync(CancellationToken cancellationToken = default);
    Task<UserSkillBackupRecord?> GetSkillBackupAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveSkillBackupAsync(UserSkillBackupRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPetRecord>> GetUserPetsAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserPetRecord> CreatePetAsync(UserPetRecord pet, CancellationToken cancellationToken = default);
    Task UpdatePetAsync(UserPetRecord pet, CancellationToken cancellationToken = default);
    Task DeletePetAsync(int petId, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserMentorRecord>> GetMentorRecordsAsync(string characterName, CancellationToken cancellationToken = default);
    Task AddMentorAsync(string masterName, string prenticeName, CancellationToken cancellationToken = default);
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

    public async Task EnsurePhase6SchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS godswar.user_skill_backup (
                user_id integer PRIMARY KEY,
                skill_ids text NOT NULL DEFAULT '',
                item_ids text NOT NULL DEFAULT '',
                r_skill_id integer NOT NULL DEFAULT -1,
                r_item_id integer NOT NULL DEFAULT -1
            );

            CREATE TABLE IF NOT EXISTS godswar.user_pet (
                id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                user_id integer NOT NULL,
                name varchar(32) NOT NULL DEFAULT 'ZodiacPet',
                template_id smallint NOT NULL DEFAULT 1,
                level smallint NOT NULL DEFAULT 1,
                exp integer NOT NULL DEFAULT 0,
                quality smallint NOT NULL DEFAULT 1,
                sex smallint NOT NULL DEFAULT 0,
                savvy smallint NOT NULL DEFAULT 10,
                state smallint NOT NULL DEFAULT 1,
                loyalty smallint NOT NULL DEFAULT 100,
                satiety smallint NOT NULL DEFAULT 100,
                rebirth smallint NOT NULL DEFAULT 0,
                hp integer NOT NULL DEFAULT 200,
                max_hp integer NOT NULL DEFAULT 200,
                attack integer NOT NULL DEFAULT 25,
                defend integer NOT NULL DEFAULT 12,
                magic_attack integer NOT NULL DEFAULT 25,
                magic_defend integer NOT NULL DEFAULT 12,
                hit integer NOT NULL DEFAULT 90
            );

            CREATE INDEX IF NOT EXISTS idx_user_pet_user_id ON godswar.user_pet (user_id);

            CREATE TABLE IF NOT EXISTS godswar.user_mentor (
                master_name varchar(32) NOT NULL,
                prentice_name varchar(32) NOT NULL,
                created_at timestamptz NOT NULL DEFAULT NOW(),
                PRIMARY KEY (master_name, prentice_name)
            );
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<UserSkillBackupRecord?> GetSkillBackupAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, skill_ids, item_ids, r_skill_id, r_item_id
            FROM godswar.user_skill_backup
            WHERE user_id = @UserId
            LIMIT 1;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<UserSkillBackupRecord>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task SaveSkillBackupAsync(UserSkillBackupRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_skill_backup (user_id, skill_ids, item_ids, r_skill_id, r_item_id)
            VALUES (@UserId, @SkillIds, @ItemIds, @RSkillId, @RItemId)
            ON CONFLICT (user_id) DO UPDATE SET
                skill_ids = EXCLUDED.skill_ids,
                item_ids = EXCLUDED.item_ids,
                r_skill_id = EXCLUDED.r_skill_id,
                r_item_id = EXCLUDED.r_item_id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserPetRecord>> GetUserPetsAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, user_id, name, template_id, level, exp, quality, sex, savvy, state,
                   loyalty, satiety, rebirth, hp, max_hp, attack, defend, magic_attack, magic_defend, hit
            FROM godswar.user_pet
            WHERE user_id = @UserId
            ORDER BY id;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<UserPetRecord> rows = await conn.QueryAsync<UserPetRecord>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<UserPetRecord> CreatePetAsync(UserPetRecord pet, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_pet (
                user_id, name, template_id, level, exp, quality, sex, savvy, state,
                loyalty, satiety, rebirth, hp, max_hp, attack, defend, magic_attack, magic_defend, hit)
            VALUES (
                @UserId, @Name, @TemplateId, @Level, @Exp, @Quality, @Sex, @Savvy, @State,
                @Loyalty, @Satiety, @Rebirth, @Hp, @MaxHp, @Attack, @Defend, @MagicAttack, @MagicDefend, @Hit)
            RETURNING id, user_id, name, template_id, level, exp, quality, sex, savvy, state,
                      loyalty, satiety, rebirth, hp, max_hp, attack, defend, magic_attack, magic_defend, hit;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.QuerySingleAsync<UserPetRecord>(
            new CommandDefinition(sql, pet, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }

    public async Task UpdatePetAsync(UserPetRecord pet, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE godswar.user_pet
            SET name = @Name,
                template_id = @TemplateId,
                level = @Level,
                exp = @Exp,
                quality = @Quality,
                sex = @Sex,
                savvy = @Savvy,
                state = @State,
                loyalty = @Loyalty,
                satiety = @Satiety,
                rebirth = @Rebirth,
                hp = @Hp,
                max_hp = @MaxHp,
                attack = @Attack,
                defend = @Defend,
                magic_attack = @MagicAttack,
                magic_defend = @MagicDefend,
                hit = @Hit
            WHERE id = @Id AND user_id = @UserId;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, pet, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeletePetAsync(int petId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM godswar.user_pet
            WHERE id = @Id AND user_id = @UserId;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = petId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserMentorRecord>> GetMentorRecordsAsync(string characterName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT master_name, prentice_name, created_at
            FROM godswar.user_mentor
            WHERE master_name = @Name OR prentice_name = @Name
            ORDER BY created_at;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<UserMentorRecord> rows = await conn.QueryAsync<UserMentorRecord>(
            new CommandDefinition(sql, new { Name = characterName }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task AddMentorAsync(string masterName, string prenticeName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO godswar.user_mentor (master_name, prentice_name, created_at)
            VALUES (@MasterName, @PrenticeName, NOW())
            ON CONFLICT (master_name, prentice_name) DO NOTHING;
            """;

        await using DbConnection conn = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { MasterName = masterName, PrenticeName = prenticeName }, cancellationToken: cancellationToken)
        ).ConfigureAwait(false);
    }
}
