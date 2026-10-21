using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Photo Ops (mission 929, Plateau) (PlateauPhotoOpsV1). Data only.
    /// </summary>
    public partial class Plateau_photo_ops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauPhotoOpsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauPhotoOpsV1.Down(migrationBuilder);
        }
    }
}
