using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The creatures of 5 creature grounds and 191 open-country packs: one row per (family, zone), 40 rows. Levels from the zone
    /// band top minus 5/4/2 for minion/thug/lieutenant; health the player's base health at that
    /// level x 0.75/1.0/1.75; the family's standard class and its name ladder's rung for the zone.
    /// region_creatures.csv has every row.
    /// </summary>
    public class RegionCreaturePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, typeof(CreatureEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 540001, "Warden Bot - Thunderhead", 6961, 0, 41, 9170, 8047, 9, 5, 55001, 55002, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540002, "Stalker - Marshes", 3781, 0, 34, 5000, 0, 9, 5, 55003, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540003, "Warnet Soldier - Plateau", 6262, 0, 27, 2045, 460, 9, 5, 55004, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540004, "Miasma - Pools", 6170, 0, 30, 2652, 429, 9, 5, 55005, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540005, "Warnet Soldier - Palisades", 6262, 0, 23, 1446, 460, 9, 5, 55006, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540006, "Fithik - Palisades", 4313, 0, 23, 1446, 372, 9, 5, 55007, 55008, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540007, "Boargar - Palisades", 6031, 0, 23, 1446, 7805, 9, 5, 55009, 55010, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540008, "Howler - Palisades", 7336, 0, 23, 1446, 8152, 9, 5, 55011, 55012, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540009, "Howler - Pools", 7336, 0, 30, 2652, 8154, 9, 5, 55013, 55014, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540010, "Xanx - Pools", 7510, 0, 30, 2652, 467, 9, 5, 55015, 55016, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540011, "Mox - Pools", 6021, 0, 31, 3856, 8797, 9, 5, 55017, 55018, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540012, "Maw - Marshes", 6332, 0, 34, 5000, 425, 9, 5, 55019, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540013, "Mox - Marshes", 6021, 0, 34, 5000, 8797, 9, 5, 55020, 55021, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540014, "Swamp Grubber - Marshes", 6167, 0, 33, 3439, 441, 9, 5, 55022, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540015, "Miasma - Plateau", 6170, 0, 27, 2045, 428, 9, 5, 55023, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540016, "Mox - Plateau", 6021, 0, 28, 2973, 432, 9, 5, 55024, 55025, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540017, "Filcher - Plateau", 6421, 0, 27, 2045, 369, 9, 5, 55026, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540018, "Fithik - Ashen Desert", 4313, 0, 39, 5783, 9170, 9, 5, 55027, 55028, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540019, "Tree Mite - Ashen Desert", 6040, 0, 39, 5783, 7860, 9, 5, 55029, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540020, "Xanx - Ashen Desert", 7510, 0, 39, 5783, 471, 9, 5, 55030, 55031, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540021, "Beam Manta - Mires", 6441, 0, 36, 4460, 8022, 9, 5, 55032, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540022, "Barb Tick - Mires", 7236, 0, 36, 4460, 8142, 9, 5, 55033, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540023, "Flaregasher - Mires", 7338, 0, 37, 6484, 9372, 9, 5, 55034, 55035, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540024, "Flaregasher - Incline", 7338, 0, 38, 7071, 9372, 9, 5, 55036, 55037, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540025, "Flaregasher - Plains", 7338, 0, 36, 5946, 9372, 9, 5, 55038, 55039, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540026, "Beam Manta - Plains", 6441, 0, 35, 4089, 8022, 9, 5, 55040, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540027, "Fithik - Plains", 4313, 0, 35, 4089, 374, 9, 5, 55041, 55042, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540028, "Barb Tick - Thunderhead", 7236, 0, 40, 6307, 8143, 9, 5, 55043, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540029, "Flaregasher - Thunderhead", 7338, 0, 41, 9170, 9372, 9, 5, 55044, 55045, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540030, "Flaregasher - Crucible", 7338, 0, 43, 10905, 9372, 9, 5, 55046, 55047, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540031, "Granitour - Abyss", 10164, 0, 42, 10000, 0, 9, 5, 55048, 55049, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540032, "Flaregasher - Abyss", 7338, 0, 42, 10000, 9372, 9, 5, 55050, 55051, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540033, "Amoeboid - Abyss", 6032, 0, 42, 10000, 7822, 9, 5, 55052, 55053, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540034, "Miasma - Howling Maw", 6170, 0, 43, 8179, 7908, 9, 5, 55054, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540035, "Maw - Howling Maw", 6332, 0, 44, 11892, 425, 9, 5, 55055, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540036, "Warnet Soldier - Howling Maw", 6262, 0, 43, 8179, 460, 9, 5, 55056, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540037, "Mox - Howling Maw", 6021, 0, 44, 11892, 8797, 9, 5, 55057, 55058, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540038, "Filcher - Howling Maw", 6421, 0, 43, 8179, 8006, 9, 5, 55059, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540039, "Xanx - Thunderhead", 7510, 0, 40, 6307, 471, 9, 5, 55060, 55061, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 540040, "Beam Manta - Thunderhead", 6441, 0, 40, 6307, 8023, 9, 5, 55062, 0, 0, 0, 0, 0, 0, 0 };
        }
    }
}
