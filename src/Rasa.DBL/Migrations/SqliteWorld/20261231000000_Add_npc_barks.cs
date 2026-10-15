using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// npc_bark: the barks an NPC says now and then (the game server's NpcBarks), by creature row.
    /// An NPC with no row never barks, so every NPC is unchanged until it is given one.
    ///
    /// And the one row known: Outpost Commander Rogers's bark 94 (NpcBarkPreloader).
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261231000000_Add_npc_barks")]
    public class Add_npc_barks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: NpcBarkEntry.TableName,
                columns: table => new
                {
                    creature_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    bark_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_bark", x => new { x.creature_id, x.bark_id });
                });

            new NpcBarkPreloader().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: NpcBarkEntry.TableName);
        }
    }
}
