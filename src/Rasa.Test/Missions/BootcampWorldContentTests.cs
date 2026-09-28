using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Context;
    using Context.World;
    using Rasa.Data;
    using Microsoft.Data.Sqlite;
    using Rasa.Navigation;
    using Repositories.World;
    using Rasa.Services.Preloader;
    using Services.DbContext;
    using Structures.Missions;
    using Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampMissionContentTests
    {
        private const string BootcampRevision = "deployment_11";
        private const string SqliteMigrationId = "20260926190153_SeedWorldContent";
        private const string MySqlMigrationId = "20260926190349_SeedWorldContent";
        private static readonly uint[] RequiredMissionIds = { 1990, 1992, 1994, 1995, 2005 };
        private static readonly uint[] AllMissionIds = { 1990, 1992, 1994, 1995, 2005 };
        private static readonly uint[] BootcampNpcIds =
        {
            510203, 510204, 510205, 510206, 510207, 510208, 510209, 510210, 510211, 510212
        };

        [TestMethod]
        public void RepeatedInitializationPreservesWorldContentAndAuthoredCapabilities()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                string Definitions() => System.Text.Json.JsonSerializer.Serialize(context.MissionContentDefinitionEntries
                    .AsNoTracking().OrderBy(row => row.MissionId).ThenBy(row => row.ContentRevision).ToArray());
                string Rewards() => System.Text.Json.JsonSerializer.Serialize(context.MissionRewardDefinitionEntries
                    .AsNoTracking().OrderBy(row => row.MissionId).ThenBy(row => row.ContentRevision)
                    .ThenBy(row => row.RewardId).ToArray());
                string Actions() => System.Text.Json.JsonSerializer.Serialize(context.MissionActionEntries
                    .AsNoTracking().Where(row => (byte)row.Kind < 10).OrderBy(row => row.MissionId)
                    .ThenBy(row => row.ContentRevision).ThenBy(row => row.ObjectiveId)
                    .ThenBy(row => row.TransitionId).ThenBy(row => row.ActionId)
                    .Select(row => new
                    {
                        row.MissionId, row.ContentRevision, row.ObjectiveId, row.TransitionId, row.ActionId,
                        row.Requirement, row.Kind, row.Sequence, row.TargetObjectiveId, row.ObjectiveState,
                        row.RewardId, row.ScenarioId, row.SpawnGroupId, row.IndicatorId, row.NpcPackageId,
                        row.PlayerFlagId, row.PlayerFlagValue, row.Comment
                    }).ToArray());
                var definitions = Definitions();
                var rewards = Rewards();
                var actions = Actions();
                var scenes = Content.MissionContentTestSupport.ReadScenes(context);

                context.Database.Migrate();

                Assert.AreEqual(definitions, Definitions());
                Assert.AreEqual(rewards, Rewards());
                Assert.AreEqual(actions, Actions());
                foreach (var (id, upgraded) in Content.MissionContentTestSupport.ReadScenes(context))
                {
                    if (id is 1995 or 2005)
                    {
                        Assert.AreEqual("bomb", upgraded.Items.Single().ItemKey);
                        Assert.AreEqual(11519U, upgraded.Items.Single().ItemTemplateId);
                        if (id == 2005)
                            Assert.AreEqual(new Rasa.Missions.Scenes.IssueMissionItemIntent(
                                "accept-bomb", 2005, "bomb", 11519, 1), upgraded.AcceptanceItems.Single());
                    }
                    Assert.AreEqual(
                        System.Text.Json.JsonSerializer.Serialize(scenes[id], Rasa.Missions.Content.MissionContentCodec.Options),
                        System.Text.Json.JsonSerializer.Serialize(upgraded, Rasa.Missions.Content.MissionContentCodec.Options),
                        $"Mission {id}'s existing scene and dialogue must survive repeated initialization.");
                }
                Assert.AreEqual(3, context.MissionActionEntries.Count(row => (byte)row.Kind >= 10));
                Assert.IsEmpty(context.Set<MissionRepeatPolicyEntry>().ToArray());
                var channel = context.Set<MissionChannelPolicyEntry>().Single();
                Assert.AreEqual(1990U, channel.MissionId);
                Assert.AreEqual(Rasa.Missions.Definitions.MissionChannel.Mixed, channel.AcceptanceChannel);
                Assert.AreEqual(Rasa.Missions.Definitions.MissionChannel.Npc, channel.CompletionChannel);
                CollectionAssert.AreEquivalent(AllMissionIds,
                    context.MissionContentDefinitionEntries.Where(row => row.Enabled).Select(row => row.MissionId).ToArray());
                Assert.IsFalse(Validate(LoadSnapshot(context), context).BlocksReadiness);
                Assert.IsFalse(context.Database.GetPendingMigrations().Any());
            });
        }

        [TestMethod]
        public void RadioChannelsSupportNullableNpcIdsAndPreserveWorldRowsOnReinitialization()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var before = System.Text.Json.JsonSerializer.Serialize(
                    context.MissionContentDefinitionEntries.AsNoTracking().OrderBy(entry => entry.MissionId)
                        .ThenBy(entry => entry.ContentRevision).ToArray());
                context.Database.Migrate();
                Assert.AreEqual(before, System.Text.Json.JsonSerializer.Serialize(
                    context.MissionContentDefinitionEntries.AsNoTracking().OrderBy(entry => entry.MissionId)
                        .ThenBy(entry => entry.ContentRevision).ToArray()));
                var policies = context.Set<MissionChannelPolicyEntry>().ToArray();
                Assert.HasCount(1, policies);
                Assert.AreEqual(1990U, policies[0].MissionId);
                Assert.AreEqual(Rasa.Missions.Definitions.MissionChannel.Mixed, policies[0].AcceptanceChannel);
                Assert.AreEqual(Rasa.Missions.Definitions.MissionChannel.Npc, policies[0].CompletionChannel);
                var snapshot = LoadSnapshot(context);
                Assert.IsTrue(snapshot.Definitions[1990].Mission.RadioSources.Single().OwnedPrivateMap);
                Assert.IsTrue(snapshot.Definitions.Values.Where(entry => AllMissionIds.Contains(entry.MissionId))
                    .All(entry => entry.Mission.RepeatPolicy.Kind == Rasa.Missions.Runtime.MissionRepeatKind.Once &&
                        entry.Mission.Shareable == false && entry.Mission.RadioCompletable == false));
                Assert.IsFalse(context.MissionContentDefinitionEntries.Any(entry => entry.Enabled &&
                    !AllMissionIds.Contains(entry.MissionId)), "No Wilderness definition may be enabled.");

                var initiation = context.MissionContentDefinitionEntries.Single(entry =>
                    entry.MissionId == 1990 && entry.ContentRevision == BootcampRevision);
                initiation.GiverId = null;
                initiation.ReceiverId = null;
                policies[0].AcceptanceChannel = Rasa.Missions.Definitions.MissionChannel.Radio;
                policies[0].CompletionChannel = Rasa.Missions.Definitions.MissionChannel.Radio;
                context.SaveChanges();
                var radio = LoadSnapshot(context).Definitions[1990].Mission;
                Assert.IsTrue(radio.IsOperational, radio.OperationalDiagnostic);
                Assert.IsNull(radio.MissionGiver);
                Assert.IsNull(radio.MissionReciver);
                Assert.IsTrue(radio.RadioCompletable);
                Assert.IsEmpty(new Rasa.Missions.Runtime.MissionRuntime(new[] { radio }).ForNpc(0, 0));
            });
        }

        [TestMethod]
        public void RadioSourceMetadataRejectsAnUnknownMapBeforeContentBecomesReady()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var policy = context.Set<MissionChannelPolicyEntry>().Single();
                policy.RadioSources = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    new Rasa.Missions.Definitions.MissionOfferSourceDefinition(
                        Rasa.Missions.Definitions.MissionOfferSourceKind.ServerEvent, "fixture.arrival", uint.MaxValue)
                });
                context.SaveChanges();
                var report = Validate(LoadSnapshot(context), context);
                Assert.IsTrue(report.BlocksReadiness);
                Assert.IsTrue(report.Diagnostics.Any(diagnostic => diagnostic.MissionId == 1990 &&
                    diagnostic.Code == "missing-radio-source-map"));
            });
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RadioSchemaChangesKeepDataInASubsequentMigration(bool mysql)
        {
            Migration schema = mysql
                ? new Rasa.Migrations.MySqlWorld.ConsolidatedWorldSchema()
                : new Rasa.Migrations.SqliteWorld.ConsolidatedWorldSchema();
            Assert.IsEmpty(schema.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>(),
                "SQLite defers the nullable-ID table rebuild; seed data only in a later migration.");
        }

        [TestMethod]
        public void SqliteBootcampMissionContentLoadsAndValidatesAgainstFreshMigratedDatabase()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);
                var report = Validate(snapshot, context);

                CollectionAssert.AreEquivalent(
                    AllMissionIds,
                    snapshot.Definitions.Keys
                        .Where(id => AllMissionIds.Contains(id))
                        .OrderBy(id => id)
                        .ToArray());
                Assert.IsFalse(report.BlocksReadiness);
                Assert.AreEqual(
                    0,
                    report.Diagnostics.Count(diagnostic =>
                        diagnostic.MissionId.HasValue &&
                        RequiredMissionIds.Contains(diagnostic.MissionId.Value)));

                Assert.AreEqual(BootcampRevision, snapshot.Definitions[1992].ContentRevision);
                Assert.AreEqual(1250U, snapshot.Definitions[1992].Rewards[1].Experience);
                Assert.AreEqual(200U, snapshot.Definitions[1992].Rewards[1].Credits);
                Assert.AreEqual(5000U, snapshot.Definitions[1994].Rewards[1].Experience);
                Assert.AreEqual(200U, snapshot.Definitions[1994].Rewards[1].Credits);
                Assert.AreEqual(MissionContentRequirement.Required, snapshot.Definitions[2005].Requirement);
            });
        }

        [TestMethod]
        public void BootcampMissionContentMarksTheRetryAsRequiredAndDefersDepartureThroughNormalizedMetadata()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);
                var initiation = snapshot.Definitions[1990];
                var gearingUp = snapshot.Definitions[1992];
                var finalMission = snapshot.Definitions[1995];
                var retryMission = snapshot.Definitions[2005];

                Assert.AreEqual(MissionAbandonmentPolicy.Prohibited, initiation.AbandonmentPolicy);
                Assert.AreEqual(MissionContentRequirement.Required, retryMission.Requirement);
                Assert.AreEqual(MissionContentRequirement.Required, retryMission.Prerequisites.Single().Requirement);
                Assert.IsTrue(retryMission.Objectives.Values.All(objective =>
                    objective.Requirement == MissionContentRequirement.Required));
                Assert.IsTrue(retryMission.Transitions.Values.All(transition =>
                    transition.Requirement == MissionContentRequirement.Required &&
                    transition.Triggers.All(trigger => trigger.Requirement == MissionContentRequirement.Required)));
                CollectionAssert.AreEqual(
                    new[] { (4U, 1U, 3U, MissionContentRequirement.Optional, MissionActionKind.GrantReward, (uint?)1U) },
                    retryMission.Transitions.Values.SelectMany(transition => transition.Actions)
                        .Where(action => action.Requirement != MissionContentRequirement.Required)
                        .Select(action => (action.ObjectiveId, action.TransitionId, action.ActionId,
                            action.Requirement, action.Kind, action.RewardId)).ToArray(),
                    "The later finale migration authors an optional reward reference, not optional retry progress.");
                Assert.AreEqual(MissionContentRequirement.Optional, retryMission.Rewards[1].Requirement);
                Assert.AreEqual(200U, retryMission.Rewards[1].Credits);
                Assert.IsTrue(retryMission.Scenarios.Values.All(scenario =>
                    scenario.Requirement == MissionContentRequirement.Required &&
                    scenario.Steps.All(step => step.Requirement == MissionContentRequirement.Required)));
                Assert.AreEqual(MissionScenarioStartPolicy.PlayerTriggered, finalMission.Scenarios[6].StartPolicy);
                Assert.AreEqual(MissionScenarioStartPolicy.PlayerTriggered, retryMission.Scenarios[5].StartPolicy);
                CollectionAssert.Contains(
                    gearingUp.Rewards[58].FixedItems.Select(item => item.ItemTemplateId).ToArray(),
                    28U);
            });
        }

        [TestMethod]
        public void BootcampMissionContentResolvesReferencesAndSnapsMeasuredRowsToNavmesh()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);
                var report = Validate(snapshot, context);
                Assert.AreEqual(
                    0,
                    report.Diagnostics.Count(diagnostic =>
                        diagnostic.MissionId.HasValue &&
                        RequiredMissionIds.Contains(diagnostic.MissionId.Value)));

                var creatureIds = context.CreatureEntries.Select(entry => entry.Id).ToHashSet();
                var packageIds = context.NpcPackageEntries.Select(entry => entry.PackageId).ToHashSet();
                var entityClassIds = context.EntityClassEntries.Select(entry => entry.Id).ToHashSet();
                var itemTemplateIds = context.ItemTemplateItemClassEntries.Select(entry => entry.ItemTemplateId).ToHashSet();
                var mapContextIds = context.MapInfoEntries.Select(entry => entry.Id).ToHashSet();

                foreach (var definition in snapshot.Definitions.Values.Where(definition => AllMissionIds.Contains(definition.MissionId)))
                {
                    if (definition.Mission.MissionGiver.HasValue && definition.Mission.MissionGiver.Value != 0)
                        Assert.IsTrue(creatureIds.Contains(definition.Mission.MissionGiver.Value),
                            $"Missing giver creature {definition.Mission.MissionGiver.Value} for mission {definition.MissionId}.");
                    if (definition.Mission.MissionReciver.HasValue && definition.Mission.MissionReciver.Value != 0)
                        Assert.IsTrue(creatureIds.Contains(definition.Mission.MissionReciver.Value),
                            $"Missing receiver creature {definition.Mission.MissionReciver.Value} for mission {definition.MissionId}.");

                    foreach (var transition in definition.Transitions.Values)
                    foreach (var trigger in transition.Triggers.Where(trigger => trigger.NpcPackageId.HasValue))
                        Assert.IsTrue(packageIds.Contains(trigger.NpcPackageId.Value),
                            $"Missing npc_package.package_id {trigger.NpcPackageId.Value} for mission {definition.MissionId} objective {transition.ObjectiveId}.");

                    foreach (var reward in definition.Rewards.Values)
                    foreach (var item in reward.FixedItems.Concat(reward.SelectableItems))
                        Assert.IsTrue(itemTemplateIds.Contains(item.ItemTemplateId),
                            $"Missing item template {item.ItemTemplateId} for mission {definition.MissionId} reward {reward.RewardId}.");

                    foreach (var area in definition.Areas.Values)
                        Assert.IsTrue(mapContextIds.Contains(area.MapContextId),
                            $"Missing map {area.MapContextId} for mission {definition.MissionId} area {area.AreaId}.");

                    foreach (var spawnGroup in definition.SpawnGroups.Values)
                    {
                        Assert.IsTrue(mapContextIds.Contains(spawnGroup.MapContextId),
                            $"Missing map {spawnGroup.MapContextId} for mission {definition.MissionId} spawn group {spawnGroup.SpawnGroupId}.");
                        foreach (var spawn in spawnGroup.Spawns)
                            Assert.IsTrue(creatureIds.Contains(spawn.CreatureId),
                                $"Missing creature {spawn.CreatureId} for mission {definition.MissionId} spawn group {spawnGroup.SpawnGroupId}.");
                    }

                    foreach (var scenario in definition.Scenarios.Values)
                    foreach (var step in scenario.Steps)
                    {
                        if (step.EntityClassId.HasValue)
                            Assert.IsTrue(entityClassIds.Contains(step.EntityClassId.Value),
                                $"Missing entity class {step.EntityClassId.Value} for mission {definition.MissionId} scenario {scenario.ScenarioId} step {step.StepId}.");
                        if (step.MapContextId.HasValue)
                            Assert.IsTrue(mapContextIds.Contains(step.MapContextId.Value),
                                $"Missing destination map {step.MapContextId.Value} for mission {definition.MissionId} scenario {scenario.ScenarioId} step {step.StepId}.");
                        if (step.RewardId.HasValue)
                            Assert.IsTrue(definition.Rewards.ContainsKey(step.RewardId.Value),
                                $"Missing reward {step.RewardId.Value} for mission {definition.MissionId} scenario {scenario.ScenarioId} step {step.StepId}.");
                    }
                }

                CollectionAssert.IsSubsetOf(BootcampNpcIds, creatureIds.OrderBy(id => id).ToArray());

                var bootcampNav = new NavMeshQuery(NavMeshFile.Read(
                    NavMeshFile.PathFor(Path.Combine(FindRepositoryRoot(), "navmesh"), "adv_bootcamp")));
                var wildernessNav = new NavMeshQuery(NavMeshFile.Read(
                    NavMeshFile.PathFor(Path.Combine(FindRepositoryRoot(), "navmesh"), "adv_foreas_concordia_wilderness")));

                foreach (var spawn in context.SpawnPoolEntries
                             .Where(entry => entry.Id >= 510203 && entry.Id <= 510206))
                    AssertOnMesh(bootcampNav, spawn.PosX, spawn.PosY, spawn.PosZ, $"static npc spawn {spawn.Id}");

                foreach (var area in context.MissionAreaEntries
                             .Where(entry => AllMissionIds.Contains(entry.MissionId) && entry.ContentRevision == BootcampRevision))
                    AssertOnMesh(ResolveNav(area.MapContextId, bootcampNav, wildernessNav), area.PosX, area.PosY, area.PosZ,
                        $"mission {area.MissionId} area {area.AreaId}");

                foreach (var indicator in context.MissionIndicatorEntries
                             .Where(entry => AllMissionIds.Contains(entry.MissionId) && entry.ContentRevision == BootcampRevision))
                    AssertOnMesh(bootcampNav, indicator.PosX, indicator.PosY, indicator.PosZ,
                        $"mission {indicator.MissionId} indicator {indicator.IndicatorId}");

                foreach (var spawn in context.MissionSpawnEntries
                             .Where(entry => AllMissionIds.Contains(entry.MissionId) && entry.ContentRevision == BootcampRevision))
                    AssertOnMesh(bootcampNav, spawn.PosX, spawn.PosY, spawn.PosZ,
                        $"mission {spawn.MissionId} spawn group {spawn.SpawnGroupId} spawn {spawn.SpawnId}");

                foreach (var step in context.MissionScenarioStepEntries
                             .Where(entry => AllMissionIds.Contains(entry.MissionId) &&
                                             entry.ContentRevision == BootcampRevision &&
                                             entry.PosX.HasValue &&
                                             entry.PosY.HasValue &&
                                             entry.PosZ.HasValue))
                    AssertOnMesh(
                        ResolveNav(step.MapContextId.GetValueOrDefault(1985), bootcampNav, wildernessNav),
                        step.PosX.Value,
                        step.PosY.Value,
                        step.PosZ.Value,
                        $"mission {step.MissionId} scenario {step.ScenarioId} step {step.StepId}");
            });
        }

        [TestMethod]
        public void BootcampMissionContentSeedsOnlyEntryObjectivesAsInitiallyIncomplete()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);

                AssertObjectiveStates(snapshot, 1990, (1U, MissionObjectiveState.Incomplete), (2U, MissionObjectiveState.Inactive));
                AssertObjectiveStates(snapshot, 1992,
                    (4U, MissionObjectiveState.Incomplete),
                    (1U, MissionObjectiveState.Inactive),
                    (2U, MissionObjectiveState.Inactive),
                    (5U, MissionObjectiveState.Inactive),
                    (6U, MissionObjectiveState.Inactive),
                    (3U, MissionObjectiveState.Inactive),
                    (9U, MissionObjectiveState.Inactive),
                    (8U, MissionObjectiveState.Inactive),
                    (7U, MissionObjectiveState.Inactive));
                AssertObjectiveStates(snapshot, 1994,
                    (4U, MissionObjectiveState.Incomplete),
                    (2U, MissionObjectiveState.Inactive),
                    (1U, MissionObjectiveState.Inactive),
                    (3U, MissionObjectiveState.Inactive));
                AssertObjectiveStates(snapshot, 1995,
                    (2U, MissionObjectiveState.Incomplete),
                    (3U, MissionObjectiveState.Inactive),
                    (1U, MissionObjectiveState.Inactive),
                    (4U, MissionObjectiveState.Inactive));
                AssertObjectiveStates(snapshot, 2005,
                    (1U, MissionObjectiveState.Incomplete),
                    (4U, MissionObjectiveState.Inactive));
            });
        }

        [TestMethod]
        public void BootcampMissionContentUsesAreaDiscoveryAndSatisfiedDeadlinesForTheFinale()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);
                var finalMission = snapshot.Definitions[1995];
                var retryMission = snapshot.Definitions[2005];

                var scoutTransition = finalMission.Transitions[(2U, 1U)];
                var scoutTrigger = scoutTransition.Triggers.Single();
                Assert.AreEqual(MissionTriggerKind.AreaEntered, scoutTrigger.Kind);
                Assert.AreEqual(435U, scoutTrigger.AreaId);
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        MissionActionKind.CompleteObjective,
                        MissionActionKind.StartScenario,
                        MissionActionKind.RevealObjective,
                        MissionActionKind.ActivateObjective
                    },
                    scoutTransition.Actions.Select(action => action.Kind).ToArray());
                Assert.AreEqual((byte)MissionObjectiveState.Completed, scoutTransition.ToStateValue);
                Assert.IsFalse(finalMission.Transitions.ContainsKey((2U, 2U)));
                Assert.AreEqual(21081U, finalMission.Transitions[(3U, 1U)].Triggers.Single().SubjectId);
                CollectionAssert.AreEqual(
                    new[] { 2U, 3U, 3U },
                    scoutTransition.Actions
                        .Where(action => action.TargetObjectiveId.HasValue)
                        .Select(action => action.TargetObjectiveId!.Value)
                        .ToArray());

                var crashSiteScene = finalMission.Scenarios[7];
                Assert.IsTrue(crashSiteScene.Steps.Any(step =>
                    step.Kind == MissionScenarioStepKind.DespawnGroup &&
                    step.SpawnGroupId == 1U));
                Assert.IsTrue(crashSiteScene.Steps.Any(step =>
                    step.Kind == MissionScenarioStepKind.SpawnDynamicObject &&
                    step.DynamicObjectKey == "bootcamp-conrad-corpse"));
                Assert.IsTrue(crashSiteScene.Steps.Any(step =>
                    step.Kind == MissionScenarioStepKind.SpawnDynamicObject &&
                    step.DynamicObjectKey == "bootcamp-dropship-debris" &&
                    step.EntityClassId == 24586));
                Assert.AreEqual(24586U, finalMission.Transitions[(1U, 1U)].Triggers.Single().SubjectId);
                Assert.AreEqual(24586U, retryMission.Transitions[(1U, 1U)].Triggers.Single().SubjectId);
                Assert.AreEqual(20000024U, context.EntityClassEntries.Single(entry => entry.Id == 24586).MeshId);

                Assert.AreEqual(
                    MissionScenarioStepKind.SatisfyDeadline,
                    finalMission.Scenarios[3].Steps.First().Kind);
                Assert.AreEqual(
                    MissionScenarioStepKind.SatisfyDeadline,
                    retryMission.Scenarios[1].Steps.First().Kind);

                Assert.IsFalse(context.SpawnPoolEntries.Any(entry =>
                    entry.Id == BootcampRuntimeTestHarness.WoundedSurvivorCreatureId));
            });
        }

        [TestMethod]
        public void BootcampMissionContentKeepsClient1992ObjectivesStartingFromDelessioGreeting()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);
                var orderedObjectiveIds = snapshot.Definitions[1992].Mission.Objectives.Values
                    .OrderBy(objective => objective.Ordinal)
                    .Select(objective => objective.ObjectiveId)
                    .ToArray();
                // The client's own compiled missionobjective table has no objective 10 for this
                // mission (only 1-9 exist); a synthetic "already accepted from McAllister"
                // objective is unrenderable (ID_ERR_MISSING_TRANSLATION), so objective 4
                // (Delessio's greeting) is the mission's real, client-matching entry point.
                CollectionAssert.AreEqual(
                    new uint[] { 4, 1, 2, 5, 6, 3, 9, 8, 7 },
                    orderedObjectiveIds);

                var entryPointEvidence = context.MissionEvidenceEntries.Single(entry =>
                    entry.MissionId == 1992 &&
                    entry.ContentRevision == BootcampRevision &&
                    entry.OwnerKind == MissionEvidenceOwnerKind.Objective &&
                    entry.OwnerId == 4);
                Assert.AreEqual(
                    MissionEvidenceSourceKind.Server,
                    entryPointEvidence.SourceKind);
                StringAssert.Contains(
                    entryPointEvidence.ReconstructionNote,
                    "npc.delessio");
            });
        }

        [TestMethod]
        public void BootcampMissionContentFoldsSurvivorConversationIntoSupportedClientObjectiveTwo()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);
                var orderedObjectiveIds = snapshot.Definitions[1995].Mission.Objectives.Values
                    .OrderBy(objective => objective.Ordinal)
                    .Select(objective => objective.ObjectiveId)
                    .ToArray();
                CollectionAssert.AreEqual(
                    new uint[] { 2, 3, 1, 4 },
                    orderedObjectiveIds);

                var reconstructedConversation = context.MissionObjectiveDefinitionEntries.Single(entry =>
                    entry.MissionId == 1995 &&
                    entry.ContentRevision == BootcampRevision &&
                    entry.ObjectiveId == 2);
                Assert.AreEqual(21556U, reconstructedConversation.ClientNameTextId);
                Assert.AreEqual(21557U, reconstructedConversation.ClientBodyTextId);

                var reconstructionEvidence = context.MissionEvidenceEntries.Single(entry =>
                    entry.MissionId == 1995 &&
                    entry.ContentRevision == BootcampRevision &&
                    entry.EvidenceId == 3);
                Assert.AreEqual(2U, reconstructionEvidence.OwnerId);
                Assert.AreEqual(
                    MissionEvidenceSourceKind.Reconstruction,
                    reconstructionEvidence.SourceKind);
                Assert.IsFalse(snapshot.Definitions[1995].Objectives.ContainsKey(10));
                var corpse = Content.MissionContentTestSupport.ReadScenes(context)[1995].Actors["bootcamp-conrad-corpse"];
                Assert.AreEqual(21081U, corpse.TemplateId);
                Assert.AreEqual(new Rasa.Missions.Scenes.SceneObjectConversation(1995, 3, 2584, 2), corpse.Conversation,
                    "Native objective 2 is the corpse dialogue alias; Continue advances bomb objective 3.");
            });
        }

        [TestMethod]
        public void BootcampMissionContentAuthorsReachableScenariosAndRetryFailurePrerequisite()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();

                var snapshot = LoadSnapshot(context);

                foreach (var missionId in new uint[] { 1992, 1994, 1995, 2005 })
                {
                    var definition = snapshot.Definitions[missionId];
                    var rootScenarioIds = definition.Transitions.Values
                        .SelectMany(transition => transition.Actions)
                        .Where(action => action.Kind == MissionActionKind.StartScenario && action.ScenarioId.HasValue)
                        .Select(action => action.ScenarioId!.Value)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToArray();
                    Assert.IsTrue(rootScenarioIds.Length > 0, $"Mission {missionId} is missing a StartScenario root.");

                    var reachableScenarioIds = new HashSet<uint>(rootScenarioIds);
                    var pending = new Queue<uint>(rootScenarioIds);
                    while (pending.Count > 0)
                    {
                        var scenarioId = pending.Dequeue();
                        foreach (var targetScenarioId in definition.Scenarios[scenarioId].Steps
                                     .Where(step => step.Kind == MissionScenarioStepKind.ScheduleScenario && step.TargetScenarioId.HasValue)
                                     .Select(step => step.TargetScenarioId!.Value))
                        {
                            if (reachableScenarioIds.Add(targetScenarioId))
                                pending.Enqueue(targetScenarioId);
                        }
                    }

                    CollectionAssert.AreEquivalent(
                        missionId == 1995 ? new uint[] { 2, 3, 4, 5, 6, 7 } : definition.Scenarios.Keys.ToArray(),
                        reachableScenarioIds.OrderBy(id => id).ToArray(),
                        $"Mission {missionId} contains unreachable scenarios.");
                    if (missionId == 1995)
                    {
                        CollectionAssert.AreEquivalent(new uint[] { 1 },
                            definition.Scenarios.Keys.Except(reachableScenarioIds).ToArray());
                        var retired = Content.MissionContentTestSupport.ReadScenes(context)[1995].Sequences[1];
                        Assert.HasCount(1, retired.World);
                        Assert.AreEqual(new Rasa.Missions.Scenes.RemoveActorIntent(
                            "retire-scout-survivor", "group-1-spawn-1-0"), retired.World.Single());
                        Assert.IsEmpty(retired.Character);
                        Assert.IsEmpty(retired.Timers);
                    }
                }

                var retryPrerequisite = snapshot.Definitions[2005].Prerequisites.Single();
                Assert.AreEqual(MissionPrerequisiteKind.MissionAccepted, retryPrerequisite.Kind);
                Assert.AreEqual(1995U, retryPrerequisite.RequiredMissionId);
                Assert.AreEqual((byte)MissionState.Failed, retryPrerequisite.RequiredMissionStateValue);
            });
        }

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext), SqliteMigrationId)]
        [DataRow(typeof(MySqlWorldContext), MySqlMigrationId)]
        public void BootcampMissionContentMigrationIsRegisteredForBothProviders(
            Type contextType,
            string migrationId)
        {
            using var context = CreateContext(contextType, "unused");
            CollectionAssert.Contains(context.Database.GetMigrations().ToArray(), migrationId);

            var sql = NormalizeSql(context.GetService<IMigrator>().GenerateScript());
            StringAssert.Contains(sql, "mission_content_definition");
            StringAssert.Contains(sql, "1990");
            StringAssert.Contains(sql, "1992");
            StringAssert.Contains(sql, "1994");
            StringAssert.Contains(sql, "1995");
            StringAssert.Contains(sql, "2005");
        }

        [TestMethod]
        public void BootcampCrateLootDisablesTheEmptyCrateWithoutGrantingItsContentsTwice()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var steps = context.MissionScenarioStepEntries.AsNoTracking().Where(step =>
                    step.MissionId == 1992 && step.ScenarioId == 2).ToArray();
                Assert.AreEqual(MissionScenarioStepKind.DisableInteraction,
                    steps.Single(step => step.StepId == 1).Kind);
                Assert.AreEqual(29877U, steps.Single(step => step.StepId == 1).EntityClassId);
                Assert.IsFalse(steps.Any(step => step.Kind == MissionScenarioStepKind.GrantRewardPackage ||
                    step.Kind == MissionScenarioStepKind.DespawnDynamicObject));
                Assert.AreEqual(2, steps.Count(step => step.Kind == MissionScenarioStepKind.GrantSkillAbility));
                Assert.IsFalse(Validate(LoadSnapshot(context), context).BlocksReadiness);
            });
        }

        [TestMethod]
        public void BootcampPracticeTargetsKeepTheTrainingStagesDistinct()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var triggers = context.MissionTriggerEntries.AsNoTracking().Where(trigger =>
                    trigger.MissionId == 1992 && (trigger.ObjectiveId == 3 || trigger.ObjectiveId == 8)).ToArray();
                Assert.IsTrue(triggers.All(trigger => trigger.EventKind == 13 && trigger.SubjectId == 29365));
                Assert.AreEqual(1U, triggers.Single(trigger => trigger.ObjectiveId == 3).CounterId);
                Assert.AreEqual(194U, triggers.Single(trigger => trigger.ObjectiveId == 8).CounterId);
                var steps = context.MissionScenarioStepEntries.AsNoTracking().Where(step =>
                    step.MissionId == 1992 && ((step.ScenarioId == 3 && step.StepId == 1) ||
                    (step.ScenarioId == 4 && step.StepId == 3))).ToArray();
                Assert.AreEqual(2, steps.Length);
                Assert.IsTrue(steps.All(step => step.Kind == MissionScenarioStepKind.EnableInteraction &&
                    step.EntityClassId == 29365 && step.SpawnGroupId == null));
            });
        }

        [TestMethod]
        public void BootcampObjectiveIndicatorsRemainStableWithoutWorldEffects()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var before = context.MissionIndicatorEntries.AsNoTracking()
                    .Where(entry => AllMissionIds.Contains(entry.MissionId)).ToArray();
                Assert.AreEqual(12, before.Length);
                Assert.IsFalse(before.Any(entry => entry.Show3DEffect));
                context.Initialize();
                var after = context.MissionIndicatorEntries.AsNoTracking()
                    .Where(entry => AllMissionIds.Contains(entry.MissionId)).ToArray();
                CollectionAssert.AreEquivalent(
                    before.Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                        entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Show3DEffect)).ToArray(),
                    after.Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                        entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Show3DEffect)).ToArray());
            });
        }

        [TestMethod]
        public void BootcampWorldContentGroundsDeSimoneAndEnablesAlistersRun()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var spawn = context.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == 510206);
                Assert.AreEqual(120.059, spawn.PosY, 0.001);
                Assert.AreEqual(510206U, spawn.Creature1Id);
                Assert.AreEqual(7U, context.CreatureEntries.AsNoTracking().Single(entry => entry.Id == 510203).RunSpeed);
            });
        }

        [TestMethod]
        public void CaptureTheFlagContentPreservesTheCaveTriggerAndEncounterOnReinitialization()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                var migrator = context.GetService<IMigrator>();
                migrator.Migrate();
                var area = context.MissionAreaEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == 1994 && entry.AreaId == 439);
                var indicator = context.MissionIndicatorEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == 1994 && entry.IndicatorId == 439);
                var beforeArea = (area.PosX, area.PosY, area.PosZ, area.Shape, area.Radius,
                    area.ExtentX, area.ExtentY, area.ExtentZ, area.MapContextId);
                var beforeIndicator = (indicator.ObjectiveId, indicator.PosX, indicator.PosY,
                    indicator.PosZ, indicator.Radius, indicator.Show3DEffect);

                context.Initialize();

                area = context.MissionAreaEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == 1994 && entry.AreaId == 439);
                indicator = context.MissionIndicatorEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == 1994 && entry.IndicatorId == 439);
                Assert.AreEqual(beforeArea, (area.PosX, area.PosY, area.PosZ, area.Shape, area.Radius,
                    area.ExtentX, area.ExtentY, area.ExtentZ, area.MapContextId));
                Assert.AreEqual(beforeIndicator, (indicator.ObjectiveId, indicator.PosX, indicator.PosY,
                    indicator.PosZ, indicator.Radius, indicator.Show3DEffect));
                Assert.IsTrue(context.SpawnPoolEntries.AsNoTracking()
                    .Where(entry => entry.Id == 520009 || entry.Id == 520010).All(entry => entry.Mode == 1));
                Assert.AreEqual(5, context.SpawnPoolEntries.Count(entry => entry.Id >= 510216 && entry.Id <= 510220));
                Assert.AreEqual(29769U, context.CreatureEntries.AsNoTracking().Single(entry => entry.Id == 510216).ClassId);
                CollectionAssert.AreEquivalent(new uint[] { 7874, 7890, 7986 },
                    context.CreatureEntries.AsNoTracking()
                        .Where(entry => entry.Id >= 510213 && entry.Id <= 510215).Select(entry => entry.NameId).ToArray());
                Assert.AreEqual(3, context.MissionSpawnEntries.Count(entry => entry.MissionId == 1994 && entry.SpawnGroupId == 1));
                Assert.IsTrue(context.MissionSpawnGroupEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == 1994 && entry.SpawnGroupId == 1).Enabled);

            });
        }

        [TestMethod]
        public void BootcampLightningCueUsesTheNativeHighlightGreeting()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                uint? Greeting(uint objective) => context.MissionActionEntries.AsNoTracking().Where(entry =>
                    entry.MissionId == 1990 && entry.ContentRevision == BootcampRevision &&
                    entry.ObjectiveId == objective && entry.Kind == MissionActionKind.ShowAmbientConversation)
                    .Select(entry => entry.NpcPackageId).Single();
                Assert.AreEqual(1634U, Greeting(1));
                Assert.AreEqual(1636U, Greeting(2));
                context.Initialize();
                Assert.AreEqual(1634U, Greeting(1));
                Assert.AreEqual(1636U, Greeting(2));
            });
        }

        [TestMethod]
        public void BootcampMissionContentSeedInsertThrowsWhenGenericRowsDoNotMatchEntityColumns()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                BootcampWorldContentSeedData.Insert(
                    builder,
                    MissionSpawnGroupEntry.TableName,
                    typeof(MissionSpawnGroupEntry),
                    new[]
                    {
                        new object[] { 1994U, BootcampRevision }
                    }));

            StringAssert.Contains(ex.Message, "mission_spawn_group");
            StringAssert.Contains(ex.Message, "expected 10");
            StringAssert.Contains(ex.Message, "but received 2");
        }

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void BootcampMissionContentScriptsSeedFinalScenarioSpawnPolicies(
            Type contextType)
        {
            using var context = CreateContext(contextType, "unused");
            var sql = NormalizeSql(context.GetService<IMigrator>().GenerateScript());

            StringAssert.Contains(
                sql,
                "insert into mission_spawn_group (mission_id, content_revision, spawn_group_id, requirement, area_id, map_context_id, enabled, respawn_seconds, comment) values");
            StringAssert.Contains(
                sql,
                "update mission_spawn_group set spawn_policy = 1, respawn_seconds = null where mission_id = 1994 and spawn_group_id in (1, 2, 3)");
        }

        [TestMethod]
        public void SqliteWorldSeedsOnlyAfterTheFinalSchemaIsInstalled()
        {
            WithDisposableSqliteWorld((context, database) =>
            {
                context.GetService<IMigrator>().Migrate("ConsolidatedWorldSchema");
                Assert.IsEmpty(context.MissionSpawnGroupEntries.ToArray());
                Assert.IsEmpty(context.MissionContentDefinitionEntries.ToArray());
                context.GetService<IMigrator>().Migrate("SeedWorldContent");
                using var reopened = (SqliteWorldContext)CreateContext(typeof(SqliteWorldContext), database);
                reopened.Initialize();
                var groups = reopened.MissionSpawnGroupEntries.Where(entry => entry.MissionId == 1994 &&
                    entry.ContentRevision == BootcampRevision && entry.SpawnGroupId <= 3).ToArray();
                Assert.AreEqual(3, groups.Length);
                Assert.IsTrue(groups.All(entry => entry.RespawnSeconds == null &&
                    entry.SpawnPolicy == MissionSpawnGroupPolicy.ScenarioControlled));
            });
        }

        [TestMethod]
        public void ConsolidatedWorldContentPreservesEveryBootcampTableRowCount()
        {
            WithDisposableSqliteWorld((context, _) =>
            {
                context.Database.Migrate();
                var expected = new Dictionary<string, int>
                {
                    ["mission_content_definition"] = 5, ["mission_prerequisite"] = 4,
                    ["mission_objective_definition"] = 21, ["mission_objective_transition"] = 27,
                    ["mission_trigger"] = 27, ["mission_action"] = 80, ["mission_reward_definition"] = 7,
                    ["mission_reward_item"] = 6, ["mission_indicator"] = 12, ["mission_area"] = 7,
                    ["mission_spawn_group"] = 12, ["mission_spawn"] = 26, ["mission_scenario"] = 20,
                    ["mission_scenario_step"] = 52, ["mission_evidence"] = 17, ["mission_scene_binding"] = 5,
                    ["mission_channel_policy"] = 1, ["mission_repeat_policy"] = 0
                };
                foreach (var (table, count) in expected)
                    Assert.AreEqual(count, context.Database.SqlQueryRaw<int>(
                        $"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = 'deployment_11'").Single(), table);
                Assert.AreEqual(1, context.Set<MissionExperienceBindingEntry>().Count());
            });
        }

        private static MissionContentSnapshot LoadSnapshot(SqliteWorldContext context)
        {
            var repository = new MissionContentRepository(context);
            return new Managers.MissionContentLoader().Load(repository);
        }

        private static MissionValidationReport Validate(
            MissionContentSnapshot snapshot,
            SqliteWorldContext context)
        {
            using var unit = new RepositoryBackedWorldUnitOfWork(context);
            return new Managers.MissionContentValidator().Validate(snapshot, unit);
        }

        private static void AssertOnMesh(
            NavMeshQuery nav,
            double x,
            double y,
            double z,
            string label)
        {
            Assert.IsTrue(
                nav.IsOnMesh(new Vector3((float)x, (float)y, (float)z)),
                $"{label} is off navmesh at ({x}, {y}, {z}).");
        }

        private static void AssertObjectiveStates(
            MissionContentSnapshot snapshot,
            uint missionId,
            params (uint ObjectiveId, MissionObjectiveState State)[] expected)
        {
            var actual = snapshot.Definitions[missionId].Mission.Objectives.Values
                .OrderBy(objective => objective.Ordinal)
                .Select(objective => (objective.ObjectiveId, objective.InitialState!.Value))
                .ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        private static NavMeshQuery ResolveNav(
            uint mapContextId,
            NavMeshQuery bootcamp,
            NavMeshQuery wilderness) =>
            mapContextId == 1220 ? wilderness : bootcamp;

        private static string NormalizeSql(string sql)
        {
            sql ??= string.Empty;
            sql = sql.ToLowerInvariant()
                .Replace("`", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
            return System.Text.RegularExpressions.Regex.Replace(sql, "\\s+", " ").Trim();
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Repository root not found.");
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
                using var context = (SqliteWorldContext)CreateContext(typeof(SqliteWorldContext), database);
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

        private sealed class RepositoryBackedWorldUnitOfWork : Repositories.World.IWorldUnitOfWork
        {
            public RepositoryBackedWorldUnitOfWork(SqliteWorldContext context)
            {
                Actions = null;
                Equipment = new EquipmentRepository(context);
                Creatures = new CreatureRepository(context);
                EntityClasses = new EntityClassRepository(context);
                Footlockers = null;
                Logoses = null;
                MapInfos = new MapInfoRepository(context);
                MapLinks = null;
                Kraftwerks = null;
                MapRegions = null;
                MapMarkers = null;
                Recipes = null;
                NpcMissions = null;
                NpcMissionRewards = null;
                MissionContent = new MissionContentRepository(context);
                NpcPackages = new NpcPackageRepository(context);
                RandomNames = null;
                Spawnpools = null;
                Teleporters = new TeleporterRepository(context);
            }

            public IActionRepository Actions { get; }
            public IEquipmentRepository Equipment { get; }
            public ICreatureRepository Creatures { get; }
            public IEntityClassRepository EntityClasses { get; }
            public IFootlockerRepository Footlockers { get; }
            public ILogosRepository Logoses { get; }
            public IMapInfoRepository MapInfos { get; }
            public IMapLinkRepository MapLinks { get; }
            public IKraftwerksRepository Kraftwerks { get; }
            public IMapRegionRepository MapRegions { get; }
            public IMapMarkerRepository MapMarkers { get; }
            public IRecipeRepository Recipes { get; }
            public INpcMissionRepository NpcMissions { get; }
            public INpcMissionRewardRepository NpcMissionRewards { get; }
            public IMissionContentRepository MissionContent { get; }
            public INpcPackageRepository NpcPackages { get; }
            public IPlayerRandomNameRepository RandomNames { get; }
            public ISpawnpoolRepository Spawnpools { get; }
            public ITeleporterRepository Teleporters { get; }
            public void Complete() { }
            public void Reject() { }
            public Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction BeginTransaction() =>
                throw new NotSupportedException();
            public void Dispose() { }
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
