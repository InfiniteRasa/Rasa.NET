using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    public partial class Bind_on_equip_items : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The character an item is bound to, 0 for none. No item has been bound this way
            // before, so every existing row starts at 0.
            migrationBuilder.AddColumn<uint>(
                name: "bound_character_id",
                table: "items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "bound_character_id", table: "items");
        }
    }
}
