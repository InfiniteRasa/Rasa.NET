using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Whether a squad's leader, as it is sent, is a bystander its members guard (npc_squad_type.leader_bystander,
    /// NpcSquads): 0, none, for the rows there are, as they went before.
    ///
    /// Down drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270925000000_Npc_squad_leader_bystander")]
    public class Npc_squad_leader_bystander : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "leader_bystander",
                table: NpcSquadTypeEntry.TableName,
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ALTER TABLE ... DROP COLUMN (SQLite 3.35): EF's DropColumn rebuilds the table from a
            // target model, which these migrations have not got.
            migrationBuilder.Sql($"ALTER TABLE {NpcSquadTypeEntry.TableName} DROP COLUMN leader_bystander;");
        }
    }
}
