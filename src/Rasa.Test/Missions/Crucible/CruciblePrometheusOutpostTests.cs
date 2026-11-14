using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using C = CrucibleOutpostIntrepidTests;
    using K = CrucibleKardashAndPrometheusNpcsV1;
    using O = CruciblePrometheusOutpostNpcsV1;
    using I = CrucibleIronGiantsV1;
    using W = CrucibleWhosInChargeHereV1;
    using B = CrucibleBigKahunaV1;

    /// <summary>
    /// Crucible set 8b, played through as a player plays it: Larai Zupa Madias' Iron Giants (1213: six Bane Juggernauts at
    /// Khulago Base), Who's In Charge Here? (1215: the Viddia hacking program into a Bane CommLink Terminal, then the hacked
    /// terminal names the base's commander) and The Big Kahuna (1216: kill Maxxum Ugoretz). With them the set's places: the
    /// Juggernaut pools, Ugoretz's pool and the terminal on the navmesh, walked to from Confirmed Kills' north trench marker
    /// with no lava on the walk.
    ///
    /// Why these tests: the Juggernaut and Ugoretz are new rows, the terminal is two actors (one per objective) swapped by a
    /// scenario. An offer before its prerequisite, a kill before the accept counted, a count off by one, progress or the
    /// hacked terminal lost at a reload, a program or terminal kept after an abandon, a turn-in that pays twice or on a full
    /// bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CruciblePrometheusOutpostTests
    {
        private const uint Kills = CrucibleConfirmedKillsV1.MissionId;
        private const uint Giants = I.MissionId, Charge = W.MissionId, Kahuna = B.MissionId;
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint ClientJuggernauts = 6;          // client missionobjective 1213,1 "Destroy 6 Juggernauts" (written out, not the data class's)
        private const uint ClientProgramTemplate = 96429;  // client item class 25313 "Viddia Syndicate Hacking Program"
        private const uint ClientTerminalClass = 10519, ClientTerminalPackage = 1453;   // the Bane console class of 1368 (PoolsPoisonControlV1); objectiveconversation 1215,1 and 1215,2

        private static readonly Vector3 Prometheus = ToVector(K.PrometheusWaypoint);
        /// <summary>Confirmed Kills' north trench marker: the way into Khulago Base with no lava from the waypoint (CrucibleKardashAndPrometheusTests).</summary>
        private static readonly Vector3 NorthTrench = ToVector(CrucibleConfirmedKillsV1.TrenchesAt[1]);
        private static readonly uint[] JuggernautPools = O.JuggernautPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The Juggernaut pools and Ugoretz's pool on the navmesh, 5 m or more from a wall, walked to from the north trench
        /// marker with no lava; their creatures come up within the rings round the point (radius 0), of the client's class and
        /// name, hostile; the terminal's place on the navmesh, walked to with no lava, 20 m or more from every hostile there.
        /// </summary>
        [TestMethod]
        public void TheKhulagoPlacesStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, NorthTrench);
            var name = crucible.MapInfo.MapName;
            AssertWalkWithoutLava(crucible, name, Prometheus, NorthTrench, "the north trench marker from the Prometheus Outpost waypoint");
            foreach (var (pool, at) in O.JuggernautPools.Append((O.UgoretzPool, O.UgoretzAt)))
            {
                var place = ToVector(at);
                AssertOnGround(crucible, place, $"pool {pool}");
                Assert.IsGreaterThanOrEqualTo(5f, crucible.NavMesh.DistanceToWall(place, 20) ?? 0, $"pool {pool} 5 m or more from a wall");
                AssertWalkWithoutLava(crucible, name, NorthTrench, place, $"pool {pool} from the north trench marker");
            }
            Spawn(crucible, JuggernautPools.Append(O.UgoretzPool).ToArray());
            var juggernauts = Alive(crucible).Where(creature => creature.DbId == O.Juggernaut).ToList();
            Assert.HasCount(3 * 2, juggernauts, "two in each of the three pools");
            foreach (var juggernaut in juggernauts)
            {
                Assert.AreEqual((7722U, TargetCategory.Hostile), ((uint)juggernaut.EntityClass, juggernaut.TargetCategory));
                Assert.IsTrue(O.JuggernautPools.Any(pool => Vector3.Distance(ToVector(pool.At), juggernaut.Position) < 8), $"a Juggernaut at {juggernaut.Position} by its point");
            }
            var ugoretz = Alive(crucible).Single(creature => creature.DbId == O.Ugoretz);
            Assert.AreEqual((9317U, TargetCategory.Hostile), (ugoretz.NameId, ugoretz.TargetCategory));
            Assert.IsLessThan(3f, Vector3.Distance(ToVector(O.UgoretzAt), ugoretz.Position), "Ugoretz on his point");

            var terminal = ToVector(W.TerminalAt);
            AssertOnGround(crucible, terminal, "the terminal");
            AssertWalkWithoutLava(crucible, name, NorthTrench, terminal, "the terminal from the north trench marker");
            Assert.IsGreaterThanOrEqualTo(20f, harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == CrucibleMissionDataV1.MapContextId)
                .Where(row => harness.Creatures.LoadedCreatures.TryGetValue(row.Creature1Id, out var c) && c.TargetCategory == TargetCategory.Hostile)
                .Min(row => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new(terminal.X, terminal.Z)) - (float)row.Radius),
                "the terminal 20 m or more from every hostile pool's circle");
        }

        /// <summary>
        /// Iron Giants after Confirmed Kills: Madias offers it (not before); a Juggernaut destroyed before the accept does not
        /// count; five leave it open (a reload at 3 kept), the sixth completes it once, a seventh counts no more; its three
        /// markers on the pools; a full bag waits, a failed save pays nothing; Madias pays the item chosen, once, and Who's In
        /// Charge Here? follows.
        /// </summary>
        [TestMethod]
        public void IronGiantsCountsSixJuggernautsForMadias()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Giants), "it waits for Confirmed Kills");
            C.SeedCompleted(harness, Kills);
            crucible = C.EnterCrucible(harness, Prometheus);
            var offer = WildernessRanjaGorgeTests.Offer(harness, madias, Giants);
            CollectionAssert.AreEqual(new[] { I.CryogenicBlade, I.IncendiaryShotgun, I.RadialRepairTool, I.EmpRifle },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Charge), "Who's In Charge Here? waits for it");
            Spawn(crucible, JuggernautPools);
            KillJuggernaut(harness, crucible);
            Accept(harness, madias, Giants);
            Assert.AreEqual(0U, Count(harness, Giants), "a kill before the accept does not count");
            Assert.IsEmpty(Topics(harness, madias, Giants), "Madias hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Giants].ObjectivesList.Single().IndicatorList;
            CollectionAssert.AreEquivalent(O.JuggernautPools.Select(pool => ((float)pool.At.X, (float)pool.At.Z)).ToArray(),
                markers.Select(one => (one.Position.X, one.Position.Z)).ToArray(), "a marker on each Juggernaut pool");

            var completed = 0;
            for (var kill = 1U; kill <= ClientJuggernauts; kill++)
            {
                if (kill == 4)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(3U, Count(harness, Giants), "kept at a reload");
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Giants, I.Destroy), $"open before kill {kill}");
                completed += KillJuggernaut(harness, crucible).OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Giants);
                if (kill == ClientJuggernauts - 1)
                    Assert.AreEqual((ClientJuggernauts - 1, MissionObjectiveState.Incomplete), (Count(harness, Giants), Objective(harness, Giants, I.Destroy)), "5 of 6");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Giants, I.Destroy), "six");
            Assert.AreEqual(1, completed, "ObjectiveCompleted once");
            KillJuggernaut(harness, crucible);
            Assert.IsLessThanOrEqualTo(ClientJuggernauts, Count(harness, Giants), "a seventh counts no more");

            FullBagWaits(harness, madias, Giants, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, madias, Giants, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, madias, Giants, 1, I.Experience, I.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Giants), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, I.IncendiaryShotgun), "the shotgun chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Giants), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, madias, Charge), "Who's In Charge Here? follows");
        }

        /// <summary>An abandon of Iron Giants at two starts the count over; taken again, the count is 0 and a kill counts.</summary>
        [TestMethod]
        public void IronGiantsAbandonedStartsTheCountOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Kills);
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Spawn(crucible, JuggernautPools);
            Accept(harness, madias, Giants);
            KillJuggernaut(harness, crucible);
            KillJuggernaut(harness, crucible);
            Assert.AreEqual(2U, Count(harness, Giants));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Giants));
            harness.Drain();
            Accept(harness, madias, Giants);
            Assert.AreEqual(0U, Count(harness, Giants), "the count starts over");
            KillJuggernaut(harness, crucible);
            Assert.AreEqual(1U, Count(harness, Giants));
        }

        /// <summary>
        /// Who's In Charge Here? after Iron Giants: Madias offers it (not before) and gives the hacking program at the accept;
        /// the player's own terminal talks objective 1 only (2 is not open; a reload keeps it); the talk takes the program,
        /// opens 2 and puts the hacked terminal in its place, which talks 2 only; a reload in the middle of the scene keeps the
        /// hacked terminal (not the first); its talk makes the mission completeable once; Madias hears no objective, a full
        /// bag waits, a failed save pays nothing; Madias pays the item chosen, once; the terminal goes with the mission and The
        /// Big Kahuna follows.
        /// </summary>
        [TestMethod]
        public void WhosInChargeHereHacksTheTerminalAndReadsTheCommander()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Charge), "it waits for Iron Giants");
            C.SeedCompleted(harness, Kills, Giants);
            crucible = C.EnterCrucible(harness, Prometheus);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Charge && obj.IsInWorld), "no terminal before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, madias, Charge);
            CollectionAssert.AreEqual(new[] { W.ElectricInjectorGun, W.LaserRifle, W.CipherTool, W.RpgLauncher },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Kahuna), "The Big Kahuna waits for it");
            Accept(harness, madias, Charge);
            Assert.AreEqual(1U, Program(harness), "the hacking program at the accept");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (Objective(harness, Charge, W.Hack), Objective(harness, Charge, W.Use)));
            Assert.IsEmpty(Topics(harness, madias, Charge), "Madias hears no objective: the terminal does");

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual((1U, MissionObjectiveState.Incomplete), (Program(harness), Objective(harness, Charge, W.Hack)), "kept at a reload");
            var terminal = Terminal(harness, crucible, W.TerminalRole);
            Assert.IsNull(FindTerminal(harness, crucible, W.HackedRole), "no hacked terminal before the hack");
            AssertTalks(harness, terminal, W.Hack);
            TalkTo(harness, terminal, W.Hack);
            Assert.AreEqual(0U, Program(harness), "the program is uploaded");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Charge, W.Use), "the hack opens 2");
            Assert.IsFalse(harness.Client.Player.Missions[Charge].Completeable);
            Tick(harness, crucible, 250);
            Assert.IsNull(FindTerminal(harness, crucible, W.TerminalRole), "the first terminal goes");
            var hacked = Terminal(harness, crucible, W.HackedRole);
            AssertTalks(harness, hacked, W.Use);

            ReloadAndRecover(harness, crucible);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (Objective(harness, Charge, W.Hack), Objective(harness, Charge, W.Use)),
                "kept at a reload in the middle of the scene");
            Assert.IsNull(FindTerminal(harness, crucible, W.TerminalRole), "the first terminal stays gone");
            hacked = Terminal(harness, crucible, W.HackedRole);
            var read = TalkTo(harness, hacked, W.Use);
            Assert.AreEqual(1, read.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Charge && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsTrue(harness.Client.Player.Missions[Charge].Completeable);
            Assert.IsNull(OpenTerminal(harness, hacked), "read once");

            Assert.IsEmpty(Topics(harness, madias, Charge), "Madias hears no objective");
            FullBagWaits(harness, madias, Charge, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, madias, Charge, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, madias, Charge, 1, W.Experience, W.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Charge), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, W.LaserRifle), "the rifle chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, crucible, 250);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Charge && obj.IsInWorld), "the terminal goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Charge), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, madias, Kahuna), "The Big Kahuna follows");
        }

        /// <summary>An abandon after the hack takes the hacked terminal; taken again, a new program and the first terminal again.</summary>
        [TestMethod]
        public void WhosInChargeHereAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Kills, Giants);
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Accept(harness, madias, Charge);
            var terminal = Terminal(harness, crucible, W.TerminalRole);
            AssertTalks(harness, terminal, W.Hack);
            TalkTo(harness, terminal, W.Hack);
            Tick(harness, crucible, 250);
            Assert.IsNotNull(FindTerminal(harness, crucible, W.HackedRole));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Charge));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Charge && obj.IsInWorld), "the terminals go with the abandon");
            Assert.AreEqual(0U, Program(harness));
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, madias, Charge), "offered again after an abandon");
            Accept(harness, madias, Charge);
            Assert.AreEqual(1U, Program(harness), "a new program");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (Objective(harness, Charge, W.Hack), Objective(harness, Charge, W.Use)));
            AssertTalks(harness, Terminal(harness, crucible, W.TerminalRole), W.Hack);
            Assert.IsNull(FindTerminal(harness, crucible, W.HackedRole), "no hacked terminal");
        }

        /// <summary>
        /// As to-fix row 55 (1538's keycard), the program gone before the hack. A player cannot lose it: it is a mission item
        /// (AssignmentIssued), and a destroy of it is refused (MissionItemProtection), so it stays and the hack works later. An
        /// abandon with it held takes it; a new accept gives one program (not two) and the first terminal, and the hack works.
        /// </summary>
        [TestMethod]
        public void WhosInChargeHereWithTheProgramGoneRecoversByAnAbandon()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Kills, Giants);
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Accept(harness, madias, Charge);
            var program = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Single(item => item.ItemTemplate.ItemTemplateId == ClientProgramTemplate);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new Rasa.Packets.Inventory.Client.PersonalInventory_DestroyItemPacket { EntityId = program.EntityId, Quantity = program.StackSize });
            harness.Drain();
            Assert.AreEqual(1U, Program(harness), "a destroy of the mission's program is refused");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Charge));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.AreEqual(0U, Program(harness), "the abandon takes the program");
            Accept(harness, madias, Charge);
            Assert.AreEqual(1U, Program(harness), "a new program, one");
            var terminal = Terminal(harness, crucible, W.TerminalRole);
            AssertTalks(harness, terminal, W.Hack);
            TalkTo(harness, terminal, W.Hack);
            Tick(harness, crucible, 250);
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete), (Program(harness), Objective(harness, Charge, W.Use)), "taken again, the hack works");
            Assert.IsNotNull(FindTerminal(harness, crucible, W.HackedRole));
        }

        /// <summary>
        /// The Big Kahuna after Who's In Charge Here?: Madias offers it (not before); Ugoretz killed before the accept does not
        /// count; its marker on him; his kill completes it once (a reload before kept it open); a full bag waits, a failed save
        /// pays nothing; Madias pays the item chosen, once.
        /// </summary>
        [TestMethod]
        public void TheBigKahunaKillsUgoretzForMadias()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Kahuna), "it waits for Who's In Charge Here?");
            C.SeedCompleted(harness, Kills, Giants, Charge);
            crucible = C.EnterCrucible(harness, Prometheus);
            var offer = WildernessRanjaGorgeTests.Offer(harness, madias, Kahuna);
            CollectionAssert.AreEqual(new[] { B.IncendiaryBlade, B.RadialHealingDisc, B.LeechGun, B.LaserChaingun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Spawn(crucible, O.UgoretzPool);
            KillUgoretz(harness, crucible);
            Accept(harness, madias, Kahuna);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Kahuna, B.Kill), "a kill before the accept does not count");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Kahuna].ObjectivesList.Single().IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)O.UgoretzAt.X, (float)O.UgoretzAt.Z)), "on Ugoretz");

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Kahuna, B.Kill), "open at a reload");
            var completed = KillUgoretz(harness, crucible).OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Kahuna);
            Assert.AreEqual((1, MissionObjectiveState.Completed), (completed, Objective(harness, Kahuna, B.Kill)), "his kill, once");
            Assert.IsTrue(harness.Client.Player.Missions[Kahuna].Completeable);

            FullBagWaits(harness, madias, Kahuna, 3, ArmorFiller);
            FailedTurnInPaysNothing(harness, madias, Kahuna, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, madias, Kahuna, 3, B.Experience, B.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Kahuna), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.LaserChaingun), "the chaingun chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Kahuna), "done once");
        }

        /// <summary>An abandon of The Big Kahuna leaves nothing done; taken again, Ugoretz's kill completes it.</summary>
        [TestMethod]
        public void TheBigKahunaAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Kills, Giants, Charge);
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Spawn(crucible, O.UgoretzPool);
            Accept(harness, madias, Kahuna);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Kahuna));
            harness.Drain();
            Accept(harness, madias, Kahuna);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Kahuna, B.Kill), "open again");
            KillUgoretz(harness, crucible);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Kahuna, B.Kill));
        }

        /// <summary>A complete navmesh walk (from the nearest navmesh points), no point of it on the client's lava (LavaDamage.InLava).</summary>
        private static void AssertWalkWithoutLava(MapChannel map, string mapName, Vector3 from, Vector3 to, string what)
        {
            var start = map.NavMesh.Nearest(from) ?? from;
            var path = map.NavMesh.FindPath(start, map.NavMesh.Nearest(to) ?? to, out var complete);
            Assert.IsTrue(path != null && complete, $"{what}: a path");
            var points = path.Prepend(start).ToList();
            for (var i = 0; i + 1 < points.Count; i++)
                for (var step = 0; step <= 20; step++)
                {
                    var at = Vector3.Lerp(points[i], points[i + 1], step / 20f);
                    Assert.IsFalse(LavaDamage.InLava(mapName, at), FormattableString.Invariant($"{what}: lava at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})"));
                }
        }

        /// <summary>A Juggernaut destroyed (the pools back if none stands); the packets it sent.</summary>
        private static List<object> KillJuggernaut(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            if (!Alive(crucible).Any(candidate => candidate.DbId == O.Juggernaut))
                Respawn(crucible, JuggernautPools);
            Kill(harness, crucible, O.Juggernaut);
            return harness.Drain().Cast<object>().ToList();
        }

        /// <summary>Ugoretz killed (his pool back if he is dead); the packets it sent.</summary>
        private static List<object> KillUgoretz(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            if (!Alive(crucible).Any(candidate => candidate.DbId == O.Ugoretz))
                Respawn(crucible, O.UgoretzPool);
            Kill(harness, crucible, O.Ugoretz);
            return harness.Drain().Cast<object>().ToList();
        }

        private static uint Count(WildernessRuntimeTestHarness harness, uint mission) =>
            harness.Client.Player.Missions[mission].Objectives[1].Counters[0];

        private static uint Program(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientProgramTemplate);

        private static DynamicObject FindTerminal(WildernessRuntimeTestHarness harness, MapChannel crucible, string role)
        {
            harness.Manager.Scenes.Tick(crucible);
            return crucible.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == Charge && obj.SceneActorRole == role && obj.IsInWorld);
        }

        /// <summary>A terminal: the player's own, of the client's class and package, at its place, on the ground.</summary>
        private static DynamicObject Terminal(WildernessRuntimeTestHarness harness, MapChannel crucible, string role)
        {
            var terminal = FindTerminal(harness, crucible, role);
            Assert.IsNotNull(terminal, $"the {role}");
            Assert.AreEqual(ClientTerminalClass, (uint)terminal.EntityClassId);
            Assert.AreEqual(ToVector(W.TerminalAt), terminal.Position);
            Assert.AreEqual(ClientTerminalPackage, terminal.MissionConversation.NpcPackageId);
            Assert.AreEqual(harness.Client.Player.Id, terminal.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(crucible, terminal.Position, role);
            return terminal;
        }

        /// <summary>The terminal talks this objective, and only it.</summary>
        private static void AssertTalks(WildernessRuntimeTestHarness harness, DynamicObject terminal, uint objective)
        {
            var talk = OpenTerminal(harness, terminal);
            Assert.IsNotNull(talk, "the terminal talks");
            Assert.IsTrue(talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.AreEqual(((int)Charge, (int)objective, 1),
                ((List<CompleteableObjectives>)topics).Select(topic => (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId)).Single());
        }

        /// <summary>The terminal's objective done (the client opens its conversation first: AssertTalks); the packets it sent.</summary>
        private static List<object> TalkTo(WildernessRuntimeTestHarness harness, DynamicObject terminal, uint objective)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = terminal.EntityId, MissionId = Charge, ObjectiveId = objective, PlayerFlagId = 1
            });
            var read = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual(1, read.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Charge && packet.ObjectiveId == objective),
                $"{Charge}.{objective} ObjectiveCompleted once");
            return read;
        }

        /// <summary>The terminal's conversation, as CrucibleOutpostIntrepidTests.OpenTerminal.</summary>
        private static ConversePacket OpenTerminal(WildernessRuntimeTestHarness harness, DynamicObject terminal)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = terminal.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }
    }
}
