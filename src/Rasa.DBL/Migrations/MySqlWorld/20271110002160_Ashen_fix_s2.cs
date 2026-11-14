using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job S2 (AshenFixS2V1), a delta on 20271110002150 (fix S), Bane Conscription Facility (1988): 1498's
    /// teleporter and Slocombe (its scene, markers 287 and 3) and the hall pads 921, 925 and 927 moved from the HQ "hall" (terrain
    /// the real client has no floor on) to Central Operations at the spiral ramp's foot. Data only, plain SQL; each UPDATE guarded
    /// on the exact values before; Down puts them back and deletes fix S2's evidence row.
    /// </summary>
    public partial class Ashen_fix_s2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixS2V1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixS2V1.Down(migrationBuilder);
    }
}
