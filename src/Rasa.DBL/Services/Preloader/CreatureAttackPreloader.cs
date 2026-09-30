using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The creature abilities Wire_creature_attacks gives out, one row per (creature, ability):
    /// the client's CR_* action at argument 1, with its range, reuse and windup, and damage from
    /// its DAMAGE_AMOUNT_MIN/MAX scaled to the creature's level as the world rows are
    /// (x 2^((level - 1) / 8), x 0.25, a boss x 0.5). See that migration for who gets what.
    /// </summary>
    public class CreatureAttackPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 54001, "Cracked Tooth (Flaregasher boss) flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 924, 1291, 1 };
            yield return new object[] { 54002, "Cracked Tooth flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 734, 924, 2 };
            yield return new object[] { 54003, "Kennilaxx (Howler boss) howler_melee_attack", 402, 1, 0.5, 3.0, 4000, 0, 99, 130, 1 };
            yield return new object[] { 54004, "Kennilaxx (Howler boss) howler_sonic_attack", 200, 1, 1.0, 40.0, 10000, 925, 52, 156, 7 };
            yield return new object[] { 54005, "Goliath (Kael boss) kael_smash", 433, 1, 0.5, 3.0, 4000, 1033, 734, 1027, 1 };
            yield return new object[] { 54006, "Goliath (Kael boss) kael_ground_pound", 171, 1, 0.5, 3.0, 4000, 1233, 660, 880, 1 };
            yield return new object[] { 54007, "Goliath (Kael boss) kael_tectonic_strike", 497, 1, 1.0, 20.0, 3500, 1033, 293, 440, 7 };
            yield return new object[] { 54008, "Painrox (Kael boss) kael_smash", 433, 1, 0.5, 3.0, 4000, 1033, 2263, 3168, 1 };
            yield return new object[] { 54009, "Painrox (Kael boss) kael_ground_pound", 171, 1, 0.5, 3.0, 4000, 1233, 2036, 2715, 1 };
            yield return new object[] { 54010, "Painrox (Kael boss) kael_tectonic_strike", 497, 1, 1.0, 20.0, 3500, 1033, 905, 1358, 7 };
            yield return new object[] { 54011, "Kael Plateau kael_tectonic_strike", 497, 1, 1.0, 20.0, 3500, 1033, 104, 156, 7 };
            yield return new object[] { 54012, "Proctor Fulgor lightbender_quill", 202, 1, 1.0, 10.0, 4000, 2266, 22, 42, 1 };
            yield return new object[] { 54013, "Hygax (Lightbender boss) lightbender_quill", 202, 1, 1.0, 10.0, 4000, 2266, 67, 130, 1 };
            yield return new object[] { 54014, "Atta Imperial Guard atta_soldier_melee", 478, 1, 0.5, 4.0, 1000, 0, 1037, 2075, 1 };
            yield return new object[] { 54015, "Atta Imperial Guard atta_soldier_acid_spit", 479, 1, 1.0, 10.0, 5000, 0, 456, 934, 4 };
            yield return new object[] { 54016, "Atta Imperial Guard atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 1037, 2075, 1 };
            yield return new object[] { 54017, "The Red Kraken atta_soldier_melee", 478, 1, 0.5, 4.0, 1000, 0, 734, 1467, 1 };
            yield return new object[] { 54018, "The Red Kraken atta_soldier_acid_spit", 479, 1, 1.0, 10.0, 5000, 0, 323, 660, 4 };
            yield return new object[] { 54019, "The Red Kraken atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 734, 1467, 1 };
            yield return new object[] { 54020, "Tiamox (Atta Soldier boss) atta_soldier_melee", 478, 1, 0.5, 4.0, 1000, 0, 1131, 2263, 1 };
            yield return new object[] { 54021, "Tiamox (Atta Soldier boss) atta_soldier_acid_spit", 479, 1, 1.0, 10.0, 5000, 0, 498, 1018, 4 };
            yield return new object[] { 54022, "Tiamox (Atta Soldier boss) atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 1131, 2263, 1 };
            yield return new object[] { 54023, "Phuumz atta_harvester_acid_spit", 475, 1, 1.0, 20.0, 5000, 0, 734, 1027, 4 };
            yield return new object[] { 54024, "Atta Soldier Ashen Desert atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 367, 734, 1 };
            yield return new object[] { 54025, "Atta Soldier Incline atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 308, 617, 1 };
            yield return new object[] { 54026, "Atta Soldier Plains atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 259, 519, 1 };
            yield return new object[] { 54027, "Atta Soldier Thunderhead atta_soldier_rock_throw", 292, 1, 1.0, 50.0, 3000, 1366, 400, 800, 1 };
            yield return new object[] { 54028, "Arioch (Xanx boss) xanx_melee", 437, 1, 0.5, 5.0, 1000, 0, 84, 106, 1 };
            yield return new object[] { 54029, "Bane Xanx xanx_melee", 437, 1, 0.5, 5.0, 1000, 0, 21, 26, 1 };
            yield return new object[] { 54030, "Xanx Divide xanx_melee", 437, 1, 0.5, 5.0, 1000, 0, 42, 53, 1 };
        }
    }
}
