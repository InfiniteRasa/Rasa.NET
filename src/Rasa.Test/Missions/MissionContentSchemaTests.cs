using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Services.DbContext;
    using Rasa.Structures.World;

    [TestClass]
    public class MissionContentSchemaTests
    {
        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void WorldMissionContentModelDefinesCompositeKeysDeleteBehaviorsAndCrossLinks(Type contextType)
        {
            using var context = CreateContext(contextType, "unused");
            var model = context.Model;

            var content = RequireEntity(model, typeof(MissionContentDefinitionEntry));
            var prerequisite = RequireEntity(model, typeof(MissionPrerequisiteEntry));
            var objective = RequireEntity(model, typeof(MissionObjectiveDefinitionEntry));
            var transition = RequireEntity(model, typeof(MissionObjectiveTransitionEntry));
            var trigger = RequireEntity(model, typeof(MissionTriggerEntry));
            var action = RequireEntity(model, typeof(MissionActionEntry));
            var reward = RequireEntity(model, typeof(MissionRewardDefinitionEntry));
            var rewardItem = RequireEntity(model, typeof(MissionRewardItemEntry));
            var indicator = RequireEntity(model, typeof(MissionIndicatorEntry));
            var area = RequireEntity(model, typeof(MissionAreaEntry));
            var spawnGroup = RequireEntity(model, typeof(MissionSpawnGroupEntry));
            var spawn = RequireEntity(model, typeof(MissionSpawnEntry));
            var scenario = RequireEntity(model, typeof(MissionScenarioEntry));
            var scenarioStep = RequireEntity(model, typeof(MissionScenarioStepEntry));
            var evidence = RequireEntity(model, typeof(MissionEvidenceEntry));
            var legacyReward = RequireEntity(model, typeof(NpcMissionRewardEntry));

            AssertKey(content, "MissionId", "ContentRevision");
            AssertKey(prerequisite, "MissionId", "ContentRevision", "PrerequisiteId");
            AssertKey(objective, "MissionId", "ContentRevision", "ObjectiveId");
            AssertKey(transition, "MissionId", "ContentRevision", "ObjectiveId", "TransitionId");
            AssertKey(trigger, "MissionId", "ContentRevision", "ObjectiveId", "TransitionId", "TriggerId");
            AssertKey(action, "MissionId", "ContentRevision", "ObjectiveId", "TransitionId", "ActionId");
            AssertKey(reward, "MissionId", "ContentRevision", "RewardId");
            AssertKey(rewardItem, "MissionId", "ContentRevision", "RewardId", "ItemId");
            AssertKey(indicator, "MissionId", "ContentRevision", "ObjectiveId", "IndicatorId");
            AssertKey(area, "MissionId", "ContentRevision", "AreaId");
            AssertKey(spawnGroup, "MissionId", "ContentRevision", "SpawnGroupId");
            AssertKey(spawn, "MissionId", "ContentRevision", "SpawnGroupId", "SpawnId");
            AssertKey(scenario, "MissionId", "ContentRevision", "ScenarioId");
            AssertKey(scenarioStep, "MissionId", "ContentRevision", "ScenarioId", "StepId");
            AssertKey(evidence, "MissionId", "ContentRevision", "EvidenceId");

            Assert.IsTrue(content.GetIndexes().Any(index =>
                index.GetDatabaseName() == "mission_content_definition_index_content_revision"));

            AssertDeleteBehavior(prerequisite, typeof(MissionContentDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(objective, typeof(MissionContentDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(transition, typeof(MissionObjectiveDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(trigger, typeof(MissionObjectiveTransitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(action, typeof(MissionObjectiveTransitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(reward, typeof(MissionContentDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(indicator, typeof(MissionObjectiveDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(area, typeof(MissionContentDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(spawnGroup, typeof(MissionContentDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(scenario, typeof(MissionContentDefinitionEntry), DeleteBehavior.Restrict);
            AssertDeleteBehavior(rewardItem, typeof(MissionRewardDefinitionEntry), DeleteBehavior.Cascade);
            AssertDeleteBehavior(spawn, typeof(MissionSpawnGroupEntry), DeleteBehavior.Cascade);
            AssertDeleteBehavior(evidence, typeof(MissionContentDefinitionEntry), DeleteBehavior.Cascade);
            AssertForeignKey(
                spawnGroup,
                typeof(MissionAreaEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "AreaId");
            AssertForeignKey(
                trigger,
                typeof(MissionObjectiveDefinitionEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "RelatedObjectiveId");
            AssertForeignKey(
                trigger,
                typeof(MissionAreaEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "AreaId");
            AssertForeignKey(
                action,
                typeof(MissionObjectiveDefinitionEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "TargetObjectiveId");
            AssertForeignKey(
                action,
                typeof(MissionRewardDefinitionEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "RewardId");
            AssertForeignKey(
                action,
                typeof(MissionSpawnGroupEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "SpawnGroupId");
            AssertForeignKey(
                action,
                typeof(MissionScenarioEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "ScenarioId");
            AssertForeignKey(
                action,
                typeof(MissionIndicatorEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "ObjectiveId",
                "IndicatorId");
            AssertForeignKey(
                scenarioStep,
                typeof(MissionScenarioEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "ScenarioId");
            AssertForeignKey(
                scenarioStep,
                typeof(MissionObjectiveDefinitionEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "TargetObjectiveId");
            AssertForeignKey(
                scenarioStep,
                typeof(MissionRewardDefinitionEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "RewardId");
            AssertForeignKey(
                scenarioStep,
                typeof(MissionSpawnGroupEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "SpawnGroupId");
            AssertForeignKey(
                scenarioStep,
                typeof(MissionSpawnEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "SpawnGroupId",
                "SpawnId");
            AssertForeignKey(
                scenarioStep,
                typeof(MissionScenarioEntry),
                DeleteBehavior.Restrict,
                "MissionId",
                "ContentRevision",
                "TargetScenarioId");
            AssertNoForeignKey(trigger, "SubjectId");

            Assert.AreEqual("content_revision", content.FindProperty("ContentRevision")?.GetColumnName());
            Assert.AreEqual("requirement", content.FindProperty("Requirement")?.GetColumnName());
            Assert.AreEqual("client_counter_0_text_id", objective.FindProperty("ClientCounter0TextId")?.GetColumnName());
            Assert.AreEqual("client_counter_1_text_id", objective.FindProperty("ClientCounter1TextId")?.GetColumnName());
            Assert.AreEqual("client_counter_2_text_id", objective.FindProperty("ClientCounter2TextId")?.GetColumnName());
            Assert.IsNull(reward.FindProperty("Kind"), "Reward definitions should no longer encode fixed/selectable shape.");
            Assert.AreEqual("selection_count", reward.FindProperty("SelectionCount")?.GetColumnName());
            Assert.AreEqual("kind", rewardItem.FindProperty("Kind")?.GetColumnName());
            Assert.AreEqual("spawn_policy", spawnGroup.FindProperty("SpawnPolicy")?.GetColumnName());
            Assert.AreEqual("target_objective_id", scenarioStep.FindProperty("TargetObjectiveId")?.GetColumnName());
            Assert.AreEqual("reward_id", scenarioStep.FindProperty("RewardId")?.GetColumnName());
            Assert.AreEqual("spawn_group_id", scenarioStep.FindProperty("SpawnGroupId")?.GetColumnName());
            Assert.AreEqual("spawn_id", scenarioStep.FindProperty("SpawnId")?.GetColumnName());
            Assert.AreEqual("dynamic_object_key", scenarioStep.FindProperty("DynamicObjectKey")?.GetColumnName());
            Assert.AreEqual("entity_class_id", scenarioStep.FindProperty("EntityClassId")?.GetColumnName());
            Assert.AreEqual("target_scenario_id", scenarioStep.FindProperty("TargetScenarioId")?.GetColumnName());
            Assert.AreEqual("delay_milliseconds", scenarioStep.FindProperty("DelayMilliseconds")?.GetColumnName());
            Assert.AreEqual("skill_id", scenarioStep.FindProperty("SkillId")?.GetColumnName());
            Assert.AreEqual("ability_id", scenarioStep.FindProperty("AbilityId")?.GetColumnName());
            Assert.AreEqual("skill_level", scenarioStep.FindProperty("SkillLevel")?.GetColumnName());
            Assert.AreEqual("ability_slot", scenarioStep.FindProperty("AbilitySlot")?.GetColumnName());
            Assert.AreEqual("tutorial_id", scenarioStep.FindProperty("TutorialId")?.GetColumnName());
            Assert.AreEqual("audio_set_id", scenarioStep.FindProperty("AudioSetId")?.GetColumnName());
            Assert.AreEqual("attempt_key", scenarioStep.FindProperty("AttemptKey")?.GetColumnName());
            Assert.AreEqual("scenario_event_id", scenarioStep.FindProperty("ScenarioEventId")?.GetColumnName());
            Assert.AreEqual("map_context_id", scenarioStep.FindProperty("MapContextId")?.GetColumnName());
            Assert.AreEqual("pos_x", scenarioStep.FindProperty("PosX")?.GetColumnName());
            Assert.AreEqual("pos_y", scenarioStep.FindProperty("PosY")?.GetColumnName());
            Assert.AreEqual("pos_z", scenarioStep.FindProperty("PosZ")?.GetColumnName());
            Assert.AreEqual("orientation", scenarioStep.FindProperty("Orientation")?.GetColumnName());
            Assert.AreEqual("initial_interaction_enabled", scenarioStep.FindProperty("InitialInteractionEnabled")?.GetColumnName());
            Assert.AreEqual("qualification_key", scenarioStep.FindProperty("QualificationKey")?.GetColumnName());
            Assert.AreEqual("qualification_value", scenarioStep.FindProperty("QualificationValue")?.GetColumnName());
            Assert.AreEqual("account_skip_entitlement", scenarioStep.FindProperty("AccountSkipEntitlement")?.GetColumnName());
            Assert.IsNull(legacyReward.FindPrimaryKey(), "Legacy reward rows should remain keyless.");
        }

        [TestMethod]
        public void WorldMissionContentSnapshotsMatchModels()
        {
            foreach (var contextType in new[] { typeof(SqliteWorldContext), typeof(MySqlWorldContext) })
            {
                using var context = CreateContext(contextType, "unused");
                Assert.IsFalse(context.Database.HasPendingModelChanges(), contextType.Name);
                Assert.IsTrue(context.Database.GetMigrations().Any(migration => migration.Contains(
                    "ConsolidatedWorldSchema", StringComparison.Ordinal)), contextType.Name);
            }
        }

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void WorldMissionContentMigrationsGenerateOfflineSqlWithConstraintsAndCrossLinks(Type contextType)
        {
            using var context = CreateContext(contextType, "unused");
            var sql = NormalizeSql(context.GetService<IMigrator>()
                .GenerateScript("Add_data_to_world", "ConsolidatedWorldSchema"));

            AssertContainsCreateTable(sql, MissionContentDefinitionEntry.TableName);
            AssertContainsCreateTable(sql, MissionPrerequisiteEntry.TableName);
            AssertContainsCreateTable(sql, MissionObjectiveDefinitionEntry.TableName);
            AssertContainsCreateTable(sql, MissionObjectiveTransitionEntry.TableName);
            AssertContainsCreateTable(sql, MissionTriggerEntry.TableName);
            AssertContainsCreateTable(sql, MissionActionEntry.TableName);
            AssertContainsCreateTable(sql, MissionRewardDefinitionEntry.TableName);
            AssertContainsCreateTable(sql, MissionRewardItemEntry.TableName);
            AssertContainsCreateTable(sql, MissionIndicatorEntry.TableName);
            AssertContainsCreateTable(sql, MissionAreaEntry.TableName);
            AssertContainsCreateTable(sql, MissionSpawnGroupEntry.TableName);
            AssertContainsCreateTable(sql, MissionSpawnEntry.TableName);
            AssertContainsCreateTable(sql, MissionScenarioEntry.TableName);
            AssertContainsCreateTable(sql, MissionScenarioStepEntry.TableName);
            AssertContainsCreateTable(sql, MissionEvidenceEntry.TableName);

            StringAssert.Contains(
                sql,
                "primary key (mission_id, content_revision, objective_id, transition_id, trigger_id)");
            StringAssert.Contains(
                sql,
                "primary key (mission_id, content_revision, reward_id, item_id)");
            StringAssert.Contains(
                sql,
                "primary key (mission_id, content_revision, scenario_id, step_id)");
            StringAssert.Contains(sql, "mission_content_definition_index_content_revision");
            StringAssert.Contains(sql, "constraint ck_mission_trigger_kind_parameter_set check");
            StringAssert.Contains(sql, "constraint ck_mission_action_kind_parameter_set check");
            StringAssert.Contains(sql, "constraint ck_mission_evidence_source_location check");
            StringAssert.Contains(sql, "constraint ck_mission_reward_definition_selection_count check");
            StringAssert.Contains(sql, "constraint ck_mission_reward_item_kind check");
            StringAssert.Contains(sql, "constraint ck_mission_scenario_step_kind_parameter_set check");
            StringAssert.Contains(sql, "constraint ck_mission_scenario_step_numeric_bounds check");
            StringAssert.Contains(sql, "kind in (1, 2, 3, 4, 5)");
            StringAssert.Contains(sql, "kind in (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)");
            StringAssert.Contains(sql, "kind in (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23)");
            StringAssert.Contains(sql, "selection_count in (0, 1)");
            StringAssert.Contains(sql, "kind in (1, 2)");
            StringAssert.Contains(sql, "delay_milliseconds >= 1 and delay_milliseconds <= 86400000");
            StringAssert.Contains(sql, "skill_level >= 1 and skill_level <= 5");
            StringAssert.Contains(sql, "ability_slot is null or ability_slot <= 24");
            StringAssert.Contains(sql, "dynamic_object_key");
            StringAssert.Contains(sql, "initial_interaction_enabled");
            StringAssert.Contains(sql, "kind <> 20 or (dynamic_object_key is not null and dynamic_object_key <> '' and entity_class_id is not null and pos_x is not null and pos_y is not null and pos_z is not null and orientation is not null");
            StringAssert.Contains(sql, "kind <> 21 or (dynamic_object_key is not null and dynamic_object_key <> ''");
            StringAssert.Contains(sql, "kind <> 22 or (spawn_group_id is not null");
            StringAssert.Contains(sql, "kind <> 23 or (");
            StringAssert.Contains(sql, "qualification_key is null or (qualification_key >= 1 and qualification_key <= 255)");
            StringAssert.Contains(sql, "qualification_value is null or (qualification_value >= 0 and qualification_value <= 1)");
            StringAssert.Contains(sql, "source_uri is not null or local_client_path is not null");
            StringAssert.Contains(sql, "client_counter_0_text_id");
            StringAssert.Contains(sql, "client_counter_1_text_id");
            StringAssert.Contains(sql, "client_counter_2_text_id");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, related_objective_id) references mission_objective_definition (mission_id, content_revision, objective_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, area_id) references mission_area (mission_id, content_revision, area_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, target_objective_id) references mission_objective_definition (mission_id, content_revision, objective_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, reward_id) references mission_reward_definition (mission_id, content_revision, reward_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, spawn_group_id) references mission_spawn_group (mission_id, content_revision, spawn_group_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, scenario_id) references mission_scenario (mission_id, content_revision, scenario_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, objective_id, indicator_id) references mission_indicator (mission_id, content_revision, objective_id, indicator_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, target_objective_id) references mission_objective_definition (mission_id, content_revision, objective_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, reward_id) references mission_reward_definition (mission_id, content_revision, reward_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, spawn_group_id) references mission_spawn_group (mission_id, content_revision, spawn_group_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, spawn_group_id, spawn_id) references mission_spawn (mission_id, content_revision, spawn_group_id, spawn_id) on delete restrict");
            StringAssert.Contains(
                sql,
                "foreign key (mission_id, content_revision, target_scenario_id) references mission_scenario (mission_id, content_revision, scenario_id) on delete restrict");
        }

        [TestMethod]
        public void SqliteMissionContentMigrationEnforcesDiscriminatorEvidenceAndCrossLinkConstraints()
        {
            WithDisposableSqliteWorld((context, database) =>
            {
                context.Database.Migrate();
                SeedAuthoredMissionGraph(context);

                context.MissionTriggerEntries.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.AreaEntered,
                    Sequence = 1,
                    AreaId = 30,
                    Comment = "Area gate"
                });
                context.MissionTriggerEntries.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ObjectiveState,
                    Sequence = 2,
                    RelatedObjectiveId = 11,
                    RelatedState = 1,
                    Comment = "Follow-up objective"
                });
                context.MissionTriggerEntries.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = 3,
                    EventKind = 1,
                    SubjectId = 999,
                    CounterId = 1,
                    InitialValue = 0,
                    TargetValue = 1,
                    SourceSpawnResolved = false,
                    Comment = "Progress event subject ids are external runtime values."
                });
                context.MissionActionEntries.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.RevealObjective,
                    Sequence = 1,
                    TargetObjectiveId = 11,
                    Comment = "Reveal next objective"
                });
                context.MissionActionEntries.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.GrantReward,
                    Sequence = 2,
                    RewardId = 40,
                    Comment = "Grant authored reward"
                });
                context.MissionActionEntries.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ActivateSpawnGroup,
                    Sequence = 3,
                    SpawnGroupId = 50,
                    Comment = "Enable spawn group"
                });
                context.MissionActionEntries.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 4,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.StartScenario,
                    Sequence = 4,
                    ScenarioId = 60,
                    Comment = "Start scenario"
                });
                context.MissionActionEntries.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 5,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ShowIndicator,
                    Sequence = 5,
                    IndicatorId = 70,
                    Comment = "Show indicator"
                });
                context.MissionEvidenceEntries.Add(new MissionEvidenceEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    EvidenceId = 1,
                    OwnerKind = MissionEvidenceOwnerKind.Mission,
                    OwnerId = 321,
                    SourceKind = MissionEvidenceSourceKind.Documentation,
                    SourceUri = "https://example.invalid/task-2",
                    Confidence = 0.8,
                    ReconstructionNote = "Documented source"
                });
                context.SaveChanges();

                Assert.AreEqual(1, context.MissionTriggerEntries.Count(entry => entry.TriggerId == 3));

                AssertSqliteConstraintViolation(database, failing =>
                {
                    failing.MissionTriggerEntries.Add(new MissionTriggerEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        TriggerId = 30,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionTriggerKind.Conversation,
                        Sequence = 30,
                        NpcPackageId = 7,
                        PlayerFlagId = 9,
                        AreaId = 30,
                        Comment = "Conversation triggers cannot carry authored area ids."
                    });
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    failing.MissionTriggerEntries.Add(new MissionTriggerEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        TriggerId = 31,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionTriggerKind.AreaEntered,
                        Sequence = 31,
                        AreaId = 999,
                        Comment = "Dangling mission area reference"
                    });
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    failing.MissionActionEntries.Add(new MissionActionEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        ActionId = 30,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionActionKind.GrantReward,
                        Sequence = 30,
                        RewardId = 40,
                        SpawnGroupId = 50,
                        Comment = "GrantReward cannot populate unrelated spawn groups."
                    });
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    failing.MissionActionEntries.Add(new MissionActionEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        ActionId = 31,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionActionKind.RevealObjective,
                        Sequence = 31,
                        TargetObjectiveId = 999,
                        Comment = "Dangling authored objective reference"
                    });
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    failing.MissionEvidenceEntries.Add(new MissionEvidenceEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        EvidenceId = 30,
                        OwnerKind = MissionEvidenceOwnerKind.Mission,
                        OwnerId = 321,
                        SourceKind = MissionEvidenceSourceKind.Reconstruction,
                        Confidence = 0.4,
                        ReconstructionNote = "Missing both URI and local path."
                    });
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    var area = failing.MissionAreaEntries.Single(entry => entry.AreaId == 30);
                    failing.MissionAreaEntries.Remove(area);
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    var objective = failing.MissionObjectiveDefinitionEntries
                        .Single(entry => entry.ObjectiveId == 11);
                    failing.MissionObjectiveDefinitionEntries.Remove(objective);
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    var reward = failing.MissionRewardDefinitionEntries.Single(entry => entry.RewardId == 40);
                    failing.MissionRewardDefinitionEntries.Remove(reward);
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    var spawnGroup = failing.MissionSpawnGroupEntries.Single(entry => entry.SpawnGroupId == 50);
                    failing.MissionSpawnGroupEntries.Remove(spawnGroup);
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    var scenario = failing.MissionScenarioEntries.Single(entry => entry.ScenarioId == 60);
                    failing.MissionScenarioEntries.Remove(scenario);
                });
                AssertSqliteConstraintViolation(database, failing =>
                {
                    var indicator = failing.MissionIndicatorEntries.Single(entry => entry.IndicatorId == 70);
                    failing.MissionIndicatorEntries.Remove(indicator);
                });
            });
        }

        [TestMethod]
        public void SqliteMissionContentMigrationAcceptsSupportedProgressEventShapesAndCounterTextColumns()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                SeedAuthoredMissionGraph(context);

                var objective = context.MissionObjectiveDefinitionEntries.Single(entry =>
                    entry.MissionId == 321 &&
                    entry.ContentRevision == "deployment_11" &&
                    entry.ObjectiveId == 10);
                objective.ClientCounter0TextId = 9100;
                objective.ClientCounter1TextId = 9101;
                objective.ClientCounter2TextId = 9102;
                context.MissionTriggerEntries.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = 10,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = 10,
                    EventKind = (byte)Rasa.Data.MissionProgressEventKind.CreatureKilled,
                    SubjectId = 501,
                    Comment = "Exact progress event"
                });
                context.MissionTriggerEntries.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = 11,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = 11,
                    EventKind = (byte)Rasa.Data.MissionProgressEventKind.ItemAcquired,
                    SubjectId = 3147,
                    InitialValue = 0,
                    TargetValue = 2,
                    Comment = "Item counter progress event"
                });

                context.SaveChanges();

                var reloaded = context.MissionObjectiveDefinitionEntries.Single(entry =>
                    entry.MissionId == 321 &&
                    entry.ContentRevision == "deployment_11" &&
                    entry.ObjectiveId == 10);
                Assert.AreEqual(9100U, reloaded.ClientCounter0TextId);
                Assert.AreEqual(9101U, reloaded.ClientCounter1TextId);
                Assert.AreEqual(9102U, reloaded.ClientCounter2TextId);
            });
        }

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void MissionContentMigrationsOrderFinalSchemaBeforeData(Type contextType)
        {
            using var context = CreateContext(contextType, "unused");
            var migrations = context.Database.GetMigrations().ToArray();
            var schemaIndex = Array.FindIndex(migrations, id => id.EndsWith("_ConsolidatedWorldSchema"));
            var dataIndex = Array.FindIndex(migrations, id => id.EndsWith("_SeedWorldContent"));
            Assert.IsTrue(schemaIndex >= 0 && dataIndex == schemaIndex + 1, contextType.Name);
            var schema = CreateMigration(context, "ConsolidatedWorldSchema");
            var data = CreateMigration(context, "SeedWorldContent");
            Assert.IsTrue(schema.UpOperations.All(operation =>
                operation is not SqlOperation and not InsertDataOperation and not UpdateDataOperation and not DeleteDataOperation));
            Assert.IsTrue(schema.DownOperations.All(operation =>
                operation is not SqlOperation and not InsertDataOperation and not UpdateDataOperation and not DeleteDataOperation));
            Assert.IsTrue(data.UpOperations.Count > 0);
            Assert.IsTrue(data.UpOperations.Concat(data.DownOperations).All(operation =>
                operation is SqlOperation or InsertDataOperation or UpdateDataOperation or DeleteDataOperation));
            Assert.IsFalse(schema.UpOperations.OfType<CreateTableOperation>().Any(table =>
                table.Name is "mission_active_release" or "mission_release_member"));
            var reward = schema.UpOperations.OfType<CreateTableOperation>()
                .Single(table => table.Name == "mission_reward_definition");
            Assert.IsFalse(reward.Columns.Any(column => column.Name == "kind"));
            Assert.IsTrue(reward.Columns.Any(column => column.Name == "selection_count"));
        }

        [TestMethod]
        public void SqliteMissionContentDataMigrationBackfillsLegacyNpcMissionRows()
        {
            WithDisposableSqliteWorld((context, database) =>
            {
                var dataMigrationId = context.Database.GetMigrations().Single(id =>
                    id.EndsWith(
                        "_SeedWorldContent",
                        StringComparison.Ordinal));
                context.GetService<IMigrator>().Migrate("ConsolidatedWorldSchema");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO npc_mission " +
                    "(id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) " +
                    "VALUES (900321, 101, 102, 9, 2, 3, 1, 0, 'Legacy river recon')");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO npc_mission " +
                    "(id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) " +
                    "VALUES (900429, 201, 202, 12, 4, 5, 0, 1, 'Legacy final assault')");

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        command.Connection.Open();
                    command.CommandText =
                        "SELECT COUNT(*) FROM mission_content_definition WHERE mission_id IN (900321, 900429)";
                    Assert.AreEqual(0L, (long)(command.ExecuteScalar() ?? 0L));
                }

                context.GetService<IMigrator>().Migrate(dataMigrationId);

                var backfilled = context.MissionContentDefinitionEntries
                    .Where(entry => entry.MissionId == 900321 || entry.MissionId == 900429)
                    .OrderBy(entry => entry.MissionId)
                    .Select(entry => new
                    {
                        entry.Requirement, entry.ClientNameTextId, entry.GiverId, entry.ReceiverId,
                        entry.Level, entry.GroupType, entry.CategoryId, entry.Shareable,
                        entry.RadioCompleteable, entry.Comment
                    })
                    .ToArray();
                Assert.AreEqual(2, backfilled.Length);

                var allLegacy = context.MissionContentDefinitionEntries
                    .Count(entry => entry.ContentRevision == "legacy");
                Assert.AreEqual(MissionContentRequirement.Optional, backfilled[0].Requirement);
                Assert.AreEqual(0U, backfilled[0].ClientNameTextId);
                Assert.AreEqual(101U, backfilled[0].GiverId);
                Assert.AreEqual(102U, backfilled[0].ReceiverId);
                Assert.AreEqual(9U, backfilled[0].Level);
                Assert.AreEqual((byte)2, backfilled[0].GroupType);
                Assert.AreEqual((byte)3, backfilled[0].CategoryId);
                Assert.IsTrue(backfilled[0].Shareable);
                Assert.IsFalse(backfilled[0].RadioCompleteable);
                Assert.AreEqual("Legacy river recon", backfilled[0].Comment);
                Assert.AreEqual(MissionContentRequirement.Optional, backfilled[1].Requirement);
                Assert.AreEqual(201U, backfilled[1].GiverId);
                Assert.AreEqual(202U, backfilled[1].ReceiverId);
                Assert.AreEqual("Legacy final assault", backfilled[1].Comment);
                Assert.AreEqual(0, context.MissionObjectiveDefinitionEntries.Count(
                    entry => entry.MissionId == 900321 || entry.MissionId == 900429));

                using (var reopened = CreateSqliteWorldContext(database))
                {
                    reopened.Database.Migrate();
                    Assert.AreEqual(
                        allLegacy,
                        reopened.MissionContentDefinitionEntries.Count(
                            entry => entry.ContentRevision == "legacy"));
                    Assert.AreEqual(2, reopened.MissionContentDefinitionEntries.Count(
                        entry => entry.MissionId == 900321 || entry.MissionId == 900429));
                }
            });
        }

        [TestMethod]
        public void MySqlWorldDataSeedsLegacyNpcMissionRows()
        {
            using var context = CreateContext(typeof(MySqlWorldContext), "unused");
            var sql = NormalizeSql(context.GetService<IMigrator>()
                .GenerateScript("ConsolidatedWorldSchema", "SeedWorldContent"));
            StringAssert.Contains(sql, "insert into mission_content_definition");
            StringAssert.Contains(sql, "select id, 'legacy', 2, 0, giver_id, reciver_id");
            StringAssert.Contains(sql, "radio_completeable, comment, 1 from npc_mission");
        }

        private static void AssertContainsCreateTable(string sql, string tableName)
        {
            StringAssert.Contains(sql, $"create table {tableName}");
        }

        private static Migration CreateMigration(RasaDbContextBase context, string typeName)
        {
            var assembly = context.GetService<IMigrationsAssembly>();
            return assembly.Migrations.Values
                .Select(type => assembly.CreateMigration(type, context.Database.ProviderName))
                .Single(candidate => candidate.GetType().Name == typeName);
        }

        private static void AssertDeleteBehavior(
            IEntityType entity,
            Type principalClrType,
            DeleteBehavior expected)
        {
            var foreignKey = entity.GetForeignKeys().Single(fk =>
                fk.PrincipalEntityType.ClrType == principalClrType);
            Assert.AreEqual(expected, foreignKey.DeleteBehavior);
        }

        private static void AssertForeignKey(
            IEntityType entity,
            Type principalClrType,
            DeleteBehavior expected,
            params string[] propertyNames)
        {
            var foreignKey = entity.GetForeignKeys().Single(fk =>
                fk.PrincipalEntityType.ClrType == principalClrType &&
                fk.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
            Assert.AreEqual(expected, foreignKey.DeleteBehavior);
        }

        private static void AssertNoForeignKey(IEntityType entity, params string[] propertyNames)
        {
            Assert.IsFalse(entity.GetForeignKeys().Any(fk =>
                fk.Properties.Select(property => property.Name).SequenceEqual(propertyNames)));
        }

        private static void AssertKey(IEntityType entity, params string[] propertyNames)
        {
            CollectionAssert.AreEqual(
                propertyNames,
                entity.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray());
        }

        private static IEntityType RequireEntity(IModel model, Type clrType)
        {
            var entity = model.FindEntityType(clrType);
            Assert.IsNotNull(entity, clrType.FullName);
            return entity!;
        }

        private static string NormalizeSql(string sql)
        {
            sql = sql.ToLowerInvariant()
                .Replace("`", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
            return Regex.Replace(sql, "\\s+", " ").Trim();
        }

        private static SqliteWorldContext CreateSqliteWorldContext(string database)
        {
            return (SqliteWorldContext)CreateContext(typeof(SqliteWorldContext), database);
        }

        private static void SeedAuthoredMissionGraph(SqliteWorldContext context)
        {
            context.MissionContentDefinitionEntries.Add(new MissionContentDefinitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                Requirement = MissionContentRequirement.Required,
                ClientNameTextId = 0,
                GiverId = 101,
                ReceiverId = 102,
                Level = 9,
                GroupType = 2,
                CategoryId = 3,
                Shareable = false,
                RadioCompleteable = false,
                Comment = "Authored mission"
            });
            context.MissionObjectiveDefinitionEntries.AddRange(
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 0,
                    ClientBodyTextId = 0,
                    Ordinal = 1,
                    InitialState = 0,
                    IsRequired = true,
                    Comment = "Primary objective"
                },
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 11,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 0,
                    ClientBodyTextId = 0,
                    Ordinal = 2,
                    InitialState = 0,
                    IsRequired = true,
                    Comment = "Secondary objective"
                });
            context.MissionObjectiveTransitionEntries.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                Comment = "Transition"
            });
            context.MissionAreaEntries.Add(new MissionAreaEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                AreaId = 30,
                Requirement = MissionContentRequirement.Required,
                MapContextId = 1220,
                Shape = MissionAreaShape.Sphere,
                PosX = 1,
                PosY = 2,
                PosZ = 3,
                Radius = 4,
                Comment = "Authored area"
            });
            context.MissionRewardDefinitionEntries.Add(new MissionRewardDefinitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                RewardId = 40,
                Requirement = MissionContentRequirement.Required,
                SelectionCount = 0,
                Experience = 100,
                Credits = 50,
                Prestige = 10,
                Comment = "Reward"
            });
            context.MissionSpawnGroupEntries.Add(new MissionSpawnGroupEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                Requirement = MissionContentRequirement.Required,
                MapContextId = 1220,
                Enabled = false,
                Comment = "Spawn group"
            });
            context.MissionScenarioEntries.Add(new MissionScenarioEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 60,
                Requirement = MissionContentRequirement.Required,
                Name = "Scenario",
                Comment = "Scenario"
            });
            context.MissionIndicatorEntries.Add(new MissionIndicatorEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                IndicatorId = 70,
                Requirement = MissionContentRequirement.Required,
                PosX = 1,
                PosY = 2,
                PosZ = 3,
                Radius = 4,
                Show3DEffect = true,
                Comment = "Indicator"
            });
            context.SaveChanges();
        }

        private static void AssertSqliteConstraintViolation(
            string database,
            Action<SqliteWorldContext> act)
        {
            var ex = Assert.ThrowsExactly<DbUpdateException>(() =>
            {
                using var context = CreateSqliteWorldContext(database);
                act(context);
                context.SaveChanges();
            });
            Assert.IsInstanceOfType(ex.InnerException, typeof(SqliteException));
        }

        private static void WithDisposableSqliteWorld(Action<SqliteWorldContext, string> body)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "TestDatabases",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var database = Path.Combine(path, "database");
                using var context = CreateSqliteWorldContext(database);
                body(context, database);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

        private static RasaDbContextBase CreateContext(Type contextType, string database)
        {
            var connection = new DatabaseConnectionConfiguration { Database = database };
            var isSqlite = contextType.Name.StartsWith("Sqlite", StringComparison.Ordinal);
            var options = Options.Create(new DatabaseConfiguration
            {
                Provider = isSqlite ? "Sqlite" : "MySql",
                Auth = connection,
                Char = connection,
                World = connection
            });
            IDbContextConfigurationService configuration = isSqlite
                ? new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory())
                : new OfflineMySqlConfigurationService();
            IDbContextPropertyModifier modifier = isSqlite
                ? new SqliteDbContextPropertyModifier()
                : new MySqlDbContextPropertyModifier();
            return (RasaDbContextBase)Activator.CreateInstance(
                contextType,
                options,
                configuration,
                modifier)!;
        }

        private sealed class OfflineMySqlConfigurationService : IDbContextConfigurationService
        {
            public void Configure(
                DbContextOptionsBuilder builder,
                DatabaseConnectionConfiguration configuration)
            {
                builder.UseMySql(
                    "Server=127.0.0.1;Database=unused",
                    new MySqlServerVersion(new Version(8, 4, 0)));
            }
        }
    }
}
