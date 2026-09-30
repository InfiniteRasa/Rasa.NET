using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The attacks of the creature grounds and open-country packs, 62 rows, one per (family, zone, attack),
    /// built as WorldCreatureActionPreloader's are: the family's own CR_* actions or weapon pair, the
    /// client's range, reuse and windup (a melee reaching at least 3 m, a ranged ability reusing at
    /// least 4 s, the Flaregasher's fireball 6 s), damage the client base x 2^((level-1)/8) x 0.25 or a
    /// weapon's 4/6/8% of a same-level player's base health. region_creature_actions.csv has sources.
    /// </summary>
    public class RegionCreatureActionPreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage", "damage_type"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 55001, "Warden Bot Thunderhead warden_bot_laser", 273, 1, 1.0, 60.0, 6000, 1999, 400, 600, 6 };
            yield return new object[] { 55002, "Warden Bot Thunderhead warden_bot_shock", 288, 1, 0.5, 5.0, 1000, 0, 304, 904, 13 };
            yield return new object[] { 55003, "Stalker Marshes 1/78", 1, 78, 1.0, 60.0, 1500, 0, 240, 360, 1 };
            yield return new object[] { 55004, "Warnet Soldier Plateau warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 59, 178, 13 };
            yield return new object[] { 55005, "Miasma Pools miasma_melee", 205, 1, 0.5, 3.0, 1000, 660, 92, 185, 3 };
            yield return new object[] { 55006, "Warnet Soldier Palisades warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 42, 126, 13 };
            yield return new object[] { 55007, "Fithik Palisades fithik_lunge", 405, 1, 1.0, 30.0, 15000, 1200, 84, 106, 1 };
            yield return new object[] { 55008, "Fithik Palisades fithik_acid_spray", 201, 1, 0.5, 5.0, 10000, 0, 64, 106, 1 };
            yield return new object[] { 55009, "Boargar Palisades boargar_stun", 182, 1, 0.5, 3.0, 4000, 1500, 52, 106, 7 };
            yield return new object[] { 55010, "Boargar Palisades 174/10", 174, 10, 0.5, 3.0, 800, 0, 62, 93, 1 };
            yield return new object[] { 55011, "Howler Palisades howler_sonic_attack", 200, 1, 1.0, 40.0, 10000, 925, 17, 50, 7 };
            yield return new object[] { 55012, "Howler Palisades howler_melee_attack", 402, 1, 0.5, 3.0, 4000, 0, 32, 42, 1 };
            yield return new object[] { 55013, "Howler Pools howler_sonic_attack", 200, 1, 1.0, 40.0, 10000, 925, 31, 92, 7 };
            yield return new object[] { 55014, "Howler Pools howler_melee_attack", 402, 1, 0.5, 3.0, 4000, 0, 58, 77, 1 };
            yield return new object[] { 55015, "Xanx Pools xanx_v1_web", 238, 1, 1.0, 40.0, 30000, 1060, 154, 308, 4 };
            yield return new object[] { 55016, "Xanx Pools xanx_melee", 437, 1, 0.5, 5.0, 1000, 0, 154, 194, 1 };
            yield return new object[] { 55017, "Mox Pools mox_energy_attack", 209, 1, 1.0, 40.0, 4000, 825, 84, 128, 13 };
            yield return new object[] { 55018, "Mox Pools 174/13", 174, 13, 0.5, 3.0, 800, 0, 185, 278, 1 };
            yield return new object[] { 55019, "Maw Marshes maw_melee", 422, 1, 0.5, 5.0, 1000, 0, 244, 493, 1 };
            yield return new object[] { 55020, "Mox Marshes mox_energy_attack", 209, 1, 1.0, 40.0, 4000, 825, 109, 166, 13 };
            yield return new object[] { 55021, "Mox Marshes 174/13", 174, 13, 0.5, 3.0, 800, 0, 240, 360, 1 };
            yield return new object[] { 55022, "Swamp Grubber Marshes 174/14", 174, 14, 0.5, 3.0, 800, 0, 147, 220, 1 };
            yield return new object[] { 55023, "Miasma Plateau miasma_melee", 205, 1, 0.5, 3.0, 1000, 660, 71, 142, 3 };
            yield return new object[] { 55024, "Mox Plateau mox_energy_attack", 209, 1, 1.0, 40.0, 4000, 825, 65, 98, 13 };
            yield return new object[] { 55025, "Mox Plateau 174/13", 174, 13, 0.5, 3.0, 800, 0, 143, 214, 1 };
            yield return new object[] { 55026, "Filcher Plateau filcher_melee", 207, 1, 0.5, 3.0, 4000, 1066, 178, 238, 1 };
            yield return new object[] { 55027, "Fithik Ashen Desert fithik_lunge", 405, 1, 1.0, 30.0, 15000, 1200, 336, 424, 1 };
            yield return new object[] { 55028, "Fithik Ashen Desert fithik_acid_spray", 201, 1, 0.5, 5.0, 10000, 0, 256, 424, 1 };
            yield return new object[] { 55029, "Tree Mite Ashen Desert treemite_spit", 196, 1, 1.0, 20.0, 4000, 825, 202, 404, 4 };
            yield return new object[] { 55030, "Xanx Ashen Desert xanx_v1_web", 238, 1, 1.0, 40.0, 30000, 1060, 336, 672, 4 };
            yield return new object[] { 55031, "Xanx Ashen Desert xanx_melee", 437, 1, 0.5, 5.0, 1000, 0, 336, 424, 1 };
            yield return new object[] { 55032, "Beam Manta Mires 1/209", 1, 209, 1.0, 20.0, 1500, 0, 190, 285, 1 };
            yield return new object[] { 55033, "Barb Tick Mires 174/17", 174, 17, 0.5, 3.0, 800, 0, 190, 285, 1 };
            yield return new object[] { 55034, "Flaregasher Mires flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 356, 498, 1 };
            yield return new object[] { 55035, "Flaregasher Mires flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 283, 356, 2 };
            yield return new object[] { 55036, "Flaregasher Incline flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 388, 543, 1 };
            yield return new object[] { 55037, "Flaregasher Incline flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 308, 388, 2 };
            yield return new object[] { 55038, "Flaregasher Plains flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 327, 456, 1 };
            yield return new object[] { 55039, "Flaregasher Plains flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 259, 327, 2 };
            yield return new object[] { 55040, "Beam Manta Plains 1/209", 1, 209, 1.0, 20.0, 1500, 0, 174, 262, 1 };
            yield return new object[] { 55041, "Fithik Plains fithik_lunge", 405, 1, 1.0, 30.0, 15000, 1200, 238, 300, 1 };
            yield return new object[] { 55042, "Fithik Plains fithik_acid_spray", 201, 1, 0.5, 5.0, 10000, 0, 181, 300, 1 };
            yield return new object[] { 55043, "Barb Tick Thunderhead 174/17", 174, 17, 0.5, 3.0, 800, 0, 269, 404, 1 };
            yield return new object[] { 55044, "Flaregasher Thunderhead flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 504, 704, 1 };
            yield return new object[] { 55045, "Flaregasher Thunderhead flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 400, 504, 2 };
            yield return new object[] { 55046, "Flaregasher Crucible flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 599, 837, 1 };
            yield return new object[] { 55047, "Flaregasher Crucible flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 476, 599, 2 };
            yield return new object[] { 55048, "Granitour Abyss granitour_melee", 471, 1, 0.5, 4.0, 1000, 0, 262, 436, 1 };
            yield return new object[] { 55049, "Granitour Abyss granitour_v1_spit", 472, 1, 1.0, 20.0, 4000, 466, 332, 550, 2 };
            yield return new object[] { 55050, "Flaregasher Abyss flaregasher_melee", 439, 1, 0.5, 3.0, 1000, 0, 550, 768, 1 };
            yield return new object[] { 55051, "Flaregasher Abyss flaregasher_fireball", 237, 1, 1.0, 40.0, 6000, 925, 436, 550, 2 };
            yield return new object[] { 55052, "Amoeboid Abyss amoeboid_melee", 431, 1, 0.5, 3.0, 1000, 0, 436, 550, 1 };
            yield return new object[] { 55053, "Amoeboid Abyss amoeboid_slime", 211, 1, 0.5, 3.0, 4000, 1800, 332, 436, 1 };
            yield return new object[] { 55054, "Miasma Howling Maw miasma_melee", 205, 1, 0.5, 3.0, 1000, 660, 285, 571, 3 };
            yield return new object[] { 55055, "Maw Howling Maw maw_melee", 422, 1, 0.5, 5.0, 1000, 0, 581, 1172, 1 };
            yield return new object[] { 55056, "Warnet Soldier Howling Maw warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 238, 714, 13 };
            yield return new object[] { 55057, "Mox Howling Maw mox_energy_attack", 209, 1, 1.0, 40.0, 4000, 825, 259, 394, 13 };
            yield return new object[] { 55058, "Mox Howling Maw 174/13", 174, 13, 0.5, 3.0, 800, 0, 571, 856, 1 };
            yield return new object[] { 55059, "Filcher Howling Maw filcher_melee", 207, 1, 0.5, 3.0, 4000, 1066, 714, 951, 1 };
            yield return new object[] { 55060, "Xanx Thunderhead xanx_v1_web", 238, 1, 1.0, 40.0, 30000, 1060, 367, 734, 4 };
            yield return new object[] { 55061, "Xanx Thunderhead xanx_melee", 437, 1, 0.5, 5.0, 1000, 0, 367, 462, 1 };
            yield return new object[] { 55062, "Beam Manta Thunderhead 1/209", 1, 209, 1.0, 20.0, 1500, 0, 269, 404, 1 };
        }
    }
}
