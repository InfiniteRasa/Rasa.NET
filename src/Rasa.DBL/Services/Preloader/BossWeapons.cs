using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The weapon swings of three bosses, which hit for placeholder damage (Boss_weapons).
    ///
    ///  - Shahrbaraz (520044, Boargar boss, level 28) swung row 4, the Young Boargar's WEAPON_MELEE
    ///    10 at 10-20, 1-4 m, every 2.5 s.
    ///  - The Fithik Hive Monarch (89, level 10) and Hive Master (520038, level 28) bit with row 44,
    ///    WEAPON_MELEE 34 at 15-20.
    ///  - The Mox Alpha Male (520053, level 32) swung row 22, the level 6 Mox's WEAPON_MELEE 13 at
    ///    16-24.
    ///
    /// Each gets a row of its own in that slot: the same weapon pair, with the reach and reuse of
    /// its family's zone rows (0.5-3 m, 800 ms, no windup) and their damage type, and damage as
    /// Wire_boss_attacks sets a boss's weapon: 8-12% of a same-level player's base health, the
    /// share the zone rows are built on (HealthBaselinePerLevel x 0.8: 2378.5 at 28, 500.0 at 10,
    /// 3363.6 at 32). The rows they had stay as they are.
    ///
    /// Guarded: a row is inserted once and only with its boss, and a slot changes only from the
    /// row it had. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class BossWeapons
    {
        public const uint IdMin = 77001;
        public const uint IdMax = 77004;

        public sealed record Weapon(uint Creature, uint Id, string Description, uint ArgId, uint MinDamage, uint MaxDamage, uint DamageType, uint Was);

        public const uint WeaponMelee = 174;

        public static readonly Weapon[] Weapons =
        {
            new(520044, 77001, "Shahrbaraz (Boargar boss) 520044 weapon 174/10", 10, 190, 285, 1, 4),          // level 28
            new(89, 77002, "Fithik Hive Monarch (Fithik boss) 89 weapon 174/34", 34, 40, 60, 1, 44),            // level 10
            new(520038, 77003, "Fithik Hive Master (Fithik boss) 520038 weapon 174/34", 34, 190, 285, 1, 44),   // level 28
            new(520053, 77004, "Mox Alpha Male (Mox boss) 520053 weapon 174/13", 13, 269, 404, 13, 22),         // level 32
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var weapon in Weapons)
            {
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {weapon.Id}, '{weapon.Description}', {WeaponMelee}, {weapon.ArgId}, 0.5, 3.0, 800, 0, {weapon.MinDamage}, {weapon.MaxDamage}, {weapon.DamageType} " +
                    $"FROM creature c WHERE c.id = {weapon.Creature} AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {weapon.Id});";

                yield return $"UPDATE creature SET action1 = {weapon.Id} WHERE id = {weapon.Creature} AND action1 = {weapon.Was} " +
                    $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {weapon.Id});";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var weapon in Weapons)
                yield return $"UPDATE creature SET action1 = {weapon.Was} WHERE id = {weapon.Creature} AND action1 = {weapon.Id};";

            yield return $"DELETE FROM creature_action WHERE id BETWEEN {IdMin} AND {IdMax};";
        }
    }
}
