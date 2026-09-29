using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The title a character wears, kept with the character so it is still on after a relog and
    /// shown to everyone who meets them: a titledata id, 0 for none. Every existing character
    /// starts with none, since the one they wore was never saved.
    /// </summary>
    public partial class Add_character_current_title : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "current_title_id",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "current_title_id", table: "character");
        }
    }
}
