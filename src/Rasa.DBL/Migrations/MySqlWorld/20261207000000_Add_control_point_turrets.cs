using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;

    /// <summary>
    /// Turrets on the walls of the AFS control points, the holder's own: 96 spots on 16 points -
    /// the outer corners of each point's outpost fence and the turret platforms the maps place
    /// there - each with an AFS turret in the AFS's garrison and a Bane turret in the Bane's
    /// (ControlPointTurrets has the spots and why each). 14 Bane turret creatures, one a zone
    /// (551001-551014, Emplacement_Bane_Turret_Standard, creature_action 73001-73014), 192 spawn
    /// pools (551101-551292) and their control_point_link rows.
    ///
    /// Down deletes the id ranges and the links to them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_control_point_turrets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ControlPointTurrets.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ControlPointTurrets.Down(migrationBuilder);
        }
    }
}
