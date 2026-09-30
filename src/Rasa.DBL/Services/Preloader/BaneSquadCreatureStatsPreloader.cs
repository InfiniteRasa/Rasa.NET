using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Stats for the Wilderness arrival squad rows: body/mind/spirit 15, armour (10 x level - 20) x 0.5.
    /// </summary>
    public class BaneSquadCreatureStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 560001, 15, 15, 15, 469, 40 };
            yield return new object[] { 560002, 15, 15, 15, 469, 40 };
        }
    }
}
