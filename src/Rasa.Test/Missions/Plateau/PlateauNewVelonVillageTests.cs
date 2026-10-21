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
    /// New Velon Village's missions, Plateau, played through as a player plays them: Loremaster
    /// Talisen's Graveyard Shift (933), Luminary Sampei's Village Of The Damned (1981) and The Book Of
    /// Velonar (930), Ranger Kaely's Going Postal (1034) with Ranger Porthus on the chasm's rim.
    ///
    /// Why these tests: as PlateauCampResistanceScroungerTests - upstream tests every quest it adds by
    /// playing it in the runtime harness. Each playthrough checks the offer and its reward preview,
    /// the count or the item, a reload halfway, a turn-in whose save fails (BeforeSave) and the
    /// reward. The book's satchel is used as MissionSceneObjectActionsTests uses a scene object.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauNewVelonVillageTests
    {
        private const uint GraveyardShift = PlateauGraveyardShiftV1.MissionId;
        private const uint VillageOfTheDamned = PlateauVillageOfTheDamnedV1.MissionId;
        private const uint BookOfVelonar = PlateauBookOfVelonarV1.MissionId;
        private const uint GoingPostal = PlateauGoingPostalV1.MissionId;
        private const uint Talisen = PlateauGraveyardShiftV1.Talisen;          // creature and pool
        private const uint Sampei = PlateauVillageOfTheDamnedV1.Sampei;
        private const uint Kaely = PlateauGoingPostalV1.Kaely;
        private const uint Porthus = PlateauGoingPostalV1.Porthus;
        private const uint TombMiasma = PlateauTombMiasmasV1.TombMiasma;
        private const uint Bull = PlateauTombMiasmasV1.Bull;
        private const uint GeneralMiasma = PlateauGraveyardShiftV1.GeneralTombMiasma;
        private const uint Infected = PlateauInfectedForeansV1.Infected;
        private const uint Book = PlateauBookOfVelonarV1.BookTemplate;
        private const uint Satchel = PlateauGoingPostalV1.SatchelTemplate;

        private static readonly uint[] MiasmaPools = PlateauTombMiasmasV1.Pools.Select(pool => pool.Id).ToArray();
        private static readonly uint[] InfectedPools = PlateauInfectedForeansV1.Pools.Select(pool => pool.Id).ToArray();
        /// <summary>The general Plateau miasma's pools on map 1497 (RegionSpawnpoolPreloader): Desolation Quarry, Martyrs Canyon.</summary>
        private static readonly uint[] GeneralMiasmaPools = { 540045, 540048, 540053 };

        [TestMethod]
        public void TheMiasmasAndTheInfectedStandOnTheirGroundAndPorthusOnTheChasmsRim()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Talisen, Sampei, Kaely, Porthus);
            Spawn(plateau, MiasmaPools.Concat(InfectedPools).ToArray());

            var porthus = RequireNpc(plateau, Porthus);
            Assert.AreEqual((460f, -60f), (porthus.Position.X, porthus.Position.Z), "Porthus on the rim");
            var ground = plateau.NavMesh.GroundHeight(porthus.Position);
            Assert.IsNotNull(ground, "Porthus has navmesh under him");
            Assert.IsTrue(System.Math.Abs(ground.Value - porthus.Position.Y) < 0.5f, $"Porthus at {porthus.Position.Y}, ground {ground}");

            foreach (var (id, creature, min, max) in PlateauTombMiasmasV1.Pools.Select(pool => (pool.Id, pool.Creature, pool.Min, pool.Max))
                .Concat(InfectedPools.Select(id => (id, Infected, 2U, 3U))))
            {
                var pool = plateau.SpawnPools.Single(candidate => candidate.DbId == id);
                var height = plateau.NavMesh.GroundHeight(pool.Position);
                Assert.IsNotNull(height, $"pool {id} has no navmesh under it");
                Assert.IsTrue(System.Math.Abs(height.Value - pool.Position.Y) < 0.5f, $"pool {id} at {pool.Position.Y}, ground {height}");
                var living = Living(plateau).Count(candidate => candidate.SpawnPool?.DbId == id && candidate.DbId == creature);
                Assert.IsTrue(living >= min && living <= max, $"{living} at pool {id}");
            }
            foreach (var creature in Living(plateau).Where(candidate => candidate.DbId is TombMiasma or Bull or Infected))
                Assert.IsNull(creature.Npc, $"{creature.DbId} is no NPC");
        }

        [TestMethod]
        public void GraveyardShiftCountsTombMiasmasOfBothKindsAndSixBulls()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Talisen);
            Spawn(plateau, MiasmaPools.Concat(GeneralMiasmaPools).Concat(InfectedPools).ToArray());
            var talisen = RequireNpc(plateau, Talisen);

            var offer = WildernessRanjaGorgeTests.Offer(harness, talisen, GraveyardShift);
            WildernessRanjaGorgeTests.AssertSelectable(offer, GraveyardShift,
                (PlateauGraveyardShiftV1.BioGloves, 23375), (PlateauGraveyardShiftV1.GravitonGloves, 15053),
                (PlateauGraveyardShiftV1.MechGloves, 17960), (PlateauGraveyardShiftV1.StealthGloves, 19853));
            Assert.AreEqual(PlateauGraveyardShiftV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);

            // A kill before the mission counts for nothing.
            Kill(harness, plateau, TombMiasma);
            harness.MoveTo(talisen.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, talisen.EntityId, GraveyardShift));
            Assert.AreEqual(GraveyardShift, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            Assert.AreEqual((0U, 0U), (Count(harness, GraveyardShift, 1), Count(harness, GraveyardShift, 2)));

            Kill(harness, plateau, TombMiasma);
            Kill(harness, plateau, GeneralMiasma);
            Kill(harness, plateau, Infected);
            Kill(harness, plateau, Bull);
            Assert.AreEqual((2U, 1U), (Count(harness, GraveyardShift, 1), Count(harness, GraveyardShift, 2)),
                "the general Tomb Miasma counts, an Infected Forean does not, a Bull counts as a Bull only");

            // The counts are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((2U, 1U), (Count(harness, GraveyardShift, 1), Count(harness, GraveyardShift, 2)));
            harness.Drain();

            for (var kill = 3; kill <= PlateauGraveyardShiftV1.Miasmas; kill++)
                Kill(harness, plateau, kill % 2 == 0 ? GeneralMiasma : TombMiasma);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[GraveyardShift].Objectives[1].State);
            Assert.IsFalse(harness.Client.Player.Missions[GraveyardShift].Completeable, "the Bulls are not done");
            for (var kill = 2; kill <= PlateauGraveyardShiftV1.Bulls; kill++)
            {
                if (!Living(plateau).Any(creature => creature.DbId == Bull))
                    Respawn(plateau, MiasmaPools);
                Kill(harness, plateau, Bull);
            }
            Assert.AreEqual(6U, Count(harness, GraveyardShift, 2));
            Assert.IsTrue(harness.Client.Player.Missions[GraveyardShift].Completeable);

            // A turn-in whose save fails pays nothing.
            var before = harness.Context.ReadRewardTotals();
            harness.MoveTo(talisen.Position);
            harness.Drain();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Graveyard Shift turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, talisen.EntityId, GraveyardShift, 1));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, talisen, GraveyardShift, 1,
                PlateauGraveyardShiftV1.Experience, PlateauGraveyardShiftV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauGraveyardShiftV1.GravitonGloves), "the selected gloves");
        }

        [TestMethod]
        public void VillageOfTheDamnedCountsTenInfectedForeans()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Sampei);
            Spawn(plateau, InfectedPools.Concat(MiasmaPools).ToArray());
            var sampei = RequireNpc(plateau, Sampei);

            var offer = WildernessRanjaGorgeTests.Offer(harness, sampei, VillageOfTheDamned);
            WildernessRanjaGorgeTests.AssertSelectable(offer, VillageOfTheDamned,
                (PlateauVillageOfTheDamnedV1.BioLegs, 23471), (PlateauVillageOfTheDamnedV1.GravitonLegs, 15125),
                (PlateauVillageOfTheDamnedV1.MechHelmet, 17636), (PlateauVillageOfTheDamnedV1.StealthHelmet, 19889));
            harness.MoveTo(sampei.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, sampei.EntityId, VillageOfTheDamned));
            harness.Drain();

            Kill(harness, plateau, TombMiasma);
            Assert.AreEqual(0U, Count(harness, VillageOfTheDamned, 1), "a miasma is no Infected Forean");
            for (var kill = 1; kill < PlateauVillageOfTheDamnedV1.Kills; kill++)
            {
                if (!Living(plateau).Any(creature => creature.DbId == Infected))
                    Respawn(plateau, InfectedPools);
                Kill(harness, plateau, Infected);
            }
            Assert.AreEqual(9U, Count(harness, VillageOfTheDamned, 1));
            Assert.IsFalse(harness.Client.Player.Missions[VillageOfTheDamned].Completeable);
            if (!Living(plateau).Any(creature => creature.DbId == Infected))
                Respawn(plateau, InfectedPools);
            harness.Drain();
            Kill(harness, plateau, Infected);
            var tenth = harness.Drain();
            Assert.IsTrue(tenth.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == VillageOfTheDamned).IsCompleteable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sampei, VillageOfTheDamned, 0,
                PlateauVillageOfTheDamnedV1.Experience, PlateauVillageOfTheDamnedV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauVillageOfTheDamnedV1.BioLegs), "the selected legs");
        }

        [TestMethod]
        public void TheBookOfVelonarLiesInItsSatchelInTheRuinsForTheOneWhoSeeksIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Sampei);
            var sampei = RequireNpc(plateau, Sampei);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == BookOfVelonar), "no satchel before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, sampei, BookOfVelonar);
            WildernessRanjaGorgeTests.AssertSelectable(offer, BookOfVelonar,
                (PlateauBookOfVelonarV1.HealthModule, 29879), (PlateauBookOfVelonarV1.PowerModule, 29879),
                (PlateauBookOfVelonarV1.CritModule, 29879), (PlateauBookOfVelonarV1.PiercingModule, 29879));
            Assert.AreEqual(PlateauBookOfVelonarV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(sampei.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, sampei.EntityId, BookOfVelonar));
            harness.Drain();

            var satchel = plateau.DynamicObjects.Single(obj => obj.SceneMissionId == BookOfVelonar && obj.SceneActorRole == "book");
            Assert.AreEqual(PlateauBookOfVelonarV1.DispenserClass, (uint)satchel.EntityClassId);
            Assert.AreEqual((UseObjectState)PlateauBookOfVelonarV1.ReadyState, satchel.StateId);
            Assert.AreEqual(new Vector3(-473, 393.08f, -8), satchel.Position, "on the north hut's floor");
            Assert.AreEqual(harness.Client.Player.Id, satchel.SceneOwnerCharacterId, "the player's own");

            harness.MoveTo(satchel.Position + new Vector3(0, 0, 1));
            Use(harness, plateau, satchel, 1, recovers: false);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Book), "not with another argument");
            Use(harness, plateau, satchel, PlateauBookOfVelonarV1.UseArgument, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Book));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[BookOfVelonar].Objectives[1].State);
            Assert.IsTrue(harness.Client.Player.Missions[BookOfVelonar].Completeable);
            Assert.AreEqual((UseObjectState)PlateauBookOfVelonarV1.TakenState, satchel.StateId);
            Assert.IsFalse(satchel.IsEnabled);
            Use(harness, plateau, satchel, PlateauBookOfVelonarV1.UseArgument, recovers: false);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Book), "one book");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Book));
            Assert.IsTrue(harness.Client.Player.Missions[BookOfVelonar].Completeable);
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sampei, BookOfVelonar, 2,
                PlateauBookOfVelonarV1.Experience, PlateauBookOfVelonarV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauBookOfVelonarV1.CritModule), "the selected module");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Book), "Sampei keeps the book");
        }

        [TestMethod]
        public void GoingPostalTakesPorthusSatchelToKaely()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Kaely, Porthus);
            var kaely = RequireNpc(plateau, Kaely);
            var porthus = RequireNpc(plateau, Porthus);

            var offer = WildernessRanjaGorgeTests.Offer(harness, kaely, GoingPostal);
            WildernessRanjaGorgeTests.AssertSelectable(offer, GoingPostal,
                (PlateauGoingPostalV1.BioBoots, 23367), (PlateauGoingPostalV1.GravitonBoots, 15017),
                (PlateauGoingPostalV1.MechBoots, 17924), (PlateauGoingPostalV1.StealthBoots, 19817));
            Assert.AreEqual(PlateauGoingPostalV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            Assert.IsFalse(Conversation(harness, porthus).ContainsKey(ConversationType.ObjectiveComplete), "nothing before the mission");
            harness.MoveTo(kaely.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, kaely.EntityId, GoingPostal));
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, kaely.EntityId, GoingPostal, 1, 1), "Porthus's");

            AssertHandInTopic(Conversation(harness, porthus), GoingPostal, 1);
            harness.MoveTo(porthus.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, porthus.EntityId, GoingPostal, 1, 1));
            var handed = harness.Drain();
            CollectionAssert.AreEquivalent(new[] { 1U, 2U }, handed.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == GoingPostal).Select(packet => packet.ObjectiveId).ToArray());
            Assert.IsTrue(handed.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == GoingPostal).IsCompleteable);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Satchel));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, porthus.EntityId, GoingPostal, 1, 1), "once");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Satchel));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kaely, GoingPostal, 3,
                PlateauGoingPostalV1.Experience, PlateauGoingPostalV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauGoingPostalV1.StealthBoots), "the selected boots");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Satchel), "Kaely keeps the satchel");
        }

        private static uint Count(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        /// <summary>Kills a living creature of one of these kinds as the player.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, uint kind)
        {
            var creature = Living(plateau).FirstOrDefault(candidate => candidate.DbId == kind && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {kind} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
        }

        /// <summary>These pools' dead come back: their respawn time and more has passed.</summary>
        private static void Respawn(MapChannel plateau, params uint[] pools)
        {
            var all = plateau.SpawnPools.ToArray();
            try
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
            }
            finally
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all);
            }
        }

        /// <summary>A use of the object, as MissionSceneObjectActionsTests.Use (private): the request, then its recovery if one is due.</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel plateau, DynamicObject obj, uint argument, bool recovers)
        {
            var count = plateau.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), plateau.PerformRecovery.Count, $"argument {argument}");
            if (!recovers)
                return;
            var recovery = plateau.PerformRecovery.Last();
            plateau.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(plateau, recovery);
        }
    }
}
