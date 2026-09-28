extern alias RasaGame;

using Rasa.Missions.Content;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ClientState = RasaGame::Rasa.Data.ClientState;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Missions.Content;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Models;
    using Rasa.Missions.Scenes;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char.CharacterMissionScenario;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampCallingForReinforcementsTests
    {
        private const uint MissingScoutAreaId = 435;
        private static readonly TimeSpan BombDeadline = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ArrivalDelay = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void PlantingBombStartsExactlySixHostileAssaultActorsBeforeTheFuseEnds()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));

            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));

            var attackers = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Where(actor => actor.SpawnPool?.SceneActorRole?.StartsWith("assault-", StringComparison.Ordinal) == true)
                .ToArray();
            Assert.HasCount(6, attackers, "Planting must begin the authored uphill assault.");
            Assert.IsTrue(attackers.All(actor => actor.Faction == Factions.Bane));
            Assert.IsTrue(attackers.All(actor =>
                actor.SpawnPool.ScenarioOwnerCharacterId == harness.Client.Player.Id));
            Assert.IsTrue(attackers.All(actor => actor.Position.Y < 96),
                "Attackers must begin below the extraction hill, not spawn on the player.");
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        [TestMethod]
        public void EvacuationArrivesWithTwoSoldiersWhileVanCheckInRemainsLockedByTheAssault()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);
            harness.UtcNow += ArrivalDelay;
            harness.Manager.TickScenarios(harness.Client);

            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-evacuation-ship"),
                "The evacuation ship must arrive before the assault is over, bringing reinforcements.");
            var soldiers = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Where(actor => actor.SpawnPool?.SceneActorRole?.StartsWith("reinforcement-", StringComparison.Ordinal) == true)
                .ToArray();
            Assert.HasCount(2, soldiers);
            Assert.IsTrue(soldiers.All(actor => actor.Faction == Factions.AFS && actor.Actions.Count > 0));
            var van = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId);
            Assert.IsNotNull(van);
            harness.MovePlayerTo(van);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, van.EntityId, 1995, 4, 1),
                "The check-in must wait for all assault enemies to be defeated.");
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        [TestMethod]
        public void ReconnectingAnAlreadyCheckedInCharacterClearsTheOldWreckBeforeStagingEvacuation()
        {
            RemoveActorIntent wreckCleanup = null;
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true,
                configureScenes: scenes =>
                {
                    var scene = scenes[1995];
                    var arrival = scene.Sequences[scene.Names["exit"]];
                    wreckCleanup = arrival.World.OfType<RemoveActorIntent>()
                        .Single(intent => intent.Role == "bootcamp-dropship-debris");
                    arrival.World.Remove(wreckCleanup);
                });
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);
            harness.UtcNow += ArrivalDelay;
            harness.Manager.TickScenarios(harness.Client);
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
            BootcampExtractionAssaultTests.DefeatAll(harness);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterMissionProgress.GetTracked(harness.Client.Player.Id, 1995)[4].ObjectiveState =
                        (byte)MissionObjectiveState.Completed;
                    unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 1995).Completeable = true;
                });
            Content.MissionContentTestSupport.ConfigureScenes(harness.WorldContext,
                scenes => scenes[1995].Sequences[scenes[1995].Names["exit"]].World.Insert(0, wreckCleanup));

            harness.ReconnectFresh();

            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-evacuation-ship"));
            Assert.IsFalse(harness.BootcampMap.SpawnPools.Any(pool =>
                pool.SpawnSlot.Any(slot => slot.CreatureId == Rasa.Services.Preloader.Missions.BootcampExtractionDataV5.AssaultTemplate)));
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        [TestMethod]
        public void BombDetonationPublishesTheNativeWreckExplosionTransition()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            var conrad = FindScenarioObject(harness, "bootcamp-conrad-corpse");
            harness.MovePlayerTo(conrad);
            CellManager.Instance.UpdateVisibility(harness.Client);
            UseNativeObject(harness, conrad);
            var wreck = FindScenarioObject(harness, "bootcamp-dropship-debris");
            harness.MovePlayerTo(wreck);
            CellManager.Instance.UpdateVisibility(harness.Client);
            UseNativeObject(harness, wreck);
            harness.Drain();
            harness.UtcNow += TimeSpan.FromSeconds(5);

            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            Assert.AreEqual(UseObjectState.DoorStateOpen, wreck.StateId,
                "The native tutorial-wreck class uses its closed-to-open transition for the detonation.");
            Assert.IsTrue(harness.Drain().OfType<UsePacket>()
                .Any(packet => packet.CurState == UseObjectState.DoorStateOpen),
                "The fuse must emit the native state-transition cue, not only complete an objective.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EvacuationArrivesAfterWreckClearanceButBoardingWaitsForAssaultAndCheckIn(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            using (var unit = harness.Context.CreateChar())
                unit.CharacterStartingExperience.Add(new CharacterStartingExperienceEntry(
                    harness.Client.Player.Id, "deployment_11", CharacterStartingExperienceState.Bootcamp));
            Assert.IsFalse(harness.BootcampMap.DynamicObjects.Any(obj =>
                obj.EntityClassId == EntityClasses.UsableTwoStateHumDropshipBeam),
                "Bootcamp must not start with an evacuation ship hovering over its old pad.");
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            var wreck = FindScenarioObject(harness, "bootcamp-dropship-debris");
            var pad = harness.BootcampMap.Teleporters[60];
            Assert.AreEqual(wreck.Position, pad.Position);
            harness.UseObjectAndRecover(wreck);
            harness.UtcNow += TimeSpan.FromSeconds(5);
            harness.Manager.TickScenarios(harness.Client);
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(wreck));
            harness.UtcNow += ArrivalDelay;
            harness.Manager.TickScenarios(harness.Client);
            Assert.IsFalse(harness.BootcampMap.DynamicObjects.Contains(wreck));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-evacuation-ship"));
            harness.MovePlayerTo(pad);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            new MapTriggerManager().TriggersProximityWorker(harness.BootcampMap);
            Assert.IsFalse(harness.Drain().OfType<EnteredWaypointPacket>().Any(),
                "Extraction must not open a waypoint menu.");
            Assert.IsNull(harness.Client.PendingTransfer);
            var van = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId);

            BootcampExtractionAssaultTests.DefeatAll(harness);
            CompleteNativeObjective(harness, van, 4);

            var evacuation = FindScenarioObject(harness, "bootcamp-evacuation-ship");
            Assert.IsNotNull(evacuation);
            Assert.AreEqual(pad.Position, evacuation.Position);
            Assert.IsNull(harness.Client.PendingTransfer, "Check-in must not board the player automatically.");
            if (reconnect)
            {
                harness.ReconnectFresh();
                Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
                Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-evacuation-ship"));
                Assert.IsNull(harness.Client.PendingTransfer);
            }
            harness.MovePlayerTo(pad);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            new MapTriggerManager().TriggersProximityWorker(harness.BootcampMap);
            Assert.IsFalse(harness.Drain().OfType<EnteredWaypointPacket>().Any());
            Assert.IsNotNull(harness.Client.PendingTransfer,
                "Walking into the beam after check-in must start the one-way flight.");
        }

        [TestMethod]
        public void FullMissionInventoryRejectsBombPickupUntilOneSlotIsFreed()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.Context.AddRewardTemplate(999001, 999001);
            var itemClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)999001];
            itemClass.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { StackSize = 1, MaxHitPoints = 1 });
            itemClass.ItemTemplates[999001].InventoryCategory = InventoryCategory.Mission;
            using (var unit = harness.Context.CreateChar())
            using (var grant = new InventoryManager.InventoryGrant())
            {
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(999001, 50) }, unit));
                grant.Publish(harness.Client);
            }
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            using (var unit = harness.Context.CreateChar())
            {
                var consume = new InventoryManager.InventoryConsumption();
                var itemId = harness.Client.Player.Inventory.PersonalInventory[150];
                unit.ExecuteTransaction(() => consume.PlanAndSave(harness.Client,
                    new Dictionary<ulong, uint> { [itemId] = 1 }, unit));
                consume.Publish(harness.Client);
            }

            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }


        [TestMethod]
        public void FailedBombPickupRollsBackItemAndObjectiveThenCanRetry()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected bomb pickup failure.");
            try
            {
                harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            }
            finally
            {
                harness.Context.AfterSave = null;
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));

            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));

            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[3].State);
        }

        [TestMethod]
        public void AbandoningTheBombAttemptRemovesItsMissionItem()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));

            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ConradBombIsAStoredMissionItemUntilPlanting(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U),
                "Recovering Conrad's bomb must place the native Explosives Detonator in mission inventory.");
            if (reconnect)
                harness.ReconnectFresh();
            var bomb = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplate.ItemTemplateId == 11519);
            Assert.IsTrue(bomb.OwnerSlotId is >= 150 and < 200);
            Assert.AreEqual(1U, bomb.StackSize);
            var deadline = ReadDeadline(harness, 1995).DueAtUtc;

            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));

            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            Assert.AreEqual(deadline, ReadDeadline(harness, 1995).DueAtUtc);
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, ReadDeadline(harness, 1995).State);
            harness.ReconnectFresh();
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        [TestMethod]
        public void BombTimeoutRemovesTheItemAndRetryIssuesOnlyOneReplacement()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            harness.UtcNow = ReadDeadline(harness, 1995).DueAtUtc + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            var youngblood = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2561);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, youngblood.EntityId, 2005));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            harness.ReconnectFresh();
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void BombWreckIsPublishedAsAnEnabledTargetableInteraction(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            if (reconnect)
                harness.ReconnectFresh();
            var wreck = FindScenarioObject(harness, "bootcamp-dropship-debris");
            var approach = harness.BootcampMap.NavMesh.Nearest(wreck.Position + new Vector3(4, 0, -0.9f));
            Assert.IsTrue(approach.HasValue);
            harness.Drain();
            harness.MovePlayerTo(approach.Value);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var created = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Single(packet => packet.EntityId == wreck.EntityId);

            Assert.IsTrue(created.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable,
                "The native body must allow mouse targeting of the mission's bomb-planting interaction.");
            Assert.IsTrue(created.EntityData.OfType<UsableInfoPacket>().Single().Enabled);
            UseNativeObject(harness, wreck);
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, ReadDeadline(harness, 1995).State);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ConradIsIntroducedAtTheGroundedObjectiveMarkerAndCanBeUsed(bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            if (reconnect)
                harness.ReconnectFresh();
            var conrad = FindScenarioObject(harness, "bootcamp-conrad-corpse");
            var indicator = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[1995].ObjectivesList
                .Single(objective => objective.ObjectiveId == 3).IndicatorList
                .Single(candidate => candidate.IndicatorId == 436);
            var marker = indicator.Position;
            var ground = harness.BootcampMap.NavMesh.Nearest(marker);
            Assert.IsTrue(ground.HasValue, $"Conrad's marker has no ground at {marker}.");
            var survivor = harness.WorldContext.MissionSpawnEntries.Single(row =>
                row.MissionId == 1995 && row.SpawnGroupId == 1 && row.SpawnId == 1);
            var survivorPosition = new Vector3((float)survivor.PosX, (float)survivor.PosY, (float)survivor.PosZ);
            var route = harness.BootcampMap.NavMesh.FindPath(survivorPosition, conrad.Position, out var complete);
            Assert.IsNotNull(route);
            Assert.IsTrue(complete,
                $"Conrad at {conrad.Position} must be on reachable ground, not the navmesh island inside the trench wall.");
            Assert.IsLessThan(0.25f, Math.Abs(conrad.Position.Y - ground.Value.Y),
                $"Conrad at {conrad.Position} must be visible above the ground at {ground.Value}.");
            Assert.IsLessThan(0.25f, Vector2.Distance(
                new Vector2(marker.X, marker.Z), new Vector2(conrad.Position.X, conrad.Position.Z)));
            Assert.IsLessThan(6f, Vector3.Distance(survivorPosition, conrad.Position));
            // The shipped corpse's footprint must fit on connected ground, not just its origin.
            foreach (var x in new[] { -0.41631162f, 0f, 0.45468274f })
                foreach (var z in new[] { -1.0020568f, 0f, 0.42947608f })
                {
                    var sample = conrad.Position + new Vector3(x, 0, z);
                    var surface = harness.BootcampMap.NavMesh.Nearest(sample);
                    Assert.IsTrue(surface.HasValue);
                    Assert.IsLessThan(0.1f, Vector2.Distance(
                        new Vector2(sample.X, sample.Z), new Vector2(surface.Value.X, surface.Value.Z)));
                    Assert.IsTrue(harness.BootcampMap.NavMesh.IsWalkClear(ground.Value, surface.Value),
                        $"Conrad's body extends into blocked ground at {sample}.");
                }
            harness.Drain();
            harness.MovePlayerTo(ground.Value);

            CellManager.Instance.UpdateVisibility(harness.Client);

            var introduced = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .SingleOrDefault(packet => packet.EntityId == conrad.EntityId);
            Assert.IsNotNull(introduced, "Approaching the objective marker must introduce Conrad to the client.");
            Assert.AreEqual((EntityClasses)21081, introduced.ClassId);
            Assert.AreEqual(conrad.Position,
                introduced.EntityData.OfType<WorldLocationDescriptorPacket>().Single().Position);
            Assert.IsTrue(conrad.IsEnabled);
            UseNativeObject(harness, conrad);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(harness.UtcNow + BombDeadline, ReadDeadline(harness, 1995).DueAtUtc);
        }


        [TestMethod]
        public void AuthoredFinalePositionsKeepTheConfirmedCrashSiteAndGroundedHandoff()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            var survivor = harness.WorldContext.MissionSpawnEntries.Single(row =>
                row.MissionId == 1995 && row.SpawnGroupId == 1 && row.SpawnId == 1);
            Assert.AreEqual(-101.2, survivor.PosX, 0.0001);
            Assert.AreEqual(86.40231, survivor.PosY, 0.0001);
            Assert.AreEqual(70.8, survivor.PosZ, 0.0001);
            var conrad = FindScenarioObject(harness, "bootcamp-conrad-corpse");
            Assert.IsLessThan(0.001f, Vector3.Distance(
                new Vector3(-99, 86.33577f, 74), conrad.Position));
            var wreck = FindScenarioObject(harness, "bootcamp-dropship-debris");
            Assert.IsLessThan(0.001f, Vector3.Distance(
                new Vector3(-225, 101.12099f, -71), wreck.Position));
            foreach (var missionId in new[] { 1995U, 2005U })
            {
                foreach (var (spawnId, creatureId, position) in new[]
                         {
                             (1U, Rasa.Services.Preloader.Missions.BootcampExtractionDataV5.SoldierTemplate, new Vector3(-218, 101.08475f, -78)),
                             (2U, Rasa.Services.Preloader.Missions.BootcampExtractionDataV5.SoldierTemplate, new Vector3(-221, 101.2538f, -74)),
                             (1U, 510209U, new Vector3(-223, 101.269646f, -76))
                         })
                {
                    var spawn = harness.WorldContext.MissionSpawnEntries.Single(row =>
                        row.MissionId == missionId && row.CreatureId == creatureId && row.SpawnId == spawnId);
                    Assert.AreEqual(position.X, spawn.PosX, 0.0001);
                    Assert.AreEqual(position.Y, spawn.PosY, 0.0001);
                    Assert.AreEqual(position.Z, spawn.PosZ, 0.0001);
                }
                var marker = harness.WorldContext.MissionIndicatorEntries.Single(row =>
                    row.MissionId == missionId && row.ObjectiveId == 1 && row.IndicatorId == 432);
                Assert.AreEqual(-225.0, marker.PosX, 0.0001);
                Assert.AreEqual(101.12099, marker.PosY, 0.0001);
                Assert.AreEqual(-71.0, marker.PosZ, 0.0001);
                var exit = harness.WorldContext.MissionAreaEntries.Single(row =>
                    row.MissionId == missionId && row.AreaId == 438);
                Assert.AreEqual(101.1059, exit.PosY, 0.0001);
            }
            var search = harness.WorldContext.MissionAreaEntries.Single(row =>
                row.MissionId == 1995 && row.AreaId == 435);
            Assert.AreEqual(12.0, search.Radius);
        }

        [TestMethod]
        [DataRow(false, true)]
        [DataRow(true, true)]
        [DataRow(false, false)]
        [DataRow(true, false)]
        public void RebuildingLegacyCrateUsesTheWreckWithoutResettingTheBombAttempt(bool planted, bool legacyClass)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            var legacy = FindScenarioObject(harness, "bootcamp-dropship-debris");
            if (planted)
                harness.UseObjectAndRecover(legacy);
            var deadline = ReadDeadline(harness, 1995);
            var receipts = ReadScenarioKeys(harness, 1995);
            if (legacyClass)
                legacy.EntityClassId = (EntityClasses)24911;
            else
                legacy.Position -= new Vector3(0, 2, 0);
            legacy.StateId = UseObjectState.IdStateActive;
            FindScenarioObject(harness, "bootcamp-conrad-corpse").StateId = UseObjectState.IdStateActive;

            harness.Manager.RebuildScenarioRuntime(harness.Client.Player.Id, harness.BootcampMap);

            var rebuilt = FindScenarioObject(harness, "bootcamp-dropship-debris");
            Assert.AreEqual(24586U, (uint)rebuilt.EntityClassId);
            Assert.IsLessThan(0.001f, Vector3.Distance(
                new Vector3(-225, 101.12099f, -71), rebuilt.Position));
            Assert.AreEqual(UseObjectState.DoorStateClosed, rebuilt.StateId);
            Assert.AreEqual((UseObjectState)0, FindScenarioObject(harness, "bootcamp-conrad-corpse").StateId);
            Assert.AreNotEqual(legacy.EntityId, rebuilt.EntityId);
            Assert.AreEqual(!planted, rebuilt.IsEnabled);
            Assert.AreEqual(deadline.DueAtUtc, ReadDeadline(harness, 1995).DueAtUtc);
            Assert.AreEqual(deadline.State, ReadDeadline(harness, 1995).State);
            CollectionAssert.AreEquivalent(receipts, ReadScenarioKeys(harness, 1995));
        }


        [TestMethod]
        public void WorldMovementAndNativeCorpseDialogueUseOnlyClientObjectivesThenReachExtraction()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var youngblood = harness.AddNpc(BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId, 2561);
            harness.SeedMission(harness.Client.Player.Id, 1994, (uint)MissionState.Completed, true);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, youngblood.EntityId, 1995));
            AssertSupportedObjectives(harness);

            var area = harness.WorldContext.MissionAreaEntries.Single(row => row.MissionId == 1995 && row.AreaId == 435);
            var center = new Vector3((float)area.PosX, (float)area.PosY, (float)area.PosZ);
            harness.MovePlayerTo(center + new Vector3((float)area.Radius.Value + 0.25f, 0, 0));
            harness.Client.Movement = new Movement(harness.Client.Player.Position, 1, 0, Vector2.Zero);
            harness.Client.Player.MoveBudget = 10;
            harness.Client.MissionAreaService = new MissionAreaService(() => harness.Manager);
            Assert.IsTrue(harness.Client.HandleMovement(new Movement(
                center + new Vector3((float)area.Radius.Value - 0.25f, 0, 0), 1, 0, Vector2.Zero)));

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[2].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2584));
            AssertSupportedObjectives(harness);

            var conrad = FindScenarioObject(harness, "bootcamp-conrad-corpse");
            Assert.AreEqual((EntityClasses)21081, conrad.EntityClassId);
            harness.MovePlayerTo(conrad);
            UseNativeObject(harness, conrad);
            Assert.AreEqual(harness.UtcNow + BombDeadline, ReadDeadline(harness, 1995).DueAtUtc);
            var wreck = FindScenarioObject(harness, "bootcamp-dropship-debris");
            Assert.AreEqual(24586U, (uint)wreck.EntityClassId,
                "Plant the bomb on the tutorial dropship wreck, not the dropship crate.");
            Assert.AreEqual(UseObjectState.DoorStateClosed, wreck.StateId);
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Interaction(24911)),
                "Using an unrelated dropship crate must not plant the mission bomb.");
            var damageOffset = Vector3.Transform(new Vector3(3.60532f, 3.21575f, 0.4568534f),
                new Quaternion(-0.20722169f, 0, 0, 0.97829396f));
            var approach = harness.BootcampMap.NavMesh.Nearest(wreck.Position + new Vector3(4, 0, damageOffset.Z));
            Assert.IsTrue(approach.HasValue);
            // Verified mesh Damage1 connector and east collision face, not the server's generous use radius.
            var damagePoint = wreck.Position + damageOffset;
            Assert.IsTrue(approach.Value.X > wreck.Position.X + 2.933772f);
            Assert.IsTrue(Vector3.Distance(approach.Value, damagePoint) <= 6,
                "The ground approach must satisfy the native client's six-metre use check.");
            var path = harness.BootcampMap.NavMesh.FindPath(harness.Client.Player.Position, approach.Value, out var complete);
            Assert.IsNotNull(path);
            Assert.IsTrue(complete, "The actual dialogue/bomb-recovery ground must connect to the wreck-side approach.");
            harness.MovePlayerTo(approach.Value);
            CellManager.Instance.UpdateVisibility(harness.Client);
            UseNativeObject(harness, wreck);
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, ReadDeadline(harness, 1995).State);
            harness.UtcNow += TimeSpan.FromSeconds(5);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            var van = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2564);
            Assert.IsNotNull(van);
            Assert.AreEqual((EntityClasses)3846, van.EntityClass,
                "The real-content regression must load the authored handoff model.");
            BootcampExtractionAssaultTests.DefeatAll(harness);
            CompleteNativeObjective(harness, van, 4);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[4].State);
            Assert.IsTrue(harness.Client.Player.Missions[1995].Completeable);
            AssertSupportedObjectives(harness);
        }

        private static void UseNativeObject(BootcampRuntimeTestHarness.Harness harness, DynamicObject target)
        {
            if (target.MissionConversation != null)
            {
                harness.UseObjectAndRecover(target);
                return;
            }
            DynamicObjectManager.Instance.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.FootlockerUseArgId,
                EntityId = target.EntityId
            });
            harness.AdvanceRecovery(target.WindupTime);
        }

        private static void CompleteNativeObjective(
            BootcampRuntimeTestHarness.Harness harness, Creature npc, uint objectiveId)
        {
            harness.MovePlayerTo(npc);
            CellManager.Instance.UpdateVisibility(harness.Client);
            new MapTriggerManager().TriggersProximityWorker(harness.BootcampMap);
            Assert.IsTrue(harness.Manager.OpenNpcConversation(harness.Client, npc.EntityId));
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteULong(npc.EntityId);
                writer.WriteUInt(1995);
                writer.WriteUInt(objectiveId);
                writer.WriteUInt(1);
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new CompleteNPCObjectivePacket();
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            new NpcManager(harness.Context, harness.Manager).CompleteNPCObjective(harness.Client, packet);
        }

        private static void AssertSupportedObjectives(BootcampRuntimeTestHarness.Harness harness)
        {
            var supported = new[] { 2U, 3U, 1U, 4U };
            CollectionAssert.AreEquivalent(supported, harness.Client.Player.Missions[1995].Objectives.Keys.ToArray());
            harness.Manager.PublishInitialState(harness.Client);
            var packets = harness.Drain();
            foreach (var packet in packets)
            {
                if (packet is MissionStatusInfoPacket status && status.MissionStatusDict.TryGetValue(1995, out var info))
                    AssertVisibleSupportedObjectives(info);
                if (packet is MissionGainedPacket gained && gained.MissionId == 1995)
                {
                    AssertVisibleSupportedObjectives(gained.MissionInfo);
                    CollectionAssert.AreEqual(new[] { 2U },
                        gained.MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
                }
                if (packet is ObjectiveRevealedPacket revealed && revealed.MissionId == 1995)
                    CollectionAssert.Contains(supported, revealed.ObjectiveId);
                if (packet is ObjectiveActivatedPacket activated && activated.MissionId == 1995)
                    CollectionAssert.Contains(supported, activated.ObjectiveId);
                if (packet is ObjectiveRevealedPacket or ObjectiveActivatedPacket or
                    ObjectiveCompletedPacket or ObjectiveFailedPacket)
                {
                    using var stream = new MemoryStream(MissionTestContext.Encode(packet));
                    using var reader = new PythonReader(new BinaryReader(stream));
                    Assert.AreEqual(packet is ObjectiveRevealedPacket ? 3 : 2, reader.ReadTuple());
                    var missionId = reader.ReadUInt();
                    var objectiveId = reader.ReadUInt();
                    if (missionId == 1995)
                        CollectionAssert.Contains(supported, objectiveId);
                }
            }
            var latest = packets.OfType<MissionStatusInfoPacket>().Last();
            if (latest.MissionStatusDict.TryGetValue(1995, out var current))
                CollectionAssert.AreEquivalent(
                    harness.Client.Player.Missions[1995].Objectives.Values
                        .Where(objective => objective.State != MissionObjectiveState.Inactive)
                        .Select(objective => objective.ObjectiveId).ToArray(),
                    current.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());

            void AssertVisibleSupportedObjectives(MissionInfo info)
            {
                var ids = info.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
                CollectionAssert.IsSubsetOf(ids, supported);
                Assert.AreEqual(ids.Length, ids.Distinct().Count());
                Assert.IsTrue(info.ObjectivesList.All(objective => objective.State != MissionObjectiveState.Inactive));
            }
        }

        [TestMethod]
        public void ConradInteractionStartsTheDeadlineAndDropshipUsesConfiguredWindup()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            StartCrashSiteScene(harness);

            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));

            var conrad = FindScenarioObject(harness, "bootcamp-conrad-corpse");
            harness.UseObjectAndRecover(conrad);

            using (var unit = harness.Context.CreateChar())
            {
                var deadline = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995);
                Assert.IsNotNull(deadline);
                Assert.AreEqual(CharacterMissionDeadlineState.Active, deadline.State);
                Assert.AreEqual(harness.UtcNow + BombDeadline, deadline.DueAtUtc);
            }

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            Assert.AreEqual(1400U, dropship.WindupTime);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[1].State);
        }

        [TestMethod]
        public void MissingScoutAreaCreatesTheCorpseAndReconnectPreservesDiscoveryWithoutASurvivor()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId,
                2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, completeable: true);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                1995));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2584));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(1995, MissingScoutAreaId)));

            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2584));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1995].Objectives[2].State);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1995].Objectives[3].State);

            var foreignMap = harness.Maps.GetOrCreatePrivateInstance(
                BootcampRuntimeTestHarness.BootcampMapContextId,
                999);
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(foreignMap, 2584));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(foreignMap, "bootcamp-conrad-corpse"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(foreignMap, "bootcamp-dropship-debris"));

            harness.ReconnectFresh();

            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.WoundedSurvivorPackageId));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1995].Objectives[2].State);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1995].Objectives[3].State);

            harness.ReconnectFresh();

            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2584));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
        }

        [TestMethod]
        public void DisconnectDuringIncompletePlantCancelsTheUseButPreservesTheExistingDeadline()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            using (var unit = harness.Context.CreateChar())
            {
                var before = unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995);
                Assert.IsNotNull(before);
                harness.BeginUseObject(dropship);
                harness.AdvanceRecovery(Math.Max(0L, dropship.WindupTime - 1L));

                Assert.AreEqual(1, harness.BootcampMap.PerformRecovery.Count);
                Assert.AreEqual(
                    MissionObjectiveState.Incomplete,
                    harness.Client.Player.Missions[1995].Objectives[1].State);

                harness.ReconnectFresh();

                using var reloaded = harness.Context.CreateChar();
                var after = reloaded.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995);
                Assert.IsNotNull(after);
                Assert.AreEqual(before.DueAtUtc, after.DueAtUtc);
                Assert.AreEqual(CharacterMissionDeadlineState.Active, after.State);
            }

            Assert.AreEqual(0, harness.BootcampMap.PerformRecovery.Count);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1995].Objectives[1].State);
            Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(FindScenarioObject(harness, "bootcamp-dropship-debris"));
        }

        [TestMethod]
        public void PlantCompletionWithFiveSecondsRemainingAllowsTheFuseToFinishAfterExpiry()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            var dueAt = ReadDeadline(harness, 1995).DueAtUtc;
            harness.UtcNow = dueAt - TimeSpan.FromSeconds(5);

            harness.UseObjectAndRecover(dropship);

            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1995].Objectives[1].State);
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, ReadDeadline(harness, 1995).State);

            harness.UtcNow = dueAt + TimeSpan.FromSeconds(1);

            Assert.IsFalse(harness.Manager.EvaluateDeadlines(harness.Client));
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1995].Objectives[1].State);
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Client.Player.Missions[1995].Objectives[4].State);
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2564));

            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Client.Player.Missions[1995].Objectives[4].State);
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                2564));
        }

        [TestMethod]
        public void AbandoningBeforeScoutAreaFailsTheMissionAndMakesRetryAvailableWithoutLeavingStaleActors()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId,
                2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, completeable: true);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                1995));

            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[1995].State);
            Assert.AreEqual(
                MissionObjectiveState.Failed,
                harness.Client.Player.Missions[1995].Objectives[1].State);
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.WoundedSurvivorPackageId));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 39));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 50));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId));
            AssertResetScenarioRecordedOnce(harness, 1995, 5, stepCount: 4);

            var classification = harness.Manager.ClassifyNpcConversation(harness.Client.Player, youngblood);
            Assert.IsTrue(classification.TryGetStatus(out _, out var missionIds));
            CollectionAssert.Contains(missionIds, 2005U);
        }

        [TestMethod]
        public void AbandoningAfterSurvivorConversationFailsTheMissionBeforeAnyDeadlineRowExists()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = StartCrashSiteScene(harness);
            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");

            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));

            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[1995].State);
            Assert.AreEqual(
                MissionObjectiveState.Failed,
                harness.Client.Player.Missions[1995].Objectives[1].State);
            using (var unit = harness.Context.CreateChar())
                Assert.IsNull(unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));
            Assert.IsTrue(dropship.IsEnabled);
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap,
                "bootcamp-conrad-corpse"));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap,
                "bootcamp-dropship-debris"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 39));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 50));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId));
            AssertResetScenarioRecordedOnce(harness, 1995, 5, stepCount: 4);

            var classification = harness.Manager.ClassifyNpcConversation(harness.Client.Player, youngblood);
            Assert.IsTrue(classification.TryGetStatus(out _, out var missionIds));
            CollectionAssert.Contains(missionIds, 2005U);
        }

        [TestMethod]
        public void AbandoningDuringActiveDeadlineFailsAndResetsMission1995OnceWhileUnlockingRetry()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));

            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            Assert.IsTrue(dropship.IsEnabled);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));

            AssertFailedMissionReset(
                harness,
                missionId: 1995,
                objectiveId: 1,
                resetScenarioId: 5,
                dropship,
                expectRetryAvailable: true,
                retryMissionId: 2005,
                retryNpc: youngblood);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));
            var retryDeadline = ReadDeadline(harness, 2005);

            Assert.IsFalse(harness.Manager.TryAbandon(harness.Client, 1995));
            AssertResetScenarioRecordedOnce(harness, 1995, 5, stepCount: 4);
            Assert.AreEqual(CharacterMissionDeadlineState.Active, ReadDeadline(harness, 2005).State);
            Assert.AreEqual(retryDeadline.DueAtUtc, ReadDeadline(harness, 2005).DueAtUtc);
        }

        [TestMethod]
        public void AbandoningDuringTheFuseFailsTheMissionAndMakesRetryAvailable()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = StartCrashSiteScene(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            var dropship = FindScenarioObject(harness, "bootcamp-dropship-debris");
            harness.UseObjectAndRecover(dropship);

            Assert.IsFalse(dropship.IsEnabled);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 1995));

            AssertFailedMissionReset(
                harness,
                missionId: 1995,
                objectiveId: 1,
                resetScenarioId: 5,
                dropship,
                expectRetryAvailable: true,
                retryMissionId: 2005,
                retryNpc: youngblood);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                2005));
            var retryDeadline = ReadDeadline(harness, 2005);

            Assert.IsFalse(harness.Manager.TryAbandon(harness.Client, 1995));
            AssertResetScenarioRecordedOnce(harness, 1995, 5, stepCount: 4);
            Assert.AreEqual(CharacterMissionDeadlineState.Active, ReadDeadline(harness, 2005).State);
            Assert.AreEqual(retryDeadline.DueAtUtc, ReadDeadline(harness, 2005).DueAtUtc);
        }

        internal static Creature StartCrashSiteScene(BootcampRuntimeTestHarness.Harness harness)
        {
            var youngblood = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId,
                2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, completeable: true);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                1995));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(1995, MissingScoutAreaId)));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[2].State);

            return youngblood;
        }

        private static DynamicObject FindScenarioObject(
            BootcampRuntimeTestHarness.Harness harness,
            string key) =>
            BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, key)
            ?? throw new AssertFailedException($"Missing scenario object {key}.");

        private static CharacterMissionDeadlineEntry ReadDeadline(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId)
        {
            using var unit = harness.Context.CreateChar();
            return unit.CharacterMissionDeadlines.Get(harness.Client.Player.Id, missionId)
                   ?? throw new AssertFailedException($"Missing mission deadline for {missionId}.");
        }

        private static void AssertFailedMissionReset(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId,
            uint objectiveId,
            uint resetScenarioId,
            DynamicObject dropship,
            bool expectRetryAvailable,
            uint retryMissionId,
            Creature retryNpc)
        {
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[missionId].State);
            Assert.AreEqual(
                MissionObjectiveState.Failed,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            Assert.AreEqual(CharacterMissionDeadlineState.Cancelled, ReadDeadline(harness, missionId).State);
            Assert.IsTrue(dropship.IsEnabled);
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap,
                "bootcamp-conrad-corpse"));
            Assert.IsNotNull(BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap,
                "bootcamp-dropship-debris"));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 39));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 50));
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId));
            AssertResetScenarioRecordedOnce(harness, missionId, resetScenarioId, stepCount: 4);

            var classification = harness.Manager.ClassifyNpcConversation(harness.Client.Player, retryNpc);
            if (!classification.TryGetStatus(out _, out var missionIds))
                missionIds = new System.Collections.Generic.List<uint>();

            if (expectRetryAvailable)
                CollectionAssert.Contains(missionIds, retryMissionId);
            else
                CollectionAssert.DoesNotContain(missionIds, retryMissionId);
        }

        private static void AssertResetScenarioRecordedOnce(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId,
            uint resetScenarioId,
            uint stepCount)
        {
            using var unit = harness.Context.CreateChar();
            var scene = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, missionId).Single();
            Assert.AreEqual("Ended", scene.Status);
            using var checkpoint = System.Text.Json.JsonDocument.Parse(scene.Checkpoint);
            Assert.AreEqual(resetScenarioId, checkpoint.RootElement.GetProperty("sequence").GetUInt32());
            Assert.IsFalse(unit.CharacterMissions.Runtime.Timers(scene.RunId).Any(timer => timer.Disposition == "Pending"));
            var effects = unit.CharacterMissions.Runtime.Effects(scene.RunId);
            Assert.AreEqual(effects.Count, effects.Select(effect => (effect.Generation, effect.OperationKey)).Distinct().Count());
        }

        private static string[] ReadScenarioKeys(
            BootcampRuntimeTestHarness.Harness harness,
            uint missionId)
        {
            using var unit = harness.Context.CreateChar();
            return unit.CharacterMissionScenario.Get(harness.Client.Player.Id, missionId)
                .Select(entry => entry.StepKey)
                .ToArray();
        }
    }
}
