using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Velon Hollow's These Old Bones (1976, PlateauTheseOldBonesV1): Elder Hathen's escort through the three temples,
    /// and Warrior Algona at the hospital. Data only.
    /// </summary>
    public partial class Plateau_these_old_bones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauTheseOldBonesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauTheseOldBonesV1.Down(migrationBuilder);
        }
    }
}
