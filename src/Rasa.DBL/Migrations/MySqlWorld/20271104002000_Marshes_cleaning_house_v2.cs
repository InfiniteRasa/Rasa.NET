using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes, playtest notes on Cleaning House (894, set 15): 1's marker round all four pumps and a prisoner who
    /// sets off a moment after his talk (MarshesCleaningHouseV2). Down writes MarshesCleaningHouseV1 again. Data only.
    /// </summary>
    public partial class Marshes_cleaning_house_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesCleaningHouseV1.Down(migrationBuilder);
            MarshesCleaningHouseV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesCleaningHouseV2.Down(migrationBuilder);
            MarshesCleaningHouseV1.Up(migrationBuilder);
        }
    }
}
