using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Stats for the region creatures: body/mind/spirit 15, health as the creature row, armour
    /// (10 x level - 20) x 0.5/0.75/1.0 by tier.
    /// </summary>
    public class RegionCreatureStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 540001, 15, 15, 15, 9170, 292 };
            yield return new object[] { 540002, 15, 15, 15, 5000, 240 };
            yield return new object[] { 540003, 15, 15, 15, 2045, 125 };
            yield return new object[] { 540004, 15, 15, 15, 2652, 140 };
            yield return new object[] { 540005, 15, 15, 15, 1446, 105 };
            yield return new object[] { 540006, 15, 15, 15, 1446, 105 };
            yield return new object[] { 540007, 15, 15, 15, 1446, 105 };
            yield return new object[] { 540008, 15, 15, 15, 1446, 105 };
            yield return new object[] { 540009, 15, 15, 15, 2652, 140 };
            yield return new object[] { 540010, 15, 15, 15, 2652, 140 };
            yield return new object[] { 540011, 15, 15, 15, 3856, 218 };
            yield return new object[] { 540012, 15, 15, 15, 5000, 240 };
            yield return new object[] { 540013, 15, 15, 15, 5000, 240 };
            yield return new object[] { 540014, 15, 15, 15, 3439, 155 };
            yield return new object[] { 540015, 15, 15, 15, 2045, 125 };
            yield return new object[] { 540016, 15, 15, 15, 2973, 195 };
            yield return new object[] { 540017, 15, 15, 15, 2045, 125 };
            yield return new object[] { 540018, 15, 15, 15, 5783, 185 };
            yield return new object[] { 540019, 15, 15, 15, 5783, 185 };
            yield return new object[] { 540020, 15, 15, 15, 5783, 185 };
            yield return new object[] { 540021, 15, 15, 15, 4460, 170 };
            yield return new object[] { 540022, 15, 15, 15, 4460, 170 };
            yield return new object[] { 540023, 15, 15, 15, 6484, 262 };
            yield return new object[] { 540024, 15, 15, 15, 7071, 270 };
            yield return new object[] { 540025, 15, 15, 15, 5946, 255 };
            yield return new object[] { 540026, 15, 15, 15, 4089, 165 };
            yield return new object[] { 540027, 15, 15, 15, 4089, 165 };
            yield return new object[] { 540028, 15, 15, 15, 6307, 190 };
            yield return new object[] { 540029, 15, 15, 15, 9170, 292 };
            yield return new object[] { 540030, 15, 15, 15, 10905, 308 };
            yield return new object[] { 540031, 15, 15, 15, 10000, 300 };
            yield return new object[] { 540032, 15, 15, 15, 10000, 300 };
            yield return new object[] { 540033, 15, 15, 15, 10000, 300 };
            yield return new object[] { 540034, 15, 15, 15, 8179, 205 };
            yield return new object[] { 540035, 15, 15, 15, 11892, 315 };
            yield return new object[] { 540036, 15, 15, 15, 8179, 205 };
            yield return new object[] { 540037, 15, 15, 15, 11892, 315 };
            yield return new object[] { 540038, 15, 15, 15, 8179, 205 };
            yield return new object[] { 540039, 15, 15, 15, 6307, 190 };
            yield return new object[] { 540040, 15, 15, 15, 6307, 190 };
        }
    }
}
