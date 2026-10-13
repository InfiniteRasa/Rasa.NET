namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Seymour's bite (Maw_boss_bite).
    ///
    /// Seymour (520036, Creature_Maw_Boss, level 38, Torden Marshes) fought with row 70008,
    /// Fix_creature_weapons' WEAPON_MELEE 9 (174/9, Weapon_Creature_Maw) at the 5-12 damage of
    /// the row it replaced, while a level 34 Maw bites for 244-493: a boss next to harmless. Every
    /// other Maw attacks with CR_MAW_MELEE 422/1, the Maw's own ability (MawMeleeAbility), and so
    /// does Seymour now, from a row of its own in the slot 70008 had.
    ///
    /// The numbers are the world Maws' (0.5-5 m, 1 s reuse, no windup, physical) and damage as
    /// Wire_creature_attacks gives a boss: the argument's DAMAGE_AMOUNT_MIN/MAX (56-113) x
    /// 2^((level - 1) / 8) x 0.5, twice what its kin hit for at its level.
    ///
    /// Guarded: the row is not inserted twice, nor without Seymour, and the slot changes only
    /// from 70008. Row 70008 stays as it was. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class MawBossBite
    {
        public const uint Seymour = 520036;
        public const uint Row = 75001;
        public const uint OldRow = 70008;

        public const uint MawMelee = 422;
        public const uint MawMeleeArg = 1;
        public const uint MinDamage = 691;
        public const uint MaxDamage = 1394;

        public static readonly string[] Up =
        {
            "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
            $"SELECT {Row}, 'Seymour (Maw boss) maw_melee', {MawMelee}, {MawMeleeArg}, 0.5, 5.0, 1000, 0, {MinDamage}, {MaxDamage}, 1 " +
            $"FROM creature c WHERE c.id = {Seymour} AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {Row});",

            $"UPDATE creature SET action1 = {Row} WHERE id = {Seymour} AND action1 = {OldRow} " +
            $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {Row});"
        };

        public static readonly string[] Down =
        {
            $"UPDATE creature SET action1 = {OldRow} WHERE id = {Seymour} AND action1 = {Row};",
            $"DELETE FROM creature_action WHERE id = {Row};"
        };
    }
}
