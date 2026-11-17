namespace GodsWar.GameServer.Game;

public enum AttackResultType : byte
{
    Crit = 0,
    Hit = 1,
    Miss = 2,
}

/// <summary>
/// Combat, damage, hit/crit/miss, and XP formulas ported from
/// <c>Combat.cpp</c>, <c>GameObject.cpp</c>, and <c>PlayerAttack.cpp</c>.
/// </summary>
public static class CombatCalculator
{
    /// <summary>
    /// Determines whether an attack is a Miss (2), Hit (1), or Crit/Force (0).
    /// </summary>
    public static AttackResultType JudgeAttackStyle(
        int attackerHit,
        int targetDodge,
        int attackerCritAppend,
        int targetCritDefend,
        Random? rng = null)
    {
        rng ??= Random.Shared;

        // Base hit chance 85%, adjusted by (attackerHit - targetDodge), clamped [15%, 98%]
        int hitChance = Math.Clamp(85 + attackerHit - targetDodge, 15, 98);
        if (rng.Next(100) >= hitChance)
        {
            return AttackResultType.Miss;
        }

        // Base crit chance 5%, adjusted by (attackerCritAppend - targetCritDefend), clamped [1%, 50%]
        int critChance = Math.Clamp(5 + attackerCritAppend - targetCritDefend, 1, 50);
        if (rng.Next(100) < critChance)
        {
            return AttackResultType.Crit;
        }

        return AttackResultType.Hit;
    }

    /// <summary>
    /// Port of <c>CGameObject::CalculateAttackDamage(CGameObject&amp;, float iPower1, int iPower2)</c>
    /// and <c>Combat.cpp</c> crit multiplier (1.5x).
    /// </summary>
    public static int CalculatePhysicalDamage(
        int attack,
        int defend,
        float phyDamageAppend = 0f,
        float power1 = 0f,
        int power2 = 0,
        int damageSorb = 0,
        float createAttackDamage = 0f,
        float beAttackDamage = 0f,
        bool isCrit = false)
    {
        int firstDamage = Math.Max(0, attack - defend);
        double raw = (firstDamage * (1.0 + phyDamageAppend) * (1.0 + power1) + power2 - damageSorb)
                     * (1.0 + createAttackDamage - beAttackDamage);

        int damage = raw <= 0 ? 1 : (int)raw;
        if (isCrit)
        {
            damage = Math.Max(1, (int)(damage * 1.5));
        }

        return damage;
    }

    /// <summary>
    /// Port of <c>CGameObject::CalculateAttackDamage(int MagicID, CGameObject&amp;)</c> for magical skills (<c>Property == 1</c>).
    /// </summary>
    public static int CalculateMagicDamage(
        int magicAttack,
        int magicDefend,
        float magicDamageAppend = 0f,
        float power1 = 0f,
        float power2 = 0f,
        int damageSorb = 0,
        float createMagicDamage = 0f,
        float beAttackDamage = 0f,
        bool isCrit = false)
    {
        int firstDamage = Math.Max(0, magicAttack - magicDefend);
        double raw = (firstDamage * (1.0 + magicDamageAppend) * (1.0 + power1) + power2 - damageSorb)
                     * (1.0 + createMagicDamage - beAttackDamage);

        int damage = raw <= 0 ? 1 : (int)raw;
        if (isCrit)
        {
            damage = Math.Max(1, (int)(damage * 1.5));
        }

        return damage;
    }

    /// <summary>
    /// Port of solo branch of <c>CPlayer::CalculateExpAndLevel</c> in <c>PlayerAttack.cpp</c>.
    /// </summary>
    public static uint CalculateSoloExp(int monsterExp, int playerLevel, int monsterLevel, float expBonus = 0f)
    {
        return CalculateTeamExp(monsterExp, monsterLevel, playerLevel, playerLevel, 1, expBonus);
    }

    /// <summary>
    /// Port of <c>CPlayer::CalculateExpAndLevel</c> in <c>PlayerAttack.cpp</c>.
    /// </summary>
    public static uint CalculateTeamExp(
        int monsterExp,
        int monsterLevel,
        int memberLevel,
        int teamLevelSum,
        int memberCount,
        float expBonus = 0f)
    {
        if (monsterExp <= 0 || memberCount <= 0 || teamLevelSum <= 0)
        {
            return 0;
        }

        float factor = (memberCount - 1) * 0.25f + 1.0f;
        int avgTeamLevel = teamLevelSum / memberCount;
        float teamLevelFactor = Math.Clamp(1.0f + (monsterLevel - avgTeamLevel) / 10.0f, 0.0f, 1.0f);
        float memLevelFactor = (float)(memberLevel + avgTeamLevel) / (teamLevelSum + avgTeamLevel * memberCount);

        double tempExp = monsterExp * teamLevelFactor * memLevelFactor * factor * (1.0 + expBonus);
        return tempExp < 1.0 ? 1u : (uint)tempExp;
    }
}
