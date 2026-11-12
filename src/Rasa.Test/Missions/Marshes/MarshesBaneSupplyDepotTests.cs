using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using static PlateauInstanceMaps;
    using N = MarshesBaneSupplyDepotNpcsV1;
    using V = MarshesSurvivorsV2;
    using B = MarshesBeinvenidosDelVoroV1;
    using I = MarshesInsertionPointV1;

    /// <summary>
    /// Marshes set 16, the Bane Supply Depot (squad instance 1451), played through as a player plays it: Headquarters'
    /// Survivors (664: the camp found in time, three waves, Ortega, the wreck's crates), Headquarters' Beinvenidos del Voro (919:
    /// three command consoles' datablades to Fitz in Paludos) and Elder Toida's Insertion Point (663: in by force, the alarm at
    /// a front guard's first hit, or by stealth, the lower entrance before it; then the mortars or the terminal; Headquarters
    /// by radio).
    ///
    /// Why these tests: the scenes are bound to a squad copy that outlives a test (the run's own actors are looked for); the
    /// radio offers come on arrival; 664 runs on a deadline and on waves; 919's blades are given by a scene; 663 has two ways in
    /// on one alarm and its mortar count is the depot's only. A way in that both completes, an alarm that does not close the
    /// quiet way, a mortar of the open Marshes counted, a blade counted twice, or progress lost at a reload would each break
    /// them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesBaneSupplyDepotTests
    {
        private const uint Depot = N.MapContextId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint Survivors = V.MissionId, Blades = B.MissionId, Insertion = I.MissionId;

        /// <summary>The depot's main navmesh by the camp: the arrival and its front entrance are a part of their own (probe 2026-11-03).</summary>
        private static readonly Vector3 CampEdge = new(129.9f, 125.66f, 312.9f);

        /// <summary>
        /// Ortega stands at the camp with the client's name, level 35 and package 542; on arrival Headquarters offers 664 and 919
        /// by radio; Toida offers 663 from level 37 only; the depot's door leads back to the Marshes door.
        /// </summary>
        [TestMethod]
        public void OrtegaStandsAndTheOffersCome()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, I.Toida);
            AtLevel(harness, 36);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, toida, Insertion), "663 needs level 37");
            AtLevel(harness, 37);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, toida, Insertion), "Toida offers 663 at level 37");

            var copy = IntoDepot(harness);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.OfferArrivalMissions(harness.Client);
            var offers = harness.Drain();
            foreach (var mission in new[] { Survivors, Blades })
                Assert.IsTrue(offers.OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == mission), $"{mission} on arrival");
            var ortega = Giver(copy, N.Ortega);
            Assert.AreEqual((N.OrtegaName, N.OrtegaLevel, N.OrtegaPackage), (ortega.NameId, ortega.Level, ortega.Npc?.NpcPackageId));
            AssertOnGround(copy, ortega.Position, "Ortega");
            AssertPath(copy, CampEdge, ortega.Position, "Ortega from the camp");
            Assert.AreNotEqual(TargetCategory.Hostile, ortega.TargetCategory);
        }

        /// <summary>
        /// 664: taken by radio in the depot; no Thrax before the camp; the camp reached (1) brings waves of four at once, after
        /// 30 s and 60 s, and opens 5 at once (to-fix 5: the fight is shown), which counts the 12 kills (a reload kept); the
        /// twelfth completes 5 and sets the wreck's two crates down; each gives its supplies once; both complete 3; Ortega takes
        /// them and pays.
        /// </summary>
        [TestMethod]
        public void SurvivorsHoldTheCampAndBringOrtegaTheSupplies()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoDepot(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Survivors);
            Assert.IsNull(SceneCreature(harness, copy, Survivors, V.WaveRole(0, 0)), "no Thrax before the camp");
            Assert.IsNull(FindSceneObject(harness, copy, Survivors, V.Crates[0].Role), "no crates before the fight is won");

            Enter(harness, ToVector(V.CampAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Survivors, V.Locate));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Survivors, V.Eliminate), "the fight is shown");
            Assert.AreEqual(0U, harness.Client.Player.Missions[Survivors].Objectives[V.Eliminate].Counters[0]);
            var killed = 0;
            for (var wave = 0; wave < V.Waves.Length; wave++)
            {
                if (wave > 0)
                {
                    Assert.IsNull(SceneCreature(harness, copy, Survivors, V.WaveRole(wave, 0)), $"wave {wave + 1} not yet");
                    for (var waited = 0U; waited < V.Waves[wave].After - V.Waves[wave - 1].After; waited += 5000)
                        Tick(harness, copy, 5000);
                    Tick(harness, copy, 250);
                }
                if (wave == 1)
                    ReloadAndRecover(harness, copy);
                for (var member = 0; member < V.Waves[wave].Members.Length; member++)
                {
                    var thrax = SceneCreature(harness, copy, Survivors, V.WaveRole(wave, member));
                    Assert.IsNotNull(thrax, $"wave {wave + 1} member {member + 1}");
                    Assert.AreEqual(TargetCategory.Hostile, thrax.TargetCategory);
                    Assert.IsNotNull(thrax.SceneAttackOrder, "sent at the player");
                    AssertPath(copy, CampEdge, thrax.Position, "a wave's place from the camp");
                    Kill(harness, copy, thrax);
                    killed++;
                    Assert.AreEqual(killed < V.Attackers ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                        Objective(harness, Survivors, V.Eliminate), $"after kill {killed}");
                    if (killed < V.Attackers)
                        Assert.AreEqual((uint)killed, harness.Client.Player.Missions[Survivors].Objectives[V.Eliminate].Counters[0], "n/12");
                }
            }

            var ortega = Giver(copy, N.Ortega);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Survivors, V.Supplies));
            for (var index = 0; index < V.Crates.Length; index++)
            {
                var crate = SceneObject(harness, copy, Survivors, V.Crates[index].Role);
                AssertOnGround(copy, crate.Position, V.Crates[index].Role);
                AssertPath(copy, CampEdge, crate.Position, $"{V.Crates[index].Role} from the camp");
                Use(harness, copy, crate, V.CrateArgument);
                Use(harness, copy, crate, V.CrateArgument);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, V.Crates[index].Template), $"{V.Crates[index].Role}, once");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Survivors, V.Supplies));
            FailedTurnInPaysNothing(harness, ortega, Survivors, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ortega, Survivors, 3, V.Experience, V.Credits);
            Assert.AreEqual(V.CryoGrenades, WildernessRanjaGorgeTests.Held(harness, V.CryoGrenade), "the chosen stack");
            foreach (var crate in V.Crates)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, crate.Template), "Ortega takes the supplies");
        }

        /// <summary>664: ten minutes and the camp not reached: 1 fails, the mission with it; no Thrax came.</summary>
        [TestMethod]
        public void SurvivorsFailWhenTheCampIsNotFoundInTime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoDepot(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Survivors);
            for (var second = 0; second < V.DeadlineSeconds + 20; second += 20)
                Tick(harness, copy, 20000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Survivors, V.Locate));
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Survivors].State);
            Assert.IsNull(SceneCreature(harness, copy, Survivors, V.WaveRole(0, 0)), "no attack");
        }

        /// <summary>
        /// 919: taken by radio in the depot; three command consoles on the walked-to ground; each gives one datablade once (a
        /// reload kept); the third completes 1; back on the Marshes Fitz takes the blades and pays.
        /// </summary>
        [TestMethod]
        public void BeinvenidosDelVoroBringsTheBladesToFitz()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoDepot(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Blades);
            for (var index = 0; index < B.ComputersAt.Length; index++)
            {
                if (index == 1)
                    ReloadAndRecover(harness, copy);
                var console = SceneObject(harness, copy, Blades, B.ComputerRole(index));
                Assert.AreEqual(B.ComputerClass, (uint)console.EntityClassId);
                AssertOnGround(copy, console.Position, $"console {index + 1}");
                AssertPath(copy, CampEdge, console.Position, $"console {index + 1} from the camp");
                Use(harness, copy, console, B.UseArgument);
                Use(harness, copy, console, B.UseArgument);
                Assert.IsFalse(console.IsEnabled, $"console {index + 1} closed after its blade");
                Assert.AreEqual((uint)index + 1, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate), $"blade {index + 1}, once");
                Assert.AreEqual(index < 2 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed, Objective(harness, Blades, B.Collect));
            }
            harness.Manager.Scenes.Detach(harness.Client, copy);
            var marshes = EnterMarshes(harness);
            var fitz = Npc(marshes, B.Fitz);
            FailedTurnInPaysNothing(harness, fitz, Blades, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, fitz, Blades, 0, B.Experience, B.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.BioHelmet), "the chosen armor");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate), "Fitz takes the blades");
        }

        /// <summary>
        /// 663 by force: taken at Toida; in the depot the four front guards stand on the ground; two mortars killed before a
        /// way in count (6 open from the accept); the first hit on a guard sounds the alarm: 4 and 5 complete, 3 fails; a mortar
        /// on the open Marshes does not count; the depot's other 11 do (a reload kept); 6 done, Headquarters pays by radio.
        /// </summary>
        [TestMethod]
        public void InsertionPointByForceDestroysTheMortars()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            AtLevel(harness, 37);
            Accept(harness, Npc(marshes, I.Toida), Insertion);
            var copy = IntoDepot(harness);
            Tick(harness, copy, 250);
            for (var index = 0; index < I.GuardsAt.Length; index++)
            {
                var guard = SceneCreature(harness, copy, Insertion, I.GuardRole(index));
                Assert.IsNotNull(guard, $"front guard {index + 1}");
                AssertOnGround(copy, guard.Position, $"front guard {index + 1}");
            }
            // Mortars count before a way in (6 open from the accept).
            var early = Here(copy).Where(creature => creature.DbId == I.BaneMortar).ToArray();
            if (early.Length < 2)
            {
                Spawn(copy, harness.World.SpawnPoolEntries.Where(row => row.MapContextId == Depot && row.Creature1Id == I.BaneMortar)
                    .Select(row => row.Id).ToArray());
                early = Here(copy).Where(creature => creature.DbId == I.BaneMortar).ToArray();
            }
            Kill(harness, copy, early[0]);
            Kill(harness, copy, early[1]);
            Assert.AreEqual(2U, Counter(harness, Insertion, I.Mortars), "two mortars before a way in");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Insertion, I.Inside));

            Hit(harness, copy, SceneCreature(harness, copy, Insertion, I.GuardRole(0)));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Force), "in by force");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Insertion, I.Stealth), "the quiet way is closed");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Insertion].State, "the optional way's fail fails nothing");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Insertion, I.Mortars));
            Enter(harness, ToVector(I.RearAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Insertion, I.Stealth), "not by stealth after the alarm");

            // A mortar of the open Marshes is not one of the depot's.
            harness.Manager.Scenes.Detach(harness.Client, copy);
            marshes = EnterMarshes(harness);
            Spawn(marshes, 592159);
            Kill(harness, marshes, I.BaneMortar);
            harness.Drain();
            Assert.AreEqual(2U, Counter(harness, Insertion, I.Mortars), "the Marshes' mortar does not count");
            copy = IntoDepot(harness);
            var mortars = Here(copy).Where(creature => creature.DbId == I.BaneMortar).ToArray();
            if (mortars.Length < I.MortarCount - 2)
            {
                Spawn(copy, harness.World.SpawnPoolEntries.Where(row => row.MapContextId == Depot && row.Creature1Id == I.BaneMortar)
                    .Select(row => row.Id).ToArray());
                mortars = Here(copy).Where(creature => creature.DbId == I.BaneMortar).ToArray();
            }
            var left = (int)I.MortarCount - 2;
            Assert.IsGreaterThanOrEqualTo(left, mortars.Length, "the depot's mortars");
            for (var index = 0; index < left; index++)
            {
                if (index == 5)
                    ReloadAndRecover(harness, copy);
                Kill(harness, copy, mortars[index]);
                Assert.AreEqual(index < left - 1 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                    Objective(harness, Insertion, I.Mortars), $"after mortar {index + 3}");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Insertion].Completeable);
            PayInsertion(harness);
        }

        /// <summary>
        /// To-fix 36: a front guard killed with one shot sounds the alarm as a hit does (a kill clears the hit before the scene
        /// looks: CreatureManager.HandleCreatureKill), each of the four - also by a GM who is invisible, as the tester's was -
        /// and for good: 4 and 5 complete, 3 fails, and stay so as the other guards die.
        /// </summary>
        [TestMethod]
        public void InsertionPointsAlarmSoundsAtAGuardKilledWithOneShot()
        {
            for (var index = 0; index < I.GuardsAt.Length; index++)
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                var marshes = EnterMarshes(harness);
                AtLevel(harness, 37);
                Accept(harness, Npc(marshes, I.Toida), Insertion);
                var copy = IntoDepot(harness);
                Tick(harness, copy, 250);
                harness.Client.Player.GmInvisible = index % 2 == 1;
                for (var other = 0; other < I.GuardsAt.Length; other++)
                {
                    var guard = SceneCreature(harness, copy, Insertion, I.GuardRole((index + other) % I.GuardsAt.Length));
                    Assert.IsNotNull(guard, $"guard {(index + other) % I.GuardsAt.Length + 1}");
                    guard.Attributes[Attributes.Health].Current = 1;
                    if (guard.Attributes.TryGetValue(Attributes.Armor, out var armor))
                        armor.Current = 0;
                    Hit(harness, copy, guard);
                    Assert.AreEqual(CharacterState.Dead, guard.State, "one shot kills it");
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Force), $"guard {index + 1} first: in by force");
                    Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Insertion, I.Stealth), "the quiet way is closed");
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Inside));
                }
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Insertion].State);
            }
        }

        /// <summary>
        /// 663 by stealth, the terminal first: the terminal's computer completes 6 at once, before any way in (6 is open from the
        /// accept), but the mission is not done: Headquarters does not pay. The lower entrance reached with no guard hit (a
        /// reload kept) completes 3 (4 stays open, optional) and 5: now it pays. A later hit leaves the stealth done.
        /// </summary>
        [TestMethod]
        public void InsertionPointByStealthUsesTheTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            AtLevel(harness, 37);
            Accept(harness, Npc(marshes, I.Toida), Insertion);
            var copy = IntoDepot(harness);
            Tick(harness, copy, 250);
            var terminal = SceneObject(harness, copy, Insertion, I.ComputerRole);
            AssertOnGround(copy, terminal.Position, "the terminal");
            AssertPath(copy, CampEdge, terminal.Position, "the terminal from the camp");
            Use(harness, copy, terminal, I.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Mortars), "deactivated before a way in");
            Assert.IsFalse(terminal.IsEnabled, "the terminal closed after its use");
            Assert.IsFalse(harness.Client.Player.Missions[Insertion].Completeable, "no way in yet");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Insertion, 2, null), "no pay without a way in");
            harness.Drain();

            ReloadAndRecover(harness, copy);
            AssertPath(copy, CampEdge, ToVector(I.RearAt), "the lower entrance from the camp");
            Enter(harness, ToVector(I.RearAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Stealth), "in by stealth");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Insertion, I.Force));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Inside));
            Assert.IsTrue(harness.Client.Player.Missions[Insertion].Completeable);

            Hit(harness, copy, SceneCreature(harness, copy, Insertion, I.GuardRole(1)));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Stealth), "the stealth stays done");
            PayInsertion(harness);
        }

        /// <summary>
        /// Abandons: 919 with one blade held loses it, taken again its consoles give again; 663 abandoned after the alarm and
        /// taken again has both ways open (the stealth not failed), the guards there again.
        /// </summary>
        [TestMethod]
        public void AbandonedDepotMissionsStartOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            AtLevel(harness, 37);
            var toida = Npc(marshes, I.Toida);
            Accept(harness, toida, Insertion);
            var copy = IntoDepot(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Blades);
            Use(harness, copy, SceneObject(harness, copy, Blades, B.ComputerRole(0)), B.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Blades));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate), "the blade goes with the abandon");
            Hit(harness, copy, SceneCreature(harness, copy, Insertion, I.GuardRole(0)));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Insertion, I.Stealth));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Insertion));
            harness.Drain();

            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Blades);
            Use(harness, copy, SceneObject(harness, copy, Blades, B.ComputerRole(0)), B.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate), "the console gives again");

            harness.Manager.Scenes.Detach(harness.Client, copy);
            marshes = EnterMarshes(harness);
            Accept(harness, Npc(marshes, I.Toida), Insertion);
            copy = IntoDepot(harness);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Insertion, I.Stealth), "the quiet way open again");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Insertion, I.Force));
            Assert.IsNotNull(SceneCreature(harness, copy, Insertion, I.GuardRole(0)), "the guards stand again");
            Enter(harness, ToVector(I.RearAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Insertion, I.Stealth), "in by stealth this time");
        }

        /// <summary>
        /// Full bags: a console's blade (919) and a crate's supplies (664) with no room give nothing and the object stays open;
        /// one slot freed, the same use gives the item once.
        /// </summary>
        [TestMethod]
        public void FullBagsKeepTheConsolesAndCratesOpen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoDepot(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Blades);
            TakeByRadio(harness, copy, Survivors);

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            var console = SceneObject(harness, copy, Blades, B.ComputerRole(0));
            Use(harness, copy, console, B.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate), "no room for the blade");
            Assert.IsTrue(console.IsEnabled, "the console waits");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Blades, B.Collect));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            Use(harness, copy, console, B.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.BladeTemplate), "the blade with the room");
            Assert.IsFalse(console.IsEnabled);

            // 664 to its crates: the camp, the 12 Thrax, Ortega.
            Enter(harness, ToVector(V.CampAt));
            Tick(harness, copy, 250);
            for (var wave = 0; wave < V.Waves.Length; wave++)
            {
                if (wave > 0)
                {
                    for (var waited = 0U; waited < V.Waves[wave].After - V.Waves[wave - 1].After; waited += 5000)
                        Tick(harness, copy, 5000);
                    Tick(harness, copy, 250);
                }
                for (var member = 0; member < V.Waves[wave].Members.Length; member++)
                    Kill(harness, copy, SceneCreature(harness, copy, Survivors, V.WaveRole(wave, member)));
            }
            Tick(harness, copy, 250);
            // The bag is full again: the blade took the freed slot.
            var crate = SceneObject(harness, copy, Survivors, V.Crates[0].Role);
            Use(harness, copy, crate, V.CrateArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, V.Crates[0].Template), "no room for the supplies");
            Assert.IsTrue(crate.IsEnabled, "the crate waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            Use(harness, copy, crate, V.CrateArgument);
            Use(harness, copy, crate, V.CrateArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, V.Crates[0].Template), "the supplies with the room, once");
        }

        private static void PayInsertion(WildernessRuntimeTestHarness harness)
        {
            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Insertion, 2, null));
            harness.Drain();
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + I.Experience, before.Credits + (int)I.Credits), (after.Experience, after.Credits));
            Assert.AreEqual(I.ResTraumaKits, WildernessRanjaGorgeTests.Held(harness, I.ResTraumaKit), "the chosen stack");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Insertion, 2, null), "and only once");
        }

        /// <summary>One weapon hit on the creature (its first hit is a scene's DamagedSequences), the scene run on.</summary>
        private static void Hit(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            Assert.IsNotNull(creature);
            Steady(harness);
            harness.MoveTo(creature.Position + new Vector3(0, 0, 3));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = creature.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, 50);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Drain();
            Tick(harness, map, 250);
        }

        /// <summary>The player at the level, live and durable (an accept checks the character row, MissionApplication).</summary>
        private static void AtLevel(WildernessRuntimeTestHarness harness, uint level)
        {
            using (var unit = harness.Context.CreateChar())
                unit.Characters.UpdateCharacterLevel(harness.Client.Player.Id, (byte)level);
            harness.Client.Player.Level = (byte)level;
        }

        /// <summary>The radio missions taken (their offers already made), the scene run on.</summary>
        private static void TakeByRadio(WildernessRuntimeTestHarness harness, MapChannel copy, uint mission)
        {
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, mission), $"{mission} taken");
            harness.Drain();
            Tick(harness, copy, 250);
        }

        /// <summary>Into the squad's copy of the depot through its door on the Marshes, the scenes resumed (as MapLoaded).</summary>
        private static MapChannel IntoDepot(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Depot))
                LoadMap(harness, Depot);
            var copy = ThroughTheDoor(harness, Depot);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];
    }
}
