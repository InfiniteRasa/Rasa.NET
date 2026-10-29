extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
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
    /// The Scrounger's chain at Camp Resistance, Plateau, played through as a player plays it:
    /// Corporal "Relay" O'Brien's Comic Relief (1251), Private "Scrounger" Scarpelo's Scavenger Hunt
    /// (1015) and The Stalk Market (1017), with Major Lulu Lonigan's trade.
    ///
    /// Why these tests: as PlateauCampResistanceTests and PlateauMartyrsCanyonTests - upstream
    /// tests every quest it adds by playing it in the runtime harness (WildernessRanjaGorgeTests,
    /// WildernessTwinPillarsTests). The Plateau map, its navmesh and its pools are added as
    /// PlateauCampResistanceTests adds them; no upstream test does that.
    ///
    /// As the upstream quest tests, each playthrough checks the offer and its reward preview, no
    /// offer before the prerequisite, the topics each NPC offers at each step, the client packets,
    /// a reload halfway, a turn-in whose save fails (BeforeSave), and a full bag. The item that
    /// one mission rewards and the next takes (the carton) is as WildernessRanjaGorgeTests' samples:
    /// a character-owned item, consumed at the NPC, an assignment item issued in its place. The drop
    /// chances are rolled as MissionQualifiedLootTests rolls them (lootRoll), both sides of each
    /// boundary.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauCampResistanceScroungerTests
    {
        private const uint ComicRelief = PlateauComicReliefV1.MissionId;
        private const uint ScavengerHunt = PlateauScavengerHuntV1.MissionId;
        private const uint StalkMarket = PlateauStalkMarketV1.MissionId;
        private const uint OBrien = PlateauScroungerNpcsV1.OBrien;           // creature and pool
        private const uint Lonigan = PlateauScroungerNpcsV1.Lonigan;
        private const uint Scarpelo = PlateauScroungerNpcsV1.Scarpelo;
        private const uint Comic = PlateauComicReliefV1.ComicTemplate;
        private const uint Carton = PlateauComicReliefV1.CartonTemplate;
        private const uint Patch = PlateauScavengerHuntV1.PatchTemplate;
        private const uint Dagger = PlateauScavengerHuntV1.DaggerTemplate;
        private const uint Stalker = PlateauBaneStalkersV1.Stalker;

        /// <summary>Thrax pools of the Plateau: Thrax Soldiers with a Grenadier or not (531612-531615).</summary>
        private static readonly uint[] ThraxPools = { 531612, 531613, 531614, 531615 };
        /// <summary>A pool with a Kael Corporal (531088), no Thrax kind.</summary>
        private const uint KaelPool = 531611, Kael = 531088;
        private static readonly uint[] StalkerPools = PlateauBaneStalkersV1.Pairs.Select(pair => pair.Id).ToArray();

        /// <summary>Fillers the upstream full-bag tests use: a mission item (inventory category 4) and a weapon (1).</summary>
        private const uint MissionItemFiller = 11519, WeaponFiller = 3869;

        [TestMethod]
        public void TheScroungerLoniganAndOBrienStandInCampAndTheStalkersInPairsSouthOfTheBridge()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, OBrien, Lonigan, Scarpelo);
            Spawn(plateau, StalkerPools);

            foreach (var (pool, x, z) in new[] { (OBrien, -621f, -234f), (Lonigan, -618f, -237f), (Scarpelo, -622f, -227f) })
            {
                var npc = RequireNpc(plateau, pool);
                Assert.AreEqual((x, z), (npc.Position.X, npc.Position.Z), $"{pool} at his place");
            }
            foreach (var pair in PlateauBaneStalkersV1.Pairs)
            {
                var pool = plateau.SpawnPools.Single(candidate => candidate.DbId == pair.Id);
                var ground = plateau.NavMesh.GroundHeight(pool.Position);
                Assert.IsNotNull(ground, $"pair {pair.Id} has no navmesh under it");
                Assert.IsTrue(System.Math.Abs(ground.Value - pool.Position.Y) < 0.5f, $"pair {pair.Id} at {pool.Position.Y}, ground {ground}");
                Assert.AreEqual(2, Living(plateau).Count(creature => creature.SpawnPool?.DbId == pair.Id && creature.DbId == Stalker),
                    $"a pair at {pair.Id}");
            }
        }

        [TestMethod]
        public void ComicReliefTradesTheScroungersCartonForRelaysComic()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var plateau = EnterPlateau(harness);
            Spawn(plateau, OBrien, Lonigan, Scarpelo);
            Spawn(plateau, ThraxPools);
            var obrien = RequireNpc(plateau, OBrien);
            var lonigan = RequireNpc(plateau, Lonigan);
            var scarpelo = RequireNpc(plateau, Scarpelo);
            // A marker on every objective: 1, 7 and 8 (she has the comic) on Lonigan, 10 and 9 (the cigarettes) on the Scrounger.
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.MissionId == ComicRelief).ToList();
            CollectionAssert.AreEquivalent(new[] { 1U, 7U, 8U, 9U, 10U }, markers.Select(row => row.ObjectiveId).ToArray());
            foreach (var row in markers)
            {
                var at = row.ObjectiveId is 9 or 10 ? scarpelo.Position : lonigan.Position;
                Assert.AreEqual(0f, System.Numerics.Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new(at.X, at.Z)), 2f,
                    $"objective {row.ObjectiveId}'s marker");
            }

            var offer = WildernessRanjaGorgeTests.Offer(harness, obrien, ComicRelief);
            WildernessRanjaGorgeTests.AssertSelectable(offer, ComicRelief,
                (PlateauComicReliefV1.BioVest, 23399), (PlateauComicReliefV1.GravitonVest, 15161),
                (PlateauComicReliefV1.MechVest, 18068), (PlateauComicReliefV1.StealthVest, 19961));
            Assert.AreEqual(PlateauComicReliefV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            Assert.IsFalse(Offers(harness, scarpelo, ScavengerHunt), "Scavenger Hunt only after the Scrounger's talk");
            harness.MoveTo(obrien.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, obrien.EntityId, ComicRelief));
            Assert.AreEqual(ComicRelief, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);

            // First Lonigan names her price; the Scrounger has nothing yet, Lonigan takes nothing yet.
            Assert.IsFalse(Conversation(harness, scarpelo).ContainsKey(ConversationType.ObjectiveComplete), "not before Lonigan");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, scarpelo.EntityId, ComicRelief, 10, 1));
            Talk(harness, lonigan, 1);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, lonigan.EntityId, ComicRelief, 7, 1),
                "no trade without the carton");
            Talk(harness, scarpelo, 10);
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PlateauComicReliefV1.ScroungerFlag]);

            // The talks and the flag are saved; the Scrounger now offers Scavenger Hunt.
            WildernessRanjaGorgeTests.Reload(harness);
            var log = harness.Client.Player.Missions[ComicRelief];
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete,
                MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete),
                (log.Objectives[1].State, log.Objectives[10].State, log.Objectives[9].State, log.Objectives[7].State, log.Objectives[8].State));
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PlateauComicReliefV1.ScroungerFlag]);
            var hunt = WildernessRanjaGorgeTests.Offer(harness, scarpelo, ScavengerHunt);
            Assert.AreEqual(0, hunt.SelectableReward.Count, "no choice: the carton");
            harness.MoveTo(scarpelo.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, scarpelo.EntityId, ScavengerHunt));
            harness.Drain();

            // Every Thrax kill drops a patch at roll 0; the one dagger comes with the first.
            for (var kill = 0; kill < PlateauScavengerHuntV1.Patches; kill++)
                PickUp(harness, Kill(harness, plateau, PlateauScavengerHuntV1.Thrax));
            Assert.AreEqual((5U, 1U), (WildernessRanjaGorgeTests.Held(harness, Patch), WildernessRanjaGorgeTests.Held(harness, Dagger)));
            Assert.IsTrue(harness.Client.Player.Missions[ScavengerHunt].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, scarpelo, ScavengerHunt, null,
                PlateauScavengerHuntV1.Experience, PlateauScavengerHuntV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Carton), "the carton, Scavenger Hunt's reward");
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, Patch), WildernessRanjaGorgeTests.Held(harness, Dagger)),
                "the Scrounger keeps the trophies");
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[ComicRelief].Objectives[9].State,
                "the carton is in the bag");
            harness.Drain();

            // Lonigan takes the carton and hands over the comic: objectives 7 and 8.
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, scarpelo.EntityId, ComicRelief, 7, 1), "Lonigan's");
            AssertHandInTopic(Conversation(harness, lonigan), ComicRelief, 7);
            harness.MoveTo(lonigan.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, lonigan.EntityId, ComicRelief, 7, 1));
            var trade = harness.Drain();
            CollectionAssert.AreEquivalent(new[] { 7U, 8U }, trade.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == ComicRelief).Select(packet => packet.ObjectiveId).ToArray());
            Assert.IsTrue(trade.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == ComicRelief).IsCompleteable);
            Assert.AreEqual((0U, 1U), (WildernessRanjaGorgeTests.Held(harness, Carton), WildernessRanjaGorgeTests.Held(harness, Comic)));

            var turnIn = (Dictionary<uint, RewardInfo>)Conversation(harness, obrien)[ConversationType.MissionComplete];
            CollectionAssert.AreEqual(offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                turnIn[ComicRelief].SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            // A turn-in whose save fails pays nothing and keeps the comic.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Comic Relief turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, obrien.EntityId, ComicRelief, 0));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Comic));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, obrien, ComicRelief, 0,
                PlateauComicReliefV1.Experience, PlateauComicReliefV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ComicRelief));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauComicReliefV1.BioVest), "the selected vest");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Comic), "O'Brien keeps his comic");
        }

        [TestMethod]
        public void ScavengerHuntsTrophiesDropOnlyFromThraxAtThirtyAndTenInAHundred()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Scarpelo);
            Spawn(plateau, ThraxPools);
            Spawn(plateau, KaelPool);
            var scarpelo = RequireNpc(plateau, Scarpelo);
            // 3's and 4's markers (ESTIMATE) cover the Thrax pools nearest Camp Resistance.
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.MissionId == ScavengerHunt).ToList();
            CollectionAssert.AreEquivalent(new[] { 3U, 4U }, markers.Select(row => row.ObjectiveId).ToArray());
            var near = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id == 531606 || pool.Id == 531609 || pool.Id == 531621).ToList();
            Assert.HasCount(3, near);
            foreach (var row in markers)
                foreach (var pool in near)
                {
                    Assert.IsTrue(PlateauScavengerHuntV1.Thrax.Contains(pool.Creature1Id), $"pool {pool.Id} is of Thrax");
                    Assert.IsTrue(System.Numerics.Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)pool.PosX, (float)pool.PosZ)) < row.Radius,
                        $"objective {row.ObjectiveId}'s marker covers pool {pool.Id}");
                }
            harness.MoveTo(scarpelo.Position);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, scarpelo.EntityId, ScavengerHunt), "no flag");
            SetFlag(harness, PlateauComicReliefV1.ScroungerFlag);
            harness.MoveTo(scarpelo.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, scarpelo.EntityId, ScavengerHunt));

            Assert.IsFalse(Holds(Kill(harness, plateau, Kael), Patch), "a Kael is no Thrax");
            percentile = PlateauScavengerHuntV1.PatchChance;
            var miss = Kill(harness, plateau, PlateauScavengerHuntV1.Thrax);
            Assert.IsFalse(Holds(miss, Patch) || Holds(miss, Dagger), "30 misses both");
            percentile = PlateauScavengerHuntV1.PatchChance - 1;
            var patchOnly = Kill(harness, plateau, PlateauScavengerHuntV1.Thrax);
            Assert.IsTrue(Holds(patchOnly, Patch), "29 drops a patch");
            Assert.IsFalse(Holds(patchOnly, Dagger), "29 misses the dagger");
            percentile = PlateauScavengerHuntV1.DaggerChance;
            Assert.IsFalse(Holds(Kill(harness, plateau, PlateauScavengerHuntV1.Thrax), Dagger), "10 misses the dagger");
            percentile = PlateauScavengerHuntV1.DaggerChance - 1;
            var both = Kill(harness, plateau, PlateauScavengerHuntV1.Thrax);
            Assert.IsTrue(Holds(both, Patch) && Holds(both, Dagger), "9 drops both");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Dagger), "a death is not a pickup");
            PickUp(harness, both);
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[ScavengerHunt].Objectives[4].State);
            Assert.IsFalse(Holds(Kill(harness, plateau, PlateauScavengerHuntV1.Thrax), Dagger), "one dagger is enough");
        }

        [TestMethod]
        public void ThePatchesStayOnTheCorpseWhileTheBagIsFull()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Scarpelo);
            Spawn(plateau, ThraxPools);
            var scarpelo = RequireNpc(plateau, Scarpelo);
            SetFlag(harness, PlateauComicReliefV1.ScroungerFlag);
            harness.MoveTo(scarpelo.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, scarpelo.EntityId, ScavengerHunt));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, plateau, PlateauScavengerHuntV1.Thrax);
            PickUp(harness, loot);
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, Patch), WildernessRanjaGorgeTests.Held(harness, Dagger)));
            Assert.IsTrue(Holds(loot, Patch) && Holds(loot, Dagger), "the trophies stay on the corpse");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[ScavengerHunt].Objectives[4].State);

            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual((1U, 1U), (WildernessRanjaGorgeTests.Held(harness, Patch), WildernessRanjaGorgeTests.Held(harness, Dagger)));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[ScavengerHunt].Objectives[4].State);
        }

        [TestMethod]
        public void TheStalkMarketWaitsForScavengerHuntAndPaysForStalkerParts()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Scarpelo);
            Spawn(plateau, StalkerPools);
            var scarpelo = RequireNpc(plateau, Scarpelo);
            SetFlag(harness, PlateauComicReliefV1.ScroungerFlag);
            Assert.IsFalse(Offers(harness, scarpelo, StalkMarket), "not before Scavenger Hunt");

            SeedCompleted(harness, ScavengerHunt);
            var offer = WildernessRanjaGorgeTests.Offer(harness, scarpelo, StalkMarket);
            WildernessRanjaGorgeTests.AssertSelectable(offer, StalkMarket,
                (PlateauStalkMarketV1.ElectricBlade, 26897), (PlateauStalkMarketV1.PhysicalStaff, 27359),
                (PlateauStalkMarketV1.EmpPistol, 27105), (PlateauStalkMarketV1.HealingDisc, 28745));
            Assert.AreEqual(PlateauStalkMarketV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(scarpelo.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, scarpelo.EntityId, StalkMarket));
            Assert.AreEqual(StalkMarket, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);

            // An actuator under 50 in 100, the other two parts under 35.
            uint Held(uint template) => WildernessRanjaGorgeTests.Held(harness, template);
            percentile = PlateauStalkMarketV1.ActuatorChance;
            Assert.IsFalse(Holds(Kill(harness, plateau, Stalker), PlateauStalkMarketV1.ActuatorTemplate), "50 misses");
            percentile = PlateauStalkMarketV1.ActuatorChance - 1;
            var actuatorOnly = Kill(harness, plateau, Stalker);
            Assert.IsTrue(Holds(actuatorOnly, PlateauStalkMarketV1.ActuatorTemplate), "49 drops an actuator");
            Assert.IsFalse(Holds(actuatorOnly, PlateauStalkMarketV1.GeostabilizerTemplate) ||
                Holds(actuatorOnly, PlateauStalkMarketV1.FuelCellTemplate), "49 misses the others");
            PickUp(harness, actuatorOnly);
            percentile = PlateauStalkMarketV1.FuelCellChance;
            var stillOne = Kill(harness, plateau, Stalker);
            Assert.IsFalse(Holds(stillOne, PlateauStalkMarketV1.FuelCellTemplate), "35 misses");
            PickUp(harness, stillOne);
            percentile = PlateauStalkMarketV1.FuelCellChance - 1;
            for (var kill = 0; kill < 4; kill++)
                PickUp(harness, Kill(harness, plateau, Stalker));
            Assert.AreEqual((6U, 4U, 4U), (Held(PlateauStalkMarketV1.ActuatorTemplate), Held(PlateauStalkMarketV1.GeostabilizerTemplate),
                Held(PlateauStalkMarketV1.FuelCellTemplate)));
            var log = harness.Client.Player.Missions[StalkMarket];
            Assert.IsTrue(new[] { 1U, 2U, 3U }.All(objective => log.Objectives[objective].State == MissionObjectiveState.Completed));
            Assert.IsTrue(log.Completeable);

            // The parts are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(6U, Held(PlateauStalkMarketV1.ActuatorTemplate));
            Assert.IsTrue(harness.Client.Player.Missions[StalkMarket].Completeable);

            // A turn-in whose save fails pays nothing and keeps the parts.
            var before = harness.Context.ReadRewardTotals();
            harness.MoveTo(scarpelo.Position);
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Stalk Market turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, scarpelo.EntityId, StalkMarket, 2));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(6U, Held(PlateauStalkMarketV1.ActuatorTemplate));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, scarpelo, StalkMarket, 2,
                PlateauStalkMarketV1.Experience, PlateauStalkMarketV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1U, Held(PlateauStalkMarketV1.EmpPistol), "the selected pistol");
            Assert.AreEqual((0U, 0U, 0U), (Held(PlateauStalkMarketV1.ActuatorTemplate), Held(PlateauStalkMarketV1.GeostabilizerTemplate),
                Held(PlateauStalkMarketV1.FuelCellTemplate)), "the Scrounger keeps the parts");
        }

        [TestMethod]
        public void AScavengerHuntDoneBeforeAcceptanceCountsAndAnAbandonTakesBackTheComic()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, OBrien, Lonigan);
            var obrien = RequireNpc(plateau, OBrien);
            var lonigan = RequireNpc(plateau, Lonigan);
            SeedCompleted(harness, ScavengerHunt);
            WildernessAliaBranchesTests.GrantItem(harness, Carton, 1);
            harness.MoveTo(obrien.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, obrien.EntityId, ComicRelief));
            var log = harness.Client.Player.Missions[ComicRelief];
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (log.Objectives[9].State, log.Objectives[7].State), "Scavenger Hunt done before counts; the trade is open");

            harness.MoveTo(lonigan.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, lonigan.EntityId, ComicRelief, 7, 1));
            Assert.AreEqual((0U, 1U), (WildernessRanjaGorgeTests.Held(harness, Carton), WildernessRanjaGorgeTests.Held(harness, Comic)));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[ComicRelief].Objectives[8].State);

            new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = ComicRelief });

            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(ComicRelief));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Comic), "the comic goes with the mission");
        }

        /// <summary>The NPC's hand-in topic of this Comic Relief objective, and the objective completes there.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), ComicRelief, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, ComicRelief, objective, 1),
                $"objective {objective}");
            Assert.IsTrue(harness.Drain().OfType<ObjectiveCompletedPacket>()
                .Any(packet => packet.MissionId == ComicRelief && packet.ObjectiveId == objective));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, ComicRelief, objective, 1),
                $"objective {objective} again");
        }

        /// <summary>A mission flag saved with the character, as a SetPlayerFlag action saves it.</summary>
        private static void SetFlag(WildernessRuntimeTestHarness harness, uint flag)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, flag, 1));
            harness.Client.Player.PlayerFlags[flag] = 1;
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        /// <summary>Kills a living creature of one of these kinds as the player; returns its corpse.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, params uint[] kinds)
        {
            var creature = Living(plateau).FirstOrDefault(candidate => kinds.Contains(candidate.DbId) && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {string.Join("/", kinds)} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
            Assert.IsTrue(plateau.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);
    }
}
