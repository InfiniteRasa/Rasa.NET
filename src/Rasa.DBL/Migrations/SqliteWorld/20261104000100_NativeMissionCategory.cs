using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class NativeMissionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "category_id",
                table: "mission_content_definition",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "category_id",
                table: "mission_content_definition",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "int(11)");
        }
    }
}
