using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix job K (to-fix row 78): the Shield Drone pools of Out Of Sight (1208), 770024-770026, hold one drone
    /// each instead of three; pool 770025 is a point (radius 0, as 770024 and 770026); pool 770026 and its 1208 marker move
    /// more than the drones' heal reach (60 m) and their rings from the other two, so no drone heals another; an evidence row
    /// for 1208 (CrucibleFixKV1). A delta on 20271108000600, 20271108001000 and 20271108001200. Data only, plain SQL; Down
    /// puts back the old rows.
    /// </summary>
    public partial class Crucible_fix_k : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixKV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixKV1.Down(migrationBuilder);
    }
}
