using System;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class BootcampMapSetupTests
    {
        private static readonly Vector3 CratePosition = new(398, 122, 173);
        private static readonly Vector3 AlisterDestination = new(400, 120, 150);

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void McAllisterReconnectWaitsForStaticSpawningWithoutReportingAnActorFailure(bool finishRun)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var original = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsNotNull(original);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(original);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, original.EntityId, 1992));
            for (var tick = 0; tick < (finishRun ? 600 : 4); tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            var previousOutput = Console.Out;
            var previousLogging = Logger.Config;
            using var output = new StringWriter();
            Logger.UpdateConfig(new Logger.LoggerConfig { IsDebugMode = true, LogToFile = false });
            Console.SetOut(output);
            try
            {
                harness.ReconnectFresh();
                Assert.IsTrue(harness.BootcampMap.SpawnPools.Any(pool =>
                    pool.DbId == BootcampRuntimeTestHarness.MajorMcAllisterCreatureId));
                Assert.IsNull(BootcampRuntimeTestHarness.FindCreature(
                    harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId));
                for (var retry = 0; retry < 3; retry++)
                {
                    harness.UtcNow += TimeSpan.FromSeconds(1);
                    harness.Manager.TickScenarios(harness.Client);
                }
                using (var unit = harness.Context.CreateChar())
                {
                    var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 0).Single();
                    var pending = unit.CharacterMissions.Runtime.Effects(scene.RunId)
                        .Single(effect => effect.OperationKey == "ensure-mcallister");
                    Assert.AreEqual("Pending", pending.Status);
                    Assert.IsNull(pending.Failure, "Waiting for a valid spawn must not persist a failure.");
                }

                SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

                var restored = BootcampRuntimeTestHarness.FindCreature(
                    harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
                Assert.IsNotNull(restored);
                Assert.AreEqual(AlisterDestination, restored.Position,
                    "Recover the authored final pose before the normal spawn worker introduces the actor.");
                using (var unit = harness.Context.CreateChar())
                {
                    var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 0).Single();
                    var applied = unit.CharacterMissions.Runtime.Effects(scene.RunId)
                        .Single(effect => effect.OperationKey == "ensure-mcallister");
                    Assert.AreEqual("Applied", applied.Status, "The normal spawn notification must complete the pending ensure.");
                    Assert.IsNull(applied.Failure);
                }
                harness.UtcNow += TimeSpan.FromSeconds(1);
                harness.Manager.TickScenarios(harness.Client);
                Assert.AreEqual(2.175, restored.Rotation, 0.001);
                Assert.IsFalse(restored.IsRunning);
                Assert.AreEqual(1, harness.BootcampMap.MapCellInfo.Cells.Values
                    .SelectMany(cell => cell.CreatureList)
                    .Count(creature => creature.DbId == BootcampRuntimeTestHarness.MajorMcAllisterCreatureId));
            }
            finally
            {
                Console.SetOut(previousOutput);
                Logger.UpdateConfig(previousLogging);
            }
            Assert.IsFalse(output.ToString().Contains("[Error]", StringComparison.Ordinal),
                $"A valid static spawn awaiting the first spawn worker is not a broken scene:{Environment.NewLine}{output}");
        }

        [TestMethod]
        public void McAllisterDepartureStartsWhenItsInitiallyDeferredActorBecomesAvailable()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Active, false);
            harness.Manager.Scenes.StageExperience(harness.Client.Player.Id, harness.BootcampMap);
            using (var unit = harness.Context.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 0).Single();
                foreach (var effect in unit.CharacterMissions.Runtime.Effects(scene.RunId)
                    .Where(effect => effect.OperationKey is "ensure-mcallister" or "mcallister-departure"))
                {
                    Assert.AreEqual("Pending", effect.Status);
                    Assert.IsNull(effect.Failure);
                }
            }

            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            var actor = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsNotNull(actor);
            Assert.IsTrue(actor.IsRunning);
            using var verify = harness.Context.CreateChar();
            var recoveredScene = verify.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 0).Single();
            var effects = verify.CharacterMissions.Runtime.Effects(recoveredScene.RunId);
            Assert.AreEqual("Applied", effects.Single(effect => effect.OperationKey == "ensure-mcallister").Status);
            Assert.AreEqual("Running", effects.Single(effect => effect.OperationKey == "mcallister-departure").Status);
        }

        [TestMethod]
        [DataRow("missing-pool")]
        [DataRow("missing-creature")]
        [DataRow("disabled-pool")]
        [DataRow("empty-pool")]
        [DataRow("invalid-counts")]
        [DataRow("missing-live-actor")]
        public void BrokenMcAllisterSpawnStillRecordsAFailure(string failure)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var pool = harness.BootcampMap.SpawnPools.Single(candidate =>
                candidate.DbId == BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            switch (failure)
            {
                case "missing-pool": harness.BootcampMap.SpawnPools.Remove(pool); break;
                case "missing-creature":
                    CreatureManager.Instance.LoadedCreatures.Remove(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
                    break;
                case "disabled-pool": pool.Mode = 1; break;
                case "empty-pool": pool.SpawnSlot.Clear(); break;
                case "invalid-counts": pool.SpawnSlot[0].CountMin = -1; break;
                case "missing-live-actor": pool.AliveCreatures = 1; break;
            }
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Active, false);

            harness.Manager.Scenes.StageExperience(harness.Client.Player.Id, harness.BootcampMap);

            using var unit = harness.Context.CreateChar();
            var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, 0).Single();
            var effect = unit.CharacterMissions.Runtime.Effects(scene.RunId)
                .Single(entry => entry.OperationKey == "ensure-mcallister");
            Assert.AreEqual("Pending", effect.Status);
            StringAssert.Contains(effect.Failure, "Public actor spawn 510203 is unavailable");
        }

        [TestMethod]
        public void CrateIsVisibleButLockedBeforeAcceptingAnyMission()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(1992));
            harness.Drain();
            harness.MovePlayerTo(CratePosition);
            CellManager.Instance.UpdateVisibility(harness.Client);

            var crate = harness.BootcampMap.DynamicObjects
                .SingleOrDefault(candidate => (uint)candidate.EntityClassId == 29877);
            Assert.IsNotNull(crate, "The equipment crate must already exist before mission acceptance.");
            Assert.AreEqual(CratePosition, crate.Position);
            Assert.IsFalse(crate.IsEnabled, "The pre-mission crate must not be usable.");
            Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Any(packet => packet.EntityId == crate.EntityId));

            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);

            Assert.IsFalse(harness.Drain().OfType<LootCorpsePacket>().Any());
            Assert.AreEqual(0UL, crate.LootDispenserEntityId);
        }

        [TestMethod]
        public void TheVisibleCrateStaysLockedUntilDelessioAndThenOpensWithoutBeingReplaced()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var crate = harness.BootcampMap.DynamicObjects.Single(candidate => (uint)candidate.EntityClassId == 29877);
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsFalse(crate.IsEnabled);
            Assert.AreEqual(0UL, crate.LootDispenserEntityId);
            var alister = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(alister);
            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));

            Assert.IsFalse(crate.IsEnabled, "Acceptance must not skip Delessio's gear briefing.");
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var delessio = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            harness.Drain();

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client, delessio.EntityId, 1992, 4, 1));

            Assert.AreSame(crate, harness.BootcampMap.DynamicObjects
                .Single(candidate => (uint)candidate.EntityClassId == 29877));
            Assert.IsTrue(crate.IsEnabled);
            Assert.AreNotEqual(0UL, crate.LootDispenserEntityId);
            Assert.IsFalse(harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Any(packet => packet.EntityId == crate.EntityId), "Enabling the crate must not respawn its prop.");
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            Assert.AreEqual(1, harness.Drain().OfType<LootCorpsePacket>().Count());
            Assert.AreEqual(6, harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId].Remaining().Count);
        }

        [TestMethod]
        public void TheUnacceptedCrateRemainsVisibleAndLockedAfterFreshReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();

            harness.ReconnectFresh();
            harness.MovePlayerTo(CratePosition);
            CellManager.Instance.UpdateVisibility(harness.Client);

            var crate = harness.BootcampMap.DynamicObjects.Single(candidate => (uint)candidate.EntityClassId == 29877);
            Assert.IsFalse(crate.IsEnabled);
            Assert.AreEqual(0UL, crate.LootDispenserEntityId);
            Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Any(packet => packet.EntityId == crate.EntityId));
        }

        [TestMethod]
        public void DeSimoneActuallySpawnsOnWalkableGroundAndIsIntroducedToTheClient()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            harness.SpawnWorldNpcs();
            var deSimone = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            Assert.IsNotNull(deSimone, "DeSimone must be an actual world NPC, not only a mission definition.");
            Assert.AreSame(deSimone, EntityManager.Instance.GetCreature(deSimone.EntityId));
            var ground = harness.BootcampMap.NavMesh.Nearest(deSimone.Position);
            Assert.IsTrue(ground.HasValue, $"No walkable surface near DeSimone at {deSimone.Position}.");
            Assert.IsTrue(Math.Abs(ground.Value.Y - deSimone.Position.Y) < 0.5f,
                $"DeSimone is at {deSimone.Position}, but the walkable surface is {ground.Value}.");
            var hartmann = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalHartmannCreatureId);
            Assert.IsNotNull(hartmann);
            var route = harness.BootcampMap.NavMesh.FindPath(
                hartmann.Position, deSimone.Position, out var complete);
            Assert.IsNotNull(route);
            Assert.IsTrue(complete, "DeSimone must be reachable from Hartmann's handoff.");
            harness.Drain();
            harness.MovePlayerTo(ground.Value);

            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Any(packet => packet.EntityId == deSimone.EntityId),
                "The player must receive DeSimone's physical entity at his actual spawn.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TheSpawnedDeSimoneTurnsInGearingUpAndOffersCaptureTheFlag(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Active, true);
            if (reconnect)
                harness.ReconnectFresh();
            harness.SpawnWorldNpcs();
            var deSimone = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            Assert.IsNotNull(deSimone);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.ClassifyNpcConversation(harness.Client.Player, deSimone)
                .TryGetStatus(out var status, out _));
            Assert.AreEqual(ConversationStatus.MissionComplete, status);

            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client, deSimone.EntityId, 1992, null, null));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));

            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[1992].State);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[1994].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1994].Objectives[4].State);
        }

        [TestMethod]
        public void AcceptingGearingUpRunsAlisterToHisFinalPositionAndLeavesHimThere()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var alister = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsNotNull(alister);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(alister);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var start = alister.Position;

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));
            Assert.IsTrue(harness.Drain().OfType<IsRunningPacket>().Any(packet => packet.IsRunning));
            for (var tick = 0; tick < 4; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            Assert.IsTrue(Vector3.Distance(start, alister.Position) > 0.1f,
                "Alister must start moving after Gearing Up is accepted.");
            Assert.IsTrue(Vector3.Distance(alister.Position, AlisterDestination) > 1,
                "Alister must run to the destination rather than teleport there.");
            for (var tick = 0; tick < 600; tick++)
            {
                var previous = alister.Position;
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
                if (previous.X != alister.Position.X || previous.Z != alister.Position.Z)
                {
                    // Detour can choose either adjoining polygon at an exact path vertex.
                    var insideSegment = Vector3.Lerp(previous, alister.Position, 0.001f);
                    var beforeEnd = Vector3.Lerp(previous, alister.Position, 0.999f);
                    Assert.IsTrue(harness.BootcampMap.NavMesh.IsWalkClear(insideSegment, beforeEnd),
                        $"Alister cut across blocked ground between {previous} and {alister.Position}.");
                }
            }

            Assert.IsTrue(Vector3.Distance(AlisterDestination, alister.Position) < 0.01f,
                $"Alister stopped at {alister.Position} instead of {AlisterDestination}.");
            Assert.AreEqual(2.175, alister.Rotation, 0.001);
            Assert.IsFalse(alister.IsRunning);
            var neighbor = harness.AddNpc(7777, position: AlisterDestination + new Vector3(0.3f, 0, 0));
            neighbor.Faction = Factions.AFS;
            for (var tick = 0; tick < 240; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            Assert.IsTrue(Vector3.Distance(AlisterDestination, alister.Position) < 0.01f);
            Assert.AreEqual(2.175, alister.Rotation, 0.001);
        }

        [TestMethod]
        public void AlisterWaitsForSuccessfulAcceptanceAndARejectedSaveDoesNotMoveHim()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var alister = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var start = alister.Position;
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));
            harness.Manager.ScenarioService.Tick(harness.Client);
            for (var tick = 0; tick < 160; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            Assert.AreEqual(start, alister.Position);
            Assert.IsFalse(alister.IsRunning);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(alister);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected acceptance failure.");
            harness.Drain();

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));

            harness.Context.BeforeSave = null;
            harness.Manager.ScenarioService.Tick(harness.Client);
            for (var tick = 0; tick < 20; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            Assert.AreEqual(start, alister.Position);
            Assert.IsFalse(harness.Drain().OfType<IsRunningPacket>().Any(packet => packet.IsRunning));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(1992));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));
            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            Assert.AreNotEqual(start, alister.Position);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AlistersFinalPositionSurvivesReconnectDuringOrAfterHisRun(bool finishRun)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var alister = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(alister);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));
            for (var tick = 0; tick < (finishRun ? 600 : 4); tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            harness.ReconnectFresh();
            harness.SpawnWorldNpcs();
            harness.Manager.ScenarioService.Tick(harness.Client);
            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            var restored = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsNotNull(restored);
            Assert.AreNotSame(alister, restored);
            Assert.AreEqual(AlisterDestination, restored.Position);
            Assert.AreEqual(2.175, restored.Rotation, 0.001);
            Assert.IsFalse(restored.IsRunning);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, restored.EntityId, 1992));
            harness.Manager.RebuildScenarioRuntime(harness.Client.Player.Id, harness.BootcampMap);
            for (var tick = 0; tick < 160; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
            Assert.AreEqual(AlisterDestination, restored.Position);
            Assert.AreEqual(2.175, restored.Rotation, 0.001);
        }

        [TestMethod]
        public void AlistersDepartureAndCrateUnlockDoNotAffectAnotherPlayersInstance()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.SpawnWorldNpcs();
            var otherMap = harness.Maps.GetOrCreatePrivateInstance(1985, 999);
            try
            {
                otherMap.SpawnPools.RemoveAll(pool =>
                    pool.DbId < BootcampRuntimeTestHarness.MajorMcAllisterCreatureId ||
                    pool.DbId > BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
                SpawnPoolManager.Instance.SpawnPoolWorker(otherMap, 0);
                var otherAlister = BootcampRuntimeTestHarness.FindCreature(
                    otherMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
                Assert.IsNotNull(otherAlister);
                var otherStart = otherAlister.Position;
                var otherCrate = otherMap.DynamicObjects.Single(candidate => (uint)candidate.EntityClassId == 29877);
                var alister = BootcampRuntimeTestHarness.FindCreature(
                    harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
                harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
                harness.MovePlayerTo(alister);
                CellManager.Instance.UpdateVisibility(harness.Client);

                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, alister.EntityId, 1992));
                var delessio = BootcampRuntimeTestHarness.FindNpcByPackage(
                    harness.BootcampMap, BootcampRuntimeTestHarness.CaptainDelessioPackageId);
                harness.MovePlayerTo(delessio);
                CellManager.Instance.UpdateVisibility(harness.Client);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                    harness.Client, delessio.EntityId, 1992, 4, 1));
                for (var tick = 0; tick < 160; tick++)
                {
                    BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
                    BehaviorManager.Instance.MapChannelThink(otherMap, 250);
                }

                Assert.AreEqual(AlisterDestination, alister.Position);
                Assert.AreEqual(otherStart, otherAlister.Position);
                Assert.IsFalse(otherAlister.IsRunning);
                Assert.IsFalse(otherCrate.IsEnabled);
                Assert.AreEqual(0UL, otherCrate.LootDispenserEntityId);
                var crate = harness.BootcampMap.DynamicObjects.Single(candidate => (uint)candidate.EntityClassId == 29877);
                Assert.IsTrue(crate.IsEnabled);
                Assert.AreNotEqual(crate.EntityId, otherCrate.EntityId);
            }
            finally
            {
                harness.Maps.ReleaseOwnedPrivateInstances(999);
            }
        }
    }
}
