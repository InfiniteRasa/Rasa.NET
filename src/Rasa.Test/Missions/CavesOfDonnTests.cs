using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using D = CavesOfDonnContent;

    /// <summary>
    /// The Caves of Donn as the client has them after the rework, with the walkthrough's fights
    /// (CavesOfDonnContent, CavesOfDonn): their rows, the six missions, the ambushes, the bombs
    /// and cave-ins, the Lost Foreans' walk out, the three tests, the gate and the chamber, and
    /// the egg clusters.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CavesOfDonnTests
    {
        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            CavesOfDonn.Reset();
            CavesOfDonn.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            CavesOfDonn.Reset();
            CavesOfDonn.Now = () => Environment.TickCount64;
        }

        [TestMethod]
        public void TheCavesHaveTheirPeopleTheirBaneAndSixMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in D.Missions)
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out _), $"mission {missionId}");

            Assert.IsTrue(harness.Manager.TryGetOperationalMission(D.TheReasonWhy, out var reason));
            CollectionAssert.AreEquivalent(new uint[] { 1, 2, 3, 7, 10, 11, 12, 13, 14, 16, 17, 18, 19 }, reason.Objectives.Keys.ToArray());
            Assert.AreEqual(D.Category, reason.CategoryId);
            Assert.IsTrue(harness.Manager.TryGetOperationalMission(D.ToTheCaves, out var toTheCaves));
            Assert.AreEqual(D.WildernessCategory, toTheCaves.CategoryId);

            // Too Quiet, What Evil Lurks and To the Caves are radioed in.
            foreach (var missionId in new[] { D.ToTheCaves, D.TooQuiet, D.WhatEvilLurks })
            {
                Assert.IsTrue(world.MissionContentDefinitionEntries.Single(row => row.MissionId == missionId && row.ContentRevision == D.Revision).RadioCompleteable);
                Assert.IsTrue(world.Set<MissionChannelPolicyEntry>().Any(row => row.MissionId == missionId && row.ContentRevision == D.Revision));
            }

            var levels = world.Set<CreatureEntry>().Where(c => c.Id >= 566010 && c.Id <= 566018).ToDictionary(c => c.Id, c => c.Level);
            Assert.AreEqual(9, levels.Count);
            Assert.IsTrue(levels.Values.All(level => level is >= 9 and <= 11));
            Assert.AreEqual(9186U, world.Set<CreatureEntry>().Single(c => c.Id == D.Nekala).NameId);
            Assert.AreEqual(10159U, world.Set<CreatureEntry>().Single(c => c.Id == D.Drak).NameId);
            Assert.AreEqual(D.NekalaPackage, world.Set<NpcPackageEntry>().Single(p => p.Id == D.Nekala).PackageId);
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == D.Stone));

            var pools = world.Set<SpawnPoolEntry>().Where(pool => pool.MapContextId == D.Donn && pool.Id >= 566200 && pool.Id < 566300).ToList();
            Assert.AreEqual(D.Pools.Length, pools.Count);

            var eggs = world.Set<WorldDestructibleEntry>().Where(row => row.MapContextId == D.Donn).ToList();
            Assert.AreEqual(6, eggs.Count);
            Assert.IsTrue(eggs.All(row => row.ClassId == D.EggClusterClass && row.CreatureId == D.Hatchling));

            // Body Count is gone from Targets of Opportunity's story missions, and nothing else.
            var binding = world.Set<MissionSceneBindingEntry>().Single(row => row.MissionId == 1449 && row.ContentRevision == WildernessTargetsKillRules.Revision);
            var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(binding.Bindings, MissionContentCodec.Options);
            var groups = scene.ObjectiveHistoryAggregations[55].Groups;
            Assert.IsFalse(groups.Any(group => group.Contains(D.BodyCount)));
            Assert.AreEqual(TargetsOfOpportunitySeed.OneOffs.WildernessScene().ObjectiveHistoryAggregations[55].Groups.Count - 1, groups.Count);
            Assert.IsTrue(groups.Any(group => group.Contains(D.WhatEvilLurks)));
        }

        [TestMethod]
        public void ToTheCavesIsBeingInTheCaves()
        {
            using var harness = Create();
            var nula = Put(harness, D.Nula, D.Entrance + new Vector3(3, 0, 0));
            WildernessTwinPillarsTests.Accept(harness, nula, D.ToTheCaves);

            Work(harness);

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.ToTheCaves, 1));
            Assert.IsTrue(harness.Client.Player.Missions[D.ToTheCaves].Completeable);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, D.ToTheCaves, 0, null), "radioed in");
        }

        [TestMethod]
        public void TooQuietRunsFromTheHillToThePyramid()
        {
            using var harness = Create();
            harness.SpawnWorld(D.StonePool, D.NekalaPool, D.DyingForeanPool);
            var stone = WildernessTwinPillarsTests.RequireNpc(harness, D.StonePool, D.Stone);
            WildernessTwinPillarsTests.Accept(harness, stone, D.TooQuiet);

            // The drop comes down on Mar Fend as soon as someone on the objective is near: from the
            // bottom of the hill, once it has been scouted.
            harness.MoveTo(D.Entrance);
            Work(harness);
            Assert.AreEqual(0, Bane(harness).Count);
            harness.MoveTo(D.HillBottom);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TooQuiet, 3));
            var ambush = Bane(harness);
            Assert.AreEqual(6, ambush.Count);
            Assert.IsTrue(ambush.Any(c => c.DbId == D.MachinaChampion));
            Assert.IsTrue(ambush.All(c => Vector3.Distance(c.Position, D.MarFend) < 20));

            foreach (var bane in ambush)
                Kill(harness, bane);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TooQuiet, 4));
            Assert.AreEqual(CavesOfDonn.StoneStage.MarFend, CavesOfDonn.StoneStageOf(harness.Map));

            var nekala = WildernessTwinPillarsTests.RequireNpc(harness, D.NekalaPool, D.Nekala);
            WildernessTwinPillarsTests.Talk(harness, nekala, D.TooQuiet, 5);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, D.TooQuiet, 8));

            harness.MoveTo(D.Village + new Vector3(10, 0, 10));
            Work(harness);
            var second = Bane(harness);
            Assert.AreEqual(5, second.Count);
            foreach (var bane in second)
                Kill(harness, bane);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TooQuiet, 8));
            Assert.AreEqual(CavesOfDonn.StoneStage.Village, CavesOfDonn.StoneStageOf(harness.Map));

            Use(harness, Object(harness, D.SignalClass));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TooQuiet, 9));

            var dying = WildernessTwinPillarsTests.RequireNpc(harness, D.DyingForeanPool, D.DyingForean);
            WildernessTwinPillarsTests.Talk(harness, dying, D.TooQuiet, 10);
            Work(harness);
            Assert.AreEqual(CavesOfDonn.StoneStage.Pyramid, CavesOfDonn.StoneStageOf(harness.Map));

            harness.MoveTo(D.Pyramid);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TooQuiet, 11));
            Assert.IsTrue(harness.Client.Player.Missions[D.TooQuiet].Completeable);
        }

        [TestMethod]
        public void TheFallenSoldiersBombBlowsTheCaveInAndTheLostForeansWalkOut()
        {
            using var harness = Create();
            var dying = Put(harness, D.DyingForean, D.DyingForeanSpot);
            WildernessTwinPillarsTests.Accept(harness, dying, D.WhatEvilLurks);
            Work(harness);

            // No bomb: the cave-in is refused.
            var caveIn = CaveIn(harness, 0);
            Use(harness, caveIn);
            Assert.IsFalse(CavesOfDonn.IsBlown(harness.Map, 0));

            Use(harness, Object(harness, D.FallenSoldierClass));
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, D.BombTemplate));
            Use(harness, Object(harness, D.FallenSoldierClass));
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, D.BombTemplate), "one at a time");

            Use(harness, caveIn);
            Assert.IsTrue(CavesOfDonn.IsBlown(harness.Map, 0));
            Assert.AreEqual(0, WildernessTwinPillarsTests.Held(harness, D.BombTemplate));
            Assert.AreEqual(UseObjectState.DoorStateOpen, caveIn.StateId);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.WhatEvilLurks, 4));

            _now += CavesOfDonn.CaveInClearMs;
            Work(harness);
            Assert.IsFalse(harness.Map.DynamicObjects.Contains(caveIn), "gone once the dust settles");

            harness.MoveTo(D.SpiritCave + new Vector3(4, 0, 4));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.WhatEvilLurks, 3));

            var survivors = CavesOfDonn.Survivors(harness.Map);
            Assert.AreEqual(3, survivors.Count);
            WildernessTwinPillarsTests.Talk(harness, survivors[0], D.WhatEvilLurks, 5);
            harness.MoveTo(D.SpiritCave + new Vector3(4, 0, 4));
            Work(harness);
            Assert.IsTrue(survivors.All(s => s.Controller.ActionFollow.FollowTargetId == harness.Client.Player.EntityId), "they follow");

            // At the extraction point, all three.
            harness.MoveTo(D.Extraction);
            foreach (var survivor in survivors)
                BehaviorManager.Instance.RestoreScriptedPose(harness.Map, survivor, D.Extraction + new Vector3(1, 0, 1), 0);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.WhatEvilLurks, 2));

            // Xang is not in the harness: the extraction point is secure.
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.WhatEvilLurks, 1));
            Assert.IsTrue(harness.Client.Player.Missions[D.WhatEvilLurks].Completeable);
        }

        [TestMethod]
        public void ALostForeanDyingOnTheWayOutFailsTheEscort()
        {
            using var harness = Create();
            var dying = Put(harness, D.DyingForean, D.DyingForeanSpot);
            WildernessTwinPillarsTests.Accept(harness, dying, D.WhatEvilLurks);
            Work(harness);
            GiveBomb(harness);
            Use(harness, CaveIn(harness, 0));
            harness.MoveTo(D.SpiritCave + new Vector3(4, 0, 4));
            Work(harness);
            var survivors = CavesOfDonn.Survivors(harness.Map).ToList();
            WildernessTwinPillarsTests.Talk(harness, survivors[0], D.WhatEvilLurks, 5);
            harness.MoveTo(D.SpiritCave + new Vector3(4, 0, 4));
            Work(harness);

            Kill(harness, survivors[2]);
            Work(harness);

            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, D.WhatEvilLurks, 2));
            Assert.AreNotEqual(MissionState.Active, harness.Client.Player.Missions.TryGetValue(D.WhatEvilLurks, out var mission) ? mission.State : MissionState.Failed);
            Assert.AreEqual(0, CavesOfDonn.Survivors(harness.Map).Count, "the rest go");

            _now += CavesOfDonn.SurvivorsBackMs;
            Work(harness);
            Assert.AreEqual(3, CavesOfDonn.Survivors(harness.Map).Count, "and are back in the cave");
        }

        [TestMethod]
        public void TheBraziersLitInTurnWakeTheirLogos()
        {
            using var harness = Create();
            var shrines = Shrines(harness);
            WildernessTwinPillarsTests.Accept(harness, Put(harness, D.Nekala, D.NekalaAtMarFend), D.TheReasonWhy);
            Assert.AreEqual(CavesOfDonn.NekalaBombs, (uint)WildernessTwinPillarsTests.Held(harness, D.BombTemplate), "Nekala's two bombs");
            Work(harness);

            Assert.IsTrue(shrines.All(shrine => !shrine.IsEnabled), "asleep");

            harness.MoveTo(D.Tests[0].Site);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 1));

            var braziers = Braziers(harness, 0);
            Use(harness, braziers[0]);
            Use(harness, braziers[1]);
            Assert.AreEqual(UseObjectState.TsState1, braziers[1].StateId);
            Use(harness, braziers[3]);
            Assert.IsTrue(braziers.All(b => b.StateId == UseObjectState.TsState0), "out of turn: all out");

            foreach (var brazier in braziers)
                Use(harness, brazier);
            Assert.IsTrue(braziers.All(b => b.StateId == UseObjectState.TsState1));
            Assert.IsTrue(braziers.All(b => !b.IsEnabled), "done with");
            Assert.IsTrue(shrines.Single(s => s.Id == D.TrapLogos).IsEnabled, "Trap wakes");
            Assert.IsFalse(shrines.Single(s => s.Id == D.FeelingLogos).IsEnabled);

            Learn(harness, D.TrapLogos);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 11));
        }

        [TestMethod]
        public void TheGateOpensForTheThreeLogosAndTheChamberIsBehindIt()
        {
            using var harness = Create();
            var nekala = Put(harness, D.Nekala, D.NekalaAtMarFend);
            WildernessTwinPillarsTests.Accept(harness, nekala, D.TheReasonWhy);
            Work(harness);

            var gate = Object(harness, D.GateClass);
            CollectionAssert.AreEquivalent(new[] { D.TrapLogos, D.FeelingLogos, D.HealLogos }, gate.Lock.LogosIds.ToArray());
            Use(harness, gate);
            Assert.IsFalse(CavesOfDonn.IsGateOpen(harness.Map), "not without the three");

            foreach (var logos in new[] { D.TrapLogos, D.FeelingLogos, D.HealLogos })
                Learn(harness, logos);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 13));

            Use(harness, gate);
            Assert.IsTrue(CavesOfDonn.IsGateOpen(harness.Map));
            Assert.AreEqual(UseObjectState.DoorStateOpen, gate.StateId);
            Assert.AreEqual(UseObjectState.DoorStateOpen, harness.Map.DynamicObjects.Single(o => (uint)o.EntityClassId == D.ChamberDoorClass).StateId);
            Assert.AreEqual(1u, harness.Client.Player.CameraScriptId, "the pass over the shrine");
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 10));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, D.TheReasonWhy, 17));

            var drak = CavesOfDonn.Find(harness.Map, D.Drak);
            Assert.IsNotNull(drak, "D'rak comes out of the dig");
            Assert.IsTrue(drak.Hate.Ranked().Any(entry => entry.Key == harness.Client.Player.EntityId));
            Kill(harness, drak);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 17));
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, D.OrdersTemplate), "his excavation orders");

            harness.MoveTo(D.Chamber + new Vector3(0, 0, 8));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 16));

            var statues = harness.Map.DynamicObjects.Where(o => (uint)o.EntityClassId == D.ShamanCorpseClass).ToList();
            Assert.AreEqual(3, statues.Count);
            foreach (var statue in statues)
                Use(harness, statue);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 19));
            foreach (var template in D.StatueTemplates)
                Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, template));

            Use(harness, Object(harness, D.ReliquaryClass));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 14));

            var projector = Object(harness, D.ProjectorClass);
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(projector.Position + new Vector3(2, 0, 0));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = projector.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = projector.EntityId, MissionId = D.TheReasonWhy, ObjectiveId = 7, PlayerFlagId = 1
            });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, D.TheReasonWhy, 7));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, D.TheReasonWhy, 18));

            // The hologram platform takes the user back to the corridor by the entrance.
            Use(harness, Object(harness, D.TeleporterClass));
            Assert.IsNotNull(harness.Client.PendingTransfer, "on the way");

            Work(harness);
            Assert.IsTrue(Vector3.Distance(nekala.Position, D.NekalaAtEntrance) < 1, $"Nekala waits by the entrance, at {nekala.Position}");
        }

        [TestMethod]
        public void EachEggClusterCountsOnceForBuggedToDeath()
        {
            using var harness = Create();
            WildernessTwinPillarsTests.Accept(harness, Put(harness, D.Napoli, D.NapoliSpot), D.BuggedToDeath);

            foreach (var egg in D.EggClusters.Take(5))
                CavesOfDonn.Destroyed(harness.Map, egg.Id);
            CavesOfDonn.Destroyed(harness.Map, D.EggClusters[0].Id);
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions[D.BuggedToDeath].Completeable);

            CavesOfDonn.Destroyed(harness.Map, D.EggClusters[5].Id);
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[D.BuggedToDeath].Completeable);
        }

        [TestMethod]
        public void EverythingStandsOnTheNavmesh()
        {
            using var harness = Create();
            var spots = new[]
                {
                    D.StoneAtEntrance, D.StoneAtMarFend, D.StoneAtVillage, D.StoneAtPyramid, D.NekalaAtMarFend, D.NekalaAtEntrance,
                    D.HillBottom, D.MarFend, D.Village, D.DistressSignal, D.DyingForeanSpot, D.Pyramid, D.FallenSoldier, D.Extraction,
                    D.NapoliSpot, D.Chamber, D.Teleporter, D.Corridor, D.DrakSpot
                }
                .Concat(D.FirstAmbushSpots).Concat(D.SecondAmbushSpots).Concat(D.LostForeanSpots).Concat(D.StatueSpots)
                .Concat(D.DrakGuardSpots).Concat(D.CaveIns.Select(c => c.Position)).Concat(D.Tests.SelectMany(t => t.Braziers))
                .Concat(D.EggClusters.Select(e => new Vector3((float)e.X, (float)e.Y, (float)e.Z)))
                .Concat(D.Pools.Select(p => new Vector3((float)p.X, (float)p.Y, (float)p.Z)));

            foreach (var spot in spots)
            {
                var ground = harness.Map.NavMesh.GroundHeight(spot);
                Assert.IsNotNull(ground, $"{spot}");
                Assert.IsTrue(Math.Abs(ground.Value - spot.Y) < 1.5f, $"{spot}: ground {ground}");
            }
        }

        [TestMethod]
        public void DownGivesBodyCountBackAndUpAgainChangesNothing()
        {
            const string migration = "20270217000000_Caves_of_donn", previous = "20270216000000_Ojasa_hive";
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(previous);
            var binding = Binding(database);
            Assert.IsTrue(binding.Contains("[691]"));

            migrator.Migrate(migration);
            Assert.AreEqual(6, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{D.Revision}' AND enabled = 1"));
            Assert.IsFalse(Binding(database).Contains("[691]"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 566000 AND 566999");

            foreach (var sql in D.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 566000 AND 566999"), "guarded");

            migrator.Migrate(previous);
            Assert.AreEqual(binding, Binding(database));
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package", "world_destructible" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 566000 AND 566999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{D.Revision}'"));

            migrator.Migrate(migration);
            Assert.AreEqual(6, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{D.Revision}' AND enabled = 1"));
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static string Binding(Rasa.Test.Database.OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1449 AND content_revision = '{WildernessTargetsKillRules.Revision}'").ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: D.Donn);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, D.Revision,
                        CharacterStartingExperienceState.Skipped)));
                harness.Client.Player.StartingExperienceCompleted = true;
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, D.Donn);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private void Work(WildernessRuntimeTestHarness harness)
        {
            _now += 100;
            CavesOfDonn.Worker(harness.Map);
            harness.Drain();
        }

        /// <summary>The living Bane of the copy (the content's rows), not its people.</summary>
        private static List<Creature> Bane(WildernessRuntimeTestHarness harness) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(c => c.DbId >= D.Soldier && c.DbId <= D.MachinaChampion && c.State != CharacterState.Dead &&
                    EntityManager.Instance.Creatures.ContainsKey(c.EntityId)).ToList();

        private static DynamicObject Object(WildernessRuntimeTestHarness harness, uint classId) =>
            harness.Map.DynamicObjects.Single(obj => (uint)obj.EntityClassId == classId);

        private static DynamicObject CaveIn(WildernessRuntimeTestHarness harness, int index) =>
            harness.Map.DynamicObjects.Single(obj => obj.ScenarioKey == D.CaveIns[index].Key);

        private static List<DynamicObject> Braziers(WildernessRuntimeTestHarness harness, int site) =>
            Enumerable.Range(0, D.Tests[site].Braziers.Length)
                .Select(i => harness.Map.DynamicObjects.Single(obj => obj.ScenarioKey == $"donn:brazier-{site}-{i}")).ToList();

        /// <summary>The three tests' Logos shrines, as the map's logos rows put them down.</summary>
        private static List<Logos> Shrines(WildernessRuntimeTestHarness harness)
        {
            var shrines = harness.World.Set<LogosEntry>().AsNoTracking().Where(entry => entry.MapContextId == D.Donn).ToList()
                .Select(entry => new Logos(entry) { MapContextId = D.Donn, RuntimeMapChannel = harness.Map }).ToList();

            foreach (var shrine in shrines)
            {
                harness.Map.DynamicObjects.Add(shrine);
                CellManager.Instance.AddToWorld(harness.Map, shrine);
            }

            Assert.AreEqual(3, shrines.Count);
            return shrines;
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions[missionId].Objectives[objectiveId].State;

        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            var lethal = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(harness.Map, creature, lethal, harness.Client.Player);
            harness.Drain();
        }

        /// <summary>A Logos learned, as a shrine teaches it.</summary>
        private static void Learn(WildernessRuntimeTestHarness harness, uint logos)
        {
            Assert.IsTrue(new CharacterManager(harness, harness.Manager).TryAddLogos(harness.Client, logos));
            harness.Drain();
        }

        private static void GiveBomb(WildernessRuntimeTestHarness harness)
        {
            Use(harness, Object(harness, D.FallenSoldierClass));
            Assert.AreEqual(1, WildernessTwinPillarsTests.Held(harness, D.BombTemplate));
        }

        private static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(1, 0, 0));
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId,
                ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.FootlockerUseArgId
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            harness.Drain();
        }

        #endregion
    }
}
