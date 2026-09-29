using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class WildernessTwinPillars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessTwinPillarsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessTwinPillarsV1.Down(migrationBuilder);
        }
    }
}
