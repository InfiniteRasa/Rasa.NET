using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Caves of Donn's Fithik (566017, level 9) given the lunge and acid spray every other
    /// Winged Fithik has (FamilyAbilities, Donn_fithik_abilities): it had only the bite and the
    /// self-destruct.
    ///
    /// The rows are FamilyAbilities' for the Wilderness Fithik (76008, 76009) at its level: the
    /// argument's DAMAGE_AMOUNT_MIN/MAX (lunge 50-63, acid spray 38-63) x 2^((9 - 1) / 8) x 0.25,
    /// halves rounded up. They go in slots 3 and 4, which are empty.
    ///
    /// Guarded: a row is inserted once, and only with the creature; a slot is filled only while it
    /// is empty. Down empties a slot only while it holds its row. Plain SQL, valid on SQLite and
    /// MySQL alike.
    /// </summary>
    public static class DonnFithikAbilities
    {
        public const uint Fithik = CavesOfDonnContent.Fithik;
        public const uint Lunge = 566103, AcidSpray = 566104;
        public const string OldId = "20270315000000_Donn_fithik_abilities";
        public const string ForgetOldId =
            "DELETE FROM __EFMigrationsHistory WHERE MigrationId = '" + OldId + "';";

        /// <summary>A creature_action row and the slot of the Fithik it goes in.</summary>
        public sealed record Row(uint Id, int Slot, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        public static readonly Row[] Rows =
        {
            new(Lunge, 3, "Fithik - Caves of Donn 566017 fithik_lunge", 405, 1, 1.0, 30.0, 15000, 1200, 25, 32, 1),
            new(AcidSpray, 4, "Fithik - Caves of Donn 566017 fithik_acid_spray", 201, 1, 0.5, 5.0, 10000, 0, 19, 32, 1)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var row in Rows)
            {
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {row.Id}, '{row.Description}', {row.ActionId}, {row.ArgId}, {N(row.RangeMin)}, {N(row.RangeMax)}, {row.Cooldown}, {row.Windup}, " +
                    $"{row.MinDamage}, {row.MaxDamage}, {row.DamageType} FROM creature c WHERE c.id = {Fithik} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {row.Id});";

                yield return $"UPDATE creature SET action{row.Slot} = {row.Id} WHERE id = {Fithik} AND action{row.Slot} = 0 " +
                    $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {row.Id});";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var row in Rows)
            {
                yield return $"UPDATE creature SET action{row.Slot} = 0 WHERE id = {Fithik} AND action{row.Slot} = {row.Id};";
                yield return $"DELETE FROM creature_action WHERE id = {row.Id};";
            }
        }

        private static string N(double value) => value.ToString("0.0##", CultureInfo.InvariantCulture);
    }
}
