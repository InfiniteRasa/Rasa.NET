using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Stats for the summoned turret and Howler: body/mind/spirit 15, health as the creature row,
    /// armour 150, at level 42.
    /// </summary>
    public class CreatureSummonStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 570001, 15, 15, 15, 3750, 150 };
            yield return new object[] { 570002, 15, 15, 15, 3750, 150 };
        }
    }
}
