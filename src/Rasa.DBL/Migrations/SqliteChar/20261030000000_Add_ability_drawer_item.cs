using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// A usable item dragged onto the ability tray - a pet, a medpack - keeps the item it came
    /// from with the slot (item.id), so the tray fires that item's action with the item, and the
    /// slot is still the item's after a relog. Null for a skill's ability, which every existing
    /// slot is.
    /// </summary>
    public partial class Add_ability_drawer_item : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "item_id",
                table: "character_ability_drawer",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "item_id", table: "character_ability_drawer");
        }
    }
}
