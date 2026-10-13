using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Fithik's bite where it was missing or reached too far (Fithik_bites).
    ///
    /// A Fithik's own attack is its weapon's, Weapon_Creature_Fithik (4314): WEAPON_MELEE 34
    /// (174/34), whose action_level gives a max range of 3 m and whose recovery plays
    /// ABILITY_CREATURE_FITHIK_MELEE_RESOLVE (animation family 734, FX family 309) on both Fithik
    /// skeletons. The Winged Fithik (72001) and the egg-cluster hatchling (73101) bite with it.
    ///
    ///  - The region Fithiks (Crucible 531022, Palisades 531068 and 540006, Ashen Desert 540018,
    ///    Plains 540027) were given the family's CR_* abilities and not its weapon, though the
    ///    same generator gave the Boargar, Mox and Swamp Grubber theirs (174/10, 174/13, 174/14,
    ///    0.5-3 m, 800 ms). With only CR_FITHIK_LUNGE (15 s) and CR_FITHIK_ACID_SPRAY (10 s), a
    ///    Fithik lunged from as far as 30 m, came in to five metres and stood there between the
    ///    two, ten seconds and more at a time. Each now has the bite in its free fourth slot, at
    ///    the generator's rank-and-file weapon damage for its level (the Boargar's at 23, the
    ///    Beam Manta's at 35, the Thrax Soldier's at 39 and 42).
    ///  - Row 44, the bite of the Fithik Hive Monarch (89) and the Fithik Hive Master (520038),
    ///    reached 0.5-10 m: both bosses stood nine metres off and bit, hitting a player with an
    ///    animation playing at a distance. It reaches 3 m, the client's range for the pair.
    ///
    /// Guarded: a row already there is not inserted twice, nor one for a creature the world does
    /// not have; a slot already used is not taken, and row 44 is changed only from what it was.
    /// Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class FithikBites
    {
        public const uint WeaponMelee = 174;
        public const uint FithikArg = 34;
        public const double RangeMin = 0.5;
        public const double RangeMax = 3.0;
        public const uint Cooldown = 800;

        public const uint IdMin = 74001;
        public const uint IdMax = 74005;

        /// <summary>The bosses' bite (Fithik Hive Monarch 89, Fithik Hive Master 520038) and the range it had.</summary>
        public const uint BossBite = 44;
        public const double BossBiteWas = 10.0;

        public sealed record Bite(uint Id, uint Creature, string Description, uint MinDamage, uint MaxDamage);

        public static readonly Bite[] Bites =
        {
            new(74001, 531022, "Fithik Crucible weapon 174/34", 320, 480),          // level 42
            new(74002, 531068, "Fithik Palisades weapon 174/34", 62, 93),           // level 23
            new(74003, 540006, "Fithik Palisades weapon 174/34", 62, 93),           // level 23
            new(74004, 540018, "Fithik Ashen Desert weapon 174/34", 247, 370),      // level 39
            new(74005, 540027, "Fithik Plains weapon 174/34", 174, 262),            // level 35
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var bite in Bites)
            {
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {bite.Id}, '{bite.Description}', {WeaponMelee}, {FithikArg}, {F(RangeMin)}, {F(RangeMax)}, {Cooldown}, 0, {bite.MinDamage}, {bite.MaxDamage}, 1 " +
                    $"FROM creature c WHERE c.id = {bite.Creature} AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {bite.Id});";

                yield return $"UPDATE creature SET action4 = {bite.Id} WHERE id = {bite.Creature} AND action4 = 0;";
            }

            yield return $"UPDATE creature_action SET range_max = {F(RangeMax)} " +
                $"WHERE id = {BossBite} AND action_id = {WeaponMelee} AND action_arg_id = {FithikArg} AND range_max = {F(BossBiteWas)};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE creature_action SET range_max = {F(BossBiteWas)} " +
                $"WHERE id = {BossBite} AND action_id = {WeaponMelee} AND action_arg_id = {FithikArg} AND range_max = {F(RangeMax)};";

            foreach (var bite in Bites)
                yield return $"UPDATE creature SET action4 = 0 WHERE id = {bite.Creature} AND action4 = {bite.Id};";

            yield return $"DELETE FROM creature_action WHERE id BETWEEN {IdMin} AND {IdMax};";
        }

        private static string F(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
