using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// All Along The Watchtowers (mission 1825, Plateau): the three Remote Sensor Terminals are scene
    /// objects of the mission (PlateauWatchtowersV1). Data only.
    /// </summary>
    public partial class Plateau_watchtowers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauWatchtowersV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauWatchtowersV1.Down(migrationBuilder);
        }
    }
}
