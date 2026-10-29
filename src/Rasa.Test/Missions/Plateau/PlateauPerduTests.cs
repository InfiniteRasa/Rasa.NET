extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Plateau;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Special Agent Perdu's line, Plateau, played through as a player plays it: Wainwright's Clean Slate
    /// (1983) - the escort to Perdu and the berserk soldier -, Perdu's Mind Bender (905), Russ's Photo Ops
    /// (929) turned in to Clark, Clark's The Results Are In (936), Perdu's Mind Slayer (719).
    ///
    /// Why these tests: as PlateauCampResistanceScroungerTests. The new and moved NPCs stand where their sources put
    /// them, on the navmesh and off every hostile pool's ground; the escort walks its whole route on the
    /// Plateau's navmesh; drops are taken from the corpse as PlateauFortDefianceTests takes the Juggernaut's.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauPerduTests
    {
        private const uint CleanSlate = PlateauCleanSlateV1.MissionId;
        private const uint MindBender = PlateauMindBenderV1.MissionId;
        private const uint PhotoOps = PlateauPhotoOpsV1.MissionId;
        private const uint ResultsAreIn = PlateauResultsAreInV1.MissionId;
        private const uint MindSlayer = PlateauMindSlayerV1.MissionId;
        private const uint Perdu = PlateauPerduNpcsV1.Perdu;     // creature and pool
        private const uint Clark = PlateauPerduNpcsV1.Clark;

        [TestMethod]
        public void TheNewAndMovedNpcsStandWhereTheirSourcesPutThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, new[] { PlateauPerduNpcsV1.WainwrightPool, PlateauPerduNpcsV1.RussPool, Perdu, Clark }
                .Concat(PlateauPerduNpcsV1.Implanted.Select(soldier => soldier.Pool)).ToArray());
            AssertStands(plateau, Npc(plateau, PlateauPerduNpcsV1.WainwrightPool, PlateauPerduNpcsV1.Wainwright),
                PlateauPerduNpcsV1.WainwrightAt, "Wainwright in New Velon Village");
            var russ = Npc(plateau, PlateauPerduNpcsV1.RussPool, PlateauPerduNpcsV1.Russ);
            AssertStands(plateau, russ, PlateauPerduNpcsV1.RussAt, "Russ by the outpost's workstation");
            Assert.AreEqual(PlateauPerduNpcsV1.RussPackage, russ.Npc.NpcPackageId);
            foreach (var move in PlateauPerduNpcsV1.Moves)
                AssertStands(plateau, RequireNpc(plateau, move.Pool), move.At, $"{move.Pool} where the client's text puts it");
            foreach (var soldier in PlateauPerduNpcsV1.Implanted)
            {
                var npc = Npc(plateau, soldier.Pool, soldier.Id);
                AssertStands(plateau, npc, soldier.At, soldier.Comment);
                Assert.AreEqual(soldier.Package, npc.Npc.NpcPackageId, soldier.Comment);
            }

            // No hostile pool's creatures come within the server's safe clearance of the new or moved NPCs.
            var places = new[] { PlateauPerduNpcsV1.WainwrightAt, PlateauPerduNpcsV1.RussAt }
                .Concat(PlateauPerduNpcsV1.Moves.Select(move => move.At))
                .Concat(PlateauPerduNpcsV1.Implanted.Select(soldier => soldier.At)).ToArray();
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking()
                .Where(pool => pool.MapContextId == PlateauMissionDataV1.MapContextId).ToArray()
                .Where(pool => harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == pool.Creature1Id).Faction == 0)
                .ToArray();
            foreach (var at in places)
                foreach (var pool in hostile)
                    Assert.IsTrue(Vector2.Distance(new Vector2((float)at.X, (float)at.Z), new Vector2((float)pool.PosX, (float)pool.PosZ))
                        - (float)pool.Radius >= SpawnPoolManager.SafeClearance, $"hostile pool {pool.Id} reaches {at}");

            // The sentinels: one a pool, numbered down the quarry's spiral, on the navmesh.
            Spawn(plateau, PlateauSurveillanceSentinelsV1.Sentinels.Select(sentinel => sentinel.Pool).ToArray());
            var previous = float.MaxValue;
            foreach (var sentinel in PlateauSurveillanceSentinelsV1.Sentinels)
            {
                var creature = Living(plateau).Single(candidate => candidate.SpawnPool?.DbId == sentinel.Pool);
                Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory);
                Assert.IsTrue(sentinel.At.Y < previous, "down the spiral");
                previous = (float)sentinel.At.Y;
                var ground = plateau.NavMesh.GroundHeight(new Vector3((float)sentinel.At.X, (float)sentinel.At.Y, (float)sentinel.At.Z));
                Assert.IsNotNull(ground, $"sentinel {sentinel.Id} on the navmesh");
                Assert.IsTrue(System.Math.Abs(ground.Value - sentinel.At.Y) < 0.5f, $"sentinel {sentinel.Id} at its ground");
            }
        }

        [TestMethod]
        public void CleanSlateEscortsTheSoldierToPerduWhereHeGoesBerserk()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.WainwrightPool, Perdu);
            var wainwright = Npc(plateau, PlateauPerduNpcsV1.WainwrightPool, PlateauPerduNpcsV1.Wainwright);
            var perdu = RequireNpc(plateau, Perdu);

            Assert.IsFalse(Offers(harness, perdu, MindBender), "Mind Bender waits for Clean Slate");
            var offer = WildernessRanjaGorgeTests.Offer(harness, wainwright, CleanSlate);
            CollectionAssert.AreEquivalent(new[] { PlateauCleanSlateV1.BioHelmet, PlateauCleanSlateV1.GravitonHelmet,
                PlateauCleanSlateV1.MechHelmet, PlateauCleanSlateV1.StealthHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(), "the four helmets");
            Assert.AreEqual(PlateauCleanSlateV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(wainwright.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, wainwright.EntityId, CleanSlate));
            Assert.AreEqual(CleanSlate, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            Tick(harness, plateau, 250);

            var soldier = SceneCreature(plateau, "soldier");
            Assert.IsNotNull(soldier, "Wainwright's soldier is there");
            Assert.AreEqual(TargetCategory.Friendly, soldier.TargetCategory);
            Assert.AreEqual(PlateauPerduNpcsV1.Soldier, soldier.DbId);
            Assert.IsTrue(Vector3.Distance(wainwright.Position, soldier.Position) < 10, $"beside Wainwright: {soldier.Position}");

            Walk(harness, plateau, soldier);
            var mission = harness.Client.Player.Missions[CleanSlate];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauCleanSlateScene.Escort].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauCleanSlateScene.Kill].State);
            Assert.IsNull(SceneCreature(plateau, "soldier"), "the walker is gone");
            var berserk = SceneCreature(plateau, "berserk");
            Assert.IsNotNull(berserk, "his berserk copy stands at the gate");
            Assert.AreEqual(TargetCategory.Hostile, berserk.TargetCategory);
            var gate = PlateauCleanSlateV1.Route[^1];
            Assert.IsTrue(Vector3.Distance(new Vector3(gate.X, gate.Y, gate.Z), berserk.Position) < 1.5f, $"at the gate: {berserk.Position}");
            Assert.IsFalse(mission.Completeable);

            harness.MoveTo(berserk.Position);
            berserk.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, berserk, harness.Client.Player);
            Tick(harness, plateau, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauCleanSlateScene.Kill].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauCleanSlateScene.Report].State);
            Assert.IsFalse(mission.Completeable, "Perdu wants to talk first");

            Talk(harness, perdu, CleanSlate, PlateauCleanSlateScene.Report);
            Assert.IsTrue(harness.Client.Player.Missions[CleanSlate].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, perdu, CleanSlate, 2,
                PlateauCleanSlateV1.Experience, PlateauCleanSlateV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauCleanSlateV1.MechHelmet), "the third choice");
            Assert.IsTrue(Offers(harness, perdu, MindBender), "Perdu's line opens");
        }

        [TestMethod]
        public void CleanSlateFailsWhenTheSoldierDiesOnTheWay()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, PlateauPerduNpcsV1.WainwrightPool);
            var wainwright = Npc(plateau, PlateauPerduNpcsV1.WainwrightPool, PlateauPerduNpcsV1.Wainwright);
            harness.MoveTo(wainwright.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, wainwright.EntityId, CleanSlate));
            harness.Drain();
            Tick(harness, plateau, 250);
            var soldier = SceneCreature(plateau, "soldier");
            for (var tick = 0; tick < 10; tick++)
                Tick(harness, plateau, 1000);
            Assert.IsTrue(Vector3.Distance(PlateauCleanSlateV1.Route[0].ToVector(), soldier.Position) > 5, "he is on his way");

            soldier.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, soldier, null);
            Tick(harness, plateau, 250);
            var mission = harness.Client.Player.Missions[CleanSlate];
            Assert.AreEqual(MissionObjectiveState.Failed, mission.Objectives[PlateauCleanSlateScene.Escort].State);
            Assert.AreEqual(MissionState.Failed, mission.State);
            Assert.IsNull(SceneCreature(plateau, "berserk"), "no berserk copy without the escort");
        }

        [TestMethod]
        public void MindBenderTakesFourImplantsWithTheExtractorPerduGivesAndTakesBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, new[] { Perdu }.Concat(PlateauPerduNpcsV1.Implanted.Select(soldier => soldier.Pool)).ToArray());
            var perdu = RequireNpc(plateau, Perdu);
            Assert.AreEqual((int)PlateauPerduNpcsV1.PerduHealth, perdu.Attributes[Attributes.Health].NormalMax,
                "he outlasts the berserk soldier at his gate (to-fix #39)");
            Assert.IsFalse(Offers(harness, perdu, MindBender), "Mind Bender waits for Clean Slate");
            SeedCompleted(harness, CleanSlate);

            var offer = WildernessRanjaGorgeTests.Offer(harness, perdu, MindBender);
            Assert.AreEqual(4, offer.SelectableReward.Count, "the four gloves");
            harness.MoveTo(perdu.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, perdu.EntityId, MindBender));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMindBenderV1.ExtractorTemplate), "the extractor");

            for (var index = 0; index < PlateauMindBenderV1.Soldiers.Length; index++)
            {
                var data = PlateauMindBenderV1.Soldiers[index];
                var soldier = Npc(plateau, PlateauPerduNpcsV1.Implanted[index].Pool, PlateauPerduNpcsV1.Implanted[index].Id);
                Assert.IsFalse(HandsIn(harness, soldier, MindBender, data.Extract), "no extraction before he is found");
                Talk(harness, soldier, MindBender, data.Find);
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, data.Implant), "finding him is not the implant");
                Talk(harness, soldier, MindBender, data.Extract);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, data.Implant), $"{data.Key}");
                Assert.AreEqual(index == PlateauMindBenderV1.Soldiers.Length - 1,
                    harness.Client.Player.Missions[MindBender].Completeable, $"after {data.Key}");
            }

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, perdu, MindBender, 2,
                PlateauMindBenderV1.Experience, PlateauMindBenderV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMindBenderV1.MechGloves), "the third choice");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauMindBenderV1.ExtractorTemplate), "Perdu takes the extractor back");
            foreach (var data in PlateauMindBenderV1.Soldiers)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, data.Implant), "and the implants");
        }

        [TestMethod]
        public void PhotoOpsTakesEachSentinelsDiskToClark()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, new[] { PlateauPerduNpcsV1.RussPool, Clark }
                .Concat(PlateauSurveillanceSentinelsV1.Sentinels.Select(sentinel => sentinel.Pool)).ToArray());
            var russ = Npc(plateau, PlateauPerduNpcsV1.RussPool, PlateauPerduNpcsV1.Russ);
            var clark = RequireNpc(plateau, Clark);
            Assert.IsFalse(Offers(harness, russ, PhotoOps), "Photo Ops waits for Mind Bender");
            SeedCompleted(harness, CleanSlate, MindBender);

            // A kill before the mission drops nothing.
            Assert.IsFalse(Holds(Kill(harness, plateau, PlateauSurveillanceSentinelsV1.Sentinels[0].Id),
                PlateauPhotoOpsV1.Disks[0].Disk), "no disk without the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, russ, PhotoOps);
            Assert.AreEqual(4, offer.SelectableReward.Count, "the four boots");
            harness.MoveTo(russ.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, russ.EntityId, PhotoOps));
            harness.Drain();

            for (var index = 0; index < PlateauPhotoOpsV1.Disks.Length; index++)
            {
                var disk = PlateauPhotoOpsV1.Disks[index];
                var sentinel = PlateauSurveillanceSentinelsV1.Sentinels[index];
                if (!Living(plateau).Any(creature => creature.DbId == sentinel.Id))
                    Respawn(plateau, sentinel.Pool);
                var corpse = Kill(harness, plateau, sentinel.Id);
                Assert.IsTrue(Holds(corpse, disk.Disk), $"sentinel {index + 1} drops disk {index + 1}");
                Assert.IsFalse(PlateauPhotoOpsV1.Disks.Where((_, other) => other != index).Any(other => Holds(corpse, other.Disk)),
                    "and no other disk");
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    harness.Client.Player.Missions[PhotoOps].Objectives[disk.Objective].State, "a kill is not a pickup");
                PickUp(harness, corpse);
                Assert.AreEqual(MissionObjectiveState.Completed,
                    harness.Client.Player.Missions[PhotoOps].Objectives[disk.Objective].State, $"disk {index + 1}");
            }
            Assert.IsTrue(harness.Client.Player.Missions[PhotoOps].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, clark, PhotoOps, 2,
                PlateauPhotoOpsV1.Experience, PlateauPhotoOpsV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauPhotoOpsV1.MechBoots), "the third choice");
            foreach (var disk in PlateauPhotoOpsV1.Disks)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, disk.Disk), "Clark keeps the disks");
            Assert.IsTrue(Offers(harness, clark, ResultsAreIn), "Clark's The Results Are In opens");
        }

        [TestMethod]
        public void TheResultsAreInSendsClarksAgentToPerduWhoOpensMindSlayer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Perdu, Clark);
            var perdu = RequireNpc(plateau, Perdu);
            var clark = RequireNpc(plateau, Clark);
            SeedCompleted(harness, CleanSlate, MindBender, PhotoOps);
            Assert.IsFalse(Offers(harness, perdu, MindSlayer), "Mind Slayer waits for The Results Are In");

            var offer = WildernessRanjaGorgeTests.Offer(harness, clark, ResultsAreIn);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice: the consumables");
            harness.MoveTo(clark.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, clark.EntityId, ResultsAreIn));
            harness.Drain();
            Talk(harness, perdu, ResultsAreIn, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, perdu, ResultsAreIn, null,
                PlateauResultsAreInV1.Experience, PlateauResultsAreInV1.Credits);
            Assert.AreEqual(PlateauResultsAreInV1.MedPacks, WildernessRanjaGorgeTests.Held(harness, PlateauResultsAreInV1.MedPack));
            Assert.IsTrue(Offers(harness, perdu, MindSlayer), "Perdu's Mind Slayer opens");
        }

        [TestMethod]
        public void MindSlayerTakesKrakatusDatapadToPerdu()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Perdu, PlateauMindSlayerV1.Krakatus);
            var perdu = RequireNpc(plateau, Perdu);
            SeedCompleted(harness, CleanSlate, MindBender, PhotoOps, ResultsAreIn);

            var offer = WildernessRanjaGorgeTests.Offer(harness, perdu, MindSlayer);
            Assert.AreEqual(4, offer.SelectableReward.Count, "the four weapons");
            harness.MoveTo(perdu.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, perdu.EntityId, MindSlayer));
            harness.Drain();

            var corpse = Kill(harness, plateau, PlateauMindSlayerV1.Krakatus);
            Assert.IsTrue(Holds(corpse, PlateauMindSlayerV1.DatapadTemplate), "Krakatus' datapad");
            Assert.IsFalse(harness.Client.Player.Missions[MindSlayer].Completeable, "a kill is not a pickup");
            PickUp(harness, corpse);
            Assert.IsTrue(harness.Client.Player.Missions[MindSlayer].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, perdu, MindSlayer, 2,
                PlateauMindSlayerV1.Experience, PlateauMindSlayerV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauMindSlayerV1.WeaponC), "the third choice");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauMindSlayerV1.DatapadTemplate), "Perdu keeps it");
        }

        /// <summary>The soldier walks his route, the player beside him, until the escort objective is done.</summary>
        private static void Walk(WildernessRuntimeTestHarness harness, MapChannel plateau, Creature soldier)
        {
            var start = soldier.Position;
            var mission = harness.Client.Player.Missions[CleanSlate];
            for (var tick = 0; tick < 1200 &&
                 mission.Objectives[PlateauCleanSlateScene.Escort].State != MissionObjectiveState.Completed; tick++)
            {
                if (soldier.State != CharacterState.Dead && SceneCreature(plateau, "soldier") != null)
                    harness.MoveTo(soldier.Position + Vector3.UnitZ);
                Tick(harness, plateau, 1000);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauCleanSlateScene.Escort].State,
                $"the soldier stalled at {soldier.Position}");
            Assert.IsTrue(Vector3.Distance(start, soldier.Position) > 300, "he walked the whole way");
        }

        /// <summary>As WildernessRuntimeTestHarness.Tick, on the Plateau.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel plateau, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(plateau, milliseconds);
            harness.Manager.Scenes.Tick(plateau);
            harness.Drain();
        }

        private static Creature SceneCreature(MapChannel plateau, string role) => Living(plateau)
            .SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role);

        /// <summary>A public NPC of its own pool (the pool's id is not the creature's).</summary>
        private static Creature Npc(MapChannel plateau, uint pool, uint creature)
        {
            var npc = Living(plateau).Single(candidate => candidate.SpawnPool?.DbId == pool && candidate.Npc != null);
            Assert.AreEqual(creature, npc.DbId);
            return npc;
        }

        private static void AssertStands(MapChannel plateau, Creature npc, (double X, double Y, double Z) at, string what)
        {
            Assert.IsTrue(Vector2.Distance(new Vector2(npc.Position.X, npc.Position.Z), new Vector2((float)at.X, (float)at.Z)) < 0.5f,
                $"{what}: {npc.Position}");
            var ground = plateau.NavMesh.GroundHeight(new Vector3((float)at.X, (float)at.Y, (float)at.Z));
            Assert.IsNotNull(ground, $"{what} has navmesh under it");
            Assert.IsTrue(System.Math.Abs(ground.Value - at.Y) < 0.5f, $"{what} at {at.Y}, ground {ground}");
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static bool HandsIn(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            return harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1);
        }

        /// <summary>The NPC's hand-in topic of this objective, and the objective completes there.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1),
                $"{mission}.{objective}");
            harness.Drain();
        }

        /// <summary>Kills a living creature of this kind as the player; returns its corpse.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, uint kind)
        {
            var creature = Living(plateau).FirstOrDefault(candidate => candidate.DbId == kind && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {kind} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
            Assert.IsTrue(plateau.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);

        /// <summary>These pools' dead come back: their respawn time and more has passed.</summary>
        private static void Respawn(MapChannel plateau, params uint[] pools)
        {
            var all = plateau.SpawnPools.ToArray();
            try
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
            }
            finally
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all);
            }
        }
    }

    internal static class ScenePositionVectors
    {
        internal static Vector3 ToVector(this Rasa.Missions.Scenes.ScenePosition position) =>
            new(position.X, position.Y, position.Z);
    }
}
