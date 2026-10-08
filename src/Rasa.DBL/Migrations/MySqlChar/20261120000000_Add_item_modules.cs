using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// items.module_1 to module_4: the module in each of an item's four module slots - an id of
    /// the world database's module_class - or 0 for an empty slot. No item has carried a module
    /// before, so every existing row starts with four empty slots.
    /// </summary>
    public partial class Add_item_modules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "module_1",
                table: "items",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "module_2",
                table: "items",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "module_3",
                table: "items",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "module_4",
                table: "items",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "module_4", table: "items");
            migrationBuilder.DropColumn(name: "module_3", table: "items");
            migrationBuilder.DropColumn(name: "module_2", table: "items");
            migrationBuilder.DropColumn(name: "module_1", table: "items");
        }
    }
}
