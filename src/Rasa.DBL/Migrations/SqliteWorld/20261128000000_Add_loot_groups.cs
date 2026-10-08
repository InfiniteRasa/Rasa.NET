using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The loot pools: loot_group, a named pool; loot_group_item, the items in it, each with a
    /// chance in percent and a quantity from min to max; creature_loot_group, the pools a
    /// creature row has. Every item of every pool a creature has is rolled on its own when it
    /// is killed (the game server's LootPools).
    ///
    /// The tables are the ones gametools' Loot Table Editor writes, by its SQL export or through
    /// the REST API (POST /updatelootpools), and they are made empty: a creature with no pool
    /// drops what it always has, so nothing changes until pools are given.
    ///
    /// Down drops the three tables.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_loot_groups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loot_group",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    comment = table.Column<string>(type: "varchar(200)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loot_group_item",
                columns: table => new
                {
                    group_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    item_template_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    chance = table.Column<double>(type: "REAL", nullable: false),
                    min_quantity = table.Column<uint>(type: "INTEGER", nullable: false),
                    max_quantity = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_group_item", x => new { x.group_id, x.item_template_id });
                });

            migrationBuilder.CreateTable(
                name: "creature_loot_group",
                columns: table => new
                {
                    creature_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    group_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_loot_group", x => new { x.creature_id, x.group_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "creature_loot_group");
            migrationBuilder.DropTable(name: "loot_group_item");
            migrationBuilder.DropTable(name: "loot_group");
        }
    }
}
