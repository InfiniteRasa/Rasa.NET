using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job R (AshenFixRV1), a delta on 20271110002120 (fix P, live), Avernus Outpost (2110): 1537's Bane
    /// pool 790658 and its marker 6 moved from the Queen's Chamber's west ledge (a navmesh island the real client could not climb
    /// to) onto the chamber's floor. Data only, plain SQL; each UPDATE guarded on set 18's exact place; Down puts them back and
    /// deletes fix R's evidence row.
    /// </summary>
    public partial class Ashen_fix_r : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixRV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixRV1.Down(migrationBuilder);
    }
}
