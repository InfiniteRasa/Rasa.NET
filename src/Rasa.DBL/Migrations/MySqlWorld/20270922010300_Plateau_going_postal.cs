using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Going Postal (mission 1034, Plateau); Ranger Porthus moves to the chasm's south rim near Desolation
    /// Quarry (PlateauGoingPostalV1). Data only.
    /// </summary>
    public partial class Plateau_going_postal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauGoingPostalV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauGoingPostalV1.Down(migrationBuilder);
        }
    }
}
