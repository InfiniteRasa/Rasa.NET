using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Wilderness Bane turret of Add_control_point_turrets bound to the Bane mortars' loot
    /// profile, which Wilderness_ordinary_loot_v1 requires of it (ControlPointTurretLoot). Data
    /// only, in plain SQL: no model of its own, and no table changes.
    ///
    /// Down takes the binding away.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261216000000_Bind_control_point_turret_loot")]
    public class Bind_control_point_turret_loot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ControlPointTurretLoot.Up);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ControlPointTurretLoot.Down);
        }
    }
}
