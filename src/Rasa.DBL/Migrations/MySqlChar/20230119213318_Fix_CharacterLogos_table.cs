using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    public partial class Fix_CharacterLogos_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_character_logos",
                table: "character_logos");

            migrationBuilder.AlterColumn<uint>(
                name: "character_id",
                table: "character_logos",
                type: "int unsigned",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "int unsigned")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_character_logos",
                table: "character_logos",
                columns: new[] { "character_id", "logos_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_character_logos",
                table: "character_logos");

            migrationBuilder.AlterColumn<uint>(
                name: "character_id",
                table: "character_logos",
                type: "int unsigned",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "int unsigned")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_character_logos",
                table: "character_logos",
                column: "character_id");
        }
    }
}
