using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// Capture the Flag's scene, with the fighting west of the fort set going once the fort is
    /// taken back: when Youngblood comes in with his squad (BootcampFortSquadV10) the six front
    /// squad pools (ProvingGroundsFrontSquads) are woken on the player's map. Youngblood holds
    /// them, as he holds the fort's quiet, so a map made again has them going from the start.
    /// Otherwise the scene is BootcampBreachAssaultV12's.
    /// </summary>
    public static class BootcampFrontSquadsV13
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public const string WakeOperation = "front-squads-woken";

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = BootcampBreachAssaultV12.CaptureTheFlag();
            var arrival = scene.Sequences[scene.Names["arrival"]];
            var held = arrival.World.FindIndex(intent => intent is QuietSpawnPoolsIntent && intent.Role == BootcampFortSquadV10.Youngblood);
            arrival.World.Insert(held + 1, new WakeSpawnPoolsIntent(WakeOperation, BootcampFortSquadV10.Youngblood, ProvingGroundsFrontSquads.Pools));
            return scene;
        }

        public static void Up(MigrationBuilder migration) =>
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, CaptureTheFlag());

        public static void Down(MigrationBuilder migration) =>
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, BootcampBreachAssaultV12.CaptureTheFlag());
    }
}
