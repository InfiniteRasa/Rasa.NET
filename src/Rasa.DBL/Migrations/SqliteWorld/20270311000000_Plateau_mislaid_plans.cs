using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Mislaid Plans (mission 965) (PlateauMislaidPlansV1). Data only.
    /// </summary>
    public partial class Plateau_mislaid_plans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauMislaidPlansV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMislaidPlansV1.Down(migrationBuilder);
        }
    }
}
