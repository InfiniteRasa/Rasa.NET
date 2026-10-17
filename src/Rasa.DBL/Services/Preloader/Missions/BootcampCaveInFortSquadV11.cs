using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// Capture the Flag's scene and the bootcamp experience with both what came after
    /// BootcampCaveInBreachV9: Youngblood's squad and the quiet fort (BootcampFortSquadV10), and
    /// the cave-in seen from across the bridge (BootcampCaveInLandmarkV10).
    ///
    /// Each of those wrote the two bindings whole, from V9 with its own change, and their
    /// migrations share a timestamp (20270109000000), so they run in the order of their names: the
    /// one run second put back V9 under the other's change. A database migrated fresh lost the
    /// cave-in's radius (Youngblood_fort_squad runs last); one that had the squad before the
    /// cave-in migration came lost the squad and the quiet fort. This writes the two together,
    /// after all of them.
    /// </summary>
    public static class BootcampCaveInFortSquadV11
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = BootcampFortSquadV10.CaptureTheFlag();
            scene.Actors[BootcampCaveInBreachV9.CaveIn] = BootcampCaveInLandmarkV10.CaveInActor();
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = BootcampFortSquadV10.Experience();
            experience.Scene.Actors[BootcampCaveInBreachV9.CaveIn] = BootcampCaveInLandmarkV10.CaveInActor();
            return experience;
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, CaptureTheFlag());
            MissionDataMigration.UpdateExperienceBindings(migration, Experience());
        }

        /// <summary>Back to the squad's bindings, as a database migrated fresh had them before this.</summary>
        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateExperienceBindings(migration, BootcampFortSquadV10.Experience());
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, BootcampFortSquadV10.CaptureTheFlag());
        }
    }
}
