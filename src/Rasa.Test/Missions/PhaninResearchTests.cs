using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using K = Rasa.Services.Preloader.KardashAttaColonyContent;
    using P = Rasa.Services.Preloader.PhaninResearchContent;
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// The Phanin Research Facility as the wiki and walkthrough tell it (PhaninResearchContent,
    /// PhaninResearch): its rows and seven missions, the keycard at the way in, the mainframes and
    /// the doors they open, the research consoles, the medical crates, the caged Atta, Treshk's
    /// proof and the three heads of the facility.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PhaninResearchTests
    {
        private const string Migration = "20271016000000_Phanin_research";
        private const string Previous = "20271015000000_Kardash_atta_colony";

        [TestInitialize]
        public void Clean()
        {
            PhaninResearch.Reset();
            KardashAttaColony.Reset();
        }

        [TestCleanup]
        public void Restore() => PhaninResearch.Reset();

        [TestMethod]
        public void TheFacilityHasItsBrannItsBossesAndItsSevenMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in P.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(P.Category, mission.CategoryId);
            }

            var packages = world.Set<NpcPackageEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual(P.PhenixPackage, packages[P.Phenix]);
            Assert.AreEqual(P.WhitakerPackage, packages[P.Whitaker], "Whitaker's own, not the console's");

            var creatures = world.Set<CreatureEntry>().AsNoTracking().ToDictionary(c => c.Id);
            Assert.AreEqual(K.BrannSwapsetMale, creatures[P.Greloran].ClassId);
            foreach (var c in P.Creatures.Where(c => c.Faction == P.TargetCategoryRow.Hostile))
                Assert.IsTrue(creatures[c.Id].Level is >= 22 and <= 25, $"{c.Comment} level {creatures[c.Id].Level}");

            var pools = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => pool.MapContextId == P.Facility).ToList();
            foreach (var boss in new[] { P.Mernash, P.Shendro, P.Phanin, P.Treshk })
                Assert.AreEqual(1, pools.Count(pool => pool.Creature1Id == boss), $"{boss}");

            var markers = world.Set<MissionIndicatorEntry>().AsNoTracking().Where(row => row.ContentRevision == P.Revision).ToList();
            Assert.IsTrue(markers.All(row => row.IndicatorId > MissionIndicator.UnnamedFrom));
        }

        [TestMethod]
        public void EveryPoolStandsOnTheGroundAndNoNpcInAnother()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var pools = database.Context.SpawnPoolEntries.AsNoTracking().ToList();
            var ours = P.Pools.Select(p => p.Id).Append(P.GreloranPool).Append(P.Phenix).Append(P.Whitaker).ToHashSet();
            var navs = new Dictionary<uint, NavMeshQuery>
            {
                [P.Facility] = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), "adv_arieki_torden_plains_penalresearch")),
                [P.Plains] = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), "adv_arieki_torden_plains"))
            };

            foreach (var pool in pools.Where(pool => ours.Contains(pool.Id)))
            {
                var at = new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ);
                var ground = navs[pool.MapContextId].Nearest(at);
                Assert.IsTrue(ground.HasValue, $"{pool.Id} off the ground");
                Assert.IsLessThan(0.6f, Vector2.Distance(new Vector2(at.X, at.Z), new Vector2(ground.Value.X, ground.Value.Z)), $"{pool.Id} over walkable ground");
                Assert.IsLessThan(1f, Math.Abs(ground.Value.Y - at.Y), $"{pool.Id}: the ground {ground.Value.Y - at.Y} m from it");
            }

            var facility = navs[P.Facility];
            foreach (var spot in P.Mainframes.Select(m => m.At).Concat(P.Research.Select(r => r.At)).Concat(P.Crates))
            {
                var ground = facility.Nearest(spot);
                Assert.IsTrue(ground.HasValue && Vector2.Distance(new Vector2(spot.X, spot.Z), new Vector2(ground.Value.X, ground.Value.Z)) < 1f &&
                    Math.Abs(ground.Value.Y - spot.Y) < 1f, $"{spot} off the ground");
            }

            var npcs = pools.Where(pool => (pool.MapContextId == P.Facility || pool.MapContextId == P.Plains) &&
                (pool.Creature1Id is >= 500000 and < 520000 || pool.Creature1Id == P.Greloran)).ToList();
            foreach (var pool in npcs.Where(pool => ours.Contains(pool.Id)))
                foreach (var other in npcs.Where(other => other.Id != pool.Id && other.MapContextId == pool.MapContextId))
                    Assert.IsFalse(Math.Sqrt(Math.Pow(pool.PosX - other.PosX, 2) + Math.Pow(pool.PosZ - other.PosZ, 2)) < 1.5 &&
                        Math.Abs(pool.PosY - other.PosY) < 3, $"{pool.Id} in {other.Id}");
        }

        /// <summary>Each door stands in its jamb's middle, where the jamb is narrower than the door: nothing gets round it.</summary>
        [TestMethod]
        public void EachDoorFillsItsJamb()
        {
            var cover = CoverMesh.Read(CoverMesh.PathFor(NavmeshDirectory(), "adv_arieki_torden_plains_penalresearch"));

            foreach (var door in P.Doors)
            {
                var turn = Quaternion.CreateFromYawPitchRoll(door.Yaw, 0f, 0f);
                var chest = door.At + new Vector3(0, 1.5f, 0);
                foreach (var side in new[] { -1f, 1f })
                    Assert.IsTrue(cover.GeometryBlocks(chest, chest + Vector3.Transform(new Vector3(side * 3.4f, 0, 0), turn)),
                        $"{door.Name}: open past the door's {(side < 0 ? "left" : "right")} edge");

                // And the corridor runs through it: open in front of and behind the door.
                Assert.IsFalse(cover.GeometryBlocks(chest + Vector3.Transform(new Vector3(0, 0, 3f), turn), chest), $"{door.Name}: shut in front");
                Assert.IsFalse(cover.GeometryBlocks(chest + Vector3.Transform(new Vector3(0, 0, -3f), turn), chest), $"{door.Name}: shut behind");
            }
        }

        [TestMethod]
        public void TheMigrationGoesInOnceAndDownPutsEverythingBack()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var poolsBefore = Pools(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(P.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{P.Revision}' AND enabled = 1"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 782000 AND 782999");
            Assert.AreEqual(P.Creatures.Length, rows);
            Assert.AreEqual(P.Pools.Length + 1, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 782000 AND 782999"));

            foreach (var sql in P.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 782000 AND 782999"), "guarded");

            migrator.Migrate(Previous);
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 782000 AND 782999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{P.Revision}'"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {P.Whitaker} AND package_id = {P.WhitakerSeededPackage}"));
            var poolsAfter = Pools(database);
            CollectionAssert.AreEquivalent(poolsBefore.Keys.ToList(), poolsAfter.Keys.ToList());
            foreach (var (id, before) in poolsBefore)
                Assert.AreEqual(before, poolsAfter[id], $"pool {id}");

            migrator.Migrate(Migration);
            Assert.AreEqual(P.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{P.Revision}' AND enabled = 1"));
        }

        /// <summary>The hospital's way down takes only a player with the keycard, or Key Information done.</summary>
        [TestMethod]
        public void TheWayInIsSealedWithoutTheKeycard()
        {
            using var harness = Create();
            Assert.IsTrue(PhaninResearch.MayEnter(harness.Client, K.Colony), "another map is another matter");
            Assert.IsFalse(PhaninResearch.MayEnter(harness.Client, P.Facility));

            Give(harness, K.KeycardTemplate);
            Assert.IsTrue(PhaninResearch.MayEnter(harness.Client, P.Facility), "with the keycard");

            using var other = Create();
            Completed(other, K.KeyInformation, K.Revision);
            Assert.IsTrue(PhaninResearch.MayEnter(other.Client, P.Facility), "Key Information done");
        }

        /// <summary>The doors, stage by stage, as the mainframes go down: shut, they stop players and creatures.</summary>
        [TestMethod]
        public void TheMainframesOpenTheDoorsStageByStage()
        {
            using var harness = Create();
            Work(harness);
            var doors = PhaninResearch.Doors(harness.Map);
            Assert.HasCount(P.Doors.Length, doors);

            // Shut: across each, nobody walks.
            for (var i = 0; i < doors.Count; i++)
            {
                var turn = Quaternion.CreateFromYawPitchRoll(P.Doors[i].Yaw, 0f, 0f);
                var front = P.Doors[i].At + Vector3.Transform(new Vector3(0, 0.5f, 2f), turn);
                var back = P.Doors[i].At + Vector3.Transform(new Vector3(0, 0.5f, -2f), turn);
                Assert.IsTrue(ForceFields.StopsPlayer(harness.Map, front, back), P.Doors[i].Name);
                Assert.IsTrue(ForceFields.Stops(harness.Map, Put(harness, P.Guard, front), front, back), $"{P.Doors[i].Name}: a Brann");
            }

            var mainframes = PhaninResearch.Objects(harness.Map, PhaninResearch.Kind.Mainframe);
            Assert.HasCount(9, mainframes);

            for (var stage = 0; stage < 3; stage++)
            {
                for (var i = 0; i < P.Mainframes.Length; i++)
                    if (P.Mainframes[i].Stage == stage)
                    {
                        Assert.IsFalse(PhaninResearch.StageOpen(harness.Map, stage + 1), $"stage {stage + 1} before {P.Mainframes[i].Room}");
                        Use(harness, mainframes[i]);
                        Assert.AreEqual(UseObjectState.TsState1, mainframes[i].StateId, P.Mainframes[i].Room);
                    }

                Work(harness);
                Assert.IsTrue(PhaninResearch.StageOpen(harness.Map, stage + 1), $"stage {stage + 1}");
                for (var d = 0; d < P.Doors.Length; d++)
                    Assert.AreEqual(P.Doors[d].Stage <= stage + 1, doors[d].IsDestroyed, $"{P.Doors[d].Name} at stage {stage + 1}");
            }

            // A dark mainframe does nothing more.
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, PhaninResearch.Refusal(harness.Client, mainframes[0]));

            // The laboratories' Atta are out.
            Assert.AreEqual(P.Cages.Sum(lab => lab.Cages.Length), PhaninResearch.FreedAtta(harness.Map).Count);
        }

        /// <summary>
        /// A new copy - the instance unloaded, or another squad's - opens for a member whose Mind of
        /// a Madman has a stage done, and all of it for one who has done the mission.
        /// </summary>
        [TestMethod]
        public void ACopyOpensWhatAMembersMissionHasDone()
        {
            using var harness = Create();
            Completed(harness, P.MindOfAMadman, P.Revision);
            Work(harness);
            for (var stage = 1; stage <= 3; stage++)
                Assert.IsTrue(PhaninResearch.StageOpen(harness.Map, stage), $"stage {stage}");
            Assert.IsTrue(PhaninResearch.Doors(harness.Map).All(door => door.IsDestroyed));
            Assert.IsFalse(ForceFields.StopsPlayer(harness.Map, P.Doors[0].At + new Vector3(0, 0.5f, 2f), P.Doors[0].At + new Vector3(0, 0.5f, -2f)));
        }

        /// <summary>Reyko to Phenix; Phenix's nine mainframes, each stage shown as the one before is done; Reyko takes it.</summary>
        [TestMethod]
        public void PhenixSendsYouThroughTheNineMainframesAndReykoTakesIt()
        {
            using var harness = Create();
            Completed(harness, K.SecurityThreat, K.Revision);
            var reyko = Put(harness, K.Reyko, P.Arrival + new Vector3(-3, 0, 0));
            Accept(harness, reyko, P.IntoTheFacility);
            var phenix = Put(harness, P.Phenix, P.Arrival + new Vector3(-6, 0, 0));
            Talk(harness, phenix, P.IntoTheFacility, 1);
            Reward(harness, phenix, P.IntoTheFacility, null, null, 10000, 1500);

            Accept(harness, phenix, P.MindOfAMadman);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, P.MindOfAMadman, P.Security1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, P.MindOfAMadman, P.Security2));
            Assert.AreNotEqual(MissionObjectiveState.Incomplete, Objective(harness, P.MindOfAMadman, P.Security3), "not yet");

            Work(harness);
            var mainframes = PhaninResearch.Objects(harness.Map, PhaninResearch.Kind.Mainframe);
            for (var i = 0; i < P.Mainframes.Length; i++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, P.MindOfAMadman, P.Mainframes[i].Objective), P.Mainframes[i].Room);
                Use(harness, mainframes[i]);
                Work(harness);
                Work(harness);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, P.MindOfAMadman, P.Mainframes[i].Objective), P.Mainframes[i].Room);
            }

            reyko = Put(harness, K.Reyko, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, reyko, P.MindOfAMadman, null, null, P.FacilityExperience, (int)P.FacilityCredits);
        }

        /// <summary>Zim's research: into the facility, each console's piece once, all of it to Zim.</summary>
        [TestMethod]
        public void TheResearchConsolesGiveTheirPiecesAndZimTakesThem()
        {
            using var harness = Create();
            Completed(harness, K.BugHunt, K.Revision);
            var zim = Put(harness, K.Zim, P.Arrival + new Vector3(-3, 0, 0));
            Accept(harness, zim, P.DoBugsFeelFear);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, P.DoBugsFeelFear, P.InfiltrateObjective), "in the facility");

            var consoles = PhaninResearch.Objects(harness.Map, PhaninResearch.Kind.Research);
            Assert.HasCount(5, consoles);
            for (var i = 0; i < consoles.Count; i++)
            {
                Use(harness, consoles[i]);
                Assert.AreEqual(1, Held(harness, P.Research[i].ItemTemplate), P.Research[i].Key);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, P.DoBugsFeelFear, P.Research[i].Objective), P.Research[i].Key);
                Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, PhaninResearch.Refusal(harness.Client, consoles[i]), "once");
            }

            zim = Put(harness, K.Zim, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, zim, P.DoBugsFeelFear, null, null, P.FacilityExperience, (int)P.FacilityCredits);
            Assert.IsTrue(P.Research.All(r => Held(harness, r.ItemTemplate) == 0), "Zim has the research");

            // Whitaker's is Bug Hugger's: not for one who did Bug Hunt.
            var whitaker = Put(harness, P.Whitaker, harness.Client.Player.Position + new Vector3(-2, 0, 0));
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, whitaker.EntityId, P.DoBugsHaveFeelings));
        }

        /// <summary>The crates: one supply each for the player, six for Finobee.</summary>
        [TestMethod]
        public void TheMedicalCratesGiveFinobeeHisSupplies()
        {
            using var harness = Create();
            Completed(harness, K.AttaControl, K.Revision);
            var reyko = Put(harness, K.Reyko, P.Arrival + new Vector3(-3, 0, 0));
            Accept(harness, reyko, P.MedicalSupplyRecovery);
            Work(harness);

            var crates = PhaninResearch.Objects(harness.Map, PhaninResearch.Kind.Crate);
            Use(harness, crates[0]);
            Assert.AreEqual(1, Held(harness, P.MedicalTemplate));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, PhaninResearch.Refusal(harness.Client, crates[0]), "one each a crate");

            for (var i = 1; i < P.SuppliesWanted; i++)
                Use(harness, crates[i]);
            Assert.AreEqual((long)P.SuppliesWanted, Held(harness, P.MedicalTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, P.MedicalSupplyRecovery, 1));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, PhaninResearch.Refusal(harness.Client, crates[(int)P.SuppliesWanted]), "enough");

            var finobee = Put(harness, K.Finobee, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, finobee, P.MedicalSupplyRecovery, null, null, P.FacilityExperience, (int)P.FacilityCredits);
            Assert.AreEqual(0, Held(harness, P.MedicalTemplate));
        }

        /// <summary>Treshk's corpse has Erodan's proof; Mernash, Shendro and Phanin dead are Greloran's justice.</summary>
        [TestMethod]
        public void TreshkHasTheProofAndTheThreeHeadsAreGreloransJustice()
        {
            using var harness = Create();
            Completed(harness, K.KeyInformation, K.Revision);
            Completed(harness, K.SecurityThreat, K.Revision);
            var erodan = Put(harness, K.Erodan, P.Arrival + new Vector3(-3, 0, 0));
            Accept(harness, erodan, P.ProofOfCollusion);
            var greloran = Put(harness, P.Greloran, P.Arrival + new Vector3(-6, 0, 0));
            Accept(harness, greloran, P.ReformistJustice);

            var treshk = Put(harness, P.Treshk, P.TreshkAt);
            WildernessDaghdasUrnTests.Kill(harness, treshk);
            var corpse = harness.Map.LootDispensers[treshk.CorpseLootEntityId];
            Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == P.ProofTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, P.ProofOfCollusion, 1));

            foreach (var (boss, at, objective) in new[] { (P.Mernash, P.MernashAt, 1U), (P.Shendro, P.ShendroAt, 2U), (P.Phanin, P.PhaninAt, 3U) })
            {
                WildernessDaghdasUrnTests.Kill(harness, Put(harness, boss, at));
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, P.ReformistJustice, objective), $"{boss}");
            }

            erodan = Put(harness, K.Erodan, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, erodan, P.ProofOfCollusion, null, null, P.FacilityExperience, (int)P.FacilityCredits);
            Assert.AreEqual(0, Held(harness, P.ProofTemplate), "Erodan has the proof");
            greloran = Put(harness, P.Greloran, harness.Client.Player.Position + new Vector3(-2, 0, 0));
            Reward(harness, greloran, P.ReformistJustice, null, null, P.FacilityExperience, (int)P.FacilityCredits);
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static Dictionary<uint, string> Pools(Rasa.Test.Database.OrdinaryLootTestDatabase database) =>
            database.Context.SpawnPoolEntries.AsNoTracking().Where(p => p.MapContextId == P.Facility || p.MapContextId == P.Plains).ToList()
                .ToDictionary(p => p.Id, p => $"{p.PosX:F2} {p.PosY:F2} {p.PosZ:F2} {p.Rotation:F2} {p.Creature1Id}");

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: P.Facility);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, P.Revision, CharacterStartingExperienceState.Skipped)));
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

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions.TryGetValue(missionId, out var log) && log.Objectives.TryGetValue(objectiveId, out var objective)
                ? objective.State
                : MissionObjectiveState.NotAssigned;

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, harness.Map.MapInfo.MapContextId);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private static void Work(WildernessRuntimeTestHarness harness)
        {
            PhaninResearch.Worker(harness.Map);
            harness.Drain();
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

        private static void Give(WildernessRuntimeTestHarness harness, uint template)
        {
            var item = ItemManager.Instance.CreateFromTemplateId(template, 1);
            Assert.IsNotNull(InventoryManager.Instance.GrantItemToInventory(harness.Client, item), $"template {template}");
            harness.Drain();
        }

        private static void Completed(WildernessRuntimeTestHarness harness, uint missionId, string revision)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = revision
                }, harness.UtcNow));
            harness.Client.Player.MissionHistory[missionId] = MissionState.Completed;
            harness.Client.Player.MissionSuccessHistory.Add(missionId);
        }

        private static string NavmeshDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "navmesh not found");
            return Path.Combine(directory.FullName, "navmesh");
        }

        #endregion
    }
}
