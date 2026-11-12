using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
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
    using F = MarshesClientFloorTests;
    using Y = CrucibleHybridArcNpcsV1;
    using D = CrucibleDarkGenesisV1;
    using M = CrucibleGeneMachineV1;
    using Dna = CrucibleDesignerGenesV1;
    using Gs = CrucibleGenomeSweetGenomeV1;
    using V = CrucibleStaalSigalsNpcsV1;

    /// <summary>
    /// Crucible set 13, the Thrax hybrid arc, played through as a player plays it: Special Agent Stark's Dark Genesis (1856: to
    /// Capriulo in the Staal nightclub, then to Zupa Eugin), Eugin's The Gene Machine (1857: Grupa Rigs lets the player search
    /// the Labbna salvage yard's eleven piles for a calibrator, a diagnostic unit and three power cells; four Silver Barb Ticks
    /// come), Designer Genes (1881: five DNA samples from the Thrax bosses) and Genome Sweet Genome (1899: the Khulago
    /// laboratory by its pad, Skeev fights or, to one who gave him the vaccine, offers the data; the terminal gives the Map of
    /// the Thrax Genome; Eugin, then Capriulo; the Thrax hybrid). With them the arc's places (as CrucibleGuardTests holds the
    /// Crucible's): the NPCs, the piles, Skeev, the terminal and the pads walked to with no climb the client fails and seen from
    /// all sides; the tunnel and the laboratory apart on the navmesh (upstream, pinned) and joined by the pads.
    ///
    /// Why these tests: Stark is new and Capriulo and Eugin moved; 1856 is level gated; 1857 has uses gated by a talk, a full
    /// bag at a pile, a tally of three, junk and an ambush; 1881 a drop from six bosses; 1899 a talk choice gated by a
    /// Wilderness flag, a talker that becomes a fighter, a talking terminal that swaps items, a zero reward that unlocks a race,
    /// and two new local pads. An offer before its gate, a use before its objective, two cells counted as three, a count lost at
    /// a reload, a sample or an item kept or lost wrongly at an abandon, the wrong Skeev text, or a turn-in that pays twice
    /// would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleHybridArcTests
    {
        private const uint Genesis = D.MissionId, Gene = M.MissionId, Designer = Dna.MissionId, Genome = Gs.MissionId;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as CrucibleGrupaChiefsTests): fills the consumables bag
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)

        /// <summary>The north trench of Khulago Base, where the walk to the tunnel's control room starts (probe 2026-11-08).</summary>
        internal static readonly Vector3 NorthTrench = new(388f, 166.71f, -26f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Stark, Capriulo and Eugin at their places, on the ground, walked to (Stark from the Outpost Intrepid waypoint, Capriulo
        /// and Eugin from the Infirmary waypoint and from each other) and back with no climb the client fails, seen from 16 of 16
        /// places 2 m round; the eleven piles walked to from the Labbna waypoint; the tunnel pad from the north trench of Khulago;
        /// Skeev and the terminal from the laboratory pad. The tunnel's control room and the laboratory are not joined by the
        /// navmesh (upstream; pinned: the pads join them).
        /// </summary>
        [TestMethod]
        public void TheArcsNpcsPadsAndPlacesStandWhereTheyAreWalkedTo()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var nav = crucible.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", crucible.MapInfo.MapName), meshes, 1);
            var stark = Npc(crucible, Y.Stark);
            var capriulo = Npc(crucible, Y.Capriulo);
            var eugin = Npc(crucible, Y.Eugin);
            foreach (var (npc, at, label) in new[] { (stark, Y.StarkAt, "Stark"), (capriulo, Y.CapriuloAt, "Capriulo"), (eugin, Y.EuginAt, "Eugin") })
            {
                Assert.AreEqual(ToVector(at).X, npc.Position.X, 0.01f, label);
                Assert.AreEqual(ToVector(at).Z, npc.Position.Z, 0.01f, label);
                AssertOnGround(crucible, npc.Position, label);
            }

            var tunnelPad = ToVector(Y.TunnelPadAt);
            var labPad = ToVector(Y.LabPadAt);
            // Both pads on the navmesh and on the client's floor (probe 2026-11-08: tunnel nav 139.88, floor 139.62; lab nav
            // 201.70, floor 201.63; no floor above either within 3 m).
            foreach (var (pad, label) in new[] { (tunnelPad, "the tunnel pad"), (labPad, "the lab pad") })
            {
                AssertOnGround(crucible, pad, label);
                var floor = F.FloorAt(F.Near(geometry, new List<Vector3> { pad }, 4)[0], pad, pad.Y - 1, pad.Y + 3);
                Assert.IsNotNull(floor, $"{label}: a client floor");
                Assert.AreEqual(pad.Y, floor.Value, 0.5f, $"{label} on the client floor");
            }
            var walks = new List<(string Label, Vector3 From, Vector3 To)>
            {
                ("Stark", C.OutpostWaypoint, stark.Position), ("Capriulo", C.InfirmaryWaypoint, capriulo.Position),
                ("Eugin", C.InfirmaryWaypoint, eugin.Position), ("Capriulo to Eugin", capriulo.Position, eugin.Position),
                ("the tunnel pad", NorthTrench, tunnelPad), ("Skeev", labPad, ToVector(Gs.SkeevAt)), ("the terminal", labPad, ToVector(Gs.TerminalAt))
            };
            for (var index = 0; index < M.Piles.Length; index++)
                walks.Add(($"pile {index + 1}", C.LabbnaWaypoint, ToVector(M.Piles[index].At)));
            // The ambush: each tick reaches the diagnostic unit's pile (the Labbna Quarter is its own navmesh island, upstream
            // row 3, so the ticks must stand on it) and walks back.
            for (var tick = 0; tick < M.TicksAt.Length; tick++)
                walks.Add(($"tick {tick + 1} to pile {M.AmbushPile + 1}", ToVector(M.TicksAt[tick]), ToVector(M.Piles[M.AmbushPile].At)));
            var failures = new List<string>();
            foreach (var (label, from, to) in walks)
            {
                var start = nav.Nearest(from) ?? from;
                var end = nav.Nearest(to) ?? to;
                var walk = nav.FindPath(start, end, out var complete);
                Assert.IsTrue(walk != null && complete, $"{label}: a walk there");
                var back = nav.FindPath(end, start, out var backComplete);
                Assert.IsTrue(back != null && backComplete, $"{label}: a walk back");
                foreach (var (way, points) in new[] { ("there", walk.Prepend(start).ToList()), ("back", back.Prepend(end).ToList()) })
                {
                    var (step, _, grade, _) = CrucibleGuardTests.ClientClimb(nav, geometry, points);
                    if (step > CrucibleGuardTests.MaxClientStep || grade > CrucibleGuardTests.MaxClientGrade)
                        failures.Add(FormattableString.Invariant($"{label} {way}: step {step:0.00}, grade {grade:0.00}"));
                }
            }
            // No lava 20 m round any of the arc's places (the NPCs, the pads, the piles, the ticks, Skeev, the terminal).
            var name = crucible.MapInfo.MapName;
            var places = new List<(string Label, Vector3 At)>
            {
                ("Stark", stark.Position), ("Capriulo", capriulo.Position), ("Eugin", eugin.Position), ("the tunnel pad", tunnelPad),
                ("the lab pad", labPad), ("Skeev", ToVector(Gs.SkeevAt)), ("the terminal", ToVector(Gs.TerminalAt))
            };
            places.AddRange(M.Piles.Select((pile, index) => ($"pile {index + 1}", ToVector(pile.At))));
            places.AddRange(M.TicksAt.Select((at, index) => ($"tick {index + 1}", ToVector(at))));
            foreach (var (label, at) in places)
            {
                if (LavaDamage.InLava(name, at))
                    failures.Add($"{label} on lava");
                for (var dx = -20f; dx <= 20; dx += 2)
                    for (var dz = -20f; dz <= 20; dz += 2)
                        if (dx * dx + dz * dz <= 400 && nav.Nearest(at + new Vector3(dx, 0, dz)) is { } ground && LavaDamage.InLava(name, ground))
                            failures.Add(FormattableString.Invariant($"lava at ({ground.X:0.0}, {ground.Y:0.0}, {ground.Z:0.0}) by {label}"));
            }

            var apart = nav.FindPath(nav.Nearest(tunnelPad) ?? tunnelPad, nav.Nearest(labPad) ?? labPad, out var joined);
            Assert.IsFalse(apart != null && joined, "the tunnel and the laboratory are apart on the navmesh: the pads join them");

            foreach (var (label, at, npc) in new[]
            {
                ("Stark", stark.Position, true), ("Capriulo", capriulo.Position, true), ("Eugin", eugin.Position, true),
                ("Skeev", ToVector(Gs.SkeevAt), true), ("the terminal", ToVector(Gs.TerminalAt), false)
            })
            {
                var near = F.Near(geometry, new List<Vector3> { at }, 8)[0];
                var aim = at + new Vector3(0, npc ? F.NpcMiddle : F.ObjectMiddle, 0);
                for (var k = 0; k < F.RingPlaces; k++)
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var stand = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    if (F.FloorAt(near, stand, at.Y - F.MaxOver, at.Y + F.MaxUnder) is not { } fy || !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                        failures.Add($"{label} not seen from ({stand.X:F1}, {stand.Z:F1})");
                }
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The laboratory pads: the tunnel's takes a player who steps onto it to the laboratory, and the laboratory's takes him
        /// back; neither sends him straight back before he steps off. Both Bane pads (class 8044), local teleporters, linked both ways.
        /// </summary>
        [TestMethod]
        public void ThePadsTakeThePlayerIntoTheLaboratoryAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, NorthTrench);
            LabPads(harness, crucible);
            Assert.IsTrue(crucible.Teleporters.TryGetValue(Y.TunnelPad, out var tunnel), "the tunnel pad");
            Assert.IsTrue(crucible.Teleporters.TryGetValue(Y.LabPad, out var lab), "the laboratory pad");

            Teleport(harness, crucible, tunnel);
            Assert.IsLessThan(3f, Vector3.Distance(ToVector(Y.LabPadAt), harness.Client.Player.Position), "in the laboratory");
            harness.Objects.DynamicObjectProximityWorker(crucible, lab, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back");

            harness.MoveTo(ToVector(Gs.SkeevAt) + new Vector3(10, 0, 0));
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(crucible, lab, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
            Teleport(harness, crucible, lab);
            Assert.IsLessThan(3f, Vector3.Distance(ToVector(Y.TunnelPadAt), harness.Client.Player.Position), "back in the tunnel");
        }

        /// <summary>
        /// Dark Genesis: Stark offers it from level 36 (not at 35), only he; Capriulo and Eugin offer nothing yet; Eugin hears
        /// nothing before Capriulo; Capriulo's talk opens 2, kept at a reload; Eugin's makes it completeable; a full bag waits, a
        /// failed save pays nothing; Eugin pays one of the four stacks, once; The Gene Machine follows at Eugin.
        /// </summary>
        [TestMethod]
        public void DarkGenesisSendsThePlayerFromStarkToCapriuloAndEugin()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(Y.StarkAt));
            var stark = Npc(crucible, Y.Stark);
            SetLevel(harness, 35);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, stark, Genesis), "not at level 35");
            SetLevel(harness, 36);
            var offer = WildernessRanjaGorgeTests.Offer(harness, stark, Genesis);
            CollectionAssert.AreEqual(new[] { D.MedPack, D.PowerCharger, D.CryogenicGrenade, D.IncendiaryGrenade },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            var capriulo = Npc(crucible, Y.Capriulo);
            var eugin = Npc(crucible, Y.Eugin);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, capriulo, Genesis), "only Stark offers it");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, eugin, Gene), "The Gene Machine waits for it");

            Accept(harness, stark, Genesis);
            Assert.IsEmpty(Topics(harness, stark, Genesis), "Stark hears no objective");
            Assert.IsEmpty(Topics(harness, eugin, Genesis), "Eugin waits for Capriulo");
            Talk(harness, capriulo, Genesis, D.LocateCapriulo);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Genesis, D.SpeakToEugin));
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Genesis, D.LocateCapriulo), Objective(harness, Genesis, D.SpeakToEugin)), "kept at a reload");
            Talk(harness, eugin, Genesis, D.SpeakToEugin);
            Assert.IsTrue(harness.Client.Player.Missions[Genesis].Completeable);

            FullBagWaits(harness, eugin, Genesis, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, eugin, Genesis, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, eugin, Genesis, 2, D.Experience, D.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Genesis), "MissionRewarded once");
            Assert.AreEqual(D.Grenades, WildernessRanjaGorgeTests.Held(harness, D.CryogenicGrenade), "the grenades chosen");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, eugin, Gene), "The Gene Machine follows");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, stark, Genesis), "done once");
        }

        /// <summary>An abandon of Dark Genesis after Capriulo's talk: taken again, 1 is open again and Eugin waits.</summary>
        [TestMethod]
        public void DarkGenesisAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(Y.StarkAt));
            SetLevel(harness, 40);
            var stark = Npc(crucible, Y.Stark);
            Accept(harness, stark, Genesis);
            Talk(harness, Npc(crucible, Y.Capriulo), Genesis, D.LocateCapriulo);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Genesis));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, stark, Genesis), "offered again after an abandon");
            Accept(harness, stark, Genesis);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Genesis, D.LocateCapriulo), Objective(harness, Genesis, D.SpeakToEugin)), "all over again");
            Assert.IsEmpty(Topics(harness, Npc(crucible, Y.Eugin), Genesis), "Eugin waits for Capriulo again");
        }

        /// <summary>
        /// The Gene Machine after Dark Genesis: Eugin offers it; the eleven piles are the player's own, of their classes and name,
        /// and take no use before Rigs' talk opens 1-3 (hidden 4 stays hidden); a full mission bag leaves the calibrator in its
        /// pile; the calibrator's pile completes 1, once; the unit's completes 2 and brings the four ticks (hostile, 20 m or more
        /// off, at the player); a junk pile gives nothing; two cells leave 3 open (n/3: 2), kept at a reload in the middle; the
        /// third completes 3; a full bag waits, a failed save pays nothing; Eugin takes the parts and pays once; Designer Genes
        /// follows.
        /// </summary>
        [TestMethod]
        public void TheGeneMachineSearchesTheSalvageYard()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Genesis);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var eugin = Npc(crucible, Y.Eugin);
            var rigs = Npc(crucible, M.Rigs);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Gene && obj.IsInWorld), "no pile before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, eugin, Gene);
            CollectionAssert.AreEqual(new[] { M.PulseRifle, M.SonicRpgLauncher, M.CryogenicNetGun, M.VirulentLeechGun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, rigs, Gene), "not Rigs' to give");
            Accept(harness, eugin, Gene);
            Tick(harness, crucible, 250);
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Gene].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            Assert.IsFalse(visible.Contains(M.CellsTally), "the cells' tally is hidden");

            var piles = Piles(harness, crucible);
            Use(harness, crucible, piles[0], M.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Gene, M.Calibrator), "no use before Rigs' talk");
            Assert.IsTrue(piles[0].IsEnabled, "the pile stays closed");
            Talk(harness, rigs, Gene, M.SpeakToRigs);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Gene, M.Calibrator), Objective(harness, Gene, M.DiagnosticUnit), Objective(harness, Gene, M.PowerCells)), "Rigs opens 1-3");

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Use(harness, crucible, piles[0], M.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, M.CalibratorTemplate), "no room");
            Assert.IsTrue(piles[0].IsEnabled, "the calibrator waits in its pile");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Gene, M.Calibrator));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, crucible, piles[0], M.UseArgument);
            Use(harness, crucible, piles[0], M.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, M.CalibratorTemplate), "the calibrator, once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Gene, M.Calibrator));
            Assert.IsFalse(piles[0].IsEnabled, "searched once");

            Assert.IsNull(SceneCreature(harness, crucible, Gene, M.TickRole(0)), "no tick before the unit's pile");
            Use(harness, crucible, piles[M.AmbushPile], M.UseArgument);
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (WildernessRanjaGorgeTests.Held(harness, M.DiagnosticUnitTemplate),
                Objective(harness, Gene, M.DiagnosticUnit)), "the unit");
            Tick(harness, crucible, 250);
            var ticks = Enumerable.Range(0, M.TicksAt.Length).Select(index => SceneCreature(harness, crucible, Gene, M.TickRole(index))).ToArray();
            Assert.IsTrue(ticks.All(tick => tick != null), "the four ticks");
            foreach (var tick in ticks)
            {
                Assert.AreEqual((M.Tick, TargetCategory.Hostile), (tick.DbId, tick.TargetCategory));
                Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(tick.Position, ToVector(M.Piles[M.AmbushPile].At)), "20 m or more off the pile");
            }
            foreach (var tick in ticks)
                Kill(harness, crucible, tick);

            Use(harness, crucible, piles[5], M.UseArgument);
            Assert.IsFalse(piles[5].IsEnabled, "a junk pile searched");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, M.PowerCellTemplate), "junk gives nothing");
            Assert.AreEqual(0U, Counter(harness, Gene, M.CellsTally), "junk is no cell");

            Use(harness, crucible, piles[2], M.UseArgument);
            Use(harness, crucible, piles[3], M.UseArgument);
            Assert.AreEqual((2U, 2U, MissionObjectiveState.Incomplete), (WildernessRanjaGorgeTests.Held(harness, M.PowerCellTemplate),
                Counter(harness, Gene, M.CellsTally), Objective(harness, Gene, M.PowerCells)), "two cells of three");
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Tick(harness, crucible, 250);
            Assert.AreEqual((2U, MissionObjectiveState.Incomplete), (Counter(harness, Gene, M.CellsTally), Objective(harness, Gene, M.PowerCells)),
                "kept at a reload in the middle of the scene");
            piles = Piles(harness, crucible);
            Assert.IsFalse(piles[2].IsEnabled || piles[3].IsEnabled, "searched piles stay searched");
            Use(harness, crucible, piles[4], M.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Gene, M.PowerCells), "three cells");
            Assert.IsTrue(harness.Client.Player.Missions[Gene].Completeable);

            FullBagWaits(harness, eugin, Gene, 3, WeaponFiller);
            FailedTurnInPaysNothing(harness, eugin, Gene, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, eugin, Gene, 3, M.Experience, M.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Gene), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, M.VirulentLeechGun), "the leech gun chosen");
            Assert.AreEqual((0U, 0U, 0U), (WildernessRanjaGorgeTests.Held(harness, M.CalibratorTemplate),
                WildernessRanjaGorgeTests.Held(harness, M.DiagnosticUnitTemplate), WildernessRanjaGorgeTests.Held(harness, M.PowerCellTemplate)),
                "Eugin takes the parts");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, eugin, Designer), "Designer Genes follows");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, eugin, Gene), "done once");
        }

        /// <summary>
        /// The Gene Machine out of order: the cells first, the last pile's cell before the others and a junk pile between, then the
        /// unit (the ticks) and the calibrator. The tally counts any three cells; the mission waits for all three parts.
        /// </summary>
        [TestMethod]
        public void TheGeneMachineCountsTheCellsInAnyOrder()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Genesis);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var eugin = Npc(crucible, Y.Eugin);
            var rigs = Npc(crucible, M.Rigs);
            Accept(harness, eugin, Gene);
            Tick(harness, crucible, 250);
            Talk(harness, rigs, Gene, M.SpeakToRigs);
            var piles = Piles(harness, crucible);

            Use(harness, crucible, piles[4], M.UseArgument);
            Assert.AreEqual((1U, 1U), (WildernessRanjaGorgeTests.Held(harness, M.PowerCellTemplate), Counter(harness, Gene, M.CellsTally)), "the last cell first");
            Use(harness, crucible, piles[8], M.UseArgument);
            Assert.AreEqual(1U, Counter(harness, Gene, M.CellsTally), "junk between counts nothing");
            Use(harness, crucible, piles[4], M.UseArgument);
            Assert.AreEqual(1U, Counter(harness, Gene, M.CellsTally), "a searched pile counts once");
            Use(harness, crucible, piles[3], M.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Gene, M.PowerCells), "two of three");
            Use(harness, crucible, piles[2], M.UseArgument);
            Assert.AreEqual((3U, 3U, MissionObjectiveState.Completed), (WildernessRanjaGorgeTests.Held(harness, M.PowerCellTemplate),
                Counter(harness, Gene, M.CellsTally), Objective(harness, Gene, M.PowerCells)), "three cells in any order");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Gene, M.Calibrator), Objective(harness, Gene, M.DiagnosticUnit)), "the parts still wait");
            Assert.IsFalse(harness.Client.Player.Missions[Gene].Completeable, "not before the parts");

            Use(harness, crucible, piles[M.AmbushPile], M.UseArgument);
            Tick(harness, crucible, 250);
            var ticks = Enumerable.Range(0, M.TicksAt.Length).Select(index => SceneCreature(harness, crucible, Gene, M.TickRole(index))).ToArray();
            Assert.IsTrue(ticks.All(tick => tick != null), "the four ticks after the cells too");
            foreach (var tick in ticks)
                Kill(harness, crucible, tick);
            Assert.IsFalse(harness.Client.Player.Missions[Gene].Completeable, "not before the calibrator");
            Use(harness, crucible, piles[0], M.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Gene, M.Calibrator));
            Assert.IsTrue(harness.Client.Player.Missions[Gene].Completeable, "all three parts");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, eugin, Gene, 0, M.Experience, M.Credits);
        }

        /// <summary>An abandon of The Gene Machine after the calibrator: the parts go; taken again, every pile is whole, 1-3 wait for Rigs again.</summary>
        [TestMethod]
        public void TheGeneMachineAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Genesis);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var eugin = Npc(crucible, Y.Eugin);
            Accept(harness, eugin, Gene);
            Tick(harness, crucible, 250);
            Talk(harness, Npc(crucible, M.Rigs), Gene, M.SpeakToRigs);
            Use(harness, crucible, Piles(harness, crucible)[0], M.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, M.CalibratorTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Gene));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, M.CalibratorTemplate), "the calibrator goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, eugin, Gene), "offered again after an abandon");
            Accept(harness, eugin, Gene);
            Tick(harness, crucible, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Gene, M.SpeakToRigs), Objective(harness, Gene, M.Calibrator)), "all over again");
            Assert.IsTrue(Piles(harness, crucible).All(pile => pile.IsEnabled), "every pile whole again");
        }

        /// <summary>
        /// Designer Genes after The Gene Machine: Eugin offers it; no sample before the mission; a Thrax soldier gives none; each
        /// boss kill gives one (the same boss again too), a death is not a pickup; an abandon keeps the samples and they count
        /// again; kept at a reload; the fifth completes it once and no sixth drops; a full bag waits, a failed save pays nothing;
        /// Eugin takes the five and pays once; Genome Sweet Genome follows.
        /// </summary>
        [TestMethod]
        public void DesignerGenesTakesFiveSamplesFromTheThraxBosses()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? Dna.SampleChance - 1 : minimum);
            C.SeedCompleted(harness, Genesis, Gene);
            var crucible = C.EnterCrucible(harness);
            var eugin = Npc(crucible, Y.Eugin);
            var bossPools = new[] { V.MolitorPool, CrucibleMinefieldAndSalvageNpcsV1.DergePool, CruciblePrometheusOutpostNpcsV1.UgoretzPool };
            Spawn(crucible, bossPools);
            var offer = WildernessRanjaGorgeTests.Offer(harness, eugin, Designer);
            CollectionAssert.AreEqual(new[] { Dna.GravitonGloves, Dna.MotorAssistVest, Dna.ReflectiveVest, Dna.HazmatVest },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(Holds(Kill(harness, crucible, V.Molitor), Dna.SampleTemplate), "no sample before the mission");
            Respawn(crucible, bossPools);
            Accept(harness, eugin, Designer);
            Assert.IsEmpty(Topics(harness, eugin, Designer), "Eugin hears no objective");

            Assert.IsFalse(Holds(Kill(harness, crucible, V.ThraxSoldier), Dna.SampleTemplate), "a Thrax soldier has none");
            var sample = Kill(harness, crucible, V.Molitor);
            Assert.IsTrue(Holds(sample, Dna.SampleTemplate), "Molitor's");
            Assert.AreEqual(0U, Samples(harness), "a death is not a pickup");
            PickUp(harness, sample);
            Assert.AreEqual(1U, Samples(harness));
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Designer));
            harness.Drain();
            Assert.AreEqual(1U, Samples(harness), "the sample is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, eugin, Designer), "offered again after an abandon");
            Accept(harness, eugin, Designer);
            harness.Drain();

            var completed = 0;
            var completeable = 0;
            foreach (var boss in new[] { CrucibleMinefieldAndSalvageNpcsV1.Derge, CruciblePrometheusOutpostNpcsV1.Ugoretz, V.Molitor, V.Molitor })
            {
                if (Samples(harness) == 3)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(3U, Samples(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Designer, Dna.Samples));
                    harness.Drain();
                }
                if (!Alive(crucible).Any(candidate => candidate.DbId == boss && candidate.Npc == null))
                    Respawn(crucible, bossPools);
                var loot = Kill(harness, crucible, boss);
                Assert.IsTrue(Holds(loot, Dna.SampleTemplate), $"{boss} gives a sample");
                PickUp(harness, loot);
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Designer);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Designer && packet.IsCompleteable);
            }
            Assert.AreEqual((Dna.SampleCount, MissionObjectiveState.Completed), (Samples(harness), Objective(harness, Designer, Dna.Samples)), "five");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted once, MissionCompleteable once");
            Respawn(crucible, bossPools);
            Assert.IsFalse(Holds(Kill(harness, crucible, CrucibleMinefieldAndSalvageNpcsV1.Derge), Dna.SampleTemplate), "no sixth");

            FullBagWaits(harness, eugin, Designer, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, eugin, Designer, 1);
            Assert.AreEqual(Dna.SampleCount, Samples(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, eugin, Designer, 1, Dna.Experience, Dna.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Designer), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Dna.MotorAssistVest), "the vest chosen");
            Assert.AreEqual(0U, Samples(harness), "Eugin takes the samples");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, eugin, Genome), "Genome Sweet Genome follows");
        }

        /// <summary>
        /// Genome Sweet Genome to one who did not give Skeev the vaccine: the program at the accept; Skeev (the player's own,
        /// neutral) has only his battle text (variant 1); the terminal hears nothing before 5; battle opens 4, the talker goes
        /// and the hostile Skeev comes at the player; a reload mid-fight keeps 4 open; his death opens 5; the terminal takes the
        /// program and gives the map; Eugin takes the map and opens 3; Capriulo's talk makes it completeable; a full bag does
        /// not hold it (no item), a failed save pays nothing; Capriulo pays once (no XP, no credits) and the account has the
        /// Thrax hybrid.
        /// </summary>
        [TestMethod]
        public void GenomeSweetGenomeSkeevFightsForTheMap()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Genesis, Gene, Designer);
            var crucible = C.EnterCrucible(harness);
            var eugin = Npc(crucible, Y.Eugin);
            Assert.AreEqual(0U, Program(harness), "no program before the mission");
            Accept(harness, eugin, Genome);
            Tick(harness, crucible, 250);
            Assert.AreEqual(1U, Program(harness), "the program at the accept");
            harness.MoveTo(ToVector(Gs.SkeevAt) + new Vector3(4, 0, 0));
            harness.Drain();
            Tick(harness, crucible, 250);

            var talker = SceneCreature(harness, crucible, Genome, Gs.TalkerRole);
            Assert.IsNotNull(talker, "Skeev, the player's own");
            Assert.AreEqual((Y.SkeevTalker, Y.SkeevPackage), (talker.DbId, talker.Npc?.NpcPackageId));
            Assert.AreNotEqual(TargetCategory.Hostile, talker.TargetCategory, "the talker is no foe");
            Assert.IsEmpty(Choices(harness, talker, Genome), "no vaccine choice");
            Assert.AreEqual(Gs.IgnoredVariant, Variant(harness, talker), "only the battle text (variant 1)");
            var terminal = SceneObject(harness, crucible, Genome, Gs.TerminalRole);
            Assert.AreEqual(Gs.TerminalClass, (uint)terminal.EntityClassId);

            Talk(harness, talker, Genome, Gs.GetMap);
            Tick(harness, crucible, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Genome, Gs.GetMap), Objective(harness, Genome, Gs.KillSkeev), Objective(harness, Genome, Gs.Download)), "battle");
            Assert.IsNull(SceneCreature(harness, crucible, Genome, Gs.TalkerRole), "the talker goes");
            var fighter = SceneCreature(harness, crucible, Genome, Gs.FighterRole);
            Assert.IsNotNull(fighter, "Skeev fights");
            Assert.AreEqual((Y.SkeevFighter, TargetCategory.Hostile), (fighter.DbId, fighter.TargetCategory));

            ReloadAndRecover(harness, crucible);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Genome, Gs.KillSkeev), "kept at a reload mid-fight");
            fighter = SceneCreature(harness, crucible, Genome, Gs.FighterRole);
            Assert.IsNotNull(fighter, "Skeev still fights after the reload");
            Assert.IsNull(SceneCreature(harness, crucible, Genome, Gs.TalkerRole), "and the talker stays gone");
            Kill(harness, crucible, fighter);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Genome, Gs.KillSkeev), Objective(harness, Genome, Gs.Download)), "Skeev dead opens 5");

            DownloadAndReturn(harness, crucible, eugin);
        }

        /// <summary>
        /// Genome Sweet Genome to one who gave Skeev the vaccine (Daghda's Urn flag 26): Skeev has his other text (variant 27,
        /// two choices); accept opens 5 with no fight (4 stays closed, no hostile Skeev); the rest as the battle. Then an abandon
        /// takes the map and the program; taken again, refuse (27's choice 1) brings the hostile Skeev.
        /// </summary>
        [TestMethod]
        public void GenomeSweetGenomeSkeevOffersTheDataToWhoGaveHimTheVaccine()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Genesis, Gene, Designer);
            SetFlag(harness, Gs.VaccineFlag, Gs.Surrendered);
            var crucible = C.EnterCrucible(harness);
            var eugin = Npc(crucible, Y.Eugin);
            Accept(harness, eugin, Genome);
            harness.MoveTo(ToVector(Gs.SkeevAt) + new Vector3(4, 0, 0));
            harness.Drain();
            Tick(harness, crucible, 250);
            var talker = SceneCreature(harness, crucible, Genome, Gs.TalkerRole);
            CollectionAssert.AreEqual(new[] { (Gs.GetMap, Gs.HelpedVariant) }, Choices(harness, talker, Genome), "the vaccine text");
            Choose(harness, talker, Genome, Gs.GetMap, Gs.HelpedVariant, 2);
            Tick(harness, crucible, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete),
                (Objective(harness, Genome, Gs.GetMap), Objective(harness, Genome, Gs.KillSkeev), Objective(harness, Genome, Gs.Download)), "accepted");
            Assert.IsNull(SceneCreature(harness, crucible, Genome, Gs.FighterRole), "no fight");

            var terminal = SceneObject(harness, crucible, Genome, Gs.TerminalRole);
            TalkToTerminal(harness, crucible, terminal);
            Assert.AreEqual((0U, 1U), (Program(harness), Map(harness)), "the map for the program");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Genome));
            harness.Drain();
            Assert.AreEqual((0U, 0U), (Program(harness), Map(harness)), "an abandon takes them");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, crucible, 250);
            Assert.IsNull(SceneCreature(harness, crucible, Genome, Gs.TalkerRole), "the talker goes with the abandon");

            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, eugin, Genome), "offered again after an abandon");
            Accept(harness, eugin, Genome);
            Assert.AreEqual(1U, Program(harness), "a new program");
            harness.MoveTo(ToVector(Gs.SkeevAt) + new Vector3(4, 0, 0));
            harness.Drain();
            Tick(harness, crucible, 250);
            talker = SceneCreature(harness, crucible, Genome, Gs.TalkerRole);
            Choose(harness, talker, Genome, Gs.GetMap, Gs.HelpedVariant, 1);
            Tick(harness, crucible, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Genome, Gs.KillSkeev), "refused: battle");
            var fighter = SceneCreature(harness, crucible, Genome, Gs.FighterRole);
            Assert.IsNotNull(fighter, "Skeev fights");
            Kill(harness, crucible, fighter);
            DownloadAndReturn(harness, crucible, eugin);
        }

        /// <summary>5, 2 and 3 of Genome Sweet Genome, and the turn-in at Capriulo: the terminal, Eugin, Capriulo; no XP, no credits; the Thrax hybrid.</summary>
        private static void DownloadAndReturn(WildernessRuntimeTestHarness harness, MapChannel crucible, Creature eugin)
        {
            Assert.IsEmpty(Topics(harness, eugin, Genome), "Eugin waits for the map");
            var terminal = SceneObject(harness, crucible, Genome, Gs.TerminalRole);
            TalkToTerminal(harness, crucible, terminal);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Genome, Gs.Download), Objective(harness, Genome, Gs.ReturnToEugin)), "downloaded");
            Assert.AreEqual((0U, 1U), (Program(harness), Map(harness)), "the map for the program");

            Talk(harness, eugin, Genome, Gs.ReturnToEugin);
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete), (Map(harness), Objective(harness, Genome, Gs.ReturnToCapriulo)), "Eugin takes the map");
            var capriulo = Npc(crucible, Y.Capriulo);
            Talk(harness, capriulo, Genome, Gs.ReturnToCapriulo);
            Assert.IsTrue(harness.Client.Player.Missions[Genome].Completeable);

            WildernessAliaBranchesTests.FillCategory(harness, WeaponFiller, 50);
            FailedTurnInPaysNothing(harness, capriulo, Genome, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, capriulo, Genome, null, 0, 0);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Genome), "MissionRewarded once");
            Assert.IsNotNull(harness.Client.AccountEntry, "the account");
            Assert.AreNotEqual(0, harness.Client.AccountEntry.HybridUnlocks & HybridUnlocks.Bit(Race.Thrax), "the Thrax hybrid unlocked");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, eugin, Genome), "done once");
        }

        /// <summary>The eleven piles of the player's own, on the ground, of their classes and name.</summary>
        private static DynamicObject[] Piles(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            harness.Manager.Scenes.Tick(crucible);
            var piles = new DynamicObject[M.Piles.Length];
            for (var index = 0; index < piles.Length; index++)
            {
                var pile = SceneObject(harness, crucible, Gene, M.PileRole(index));
                Assert.AreEqual((M.Piles[index].Class, M.PileName), ((uint)pile.EntityClassId, pile.NameOverrideId), $"pile {index + 1}");
                Assert.AreEqual(ToVector(M.Piles[index].At), pile.Position);
                Assert.AreEqual(harness.Client.Player.Id, pile.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(crucible, pile.Position, $"pile {index + 1}");
                piles[index] = pile;
            }
            return piles;
        }

        /// <summary>The terminal's talk of 5 (its scene conversation), from beside it.</summary>
        private static void TalkToTerminal(WildernessRuntimeTestHarness harness, MapChannel crucible, DynamicObject terminal)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, terminal.EntityId, Genome, Gs.Download, 1), "the terminal");
            harness.Drain();
            Tick(harness, crucible, 250);
        }

        /// <summary>The choice <paramref name="choice"/> of the topic (<paramref name="objective"/>, <paramref name="flag"/>) at <paramref name="npc"/>.</summary>
        private static void Choose(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective, uint flag, uint choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position + new Vector3(2, 0, 0));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = mission, ObjectiveId = objective, PlayerFlagId = flag, ChoiceIdx = (int)choice
            });
            harness.Drain();
        }

        /// <summary>The choice topics (objective, variant) of <paramref name="mission"/> that <paramref name="npc"/> offers now.</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position + new Vector3(2, 0, 0));
            harness.Drain();
            return Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : Array.Empty<(uint, uint)>();
        }

        /// <summary>The variant of Skeev's completion topic of 7 (as CrucibleAwolCampTests.MelchingVariant).</summary>
        private static uint Variant(WildernessRuntimeTestHarness harness, Creature npc)
        {
            harness.MoveTo(npc.Position + new Vector3(2, 0, 0));
            harness.Drain();
            Assert.IsTrue(Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value), "Skeev has a topic");
            var topic = ((List<CompleteableObjectives>)value).Single(topic => topic.MissionId == Genome);
            Assert.AreEqual(Gs.GetMap, (uint)topic.ObjectiveId);
            return (uint)topic.PlayerFlagId;
        }

        /// <summary>A mission flag saved with the character (as PlateauCampResistanceScroungerTests.SetFlag), with its value.</summary>
        private static void SetFlag(WildernessRuntimeTestHarness harness, uint flag, uint value)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, flag, value));
            harness.Client.Player.PlayerFlags[flag] = value;
        }

        /// <summary>The character's level, saved and in play (as MissionRequirementProductionTests.SetLevel).</summary>
        private static void SetLevel(WildernessRuntimeTestHarness harness, byte level)
        {
            using (var unit = harness.CreateChar())
                unit.Characters.UpdateCharacterProgression(harness.Client.Player.Id, harness.Client.Player.Experience, level);
            harness.Client.Player.Level = level;
            harness.Drain();
        }

        /// <summary>The laboratory pads as DynamicObjectManager loads them (as CrucibleGrupaChiefsTests.OfficePads).</summary>
        private static void LabPads(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.Id == Y.TunnelPad || row.Id == Y.LabPad).ToList();
            Assert.HasCount(2, rows);
            foreach (var row in rows)
            {
                Assert.AreEqual((CrucibleMissionDataV1.MapContextId, Y.PadClass, Y.LocalTeleporter), (row.MapContextId, row.ClassId, (uint)row.Type), $"{row.Id}");
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = crucible,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                crucible.Teleporters.TryAdd(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(crucible, 0);
        }

        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        private static uint Samples(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, Dna.SampleTemplate);
        private static uint Program(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, Gs.ProgramTemplate);
        private static uint Map(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, Gs.MapTemplate);
    }
}
