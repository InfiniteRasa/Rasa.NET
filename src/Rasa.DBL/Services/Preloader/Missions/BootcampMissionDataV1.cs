using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;

namespace Rasa.Services.Preloader.Missions
{
    // Fixed data for the migration that introduced migration-owned mission content.
    public static partial class BootcampMissionDataV1
    {
        public const string Revision = "deployment_11";
        public static IReadOnlyDictionary<uint, MissionSceneDefinition> Scenes() => new Dictionary<uint, MissionSceneDefinition>
        {
            [1990] = Mission1990(), [1992] = Mission1992(), [1994] = Mission1994(),
            [1995] = Mission1995(), [2005] = Mission2005()
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var entry in Scenes())
            {
                MissionDataMigration.EnableMission(migration, entry.Key, Revision);
                MissionDataMigration.InsertScene(migration, entry.Key, Revision, entry.Value);
            }
            MissionDataMigration.InsertExperience(migration, Experience());
            migration.UpdateData("mission_indicator",
                new[] { "mission_id", "content_revision", "objective_id", "indicator_id" },
                new object[] { 1995U, Revision, 3U, 436U },
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -99.0, 86.32086, 74.0 });
            migration.UpdateData("mission_scenario_step",
                new[] { "mission_id", "content_revision", "scenario_id", "step_id" },
                new object[] { 1995U, Revision, 7U, 2U },
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -99.0, 86.41823, 74.0 });
            migration.InsertData("itemtemplate_weapon",
                new[] { "id", "aim_rate", "reload_time", "alt_action_id", "alt_action_arg_id", "ae_type", "ae_radius",
                    "recoil_amount", "reuse_override", "cool_rate", "heat_per_shot", "tool_type", "ammo_per_shot",
                    "windup", "recovery", "refire", "range", "alt_max_damage", "alt_damage_type", "alt_range",
                    "alt_ae_radius", "alt_ae_type", "attack_type" },
                new object[] { 17131U, 1.0, 1500U, 1U, 133U, 0U, 1U, 1U, 0U, 100U, 10.0, 8U, 1U,
                    800U, 1U, 800U, 80U, 25U, 1U, 80U, 1U, 1U, 2U });
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.UpdateData("mission_indicator",
                new[] { "mission_id", "content_revision", "objective_id", "indicator_id" },
                new object[] { 1995U, Revision, 3U, 436U },
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -102.4, 86.10937, 66.8 });
            migration.UpdateData("mission_scenario_step",
                new[] { "mission_id", "content_revision", "scenario_id", "step_id" },
                new object[] { 1995U, Revision, 7U, 2U },
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -102.4, 86.20677, 66.8 });
            migration.DeleteData("itemtemplate_weapon", "id", 17131U);
            migration.DeleteData("mission_experience_binding", "experience_key", "bootcamp");
            foreach (var missionId in new uint[] { 1990, 1992, 1994, 1995, 2005 })
            {
                migration.DeleteData("mission_scene_binding", new[] { "mission_id", "content_revision" },
                    new object[] { missionId, Revision });
                MissionDataMigration.EnableMission(migration, missionId, Revision, false);
            }
        }
    }
}
