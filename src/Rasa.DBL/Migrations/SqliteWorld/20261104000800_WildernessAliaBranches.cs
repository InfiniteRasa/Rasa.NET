using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class WildernessAliaBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fresh upgrades need capacity before the first long Wilderness evidence note.
            migrationBuilder.AlterColumn<string>(
                name: "reconstruction_note",
                table: "mission_evidence",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(256)");
            WildernessAliaBranchesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessAliaBranchesV1.Down(migrationBuilder);
            // Evidence capacity is additive; rollback never truncates surviving notes.
        }
    }
}
