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
using Rasa.Missions.Content.Marshes;
using Rasa.Packets;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using E = MarshesEscortsTests;
    using F = MarshesClientFloorTests;
    using Codes = AshenCycledFrequenciesV1;
    using Radar = AshenOnTheSameFrequencyV1;
    using Fix = AshenFixEV1;
    using King = AshenKingOfTheHillV1;
    using D = MarshesDefenceScene;

    /// <summary>
    /// Ashen Desert set 9, Stonewall Ridge, played through as a player plays it: Captain Marcus Emmons' Cycled Frequencies (1275:
    /// the Bane codes from a dropship's salvage in the Boneyard), On The Same Frequency (1277: the four radar towers' consoles,
    /// on their second floors through the towers' teleporters, with ambushes at three) and King of the Hill (1278: up to the
    /// four Anti-Air Batteries within three minutes, then four waves on them and a three minute hold). With them the walks on
    /// the client's floor from the Stonewall Ridge waypoint (197) to Emmons, the towers, the consoles, the batteries and every
    /// place a wave or an ambush comes from, and from the Ashoka Settlement waypoint to the salvage.
    ///
    /// Why these tests: the three are a chain, so an offer out of turn opens the next to a player it is not for; 1275 and 1277
    /// are personal objects each used once, 1278 a defence of four of the run's own turrets that stand in for the public ones
    /// (hidden from the player while they stand) and that count as lost one by one (the client's "Towers Lost"), with a deadline on its first
    /// objective. A use that counts twice or for the wrong tower, an ambush that does not come or comes at the wrong tower, a
    /// wave out of time, a loss not counted or failing too soon, a public turret left hidden or shown twice, progress lost at a
    /// reload, a turn-in that pays twice or on a full bag, or a place the client's floor does not reach would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenStonewallRidgeTests
    {
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ThraxSoldier = 531008;          // Thrax Soldier - Ashen Desert (World DB)

        private static readonly Vector3 Stonewall = AshenLeadInsTests.StonewallWaypoint;
        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;

        /// <summary>The Boneyard wreck the salvage lies by: the client static BaneVehicleDropshipV01Destroyed (mapfile.py, research).</summary>
        private static readonly Vector3 Wreck = new(537f, 282f, -279f);

        /// <summary>The four towers' ground and second floor teleporters (World DB teleporter rows, linked in local_teleporter_link).</summary>
        private static readonly (uint Ground, uint Upper)[] TowerPads = { (539, 823), (538, 824), (537, 825), (540, 826) };

        /// <summary>The four batteries' public AFS Turret pools (World DB spawnpool rows).</summary>
        private static readonly uint[] BatteryPools = { 550001, 550002, 550003, 550004 };

        /// <summary>
        /// The ground at each battery's foot (the turret platforms are navmesh islands of their own), on the navmesh joined to the
        /// waypoint and on the client's floor (the probe of 2026-11-10).
        /// </summary>
        private static readonly Vector3[] BatteryFeet =
        {
            new(60.40f, 348.65f, -485.60f), new(88.19f, 345.96f, -516.37f), new(114.00f, 346.64f, -543.36f), new(138.87f, 345.75f, -566.20f)
        };

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Emmons offers the chain in turn: Cycled Frequencies first; On The Same Frequency not before it is done (nor while it
        /// is open), King of the Hill not before On The Same Frequency is done; a reload keeps the offers.
        /// </summary>
        [TestMethod]
        public void EmmonsOffersTheChainInTurn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, Codes.MissionId), "Cycled Frequencies needs nothing");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, Radar.MissionId), "not before Cycled Frequencies");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "not before On The Same Frequency");
            Accept(harness, emmons, Codes.MissionId);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, Radar.MissionId), "not while Cycled Frequencies is open");

            AshenRuntime.SeedCompleted(harness, Codes.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, Radar.MissionId), "Emmons offers On The Same Frequency");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "King of the Hill waits for it");
            AshenRuntime.SeedCompleted(harness, Radar.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "Emmons offers King of the Hill");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "after a reload too");
        }

        /// <summary>
        /// Cycled Frequencies: Emmons offers it with TaRapedia's four weapons; no crate before it. Accepted, Emmons hears no
        /// objective; a marker on the crate, on Ashen Desert. The crate is the player's own Bane crate (7904, "Bane Dropship
        /// Salvage" 66) by the Boneyard wreck, on the ground, closed. A full bag leaves the codes in it; one slot free and its
        /// use gives the codes once and completes 1 once (MissionCompleteable once); a second use gives nothing; a reload keeps
        /// them. A full bag waits at the turn-in, a failed save pays nothing; Emmons pays the rifle chosen, once, and takes the
        /// codes; then he offers On The Same Frequency.
        /// </summary>
        [TestMethod]
        public void CycledFrequenciesTakesTheCodesFromTheDropshipSalvage()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Codes.MissionId), "no crate before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, emmons, Codes.MissionId);
            CollectionAssert.AreEqual(new uint[] { 14658, 48712, 49172, 15025 }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                "Sonic Rifle, Endothermic Polarity Gun, Sonic RPG Launcher, Pulse Shotgun");
            Accept(harness, emmons, Codes.MissionId);
            Assert.IsEmpty(Topics(harness, emmons, Codes.MissionId), "Emmons hears no objective");

            var crate = Crate(harness, desert);
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Codes.MissionId].ObjectivesList.Single().IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            Assert.IsLessThan(1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(crate.Position.X, crate.Position.Z)), "the marker on the crate");

            var log = new List<PythonPacket>();
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Search(harness, desert, crate, log);
            Assert.AreEqual(0U, CodesHeld(harness), "no room");
            Assert.IsTrue(crate.IsEnabled, "the crate waits");
            Assert.AreEqual((UseObjectState)Codes.ClosedState, crate.StateId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Codes.MissionId, Codes.Recover));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Search(harness, desert, crate, log);
            Assert.AreEqual(1U, CodesHeld(harness), "the codes");
            Assert.AreEqual((UseObjectState)Codes.OpenedState, crate.StateId);
            Assert.IsFalse(crate.IsEnabled, "searched once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Codes.MissionId, Codes.Recover));
            TrySearch(harness, desert, crate);
            Assert.AreEqual(1U, CodesHeld(harness), "a second use gives nothing");
            log.AddRange(harness.Drain());
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Codes.MissionId), "ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Codes.MissionId && packet.IsCompleteable),
                "MissionCompleteable once");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.AreEqual(1U, CodesHeld(harness), "kept at a reload");
            Assert.IsTrue(harness.Client.Player.Missions[Codes.MissionId].Completeable, "completeable after a reload");

            FullBagWaits(harness, emmons, Codes.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, emmons, Codes.MissionId, 0);
            Assert.AreEqual(1U, CodesHeld(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, emmons, Codes.MissionId, 0, 95000, 6300);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Codes.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, 14658), "the Sonic Rifle chosen");
            Assert.AreEqual(0U, CodesHeld(harness), "Emmons takes the codes");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, Codes.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, Radar.MissionId), "then On The Same Frequency");
        }

        /// <summary>An abandon takes the codes and the crate; taken again, a closed crate and no codes counted.</summary>
        [TestMethod]
        public void CycledFrequenciesAbandonedTakesTheCodesAndTheCrate()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            Accept(harness, emmons, Codes.MissionId);
            Search(harness, desert, Crate(harness, desert));
            Assert.AreEqual(1U, CodesHeld(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Codes.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, CodesHeld(harness), "the codes go");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Codes.MissionId && obj.IsInWorld), "the crate goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, Codes.MissionId), "offered again after an abandon");
            Accept(harness, emmons, Codes.MissionId);
            var crate = Crate(harness, desert);
            Assert.IsTrue(crate.IsEnabled, "a closed crate");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Codes.MissionId, Codes.Recover));
        }

        /// <summary>
        /// On The Same Frequency: Emmons offers it with TaRapedia's four weapons. Accepted, a marker on each tower's ground
        /// teleporter, on Ashen Desert; a console of the player's own on each tower's second floor, by its upper teleporter and
        /// on that floor's navmesh, off. Used in the order 3, 1, 4, 2: each use completes its own tower's objective only, once,
        /// and turns the console on; a second use does nothing; at towers 2-4 three Thrax Soldiers come at the tower's foot and
        /// attack the player, at tower 1 none; a reload halfway keeps what is done. The fourth makes it completeable once; a full
        /// bag waits, a failed save pays nothing; Emmons pays the blade chosen, once; then he offers King of the Hill.
        /// </summary>
        [TestMethod]
        public void OnTheSameFrequencyProgramsTheFourConsolesInAnyOrder()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Codes.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            var offer = WildernessRanjaGorgeTests.Offer(harness, emmons, Radar.MissionId);
            CollectionAssert.AreEqual(new uint[] { 12101, 11953, 48300, 14409 }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                "Cryogenic Blade, Electric Rifle, Chaingun, Pistol");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Radar.MissionId), "no console before the mission");
            Accept(harness, emmons, Radar.MissionId);
            Assert.IsEmpty(Topics(harness, emmons, Radar.MissionId), "Emmons hears no objective");

            var pads = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList()
                .ToDictionary(row => row.Id, row => new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ));
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Radar.MissionId].ObjectivesList;
            Assert.HasCount(4, objectives, "the four towers");
            var consoles = Consoles(harness, desert);
            for (var tower = 0; tower < 4; tower++)
            {
                var (ground, upper) = TowerPads[tower];
                Assert.AreEqual(1, Links(harness, ground, upper), $"tower {tower + 1}: {ground} goes to {upper}");
                Assert.AreEqual(1, Links(harness, upper, ground), $"tower {tower + 1}: {upper} back to {ground}");
                var marker = objectives.Single(entry => entry.ObjectiveId == tower + 1).IndicatorList.Single();
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(pads[ground].X, pads[ground].Z)),
                    $"tower {tower + 1}'s marker at its ground teleporter {ground}");
                var console = consoles[tower];
                var off = Vector2.Distance(new(console.Position.X, console.Position.Z), new(pads[upper].X, pads[upper].Z));
                Assert.IsTrue(off is >= 5f and <= 6f, $"console {tower + 1} {off:0.0} m off its upper teleporter {upper} (fix E)");
                Assert.IsLessThan(1f, MathF.Abs(console.Position.Y - pads[upper].Y), $"console {tower + 1} on the second floor");
                var nav = desert.NavMesh;
                Assert.IsTrue(nav.FindPath(nav.Nearest(pads[upper]).Value, nav.Nearest(console.Position).Value, out var walked) != null && walked,
                    $"console {tower + 1} is walked to from its upper teleporter");
            }

            var log = new List<PythonPacket>();
            var order = new[] { 2, 0, 3, 1 };
            for (var step = 0; step < order.Length; step++)
            {
                var tower = order[step];
                if (step == 2)
                {
                    AshenRuntime.Reload(harness);
                    harness.Drain();
                    E.Tick(harness, desert, 250);
                    consoles = Consoles(harness, desert);
                    Assert.IsFalse(consoles[order[0]].IsEnabled, "a programmed console stays programmed after a reload");
                    Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed), (Objective(harness, Radar.MissionId, 3),
                        Objective(harness, Radar.MissionId, 1)), "kept at a reload");
                }
                Assert.IsTrue(Ambushers(harness, desert, tower).All(creature => creature == null), $"no ambush at tower {tower + 1} before its console");
                Program(harness, desert, consoles[tower], log);
                Assert.AreEqual((UseObjectState)Radar.OnState, consoles[tower].StateId, $"console {tower + 1} on");
                Assert.IsFalse(consoles[tower].IsEnabled, $"console {tower + 1} used once");
                for (var other = 0; other < 4; other++)
                    Assert.AreEqual(order.Take(step + 1).Contains(other) ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                        Objective(harness, Radar.MissionId, (uint)other + 1), $"after tower {tower + 1}: tower {other + 1}");
                E.Tick(harness, desert, 250);
                var ambush = Ambushers(harness, desert, tower);
                if (tower == 0)
                    Assert.IsTrue(ambush.All(creature => creature == null), "no ambush at Emmons' tower");
                else
                {
                    Assert.HasCount(3, ambush);
                    foreach (var thrax in ambush)
                    {
                        Assert.IsNotNull(thrax, $"tower {tower + 1}: three Thrax Soldiers");
                        Assert.AreEqual((ThraxSoldier, TargetCategory.Hostile), (thrax.DbId, thrax.TargetCategory));
                        Assert.IsTrue(thrax.SceneAttackPastLeash, $"tower {tower + 1}: sent at the player");
                        AssertOnGround(desert, thrax.Position, $"tower {tower + 1}'s ambusher");
                        // Fix E (to-fix row 14): on the top floor, where the player is, out of the upper pad's 2 m trigger.
                        var upper = pads[TowerPads[tower].Upper];
                        var off = Vector2.Distance(new(thrax.Position.X, thrax.Position.Z), new(upper.X, upper.Z));
                        Assert.IsTrue(off is >= 3f and <= 5f, $"tower {tower + 1}'s ambusher {off:0.0} m from its upper teleporter");
                        Assert.IsLessThan(1.5f, MathF.Abs(thrax.Position.Y - upper.Y), $"tower {tower + 1}'s ambusher on the top floor");
                        Assert.AreEqual(harness.Client.Player.EntityId, Target(thrax), $"tower {tower + 1}'s ambusher fights the player");
                    }
                    foreach (var thrax in ambush)
                        E.Die(harness, desert, thrax);
                }
                TryProgram(harness, desert, consoles[tower]);
                log.AddRange(harness.Drain());
            }
            for (var tower = 1; tower <= 4; tower++)
                Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Radar.MissionId && packet.ObjectiveId == tower),
                    $"tower {tower}: ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Radar.MissionId && packet.IsCompleteable),
                "MissionCompleteable once");

            FullBagWaits(harness, emmons, Radar.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, emmons, Radar.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, emmons, Radar.MissionId, 0, 190000, 8400);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Radar.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, 12101), "the Cryogenic Blade chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, Radar.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "then King of the Hill");
        }

        /// <summary>
        /// Fix E (to-fix row 13): a console is used only from its own floor. From the ground floor straight under console 2 (the
        /// ground teleporter's height, 13.5 m under, within the 20 m use reach) the use is refused, "out of range", and nothing
        /// comes of it: the objective stays open, the console off, no ambush; 3.5 m over the floor too. On the top floor by it,
        /// the use completes 2. The limit is the consoles' own (opt-in): 1275's crate is still used from 10 m over it.
        /// </summary>
        [TestMethod]
        public void OnTheSameFrequencysConsolesAreUsedOnlyFromTheirFloor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Codes.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            Accept(harness, emmons, Radar.MissionId);
            var console = Consoles(harness, desert)[1];
            var ground = harness.World.TeleporterEntries.AsNoTracking().Single(row => row.Id == TowerPads[1].Ground);
            foreach (var (y, where) in new[] { ((float)ground.PosY, "the ground floor"), (console.Position.Y + 3.5f, "3.5 m over the floor") })
            {
                harness.MoveTo(console.Position with { Y = y, Z = console.Position.Z + 1 });
                harness.Drain();
                var count = desert.PerformRecovery.Count;
                harness.Objects.RequestUseObjectPacket(harness.Client, new Rasa.Packets.MapChannel.Client.RequestUseObjectPacket
                {
                    EntityId = console.EntityId, ActionId = ActionId.UseObject, ActionArgId = Radar.UseArgument
                });
                Assert.AreEqual(PlayerMessage.PmTargetOutOfRange, harness.Drain().OfType<Rasa.Packets.MapChannel.Server.UserActionFailedPacket>().Single().MsgId, $"from {where}: out of range");
                Assert.AreEqual(count, desert.PerformRecovery.Count, $"from {where}: no use begins");
                E.Tick(harness, desert, 250);
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Radar.MissionId, 2), $"from {where}: tower 2 open");
                Assert.AreEqual((UseObjectState)Radar.OffState, console.StateId, $"from {where}: the console off");
                Assert.IsTrue(Ambushers(harness, desert, 1).All(creature => creature == null), $"from {where}: no ambush");
            }
            var log = new List<PythonPacket>();
            Program(harness, desert, console, log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Radar.MissionId, 2), "on its floor: tower 2 programmed");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Radar.MissionId && packet.ObjectiveId == 2), "once");

            // Opt-in: 1275's crate has no floor limit.
            using var other = WildernessRuntimeTestHarness.Create();
            var desert2 = AshenLeadInsTests.EnterAshen(other, Stonewall);
            Accept(other, Npc(desert2, AshenLeadInNpcsV1.Emmons), Codes.MissionId);
            var crate = Crate(other, desert2);
            other.MoveTo(crate.Position + new Vector3(0, 10, 1));
            other.Drain();
            Use(other, desert2, crate, Codes.UseArgument, recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(other, Codes.MissionId, Codes.Recover), "the crate from 10 m over it");
        }

        /// <summary>
        /// King of the Hill: Emmons offers it with TaRapedia's four armour pieces. Accepted: four AFS Turrets of the player's own
        /// stand at the four batteries' public turrets' places (their pools' rows), friendly, and the public turrets are hidden
        /// from the player (not before); 1 shows its countdown (180 s), 2 is hidden; no wave. The player walks from Emmons up to
        /// the batteries: 1 completes, 2 opens and wave 1 comes at once (three Thrax Soldiers); wave 2 not before 45 s, 3 not
        /// before 90 s, 4 not before 135 s, each with its members (Desert Stalkers, Thrax Ravagers, Soldiers), hostile, on the
        /// ground, sent at the turrets, each reaching a battery's foot. A turret lost counts on the hidden 7 (1; nothing of it goes to the
        /// client) and 2 goes on; a reload between keeps it (the lost turret stays lost, its public turret shown again, the
        /// others hidden). Not before the hold's 180 s 2 completes, then once (MissionCompleteable once); the turrets and the
        /// waves go and the public turrets are back. A full bag waits, a failed save pays nothing; Emmons pays the helmet chosen,
        /// once; the chain ends.
        /// </summary>
        [TestMethod]
        public void KingOfTheHillHoldsTheBatteriesThroughFourWaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Codes.MissionId, Radar.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            var publicTurrets = PublicTurrets(desert);
            Assert.IsFalse(publicTurrets.Any(turret => HiddenCreatures.IsHiddenFrom(turret, harness.Client)), "the public turrets before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, emmons, King.MissionId);
            CollectionAssert.AreEqual(new uint[] { 19673, 20504, 37985, 45843 }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                "Bio Helmet V03, Hazmat Gloves V02, Reflective Gloves V05, Motor Assist Boots V09");
            E.Begin(harness, desert, emmons, King.MissionId);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, King.MissionId, King.Defend), "2 hidden");
            var hurry = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[King.MissionId].ObjectivesList.Single(entry => entry.ObjectiveId == King.Hurry);
            Assert.IsTrue(hurry.TimeRemaining is > 170 and <= 180, $"1's countdown: {hurry.TimeRemaining}");

            var turrets = Turrets(harness, desert);
            var rows = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => BatteryPools.Contains(row.Id)).ToList().OrderBy(row => row.Id).ToArray();
            for (var battery = 0; battery < 4; battery++)
            {
                var turret = turrets[battery];
                Assert.IsNotNull(turret, $"battery {battery + 1}'s turret");
                Assert.AreEqual(rows[battery].Creature1Id, turret.DbId, $"battery {battery + 1}: the public turret's row");
                Assert.AreNotEqual(TargetCategory.Hostile, turret.TargetCategory, "friendly");
                Assert.AreEqual(harness.Client.Player.Id, turret.SpawnPool.ScenarioOwnerCharacterId, "the player's own");
                Assert.IsLessThan(0.1f, Vector3.Distance(turret.Position, new((float)rows[battery].PosX, (float)rows[battery].PosY, (float)rows[battery].PosZ)),
                    $"battery {battery + 1}: at the public turret's place");
                Assert.IsTrue(HiddenCreatures.IsHiddenFrom(publicTurrets[battery], harness.Client), $"battery {battery + 1}: the public turret hidden");
            }
            Assert.IsFalse(Wave(harness, desert, 1).Any(), "no wave before the top");

            var log = new List<PythonPacket>();
            E.WalkTo(harness, desert, BatteryFeet[0], log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, King.MissionId, King.Hurry), "on top of Stonewall Ridge");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, King.MissionId, King.Defend), "2 open");
            var start = harness.UtcNow;
            E.Tick(harness, desert, 250);
            void AdvanceTo(uint milliseconds)
            {
                while ((harness.UtcNow - start).TotalMilliseconds < milliseconds)
                {
                    // The fight is held still: only the scene's clock is under test.
                    foreach (var creature in Enumerable.Range(1, King.Waves.Length).SelectMany(wave => Wave(harness, desert, wave))
                                 .Concat(Turrets(harness, desert).Where(turret => turret != null)))
                        creature.Attributes[Attributes.Health].Current = creature.Attributes[Attributes.Health].NormalMax;
                    log.AddRange(E.Tick(harness, desert, 1000));
                }
            }

            for (var wave = 1; wave <= 4; wave++)
            {
                var after = new uint[] { 0, 45000, 90000, 135000 }[wave - 1];
                if (wave > 1)
                {
                    AdvanceTo(after - 5000);
                    Assert.IsFalse(Wave(harness, desert, wave).Any(), $"wave {wave} not before {after - 5000} ms");
                }
                AdvanceTo(after + 1000);
                var members = Wave(harness, desert, wave).ToArray();
                Assert.HasCount(new[] { 3, 3, 4, 4 }[wave - 1], members, $"wave {wave}");
                CollectionAssert.AreEquivalent(new[]
                    {
                        new uint[] { ThraxSoldier, ThraxSoldier, ThraxSoldier }, new uint[] { 790121, 790121, 531009 },
                        new uint[] { ThraxSoldier, ThraxSoldier, 531009, 531009 }, new uint[] { 790121, 790121, ThraxSoldier, ThraxSoldier }
                    }[wave - 1], members.Select(member => member.DbId).ToArray(), $"wave {wave}: TaRapedia's stalkers and Bane");
                var living = Turrets(harness, desert).Where(turret => turret != null).Select(turret => turret.EntityId).ToHashSet();
                foreach (var member in members)
                {
                    Assert.AreEqual(TargetCategory.Hostile, member.TargetCategory, "a foe");
                    Assert.IsTrue(member.SceneAttackPastLeash, $"wave {wave}: sent at the turrets");
                    Assert.IsTrue(living.Contains(Target(member)), $"wave {wave} member {member.DbId}: at a living turret");
                    AssertOnGround(desert, member.Position, $"wave {wave} member");
                    Assert.IsTrue(BatteryFeet.Any(foot => desert.NavMesh.FindPath(member.Position, foot, out var reaches) != null && reaches),
                        $"wave {wave}: reaches a battery's foot");
                }
                if (wave == 2)
                {
                    var lostId = Turrets(harness, desert)[1].EntityId;
                    log.AddRange(E.Die(harness, desert, Turrets(harness, desert)[1]));
                    log.AddRange(E.Tick(harness, desert, 1000));
                    void AtTheTurretsLeft(string when)
                    {
                        var left = Turrets(harness, desert).Where(turret => turret != null).Select(turret => turret.EntityId).ToHashSet();
                        Assert.HasCount(3, left, $"{when}: three turrets stand");
                        Assert.DoesNotContain(lostId, left, $"{when}: the lost one is not among them");
                        var attackers = Enumerable.Range(1, 2).SelectMany(back => Wave(harness, desert, back)).ToArray();
                        Assert.IsNotEmpty(attackers, $"{when}: waves 1 and 2 live");
                        foreach (var member in attackers)
                            Assert.IsTrue(left.Contains(Target(member)), $"{when}: wave member {member.DbId} at a turret still standing ({Target(member)})");
                    }
                    AtTheTurretsLeft("after the loss");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, King.MissionId, King.Defend), "one battery lost");
                    Assert.AreEqual(1U, TowersLost(harness), "Towers Lost 1");
                    E.ReloadAndRecover(harness, desert);
                    Assert.AreEqual((MissionObjectiveState.Incomplete, 1U), (Objective(harness, King.MissionId, King.Defend), TowersLost(harness)), "kept at a reload");
                    var after2 = Turrets(harness, desert);
                    Assert.IsNull(after2[1], "the lost turret stays lost");
                    Assert.IsTrue(new[] { 0, 2, 3 }.All(battery => after2[battery] != null), "the others stand");
                    Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicTurrets[1], harness.Client), "the lost one's public turret shown again");
                    Assert.IsTrue(new[] { 0, 2, 3 }.All(battery => HiddenCreatures.IsHiddenFrom(publicTurrets[battery], harness.Client)), "the others' still hidden");
                    AtTheTurretsLeft("after the reload");
                }
            }
            Assert.IsFalse(log.OfType<UpdateObjectiveCounterPacket>().Any(packet => packet.MissionId == King.MissionId && packet.ObjectiveId == King.LostTally),
                "nothing of the hidden 7 goes to the client");
            AdvanceTo(King.HoldMilliseconds - 5000);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, King.MissionId, King.Defend), "not before the hold");
            AdvanceTo(King.HoldMilliseconds + 2000);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, King.MissionId, King.Defend), "held");
            log.AddRange(harness.Drain());
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == King.MissionId && packet.ObjectiveId == King.Defend),
                "ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == King.MissionId && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsTrue(Turrets(harness, desert).All(turret => turret == null), "the turrets go");
            for (var wave = 1; wave <= 4; wave++)
                Assert.IsFalse(Wave(harness, desert, wave).Any(), $"wave {wave} goes");
            Assert.IsFalse(publicTurrets.Any(turret => HiddenCreatures.IsHiddenFrom(turret, harness.Client)), "the public turrets are back");

            FullBagWaits(harness, emmons, King.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, emmons, King.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, emmons, King.MissionId, 0, 190000, 8400);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == King.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, 19673), "the Bio Helmet chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "done once");
        }

        /// <summary>
        /// The four batteries lost during the defence, one by one: the hidden count 1, 2, 3 with 2 still open, the fourth fails 2
        /// and the mission; the waves go and the public turrets are back.
        /// </summary>
        [TestMethod]
        public void KingOfTheHillFailsWithTheFourthBatteryLost()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Codes.MissionId, Radar.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            var publicTurrets = PublicTurrets(desert);
            E.Begin(harness, desert, emmons, King.MissionId);
            var log = new List<PythonPacket>();
            E.WalkTo(harness, desert, BatteryFeet[0], log);
            E.Tick(harness, desert, 250);
            Assert.IsTrue(Wave(harness, desert, 1).Any(), "wave 1");
            var turrets = Turrets(harness, desert);
            for (var battery = 0; battery < 4; battery++)
            {
                E.Die(harness, desert, turrets[battery]);
                if (battery < 3)
                    Assert.AreEqual((MissionObjectiveState.Incomplete, (uint)battery + 1), (Objective(harness, King.MissionId, King.Defend), TowersLost(harness)),
                        $"{battery + 1} lost: 2 goes on");
            }
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, King.MissionId, King.Defend), "the fourth lost: 2 fails");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[King.MissionId].State);
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, King.MissionId), "the failed mission cleared");
            E.Tick(harness, desert, 250);
            Assert.IsFalse(Wave(harness, desert, 1).Any(), "the wave goes");
            Assert.IsFalse(publicTurrets.Any(turret => HiddenCreatures.IsHiddenFrom(turret, harness.Client)), "the public turrets are back");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "offered again");
        }

        /// <summary>
        /// Three turrets lost before the top: 1 still completes at the top, the defence starts with the one turret left, the
        /// wave goes for it, and its loss (the fourth) fails 2 and the mission.
        /// </summary>
        [TestMethod]
        public void KingOfTheHillCountsTheLossesBeforeTheTopIntoTheDefence()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Codes.MissionId, Radar.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            E.Begin(harness, desert, emmons, King.MissionId);
            var turrets = Turrets(harness, desert);
            for (var battery = 1; battery < 4; battery++)
                E.Die(harness, desert, turrets[battery]);
            Assert.AreEqual((MissionObjectiveState.Incomplete, 3U), (Objective(harness, King.MissionId, King.Hurry), TowersLost(harness)), "three lost, 1 open");
            var log = new List<PythonPacket>();
            E.WalkTo(harness, desert, BatteryFeet[0], log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, King.MissionId, King.Hurry), "on top with one turret");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, King.MissionId, King.Defend), "2 open");
            E.Tick(harness, desert, 250);
            var members = Wave(harness, desert, 1).ToArray();
            Assert.HasCount(3, members, "wave 1");
            Assert.IsTrue(members.All(member => Target(member) == turrets[0].EntityId), "wave 1 at the one turret left");
            E.Die(harness, desert, turrets[0]);
            Assert.AreEqual((4U, MissionObjectiveState.Failed, MissionState.Failed), (TowersLost(harness), Objective(harness, King.MissionId, King.Defend),
                harness.Client.Player.Missions[King.MissionId].State), "the fourth lost: 2 fails");
        }

        /// <summary>
        /// 1 fails at its deadline: at 170 s it is still open, past 180 s it fails, and the mission with it; cleared, the turrets
        /// go and the public ones are back. Taken again: three turrets lost before the top leave 1 open (counted on the hidden 7),
        /// the fourth fails it. Taken again and abandoned
        /// mid-defence: failed (a timed mission, as Daghda's Urn), cleared the waves and the turrets go and the public turrets
        /// are back; offered again.
        /// </summary>
        [TestMethod]
        public void KingOfTheHillFailsAtItsDeadlineOrAllFourLostBeforeTheTop()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Codes.MissionId, Radar.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var emmons = Npc(desert, AshenLeadInNpcsV1.Emmons);
            var publicTurrets = PublicTurrets(desert);
            E.Begin(harness, desert, emmons, King.MissionId);
            for (var second = 0; second < 170; second++)
                E.Tick(harness, desert, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, King.MissionId, King.Hurry), "170 s: still open");
            for (var second = 0; second < 15; second++)
                E.Tick(harness, desert, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            E.Tick(harness, desert, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, King.MissionId, King.Hurry), "past 180 s");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[King.MissionId].State);
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, King.MissionId), "the failed mission cleared");
            E.Tick(harness, desert, 250);
            Assert.IsTrue(Turrets(harness, desert).All(turret => turret == null), "the turrets go");
            Assert.IsFalse(publicTurrets.Any(turret => HiddenCreatures.IsHiddenFrom(turret, harness.Client)), "the public turrets are back");

            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "offered again");
            E.Begin(harness, desert, emmons, King.MissionId);
            var early = Turrets(harness, desert);
            for (var battery = 0; battery < 4; battery++)
            {
                E.Die(harness, desert, early[battery]);
                if (battery < 3)
                    Assert.AreEqual((MissionObjectiveState.Incomplete, MissionState.Active, (uint)battery + 1),
                        (Objective(harness, King.MissionId, King.Hurry), harness.Client.Player.Missions[King.MissionId].State, TowersLost(harness)),
                        $"{battery + 1} lost before the top: 1 goes on (the client: all four destroyed fail it)");
            }
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, King.MissionId, King.Hurry), "all four lost before the top");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[King.MissionId].State);
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, King.MissionId));
            E.Tick(harness, desert, 250);
            Assert.IsTrue(Turrets(harness, desert).All(turret => turret == null), "the turrets go");
            Assert.IsFalse(publicTurrets.Any(turret => HiddenCreatures.IsHiddenFrom(turret, harness.Client)), "the public turrets are back");

            E.Begin(harness, desert, emmons, King.MissionId);
            var log = new List<PythonPacket>();
            E.WalkTo(harness, desert, BatteryFeet[0], log);
            E.Tick(harness, desert, 250);
            Assert.IsTrue(Wave(harness, desert, 1).Any(), "wave 1");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, King.MissionId));
            harness.Drain();
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[King.MissionId].State, "abandoned: failed");
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, King.MissionId), "the failed mission cleared");
            E.Tick(harness, desert, 250);
            Assert.IsFalse(Wave(harness, desert, 1).Any(), "the wave goes");
            Assert.IsTrue(Turrets(harness, desert).All(turret => turret == null), "the turrets go");
            Assert.IsFalse(publicTurrets.Any(turret => HiddenCreatures.IsHiddenFrom(turret, harness.Client)), "the public turrets are back");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, King.MissionId), "offered again");
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorWalk: every half metre, never more than 1.1 m up over a
        /// metre, nothing of the placed meshes in the way at knee, chest or head height), there and back: from the Stonewall
        /// Ridge waypoint (197) Emmons, the four towers' ground teleporters, each battery's foot and every wave member's spot; on
        /// each tower's second floor from its upper teleporter by the lift's exit to its console, and from every ambush spot by
        /// the exit to the console (fix E: the ambushes on the top floor); from the Ashoka Settlement
        /// waypoint the salvage crate. Where the straight navmesh walk meets a step the client's floor does not take (a knee-high
        /// rise at (21.6, -437.0) on the way east, a 1.12-1.20 m step at (35.9, -502.1) and (-39.5, -450.4)), the walk goes by
        /// the corners Route names (the probe of 2026-11-10; ESTIMATE of a player's line).
        /// </summary>
        [TestMethod]
        public void StonewallRidgesPlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var pads = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList()
                .ToDictionary(row => row.Id, row => new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ));
            var failures = new List<string>();
            var walked = 0;
            void Walk(string name, IReadOnlyList<Vector3> there, IReadOnlyList<Vector3> back)
            {
                foreach (var (way, label) in new[] { (there, "there"), (back, "back") })
                    for (var leg = 0; leg + 1 < way.Count; leg++)
                    {
                        var from = nav.Nearest(way[leg]) ?? way[leg];
                        var to = nav.Nearest(way[leg + 1]) ?? way[leg + 1];
                        walked++;
                        if (AshenFixBTests.ClientFloorWalk(nav, geometry, from, to) is { } why)
                            failures.Add(FormattableString.Invariant($"{name} {label}, leg {leg + 1} ({from.X:0.0}, {from.Z:0.0}) to ({to.X:0.0}, {to.Z:0.0}): {why}"));
                    }
            }
            void FromStonewall(string name, Vector3 target)
            {
                var (there, back) = Route(target);
                Walk(name, there.Prepend(Stonewall).Append(target).ToList(), back.Prepend(target).Append(Stonewall).ToList());
            }

            var emmons = harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == AshenLeadInNpcsV1.Emmons);
            FromStonewall("Emmons", new((float)emmons.PosX, (float)emmons.PosY, (float)emmons.PosZ));
            for (var tower = 0; tower < 4; tower++)
            {
                var (ground, upper) = TowerPads[tower];
                FromStonewall($"tower {tower + 1}'s ground teleporter {ground}", pads[ground]);
                // Fix E: from the upper teleporter off by the lift's exit and on to the console, not back over the pad; the ambushers
                // come on the top floor and walk to the exit and the console (AshenFixETests).
                var console = Fix.Towers[tower].ConsoleAt;
                var at = new Vector3(console.X, console.Y, console.Z);
                var exit = new Vector3(Fix.ExitAt[tower].X, Fix.ExitAt[tower].Y, Fix.ExitAt[tower].Z);
                Walk($"console {tower + 1} from its upper teleporter {upper} by the lift's exit", new[] { pads[upper], exit, at }, new[] { at, exit });
                foreach (var spot in Fix.Towers[tower].AmbushAt)
                {
                    var from = new Vector3(spot.X, spot.Y, spot.Z);
                    Walk(FormattableString.Invariant($"tower {tower + 1}'s ambush spot ({spot.X}, {spot.Z})"), new[] { from, exit, at }, new[] { at, from });
                }
            }
            for (var battery = 0; battery < 4; battery++)
                FromStonewall($"battery {battery + 1}'s foot", BatteryFeet[battery]);
            foreach (var (after, members) in King.Waves)
                foreach (var (_, spot) in members)
                    FromStonewall(FormattableString.Invariant($"the {after} ms wave's spot ({spot.X}, {spot.Z})"), new(spot.X, spot.Y, spot.Z));
            var crate = new Vector3(Codes.CrateAt.X, Codes.CrateAt.Y, Codes.CrateAt.Z);
            Walk("the salvage crate from the Ashoka Settlement waypoint", new[] { Ashoka, crate }, new[] { crate, Ashoka });
            Assert.IsGreaterThan(80, walked, "the legs are walked");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The corners of the walk from the Stonewall Ridge waypoint to a place east or south of it, and back (the probe of
        /// 2026-11-10): east of x 0 by (18.5, -429.6) and (32.8, -438.1) round the rise at (21.6, -437.0), back by (25.8, -437.1)
        /// and then (38.4, -430.9) (the probe's way round it); south of z -500 also by Artillery Control 1's foot (59.4, -474.9) and back by it; the Operations
        /// tower's back by (-35.5, -450.4); Artillery Control 2's foot (z under -585) by (39.9, -502.1) and its ground
        /// teleporter (146.8, -602.8), and back by that teleporter, from where the walk to the waypoint is straight. None for the places by the waypoint.
        /// </summary>
        private static (Vector3[] There, Vector3[] Back) Route(Vector3 target)
        {
            var east = new[] { new Vector3(18.54f, 346.79f, -429.61f), new Vector3(32.79f, 346.65f, -438.06f) };
            var westward = new[] { new Vector3(25.80f, 349.08f, -437.10f), new Vector3(38.42f, 338.63f, -430.87f) };
            var artillery = new Vector3(59.41f, 350.58f, -474.90f);
            var control2 = new Vector3(146.75f, 337.25f, -602.82f);
            if (target.X > 100 && target.Z < -585)
                return (new[] { new Vector3(39.90f, 350.28f, -502.10f), control2 }, new[] { control2 });
            if (target.X > 0)
                return target.Z < -480
                    ? (east.Append(artillery).ToArray(), new[] { artillery }.Concat(westward).ToArray())
                    : (east, westward);
            if (target.Z < -440)
                return (Array.Empty<Vector3>(), new[] { new Vector3(-35.50f, 350.06f, -450.40f) });
            return (Array.Empty<Vector3>(), Array.Empty<Vector3>());
        }

        /// <summary>Whom the creature fights now (0: nobody).</summary>
        private static ulong Target(Creature creature) => creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting
            ? creature.Controller.ActionFighting.TargetEntityId : 0;

        private static uint CodesHeld(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, Codes.CodesTemplate);

        /// <summary>1278's hidden 7's count of the batteries lost.</summary>
        private static uint TowersLost(WildernessRuntimeTestHarness harness) =>
            (uint)harness.Client.Player.Missions[King.MissionId].Objectives[King.LostTally].Counters.Values.Sum(value => (long)value);

        /// <summary>The links of local_teleporter_link from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static int Links(WildernessRuntimeTestHarness harness, uint from, uint to) => harness.World.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id = {from} AND to_teleporter_id = {to}")
            .AsEnumerable().Single();

        /// <summary>The salvage crate: the player's own, of its class and name, closed, on the ground within 15 m of the wreck, walked to from the Ashoka waypoint.</summary>
        private static DynamicObject Crate(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            harness.Manager.Scenes.Tick(desert);
            var crate = desert.DynamicObjects.Single(obj => obj.SceneMissionId == Codes.MissionId && obj.SceneActorRole == Codes.CrateRole && obj.IsInWorld);
            Assert.AreEqual((7904U, harness.Client.Player.Id), ((uint)crate.EntityClassId, crate.SceneOwnerCharacterId), "the player's own Bane crate");
            Assert.IsLessThanOrEqualTo(15f, Vector2.Distance(new(crate.Position.X, crate.Position.Z), new(Wreck.X, Wreck.Z)), "by the wreck");
            AssertOnGround(desert, crate.Position, "the crate");
            Assert.IsTrue(desert.NavMesh.FindPath(desert.NavMesh.Nearest(Ashoka) ?? Ashoka, crate.Position, out var complete) != null && complete,
                "the crate is walked to");
            return crate;
        }

        /// <summary>The four consoles in tower order: the player's own, of the human monitor class (9478).</summary>
        private static DynamicObject[] Consoles(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            harness.Manager.Scenes.Tick(desert);
            var consoles = new DynamicObject[4];
            for (var tower = 0; tower < 4; tower++)
            {
                consoles[tower] = desert.DynamicObjects.Single(obj => obj.SceneMissionId == Radar.MissionId && obj.SceneActorRole == Radar.ConsoleRole(tower) &&
                    obj.IsInWorld);
                Assert.AreEqual((9478U, harness.Client.Player.Id), ((uint)consoles[tower].EntityClassId, consoles[tower].SceneOwnerCharacterId),
                    $"console {tower + 1}: the player's own human monitor");
            }
            return consoles;
        }

        private static Creature[] Ambushers(WildernessRuntimeTestHarness harness, MapChannel desert, int tower) =>
            Enumerable.Range(0, Radar.Towers[tower].AmbushAt.Length).Select(member => E.SceneCreature(harness, desert, Radar.MissionId, Radar.AmbushRole(tower, member)))
                .ToArray();

        /// <summary>The run's four turrets in battery order (null where one is lost or gone).</summary>
        private static Creature[] Turrets(WildernessRuntimeTestHarness harness, MapChannel desert) =>
            Enumerable.Range(1, 4).Select(battery => E.SceneCreature(harness, desert, King.MissionId, D.Ward(battery))).ToArray();

        /// <summary>Wave <paramref name="wave"/>'s living members of this run, in member order.</summary>
        private static IEnumerable<Creature> Wave(WildernessRuntimeTestHarness harness, MapChannel desert, int wave) =>
            Enumerable.Range(1, 8).Select(member => E.SceneCreature(harness, desert, King.MissionId, D.WaveRole(wave, member))).Where(creature => creature != null);

        /// <summary>The four batteries' public turrets, spawned, in battery order.</summary>
        private static Creature[] PublicTurrets(MapChannel desert)
        {
            Spawn(desert, BatteryPools);
            return BatteryPools.Select(pool => Living(desert).Single(creature => creature.SpawnPool?.DbId == pool && creature.SpawnPool.SceneRunId == null)).ToArray();
        }

        private static void Search(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject crate, List<PythonPacket> log = null)
        {
            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            log?.AddRange(harness.Drain());
            Use(harness, desert, crate, Codes.UseArgument, recovers: true);
            log?.AddRange(harness.Drain());
        }

        private static void Program(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject console, List<PythonPacket> log)
        {
            harness.MoveTo(console.Position + new Vector3(0, 0, 1));
            log.AddRange(harness.Drain());
            Use(harness, desert, console, Radar.UseArgument, recovers: true);
            log.AddRange(harness.Drain());
        }

        /// <summary>A use the server may refuse at once or after its recovery: the caller checks that nothing came of it.</summary>
        private static void TrySearch(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj) => TryUse(harness, desert, obj, Codes.UseArgument);

        private static void TryProgram(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj) => TryUse(harness, desert, obj, Radar.UseArgument);

        private static void TryUse(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = desert.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new Rasa.Packets.MapChannel.Client.RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (desert.PerformRecovery.Count > count)
            {
                var recovery = desert.PerformRecovery.Last();
                desert.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(desert, recovery);
            }
        }
    }
}
