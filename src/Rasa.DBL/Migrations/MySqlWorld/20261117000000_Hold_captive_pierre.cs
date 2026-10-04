using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader.Missions.Wilderness;

    /// <summary>
    /// Sgt. Pierre is held out of combat in the Bane cache until her forcefield falls: Escape
    /// Velocity's public encounter (mission 666) becomes a ManualCombat one. Data only - the
    /// one scene binding; see <see cref="WildernessHeldCaptiveV1"/>. Down writes the binding
    /// back as the Wilderness missions seeded it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261117000000_Hold_captive_pierre")]
    public partial class Hold_captive_pierre : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessHeldCaptiveV1.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessHeldCaptiveV1.Down(migrationBuilder);
    }
}
