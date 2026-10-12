using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader.Missions.Wilderness;

    /// <summary>
    /// The Ranja egg clusters of Sacs And Violence (860) set down in their idle state,
    /// USE_CS_STATE_IDLE (187), where they had been in state 0, which their class does not
    /// have; and the Fithik that hatch from them when a player walks onto one: creature 552001,
    /// Bane_Fithik_Wingless_EggCluster's own class (21499), with its bite (creature_action 73101).
    /// RanjaEggClusterHatching has the rows; FithikEggClusters does the hatching.
    ///
    /// Down puts the scene back and deletes the rows.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Ranja_egg_clusters_hatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RanjaEggClusterHatching.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RanjaEggClusterHatching.Down(migrationBuilder);
        }
    }
}
