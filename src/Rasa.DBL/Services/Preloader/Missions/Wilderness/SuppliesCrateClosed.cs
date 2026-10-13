using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    /// <summary>
    /// Supplies On The Double (428)'s crate in the state it is in (Supplies_crate_closed).
    ///
    /// The crate, UsableTreasureDispHumCrateV05 (26721), was set down in state 0, which a
    /// TreasureDispenser (augmentation 64) does not have: its states are USE_TD_STATE_CLOSED
    /// (200), USE_TD_STATE_OPENED (201) and USE_STATE_REMOVED (202). The client takes a state it
    /// does not have for none at all (usable.py _SetState: no such state in _fsmStates, so nothing
    /// is set), and the crate stood with no state. It is a closed crate: USE_TD_STATE_CLOSED,
    /// which is also the state a TreasureDispenser can be ciphered in (IsClosed). The class has
    /// no animation or effect for its states; only REMOVED has a mesh of its own (null_geo).
    /// </summary>
    public static class SuppliesCrateClosed
    {
        public const uint MissionId = 428;
        public const uint ClosedState = 200;            // USE_TD_STATE_CLOSED

        /// <summary>The scene as WildernessAliaBranches left it, with the crate closed.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = Original();

            foreach (var role in scene.Actors.Keys.ToList())
                scene.Actors[role] = scene.Actors[role] with { InitialObjectState = ClosedState };

            return scene;
        }

        /// <summary>The scene as WildernessAliaBranches left it.</summary>
        public static MissionSceneDefinition Original() =>
            WildernessAliaBranchesV1.SuppliesScene(WildernessAliaBranchesV1.SuppliesCrateClass,
                WildernessAliaBranchesV1.SuppliesCratePosition, WildernessAliaBranchesV1.SuppliesCrateOrientation);

        public static void Up(MigrationBuilder migration) =>
            MissionDataMigration.UpdateScene(migration, MissionId, WildernessMissionDataV1.Revision, Scene());

        public static void Down(MigrationBuilder migration) =>
            MissionDataMigration.UpdateScene(migration, MissionId, WildernessMissionDataV1.Revision, Original());
    }
}
