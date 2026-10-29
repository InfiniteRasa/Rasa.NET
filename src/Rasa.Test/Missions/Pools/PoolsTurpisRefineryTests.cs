extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;
    using static PlateauInstanceMaps;

    /// <summary>
    /// Turpis Refinery's missions (squad instance 1429), played through as a player plays them: Turpis Transport (1305:
    /// Corporal Hash on the Pools, done on arrival), Things That Blow Up (882: the wall, then five bombs inside), Hard Core
    /// (884: three hard drives), Chemistry 101 (885: samples from the refinery's technicians), The Bad Doctor (886: the
    /// Toxicologist comes when the player comes near) and Cost of Doing Business (1079: 72 hours of lab equipment).
    ///
    /// Why these tests: the givers are new NPCs inside a squad copy; the refinery's navmesh breaks between the landing zone
    /// and the facility, so the givers must stand in the landing zone's part and the mission objects on the facility's
    /// floors; the objects and the Toxicologist are the player's own in a copy that outlives a test; 1079 counts hours,
    /// several a destruction, and 882 opens five objectives at once. An object off the ground, a bomb planted before the
    /// wall, an hour counted twice or not at all, a Toxicologist there too soon, a sample from a creature that is not a
    /// technician, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsTurpisRefineryTests
    {
        private const uint Refinery = PoolsTurpisRefineryNpcsV1.MapContextId;
        private const uint Transport = PoolsTurpisTransportV1.MissionId;
        private const uint BlowUp = PoolsThingsThatBlowUpV1.MissionId;
        private const uint HardCore = PoolsHardCoreV1.MissionId;
        private const uint Chemistry = PoolsChemistry101V1.MissionId;
        private const uint Doctor = PoolsTheBadDoctorV1.MissionId;
        private const uint Business = PoolsCostOfDoingBusinessV1.MissionId;
        private const uint MissionItemFiller = 11519;   // as PlateauCampResistanceTests

        private static readonly Vector3 Door = ToVector(PoolsTurpisRefineryNpcsV1.DoorArrival);
        private static readonly Vector3 Facility = ToVector(PoolsThingsThatBlowUpV1.WallAt);
        private const uint ThraxTechnician = 531047;   // Thrax Technician - Marshes, written out: a wrong id in the data fails
        private static readonly uint[] TechnicianPools = { 531370, 531371, 531372, 531373, 531374, 531375, 531376 };

        /// <summary>
        /// The five givers stand in the landing zone's part of the navmesh, each with its name and level, and offer their
        /// missions with no requirement; Hash stands on the Pools by the door. No Toxicologist before his mission.
        /// </summary>
        [TestMethod]
        public void TheGiversStandAtTheLandingZoneAndHashByTheDoor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hash = Npc(pools, PoolsTurpisRefineryNpcsV1.Hash);
            Assert.AreEqual(PoolsTurpisRefineryNpcsV1.HashName, hash.NameId);
            AssertOnTheGround(pools, ToVector(PoolsTurpisRefineryNpcsV1.HashAt), "Hash");
            var door = harness.World.MapLinkEntries.AsNoTracking().Single(row => row.DestMapContextId == Refinery);
            Assert.IsTrue(Vector3.Distance(hash.Position, new Vector3((float)door.PosX, (float)door.PosY, (float)door.PosZ)) < 15, "by the door");
            Assert.IsTrue(Offers(harness, hash, Transport), "Hash offers Turpis Transport");

            var copy = IntoRefinery(harness);
            foreach (var (pool, name, level, at, mission) in new[]
                {
                    (PoolsTurpisRefineryNpcsV1.Niebolo, PoolsTurpisRefineryNpcsV1.NieboloName, PoolsTurpisRefineryNpcsV1.GiverLevel,
                        PoolsTurpisRefineryNpcsV1.NieboloAt, BlowUp),
                    (PoolsTurpisRefineryNpcsV1.Terentyeva, PoolsTurpisRefineryNpcsV1.TerentyevaName, PoolsTurpisRefineryNpcsV1.GiverLevel,
                        PoolsTurpisRefineryNpcsV1.TerentyevaAt, HardCore),
                    (PoolsTurpisRefineryNpcsV1.Larouche, PoolsTurpisRefineryNpcsV1.LaroucheName, PoolsTurpisRefineryNpcsV1.LaroucheLevel,
                        PoolsTurpisRefineryNpcsV1.LaroucheAt, Chemistry),
                    (PoolsTurpisRefineryNpcsV1.Lindloff, PoolsTurpisRefineryNpcsV1.LindloffName, PoolsTurpisRefineryNpcsV1.GiverLevel,
                        PoolsTurpisRefineryNpcsV1.LindloffAt, Doctor),
                    (PoolsTurpisRefineryNpcsV1.Johansson, PoolsTurpisRefineryNpcsV1.JohanssonName, PoolsTurpisRefineryNpcsV1.GiverLevel,
                        PoolsTurpisRefineryNpcsV1.JohanssonAt, Business),
                })
            {
                var npc = Giver(copy, pool);
                Assert.AreEqual((name, level), (npc.NameId, npc.Level), $"giver {pool}");
                AssertOnTheGround(copy, ToVector(at), $"giver {pool}");
                AssertPath(copy, Door, ToVector(at), $"giver {pool} from the landing spot");
                Assert.IsTrue(Offers(harness, npc, mission), $"{pool} offers {mission}");
            }
            Assert.IsTrue(Vector3.Distance(ToVector(PoolsTurpisRefineryNpcsV1.LaroucheAt), new Vector3(-203.5f, 107.35f, -242.6f)) < 6,
                "Larouche by the medical unit");
            Assert.IsTrue(ToVector(PoolsTurpisRefineryNpcsV1.NieboloAt).Y > Door.Y + 10, "Niebolo up the hill");
            Assert.IsFalse(Here(copy).Any(creature => creature.DbId == PoolsTurpisRefineryNpcsV1.Toxicologist), "no Toxicologist yet");
        }

        /// <summary>1305: Hash offers it; it waits on the Pools, completes on arrival in the refinery, and is turned in by radio, once.</summary>
        [TestMethod]
        public void TurpisTransportCompletesOnArrivalInTheRefinery()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hash = Npc(pools, PoolsTurpisRefineryNpcsV1.Hash);
            Accept(harness, hash, Transport);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Transport, PoolsTurpisTransportV1.Complete));
            Assert.IsFalse(harness.Client.MissionAreaService.RecordArrival(harness.Client), "an arrival on the Pools changes nothing");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Transport, PoolsTurpisTransportV1.Complete), "not on the Pools");

            IntoRefinery(harness);
            Assert.IsTrue(harness.Client.MissionAreaService.RecordArrival(harness.Client), "arrived, as ManifestationManager.MissionArrival tells it");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Transport, PoolsTurpisTransportV1.Complete));
            Assert.IsTrue(harness.Client.Player.Missions[Transport].Completeable);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Transport, null, null));
            harness.Drain();
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Transport, null, null), "and only once");
        }

        /// <summary>
        /// 882: six explosives at the accept; the bombs inside wait for the wall; the wall's bomb opens 3-7; each inside
        /// takes one bomb and completes its own, in any order, once; a reload keeps them; Niebolo pays.
        /// </summary>
        [TestMethod]
        public void ThingsThatBlowUpBlowsTheWallThenPlantsFiveBombs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var niebolo = Giver(copy, PoolsTurpisRefineryNpcsV1.Niebolo);
            var offer = WildernessRanjaGorgeTests.Offer(harness, niebolo, BlowUp);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsThingsThatBlowUpV1.BioGloves, PoolsThingsThatBlowUpV1.GravitonHelmet,
                    PoolsThingsThatBlowUpV1.MechGloves, PoolsThingsThatBlowUpV1.StealthGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, niebolo, BlowUp);
            Assert.AreEqual(PoolsThingsThatBlowUpV1.Explosives, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate));

            var wall = SceneObject(harness, copy, BlowUp, PoolsThingsThatBlowUpV1.WallRole);
            Assert.AreEqual(PoolsThingsThatBlowUpV1.SpotClass, (uint)wall.EntityClassId);
            AssertOnTheGround(copy, wall.Position, "the wall's bomb");
            Assert.IsTrue(Vector3.Distance(wall.Position, ToVector(PoolsTurpisRefineryNpcsV1.NieboloAt)) < 20, "by Niebolo");
            var bombs = Enumerable.Range(0, PoolsThingsThatBlowUpV1.BombsAt.Length)
                .Select(index => SceneObject(harness, copy, BlowUp, PoolsThingsThatBlowUpV1.BombRole(index))).ToArray();
            foreach (var bomb in bombs)
            {
                AssertOnTheGround(copy, bomb.Position, bomb.SceneActorRole);
                AssertPath(copy, Facility, bomb.Position, $"{bomb.SceneActorRole} inside");
            }
            for (var index = 0; index < bombs.Length; index++)
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.BombObjective(index)));

            Use(harness, copy, bombs[0], PoolsThingsThatBlowUpV1.UseArgument);
            Assert.AreEqual(PoolsThingsThatBlowUpV1.Explosives, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate),
                "no bomb inside before the wall");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.BombObjective(0)));

            Use(harness, copy, wall, PoolsThingsThatBlowUpV1.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.WayIn));
            Assert.AreEqual(1U, Counter(harness, BlowUp, PoolsThingsThatBlowUpV1.WayIn), "Door Blown 1/1");
            Assert.AreEqual(PoolsThingsThatBlowUpV1.SpotPlantedState, (uint)wall.StateId);
            for (var index = 0; index < bombs.Length; index++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.BombObjective(index)));

            var order = new[] { 4, 2, 0, 1, 3 };
            for (var step = 0; step < order.Length; step++)
            {
                var index = order[step];
                Use(harness, copy, bombs[index], PoolsThingsThatBlowUpV1.UseArgument);
                Use(harness, copy, bombs[index], PoolsThingsThatBlowUpV1.UseArgument);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.BombObjective(index)), $"bomb {index + 1}");
                Assert.AreEqual(PoolsThingsThatBlowUpV1.Explosives - 2 - (uint)step,
                    WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate), "one bomb a spot");
                if (step == 1)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.BombObjective(order[0])), "kept at a reload");
                }
                if (step < order.Length - 1)
                    Assert.IsFalse(harness.Client.Player.Missions[BlowUp].Completeable);
            }
            Assert.IsTrue(harness.Client.Player.Missions[BlowUp].Completeable);
            FailedTurnInPaysNothing(harness, niebolo, BlowUp, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, niebolo, BlowUp, 3,
                PoolsThingsThatBlowUpV1.Experience, PoolsThingsThatBlowUpV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.StealthGloves));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate), "all six used");
        }

        /// <summary>882 abandoned after the wall: the explosives and spots go; taken again, six explosives and a wall to blow.</summary>
        [TestMethod]
        public void ThingsThatBlowUpAbandonedTakesTheExplosives()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var niebolo = Giver(copy, PoolsTurpisRefineryNpcsV1.Niebolo);
            Accept(harness, niebolo, BlowUp);
            Use(harness, copy, SceneObject(harness, copy, BlowUp, PoolsThingsThatBlowUpV1.WallRole), PoolsThingsThatBlowUpV1.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.WayIn));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, BlowUp));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate), "the explosives go");
            Accept(harness, niebolo, BlowUp);
            Assert.AreEqual(PoolsThingsThatBlowUpV1.Explosives, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.WayIn));
            var wall = SceneObject(harness, copy, BlowUp, PoolsThingsThatBlowUpV1.WallRole);
            Assert.AreEqual(PoolsThingsThatBlowUpV1.SpotReadyState, (uint)wall.StateId, "a wall to blow again");
            Use(harness, copy, wall, PoolsThingsThatBlowUpV1.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BlowUp, PoolsThingsThatBlowUpV1.WayIn), "it counts again");
            Assert.AreEqual(PoolsThingsThatBlowUpV1.Explosives - 1, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate));
        }

        /// <summary>882 with a full bag: no accept (no room for the explosives); with a slot free it is accepted.</summary>
        [TestMethod]
        public void ThingsThatBlowUpWaitsForRoomForTheExplosives()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var niebolo = Giver(copy, PoolsTurpisRefineryNpcsV1.Niebolo);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(niebolo.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, niebolo.EntityId, BlowUp), "no room for the explosives");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BlowUp));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, niebolo, BlowUp);
            Assert.AreEqual(PoolsThingsThatBlowUpV1.Explosives, WildernessRanjaGorgeTests.Held(harness, PoolsThingsThatBlowUpV1.ExplosivesTemplate), "one stack");
        }

        /// <summary>
        /// 884: three crates at Ten Ton Hammer's points; each gives one drive, completes its own and counts one for 1, once;
        /// any order; Terentyeva takes the three and pays.
        /// </summary>
        [TestMethod]
        public void HardCoreTakesThreeHardDrives()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var terentyeva = Giver(copy, PoolsTurpisRefineryNpcsV1.Terentyeva);
            Accept(harness, terentyeva, HardCore);
            var crates = Enumerable.Range(0, PoolsHardCoreV1.DrivesAt.Length)
                .Select(index => SceneObject(harness, copy, HardCore, PoolsHardCoreV1.CrateRole(index))).ToArray();
            foreach (var (crate, (x, z)) in crates.Zip(new[] { (-20f, 175f), (64f, -96f), (-247f, -64f) }))
            {
                Assert.AreEqual(PoolsHardCoreV1.CrateClass, (uint)crate.EntityClassId);
                AssertOnTheGround(copy, crate.Position, crate.SceneActorRole);
                Assert.IsTrue(Vector2.Distance(new Vector2(crate.Position.X, crate.Position.Z), new Vector2(x, z)) < 10,
                    $"{crate.SceneActorRole} at Ten Ton Hammer's point");
            }
            // The navmesh joins Bravo's part to the facility's nowhere: a 4 m step at (158, 108, 32) that the real client
            // walks up, as it walks through the cave's passage by the wall (tester bot 2026-10-28).
            AssertPath(copy, Facility, crates[0].Position, "Alpha's crate in the facility");
            AssertPath(copy, Facility, crates[2].Position, "Charlie's crate in the facility");
            var order = new[] { 2, 0, 1 };
            for (var step = 0; step < order.Length; step++)
            {
                var index = order[step];
                Use(harness, copy, crates[index], PoolsHardCoreV1.UseArgument);
                Use(harness, copy, crates[index], PoolsHardCoreV1.UseArgument);
                Assert.AreEqual((uint)step + 1, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate), "a drive a crate, once");
                Assert.AreEqual((uint)step + 1, Counter(harness, HardCore, PoolsHardCoreV1.Acquire), $"drive {step + 1}");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, HardCore, PoolsHardCoreV1.DrivesAt[index].Objective));
                if (step < order.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HardCore, PoolsHardCoreV1.Acquire));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, HardCore, PoolsHardCoreV1.Acquire));
            Assert.IsTrue(harness.Client.Player.Missions[HardCore].Completeable);
            FailedTurnInPaysNothing(harness, terentyeva, HardCore, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, terentyeva, HardCore, 3,
                PoolsHardCoreV1.Experience, PoolsHardCoreV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.PulseRifle));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate), "Terentyeva has them");
        }

        /// <summary>884: a full bag: a crate gives nothing and stays usable; with a slot free it gives once.</summary>
        [TestMethod]
        public void HardCoreCrateWaitsForAFreeSlot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            Accept(harness, Giver(copy, PoolsTurpisRefineryNpcsV1.Terentyeva), HardCore);
            var crate = SceneObject(harness, copy, HardCore, PoolsHardCoreV1.CrateRole(0));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Use(harness, copy, crate, PoolsHardCoreV1.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate), "no room");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HardCore, PoolsHardCoreV1.Alpha));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, copy, crate, PoolsHardCoreV1.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate), "with room it gives");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, HardCore, PoolsHardCoreV1.Alpha));
            // The drives do not stack: the next one needs a slot of its own.
            var second = SceneObject(harness, copy, HardCore, PoolsHardCoreV1.CrateRole(1));
            Use(harness, copy, second, PoolsHardCoreV1.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate), "no slot for the second");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HardCore, PoolsHardCoreV1.Bravo));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, copy, second, PoolsHardCoreV1.UseArgument);
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate));
        }

        /// <summary>884: a reload keeps two drives; an abandon takes them; taken again, three full crates.</summary>
        [TestMethod]
        public void HardCoreKeepsItsDrivesAtAReloadAndAbandonedTakesThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var terentyeva = Giver(copy, PoolsTurpisRefineryNpcsV1.Terentyeva);
            Accept(harness, terentyeva, HardCore);
            for (var index = 0; index < 2; index++)
                Use(harness, copy, SceneObject(harness, copy, HardCore, PoolsHardCoreV1.CrateRole(index)), PoolsHardCoreV1.UseArgument);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(2U, Counter(harness, HardCore, PoolsHardCoreV1.Acquire), "kept at a reload");
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, HardCore));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsHardCoreV1.DriveTemplate), "the drives go");
            Accept(harness, terentyeva, HardCore);
            Assert.AreEqual(0U, Counter(harness, HardCore, PoolsHardCoreV1.Acquire));
            for (var index = 0; index < PoolsHardCoreV1.DrivesAt.Length; index++)
                Assert.AreEqual(PoolsHardCoreV1.ReadyState,
                    (uint)SceneObject(harness, copy, HardCore, PoolsHardCoreV1.CrateRole(index)).StateId, $"crate {index + 1} full");
            Use(harness, copy, SceneObject(harness, copy, HardCore, PoolsHardCoreV1.CrateRole(0)), PoolsHardCoreV1.UseArgument);
            Assert.AreEqual(1U, Counter(harness, HardCore, PoolsHardCoreV1.Acquire), "it counts again");
        }

        /// <summary>
        /// 885: a technician killed before the accept drops nothing; after it, each technician's corpse holds one sample of
        /// each, once; a soldier's none; five technicians complete both; Larouche takes the ten and pays.
        /// </summary>
        [TestMethod]
        public void ChemistryTakesFiveOfEachSampleFromTheTechnicians()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var before = Kill(harness, copy, Technician(harness, copy, TechnicianPools[0]));
            Assert.IsFalse(before != null && before.LootItems.Any(item => item.ItemTemplateId == PoolsChemistry101V1.RefinedTemplate ||
                item.ItemTemplateId == PoolsChemistry101V1.UnrefinedTemplate), "no sample before the accept");
            var larouche = Giver(copy, PoolsTurpisRefineryNpcsV1.Larouche);
            Accept(harness, larouche, Chemistry);

            var soldier = Kill(harness, copy, Pooled(harness, copy, TechnicianPools[1], ThraxTechnician, false));
            Assert.IsFalse(soldier != null && soldier.LootItems.Any(item => item.ItemTemplateId == PoolsChemistry101V1.RefinedTemplate),
                "a soldier carries none");
            for (var kill = 1U; kill <= PoolsChemistry101V1.Samples; kill++)
            {
                var corpse = Kill(harness, copy, Technician(harness, copy, TechnicianPools[(int)kill % TechnicianPools.Length]));
                Assert.IsNotNull(corpse, "a technician's corpse");
                PickUp(harness, corpse);
                harness.Drain();
                PickUp(harness, corpse);
                harness.Drain();
                Assert.AreEqual(kill, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.RefinedTemplate), $"refined {kill}, once a corpse");
                Assert.AreEqual(kill, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.UnrefinedTemplate), $"unrefined {kill}");
                if (kill < PoolsChemistry101V1.Samples)
                    Assert.IsFalse(harness.Client.Player.Missions[Chemistry].Completeable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Chemistry, PoolsChemistry101V1.Refined));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Chemistry, PoolsChemistry101V1.Unrefined));
            FailedTurnInPaysNothing(harness, larouche, Chemistry, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, larouche, Chemistry, 1,
                PoolsChemistry101V1.Experience, PoolsChemistry101V1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.HazmatVest));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.RefinedTemplate), "Larouche has them");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.UnrefinedTemplate));
        }

        /// <summary>885: a reload keeps three of each; an abandon leaves the samples held, and taken again they count at once.</summary>
        [TestMethod]
        public void ChemistryKeepsItsSamplesAtAReloadAndAnAbandon()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var larouche = Giver(copy, PoolsTurpisRefineryNpcsV1.Larouche);
            Accept(harness, larouche, Chemistry);
            for (var kill = 0; kill < 3; kill++)
            {
                PickUp(harness, Kill(harness, copy, Technician(harness, copy, TechnicianPools[kill])));
                harness.Drain();
            }
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.RefinedTemplate), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Chemistry, PoolsChemistry101V1.Refined));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Chemistry));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.RefinedTemplate), "the samples stay");
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, PoolsChemistry101V1.UnrefinedTemplate));
            Accept(harness, larouche, Chemistry);
            for (var kill = 3; kill < PoolsChemistry101V1.Samples; kill++)
            {
                PickUp(harness, Kill(harness, copy, Technician(harness, copy, TechnicianPools[kill])));
                harness.Drain();
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Chemistry, PoolsChemistry101V1.Refined), "the held ones count");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Chemistry, PoolsChemistry101V1.Unrefined));
        }

        /// <summary>
        /// 886: no Toxicologist at the accept; he comes when the player comes within his area, hostile, on the facility's
        /// floor; a reload keeps him; his death completes 1; Lindloff pays.
        /// </summary>
        [TestMethod]
        public void TheBadDoctorComesWhenThePlayerComesNear()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var lindloff = Giver(copy, PoolsTurpisRefineryNpcsV1.Lindloff);
            Accept(harness, lindloff, Doctor);
            Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Doctor, PoolsTheBadDoctorV1.ToxicologistRole), "not before the player comes near");
            var lair = ToVector(PoolsTheBadDoctorV1.ToxicologistAt);
            AssertOnTheGround(copy, lair, "the Toxicologist's place");
            AssertPath(copy, Facility, lair, "the Toxicologist's place in the facility");
            Assert.IsTrue(Vector2.Distance(new Vector2(lair.X, lair.Z), new Vector2(133, -25)) < 6, "Ten Ton Hammer's point");

            Enter(harness, lair + new Vector3(15, 0, 0));
            Tick(harness, copy, 250);
            var toxicologist = SceneCreature(harness, copy, Doctor, PoolsTheBadDoctorV1.ToxicologistRole);
            Assert.IsNotNull(toxicologist, "he comes");
            Assert.AreEqual((PoolsTurpisRefineryNpcsV1.ToxicologistName, PoolsTurpisRefineryNpcsV1.ToxicologistLevel),
                (toxicologist.NameId, toxicologist.Level));
            Assert.AreEqual((int)PoolsTurpisRefineryNpcsV1.ToxicologistHitPoints, (int)toxicologist.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, toxicologist.TargetCategory);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Doctor, PoolsTheBadDoctorV1.Kill));

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.IsNotNull(SceneCreature(harness, copy, Doctor, PoolsTheBadDoctorV1.ToxicologistRole), "still there at a reload");
            Kill(harness, copy, toxicologist);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Doctor, PoolsTheBadDoctorV1.Kill));
            FailedTurnInPaysNothing(harness, lindloff, Doctor, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lindloff, Doctor, 2,
                PoolsTheBadDoctorV1.Experience, PoolsTheBadDoctorV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTheBadDoctorV1.MotorAssistBoots));
        }

        /// <summary>886 abandoned after he came: he goes; taken again, he waits for the player to come near once more.</summary>
        [TestMethod]
        public void TheBadDoctorAbandonedTakesTheToxicologist()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var lindloff = Giver(copy, PoolsTurpisRefineryNpcsV1.Lindloff);
            Accept(harness, lindloff, Doctor);
            var lair = ToVector(PoolsTheBadDoctorV1.ToxicologistAt);
            Enter(harness, lair);
            Tick(harness, copy, 250);
            var toxicologist = SceneCreature(harness, copy, Doctor, PoolsTheBadDoctorV1.ToxicologistRole);
            Assert.IsNotNull(toxicologist);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Doctor));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(Here(copy).Contains(toxicologist), "he goes");
            Accept(harness, lindloff, Doctor);
            Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Doctor, PoolsTheBadDoctorV1.ToxicologistRole), "not before the player comes near again");
            Enter(harness, lair);
            Tick(harness, copy, 250);
            Assert.IsNotNull(SceneCreature(harness, copy, Doctor, PoolsTheBadDoctorV1.ToxicologistRole), "he comes again");
        }

        /// <summary>
        /// 1079: twenty-two objects on the facility's floors, 72 hours in all; a hit that leaves one standing counts
        /// nothing; each destruction adds its hours to "Hours to Reconstruct", once; a reload and a way out and back keep
        /// them; the last completes 2; Johansson pays.
        /// </summary>
        [TestMethod]
        public void CostOfDoingBusinessCountsSeventyTwoHours()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var johansson = Giver(copy, PoolsTurpisRefineryNpcsV1.Johansson);
            Accept(harness, johansson, Business);
            Assert.AreEqual(PoolsCostOfDoingBusinessV1.Hours, (uint)PoolsCostOfDoingBusinessV1.Objects.Sum(entry => entry.Hours), "72 hours in all");
            Assert.AreEqual(6, PoolsCostOfDoingBusinessV1.Objects.Select(entry => entry.Hours).Distinct().Count(), "six kinds, six costs");
            var objects = ObjectsOf(harness, copy);
            for (var index = 0; index < objects.Length; index++)
            {
                Assert.AreEqual(PoolsCostOfDoingBusinessV1.Objects[index].Class, (uint)objects[index].EntityClassId);
                Assert.AreEqual(harness.Client.Player.Id, objects[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(copy, objects[index].Position, objects[index].SceneActorRole);
                AssertPath(copy, Facility, objects[index].Position, $"{objects[index].SceneActorRole} in the facility");
            }

            var hours = 0U;
            for (var index = 0; index < objects.Length; index++)
            {
                Damage(harness, copy, objects[index], 60);
                Assert.AreEqual(hours, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction), "not at 60 of 100");
                Damage(harness, copy, objects[index], 60);
                hours += PoolsCostOfDoingBusinessV1.Objects[index].Hours;
                Assert.AreEqual(hours, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction), $"object {index + 1}: its hours");
                Damage(harness, copy, objects[index], 100);
                Assert.AreEqual(hours, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction), "counted once");
                if (index == 10)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(hours, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction), "kept at a reload");
                    OutOfRefinery(harness, copy);
                    copy = IntoRefinery(harness);
                    objects = ObjectsOf(harness, copy);
                    Assert.AreEqual(11, objects.Count(entry => entry.CurrentHitPoints == 0), "the eleven stay destroyed");
                }
                if (index < objects.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Business, PoolsCostOfDoingBusinessV1.Destruction));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Business, PoolsCostOfDoingBusinessV1.Destruction));
            Assert.IsTrue(harness.Client.Player.Missions[Business].Completeable);
            FailedTurnInPaysNothing(harness, johansson, Business, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, johansson, Business, 1,
                PoolsCostOfDoingBusinessV1.Experience, PoolsCostOfDoingBusinessV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsCostOfDoingBusinessV1.RadialHealingDisc));
        }

        /// <summary>1079 abandoned: the objects go; taken again, twenty-two whole ones and no hours.</summary>
        [TestMethod]
        public void CostOfDoingBusinessAbandonedTakesTheObjects()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoRefinery(harness);
            var johansson = Giver(copy, PoolsTurpisRefineryNpcsV1.Johansson);
            Accept(harness, johansson, Business);
            var objects = ObjectsOf(harness, copy);
            Damage(harness, copy, objects[6], 100);
            Assert.AreEqual(8U, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction), "a lab table, 8 hours");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Business));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(objects.Any(entry => entry.IsInWorld), "the objects go");
            Accept(harness, johansson, Business);
            Assert.AreEqual(0U, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction));
            objects = ObjectsOf(harness, copy);
            Assert.IsTrue(objects.All(entry => entry.CurrentHitPoints > 0), "twenty-two whole objects");
            Damage(harness, copy, objects[6], 100);
            Assert.AreEqual(8U, Counter(harness, Business, PoolsCostOfDoingBusinessV1.Destruction), "the lab table counts again");
        }

        private static DynamicObject[] ObjectsOf(WildernessRuntimeTestHarness harness, MapChannel copy) =>
            Enumerable.Range(0, PoolsCostOfDoingBusinessV1.Objects.Length)
                .Select(index => SceneObject(harness, copy, Business, PoolsCostOfDoingBusinessV1.ObjectRole(index))).ToArray();

        private static MapChannel IntoRefinery(WildernessRuntimeTestHarness harness)
        {
            PlateauValverdePoolsTests.OntoPools(harness);
            if (!harness.Maps.MapChannelArray.ContainsKey(Refinery))
                LoadMap(harness, Refinery);
            var copy = ThroughTheDoor(harness, Refinery);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.Scenes.Resume(harness.Client);   // as MapChannelManager.MapLoaded does (ResumeMissionScenes)
            harness.Drain();
            return copy;
        }

        /// <summary>Back to the Pools, the scene detached from the copy as a map change does.</summary>
        private static void OutOfRefinery(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            PlateauValverdePoolsTests.OntoPools(harness);
        }

        /// <summary>Into a mission area at <paramref name="at"/>, as MissionAreaService takes a step from far off.</summary>
        private static void Enter(WildernessRuntimeTestHarness harness, Vector3 at)
        {
            var from = at + new Vector3(60, 0, 60);
            harness.MoveTo(at);
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, from, at);
            harness.Drain();
        }

        /// <summary>The living creatures of this copy only (PlateauUstorYardTests.Here).</summary>
        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static Creature Giver(MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool))
                Spawn(copy, pool);
            var giver = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(giver, $"giver {pool} stands in the copy");
            return giver;
        }

        /// <summary>The NPC of the pool on a public map.</summary>
        private static Creature Npc(MapChannel map, uint pool)
        {
            Spawn(map, pool);
            var npc = Living(map).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"pool {pool} stands an NPC");
            return npc;
        }

        private static Creature Technician(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool) =>
            Pooled(harness, copy, pool, ThraxTechnician, true);

        /// <summary>
        /// A living creature of <paramref name="pool"/> that is (or is not) of <paramref name="kind"/>. A squad copy
        /// outlives a test, and a cleared pool in it comes back only after the squad's respawn time (SquadInstanceState):
        /// one without such a creature is set back as the weekly reset does it, then spawned.
        /// </summary>
        private static Creature Pooled(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool, uint kind, bool of)
        {
            bool Wanted(Creature creature) => creature.SpawnPool?.DbId == pool && creature.State != CharacterState.Dead &&
                (creature.DbId == kind) == of;
            for (var attempt = 0; attempt < 3 && !Here(copy).Any(Wanted); attempt++)
            {
                foreach (var stale in Here(copy).Where(creature => creature.SpawnPool?.DbId == pool && creature.State != CharacterState.Dead).ToArray())
                {
                    stale.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(copy, stale, harness.Client.Player);
                }
                var spawnPool = copy.SpawnPools.Single(candidate => candidate.DbId == pool);
                spawnPool.ClearedAtUtcMs = 0;
                spawnPool.UpdateTimer = spawnPool.RespawnTime;
                Spawn(copy, pool);
            }
            var creature = Here(copy).FirstOrDefault(Wanted);
            Assert.IsNotNull(creature, $"pool {pool} stands the creature wanted");
            return creature;
        }

        private static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>The object of this test's own run: a squad copy can still hold an earlier test's.</summary>
        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            var obj = map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>Kills the creature as the player and runs the scene on; returns its corpse, if it has one (PlateauUstorYardTests.Kill).</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
            return map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot) ? loot : null;
        }

        /// <summary>As PlateauUstorYardTests.Tick.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauUstorYardTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        /// <summary>A weapon hit on the object (PlateauUstorYardTests.Damage).</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        private static void FailedTurnInPaysNothing(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice)
        {
            harness.MoveTo(receiver.Position);
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException($"Injected {mission} turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            harness.Drain();
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        private static void AssertOnTheGround(MapChannel map, Vector3 position, string what)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        /// <summary>A complete path on the navmesh from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static void AssertPath(MapChannel map, Vector3 from, Vector3 to, string what) =>
            Assert.IsTrue(map.NavMesh.FindPath(from, to, out var complete) != null && complete, $"{what}: a path");

        private static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static Vector3 ToVector(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);
    }
}
