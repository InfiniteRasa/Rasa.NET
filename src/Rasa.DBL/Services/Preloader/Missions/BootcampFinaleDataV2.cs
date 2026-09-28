using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampFinaleDataV2
    {
        private const string Wreck = "bootcamp-dropship-debris";
        private const string Evacuation = "bootcamp-evacuation-ship";

        public static MissionSceneDefinition Reinforcements()
        {
            var scene = BootcampMissionDataV1.Mission1995();
            scene.Sequences[2].World.Add(new TransitionObjectStateIntent("detonate-wreck", Wreck, 91, 2000));
            scene.Sequences[4].World.Insert(0, new RemoveActorIntent("clear-detonated-wreck", Wreck));
            scene.Sequences[5].World[0] = new SetInteractionIntent(
                "sequence-5-step-1-bootcamp-dropship-debris", Wreck, true, ObjectState: 31, IfPresent: true);
            return scene;
        }

        public static MissionSceneDefinition Retry()
        {
            var scene = BootcampMissionDataV1.Mission2005();
            scene.Sequences[2].World.Add(new TransitionObjectStateIntent("detonate-wreck", Wreck, 91, 2000));
            scene.Sequences[3].World.Insert(0, new RemoveActorIntent("clear-detonated-wreck", Wreck));
            scene.Sequences[0].World[1] = new SetInteractionIntent("retry-enable-wreck", Wreck, true, ObjectState: 31);
            scene.Sequences[4].World[0] = new SetInteractionIntent(
                "sequence-4-step-1-bootcamp-dropship-debris", Wreck, true, ObjectState: 31, IfPresent: true);
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = BootcampMissionDataV1.Experience();
            experience.Scene.Actors[Evacuation] = new SceneActorDefinition(
                Evacuation, SceneActorKind.Object, 10516, new ScenePosition(-225, 101.12099f, -71),
                InitiallyInteractable: false, InitialObjectState: 56, SharedKey: Evacuation);
            experience.Scene.Sequences[4] = new SceneSequenceDefinition
            {
                World = new() { new EnsureActorIntent("stage-ready-evacuation", Evacuation) }
            };
            experience.Scene.Sequences[5] = new SceneSequenceDefinition
            {
                World = new() { new RemoveActorIntent("board-ready-evacuation", Evacuation) }
            };
            foreach (var mission in new uint[] { 1995, 2005 })
            {
                experience.MissionTriggers.Add(new ExperienceMissionTrigger(mission, "Completeable", 4));
                experience.MissionTriggers.Add(new ExperienceMissionTrigger(mission, "Departing", 5));
            }
            return experience;
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateScene(migration, 1995, BootcampMissionDataV1.Revision, Reinforcements());
            MissionDataMigration.UpdateScene(migration, 2005, BootcampMissionDataV1.Revision, Retry());
            MissionDataMigration.UpdateExperience(migration, Experience());
            migration.UpdateData("teleporter", "id", 60U,
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -225.0, 101.12099, -71.0 });
        }

        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateScene(migration, 1995, BootcampMissionDataV1.Revision, BootcampMissionDataV1.Mission1995());
            MissionDataMigration.UpdateScene(migration, 2005, BootcampMissionDataV1.Revision, BootcampMissionDataV1.Mission2005());
            MissionDataMigration.UpdateExperience(migration, BootcampMissionDataV1.Experience());
            migration.UpdateData("teleporter", "id", 60U,
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -255.3125, 101.05078, -70.4375 });
        }
    }
}
