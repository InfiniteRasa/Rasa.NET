using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using P = MarshesPreoDasTests;
    using E = MarshesEscortsTests;
    using B = AshenMaunaBarrensNpcsV1;
    using Z = AshenEyesOnTheGroundV1;

    /// <summary>
    /// Ashen Desert set 2, the Mauna Barrens, played through as a player plays it: Lieutenant Simon Kuang's Angry Angry Atta
    /// (1242: ten Ashen Atta Soldiers and ten Ashen Atta Workers), Harmful Mutations (1243: four Mutated Atta Blood Samples for
    /// Doctor Jung Kwon) and Chemical Oversight (1244: Thrax Overseer Exing), and Captain Sera Karter's Eyes on the Ground (1245:
    /// five Atta Sensor Beacons planted in the Barrens). With them the set's places: Kuang, Kwon and Karter at Shadow's Edge Post,
    /// the six Atta pools, Exing's pool moved, the beacon spots.
    ///
    /// Why these tests: the three NPCs and the Atta are new on Ashen Desert, Exing's pool moved off a navmesh island; 1242 counts
    /// two kinds on two objectives, 1243 drops one item from one kind at a chance, 1244 and 1245 wait for their prerequisites,
    /// 1245's spots are five personal item acceptors with the beacons a mission item. A pool whose creatures no player reaches,
    /// an NPC off the navmesh, an offer before its prerequisite or at the wrong NPC, a kill that counts on the wrong objective,
    /// a drop on the wrong side of its chance or before the mission, a kill taken for a pickup, progress lost at a reload, a
    /// beacon kept after an abandon or a spot that takes two, a turn-in that pays twice or on a full bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenMaunaBarrensTests
    {
        private const uint Atta = AshenAngryAngryAttaV1.MissionId;
        private const uint Mutations = AshenHarmfulMutationsV1.MissionId;
        private const uint Oversight = AshenChemicalOversightV1.MissionId;
        private const uint Eyes = Z.MissionId;
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint Thrax = 531008;                 // Thrax Soldier Mark VI (World DB, Ashen Desert)
        private const uint ClientSampleTemplate = 97458;   // client item class 25409 "Mutated Atta Blood Samples"
        private const uint ClientBeaconTemplate = 97460;   // client item class 25411 "Atta Sensor Beacon"
        private const uint ClientSpotClass = 7847;         // UsableItemAccHumGpsBeacon
        private const uint ClientSpotName = 71;            // usablenameoverride LIGO_ASHEN_ATTA_BEACON "Atta Sensor Beacon"
        private const uint ClientSpotUseArgument = 3;      // usabledata lookup[7847][0]
        private const uint ClientBeacons = 5;              // client missionobjective 1245,1-5; "the 5 Mauna Sensor Beacons" (9940)
        private const uint SpotReady = 119, SpotPlanted = 121;

        private static readonly Vector3 Shadow = AshenLeadInsTests.ShadowsEdgeWaypoint;
        private static readonly Vector3 Stonewall = AshenLeadInsTests.StonewallWaypoint;
        private static readonly uint[] AttaPools = B.AttaPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Kuang, Kwon and Karter friendly at their places with the client's names and their packages, on the navmesh, walked to
        /// from the Shadow's Edge Post waypoint. Each Atta pool has walkable ground and puts down one living Atta of its kind (three before fix G)
        /// (class, level, hostile, the hit points of the row it copies), each on the navmesh, walked to from the Shadow's Edge
        /// Post waypoint and inside its marker. Exing comes down on the navmesh at his new place, walked to from the Stonewall
        /// Ridge and Shadow's Edge Post waypoints, inside his marker. Each beacon spot on the navmesh, 5 m or more from a wall,
        /// walked to from the Shadow's Edge Post waypoint, 20 m or more outside every hostile pool's circle.
        /// </summary>
        [TestMethod]
        public void TheMaunaBarrensPlacesStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            bool Walked(Vector3 from, Vector3 to) => nav.FindPath(nav.Nearest(from) ?? from, to, out var complete) != null && complete;
            foreach (var (pool, name, package) in new (uint, uint, uint)[] { (B.Kuang, 9291, B.KuangPackage), (B.Kwon, 9298, B.KwonPackage), (B.Karter, 9281, B.KarterPackage) })
            {
                var npc = Npc(desert, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                var at = B.At(pool);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at its place");
                AssertOnGround(desert, npc.Position, $"{pool}");
                Assert.IsTrue(Walked(Shadow, npc.Position), $"{pool} is walked to from the Shadow's Edge Post waypoint");
            }

            // Set 2's own markers (set 10's 1287 marks the same Atta pools).
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable().Where(row => row.ContentRevision == AshenMissionDataV1.Revision && row.MissionId != AshenDesignerJewelryV1.MissionId).ToList();
            Spawn(desert, AttaPools.Append(B.Exing).ToArray());
            foreach (var (poolId, creature, at) in B.AttaPools)
            {
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                var here = Alive(desert).Where(candidate => candidate.SpawnPool?.DbId == poolId).Distinct().ToArray();
                Assert.HasCount((int)AshenFixGV1.AttaCount, here, $"pool {poolId}: one (set 2's three, fix G's one)");
                var copy = B.Atta.Single(row => row.Id == creature).Copy;
                var source = harness.World.CreatureEntries.AsEnumerable().Single(row => row.Id == copy);
                var stat = harness.World.CreatureStatEntries.AsEnumerable().Single(row => row.Id == copy);
                var marker = markers.Single(row => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)at.X, (float)at.Z)) < 0.1f);
                foreach (var one in here)
                {
                    Assert.AreEqual((creature, source.Level, TargetCategory.Hostile, source.ClassId), (one.DbId, one.Level, one.TargetCategory, (uint)one.EntityClass),
                        $"pool {poolId}");
                    Assert.AreEqual(stat.Health, (int)one.Attributes[Attributes.Health].NormalMax, $"pool {poolId}: the hit points of {copy}");
                    Assert.IsTrue(nav.IsOnMesh(one.Position), $"pool {poolId}'s Atta on the navmesh at {one.Position}");
                    Assert.IsTrue(Walked(Shadow, one.Position), $"pool {poolId}'s Atta at {one.Position} walked to from the Shadow's Edge Post waypoint");
                    Assert.IsLessThanOrEqualTo((float)marker.Radius, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)),
                        FormattableString.Invariant($"pool {poolId}'s Atta at {one.Position} inside its marker at ({marker.PosX}, {marker.PosZ}) r{marker.Radius}"));
                }
            }

            // Exing where fix B put him (AshenFixBV1: set 2's spot on the mesa is not walked to on the client's floor).
            var exing = Alive(desert).Single(candidate => candidate.DbId == B.Exing && candidate.SpawnPool?.DbId == B.Exing);
            Assert.IsLessThan(3f, Vector2.Distance(new((float)AshenFixBV1.PoolNow.X, (float)AshenFixBV1.PoolNow.Z), new(exing.Position.X, exing.Position.Z)), "Exing at fix B's place");
            Assert.IsTrue(nav.IsOnMesh(exing.Position), $"Exing on the navmesh at {exing.Position}");
            Assert.IsTrue(Walked(Stonewall, exing.Position), "Exing walked to from the Stonewall Ridge waypoint");
            Assert.IsTrue(Walked(Shadow, exing.Position), "Exing walked to from the Shadow's Edge Post waypoint");
            var exingMarker = markers.Single(row => row.MissionId == Oversight);
            Assert.IsLessThanOrEqualTo((float)exingMarker.Radius, Vector2.Distance(new((float)exingMarker.PosX, (float)exingMarker.PosZ), new(exing.Position.X, exing.Position.Z)),
                "Exing inside his marker");
            var upstream = new Vector3((float)B.ExingBefore.X, (float)B.ExingBefore.Y, (float)B.ExingBefore.Z);
            Assert.IsFalse(Walked(Stonewall, nav.Nearest(upstream) ?? upstream), "upstream's spot is the navmesh island no walk reaches");
            // Exing's walks on the client's floor: AshenFixBTests.ExingIsWalkedToOnTheClientsFloor (it needs the client's files).

            var hostiles = harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId)
                .Where(row => harness.Creatures.LoadedCreatures.TryGetValue(row.Creature1Id, out var c) && c.TargetCategory == TargetCategory.Hostile).ToList();
            foreach (var (spot, index) in Z.Spots.Select((spot, index) => (P.ToVector(spot), index + 1)))
            {
                AssertOnGround(desert, spot, $"beacon spot {index}");
                Assert.IsGreaterThanOrEqualTo(5f, nav.DistanceToWall(spot, 20) ?? 0, $"beacon spot {index} 5 m or more from a wall");
                Assert.IsTrue(Walked(Shadow, spot), $"beacon spot {index} walked to from the Shadow's Edge Post waypoint");
                Assert.IsGreaterThanOrEqualTo(20f, hostiles.Min(row => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new(spot.X, spot.Z)) - (float)row.Radius),
                    $"beacon spot {index} 20 m or more outside every hostile pool's circle");
            }
        }

        /// <summary>
        /// Angry Angry Atta: Kuang offers it (only he), and Chemical Oversight waits for it; accepted, Kuang, Kwon and Karter hear
        /// no objective; two markers on each objective, on Ashen Desert; a Mutated Atta Soldier counts on neither, a Worker only
        /// on objective 2, a Soldier only on 1; the counts kept at a reload; the tenth of each completes its objective once, an
        /// eleventh changes nothing; the last makes it completeable once; a full bag waits, a failed save pays nothing; Kuang pays
        /// the weapon chosen, once; then he offers Chemical Oversight.
        /// </summary>
        [TestMethod]
        public void AngryAngryAttaKillsTenSoldiersAndTenWorkers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var kuang = Npc(desert, B.Kuang);
            var kwon = Npc(desert, B.Kwon);
            var karter = Npc(desert, B.Karter);
            Spawn(desert, AttaPools);

            var offer = WildernessRanjaGorgeTests.Offer(harness, kuang, Atta);
            CollectionAssert.AreEqual(new[]
                {
                    AshenAngryAngryAttaV1.LaserTorqueshell, AshenAngryAngryAttaV1.IcePolarityGun, AshenAngryAngryAttaV1.Chaingun, AshenAngryAngryAttaV1.PulseShotgun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kwon, Atta), "not Kwon's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Atta), "not Karter's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kuang, Oversight), "Chemical Oversight waits for Angry Angry Atta");
            Accept(harness, kuang, Atta);
            foreach (var npc in new[] { kuang, kwon, karter })
                Assert.IsEmpty(Topics(harness, npc, Atta), $"{npc.DbId} hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Atta].ObjectivesList;
            foreach (var objective in new[] { AshenAngryAngryAttaV1.Soldiers, AshenAngryAngryAttaV1.Workers })
            {
                var list = snapshot.Single(o => o.ObjectiveId == objective).IndicatorList;
                Assert.HasCount(2, list, $"objective {objective}'s markers");
                Assert.IsTrue(list.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");
            }

            KillOne(harness, desert, B.MutatedSoldier);
            KillOne(harness, desert, Thrax, spawnFrom: PoolsOf(harness, Thrax));
            harness.Drain();
            Assert.AreEqual(0U, Count(harness, Atta, AshenAngryAngryAttaV1.Soldiers) + Count(harness, Atta, AshenAngryAngryAttaV1.Workers), "a Mutated Atta and a Thrax count on neither");
            KillOne(harness, desert, B.AttaWorker);
            harness.Drain();
            Assert.AreEqual((0U, 1U), (Count(harness, Atta, AshenAngryAngryAttaV1.Soldiers), Count(harness, Atta, AshenAngryAngryAttaV1.Workers)), "a Worker counts on objective 2 only");

            var completed = new List<uint>();
            var completeable = 0;
            void Collect(IEnumerable<object> drained)
            {
                var list = drained.ToList();
                completed.AddRange(list.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == Atta).Select(packet => packet.ObjectiveId));
                completeable += list.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Atta && packet.IsCompleteable);
            }
            for (var kill = 1; kill <= AshenAngryAngryAttaV1.Kills + 1; kill++)
            {
                if (kill == 6)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((5U, 1U), (Count(harness, Atta, AshenAngryAngryAttaV1.Soldiers), Count(harness, Atta, AshenAngryAngryAttaV1.Workers)), "kept at a reload");
                    harness.Drain();
                }
                KillOne(harness, desert, B.AttaSoldier);
                Collect(harness.Drain());
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Atta, AshenAngryAngryAttaV1.Soldiers), "ten Soldiers");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Atta, AshenAngryAngryAttaV1.Workers), "one Worker");
            Assert.IsFalse(harness.Client.Player.Missions[Atta].Completeable);
            for (var kill = 2; kill <= AshenAngryAngryAttaV1.Kills; kill++)
            {
                KillOne(harness, desert, B.AttaWorker);
                Collect(harness.Drain());
            }
            CollectionAssert.AreEquivalent(new[] { AshenAngryAngryAttaV1.Soldiers, AshenAngryAngryAttaV1.Workers }, completed, "ObjectiveCompleted once each");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kwon.EntityId, Atta, 1), "not Kwon's to take");
            FullBagWaits(harness, kuang, Atta, 1, WeaponFiller);
            FailedTurnInPaysNothing(harness, kuang, Atta, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kuang, Atta, 1, AshenAngryAngryAttaV1.Experience, AshenAngryAngryAttaV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Atta), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, AshenAngryAngryAttaV1.IcePolarityGun), "the polarity gun chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kuang, Atta), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kuang, Oversight), "Kuang offers Chemical Oversight");
        }

        /// <summary>An abandon drops the counts; taken again, the kills start from none.</summary>
        [TestMethod]
        public void AngryAngryAttaAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var kuang = Npc(desert, B.Kuang);
            Spawn(desert, AttaPools);
            Accept(harness, kuang, Atta);
            for (var kill = 0; kill < 4; kill++)
                KillOne(harness, desert, B.AttaSoldier);
            harness.Drain();
            Assert.AreEqual(4U, Count(harness, Atta, AshenAngryAngryAttaV1.Soldiers));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Atta));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kuang, Atta), "offered again after an abandon");
            Accept(harness, kuang, Atta);
            KillOne(harness, desert, B.AttaSoldier);
            harness.Drain();
            Assert.AreEqual(1U, Count(harness, Atta, AshenAngryAngryAttaV1.Soldiers), "one kill, not five");
        }

        /// <summary>
        /// Harmful Mutations: Kuang offers it (not Kwon), and Eyes on the Ground waits for it; no sample drops before it;
        /// accepted, Kuang and Kwon hear no objective, two markers on the Mutated Atta pools; a sample drops from a Mutated Atta
        /// Soldier under 50 only, not from an Ashen Atta Soldier, and a death is not a pickup; the count kept at a reload; the
        /// fourth completes it once; Kuang cannot take it; a full bag waits, a failed save pays nothing and keeps the samples; Kwon
        /// takes them and pays the item chosen, once; Karter offers Eyes on the Ground.
        /// </summary>
        [TestMethod]
        public void HarmfulMutationsTakesFourSamplesToKwon()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var kuang = Npc(desert, B.Kuang);
            var kwon = Npc(desert, B.Kwon);
            var karter = Npc(desert, B.Karter);
            Spawn(desert, AttaPools);
            // One Mutated Atta a pool since fix G: respawn the pools when none is left.
            void Ready()
            {
                if (!Alive(desert).Any(candidate => candidate.DbId == B.MutatedSoldier && candidate.Npc == null))
                    Respawn(desert, AttaPools);
            }

            var offer = WildernessRanjaGorgeTests.Offer(harness, kuang, Mutations);
            CollectionAssert.AreEqual(new[]
                {
                    AshenHarmfulMutationsV1.MechGloves, AshenHarmfulMutationsV1.StealthLegs, AshenHarmfulMutationsV1.ReflectiveLegs, AshenHarmfulMutationsV1.MotorAssistGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kwon, Mutations), "not Kwon's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Eyes), "Eyes on the Ground waits for Harmful Mutations");
            Ready();
            Assert.IsFalse(Holds(Kill(harness, desert, B.MutatedSoldier), ClientSampleTemplate), "no sample before the mission");
            Accept(harness, kuang, Mutations);
            Assert.IsEmpty(Topics(harness, kuang, Mutations), "Kuang hears no objective");
            Assert.IsEmpty(Topics(harness, kwon, Mutations), "Kwon hears no objective: the samples count as they are picked up");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Mutations].ObjectivesList.Single().IndicatorList;
            Assert.HasCount(2, markers, "a marker on each Mutated Atta pool");

            percentile = 0;
            Assert.IsFalse(Holds(Kill(harness, desert, B.AttaSoldier), ClientSampleTemplate), "an Ashen Atta Soldier has no sample");
            percentile = 50;
            Ready();
            Assert.IsFalse(Holds(Kill(harness, desert, B.MutatedSoldier), ClientSampleTemplate), "50 misses");
            percentile = 49;
            Ready();
            var sample = Kill(harness, desert, B.MutatedSoldier);
            Assert.IsTrue(Holds(sample, ClientSampleTemplate), "49 drops a sample");
            Assert.AreEqual(0U, Samples(harness), "a death is not a pickup");
            PickUp(harness, sample);
            Assert.AreEqual(1U, Samples(harness));
            harness.Drain();

            var completed = 0;
            var completeable = 0;
            while (Samples(harness) < AshenHarmfulMutationsV1.SampleCount)
            {
                if (Samples(harness) == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(2U, Samples(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mutations, AshenHarmfulMutationsV1.Samples));
                    harness.Drain();
                }
                Ready();
                var loot = Kill(harness, desert, B.MutatedSoldier);
                Assert.IsTrue(Holds(loot, ClientSampleTemplate), "49 drops");
                PickUp(harness, loot);
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Mutations);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Mutations && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mutations, AshenHarmfulMutationsV1.Samples), "four");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted once, MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kuang.EntityId, Mutations, 2), "not Kuang's to take");
            FullBagWaits(harness, kwon, Mutations, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, kwon, Mutations, 2);
            Assert.AreEqual(AshenHarmfulMutationsV1.SampleCount, Samples(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kwon, Mutations, 2, AshenHarmfulMutationsV1.Experience, AshenHarmfulMutationsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Mutations), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, AshenHarmfulMutationsV1.ReflectiveLegs), "the legs chosen");
            Assert.AreEqual(0U, Samples(harness), "Kwon takes the samples");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kuang, Mutations), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karter, Eyes), "Karter offers Eyes on the Ground");
        }

        /// <summary>A sample stays on the corpse while the bag is full; an abandon keeps the samples, and taken again they count.</summary>
        [TestMethod]
        public void TheSamplesWaitForRoomAndHarmfulMutationsCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var kuang = Npc(desert, B.Kuang);
            Spawn(desert, AttaPools);
            Accept(harness, kuang, Mutations);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, desert, B.MutatedSoldier);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Samples(harness), "no room");
            Assert.IsTrue(Holds(loot, ClientSampleTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, Samples(harness), "picked up once there is room");
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Mutations));
            harness.Drain();
            Assert.AreEqual(1U, Samples(harness), "the sample is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kuang, Mutations), "offered again after an abandon");
            Accept(harness, kuang, Mutations);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mutations, AshenHarmfulMutationsV1.Samples), "one of four");
            harness.Drain();
            for (var slot = 0; slot < 3; slot++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            for (var kill = 0; kill < 3; kill++)
            {
                if (!Alive(desert).Any(candidate => candidate.DbId == B.MutatedSoldier && candidate.Npc == null))
                    Respawn(desert, AttaPools);
                PickUp(harness, Kill(harness, desert, B.MutatedSoldier));
            }
            Assert.AreEqual(4U, Samples(harness), "the held sample and three more");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mutations, AshenHarmfulMutationsV1.Samples), "four");
        }

        /// <summary>
        /// Chemical Oversight after Angry Angry Atta: Kuang offers it (not Karter); accepted, one marker on Exing; a Thrax Soldier
        /// does not count; Exing's death completes it once, kept at a reload; a full bag waits, a failed save pays nothing; Kuang
        /// pays the item chosen, once. Abandoned before the kill, it is offered again.
        /// </summary>
        [TestMethod]
        public void ChemicalOversightKillsExing()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Atta);
            var desert = AshenLeadInsTests.EnterAshen(harness, Stonewall);
            var kuang = Npc(desert, B.Kuang);
            var karter = Npc(desert, B.Karter);
            Spawn(desert, B.Exing);

            var offer = WildernessRanjaGorgeTests.Offer(harness, kuang, Oversight);
            CollectionAssert.AreEqual(new[]
                {
                    AshenChemicalOversightV1.MotorAssistBoots, AshenChemicalOversightV1.StealthGloves, AshenChemicalOversightV1.BioBoots, AshenChemicalOversightV1.ReflectiveGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Oversight), "not Karter's to give");
            Accept(harness, kuang, Oversight);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Oversight));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kuang, Oversight), "offered again after an abandon");
            Accept(harness, kuang, Oversight);
            Assert.IsEmpty(Topics(harness, kuang, Oversight), "Kuang hears no objective");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Oversight].ObjectivesList.Single().IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId);
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)AshenFixBV1.MarkerNow.X, (float)AshenFixBV1.MarkerNow.Z)), "the marker on Exing (fix B)");

            KillOne(harness, desert, Thrax, spawnFrom: PoolsOf(harness, Thrax));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Oversight, AshenChemicalOversightV1.Kill), "a Thrax Soldier is not Exing");
            KillOne(harness, desert, B.Exing);
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Oversight), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Oversight && packet.IsCompleteable), "MissionCompleteable once");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Oversight, AshenChemicalOversightV1.Kill), "kept at a reload");
            harness.Drain();

            FullBagWaits(harness, kuang, Oversight, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, kuang, Oversight, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kuang, Oversight, 0, AshenChemicalOversightV1.Experience, AshenChemicalOversightV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Oversight), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, AshenChemicalOversightV1.MotorAssistBoots), "the boots chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kuang, Oversight), "done once");
        }

        /// <summary>
        /// Eyes on the Ground after Harmful Mutations: Karter offers it (not before, not Kuang) and gives five Atta Sensor Beacons
        /// at the accept; five spots of the client's class, name and use argument, the player's own, ready, one marker on each,
        /// keyed by the client's names 155-159, on Ashen Desert; Karter hears no objective; each spot takes one beacon and
        /// completes its own objective once (any order: 3, 1, 5, 2, 4), a used spot takes no second one; a reload after one keeps
        /// the spot planted and the beacons left; the fifth makes the mission completeable once, no beacon is left; a full bag
        /// waits, a failed save pays nothing; Karter pays the item chosen, once; the spots go with the mission.
        /// </summary>
        [TestMethod]
        public void EyesOnTheGroundPlantsFiveBeaconsForKarter()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var karter = Npc(desert, B.Karter);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Eyes), "it waits for Harmful Mutations");
            SeedCompleted(harness, Mutations);
            desert = AshenLeadInsTests.EnterAshen(harness);
            karter = Npc(desert, B.Karter);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Eyes && obj.IsInWorld), "no spot before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, karter, Eyes);
            CollectionAssert.AreEqual(new[] { Z.MechBoots, Z.GravitonGloves, Z.HazmatBoots, Z.BioGloves },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(desert, B.Kuang), Eyes), "only Karter offers it");
            Accept(harness, karter, Eyes);
            Assert.AreEqual(ClientBeacons, Beacons(harness), "five beacons at the accept");
            Assert.IsEmpty(Topics(harness, karter, Eyes), "Karter hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Eyes].ObjectivesList;
            Assert.HasCount((int)ClientBeacons, snapshot);
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var spot = Z.Spots[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(spot.X, spot.Z)), $"objective {objective.ObjectiveId}'s marker on its spot");
                Assert.AreEqual(154 + objective.ObjectiveId, marker.IndicatorId, $"objective {objective.ObjectiveId}: the client's 'Atta Beacon Point {objective.ObjectiveId}'");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }

            var spots = Spots(harness, desert);
            var log = new List<object>();
            var placed = 0U;
            foreach (var number in new[] { 3, 1, 5, 2, 4 })
            {
                var spot = spots[number - 1];
                harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, desert, spot, recovers: true);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                placed++;
                Assert.AreEqual(ClientBeacons - placed, Beacons(harness), $"beacon {number} takes one");
                Assert.AreEqual((UseObjectState)SpotPlanted, spot.StateId, $"beacon {number} planted");
                Assert.IsFalse(spot.IsEnabled, $"spot {number} closed");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Eyes && packet.ObjectiveId == Z.Place(number)),
                    $"beacon {number}: its objective, once");
                Assert.AreEqual((int)placed, Enumerable.Range(1, (int)ClientBeacons).Count(other => Objective(harness, Eyes, Z.Place(other)) == MissionObjectiveState.Completed),
                    $"beacon {number}: only its own objective");
                if (placed == ClientBeacons - 1)
                    Assert.IsFalse(harness.Client.Player.Missions[Eyes].Completeable, "4 of 5: not completeable");
                if (number == 3)
                {
                    Use(harness, desert, spot, recovers: false);
                    Assert.AreEqual(4U, Beacons(harness), "one beacon a spot");
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual((4U, MissionObjectiveState.Completed), (Beacons(harness), Objective(harness, Eyes, Z.Place(3))), "kept at a reload");
                    spots = Spots(harness, desert, planted: new[] { 3 });
                    Assert.IsFalse(harness.Client.Player.Missions[Eyes].Completeable);
                }
            }
            Assert.AreEqual(0U, Beacons(harness), "no beacon left");
            Assert.IsTrue(harness.Client.Player.Missions[Eyes].Completeable, "five beacons");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Eyes && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsEmpty(Topics(harness, karter, Eyes), "Karter hears no objective");

            FullBagWaits(harness, karter, Eyes, 3, ArmorFiller);
            FailedTurnInPaysNothing(harness, karter, Eyes, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karter, Eyes, 3, Z.Experience, Z.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Eyes), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Z.BioGloves), "the gloves chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                E.Tick(harness, desert, 250);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Eyes && obj.IsInWorld), "the spots go with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karter, Eyes), "done once");
        }

        /// <summary>
        /// An abandon of Eyes on the Ground after one beacon takes the beacons left and the spots; taken again, five new beacons
        /// (not nine) and five ready spots, nothing done.
        /// </summary>
        [TestMethod]
        public void EyesOnTheGroundAbandonedTakesTheBeacons()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, Mutations);
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var karter = Npc(desert, B.Karter);
            Accept(harness, karter, Eyes);
            var spot = Spots(harness, desert)[0];
            harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, spot, recovers: true);
            E.Tick(harness, desert, 250);
            harness.Drain();
            Assert.AreEqual(4U, Beacons(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Eyes));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Beacons(harness), "the beacons go with the mission");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Eyes && obj.IsInWorld), "the spots go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karter, Eyes), "offered again after an abandon");
            Accept(harness, karter, Eyes);
            Assert.AreEqual(ClientBeacons, Beacons(harness), "five again");
            Spots(harness, desert);
            for (var number = 1; number <= ClientBeacons; number++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Eyes, Z.Place(number)), $"beacon {number} open again");
        }

        /// <summary>The five beacon spots: the player's own, of the client's class, name and use argument, at their places, ready or, for <paramref name="planted"/>, planted.</summary>
        private static DynamicObject[] Spots(WildernessRuntimeTestHarness harness, MapChannel desert, int[] planted = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var spots = Enumerable.Range(1, (int)ClientBeacons).Select(number => desert.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Eyes && obj.SceneActorRole == Z.SpotRole(number) && obj.IsInWorld)).ToArray();
            for (var number = 1; number <= spots.Length; number++)
            {
                var spot = spots[number - 1];
                var isPlanted = planted?.Contains(number) == true;
                Assert.AreEqual((ClientSpotClass, ClientSpotName), ((uint)spot.EntityClassId, spot.NameOverrideId), $"spot {number}");
                Assert.AreEqual(ClientSpotUseArgument, spot.MissionUseAction.ActionArgId, $"spot {number}: the client's use argument");
                Assert.AreEqual(P.ToVector(Z.Spots[number - 1]), spot.Position, $"spot {number} at its place");
                Assert.AreEqual(harness.Client.Player.Id, spot.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(isPlanted ? SpotPlanted : SpotReady, (uint)spot.StateId, $"spot {number} state");
                Assert.AreEqual(!isPlanted, spot.IsEnabled, $"spot {number} open");
                AssertOnGround(desert, spot.Position, $"spot {number}");
            }
            return spots;
        }

        /// <summary>A use of the spot: the request, then its recovery if one is due (as CrucibleKhulagoStrikesTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj, bool recovers)
        {
            var count = desert.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = ClientSpotUseArgument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), desert.PerformRecovery.Count, $"use of {obj.SceneActorRole}");
            if (!recovers)
                return;
            var recovery = desert.PerformRecovery.Last();
            desert.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(desert, recovery);
        }

        private static uint Beacons(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientBeaconTemplate);

        private static uint Samples(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientSampleTemplate);

        /// <summary>The kill count of an objective (counter 0).</summary>
        private static uint Count(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        /// <summary>A kill of a living creature of this row, the pools respawned when none is left (or <paramref name="spawnFrom"/> spawned first).</summary>
        private static void KillOne(WildernessRuntimeTestHarness harness, MapChannel desert, uint creature, uint[] spawnFrom = null)
        {
            if (spawnFrom != null)
                Spawn(desert, spawnFrom);
            if (!Alive(desert).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                Respawn(desert, AttaPools.Append(B.Exing).ToArray());
            var one = Alive(desert).First(candidate => candidate.DbId == creature && candidate.Npc == null);
            harness.MoveTo(one.Position);
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
        }

        /// <summary>One automatic Ashen Desert pool that holds this creature (World DB).</summary>
        private static uint[] PoolsOf(WildernessRuntimeTestHarness harness, uint creature) =>
            harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId &&
                    new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }.Contains(creature) &&
                    row.Mode == SpawnPoolManager.ModeAutomatic)
                .Select(row => row.Id).Take(1).ToArray();

        /// <summary>Completed Ashen missions in the character's history (as CrucibleOutpostIntrepidTests.SeedCompleted), then a reload.</summary>
        private static void SeedCompleted(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed)
                        unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = AshenMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            WildernessRanjaGorgeTests.Reload(harness);
        }
    }
}
