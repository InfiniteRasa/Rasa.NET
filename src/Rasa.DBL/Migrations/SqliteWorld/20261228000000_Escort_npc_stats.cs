using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The NPCs whose death fails the mission they are escorted in - Matthew Corman, Sgt. Pierre,
    /// Ranger Milpas and the Alia escort ranger - are given 4000 health and 300 armour
    /// (EscortNpcStats). Matthew had no creature_stat row and spawned with 100 health; the others
    /// had 600 to 1000. Data only, in plain SQL: no model of its own.
    ///
    /// Down puts back what they had, only where a row is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261228000000_Escort_npc_stats")]
    public class Escort_npc_stats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in EscortNpcStats.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in EscortNpcStats.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
