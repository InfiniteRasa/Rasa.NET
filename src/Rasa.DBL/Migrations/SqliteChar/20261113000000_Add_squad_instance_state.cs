using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The squad instances that stand (the game server's squad instances), so that they last
    /// through a restart: squad_instance, one row an instance, with its map, its owner's
    /// character and since when; squad_instance_pool, one row an instance and spawn pool whose
    /// creatures are all dead, with when the last of them died; and squad_instance_visitor, one
    /// row a character, the instance they last went into.
    /// </summary>
    public partial class Add_squad_instance_state : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "squad_instance",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    owner_character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_squad_instance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "squad_instance_pool",
                columns: table => new
                {
                    instance_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    spawnpool_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    cleared_at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_squad_instance_pool", x => new { x.instance_id, x.spawnpool_id });
                });

            migrationBuilder.CreateTable(
                name: "squad_instance_visitor",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    instance_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_squad_instance_visitor", x => x.character_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "squad_instance_visitor");
            migrationBuilder.DropTable(name: "squad_instance_pool");
            migrationBuilder.DropTable(name: "squad_instance");
        }
    }
}
