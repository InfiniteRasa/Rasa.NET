using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// The rocks in the Proving Grounds' cavern doorwell (BootcampCaveInBreachV9) seen from across
    /// the bridge. They are a server object, and a client is given the objects of the cells
    /// around its player: the pile at x 275 was there for a player only once they were west of
    /// x 332.8, halfway over the bridge, while the doorway it fills was in view from the far
    /// end and the road to it. With a <see cref="CaveInViewRadius"/> it is a landmark
    /// (Managers.Landmarks): given to a client within that reach of it wherever its cells are,
    /// and its blowing open seen from there.
    ///
    /// Both scenes that stand it up - the experience and Capture the Flag - carry the radius,
    /// the pile being the one object they share.
    /// </summary>
    public static class BootcampCaveInLandmarkV10
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        /// <summary>The far end of the bridge is 78 m off, and the road and the river bank before it further.</summary>
        public const float CaveInViewRadius = 150f;

        public static SceneActorDefinition CaveInActor() =>
            BootcampCaveInBreachV9.CaveInActor() with { ViewRadius = CaveInViewRadius };

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = BootcampCaveInBreachV9.CaptureTheFlag();
            scene.Actors[BootcampCaveInBreachV9.CaveIn] = CaveInActor();
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = BootcampCaveInBreachV9.Experience();
            experience.Scene.Actors[BootcampCaveInBreachV9.CaveIn] = CaveInActor();
            return experience;
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, CaptureTheFlag());
            MissionDataMigration.UpdateExperienceBindings(migration, Experience());
        }

        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateExperienceBindings(migration, BootcampCaveInBreachV9.Experience());
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, BootcampCaveInBreachV9.CaptureTheFlag());
        }
    }
}
