using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible fix job G (to-fix row 61, Out Of Sight 1208): Shield Drone pool 770026 made a point (radius 0), so no
    /// drone is set down on the lava 46 m off its point, and an evidence row (CrucibleFixGV1). A delta on 20271108000600,
    /// which the live World DB has applied. Data only, plain SQL; Down puts back radius 6.
    /// </summary>
    public partial class Crucible_fix_g : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixGV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixGV1.Down(migrationBuilder);
    }
}
