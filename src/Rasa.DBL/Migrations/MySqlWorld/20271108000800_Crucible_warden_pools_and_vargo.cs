using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix (to-fix rows 43, 44): the Warden Bot pools and Reconstructor pool 770028 moved clear of the Chaukas
    /// Robotics Facility door (map link 98) with their markers, and Merchant Vargo moved clear of the Staal Centrum crafting
    /// station with his markers (CrucibleWardenPoolsAndVargoV1). A delta on 20271108000200-20271108000600, of which the live
    /// World DB has applied some. Data only, plain SQL; Down puts back their values.
    /// </summary>
    public partial class Crucible_warden_pools_and_vargo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleWardenPoolsAndVargoV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleWardenPoolsAndVargoV1.Down(migrationBuilder);
    }
}
