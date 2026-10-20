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

        // UpdateScene and UpdateExperience take their columns' types from the migration's target
        // model, which only a migration with a Designer has. A data-only migration without one
        // names them, per provider, as the model snapshots have them.

        /// <summary>UpdateScene for a migration with no target model of its own.</summary>
        public static void UpdateSceneBindings(MigrationBuilder migration, uint missionId, string revision, MissionSceneDefinition scene)
        {
            var sqlite = IsSqlite(migration);
            migration.UpdateData("mission_scene_binding",
                new[] { "mission_id", "content_revision" }, new[] { sqlite ? "integer" : "int(11) unsigned", "varchar(32)" },
                new object[] { missionId, revision },
                new[] { "script_key", "state_version", "bindings" }, new[] { "varchar(64)", sqlite ? "INTEGER" : "int", "text" },
                new object[] { scene.Script, scene.StateVersion, JsonSerializer.Serialize(scene, MissionContentCodec.Options) });
        }

        /// <summary>UpdateExperience for a migration with no target model of its own.</summary>
        public static void UpdateExperienceBindings(MigrationBuilder migration, MissionExperienceDefinition experience, bool enabled = true)
        {
            var sqlite = IsSqlite(migration);
            migration.UpdateData("mission_experience_binding",
                new[] { "experience_key" }, new[] { "varchar(64)" }, new object[] { experience.Key },
                new[] { "map_context_id", "enabled", "bindings" },
                new[] { sqlite ? "INTEGER" : "int unsigned", sqlite ? "INTEGER" : "tinyint(1)", "text" },
                new object[] { experience.MapContextId, enabled, JsonSerializer.Serialize(experience, MissionContentCodec.Options) });
        }

        private static bool IsSqlite(MigrationBuilder migration) =>
            migration.ActiveProvider?.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase) == true;
    }
}
