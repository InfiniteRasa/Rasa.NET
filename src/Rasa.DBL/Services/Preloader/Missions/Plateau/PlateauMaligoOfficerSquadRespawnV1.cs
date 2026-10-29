using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// To-fix #18, reopened: in a squad's copy of Maligo Base a killed pool comes back only after the copy's respawn
    /// (SquadInstanceState, 45 minutes), not after its own respawn time. Overseer Kythura stands
    /// on the way to Neuman, so every player killed her before One Way Trip's objective 7 (her keypass) was open, and
    /// she did not come back. Watchman Zyrutha (Showdown 876) the same, for a squad member who comes in after another
    /// killed him.
    ///
    /// The scenes of 876 and 877 name the officers' pools as public-spawn actors (<see cref="ZyruthaRole"/>,
    /// <see cref="KythuraRole"/>): a pool a mission scene names so comes back on its own respawn time in a squad copy
    /// (SpawnPoolManager.ReturnsOnOwnTime). The actors are only named: no sequence uses them, so the officers fight,
    /// drop and count for kills as before, and every player sees them. Not a public encounter
    /// (PublicEncounterBinding): that reserves the NPC for one player when the mission is taken, and 877 is taken on
    /// the Plateau, where Kythura is not.
    ///
    /// The officers' respawn: spawnpool.respown_time is in tenths of a second (SpawnPoolManager), so the 60 that
    /// PlateauMaligoOfficerRespawnV1 set was 6 s, and PlateauMaligoBaseV1's 600 was already a minute. Back to
    /// <see cref="OfficerRespawnTenths"/> (60 s, ESTIMATE as before).
    ///
    /// Down puts back the scenes without the actors and the respawn of 60 (6 s).
    /// </summary>
    public static class PlateauMaligoOfficerSquadRespawnV1
    {
        private const string Revision = PlateauMissionDataV1.Revision;

        public const string ZyruthaRole = "zyrutha", KythuraRole = "kythura";

        /// <summary>60 s in spawnpool.respown_time's tenths of a second.</summary>
        public const uint OfficerRespawnTenths = 600;

        public static MissionSceneDefinition Showdown()
        {
            var scene = PlateauShowdownV1.Scene();
            scene.Actors[ZyruthaRole] = new SceneActorDefinition(ZyruthaRole, SceneActorKind.PublicSpawn, PlateauMaligoBaseV1.Zyrutha);
            return scene;
        }

        public static MissionSceneDefinition OneWayTrip()
        {
            var scene = PlateauOneWayTripV1.Scene();
            scene.Actors[KythuraRole] = new SceneActorDefinition(KythuraRole, SceneActorKind.PublicSpawn, PlateauMaligoBaseV1.Kythura);
            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateSceneBindings(migration, PlateauShowdownV1.MissionId, Revision, Showdown());
            MissionDataMigration.UpdateSceneBindings(migration, PlateauOneWayTripV1.MissionId, Revision, OneWayTrip());
            migration.Sql(Respawn(OfficerRespawnTenths));
        }

        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateSceneBindings(migration, PlateauOneWayTripV1.MissionId, Revision, PlateauOneWayTripV1.Scene());
            MissionDataMigration.UpdateSceneBindings(migration, PlateauShowdownV1.MissionId, Revision, PlateauShowdownV1.Scene());
            migration.Sql(Respawn(PlateauMaligoOfficerRespawnV1.OfficerRespawnSeconds));
        }

        private static string Respawn(uint tenths) =>
            $"UPDATE spawnpool SET respown_time = {tenths} WHERE id IN ({string.Join(", ", PlateauMaligoOfficerRespawnV1.Officers)});";
    }
}
