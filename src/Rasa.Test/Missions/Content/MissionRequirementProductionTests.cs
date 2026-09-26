using Rasa.Missions.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Content
{
    using Rasa.Data;
    using Rasa.Game.Missions.Content;
    using Rasa.Game.Missions.Integration;
    using Rasa.Game.Missions.Persistence;
    using Rasa.Managers;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionRequirementProductionTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CurrentRepeatBlocksAFailurePrerequisiteInRuntimeAndDurableAdmission(bool staleRuntime)
        {
            var requirement = new MissionStateRequirement(321, MissionState.Failed);
            var repeat = new Mission(321, "Repeat", 321, 77, 88, 1, 1, 2, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, repeatPolicy: new(MissionRepeatKind.Immediate));
            var dependent = new Mission(322, "Failure branch", 322, 99, 88, 1, 1, 2, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, requirement: requirement);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = repeat, [322] = dependent });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            var candidate = staleRuntime ? context.CreateCompetingClient() : context.Client;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));

            Assert.IsFalse(context.Manager.AcceptOfferedMission(candidate, context.AddNpc(99).EntityId, 322));

            var service = new MissionRequirementService();
            Assert.AreEqual(staleRuntime, service.Evaluate(candidate.Player, requirement));
            using (var unit = context.CreateChar())
                Assert.IsFalse(service.Evaluate(candidate.Player, requirement, unit),
                    "An archived failure cannot override the durable active journal.");

            using var verify = context.CreateChar();
            Assert.IsNull(verify.CharacterMissions.GetByCharacterAndMission(1, 322));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(MissionState.Success)]
        [DataRow(MissionState.Completed)]
        public void LifetimeSuccessAndRewardPrerequisitesSurviveACurrentFailedRepeat(MissionState? requiredState)
        {
            var requirement = new MissionStateRequirement(321, requiredState);
            var repeat = new Mission(321, "Repeat", 321, 77, 88, 1, 1, 2, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, repeatPolicy: new(MissionRepeatKind.Immediate));
            var dependent = new Mission(322, "Success branch", 322, 99, 88, 1, 1, 2, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true, requirement: requirement);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = repeat, [322] = dependent },
                new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            var service = new MissionRequirementService();
            Assert.IsTrue(service.Evaluate(context.Client.Player, requirement));
            using (var unit = context.CreateChar())
                Assert.IsTrue(service.Evaluate(context.Client.Player, requirement, unit));

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(99).EntityId, 322));
        }

        [TestMethod]
        public void LoadedAdmissionHandlerControlsOffersAndDurableAcceptanceButNotLaterStages()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: packs =>
                packs[1990].Requirement = new AllRequirements(new MissionRequirement[]
                {
                    new CustomRequirement("example.even-level"), new FlagRequirement(903, 1)
                }));
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.Client.Player.PlayerFlags[903] = 1;
            using (var unit = harness.Context.CreateChar())
                unit.CharacterFlags.Set(harness.Client.Player.Id, 903, 1);
            Assert.IsFalse(HasMission(Converse(harness, giver), ConversationType.MissionDispense, 1990));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));

            harness.Client.Player.Level = 2;
            Assert.IsTrue(HasMission(Converse(harness, giver), ConversationType.MissionDispense, 1990));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 1990));

            SetLevel(harness, 2);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            using (var unit = harness.Context.CreateChar())
                Assert.IsNotNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 1990));

            SetLevel(harness, 3);
            harness.Client.Player.PlayerFlags.Remove(903);
            using (var unit = harness.Context.CreateChar())
                unit.CharacterFlags.Remove(harness.Client.Player.Id, 903);
            CompleteInitiationObjectives(harness);
            Assert.IsTrue(HasMission(Converse(harness, giver), ConversationType.MissionComplete, 1990));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, 1990, null, null));
            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual((byte)MissionState.Completed,
                    unit.CharacterMissions.Runtime.History(harness.Client.Player.Id).Single(entry => entry.MissionId == 1990).Outcome);
        }

        [TestMethod]
        public void LoadedObjectiveHandlerGatesNpcConversationAndItsDurableMutation()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: packs =>
                packs[1992].ObjectiveRequirements[4] = new CustomRequirement("example.even-level"));
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, false);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1992));

            Assert.IsFalse(HasObjective(Converse(harness, delessio), 1992, 4));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));
            harness.Client.Player.Level = 2;
            Assert.IsTrue(HasObjective(Converse(harness, delessio), 1992, 4));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1992].Objectives[4].State);

            SetLevel(harness, 2);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1992].Objectives[4].State);
        }

        [TestMethod]
        public void LoadedObjectiveHandlerGatesRuntimeAndDurableWorldProgress()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: packs =>
                packs[1990].ObjectiveRequirements[1] = new CustomRequirement("example.even-level"));
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            var progress = MissionProgressEvent.Area(1990, 430);
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, progress));
            harness.Client.Player.Level = 2;
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, progress));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1990].Objectives[1].State);
            SetLevel(harness, 2);
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, progress));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1990].Objectives[1].State);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LoadedTurnInHandlerGatesNpcQueryAndDurableRewardWithoutBlockingProgress(bool legacySuccess)
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: packs =>
                packs[1990].TurnInRequirement = new CustomRequirement("example.even-level"));
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            CompleteInitiationObjectives(harness);
            if (legacySuccess)
            {
                using var unit = harness.Context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var mission = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 1990);
                    mission.MissionState = (uint)MissionState.Success;
                    mission.Completeable = false;
                });
                harness.Client.Player.Missions[1990].State = MissionState.Success;
                harness.Client.Player.Missions[1990].Completeable = false;
            }
            Assert.IsFalse(HasMission(Converse(harness, giver), ConversationType.MissionComplete, 1990));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, 1990, null, null));
            harness.Client.Player.Level = 2;
            Assert.IsTrue(HasMission(Converse(harness, giver), ConversationType.MissionComplete, 1990));
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, 1990, null, null));
            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(0, unit.CharacterMissions.Runtime.History(harness.Client.Player.Id).Count);
            SetLevel(harness, 2);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, 1990, null, null));
        }

        [TestMethod]
        public void LoadedObjectiveHandlerAlsoGatesSceneCompletionIntents()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: packs =>
            {
                packs[1990] = new MissionSceneDefinition
                {
                    Script = "data.sequence",
                    ObjectiveRequirements = new() { [1] = new CustomRequirement("example.even-level") },
                    Sequences = new()
                    {
                        [0] = new(),
                        [1] = new()
                        {
                            Character = new()
                            {
                                new ObjectiveIntent("finish", 1990, 1, MissionObjectiveState.Completed)
                            }
                        }
                    }
                };
            });
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            string runId;
            using (var unit = harness.Context.CreateChar())
                runId = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 1990).Single().RunId;
            var signal = new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1);
            Assert.IsFalse(harness.Manager.Scenes.Submit(runId, signal));
            harness.Client.Player.Level = 2;
            Assert.IsFalse(harness.Manager.Scenes.Submit(runId, signal));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1990].Objectives[1].State);
            using (var unit = harness.Context.CreateChar())
                Assert.IsFalse(unit.CharacterMissions.Runtime.HasReceipt(runId, 1, "finish"));

            SetLevel(harness, 2);
            Assert.IsTrue(harness.Manager.Scenes.Submit(runId, signal));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1990].Objectives[1].State);
        }

        [TestMethod]
        public void LoadedAccountHandlerUsesLiveFactsAndRechecksDurableEntitlement()
        {
            using var harness = BootcampRuntimeTestHarness.Create(configureScenes: packs =>
                packs[1990].Requirement = new CustomRequirement("account.starting-experience-entitlement"));
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsFalse(HasMission(Converse(harness, giver), ConversationType.MissionDispense, 1990));
            harness.Client.AccountEntry.CanSkipBootcamp = true;
            Assert.IsTrue(HasMission(Converse(harness, giver), ConversationType.MissionDispense, 1990));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
            using (var unit = harness.Context.CreateChar())
                unit.GameAccounts.UpdateCanSkipBootcamp(harness.Client.AccountEntry.Id, true);
            harness.Client.ReloadGameAccountEntry();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
        }

        [TestMethod]
        [DataRow("admission")]
        [DataRow("objective")]
        [DataRow("turn-in")]
        [DataRow("missing-objective")]
        public void InvalidMigratedRequirementBindingsAreRejectedAtStartup(string stage)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var packs = MissionContentTestSupport.ReadScenes(harness.WorldContext);
            var scene = packs[1990];
            var unknown = new CustomRequirement("missing.handler");
            switch (stage)
            {
                case "admission": scene.Requirement = unknown; break;
                case "objective": scene.ObjectiveRequirements[1] = unknown; break;
                case "turn-in": scene.TurnInRequirement = unknown; break;
                case "missing-objective":
                    scene.ObjectiveRequirements[999] = new CustomRequirement("example.even-level");
                    break;
            }
            var binding = harness.WorldContext.Set<MissionSceneBindingEntry>().Find(1990U, "deployment_11");
            binding.Bindings = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            harness.WorldContext.SaveChanges();
            Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
        }

        [TestMethod]
        public void RegisteredHandlersCannotRequireFactsWithoutAnAuthoritativeGameProvider()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var evaluator = new MissionRequirementEvaluator();
            evaluator.Register("test.unsupported-fact", new UnsupportedFactRequirement());
            var mission = context.Manager.LoadedMissions[321].WithPolicies(
                new Dictionary<uint, MissionCreditPolicy>(), new CustomRequirement("test.unsupported-fact"));
            var error = Assert.ThrowsExactly<MissionRuleException>(() =>
                new MissionRequirementService(evaluator).Validate(mission));
            StringAssert.Contains(error.Message, "unsupported Game fact");
        }

        [TestMethod]
        public void MissingRuntimeAccountFactsCannotSatisfyANegatedEntitlementRequirement()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.Map.ClientList.Remove(context.Client);
            var service = new MissionRequirementService();
            Assert.ThrowsExactly<MissionRuleException>(() => service.Evaluate(context.Client.Player,
                new NotRequirement(new CustomRequirement("account.starting-experience-entitlement"))));
        }

        [TestMethod]
        [DataRow(false, "none")]
        [DataRow(true, "none")]
        [DataRow(false, "flag")]
        [DataRow(true, "flag")]
        [DataRow(true, "mission")]
        [DataRow(true, "history")]
        [DataRow(true, "entitlement")]
        [DataRow(true, "qualification")]
        public void SceneAndProgressPlannersProjectCapturedRequirementsInOrder(bool typedFlagLast, string lateChange)
        {
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>
            {
                [321] = ProjectionMission(321, new MissionActionDefinition
                {
                    Kind = MissionActionKind.SetPlayerFlag, Sequence = 1, PlayerFlagId = 901, PlayerFlagValue = 7
                }),
                [322] = ProjectionMission(322)
            });
            context.SeedMission(1, 321, (uint)MissionState.Active, false);
            context.SeedMission(1, 322, (uint)MissionState.Active, false);
            using (var seed = context.CreateChar())
                seed.CharacterFlags.Set(1, 901, 1);
            context.Client.Player.PlayerFlags[901] = 1;
            context.ReloadPlayerMissions();
            context.Drain();
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new FlagRequirement(901, 1),
                new MissionStateRequirement(322, MissionState.Active),
                new NotRequirement(new CustomRequirement("account.starting-experience-entitlement")),
                new NotRequirement(new CustomRequirement("character.starting-experience-completed"))
            });
            var service = new MissionRequirementService();
            var manifestations = new ManifestationManager(context);
            var adapter = new SceneCharacterAdapter(context.Manager, manifestations);
            var run = new SceneRun("projection", "test", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, 1);
            var changed = false;
            context.AfterSave = db =>
            {
                if (lateChange == "none" || changed || !db.ChangeTracker.Entries<CharacterFlagEntry>()
                    .Any(entry => entry.Entity.FlagId == CharacterFlagIds.BootcampComplete && entry.Entity.Value == 1))
                    return;
                changed = true;
                switch (lateChange)
                {
                    case "flag":
                        db.Database.ExecuteSqlRaw("UPDATE character_flag SET value = 8 WHERE character_id = 1 AND flag_id = 901");
                        break;
                    case "mission":
                        db.Database.ExecuteSqlRaw("UPDATE character_mission SET mission_state = 1 WHERE character_id = 1 AND mission_id = 322");
                        break;
                    case "history":
                        db.Database.ExecuteSqlRaw("UPDATE character_mission_history SET outcome = 4, rewarded = 1 WHERE character_id = 1 AND mission_id = 322");
                        break;
                    case "entitlement":
                        db.Set<GameAccountEntry>().Where(entry => entry.Id == 1)
                            .ExecuteUpdate(setters => setters.SetProperty(entry => entry.CanSkipBootcamp, false));
                        break;
                    case "qualification":
                        db.Set<CharacterFlagEntry>().Where(entry => entry.CharacterId == 1 && entry.FlagId == CharacterFlagIds.BootcampComplete)
                            .ExecuteDelete();
                        break;
                }
            };
            using var unit = context.CreateChar();
            void Apply()
            {
                using var publication = new MissionScenarioPlan();
                unit.ExecuteTransaction(() =>
                {
                    var capture = service.Capture(context.Client.Player, requirement, unit);
                    if (!typedFlagLast)
                        adapter.Apply(context.Client, run, new SetCharacterFlagIntent("typed", 901, 9), unit, publication);
                    publication.AddProgressPlan(context.Manager.PlanProgress(context.Client,
                        new[] { MissionProgressEvent.Creature(321) }, unit));
                    if (typedFlagLast)
                        adapter.Apply(context.Client, run, new SetCharacterFlagIntent("typed", 901, 9), unit, publication);
                    adapter.Apply(context.Client, run, new ObjectiveIntent("fail", 322, 1, MissionObjectiveState.Failed), unit, publication);
                    adapter.Apply(context.Client, run, new SetEntitlementIntent("entitle", true), unit, publication);
                    adapter.Apply(context.Client, run,
                        new SetQualificationIntent("qualify", (byte)CharacterQualificationKey.BootcampComplete, true), unit, publication);
                    TransactionValidation.AtCommitBoundary(unit, capture.Validate);
                });
                publication.ApplyRuntime(context.Client, manifestations, context.Manager);
            }

            if (lateChange != "none")
            {
                Assert.ThrowsExactly<GameplayRejectionException>(Apply);
                context.AfterSave = null;
                Assert.IsTrue(changed, "The late write must follow the composed scene's last intended mutation.");
                Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
                Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[322].State);
                Assert.IsFalse(context.Client.Player.StartingExperienceCompleted);
                Assert.IsFalse(context.Client.AccountEntry.CanSkipBootcamp);
                Assert.AreEqual(1U, context.Client.Player.PlayerFlags[901]);
                using var verify = context.CreateChar();
                Assert.AreEqual(1U, verify.CharacterFlags.GetValue(1, 901));
                Assert.IsNull(verify.CharacterFlags.GetValue(1, CharacterFlagIds.BootcampComplete));
                Assert.IsFalse(verify.GameAccounts.Get(1).CanSkipBootcamp);
                Assert.IsEmpty(verify.CharacterMissions.Runtime.History(1));
                Assert.IsEmpty(context.Drain());
            }
            Apply();
            context.AfterSave = null;

            Assert.AreEqual(typedFlagLast ? 9U : 7U, context.Client.Player.PlayerFlags[901]);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionState.Failed, context.Client.Player.Missions[322].State);
            Assert.IsTrue(context.Client.Player.StartingExperienceCompleted);
            Assert.IsTrue(context.Client.AccountEntry.CanSkipBootcamp);
            using var final = context.CreateChar();
            Assert.AreEqual(typedFlagLast ? 9U : 7U, final.CharacterFlags.GetValue(1, 901));
            var history = final.CharacterMissions.Runtime.History(1).Single();
            Assert.AreEqual(322U, history.MissionId);
            Assert.AreEqual((uint)MissionState.Failed, history.Outcome);
            Assert.IsFalse(history.Rewarded);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SceneRewardPlannerProjectsItsLevelBeforeFinalRequirementValidation(bool lateOverwrite)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionDialogueTestContent.Install(harness.WorldContext);
            var reward = harness.WorldContext.Set<MissionRewardDefinitionEntry>()
                .Single(entry => entry.MissionId == 339 && entry.RewardId == 401);
            reward.Experience = 100;
            harness.WorldContext.SaveChanges();
            harness.Manager.LoadMissions();
            using (var seed = harness.Context.CreateChar())
            {
                seed.Characters.UpdateCharacterProgression(harness.Client.Player.Id, 272400, 9);
                seed.Characters.UpdateCharacterCloneCredits(harness.Client.Player.Id, 1);
            }
            harness.Client.Player.Level = 9;
            harness.Client.Player.Experience = 272400;
            harness.Client.Player.CloneCredits = 1;
            var before = harness.Context.ReadRewardTotals();
            harness.Drain();
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new NotRequirement(new LevelRequirement(10)),
                new NotRequirement(new CustomRequirement("example.even-level"))
            });
            var manifestations = new ManifestationManager(harness.Context);
            var adapter = new SceneCharacterAdapter(harness.Manager, manifestations);
            var run = new SceneRun("reward-projection", "test", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, harness.Client.Player.Id);
            using var unit = harness.Context.CreateChar();
            void Apply(bool overwrite)
            {
                using var publication = new MissionScenarioPlan();
                unit.ExecuteTransaction(() =>
                {
                    var capture = new MissionRequirementService().Capture(harness.Client.Player, requirement, unit);
                    adapter.Apply(harness.Client, run, new GrantRewardIntent("reward", 339, 401), unit, publication);
                    if (overwrite)
                        TransactionValidation.BeforeValidation(unit, () =>
                            unit.Characters.UpdateCharacterProgression(harness.Client.Player.Id, 272600, 12));
                    TransactionValidation.AtCommitBoundary(unit, capture.Validate);
                });
                publication.ApplyRuntime(harness.Client, manifestations, harness.Manager);
            }

            if (lateOverwrite)
            {
                Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(true));
                Assert.AreEqual(before, harness.Context.ReadRewardTotals());
                Assert.AreEqual((byte)9, harness.Client.Player.Level);
                Assert.IsEmpty(harness.Drain());
            }
            Apply(false);
            Assert.AreEqual((byte)10, harness.Client.Player.Level);
            Assert.AreEqual(272500U, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(before.Credits + 10, harness.Context.ReadRewardTotals().Credits);
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, false, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, true)]
        [DataRow(true, false, true)]
        [DataRow(true, true, true)]
        public void SceneFlagProjectionDistinguishesRemovalFromZeroAndRetainsTerminalExperience(
            bool terminalExperience, bool finalZero, bool lateOverwrite)
        {
            using var context = MissionTestContext.WithDefinitions(321);
            using (var seed = context.CreateChar())
                seed.ExecuteTransaction(() =>
                {
                    seed.CharacterFlags.Set(1, 902, 1);
                    seed.CharacterFlags.Set(1, CharacterFlagIds.BootcampComplete, 1);
                    seed.CharacterStartingExperience.Add(new CharacterStartingExperienceEntry(1, "fixture",
                        terminalExperience ? CharacterStartingExperienceState.Completed : CharacterStartingExperienceState.Bootcamp));
                });
            context.Client.Player.PlayerFlags[902] = 1;
            context.Client.Player.PlayerFlags[CharacterFlagIds.BootcampComplete] = 1;
            context.Client.Player.StartingExperienceCompleted = true;
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new FlagRequirement(902, 1), new CustomRequirement("character.starting-experience-completed")
            });
            var manifestations = new ManifestationManager(context);
            var adapter = new SceneCharacterAdapter(context.Manager, manifestations);
            var run = new SceneRun("presence-projection", "test", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, 1);
            context.Drain();
            using var unit = context.CreateChar();
            void Apply(bool overwrite)
            {
                using var publication = new MissionScenarioPlan();
                unit.ExecuteTransaction(() =>
                {
                    var capture = new MissionRequirementService().Capture(context.Client.Player, requirement, unit);
                    adapter.Apply(context.Client, run, new SetCharacterFlagIntent("zero", 902, 0), unit, publication);
                    adapter.Apply(context.Client, run, new SetCharacterFlagIntent("remove", 902, null), unit, publication);
                    if (finalZero)
                        adapter.Apply(context.Client, run, new SetCharacterFlagIntent("final-zero", 902, 0), unit, publication);
                    adapter.Apply(context.Client, run,
                        new SetQualificationIntent("unqualify", (byte)CharacterQualificationKey.BootcampComplete, false), unit, publication);
                    if (overwrite)
                        TransactionValidation.BeforeValidation(unit, () =>
                        {
                            if (finalZero)
                                unit.CharacterFlags.Remove(1, 902);
                            else
                                unit.CharacterFlags.Set(1, 902, 0);
                        });
                    TransactionValidation.AtCommitBoundary(unit, capture.Validate);
                });
                publication.ApplyRuntime(context.Client, manifestations, context.Manager);
            }

            if (lateOverwrite)
            {
                Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(true));
                Assert.AreEqual(1U, context.Client.Player.PlayerFlags[902]);
                Assert.IsTrue(context.Client.Player.StartingExperienceCompleted);
                using var verify = context.CreateChar();
                Assert.AreEqual(1U, verify.CharacterFlags.GetValue(1, 902));
                Assert.AreEqual(1U, verify.CharacterFlags.GetValue(1, CharacterFlagIds.BootcampComplete));
                Assert.IsEmpty(context.Drain());
            }
            Apply(false);
            Assert.AreEqual(finalZero, context.Client.Player.PlayerFlags.ContainsKey(902));
            Assert.AreEqual(terminalExperience, context.Client.Player.StartingExperienceCompleted);
            using var final = context.CreateChar();
            Assert.AreEqual<uint?>(finalZero ? 0U : null, final.CharacterFlags.GetValue(1, 902));
            Assert.IsNull(final.CharacterFlags.GetValue(1, CharacterFlagIds.BootcampComplete));
            Assert.AreEqual(terminalExperience, MissionRequirementFactsAdapter.HasCompletedStartingExperience(final, 1));
        }

        private static Mission ProjectionMission(uint missionId, params MissionActionDefinition[] actions) =>
            new(missionId, "Projection fixture", missionId, 77, 88, 1, 1, 2, false, false, new[]
            {
                new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { null, null, null }, 1,
                    MissionObjectiveState.Incomplete, true,
                    new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                    Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(), executableTransitions: new[]
                    {
                        new MissionObjectiveExecutableTransition(1, 1, MissionObjectiveState.Completed, null,
                            MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, missionId),
                            null, null, actions)
                    })
            }, enableOperational: true);

        private static void SetLevel(BootcampRuntimeTestHarness.Harness harness, byte level)
        {
            using var unit = harness.Context.CreateChar();
            unit.Characters.UpdateCharacterProgression(harness.Client.Player.Id, harness.Client.Player.Experience, level);
            harness.Client.Player.Level = level;
        }

        private static void CompleteInitiationObjectives(BootcampRuntimeTestHarness.Harness harness)
        {
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 431)));
        }

        private static Dictionary<ConversationType, object> Converse(BootcampRuntimeTestHarness.Harness harness, Creature npc)
        {
            harness.Drain();
            new NpcManager(harness.Context, harness.Manager).RequestNpcConverse(
                harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            return harness.Drain().OfType<ConversePacket>().Single().ConvoDataDict;
        }

        private static bool HasMission(Dictionary<ConversationType, object> data, ConversationType type, uint id) =>
            data.TryGetValue(type, out var value) && (type == ConversationType.MissionDispense
                ? ((Dictionary<uint, MissionInfo>)value).ContainsKey(id)
                : ((Dictionary<uint, RewardInfo>)value).ContainsKey(id));

        private static bool HasObjective(Dictionary<ConversationType, object> data, uint mission, uint objective) =>
            data.TryGetValue(ConversationType.ObjectiveComplete, out var value) &&
            ((List<CompleteableObjectives>)value).Any(entry => entry.MissionId == mission && entry.ObjectiveId == objective);

        private sealed class UnsupportedFactRequirement : IMissionRequirementHandler
        {
            public IReadOnlyCollection<string> RequiredFacts { get; } = new[] { "unsupported.fact" };
            public bool Evaluate(IReadOnlyDictionary<string, bool> facts) => facts["unsupported.fact"];
        }
    }
}
