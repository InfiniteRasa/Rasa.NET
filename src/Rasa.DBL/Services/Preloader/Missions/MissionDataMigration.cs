using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;

namespace Rasa.Services.Preloader.Missions
{
    public static class MissionDataMigration
    {
        public static void EnableMission(MigrationBuilder migration, uint missionId, string revision, bool enabled = true) =>
            migration.UpdateData("mission_content_definition",
                new[] { "mission_id", "content_revision" }, new object[] { missionId, revision },
                "enabled", enabled);

        public static void InsertScene(MigrationBuilder migration, uint missionId, string revision, MissionSceneDefinition scene) =>
            migration.InsertData("mission_scene_binding",
                new[] { "mission_id", "content_revision", "script_key", "state_version", "bindings" },
                new object[] { missionId, revision, scene.Script, scene.StateVersion,
                    JsonSerializer.Serialize(scene, MissionContentCodec.Options) });

        public static void UpdateScene(MigrationBuilder migration, uint missionId, string revision, MissionSceneDefinition scene) =>
            migration.UpdateData("mission_scene_binding",
                new[] { "mission_id", "content_revision" }, new object[] { missionId, revision },
                new[] { "script_key", "state_version", "bindings" },
                new object[] { scene.Script, scene.StateVersion, JsonSerializer.Serialize(scene, MissionContentCodec.Options) });

        public static void InsertExperience(MigrationBuilder migration, MissionExperienceDefinition experience, bool enabled = true) =>
            migration.InsertData("mission_experience_binding",
                new[] { "experience_key", "map_context_id", "enabled", "bindings" },
                new object[] { experience.Key, experience.MapContextId, enabled,
                    JsonSerializer.Serialize(experience, MissionContentCodec.Options) });

        public static void UpdateExperience(MigrationBuilder migration, MissionExperienceDefinition experience, bool enabled = true) =>
            migration.UpdateData("mission_experience_binding", "experience_key", experience.Key,
                new[] { "map_context_id", "enabled", "bindings" },
                new object[] { experience.MapContextId, enabled, JsonSerializer.Serialize(experience, MissionContentCodec.Options) });
    }
}
