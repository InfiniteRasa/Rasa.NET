using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using Rasa.Data;
    using Rasa.Game.Missions;
    using Rasa.Managers;
    using Rasa.Missions.Runtime;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class SceneInputDurabilityTests
    {
        [TestMethod]
        public void RecreatedPrivateInstanceCanForwardANewAssignmentsWorkToItsPersistedExperienceRoot()
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            context.Manager.Scenes.Detach(1, context.Map);
            context.Map.InstanceId++;
            context.Manager.Scenes.Attach(context.Client, fixture.RootId, fixture.RootBindings);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));

            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "disable"));

            Assert.IsFalse(fixture.Guide.IsInteractable,
                "A private root's stored instance key predates new assignments accepted after instance recreation.");
            using var verify = context.CreateChar();
            var root = verify.CharacterMissions.Runtime.Scene(fixture.RootId);
            var source = verify.CharacterMissions.Runtime.AssignmentScene(context.ReadMission(321).AssignmentId);
            Assert.AreNotEqual(root.MapKey, source.MapKey);
            Assert.AreEqual("Applied", verify.CharacterMissions.Runtime.ForwardedEffects(source.RunId).Single().Status);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReplacementRetiresDeferredForwardedWorkWithoutReplayingItOnReconnect(bool reconnect)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            fixture.DeferGuideSpawn();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "disable"));
            var old = context.ReadMission(321);
            using (var unit = context.CreateChar())
            {
                var source = unit.CharacterMissions.Runtime.AssignmentScene(old.AssignmentId);
                var effect = unit.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(entry => entry.OperationKey.StartsWith("shared-", StringComparison.Ordinal));
                Assert.AreEqual("Pending", effect.Status);
                Assert.IsNull(effect.Failure);
                Assert.AreEqual(old.AssignmentId, effect.SourceAssignmentId);
                Assert.AreEqual(old.Generation, effect.SourceAssignmentGeneration);
                Assert.AreEqual(source.RunId, effect.SourceRunId);
                Assert.AreEqual(source.Generation, effect.SourceGeneration);
            }
            Assert.IsTrue(context.Manager.TryFailMission(context.Client, 321));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            fixture.RestoreGuideSpawn();
            var scenes = reconnect
                ? new SceneApplication(context, context.Manager, new ManifestationManager(context), utcNow: () => fixture.Now)
                : context.Manager.Scenes;
            try
            {
                if (reconnect)
                {
                    context.Manager.Scenes.Detach(1, context.Map);
                    scenes.Attach(context.Client, fixture.RootId, fixture.RootBindings);
                }
                else
                {
                    fixture.Now = fixture.Now.AddSeconds(1);
                    scenes.Tick(context.Map);
                }
                Assert.IsTrue(fixture.Guide.IsInteractable);
                Assert.IsNotNull(fixture.Independent.Controller.ScriptedMove);
                using var verify = context.CreateChar();
                Assert.AreEqual("Cancelled", verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(effect => effect.SourceAssignmentId == old.AssignmentId).Status);
                Assert.AreEqual(1U, verify.CharacterMissions.Runtime.Scene(fixture.RootId).Generation);
            }
            finally
            {
                if (reconnect)
                    scenes.LeaseReset(fixture.RootId);
            }
        }

        [TestMethod]
        public void ClearedOnceSuccessRetainsItsForwardedExperienceStateOnReconnect()
        {
            using var fixture = new SharedActorRepeatFixture(MissionRepeatKind.Once);
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            var assignment = context.ReadMission(321);
            fixture.DeferGuideSpawn();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "disable"));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            context.Manager.Scenes.Detach(1, context.Map);
            fixture.RestoreGuideSpawn();
            var scenes = new SceneApplication(context, context.Manager, new ManifestationManager(context), utcNow: () => fixture.Now);
            try
            {
                scenes.Attach(context.Client, fixture.RootId, fixture.RootBindings);

                Assert.IsFalse(fixture.Guide.IsInteractable, "A one-time success retains authored experience state after journal clear.");
                using var verify = context.CreateChar();
                Assert.IsNull(verify.CharacterMissions.GetByCharacterAndMission(1, 321));
                Assert.AreEqual("Applied", verify.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(effect => effect.SourceAssignmentId == assignment.AssignmentId).Status);
                Assert.AreEqual((uint)MissionState.Completed, verify.CharacterMissions.Runtime.ReadHistory(assignment.AssignmentId).Outcome);
            }
            finally
            {
                scenes.LeaseReset(fixture.RootId);
            }
        }

        [TestMethod]
        [DataRow("assignment", false)]
        [DataRow("assignment-generation", false)]
        [DataRow("scene-generation", false)]
        [DataRow("participant", false)]
        [DataRow("assignment", true)]
        [DataRow("assignment-generation", true)]
        [DataRow("scene-generation", true)]
        [DataRow("participant", true)]
        public void DeferredForwardedEffectsRecheckTheirSourceBeforeRetryOrReconnect(string change, bool reconnect)
        {
            using var fixture = new SharedActorRepeatFixture();
            var context = fixture.Context;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, fixture.Giver.EntityId, 321));
            fixture.DeferGuideSpawn();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 321, "disable"));
            using (var unit = context.CreateChar())
            {
                var effect = unit.CharacterMissions.Runtime.Effects(fixture.RootId)
                    .Single(entry => entry.OperationKey.StartsWith("shared-", StringComparison.Ordinal));
                Assert.AreEqual("Pending", effect.Status);
                Assert.IsNull(effect.Failure, "The real world adapter must defer an automatic private spawn, not fail it.");
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 321);
                    var source = unit.CharacterMissions.Runtime.AssignmentScene(assignment.AssignmentId);
                    switch (change)
                    {
                        case "assignment": assignment.AssignmentId = Guid.NewGuid().ToString("N"); break;
                        case "assignment-generation": assignment.Generation++; break;
                        case "scene-generation": source.Generation++; break;
                        case "participant": unit.CharacterMissions.Runtime.Participants(source.RunId).Single().Active = false; break;
                    }
                });
            }
            fixture.RestoreGuideSpawn();
            var scenes = reconnect
                ? new SceneApplication(context, context.Manager, new ManifestationManager(context), utcNow: () => fixture.Now)
                : context.Manager.Scenes;
            try
            {
                if (reconnect)
                    scenes.Attach(context.Client, fixture.RootId, fixture.RootBindings);
                else
                {
                    fixture.Now = fixture.Now.AddSeconds(1);
                    scenes.Tick(context.Map);
                }

                Assert.IsTrue(fixture.Guide.IsInteractable, "Stale forwarded work must not disable a root-owned actor.");
                using var verify = context.CreateChar();
                var effects = verify.CharacterMissions.Runtime.Effects(fixture.RootId);
                Assert.AreEqual("Cancelled", effects.Single(effect => effect.OperationKey.StartsWith("shared-", StringComparison.Ordinal)).Status);
                Assert.AreEqual("Running", effects.Single(effect => effect.OperationKey == "root-route").Status);
                Assert.AreEqual("Pending", verify.CharacterMissions.Runtime.Timer(fixture.RootId, "root-wait").Disposition);
            }
            finally
            {
                if (reconnect)
                    scenes.LeaseReset(fixture.RootId);
            }
        }

        [TestMethod]
        public void CommittedNpcObjectiveRecoversItsSceneInputAfterTheSceneWriteFails()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection(useWorldContent: true);
            harness.SpawnWorldNpcs();
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            var giver = BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap, 510203);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1992));
            var delessio = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2560);
            harness.Context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected post-objective scene write failure.");
            };

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1992].Objectives[4].State);
            var crate = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsFalse(crate.IsEnabled);
            using (var unit = harness.Context.CreateChar())
            {
                var run = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 1992).Single();
                Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(run.RunId).Count(message => message.Status == "Pending"));
            }

            harness.Context.BeforeSave = null;
            harness.ReconnectFresh();

            crate = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsTrue(crate.IsEnabled, "The objective's durable input must survive even though its post-commit scene dispatch failed.");
            using (var unit = harness.Context.CreateChar())
            {
                var run = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 1992).Single();
                Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(run.RunId).Count(message => message.Status == "Handled"));
            }
        }
    }
}
