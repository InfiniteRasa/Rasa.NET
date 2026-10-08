using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
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
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "module_2",
                table: "items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "module_3",
                table: "items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "module_4",
                table: "items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // As SQL, not DropColumn: the SQLite provider drops a column by rebuilding the table
            // from the model of the migration before, and the one before this (Oneoff_titles)
            // is data only and has none - "SQLite does not support this migration operation".
            // SQLite itself has dropped columns since 3.35.
            migrationBuilder.Sql("alter table items drop column module_4;");
            migrationBuilder.Sql("alter table items drop column module_3;");
            migrationBuilder.Sql("alter table items drop column module_2;");
            migrationBuilder.Sql("alter table items drop column module_1;");
        }
    }
}
