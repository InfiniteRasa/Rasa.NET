using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>map_emitter: FX packages placed on maps (MapEmitterEntry). Nothing to seed - no .map places one.</summary>
    public partial class Add_map_emitter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: MapEmitterEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    pos_x = table.Column<double>(type: "REAL", nullable: false),
                    pos_y = table.Column<double>(type: "REAL", nullable: false),
                    pos_z = table.Column<double>(type: "REAL", nullable: false),
                    rotation = table.Column<double>(type: "REAL", nullable: false),
                    package_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    is_on = table.Column<byte>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "varchar(96)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_emitter", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: MapEmitterEntry.TableName);
        }
    }
}
