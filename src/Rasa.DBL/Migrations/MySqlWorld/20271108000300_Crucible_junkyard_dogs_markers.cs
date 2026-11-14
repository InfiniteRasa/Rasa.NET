using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix (to-fix rows 23, 24, 25): Junkyard Dogs' (1149) Flaregasher pools and markers off the Staal Junkyard
    /// door (map link 96) and onto the navmesh before the bridge, Game Wardens' (1147) markers clear of the Chaukas Robotics
    /// Facility door (map link 98), and Flaregasher pool 540144 off the Thunderhead border (map link 94)
    /// (CrucibleMarkersClearOfLinksV2). A delta on 20271108000100 and 20271108000200, which the live World DB has applied.
    /// Data only, plain SQL; Down puts back their values.
    /// </summary>
    public partial class Crucible_junkyard_dogs_markers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleMarkersClearOfLinksV2.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleMarkersClearOfLinksV2.Down(migrationBuilder);
    }
}
