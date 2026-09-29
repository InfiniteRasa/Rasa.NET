using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class WildernessRanjaGorge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessRanjaGorgeV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessRanjaGorgeV1.Down(migrationBuilder);
        }
    }
}
