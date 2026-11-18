namespace GodsWar.Database.Models;

public sealed class UserQuestRecord
{
    public int Id { get; set; }
    public string AllQuestStatus { get; set; } = string.Empty;
    public string ActiveStatus { get; set; } = string.Empty;
    public string TimeQuest { get; set; } = string.Empty;
}

public sealed class UserRelationRecord
{
    public int UserId { get; set; }
    public string Friend { get; set; } = string.Empty;
    public string Blacklist { get; set; } = string.Empty;
}

public sealed class UserAltarRecord
{
    public int UserId { get; set; }
    public int Altar1 { get; set; }
    public int Altar2 { get; set; }
    public int Altar3 { get; set; }
    public int Altar4 { get; set; }
    public int Altar5 { get; set; }
    public int Altar6 { get; set; }
    public int Altar7 { get; set; }
    public int Altar8 { get; set; }
    public int Altar9 { get; set; }
    public int Altar10 { get; set; }
    public int Altar11 { get; set; }
    public int Altar12 { get; set; }

    public int[] ToArray() =>
    [
        Altar1, Altar2, Altar3, Altar4, Altar5, Altar6,
        Altar7, Altar8, Altar9, Altar10, Altar11, Altar12
    ];

    public void FromArray(int[] values)
    {
        Altar1 = values.Length > 0 ? values[0] : 0;
        Altar2 = values.Length > 1 ? values[1] : 0;
        Altar3 = values.Length > 2 ? values[2] : 0;
        Altar4 = values.Length > 3 ? values[3] : 0;
        Altar5 = values.Length > 4 ? values[4] : 0;
        Altar6 = values.Length > 5 ? values[5] : 0;
        Altar7 = values.Length > 6 ? values[6] : 0;
        Altar8 = values.Length > 7 ? values[7] : 0;
        Altar9 = values.Length > 8 ? values[8] : 0;
        Altar10 = values.Length > 9 ? values[9] : 0;
        Altar11 = values.Length > 10 ? values[10] : 0;
        Altar12 = values.Length > 11 ? values[11] : 0;
    }
}

public sealed class ConsortiaRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Placard { get; set; }
    public int Found { get; set; }
    public int Bijou { get; set; }
    public int Lv { get; set; }
    public int Honor { get; set; }
    public string? AltarInfo { get; set; }
}

public sealed class ConsortiaMemberView
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short FighterJobLv { get; set; }
    public short ScholarJobLv { get; set; }
    public short Profession { get; set; }
    public short Map { get; set; }
    public short ConsortiaJob { get; set; }
    public int ConsortiaContribute { get; set; }
}

public sealed class UserSkillBackupRecord
{
    public int UserId { get; set; }
    public string SkillIds { get; set; } = string.Empty;
    public string ItemIds { get; set; } = string.Empty;
    public int RSkillId { get; set; } = -1;
    public int RItemId { get; set; } = -1;
}

public sealed class UserPetRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "ZodiacPet";
    public short TemplateId { get; set; } = 1;
    public short Level { get; set; } = 1;
    public int Exp { get; set; }
    public short Quality { get; set; } = 1;
    public short Sex { get; set; }
    public short Savvy { get; set; } = 10;
    public short State { get; set; } = 1; // 1 = rest, 2 = summoned, 3 = merged, 4 = work
    public short Loyalty { get; set; } = 100;
    public short Satiety { get; set; } = 100;
    public short Rebirth { get; set; }
    public int Hp { get; set; } = 200;
    public int MaxHp { get; set; } = 200;
    public int Attack { get; set; } = 25;
    public int Defend { get; set; } = 12;
    public int MagicAttack { get; set; } = 25;
    public int MagicDefend { get; set; } = 12;
    public int Hit { get; set; } = 90;
}

public sealed class UserMentorRecord
{
    public string MasterName { get; set; } = string.Empty;
    public string PrenticeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
