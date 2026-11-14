using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 8, the AFS side of Formal Complaints (player flag 251): Shinichiro Ito's Robotic Salvage Acquisitions
    /// (1267, AshenRoboticAcquisitionsV1), Weapon Salvage Acquisitions (1269, AshenWeaponAcquisitionsV1) and Heavy Salvage
    /// Acquisitions (1268, AshenHeavyAcquisitionsV1). Mission content only, on the creatures and pools of sets 3, 4 and 7; no
    /// new world rows, no upstream row changes.
    /// </summary>
    public partial class Ashen_logistician : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AshenRoboticAcquisitionsV1.Up(migrationBuilder);
            AshenWeaponAcquisitionsV1.Up(migrationBuilder);
            AshenHeavyAcquisitionsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenHeavyAcquisitionsV1.Down(migrationBuilder);
            AshenWeaponAcquisitionsV1.Down(migrationBuilder);
            AshenRoboticAcquisitionsV1.Down(migrationBuilder);
        }
    }
}
