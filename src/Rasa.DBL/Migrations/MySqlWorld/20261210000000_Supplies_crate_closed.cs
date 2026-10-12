using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader.Missions.Wilderness;

    /// <summary>
    /// Supplies On The Double (428)'s crate set down closed, USE_TD_STATE_CLOSED (200), where it
    /// had been in state 0, which its class (UsableTreasureDispHumCrateV05, a TreasureDispenser)
    /// does not have. SuppliesCrateClosed has the scene.
    ///
    /// Down puts the scene back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Supplies_crate_closed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SuppliesCrateClosed.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            SuppliesCrateClosed.Down(migrationBuilder);
        }
    }
}
