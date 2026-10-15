using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The barks NPCs are seeded with (npc_bark). The client has 1718 bark lines and nothing that
    /// says whose they are; a row is given only where who says it is known.
    ///
    /// Outpost Commander Rogers at Alia Das (creature 100) has bark 94, "I've got reports of
    /// heavy fire on the river front. Let's get reinforcements down there, a-sap!": his, by
    /// footage of the live game.
    /// </summary>
    public class NpcBarkPreloader : PreloaderBase, IPreloader
    {
        public const uint OutpostCommanderRogers = 100;
        public const uint RiverFrontReinforcements = 94;

        /// <summary>
        /// Plain SQL: Add_npc_barks has no target model (no Designer), and EF builds an InsertData
        /// only against a model that maps the table.
        /// </summary>
        public void Preload(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in Up())
                migrationBuilder.Sql(sql);
        }

        /// <summary>The rows' inserts, one statement each, valid on SQLite and MySQL alike.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var row in Rows())
                yield return $"INSERT INTO {NpcBarkEntry.TableName} (creature_id, bark_id) VALUES ({row[0]}, {row[1]});";
        }

        /// <summary>The rows, as (creature id, bark id).</summary>
        public static IEnumerable<object[]> Rows()
        {
            yield return new object[] { OutpostCommanderRogers, RiverFrontReinforcements };
        }

        protected override IEnumerable<object[]> GetRows() => Rows();
    }
}
