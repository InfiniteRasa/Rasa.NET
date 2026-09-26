using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Missions;

    public static class WorldContentDataV1
    {
        public static void Up(MigrationBuilder migration)
        {
            foreach (var preloader in new IPreloader[]
            {
                new MapLinkPreloader(),
                new KraftwerksPreloader(),
                new MapRegionPreloader(),
                new RecipePreloader(),
                new RecipeInputPreloader(),
                new MapMarkerPreloader(),
                new ActionPreloader(),
                new ActionLevelPreloader(),
                new ActionCostPreloader(),
                new ActionPropertyPreloader(),
                new ActionItemRequirementPreloader(),
                new ItemTemplateActionPreloader(),
                new CreatureClassFlagPreloader(),
                new SkillCharacterPreloader(),
                new ServiceNpcCreaturePreloader(),
                new ServiceNpcAppearancePreloader(),
                new ServiceNpcSpawnpoolPreloader(),
                new ServiceNpcVendorPreloader(),
                new ServiceNpcVendorItemPreloader(),
                new MissionNpcCreaturePreloader(),
                new MissionNpcAppearancePreloader(),
                new MissionNpcSpawnpoolPreloader(),
                new MissionNpcPackagePreloader(),
                new MinionCreaturePreloader(),
                new MinionCreatureStatsPreloader(),
                new BossCreaturePreloader(),
                new BossCreatureStatsPreloader(),
                new BossSpawnpoolPreloader(),
                new ClassTrainerCreaturePreloader(),
                new ClassTrainerAppearancePreloader(),
                new ClassTrainerSpawnpoolPreloader()
            })
                preloader.Preload(migration);

            // The preserved initial migrations already use the current item, Logos, and teleporter preloaders.
            migration.Sql("update creature set name_id = 6727 where id = 77 and name_id = 476;");
            migration.Sql("update creature set name_id = 8522 where id = 89 and name_id = 375;");
            migration.Sql("update creature set name_id = 10100 where id = 76 and name_id = 510;");
            SeedMissions(migration);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var table in new[]
            {
                "mission_channel_policy", "mission_repeat_policy", "mission_scene_binding",
                "mission_experience_binding", "mission_evidence", "mission_scenario_step",
                "mission_action", "mission_trigger", "mission_scenario", "mission_spawn",
                "mission_spawn_group", "mission_indicator", "mission_area", "mission_reward_item",
                "mission_reward_definition", "mission_objective_transition", "mission_objective_definition",
                "mission_prerequisite", "mission_content_definition", "item_template_action",
                "action_item_requirement", "action_property", "action_cost", "action_level", "action",
                "creature_class_flag", "skill_character", "recipe_input", "recipe", "map_marker",
                "map_region", "map_link", "kraftwerks"
            })
                migration.Sql($"delete from {table};");

            DeleteRange(migration, 500001, 500325, "vendor_item", "vendor", "spawnpool", "creature_appearance", "creature");
            DeleteRange(migration, 501001, 501038, "spawnpool", "creature_appearance", "creature");
            DeleteRange(migration, 510001, 510202, "npc_package", "spawnpool", "creature_appearance", "creature");
            DeleteRange(migration, 510203, 510209, "npc_package");
            DeleteRange(migration, 510203, 510206, "spawnpool");
            DeleteRange(migration, 510216, 510271, "spawnpool");
            DeleteRange(migration, 510203, 510217, "creature_appearance", "creature_stat", "creature", "creature_action");
            DeleteRange(migration, 510221, 510228, "creature_appearance", "creature_stat", "creature", "creature_action");
            DeleteRange(migration, 520001, 520999, "spawnpool", "creature_stat", "creature");
            DeleteRange(migration, 600001, 600004, "creature_stat", "creature");
            migration.DeleteData("itemtemplate_weapon", "id", 17131U);
            migration.UpdateData("teleporter", "id", 60U,
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { -255.3125, 101.05078, -70.4375 });
            migration.Sql("update creature set name_id = 476 where id = 77 and name_id = 6727;");
            migration.Sql("update creature set name_id = 375 where id = 89 and name_id = 8522;");
            migration.Sql("update creature set name_id = 510 where id = 76 and name_id = 10100;");
        }

        private static void SeedMissions(MigrationBuilder migration)
        {
            migration.Sql(
                "insert into mission_content_definition " +
                "(mission_id, content_revision, requirement, client_name_text_id, giver_id, receiver_id, level, " +
                "group_type, category_id, shareable, radio_completeable, comment, abandonment_policy) " +
                "select id, 'legacy', 2, 0, giver_id, reciver_id, level, group_type, category_id, shareable, " +
                "radio_completeable, comment, 1 from npc_mission;");

            foreach (var preloader in new IPreloader[]
            {
                new BootcampMissionNpcCreaturePreloader(),
                new BootcampMissionNpcAppearancePreloader(),
                new BootcampMissionNpcPackagePreloader(),
                new BootcampMissionNpcSpawnpoolPreloader(),
                new BootcampMissionContentDefinitionPreloader(),
                new BootcampMissionPrerequisitePreloader(),
                new BootcampMissionObjectiveDefinitionPreloader(),
                new BootcampMissionObjectiveTransitionPreloader(),
                new BootcampMissionRewardDefinitionPreloader(),
                new BootcampMissionRewardItemPreloader(),
                new BootcampMissionIndicatorPreloader(),
                new BootcampMissionAreaPreloader(),
                new BootcampMissionSpawnGroupPreloader(),
                new BootcampMissionSpawnPreloader(),
                new BootcampMissionScenarioPreloader(),
                new BootcampMissionTriggerPreloader(),
                new BootcampMissionActionPreloader(),
                new BootcampMissionScenarioStepPreloader(),
                new BootcampMissionEvidencePreloader()
            })
                preloader.Preload(migration);

            migration.Sql("update mission_spawn_group set spawn_policy = 1, respawn_seconds = null where mission_id = 1994 and spawn_group_id in (1, 2, 3);");
            migration.Sql("update mission_content_definition set abandonment_policy = 2 where mission_id = 1990 and content_revision = 'deployment_11';");
            migration.Sql("update mission_content_definition set requirement = 1 where mission_id = 2005 and content_revision = 'deployment_11';");
            migration.Sql("update mission_prerequisite set requirement = 1 where mission_id = 2005 and content_revision = 'deployment_11';");
            migration.Sql("update mission_scenario set start_policy = 2 where content_revision = 'deployment_11' and ((mission_id = 1995 and scenario_id = 6) or (mission_id = 2005 and scenario_id = 5));");
            migration.InsertData("mission_reward_item",
                new[] { "mission_id", "content_revision", "reward_id", "item_id", "kind", "item_template_id", "quantity" },
                new object[] { 1992U, "deployment_11", 58U, 6U, (byte)1, 28U, 20U });
            migration.Sql(
                "update mission_scenario_step set kind = 4, reward_id = null, entity_class_id = 29877, " +
                "comment = 'Disable empty equipment crate' where mission_id = 1992 and content_revision = 'deployment_11' " +
                "and scenario_id = 2 and step_id = 1 and kind = 11 and reward_id = 58;");
            migration.Sql(
                "delete from mission_scenario_step where mission_id = 1992 and content_revision = 'deployment_11' " +
                "and scenario_id = 2 and step_id = 2 and kind = 21 and dynamic_object_key = 'bootcamp-equipment-crate';");
            migration.Sql(
                "update mission_trigger set event_kind = 13, subject_id = 29365, counter_id = case objective_id when 3 then 1 else 194 end, " +
                "initial_value = null, target_value = null, source_spawn_resolved = null where mission_id = 1992 " +
                "and content_revision = 'deployment_11' and objective_id in (3, 8) and transition_id = 1 and trigger_id = 1;");
            migration.Sql(
                "update mission_scenario_step set kind = 3, spawn_group_id = null, entity_class_id = 29365, " +
                "comment = 'Keep the existing practice targets enabled' where mission_id = 1992 and content_revision = 'deployment_11' " +
                "and ((scenario_id = 3 and step_id = 1) or (scenario_id = 4 and step_id = 3));");
            migration.Sql("update mission_indicator set show_3d_effect = 0 where content_revision = 'deployment_11' and mission_id in (1990, 1992, 1994, 1995, 2005);");
            migration.Sql("update spawnpool set pos_y = 120.059 where id = 510206 and map_context_id = 1985;");
            migration.Sql("update creature set run_speed = 7 where id = 510203;");
            BootcampCaptureTheFlagContent.Up(migration);
            migration.Sql("update mission_action set npc_package_id = 1634 where mission_id = 1990 and content_revision = 'deployment_11' and objective_id = 1 and transition_id = 1 and action_id = 4 and kind = 9;");
            BootcampCombatContent.Up(migration);
            BootcampReinforcementsContent.Up(migration);
            BootcampMissionDataV1.Up(migration);
            BootcampFinaleDataV2.Up(migration);
            BootcampAudioAndCreditsV3.Up(migration);
            BootcampEvacuationReadyV4.Up(migration);
            BootcampExtractionDataV5.Up(migration);
            BootcampCorpseDialogueV6.Up(migration);
            BootcampMissionItemsV7.Up(migration);
            BootcampRadioOffersV1.Up(migration);
        }

        private static void DeleteRange(MigrationBuilder migration, uint first, uint last, params string[] tables)
        {
            foreach (var table in tables)
                migration.Sql($"delete from {table} where id between {first} and {last};");
        }
    }
}
