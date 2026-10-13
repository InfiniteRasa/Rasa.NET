using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// world_usable: the usable world objects the server puts on maps (WorldUsableEntry; the game
    /// server's PlacedUsables) - switches, doors, item dispensers and acceptors, random
    /// explosions, bombs - with how each starts and what it does. No rows: they are put in by
    /// later migrations (WorldUsablePreloader).
    ///
    /// Down drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_world_usables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: WorldUsableEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    state = table.Column<uint>(type: "int unsigned", nullable: false),
                    usable = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_count = table.Column<uint>(type: "int unsigned", nullable: false),
                    reset_seconds = table.Column<uint>(type: "int unsigned", nullable: false),
                    interval_min_seconds = table.Column<uint>(type: "int unsigned", nullable: false),
                    interval_max_seconds = table.Column<uint>(type: "int unsigned", nullable: false),
                    damage_min = table.Column<uint>(type: "int unsigned", nullable: false),
                    damage_max = table.Column<uint>(type: "int unsigned", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: false),
                    action_arg = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_usable", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: WorldUsableEntry.TableName);
        }
    }
}
