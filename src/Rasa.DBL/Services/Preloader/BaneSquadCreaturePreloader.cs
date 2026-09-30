using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Thrax soldier and grenadier for Wilderness's arrival squads (Pravus Research): level 10 (band
    /// top 15 minus 5), health a same-level player's base health x 0.75, the actions Divide's rows use.
    /// </summary>
    public class BaneSquadCreaturePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, typeof(CreatureEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 560001, "Thrax Soldier - Wilderness", 20757, 0, 10, 469, 235, 9, 5, 2, 3, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 560002, "Thrax Grenadier - Wilderness", 9354, 0, 10, 469, 0, 9, 5, 19, 20, 0, 0, 0, 0, 0, 0 };
        }
    }
}
