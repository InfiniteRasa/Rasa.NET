using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// How far from its leader a squad's member goes in a fight (npc_squad_type.tether, NpcSquads):
    /// 0, no tether, for the rows there are, as they went before.
    ///
    /// Down drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270919000000_Npc_squad_tether")]
    public class Npc_squad_tether : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "tether",
                table: NpcSquadTypeEntry.TableName,
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ALTER TABLE ... DROP COLUMN (SQLite 3.35): EF's DropColumn rebuilds the table from a
            // target model, which these migrations have not got.
            migrationBuilder.Sql($"ALTER TABLE {NpcSquadTypeEntry.TableName} DROP COLUMN tether;");
        }
    }
}
