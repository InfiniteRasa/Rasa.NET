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
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// The recon squad commanders' missions in Martyr's Canyon, Plateau, played through as a player
    /// plays them: Carvelle's Canyon Sweep (1045), Petrie's Blueprint For Victory (1046) and
    /// Locke's Toxic Detox (1053), each offered once Incommunicado (1040) delivered that
    /// commander's communicator.
    ///
    /// Why these tests: as PlateauCampResistanceTests, for the same reason - upstream tests every
    /// quest it adds by playing it in the runtime harness (WildernessRanjaGorgeTests,
    /// WildernessTwinPillarsTests, WildernessDaghdasUrnTests). The Plateau map, its navmesh and
    /// its pools are added as PlateauCampResistanceTests adds them; no upstream test does that.
    ///
    /// Each playthrough also checks, as the upstream quest tests do: the offer and its reward
    /// preview, no offer before the prerequisite, the topics each NPC offers at each step, refusal
    /// at the wrong NPC, the client packets, a reload of the character halfway, a turn-in whose
    /// save fails (BeforeSave), and a full bag. The kill count is the Wilderness kill missions'
    /// (WildernessTargetsKillRulesTests); the pumps are destroyed as Cache of the Day's containers
    /// are (WildernessLandingZoneTests.FuelCounterRequiresTenDistinctDestroyedNativeContainers);
    /// the blueprints' chance is rolled as MissionQualifiedLootTests rolls it (lootRoll).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauMartyrsCanyonTests
    {
        private const uint GiantKiller = PlateauGiantKillerV1.MissionId;
        private const uint Incommunicado = PlateauIncommunicadoV1.MissionId;
        private const uint CanyonSweep = PlateauCanyonSweepV1.MissionId;
        private const uint Blueprint = PlateauBlueprintForVictoryV1.MissionId;
        private const uint ToxicDetox = PlateauToxicDetoxV1.MissionId;
        private const uint Bosley = PlateauGiantKillerV1.Bosley;             // creature and pool
        private const uint Carvelle = PlateauReconSquadsV1.Carvelle;
        private const uint Petrie = PlateauReconSquadsV1.Petrie;
        private const uint Locke = PlateauReconSquadsV1.Locke;
        private const uint Guard = PlateauMaligoGuardsV1.Guard;
        private const uint Guardmaster = PlateauMaligoGuardsV1.Guardmaster;
        private const uint Blueprints = PlateauBlueprintForVictoryV1.BlueprintsTemplate;

        /// <summary>Fillers the upstream full-bag tests use: a mission item (inventory category 4) and a weapon (1).</summary>
        private const uint MissionItemFiller = 11519, WeaponFiller = 3869;

        private static readonly uint[] Packs = PlateauMaligoGuardsV1.Packs.Select(pack => pack.Id).ToArray();

        [TestMethod]
        public void EachMaligoPackIsAGuardmasterWithTwoOrThreeGuardsOnTheGround()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Packs);

            foreach (var id in Packs)
            {
                var pool = plateau.SpawnPools.Single(candidate => candidate.DbId == id);
                var ground = plateau.NavMesh.GroundHeight(pool.Position);
                Assert.IsNotNull(ground, $"pack {id} has no navmesh under it");
                Assert.IsTrue(System.Math.Abs(ground.Value - pool.Position.Y) < 0.5f, $"pack {id} at {pool.Position.Y}, ground {ground}");

                var pack = Living(plateau).Where(creature => creature.SpawnPool?.DbId == id).ToArray();
                Assert.AreEqual(1, pack.Count(creature => creature.DbId == Guardmaster), $"pack {id}");
                var guards = pack.Count(creature => creature.DbId == Guard);
                Assert.IsTrue(guards >= 2 && guards <= 3, $"{guards} guards in pack {id}");
                Assert.AreEqual(pack.Length, 1 + guards, $"pack {id} holds only Maligo Elite Guards");
            }
        }

        [TestMethod]
        public void EachDeliveryUnlocksOnlyThatCommandersMissionAndAnAbandonKeepsIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Carvelle, Petrie, Locke);
            var commanders = new[] { (Carvelle, CanyonSweep), (Petrie, Blueprint), (Locke, ToxicDetox) };
            AcceptIncommunicado(harness, plateau);

            foreach (var (commander, mission) in commanders)
            {
                var npc = RequireNpc(plateau, commander);
                Assert.IsFalse(Offers(harness, npc, mission), $"{mission} before the delivery");
                harness.MoveTo(npc.Position);
                Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} before the delivery");
            }

            Deliver(harness, plateau, 1);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PlateauIncommunicadoFlagsV1.CarvelleFlag]);
            Assert.IsTrue(Offers(harness, RequireNpc(plateau, Carvelle), CanyonSweep), "Carvelle's delivery: Canyon Sweep");
            Assert.IsFalse(Offers(harness, RequireNpc(plateau, Petrie), Blueprint), "not Petrie's");
            Assert.IsFalse(Offers(harness, RequireNpc(plateau, Locke), ToxicDetox), "not Locke's");

            // The flag is saved with the character, and an abandon of Incommunicado leaves it.
            new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = Incommunicado });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Incommunicado));
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PlateauIncommunicadoFlagsV1.CarvelleFlag]);
            Assert.IsTrue(Offers(harness, RequireNpc(plateau, Carvelle), CanyonSweep), "after the abandon");
            Assert.IsFalse(Offers(harness, RequireNpc(plateau, Petrie), Blueprint), "after the abandon");

            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 2, 3);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PlateauIncommunicadoFlagsV1.PetrieFlag]);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PlateauIncommunicadoFlagsV1.LockeFlag]);
            foreach (var (commander, mission) in commanders)
                Assert.IsTrue(Offers(harness, RequireNpc(plateau, commander), mission), $"{mission} after all deliveries");
        }

        [TestMethod]
        public void CanyonSweepIsCarvellesAndCountsTwelveGuardsAndGuardmasters()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Carvelle, Petrie);
            Spawn(plateau, Packs);
            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 1);
            var carvelle = RequireNpc(plateau, Carvelle);

            var offer = WildernessRanjaGorgeTests.Offer(harness, carvelle, CanyonSweep);
            WildernessRanjaGorgeTests.AssertSelectable(offer, CanyonSweep,
                (PlateauCanyonSweepV1.ElectricInjectorGun, 26967), (PlateauCanyonSweepV1.ElectricChaingun, 27032),
                (PlateauCanyonSweepV1.SonicCannon, 27279), (PlateauCanyonSweepV1.SonicShotgun, 27334));
            Assert.AreEqual(PlateauCanyonSweepV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);

            // A kill before the mission counts for nothing.
            Kill(harness, plateau, Guard);
            harness.MoveTo(carvelle.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, carvelle.EntityId, CanyonSweep));
            Assert.AreEqual(CanyonSweep, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            Assert.AreEqual(0U, Count(harness, CanyonSweep));

            for (var kill = 1; kill <= 5; kill++)
                Kill(harness, plateau, Guard);
            Kill(harness, plateau, Guardmaster);
            Assert.AreEqual(6U, Count(harness, CanyonSweep), "a guardmaster counts as a guard");

            // The count is saved.
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(6U, Count(harness, CanyonSweep));
            Assert.IsFalse(Conversation(harness, carvelle).ContainsKey(ConversationType.MissionComplete), "6 of 12");
            harness.Drain();

            for (var kill = 7; kill <= 11; kill++)
                Kill(harness, plateau, kill % 2 == 0 ? Guardmaster : Guard);
            Assert.AreEqual(11U, Count(harness, CanyonSweep));
            Assert.IsFalse(harness.Client.Player.Missions[CanyonSweep].Completeable);
            harness.Drain();
            Kill(harness, plateau, Guard);
            var twelfth = harness.Drain();
            Assert.AreEqual(1U, twelfth.OfType<ObjectiveCompletedPacket>().Single(packet => packet.MissionId == CanyonSweep).ObjectiveId);
            Assert.IsTrue(twelfth.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == CanyonSweep).IsCompleteable);
            Kill(harness, plateau, Guard);
            Assert.AreEqual(12U, Count(harness, CanyonSweep), "the count stops at 12");

            // Petrie is not the receiver.
            var petrie = RequireNpc(plateau, Petrie);
            harness.MoveTo(petrie.Position);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, petrie.EntityId, CanyonSweep, 0));

            var turnIn = (Dictionary<uint, RewardInfo>)Conversation(harness, carvelle)[ConversationType.MissionComplete];
            CollectionAssert.AreEqual(offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                turnIn[CanyonSweep].SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            // A turn-in whose save fails pays nothing.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Canyon Sweep turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, carvelle.EntityId, CanyonSweep, 0));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, carvelle, CanyonSweep, 0,
                PlateauCanyonSweepV1.Experience, PlateauCanyonSweepV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauCanyonSweepV1.ElectricInjectorGun), "the selected weapon");
        }

        [TestMethod]
        public void AFullRewardBagHoldsCanyonSweepsWeaponUntilItFits()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Carvelle);
            Spawn(plateau, Packs);
            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 1);
            var carvelle = RequireNpc(plateau, Carvelle);
            harness.MoveTo(carvelle.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, carvelle.EntityId, CanyonSweep));
            for (var kill = 0; kill < PlateauCanyonSweepV1.Kills; kill++)
                Kill(harness, plateau, Guard, Guardmaster);
            Assert.IsTrue(harness.Client.Player.Missions[CanyonSweep].Completeable);
            WildernessAliaBranchesTests.FillCategory(harness, WeaponFiller, 50);
            harness.FillInbox();
            var before = harness.Context.ReadRewardTotals();

            harness.MoveTo(carvelle.Position);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, carvelle.EntityId, CanyonSweep, 1));
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauCanyonSweepV1.ElectricChaingun));

            WildernessAliaBranchesTests.FreeOneSlot(harness, WeaponFiller);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, carvelle, CanyonSweep, 1,
                PlateauCanyonSweepV1.Experience, PlateauCanyonSweepV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauCanyonSweepV1.ElectricChaingun));
        }

        [TestMethod]
        public void BlueprintForVictoryGoesFromPetrieToBosleyWithAGuardmastersBlueprints()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Petrie, Carvelle);
            Spawn(plateau, Packs);
            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 2);
            var petrie = RequireNpc(plateau, Petrie);
            var bosley = RequireNpc(plateau, Bosley);

            var offer = WildernessRanjaGorgeTests.Offer(harness, petrie, Blueprint);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Blueprint,
                (PlateauBlueprintForVictoryV1.BioLegs, 23392), (PlateauBlueprintForVictoryV1.GravitonLegs, 15130),
                (PlateauBlueprintForVictoryV1.MechHelmet, 18001), (PlateauBlueprintForVictoryV1.StealthHelmet, 19894));
            Assert.AreEqual(PlateauBlueprintForVictoryV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(petrie.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, petrie.EntityId, Blueprint));
            Assert.AreEqual(Blueprint, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);

            // Only a guardmaster carries them, at a roll under 25 of 100.
            Assert.IsFalse(Holds(Kill(harness, plateau, Guard), Blueprints), "a guard never");
            percentile = PlateauBlueprintForVictoryV1.DropChance;
            Assert.IsFalse(Holds(Kill(harness, plateau, Guardmaster), Blueprints), "a roll of 25 misses");
            percentile = PlateauBlueprintForVictoryV1.DropChance - 1;
            var loot = Kill(harness, plateau, Guardmaster);
            Assert.IsTrue(Holds(loot, Blueprints), "a roll of 24 drops them");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Blueprints), "a death is not a pickup");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Blueprint].Objectives[1].State);
            harness.Drain();

            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Blueprints));
            Assert.AreEqual(1U, harness.Drain().OfType<ObjectiveCompletedPacket>()
                .Single(packet => packet.MissionId == Blueprint).ObjectiveId, "the pickup");

            // The blueprints and objective 3 are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            var log = harness.Client.Player.Missions[Blueprint];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[3].State);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Blueprints));

            // Petrie does not take them; Bosley does.
            Assert.IsFalse(Conversation(harness, petrie).ContainsKey(ConversationType.ObjectiveComplete));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, petrie.EntityId, Blueprint, 3, 1));
            AssertHandInTopic(Conversation(harness, bosley), Blueprint, 3);
            harness.MoveTo(bosley.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, bosley.EntityId, Blueprint, 3, 1));
            var handIn = harness.Drain();
            Assert.AreEqual(3U, handIn.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.IsTrue(handIn.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == Blueprint).IsCompleteable);

            var turnIn = (Dictionary<uint, RewardInfo>)Conversation(harness, bosley)[ConversationType.MissionComplete];
            CollectionAssert.AreEqual(offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                turnIn[Blueprint].SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            // A turn-in whose save fails pays nothing and keeps the blueprints.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Blueprint For Victory turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, bosley.EntityId, Blueprint, 0));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Blueprints));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bosley, Blueprint, 0,
                PlateauBlueprintForVictoryV1.Experience, PlateauBlueprintForVictoryV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBlueprintForVictoryV1.BioLegs), "the selected armor");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Blueprints), "Bosley keeps the blueprints");
        }

        [TestMethod]
        public void TheBlueprintsStayOnTheCorpseWhileTheBagIsFull()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Petrie);
            Spawn(plateau, Packs);
            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 2);
            var petrie = RequireNpc(plateau, Petrie);
            harness.MoveTo(petrie.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, petrie.EntityId, Blueprint));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, plateau, Guardmaster);
            Assert.IsTrue(Holds(loot, Blueprints));
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Blueprints));
            Assert.IsTrue(Holds(loot, Blueprints), "the blueprints stay on the corpse");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Blueprint].Objectives[1].State);

            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Blueprints));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Blueprint].Objectives[1].State);
        }

        [TestMethod]
        public void ToxicDetoxIsLockesAndCountsFiveDistinctPumpsAtTheSepticBog()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Locke, Carvelle);
            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 3);
            var locke = RequireNpc(plateau, Locke);
            Assert.AreEqual(0, plateau.DynamicObjects.Count(obj => obj.SceneMissionId == ToxicDetox), "no pump before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, locke, ToxicDetox);
            WildernessRanjaGorgeTests.AssertSelectable(offer, ToxicDetox,
                (PlateauToxicDetoxV1.AreaRepairTool, 28725), (PlateauToxicDetoxV1.SonicDisperser, 27181),
                (PlateauToxicDetoxV1.Rifle, 27226), (PlateauToxicDetoxV1.SonicTorqueshellRifle, 27394));
            Assert.AreEqual(PlateauToxicDetoxV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(locke.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, locke.EntityId, ToxicDetox));
            Assert.AreEqual(ToxicDetox, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);

            var pumps = Pumps(plateau);
            Assert.AreEqual(5, pumps.Select(pump => pump.EntityId).Distinct().Count());
            foreach (var pump in pumps)
            {
                Assert.AreEqual(PlateauToxicDetoxV1.PumpClass, (uint)pump.EntityClassId);
                Assert.AreEqual(PlateauToxicDetoxV1.IntactState, (uint)pump.StateId);
                Assert.AreEqual(PlateauToxicDetoxV1.PumpHitPoints, pump.CurrentHitPoints);
                var ground = plateau.NavMesh.GroundHeight(pump.Position);
                Assert.IsNotNull(ground, $"{pump.SceneActorRole} has no navmesh under it");
                Assert.IsTrue(System.Math.Abs(ground.Value - pump.Position.Y) < 0.5f, $"{pump.SceneActorRole} at {pump.Position.Y}, ground {ground}");
            }

            DamagePump(harness, plateau, pumps[0], 99);
            Assert.AreEqual(1U, pumps[0].CurrentHitPoints);
            Assert.AreEqual(0U, Count(harness, ToxicDetox));
            DamagePump(harness, plateau, pumps[0], 1);
            Assert.AreEqual(PlateauToxicDetoxV1.DestroyedState, (uint)pumps[0].StateId);
            Assert.IsFalse(pumps[0].IsEnabled);
            DamagePump(harness, plateau, pumps[0], 100);
            Assert.AreEqual(1U, Count(harness, ToxicDetox), "one pump counts once");

            // The count and the destroyed pump are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, Count(harness, ToxicDetox));
            pumps = Pumps(plateau);
            Assert.AreEqual(PlateauToxicDetoxV1.DestroyedState, (uint)pumps[0].StateId);
            Assert.IsFalse(pumps[0].IsEnabled);

            for (var index = 1; index < 4; index++)
                DamagePump(harness, plateau, pumps[index], 100);
            Assert.AreEqual(4U, Count(harness, ToxicDetox));
            Assert.IsFalse(harness.Client.Player.Missions[ToxicDetox].Completeable);
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Scenario(ToxicDetox, 2, 5)),
                "an id beyond the five");
            harness.Drain();
            DamagePump(harness, plateau, pumps[4], 100);
            var fifth = harness.Drain();
            Assert.AreEqual(5U, Count(harness, ToxicDetox));
            Assert.AreEqual(1U, fifth.OfType<ObjectiveCompletedPacket>().Single(packet => packet.MissionId == ToxicDetox).ObjectiveId);
            Assert.IsTrue(fifth.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == ToxicDetox).IsCompleteable);

            var turnIn = (Dictionary<uint, RewardInfo>)Conversation(harness, locke)[ConversationType.MissionComplete];
            CollectionAssert.AreEqual(offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                turnIn[ToxicDetox].SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            // A turn-in whose save fails pays nothing.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Toxic Detox turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, locke.EntityId, ToxicDetox, 0));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, locke, ToxicDetox, 0,
                PlateauToxicDetoxV1.Experience, PlateauToxicDetoxV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauToxicDetoxV1.AreaRepairTool), "the selected tool");
        }

        [TestMethod]
        public void AbandoningToxicDetoxTakesAwayItsPumpsAndANewOneStartsAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Locke);
            AcceptIncommunicado(harness, plateau);
            Deliver(harness, plateau, 3);
            var locke = RequireNpc(plateau, Locke);
            harness.MoveTo(locke.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, locke.EntityId, ToxicDetox));
            DamagePump(harness, plateau, Pumps(plateau)[0], 100);
            Assert.AreEqual(1U, Count(harness, ToxicDetox));

            new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = ToxicDetox });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(ToxicDetox));
            Assert.AreEqual(0, plateau.DynamicObjects.Count(obj => obj.SceneMissionId == ToxicDetox), "the pumps go with the mission");

            harness.MoveTo(locke.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, locke.EntityId, ToxicDetox));
            Assert.AreEqual(0U, Count(harness, ToxicDetox));
            Assert.IsTrue(Pumps(plateau).All(pump => pump.IsEnabled && (uint)pump.StateId == PlateauToxicDetoxV1.IntactState));
        }

        /// <summary>Giant Killer done and Incommunicado accepted at Bosley: three communicators held.</summary>
        private static void AcceptIncommunicado(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            if (!harness.Client.Player.MissionHistory.ContainsKey(GiantKiller))
                SeedCompleted(harness, GiantKiller);
            var bosley = RequireNpc(plateau, Bosley);
            harness.MoveTo(bosley.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, Incommunicado));
            harness.Drain();
        }

        /// <summary>Incommunicado's deliveries: objective 1 to Carvelle, 2 to Petrie, 3 to Locke.</summary>
        private static void Deliver(WildernessRuntimeTestHarness harness, MapChannel plateau, params uint[] objectives)
        {
            foreach (var objective in objectives)
            {
                var npc = RequireNpc(plateau, objective switch { 1 => Carvelle, 2 => Petrie, _ => Locke });
                harness.MoveTo(npc.Position);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Incommunicado, objective, 1),
                    $"delivery {objective}");
            }
            harness.Drain();
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static uint Count(WildernessRuntimeTestHarness harness, uint mission) =>
            harness.Client.Player.Missions[mission].Objectives[1].Counters[0];

        /// <summary>Kills a living Maligo Elite Guard of the given kinds as the player; returns its corpse.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, params uint[] kinds)
        {
            var creature = Living(plateau).FirstOrDefault(candidate => kinds.Contains(candidate.DbId) &&
                Packs.Contains(candidate.SpawnPool?.DbId ?? 0));
            Assert.IsNotNull(creature, $"no living {string.Join("/", kinds)} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
            Assert.IsTrue(plateau.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);

        private static DynamicObject[] Pumps(MapChannel plateau) => Enumerable.Range(1, PlateauToxicDetoxV1.Pumps.Length)
            .Select(id => plateau.DynamicObjects.Single(obj => obj.SceneMissionId == ToxicDetox && obj.SceneActorRole == $"pump-{id}"))
            .ToArray();

        /// <summary>A weapon hit on the pump, as WildernessLandingZoneTests.DamageObject (private) hits a container.</summary>
        private static void DamagePump(WildernessRuntimeTestHarness harness, MapChannel plateau, DynamicObject pump, int damage)
        {
            harness.MoveTo(pump.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = pump.EntityId };
            MissileManager.Instance.MissileLaunch(plateau, action, damage);
            MissileManager.Instance.DoWork(plateau, 1000);
        }
    }
}
