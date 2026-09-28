using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampCorpseDialogueV6
    {
        private const string Revision = BootcampMissionDataV1.Revision;
        private const string Corpse = "bootcamp-conrad-corpse";
        private const string Mission = "mission_id = 1995 and content_revision = 'deployment_11'";

        public static MissionSceneDefinition Scene()
        {
            var scene = BootcampExtractionDataV5.Scene(1995);
            scene.Actors[Corpse] = CorpseActor(scene.Actors[Corpse]);
            scene.Sequences[1] = new()
            {
                World = new() { new RemoveActorIntent("retire-scout-survivor", "group-1-spawn-1-0") }
            };
            return scene;
        }

        public static MissionExperienceDefinition Experience()
        {
            var experience = BootcampExtractionDataV5.Experience();
            experience.Scene.Actors[Corpse] = CorpseActor(experience.Scene.Actors[Corpse]);
            return experience;
        }

        private static SceneActorDefinition CorpseActor(SceneActorDefinition actor) =>
            actor with
            {
                TemplateId = 21081,
                Position = new ScenePosition(-99, 86.33577f, 74),
                InitialObjectState = 0,
                Conversation = new SceneObjectConversation(1995, 3, 2584, 2)
            };

        public static void Up(MigrationBuilder migration)
        {
            migration.Sql($"delete from mission_action where {Mission} and objective_id = 2 and transition_id = 1;");
            migration.Sql($"update mission_action set transition_id = 1 where {Mission} and objective_id = 2 and transition_id = 2;");
            migration.Sql($"delete from mission_trigger where {Mission} and objective_id = 2 and transition_id = 2;");
            migration.Sql($"delete from mission_objective_transition where {Mission} and objective_id = 2 and transition_id = 2;");
            migration.Sql($"update mission_objective_transition set to_state = 2 where {Mission} and objective_id = 2 and transition_id = 1;");
            migration.Sql($"update mission_trigger set subject_id = 21081 where {Mission} and objective_id = 3 and subject_id = 24990;");
            migration.Sql($"update mission_scenario_step set entity_class_id = 21081, pos_y = 86.33577 where {Mission} and dynamic_object_key = '{Corpse}';");
            migration.Sql($"update mission_evidence set reconstruction_note = 'Proximity completes the search. Native NPC corpse dialogue 1995/2/2584/1 advances bomb objective 3 only on Continue.' where {Mission} and evidence_id in (2, 3);");
            MissionDataMigration.UpdateScene(migration, 1995, Revision, Scene());
            MissionDataMigration.UpdateExperience(migration, Experience());
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql(
                "insert into mission_objective_transition (mission_id, content_revision, objective_id, transition_id, requirement, sequence, from_state, to_state, comment) " +
                $"select mission_id, content_revision, 2, 2, requirement, 2, from_state, to_state, comment from mission_objective_transition where {Mission} and objective_id = 2 and transition_id = 1;");
            migration.Sql($"update mission_action set transition_id = 2 where {Mission} and objective_id = 2 and transition_id = 1;");
            migration.Sql($"update mission_objective_transition set to_state = 1 where {Mission} and objective_id = 2 and transition_id = 1;");
            BootcampWorldContentSeedData.Insert(migration, Structures.World.MissionTriggerEntry.TableName,
                typeof(Structures.World.MissionTriggerEntry),
                BootcampWorldContentSeedData.MissionTriggers().Where(row => (uint)row[0] == 1995 && (uint)row[2] == 10)
                    .Select(row => { var copy = (object[])row.Clone(); copy[2] = 2U; copy[3] = 2U; return copy; }));
            BootcampWorldContentSeedData.Insert(migration, Structures.World.MissionActionEntry.TableName,
                typeof(Structures.World.MissionActionEntry),
                BootcampWorldContentSeedData.MissionActions().Where(row => (uint)row[0] == 1995 &&
                    (uint)row[2] == 2 && (uint)row[4] == 2));
            migration.Sql($"update mission_trigger set subject_id = 24990 where {Mission} and objective_id = 3 and subject_id = 21081;");
            migration.Sql($"update mission_scenario_step set entity_class_id = 24990, pos_y = 86.41823 where {Mission} and dynamic_object_key = '{Corpse}';");
            MissionDataMigration.UpdateScene(migration, 1995, Revision, BootcampExtractionDataV5.Scene(1995));
            MissionDataMigration.UpdateExperience(migration, BootcampExtractionDataV5.Experience());
        }

    }
}
