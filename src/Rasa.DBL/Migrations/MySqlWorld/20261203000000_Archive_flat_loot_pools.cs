using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class Archive_flat_loot_pools : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("loot_group", newName: "legacy_flat_loot_group");
            migrationBuilder.RenameTable("loot_group_item", newName: "legacy_flat_loot_group_item");
            migrationBuilder.RenameTable("creature_loot_group", newName: "legacy_flat_creature_loot_group");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("legacy_flat_creature_loot_group", newName: "creature_loot_group");
            migrationBuilder.RenameTable("legacy_flat_loot_group_item", newName: "loot_group_item");
            migrationBuilder.RenameTable("legacy_flat_loot_group", newName: "loot_group");
        }
    }
}
