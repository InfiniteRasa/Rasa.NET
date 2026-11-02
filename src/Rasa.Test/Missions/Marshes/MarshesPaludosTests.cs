using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static PlateauCampResistanceTests;
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// Marshes set 1 at Paludos, played through as a player plays it: Elder Toida's Roll out the Welcome Wagon (541: kill
    /// Overseer Cast'ka) and Divided We Fall (917: talk to Alia, Siva and Veska, find Helka's body), Sergeant Starling's
    /// Cannister Collection (498: a canister from the Bane) and Shaman Geli's What's that Gas? (539: four parts from the
    /// marsh's creatures).
    ///
    /// Why these tests: three NPCs and six pools are new on the server, Alia walks a beat, the drops come only from the
    /// named creatures at their chance, and Helka's body is the player's own scene object. An NPC off the navmesh, a talk
    /// at the wrong NPC, a drop from the wrong creature or on the wrong side of its chance, a kill counted as a pickup, a
    /// body searched twice, progress lost at a reload, or a turn-in that pays twice would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesPaludosTests
    {
        private const uint WelcomeWagon = MarshesWelcomeWagonV1.MissionId;
        private const uint Cannister = MarshesCannisterCollectionV1.MissionId;
        private const uint Gas = MarshesWhatsThatGasV1.MissionId;
        private const uint Divided = MarshesDividedWeFallV1.MissionId;
        private const uint Toida = 510160, Starling = 510171, Geli = 510172;
        private const uint ThraxCaretaker = 531048;        // a Bane of the Marshes that is no Thrax Soldier or Grenadier
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Paludos waypoint (World DB teleporter 135), where a player arrives.</summary>
        private static readonly Vector3 PaludosWaypoint = new(-304.35f, 218.3f, 97.36f);

        /// <summary>A point on the road out of Paludos (Alia's beat): Paludos' own navmesh is walled off from the marsh.</summary>
        private static readonly Vector3 Road = new(-420f, 216.9f, 472.4f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void SivaVeskaAndAliaStandOnTheNavmeshAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (pool, name, package, at, from) in new[]
            {
                (MarshesPaludosNpcsV1.Siva, MarshesPaludosNpcsV1.SivaName, MarshesPaludosNpcsV1.SivaPackage, MarshesPaludosNpcsV1.SivaAt, PaludosWaypoint),
                (MarshesPaludosNpcsV1.Veska, MarshesPaludosNpcsV1.VeskaName, MarshesPaludosNpcsV1.VeskaPackage, MarshesPaludosNpcsV1.VeskaAt, Road)
            })
            {
                var npc = Npc(marshes, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at its place");
                AssertOnGround(marshes, npc.Position, $"{pool}");
                Assert.IsTrue(marshes.NavMesh.FindPath(from, npc.Position, out var complete) != null && complete, $"{pool} is walked to");
            }

            // Alia: a squad of one, a bystander, friendly, with her package, on a beat of four road corners walked there and back.
            var alia = Alia(harness, marshes);
            Assert.AreEqual(MarshesPaludosNpcsV1.AliaName, alia.NameId);
            Assert.AreEqual(MarshesPaludosNpcsV1.AliaPackage, alia.Npc.NpcPackageId);
            Assert.IsTrue(S.IsBystander(alia), "she takes up no fight");
            Assert.AreNotEqual(TargetCategory.Hostile, alia.TargetCategory);
            Assert.AreEqual(S.PathMode.Back, S.Paths[MarshesPaludosNpcsV1.AliaSquad].Mode);
            var beat = MarshesPaludosNpcsV1.AliaBeat;
            for (var step = 0; step < beat.Length; step++)
            {
                var at = new Vector3((float)beat[step].X, (float)beat[step].Y, (float)beat[step].Z);
                AssertOnGround(marshes, at, $"step {step}");
                if (step < beat.Length - 1)
                    Assert.IsTrue(marshes.NavMesh.FindPath(at, new Vector3((float)beat[step + 1].X, (float)beat[step + 1].Y,
                        (float)beat[step + 1].Z), out var complete) != null && complete, $"step {step} to the next is walked");
            }
            Assert.IsTrue(marshes.NavMesh.FindPath(Road, alia.Position, out var reached) != null && reached, "Alia is walked to");

            var start = alia.Position;
            var farthest = 0f;
            for (var tick = 0; tick < 240; tick++)
            {
                PlateauGiantKillerSquadTests.Step(harness, marshes, 250);
                farthest = System.Math.Max(farthest, Vector3.Distance(start, alia.Position));
            }
            Assert.IsGreaterThan(20f, farthest, $"Alia walked {farthest:0} m");
            Assert.IsLessThan(MarshesPaludosNpcsV1.AliaBeatRadius, Vector2.Distance(new(alia.Position.X, alia.Position.Z),
                new((float)MarshesPaludosNpcsV1.AliaBeatCentre.X, (float)MarshesPaludosNpcsV1.AliaBeatCentre.Z)), "still inside her marker");
        }

        [TestMethod]
        public void RollOutTheWelcomeWagonTakesCastkasDeathToToida()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, Toida);
            var starling = Npc(marshes, Starling);
            Spawn(marshes, MarshesWelcomeWagonV1.Castka);
            AssertOnGround(marshes, Living(marshes).Single(creature => creature.DbId == MarshesWelcomeWagonV1.Castka).Position, "Cast'ka");

            var offer = WildernessRanjaGorgeTests.Offer(harness, toida, WelcomeWagon);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesWelcomeWagonV1.LaserChaingun, MarshesWelcomeWagonV1.IncendiaryNetGun,
                    MarshesWelcomeWagonV1.ExothermicPolarityGun, MarshesWelcomeWagonV1.SonicShotgun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, starling, WelcomeWagon), "not Starling's to give");
            Accept(harness, toida, WelcomeWagon);

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, WelcomeWagon, MarshesWelcomeWagonV1.Kill), "kept at a reload");
            harness.Drain();

            Kill(harness, marshes, MarshesWelcomeWagonV1.Castka);
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == WelcomeWagon));
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == WelcomeWagon && packet.IsCompleteable));
            Assert.IsTrue(harness.Client.Player.Missions[WelcomeWagon].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, starling.EntityId, WelcomeWagon, 0), "not Starling's to take");

            FailedTurnInPaysNothing(harness, toida, WelcomeWagon, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, toida, WelcomeWagon, 1,
                MarshesWelcomeWagonV1.Experience, MarshesWelcomeWagonV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == WelcomeWagon));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesWelcomeWagonV1.IncendiaryNetGun), "the selected net gun");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, toida, WelcomeWagon), "done once");
        }

        /// <summary>An abandon takes the kill back: offered again, and taken again Cast'ka must die again.</summary>
        [TestMethod]
        public void RollOutTheWelcomeWagonAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, Toida);
            Spawn(marshes, MarshesWelcomeWagonV1.Castka);
            Accept(harness, toida, WelcomeWagon);
            Kill(harness, marshes, MarshesWelcomeWagonV1.Castka);
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[WelcomeWagon].Completeable);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, WelcomeWagon));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(WelcomeWagon), "gone from the log");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, toida, WelcomeWagon), "offered again after an abandon");
            Accept(harness, toida, WelcomeWagon);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, WelcomeWagon, MarshesWelcomeWagonV1.Kill), "the old kill does not count");
            Assert.IsFalse(harness.Client.Player.Missions[WelcomeWagon].Completeable);
        }

        [TestMethod]
        public void CannisterCollectionTakesABaneCanisterToStarling()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var marshes = EnterMarshes(harness);
            var starling = Npc(marshes, Starling);
            var toida = Npc(marshes, Toida);
            var geli = Npc(marshes, Geli);
            var bane = PoolsOf(harness, MarshesCannisterCollectionV1.ThraxSoldier, MarshesCannisterCollectionV1.ThraxGrenadier, ThraxCaretaker);
            Spawn(marshes, bane);

            var offer = WildernessRanjaGorgeTests.Offer(harness, starling, Cannister);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesCannisterCollectionV1.CryogenicInjectorGun, MarshesCannisterCollectionV1.PulseChaingun,
                    MarshesCannisterCollectionV1.ElectricRifle, MarshesCannisterCollectionV1.RocketLauncher
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesCannisterCollectionV1.ThraxSoldier), MarshesCannisterCollectionV1.CanisterTemplate),
                "no drop before the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, toida, Cannister), "not Toida's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, geli, Cannister), "not Geli's to give");
            Accept(harness, starling, Cannister);

            Assert.IsFalse(Holds(Kill(harness, marshes, ThraxCaretaker), MarshesCannisterCollectionV1.CanisterTemplate), "a Caretaker has none");
            percentile = MarshesCannisterCollectionV1.DropChance;
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesCannisterCollectionV1.ThraxSoldier), MarshesCannisterCollectionV1.CanisterTemplate), "25 misses");
            percentile = MarshesCannisterCollectionV1.DropChance - 1;
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            var loot = Kill(harness, marshes, MarshesCannisterCollectionV1.ThraxGrenadier);
            Assert.IsTrue(Holds(loot, MarshesCannisterCollectionV1.CanisterTemplate), "24 drops, from a Grenadier too");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate), "a death is not a pickup");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Cannister, MarshesCannisterCollectionV1.Collect));
            harness.Drain();
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate));
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Cannister));
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Cannister && packet.IsCompleteable));
            foreach (var other in new[] { toida, geli })
            {
                harness.MoveTo(other.Position);
                harness.Drain();
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, other.EntityId, Cannister, 2), $"not {other.DbId}'s to take");
            }

            FailedTurnInPaysNothing(harness, starling, Cannister, 2);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, starling, Cannister, 2,
                MarshesCannisterCollectionV1.Experience, MarshesCannisterCollectionV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Cannister));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.ElectricRifle), "the selected rifle");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate), "Starling keeps it");
        }

        /// <summary>The canister stays on the corpse while the bag is full; an abandoned mission is offered again.</summary>
        [TestMethod]
        public void TheCanisterWaitsForRoomAndCannisterCollectionCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var marshes = EnterMarshes(harness);
            var starling = Npc(marshes, Starling);
            Spawn(marshes, PoolsOf(harness, MarshesCannisterCollectionV1.ThraxSoldier));
            Accept(harness, starling, Cannister);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, marshes, MarshesCannisterCollectionV1.ThraxSoldier);
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate), "no room");
            Assert.IsTrue(Holds(loot, MarshesCannisterCollectionV1.CanisterTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Cannister));
            harness.Drain();
            // The binding retains the canister at an abandon (Abandonment Retain, as Pools 1314's parts).
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate), "kept at the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, starling, Cannister), "offered again after an abandon");

            // Taken again, the canister already held counts: the mission is ready to turn in at once.
            Accept(harness, starling, Cannister);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Cannister, MarshesCannisterCollectionV1.Collect), "the held canister counts");
            Assert.IsTrue(harness.Client.Player.Missions[Cannister].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, starling, Cannister, 0,
                MarshesCannisterCollectionV1.Experience, MarshesCannisterCollectionV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesCannisterCollectionV1.CanisterTemplate), "Starling keeps it");
        }

        [TestMethod]
        public void WhatsThatGasTakesTheMarshsSamplesToGeli()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var marshes = EnterMarshes(harness);
            var geli = Npc(marshes, Geli);
            var starling = Npc(marshes, Starling);
            var pools = MarshesPaludosNpcsV1.GasPools.Select(pool => pool.Id).ToArray();
            Spawn(marshes, pools);
            foreach (var (id, _, x, y, z) in MarshesPaludosNpcsV1.GasPools)
            {
                AssertOnGround(marshes, new Vector3((float)x, (float)y, (float)z), $"pool {id}");
                Assert.IsTrue(marshes.NavMesh.FindPath(Road, new Vector3((float)x, (float)y, (float)z), out var complete) != null && complete,
                    $"pool {id} is walked to");
            }

            var offer = WildernessRanjaGorgeTests.Offer(harness, geli, Gas);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesWhatsThatGasV1.CryogenicLauncher, MarshesWhatsThatGasV1.IncendiaryLeechGun,
                    MarshesWhatsThatGasV1.ElectricNetGun, MarshesWhatsThatGasV1.Pistol
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesWhatsThatGasV1.SwampGrubber), MarshesWhatsThatGasV1.HideTemplate),
                "no drop before the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, starling, Gas), "not Starling's to give");
            Accept(harness, geli, Gas);

            // A Grubber always gives a hide, a frond only under 50; a Maw's gland and a Mox's heart only under 50.
            percentile = 50;
            var grubber = Kill(harness, marshes, MarshesWhatsThatGasV1.SwampGrubber);
            Assert.IsTrue(Holds(grubber, MarshesWhatsThatGasV1.HideTemplate), "a hide at 50");
            Assert.IsFalse(Holds(grubber, MarshesWhatsThatGasV1.FernTemplate), "no frond at 50");
            Assert.IsFalse(Holds(grubber, MarshesWhatsThatGasV1.GlandTemplate), "a Grubber has no gland");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.HideTemplate), "a death is not a pickup");
            PickUp(harness, grubber);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.HideTemplate));
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesWhatsThatGasV1.Maws[0]), MarshesWhatsThatGasV1.GlandTemplate), "no gland at 50");
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesWhatsThatGasV1.Mox), MarshesWhatsThatGasV1.HeartTemplate), "no heart at 50");

            percentile = 49;
            var completed = new List<uint>();
            var completeable = 0;
            foreach (var (creature, template, count, objective) in new[]
            {
                (MarshesWhatsThatGasV1.SwampGrubber, MarshesWhatsThatGasV1.FernTemplate, MarshesWhatsThatGasV1.FernCount, MarshesWhatsThatGasV1.Ferns),
                (MarshesWhatsThatGasV1.SwampGrubber, MarshesWhatsThatGasV1.HideTemplate, MarshesWhatsThatGasV1.HideCount, MarshesWhatsThatGasV1.Hides),
                (MarshesWhatsThatGasV1.Maws[0], MarshesWhatsThatGasV1.GlandTemplate, MarshesWhatsThatGasV1.GlandCount, MarshesWhatsThatGasV1.Glands),
                (MarshesWhatsThatGasV1.Mox, MarshesWhatsThatGasV1.HeartTemplate, MarshesWhatsThatGasV1.HeartCount, MarshesWhatsThatGasV1.Hearts)
            })
            {
                while (WildernessRanjaGorgeTests.Held(harness, template) < count)
                {
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Gas, objective), $"{objective} open");
                    if (!Alive(marshes).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                        Respawn(marshes, pools);
                    var loot = Kill(harness, marshes, creature);
                    Assert.IsTrue(Holds(loot, template), $"{template} at 49");
                    PickUp(harness, loot);
                    var drained = harness.Drain();
                    completed.AddRange(drained.OfType<ObjectiveCompletedPacket>()
                        .Where(packet => packet.MissionId == Gas).Select(packet => packet.ObjectiveId));
                    completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Gas && packet.IsCompleteable);
                    if (objective == MarshesWhatsThatGasV1.Glands && WildernessRanjaGorgeTests.Held(harness, template) == 2)
                    {
                        WildernessRanjaGorgeTests.Reload(harness);
                        Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, template), "kept at a reload");
                        harness.Drain();
                    }
                }
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Gas, objective), $"{objective} done");
            }
            CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U, 4U }, completed.ToArray(), "one ObjectiveCompleted each");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");
            Assert.IsTrue(harness.Client.Player.Missions[Gas].Completeable);
            harness.MoveTo(starling.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, starling.EntityId, Gas, 3), "not Starling's to take");

            FailedTurnInPaysNothing(harness, geli, Gas, 3);
            Assert.AreEqual(MarshesWhatsThatGasV1.HeartCount, WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.HeartTemplate));
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, geli, Gas, 3,
                MarshesWhatsThatGasV1.Experience, MarshesWhatsThatGasV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Gas));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.Pistol), "the selected pistol");
            foreach (var template in new[] { MarshesWhatsThatGasV1.FernTemplate, MarshesWhatsThatGasV1.GlandTemplate, MarshesWhatsThatGasV1.HeartTemplate })
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, template), $"Geli keeps {template}");
        }

        /// <summary>A Grubber's parts stay on the corpse while the bag is full; an abandon keeps the parts and the mission is offered again.</summary>
        [TestMethod]
        public void WhatsThatGasPartsWaitForRoomAndTheMissionCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var marshes = EnterMarshes(harness);
            var geli = Npc(marshes, Geli);
            Spawn(marshes, MarshesPaludosNpcsV1.GasPools.Select(pool => pool.Id).ToArray());
            Accept(harness, geli, Gas);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, marshes, MarshesWhatsThatGasV1.SwampGrubber);
            PickUp(harness, loot);
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.HideTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.FernTemplate)), "no room");
            Assert.IsTrue(Holds(loot, MarshesWhatsThatGasV1.HideTemplate) && Holds(loot, MarshesWhatsThatGasV1.FernTemplate), "both still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual((1U, 1U), (WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.HideTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.FernTemplate)), "both picked up with room for them");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Gas));
            harness.Drain();
            // The bindings retain the parts at an abandon (Abandonment Retain, as Pools 1314's parts).
            Assert.AreEqual((1U, 1U), (WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.HideTemplate),
                WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.FernTemplate)), "kept at the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, geli, Gas), "offered again after an abandon");
            Accept(harness, geli, Gas);
            foreach (var objective in new[] { MarshesWhatsThatGasV1.Ferns, MarshesWhatsThatGasV1.Hides, MarshesWhatsThatGasV1.Glands, MarshesWhatsThatGasV1.Hearts })
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Gas, objective), $"{objective} open again (one part held is not five or ten)");
        }

        /// <summary>The second Maw row of the Marshes (540012, the open world's other Maw pools) gives glands too.</summary>
        [TestMethod]
        public void EitherMawGivesAGland()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var marshes = EnterMarshes(harness);
            var geli = Npc(marshes, Geli);
            Spawn(marshes, PoolsOf(harness, MarshesWhatsThatGasV1.Maws[1]));
            Accept(harness, geli, Gas);
            var loot = Kill(harness, marshes, MarshesWhatsThatGasV1.Maws[1]);
            Assert.IsTrue(Holds(loot, MarshesWhatsThatGasV1.GlandTemplate));
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesWhatsThatGasV1.GlandTemplate));
        }

        [TestMethod]
        public void DividedWeFallFindsToidasFamily()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, Toida);
            var siva = Npc(marshes, MarshesPaludosNpcsV1.Siva);
            var veska = Npc(marshes, MarshesPaludosNpcsV1.Veska);
            var alia = Alia(harness, marshes);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Divided), "no body before the mission");
            Assert.IsFalse(Conversation(harness, siva).ContainsKey(ConversationType.ObjectiveComplete), "no talk before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, toida, Divided);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesDividedWeFallV1.HazmatBoots, MarshesDividedWeFallV1.MotorAssistBoots,
                    MarshesDividedWeFallV1.ReflectiveBoots, MarshesDividedWeFallV1.StealthLegs
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, toida, Divided);

            // Each sibling hears only their own objective; Toida hears none of them.
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, siva.EntityId, Divided, MarshesDividedWeFallV1.LocateVeska, 1),
                "not Siva's to hear");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, toida.EntityId, Divided, MarshesDividedWeFallV1.LocateSiva, 1),
                "not Toida's to hear");
            Talk(harness, siva, Divided, MarshesDividedWeFallV1.LocateSiva);
            Talk(harness, veska, Divided, MarshesDividedWeFallV1.LocateVeska);

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Divided, MarshesDividedWeFallV1.LocateSiva), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Divided, MarshesDividedWeFallV1.LocateAlia));
            harness.Drain();

            Talk(harness, alia, Divided, MarshesDividedWeFallV1.LocateAlia);
            Assert.IsFalse(harness.Client.Player.Missions[Divided].Completeable, "Helka is still to find");

            // Helka: the player's own body and pack at TaRapedia's place, on the navmesh; one search finds her.
            var (corpse, pack) = Helka(harness, marshes);
            harness.MoveTo(pack.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, pack, MarshesDividedWeFallV1.UseArgument, recovers: true);
            Assert.AreEqual((UseObjectState)MarshesDividedWeFallV1.TakenState, pack.StateId);
            Assert.IsFalse(pack.IsEnabled, "searched once");
            Assert.IsFalse(corpse.IsEnabled, "the body itself takes no use");
            Use(harness, marshes, pack, MarshesDividedWeFallV1.UseArgument, recovers: false);
            var packets = harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Divided, MarshesDividedWeFallV1.LocateHelka));
            Assert.IsTrue(harness.Client.Player.Missions[Divided].Completeable);

            FailedTurnInPaysNothing(harness, toida, Divided, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, toida, Divided, 0,
                MarshesDividedWeFallV1.Experience, MarshesDividedWeFallV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Divided));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesDividedWeFallV1.HazmatBoots), "the selected boots");
        }

        /// <summary>The packets of a run: MissionGained once, ObjectiveCompleted once each, MissionCompleteable once.</summary>
        [TestMethod]
        public void DividedWeFallSendsEachPacketOnce()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, Toida);
            var siva = Npc(marshes, MarshesPaludosNpcsV1.Siva);
            var veska = Npc(marshes, MarshesPaludosNpcsV1.Veska);
            var alia = Alia(harness, marshes);
            harness.MoveTo(toida.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, toida.EntityId, Divided));
            var packets = new List<object>(harness.Drain());
            foreach (var (npc, objective) in new[]
            {
                (veska, MarshesDividedWeFallV1.LocateVeska), (alia, MarshesDividedWeFallV1.LocateAlia), (siva, MarshesDividedWeFallV1.LocateSiva)
            })
            {
                harness.MoveTo(npc.Position);
                harness.Drain();
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Divided, objective, 1), $"{objective}");
                packets.AddRange(harness.Drain());
            }
            var (_, pack) = Helka(harness, marshes);
            harness.MoveTo(pack.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, pack, MarshesDividedWeFallV1.UseArgument, recovers: true);
            packets.AddRange(harness.Drain());

            Assert.AreEqual(1, packets.OfType<MissionGainedPacket>().Count(packet => packet.MissionId == Divided), "MissionGained once");
            CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U, 4U }, packets.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == Divided).Select(packet => packet.ObjectiveId).ToArray(), "ObjectiveCompleted once each");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Divided && packet.IsCompleteable),
                "MissionCompleteable once");
        }

        /// <summary>An abandon takes the body away and the talks back; taken again, the body stands whole and Siva talks again.</summary>
        [TestMethod]
        public void DividedWeFallAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, Toida);
            var siva = Npc(marshes, MarshesPaludosNpcsV1.Siva);
            Accept(harness, toida, Divided);
            Talk(harness, siva, Divided, MarshesDividedWeFallV1.LocateSiva);
            var (_, pack) = Helka(harness, marshes);
            harness.MoveTo(pack.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, pack, MarshesDividedWeFallV1.UseArgument, recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Divided, MarshesDividedWeFallV1.LocateHelka));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Divided));
            harness.Drain();
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Divided && obj.IsInWorld), "the body goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, toida, Divided), "offered again after an abandon");

            Accept(harness, toida, Divided);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Divided, MarshesDividedWeFallV1.LocateSiva));
            var (_, again) = Helka(harness, marshes);
            Assert.IsTrue(again.IsEnabled, "a whole pack");
            Talk(harness, siva, Divided, MarshesDividedWeFallV1.LocateSiva);
        }

        /// <summary>Valverde Marshes as the server loads it, the player at the Paludos waypoint.</summary>
        internal static MapChannel EnterMarshes(WildernessRuntimeTestHarness harness)
        {
            var map = harness.Maps.MapChannelArray.TryGetValue(MarshesMissionDataV1.MapContextId, out var loaded)
                ? loaded
                : PlateauInstanceMaps.LoadMap(harness, MarshesMissionDataV1.MapContextId);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = MarshesMissionDataV1.MapContextId;
            map.ClientList.Add(client);
            harness.MoveTo(PaludosWaypoint);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return map;
        }

        /// <summary>Alia, sent as her squad of one (the squads' worker on the Marshes).</summary>
        private static Creature Alia(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            S.Init(harness);
            Assert.IsTrue(S.Spawns.ContainsKey(MarshesPaludosNpcsV1.AliaSquad), "her squad is in the world database");
            S.Worker(marshes);
            harness.Drain();
            var alia = S.On(marshes)[MarshesPaludosNpcsV1.AliaSquad].Members.Single().Creature;
            Assert.AreEqual(MarshesPaludosNpcsV1.Alia, alia.DbId);
            Assert.IsNotNull(alia.Npc, "she can talk");
            return alia;
        }

        /// <summary>Helka's body and pack: the player's own, at TaRapedia's place, on the navmesh, of their classes.</summary>
        private static (DynamicObject Corpse, DynamicObject Pack) Helka(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            DynamicObject Role(string role) => marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Divided &&
                obj.SceneActorRole == role && obj.IsInWorld);
            var corpse = Role(MarshesDividedWeFallV1.CorpseRole);
            var pack = Role(MarshesDividedWeFallV1.PackRole);
            var at = MarshesDividedWeFallV1.HelkaAt;
            Assert.AreEqual((MarshesDividedWeFallV1.CorpseClass, MarshesDividedWeFallV1.PackClass), ((uint)corpse.EntityClassId, (uint)pack.EntityClassId));
            foreach (var obj in new[] { corpse, pack })
            {
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), obj.Position);
                Assert.AreEqual(harness.Client.Player.Id, obj.SceneOwnerCharacterId, "the player's own");
            }
            AssertOnGround(marshes, pack.Position, "Helka");
            Assert.IsTrue(marshes.NavMesh.FindPath(Road, pack.Position, out var complete) != null && complete, "Helka is walked to");
            return (corpse, pack);
        }

        private static void AssertOnGround(MapChannel map, Vector3 position, string what)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        private static Creature Npc(MapChannel map, uint pool)
        {
            Spawn(map, pool);
            return RequireNpc(map, pool);
        }

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission));
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count(packet => packet.MissionId == mission), "MissionGained once");
        }

        /// <summary>The NPC hears the objective (only that one), and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            Assert.AreEqual(1, harness.Drain().OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission &&
                packet.ObjectiveId == objective), $"{mission}.{objective} ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        /// <summary>A turn-in whose save fails pays nothing; the mission stays to be turned in (as PoolsSupplyChainTests).</summary>
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

        /// <summary>The Marshes' spawn pools that hold one of these creatures.</summary>
        private static uint[] PoolsOf(WildernessRuntimeTestHarness harness, params uint[] creatures) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == MarshesMissionDataV1.MapContextId).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(creatures.Contains))
                .Select(row => row.Id).ToArray();

        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel map, uint kind)
        {
            var creature = Alive(map).FirstOrDefault(candidate => candidate.DbId == kind && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {kind} left");
            harness.MoveTo(creature.Position);
            // A Maw lies under the ground until someone comes near (MawLurk): it comes up at the player.
            if (MawLurk.IsBuried(creature))
                MawLurk.Emerge(map, creature, harness.Client.Player.EntityId);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Assert.IsTrue(map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        /// <summary>These pools' dead come back: their respawn time and more has passed (as PoolsSnakePitTests).</summary>
        private static void Respawn(MapChannel map, params uint[] pools)
        {
            var all = map.SpawnPools.ToArray();
            try
            {
                map.SpawnPools.Clear();
                map.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 600_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 600_000);
            }
            finally
            {
                map.SpawnPools.Clear();
                map.SpawnPools.AddRange(all);
            }
        }

        /// <summary>The map's living creatures, the Maws still under the ground too (MawLurk).</summary>
        private static IEnumerable<Creature> Alive(MapChannel map) => Living(map)
            .Concat(MawLurk.BuriedOn(map).Where(creature => creature.State != CharacterState.Dead));

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);

        /// <summary>A use of the object: the request, then its recovery if one is due (as PoolsSnakePitTests).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, uint argument, bool recovers)
        {
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), map.PerformRecovery.Count, $"argument {argument}");
            if (!recovers)
                return;
            var recovery = map.PerformRecovery.Last();
            map.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(map, recovery);
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;
    }
}
