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
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// The Kardash Atta Colony as the guide tells it (KardashAttaColonyContent, KardashAttaColony):
    /// its rows and ten missions, the Irendas givers where they stand, the console, the
    /// transducers, Donners' choice, Bug Hugger's places and the Bane at the Queen, and the chain
    /// to the Phanin Research Facility's keycard.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KardashAttaColonyTests
    {
        private const string Migration = "20271015000000_Kardash_atta_colony";
        private const string Previous = "20271014000000_Warnet_caverns";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            KardashAttaColony.Reset();
            KardashAttaColony.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            KardashAttaColony.Reset();
            KardashAttaColony.Now = () => Environment.TickCount64;
        }

        [TestMethod]
        public void TheColonyHasItsPeopleItsAttaItsBaneAndItsTenMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in K.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(K.Category, mission.CategoryId);
            }

            var packages = world.Set<NpcPackageEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual(K.MirasPackage, packages[K.Miras]);
            Assert.AreEqual(K.ReshiPackage, packages[K.Reshi]);
            Assert.AreEqual(K.ErodanPackage, packages[K.Erodan]);
            Assert.AreEqual(K.TralosPackage, packages[K.Tralos]);
            Assert.AreEqual(K.ReykoPackage, packages[K.Reyko]);
            Assert.AreEqual(K.DonnersPackage, packages[K.Donners]);

            // Tralos is Brann, as Miras, Kranix, Reshi and Erodan are.
            var creatures = world.Set<CreatureEntry>().AsNoTracking().ToDictionary(c => c.Id);
            foreach (var brann in K.NewPeople.Append(K.Tralos))
            {
                Assert.AreEqual(K.BrannSwapsetMale, creatures[brann].ClassId, $"{brann}");
                Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == brann && a.SlotId == 15 && a.ClassId == 7256), $"{brann}");
            }

            // Every Atta of the colony is the colony's own, at the instance's levels; none is the Plains'.
            var pools = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => pool.MapContextId == K.Colony).ToList();
            var slots = pools.SelectMany(pool => new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id,
                pool.Creature5Id, pool.Creature6Id }).Where(id => id != 0).Distinct().ToList();
            Assert.IsFalse(slots.Contains(K.PlainsAttaSoldier) || slots.Contains(K.PlainsAttaHarvester), "a Plains row left in the colony");
            foreach (var id in slots.Where(id => creatures[id].Faction == 0))
                Assert.IsTrue(creatures[id].Level is >= 20 and <= 25, $"{id} {creatures[id].Comment} level {creatures[id].Level}");
            Assert.AreEqual(25U, creatures[K.Queen].Level);
            Assert.AreEqual(42, pools.Count(pool => pool.Creature1Id is K.Soldier or K.Worker), "the seeded 42");

            // The gear carries its brand's module.
            var rewards = world.Set<MissionRewardItemEntry>().AsNoTracking().Where(item => item.ContentRevision == K.Revision).ToList();
            var modules = rewards.ToDictionary(item => (item.MissionId, item.ItemTemplateId), item => item.Module1);
            Assert.AreEqual(K.ResistIce, modules[(K.UnexpectedGuests, K.StealthLegs)]);
            Assert.AreEqual(K.DebuffFire, modules[(K.UnexpectedGuests, K.IncendiaryLeechGun)]);
            Assert.AreEqual(K.ArmorAbsorption, modules[(K.UnexpectedGuests, K.MotorAssistGloves)]);
            Assert.AreEqual(K.ArmorAbsorption, modules[(K.JustToBeCertain, K.BioVest)]);
            Assert.AreEqual(K.PowerRegen, modules[(K.JustToBeCertain, K.GravitonHelmet)]);
            Assert.AreEqual(K.DebuffPhysical, modules[(K.JustToBeCertain, K.Shotgun)]);
            Assert.AreEqual(0U, modules[(K.KeyInformation, K.KeycardTemplate)]);

            // Each marker is the client's own name or none, and is on the map it is on.
            var markers = world.Set<MissionIndicatorEntry>().AsNoTracking().Where(row => row.ContentRevision == K.Revision).ToList();
            Assert.IsTrue(markers.All(row => row.IndicatorId == K.QueensChamberMarker || row.IndicatorId > MissionIndicator.UnnamedFrom));
            foreach (var row in markers)
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(row.MissionId, out _));
        }

        /// <summary>Everyone this puts in the world or moves stands on its map's ground, and no two NPCs in each other.</summary>
        [TestMethod]
        public void EveryPersonAndPoolStandsOnTheGroundAndNoNpcInAnother()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var pools = database.Context.SpawnPoolEntries.AsNoTracking().ToList();
            var ours = K.Pools.Select(p => p.Id).Concat(K.Moved.Select(m => m.Pool)).ToHashSet();
            var navs = new Dictionary<uint, NavMeshQuery>
            {
                [K.Colony] = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), "adv_arieki_torden_plains_attacolony")),
                [K.Plains] = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), "adv_arieki_torden_plains"))
            };

            foreach (var pool in pools.Where(pool => ours.Contains(pool.Id)))
            {
                var at = new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ);
                var ground = navs[pool.MapContextId].Nearest(at);
                Assert.IsTrue(ground.HasValue, $"{pool.Id} off the ground");
                Assert.IsLessThan(0.6f, Vector2.Distance(new Vector2(at.X, at.Z), new Vector2(ground.Value.X, ground.Value.Z)), $"{pool.Id} over walkable ground");
                Assert.IsLessThan(1f, Math.Abs(ground.Value.Y - at.Y), $"{pool.Id}: the ground {ground.Value.Y - at.Y} m from it");
            }

            var npcs = pools.Where(pool => (pool.MapContextId == K.Colony || pool.MapContextId == K.Plains) &&
                (pool.Creature1Id is >= 500000 and < 520000 || K.NewPeople.Contains(pool.Creature1Id))).ToList();
            foreach (var pool in npcs.Where(pool => ours.Contains(pool.Id)))
                foreach (var other in npcs.Where(other => other.Id != pool.Id && other.MapContextId == pool.MapContextId))
                    Assert.IsFalse(Math.Sqrt(Math.Pow(pool.PosX - other.PosX, 2) + Math.Pow(pool.PosZ - other.PosZ, 2)) < 1.5 &&
                        Math.Abs(pool.PosY - other.PosY) < 3, $"{pool.Id} in {other.Id}");
        }

        [TestMethod]
        public void TheMigrationGoesInOnceAndDownPutsEverythingBack()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var poolsBefore = Pools(database);
            var tralosBefore = database.Context.CreatureAppearanceEntries.AsNoTracking().Where(a => a.Id == K.Tralos).Select(a => new { a.SlotId, a.ClassId, a.Color }).ToList();

            migrator.Migrate(Migration);
            Assert.AreEqual(K.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{K.Revision}' AND enabled = 1"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 781000 AND 781999");
            Assert.AreEqual(K.Creatures.Length, rows);
            Assert.AreEqual(K.Pools.Length, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 781000 AND 781999"));

            foreach (var sql in K.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 781000 AND 781999"), "guarded");

            migrator.Migrate(Previous);
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 781000 AND 781999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{K.Revision}'"));
            var poolsAfter = Pools(database);
            CollectionAssert.AreEquivalent(poolsBefore.Keys.ToList(), poolsAfter.Keys.ToList());
            foreach (var (id, before) in poolsBefore)
                Assert.AreEqual(before, poolsAfter[id], $"pool {id}");
            Assert.AreEqual((uint)K.HumanSwapsetMale, database.Context.CreatureEntries.AsNoTracking().Single(c => c.Id == K.Tralos).ClassId);
            CollectionAssert.AreEquivalent(tralosBefore,
                database.Context.CreatureAppearanceEntries.AsNoTracking().Where(a => a.Id == K.Tralos).Select(a => new { a.SlotId, a.ClassId, a.Color }).ToList());

            migrator.Migrate(Migration);
            Assert.AreEqual(K.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{K.Revision}' AND enabled = 1"));
        }

        /// <summary>Reyko's Into the Hive reported from the console, by radio, and Atta Control offered next.</summary>
        [TestMethod]
        public void TheConsoleTakesTheReportAndAttaControlIsOfferedByRadio()
        {
            using var harness = Create();
            var reyko = Put(harness, K.Reyko, K.Arrival + new Vector3(2, 0, 0));
            Accept(harness, reyko, K.IntoTheHive);
            Work(harness);

            var console = KardashAttaColony.Console(harness.Map);
            Assert.IsNotNull(console);
            Assert.IsLessThan(1f, Vector3.Distance(console.Position, K.Console));
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(console.Position + new Vector3(-2, 0, 0));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = console.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = console.EntityId, MissionId = K.IntoTheHive, ObjectiveId = 1, PlayerFlagId = 1
            });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.IntoTheHive, 1));

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, K.IntoTheHive, null, null), "radioed in");
            harness.Drain();

            Work(harness);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, K.AttaControl), "offered by radio");
            harness.Drain();
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[K.AttaControl].State);
        }

        /// <summary>The five transducers, each its objective, then the report to Reyko shown and taken.</summary>
        [TestMethod]
        public void EachTransducerDownIsItsObjectiveAndAllFiveShowTheReportToReyko()
        {
            using var harness = Create();
            Completed(harness, K.IntoTheHive);
            Work(harness);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, K.AttaControl));
            harness.Drain();

            var transducers = KardashAttaColony.Transducers(harness.Map);
            Assert.HasCount(5, transducers);
            for (var i = 0; i < transducers.Count; i++)
            {
                Assert.IsNotNull(transducers[i], $"transducer {i + 1}");
                Assert.IsLessThan(1.5f, Vector3.Distance(transducers[i].Object.Position, K.Transducers[i].At), $"transducer {i + 1}");
            }

            for (var i = 0; i < 4; i++)
            {
                harness.MoveTo(transducers[i].Object.Position + new Vector3(3, 0, 0));
                Assert.IsTrue(PlacedDestructibles.Destroy(transducers[i], harness.Client.Player));
                Work(harness);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.AttaControl, (uint)i + 1));
            }

            Assert.AreNotEqual(MissionObjectiveState.Incomplete, Objective(harness, K.AttaControl, K.ReportToReyko), "not yet");

            harness.MoveTo(transducers[4].Object.Position + new Vector3(3, 0, 0));
            Assert.IsTrue(PlacedDestructibles.Destroy(transducers[4], harness.Client.Player));
            Work(harness);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.AttaControl, K.TransducerCounter));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, K.AttaControl, K.ReportToReyko));

            // Down, they stay down.
            _now += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(transducers.All(live => live.IsDown));

            var reyko = Put(harness, K.Reyko, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Talk(harness, reyko, K.AttaControl, K.ReportToReyko);
            Reward(harness, reyko, K.AttaControl, null, null, 20000, 3000);
        }

        /// <summary>Zim's Bug Hunt: the colony found, the brains counted, Donners kept to.</summary>
        [TestMethod]
        public void BugHuntFindsTheColonyCountsTheBrainsAndDonnersIsKeptTo()
        {
            using var harness = Create();
            var zim = Put(harness, K.Zim, K.Arrival + new Vector3(2, 0, 0));
            Accept(harness, zim, K.BugHunt);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHunt, K.FindColony), "in the colony");

            // Boritak's brain: off his corpse, not his death.
            var boritak = Put(harness, K.Boritak, K.BoritakAt);
            WildernessDaghdasUrnTests.Kill(harness, boritak);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, K.BugHunt, 5), "a kill is not a brain");
            var corpse = harness.Map.LootDispensers[boritak.CorpseLootEntityId];
            Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == K.BoritakBrainTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHunt, 5));

            for (var i = 0; i < K.BrainsWanted; i++)
                Give(harness, K.AttaBrainTemplate);
            foreach (var brain in K.BossBrains.Where(brain => brain.Boss != K.Boritak))
                Give(harness, brain.ItemTemplate);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHunt, 4));
            foreach (var brain in K.BossBrains)
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHunt, brain.Objective), brain.Key);

            harness.SpawnWorld(K.Donners);
            var donners = RequireNpc(harness, K.Donners, K.Donners);
            OpenNativeNpcConversation(harness, donners);
            Assert.IsTrue(harness.Manager.TryPerformNpcChoice(harness.Client, donners.EntityId, K.BugHunt, K.SpeakWithDonners, 1, 1));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHunt, K.SpeakWithDonners));

            zim = Put(harness, K.Zim, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, zim, K.BugHunt, null, null, 22000, 3300);
            Assert.AreEqual(0, Held(harness, K.AttaBrainTemplate), "Zim has the brains");
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, donners.EntityId, K.BugHugger), "Bug Hunt done, not Bug Hugger");
        }

        /// <summary>Given up at Donners' asking, Bug Hunt fails and she has Bug Hugger; the places, the Queen, Reshi, then her.</summary>
        [TestMethod]
        public void GivingBugHuntUpForDonnersLeadsToBugHuggerAndItsPlacesAndTheQueen()
        {
            using var harness = Create();
            var zim = Put(harness, K.Zim, K.Arrival + new Vector3(2, 0, 0));
            Accept(harness, zim, K.BugHunt);
            Work(harness);

            harness.SpawnWorld(K.Donners);
            var donners = RequireNpc(harness, K.Donners, K.Donners);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, donners.EntityId, K.BugHugger), "not while on Bug Hunt");
            OpenNativeNpcConversation(harness, donners);
            Assert.IsTrue(harness.Manager.TryPerformNpcChoice(harness.Client, donners.EntityId, K.BugHunt, K.SpeakWithDonners, 1, 2));
            harness.Drain();
            Assert.AreNotEqual(MissionState.Active, harness.Client.Player.Missions.TryGetValue(K.BugHunt, out var hunt) ? hunt.State : MissionState.Failed);

            Accept(harness, donners, K.BugHugger);
            Assert.AreNotEqual(MissionObjectiveState.Incomplete, Objective(harness, K.BugHugger, K.TalkToDonners), "not yet");

            foreach (var (objective, at) in new (uint, Vector3)[]
                     {
                         (K.ExploreGrotto, new Vector3(379.13f, 200.53f, -60.43f)), (K.ExploreInnerNest, new Vector3(409.52f, 114.97f, -381.9f)),
                         (K.ExploreRoyalNest, new Vector3(-302.24f, 45.05f, -300.45f))
                     })
            {
                harness.MoveTo(at);
                Work(harness);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHugger, objective), $"{objective}");
            }

            // The Harvest Grotto's circle reaches over the Inner Nest: from below, it is not the grotto.
            Assert.IsFalse(K.Places[0].Contains(new Vector3(409.52f, 114.97f, -160f)));

            harness.MoveTo(new Vector3(-340f, 41.13f, 210f));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHugger, K.ExploreQueensChamber));
            var raiders = KardashAttaColony.Raiders(harness.Map);
            Assert.HasCount(K.RaiderSpots.Length, raiders);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, K.BugHugger, K.ProtectQueen));
            foreach (var raider in raiders.ToList())
                Kill(harness, raider);
            harness.MoveTo(new Vector3(-340f, 41.13f, 210f));
            Work(harness);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.BugHugger, K.ProtectQueen));

            var reshi = Put(harness, K.Reshi, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Talk(harness, reshi, K.BugHugger, K.TalkToReshi);
            Work(harness);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, K.BugHugger, K.TalkToDonners));

            donners = Put(harness, K.Donners, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Talk(harness, donners, K.BugHugger, K.TalkToDonners);
            zim = Put(harness, K.Zim, harness.Client.Player.Position + new Vector3(-2, 0, 0));
            Reward(harness, zim, K.BugHugger, null, null, 22000, 3300);
        }

        /// <summary>Erodan to Tralos to Miras; the infiltrators; Miras' package to Tralos and the keycard; Reyko.</summary>
        [TestMethod]
        public void TheBrothersChainEndsWithTheKeycardAndReyko()
        {
            using var harness = Create();
            var erodan = Put(harness, K.Erodan, K.Arrival + new Vector3(2, 0, 0));
            Accept(harness, erodan, K.ADividedPeople);
            var tralos = Put(harness, K.Tralos, K.Arrival + new Vector3(-2, 0, 0));
            Talk(harness, tralos, K.ADividedPeople, 1);
            Reward(harness, tralos, K.ADividedPeople, null, null, 6000, 900);

            Accept(harness, tralos, K.TaleOfTwoBrothers);
            harness.SpawnWorld(K.MirasPool, K.KranixPool);
            var miras = RequireNpc(harness, K.MirasPool, K.Miras);
            Talk(harness, miras, K.TaleOfTwoBrothers, 1);
            Reward(harness, miras, K.TaleOfTwoBrothers, null, null, 8000, 1200);

            Accept(harness, miras, K.UnexpectedGuests);
            var kranix = RequireNpc(harness, K.KranixPool, K.Kranix);
            Accept(harness, kranix, K.JustToBeCertain);

            for (var i = 0; i < K.InfiltratorsWanted; i++)
                Kill(harness, Put(harness, K.Infiltrator, K.Transducers[1].At));
            foreach (var elite in K.Elites)
                Kill(harness, Put(harness, elite, K.Transducers[1].At));
            for (uint objective = 1; objective <= 5; objective++)
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.UnexpectedGuests, objective), $"{objective}");

            miras = Put(harness, K.Miras, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, miras, K.UnexpectedGuests, 0, K.StealthLegs, 20000, 5000);

            Give(harness, K.DataDiscTemplate);
            harness.Drain();
            kranix = Put(harness, K.Kranix, harness.Client.Player.Position + new Vector3(-2, 0, 0));
            Reward(harness, kranix, K.JustToBeCertain, 2, K.Shotgun, 15000, 5000);
            Assert.AreEqual(0, Held(harness, K.DataDiscTemplate), "Kranix has the disc");

            Accept(harness, miras, K.KeyInformation);
            Assert.AreEqual(1, Held(harness, K.PackageTemplate), "Miras' package");
            tralos = Put(harness, K.Tralos, harness.Client.Player.Position + new Vector3(0, 0, 2));
            Talk(harness, tralos, K.KeyInformation, 1);
            Reward(harness, tralos, K.KeyInformation, null, K.KeycardTemplate, 10000, 1500);
            Assert.AreEqual(0, Held(harness, K.PackageTemplate), "Tralos has the package");
            Assert.AreEqual(1, Held(harness, K.KeycardTemplate), "the keycard is the player's");

            Accept(harness, tralos, K.SecurityThreat);
            var reyko = Put(harness, K.Reyko, harness.Client.Player.Position + new Vector3(0, 0, -2));
            Talk(harness, reyko, K.SecurityThreat, 1);
            Reward(harness, reyko, K.SecurityThreat, null, null, 8000, 1200);
            Assert.AreEqual(1, Held(harness, K.KeycardTemplate), "kept");
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static Dictionary<uint, string> Pools(Rasa.Test.Database.OrdinaryLootTestDatabase database) =>
            database.Context.SpawnPoolEntries.AsNoTracking().Where(p => p.MapContextId == K.Colony || p.MapContextId == K.Plains).ToList()
                .ToDictionary(p => p.Id, p => $"{p.PosX:F2} {p.PosY:F2} {p.PosZ:F2} {p.Rotation:F2} {p.Creature1Id} {p.Creature2Id}");

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: K.Colony);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, K.Revision, CharacterStartingExperienceState.Skipped)));
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

        private void Work(WildernessRuntimeTestHarness harness, long milliseconds = 100)
        {
            _now += milliseconds;
            KardashAttaColony.Worker(harness.Map);
            harness.Drain();
        }

        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player, CritKill.Own);
            harness.Drain();
        }

        /// <summary>An item into the player's inventory as loot comes, its acquisition recorded.</summary>
        private static void Give(WildernessRuntimeTestHarness harness, uint template)
        {
            var item = ItemManager.Instance.CreateFromTemplateId(template, 1);
            Assert.IsNotNull(InventoryManager.Instance.GrantItemToInventory(harness.Client, item), $"template {template}");
            harness.Drain();
        }

        /// <summary>The mission done before, as a history row the server keeps.</summary>
        private static void Completed(WildernessRuntimeTestHarness harness, uint missionId)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = K.Revision
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
