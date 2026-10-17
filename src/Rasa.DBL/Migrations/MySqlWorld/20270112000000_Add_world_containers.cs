using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// world_container: the lootable containers the server puts on maps (the game server's
    /// WorldContainers), and world_container_item: the items each always holds. Both start empty;
    /// a container is a row each, given.
    ///
    /// Down drops both.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270112000000_Add_world_containers")]
    public class Add_world_containers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: WorldContainerEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    loot_profile_key = table.Column<string>(type: "varchar(64)", nullable: true),
                    cipher_level = table.Column<uint>(type: "int unsigned", nullable: false),
                    reset_seconds = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(200)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_container", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: WorldContainerItemEntry.TableName,
                columns: table => new
                {
                    container_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    quantity = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_container_item", x => new { x.container_id, x.item_template_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: WorldContainerItemEntry.TableName);
            migrationBuilder.DropTable(name: WorldContainerEntry.TableName);
        }
    }
}
