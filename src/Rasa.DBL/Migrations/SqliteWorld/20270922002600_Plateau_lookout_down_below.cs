using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Lookout Down Below (mission 970, Plateau) (PlateauLookoutDownBelowV1). Data only.
    /// </summary>
    public partial class Plateau_lookout_down_below : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauLookoutDownBelowV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauLookoutDownBelowV1.Down(migrationBuilder);
        }
    }
}
