namespace GodsWar.Database.Models;

public sealed class CharacterBase
{
    public int Id { get; set; }
    public string Accounts { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = "male";
    public short Gm { get; set; }
    public short Camp { get; set; }
    public short Profession { get; set; }
    public short FighterJobLv { get; set; }
    public short ScholarJobLv { get; set; }
    public int FighterJobExp { get; set; }
    public int ScholarJobExp { get; set; }
    public int CurHp { get; set; }
    public int CurMp { get; set; }
    public short Status { get; set; }
    public short Belief { get; set; }
    public int Prestige { get; set; }
    public short EarlRank { get; set; }
    public int Consortia { get; set; }
    public short ConsortiaJob { get; set; }
    public int ConsortiaContribute { get; set; }
    public int StoreNum { get; set; }
    public int BagNum { get; set; }
    public short HairStyle { get; set; }
    public short? FaceShap { get; set; }
    public short Map { get; set; }
    public float PosX { get; set; }
    public float PosZ { get; set; }
    public int Money { get; set; }
    public int Stone { get; set; }
    public int SkillPoint { get; set; }
    public int SkillExp { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public DateTime RegisterTime { get; set; }
    public DateTime LastLoginTime { get; set; }
    public int? Mutetime { get; set; }
}

public sealed class CharacterKitbag
{
    public int UserId { get; set; }
    public string? Kitbag1 { get; set; }
    public string? Kitbag2 { get; set; }
    public string? Kitbag3 { get; set; }
    public string? Kitbag4 { get; set; }
    public string? Storage { get; set; }
    public string? Equip { get; set; }
}

public sealed record CreateCharacterParams(
    string Account,
    string Name,
    string Gender,
    short Camp,
    short Profession,
    short Belief,
    short HairStyle,
    short FaceShap,
    short Map,
    float PosX,
    float PosZ,
    int MaxHp,
    int MaxMp);

public enum CreateCharacterStatus
{
    Success = 1,
    NameTooLong = 2,
    DuplicateName = 5,
    LimitReached = 4,
}

public sealed record CreateCharacterResult(CreateCharacterStatus Status, CharacterBase? Character);
