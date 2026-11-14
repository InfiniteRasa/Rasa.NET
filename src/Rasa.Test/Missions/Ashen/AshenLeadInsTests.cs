using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
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
    using N = AshenLeadInNpcsV1;

    /// <summary>
    /// Ashen Desert set 1, the lead-ins, played through as a player plays them: Sergeant Bernard Ames' The Dustiest Place on
    /// Earth (1250: the Ashen Lookout to Colonel Mason Linier at Shadow's Edge Post), Captain Flynn Harris' Report to Stonewall
    /// Ridge (1273: Ashoka to Captain Marcus Emmons), Emmons' Fithik Infestation (1274: ten Fithik), Lt. Colonel Trent Ivan's
    /// Good Day to Die (1283: Shadow's Edge to Captain Akio Yamamoto in the Boneyard) and Lt. Colonel Dania Caldwell's You
    /// Can't Drink Ash (1284: Shadow's Edge to Captain Daris Linier at White Oasis Post).
    ///
    /// Why these tests: four NPCs are new and four upstream ones moved; each report is a talk at another NPC far away (the
    /// receiver hears it, the giver and the other NPCs do not); Fithik Infestation counts any Fithik by its species flag. An
    /// NPC off the navmesh, an offer at the wrong NPC, a talk heard by the wrong NPC, a kill that does not count or counts
    /// past ten, progress lost at a reload, a full bag that loses an item, or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Ashen Desert (1734) is loaded into the harness as the Crucible tests load theirs (PlateauInstanceMaps.LoadMap).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenLeadInsTests
    {
        private const uint Dustiest = AshenDustiestPlaceV1.MissionId;
        private const uint Stonewall = AshenReportToStonewallV1.MissionId;
        private const uint Fithik = AshenFithikInfestationV1.MissionId;
        private const uint GoodDay = AshenGoodDayToDieV1.MissionId;
        private const uint DrinkAsh = AshenCantDrinkAshV1.MissionId;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as the Crucible tests): fills the consumables bag
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3, as CrucibleMinefieldAndSalvageTests)
        private const uint Thrax = 531008;                 // Thrax Soldier Mark VI (World DB, Ashen Desert): not a Fithik

        /// <summary>The waypoints (World DB teleporter 209, 195, 196, 197, 317), where a player arrives.</summary>
        internal static readonly Vector3 LookoutWaypoint = new(351.5f, 310.99f, -822.11f);
        internal static readonly Vector3 ShadowsEdgeWaypoint = new(-131.44f, 295.85f, -795.17f);
        internal static readonly Vector3 AshokaWaypoint = new(396.97f, 280.34f, -458.81f);
        internal static readonly Vector3 StonewallWaypoint = new(-57.22f, 349.82f, -419.65f);
        internal static readonly Vector3 WhiteOasisWaypoint = new(-479.95f, 281.49f, 127.06f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Every NPC of set 1 stands on the navmesh at its place, with the client's name and package, friendly, and is walked
        /// to from its area's waypoint: Ames from the Ashen Lookout's, Mason Linier, Ivan and Caldwell from Shadow's Edge
        /// Post's, Harris and Yamamoto from the Ashoka Settlement's, Emmons from Stonewall Ridge's, Daris Linier from White
        /// Oasis Post's (the control point AFS-held, as by default: he is there).
        /// </summary>
        [TestMethod]
        public void TheNpcsStandOnTheNavmeshAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshen(harness);
            foreach (var (pool, name, package, from) in new (uint, uint, uint, Vector3)[]
            {
                (N.Ames, 9279, N.AmesPackage, LookoutWaypoint), (N.MasonLinier, 9260, 1389, ShadowsEdgeWaypoint),
                (N.Ivan, 9261, N.IvanPackage, ShadowsEdgeWaypoint), (N.Caldwell, 9262, N.CaldwellPackage, ShadowsEdgeWaypoint),
                (N.Harris, 9263, 1412, AshokaWaypoint), (N.Yamamoto, 9303, 1414, AshokaWaypoint), (N.Emmons, 9300, 1411, StonewallWaypoint),
                (N.DarisLinier, 9358, 1476, WhiteOasisWaypoint)
            })
            {
                var npc = Npc(desert, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                var at = N.At(pool);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at its place");
                AssertOnGround(desert, npc.Position, $"{pool}");
                Assert.IsTrue(desert.NavMesh.FindPath(desert.NavMesh.Nearest(from) ?? from, npc.Position, out var complete) != null && complete,
                    $"{pool} is walked to");
            }
            Assert.IsFalse(SpawnPoolManager.Instance.LoadedSpawnPools.Values.Single(pool => pool.DbId == N.DarisLinier).ServiceSuspended,
                "White Oasis Post AFS-held: Daris Linier serves");
        }

        [TestMethod]
        public void TheDustiestPlaceOnEarthReportsToColonelLinier() =>
            PlayReport(Dustiest, AshenDustiestPlaceV1.Report, N.Ames, N.MasonLinier,
                new[] { AshenDustiestPlaceV1.AdvancedMedPack, AshenDustiestPlaceV1.EmpGrenade, AshenDustiestPlaceV1.ResTraumaKit, AshenDustiestPlaceV1.ConcussionGrenade },
                0, AshenDustiestPlaceV1.AdvancedMedPack, AshenDustiestPlaceV1.AdvancedMedPacks,
                AshenDustiestPlaceV1.Experience, AshenDustiestPlaceV1.Credits);

        [TestMethod]
        public void ReportToStonewallRidgeReportsToEmmons() =>
            PlayReport(Stonewall, AshenReportToStonewallV1.Report, N.Harris, N.Emmons,
                new[] { AshenReportToStonewallV1.IncendiaryGrenade, AshenReportToStonewallV1.AdvancedMedPack, AshenReportToStonewallV1.EmpGrenade, AshenReportToStonewallV1.ResTraumaKit },
                0, AshenReportToStonewallV1.IncendiaryGrenade, AshenReportToStonewallV1.IncendiaryGrenades,
                AshenReportToStonewallV1.Experience, AshenReportToStonewallV1.Credits);

        [TestMethod]
        public void GoodDayToDieReportsToYamamoto() =>
            PlayReport(GoodDay, AshenGoodDayToDieV1.Report, N.Ivan, N.Yamamoto,
                new[] { AshenGoodDayToDieV1.ConcussionGrenade, AshenGoodDayToDieV1.BasicMedPack, AshenGoodDayToDieV1.FragGrenade, AshenGoodDayToDieV1.EmpBomb },
                3, AshenGoodDayToDieV1.EmpBomb, AshenGoodDayToDieV1.EmpBombs,
                AshenGoodDayToDieV1.Experience, AshenGoodDayToDieV1.Credits);

        [TestMethod]
        public void YouCantDrinkAshReportsToDarisLinier() =>
            PlayReport(DrinkAsh, AshenCantDrinkAshV1.Report, N.Caldwell, N.DarisLinier,
                new[] { AshenCantDrinkAshV1.EmpGrenade, AshenCantDrinkAshV1.ResTraumaKit, AshenCantDrinkAshV1.ConcussionGrenade, AshenCantDrinkAshV1.BasicMedPack },
                3, AshenCantDrinkAshV1.BasicMedPack, AshenCantDrinkAshV1.BasicMedPacks,
                AshenCantDrinkAshV1.Experience, AshenCantDrinkAshV1.Credits);

        /// <summary>An abandon takes the talk back; the mission is offered again, and taken again the old talk does not count.</summary>
        [TestMethod]
        public void GoodDayToDieAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshen(harness);
            var ivan = Npc(desert, N.Ivan);
            var yamamoto = Npc(desert, N.Yamamoto);
            Accept(harness, ivan, GoodDay);
            Talk(harness, yamamoto, GoodDay, AshenGoodDayToDieV1.Report);
            Assert.IsTrue(harness.Client.Player.Missions[GoodDay].Completeable);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, GoodDay));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(GoodDay));
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ivan, GoodDay), "offered again after an abandon");
            Accept(harness, ivan, GoodDay);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GoodDay, AshenGoodDayToDieV1.Report), "the old talk does not count");
            Talk(harness, yamamoto, GoodDay, AshenGoodDayToDieV1.Report);
        }

        /// <summary>
        /// Fithik Infestation: Emmons offers it (only he), no requirement; the client gets objective 1's two markers on Ashen
        /// Desert, on the Ridge's Fithik pools; a Thrax kill does not count; ten Fithik do, the count kept at a reload at five;
        /// the tenth completes it once, an eleventh changes nothing; a full bag waits, a failed save pays nothing; Emmons pays
        /// the module chosen, once.
        /// </summary>
        [TestMethod]
        public void FithikInfestationKillsTenFithik()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshen(harness, StonewallWaypoint);
            var emmons = Npc(desert, N.Emmons);
            var harris = Npc(desert, N.Harris);

            var offer = WildernessRanjaGorgeTests.Offer(harness, emmons, Fithik);
            CollectionAssert.AreEqual(new[]
                {
                    AshenFithikInfestationV1.ResistPhysical, AshenFithikInfestationV1.ThreatReduction, AshenFithikInfestationV1.DebuffElectric,
                    AshenFithikInfestationV1.ResistFire
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, harris, Fithik), "not Harris' to give");
            Accept(harness, emmons, Fithik);
            Assert.IsEmpty(Topics(harness, emmons, Fithik), "Emmons hears no objective");

            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Fithik].ObjectivesList
                .Single(objective => objective.ObjectiveId == AshenFithikInfestationV1.Kill).IndicatorList;
            Assert.HasCount(AshenFithikInfestationV1.Pools.Length, markers, "objective 1's markers");
            foreach (var marker in markers)
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");

            var pools = AshenFithikInfestationV1.Pools.Select(pool => pool.Pool).ToArray();
            Spawn(desert, pools);
            // The marked pool, a point on the open east slope since fix job A (AshenFixATests): its Fithik on the point or its
            // rings, inside the marker (on the point since fix job A) and walked to from the Stonewall waypoint. The Ridge's other automatic pool,
            // 540066, has no marker: its Fithik come down in an isolated navmesh pocket (y 325.3 under layers at 330.6 and 336.5,
            // probe of 2026-11-09) with no path to any waypoint; not asserted here (where in the pocket they land is random).
            foreach (var poolId in pools)
            {
                var at = poolId == AshenFixAV1.Pool ? AshenFixAV1.MarkerNow : AshenFithikInfestationV1.Pools.Single(pool => pool.Pool == poolId).At;
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                var fithik = Fithiks(desert).FirstOrDefault(creature => creature.SpawnPool?.DbId == poolId);
                Assert.IsNotNull(fithik, $"{poolId} puts down a living Fithik");
                Assert.IsTrue(desert.NavMesh.IsOnMesh(fithik.Position), $"{poolId}'s Fithik on the navmesh at {fithik.Position}");
                Assert.IsTrue(desert.NavMesh.FindPath(desert.NavMesh.Nearest(StonewallWaypoint) ?? StonewallWaypoint, fithik.Position,
                    out var complete) != null && complete, $"{poolId}'s Fithik walked to from the Stonewall waypoint");
                Assert.IsLessThanOrEqualTo((float)AshenFithikInfestationV1.MarkerRadius,
                    Vector2.Distance(new((float)at.X, (float)at.Z), new(fithik.Position.X, fithik.Position.Z)), $"{poolId}'s Fithik inside its marker");
            }
            Spawn(desert, PoolsOfAshen(harness, Thrax).First());
            var thrax = Alive(desert).First(creature => creature.DbId == Thrax && creature.Npc == null);
            KillOne(harness, desert, thrax);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fithik, AshenFithikInfestationV1.Kill), "a Thrax is no Fithik");

            var completed = 0;
            var completeable = 0;
            for (var kill = 1; kill <= AshenFithikInfestationV1.Kills + 1; kill++)
            {
                if (kill == 6)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                }
                if (kill <= AshenFithikInfestationV1.Kills)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fithik, AshenFithikInfestationV1.Kill), $"open before kill {kill}");
                if (!Fithiks(desert).Any())
                    Respawn(desert, pools);
                KillOne(harness, desert, Fithiks(desert).First());
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Fithik);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Fithik && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Fithik, AshenFithikInfestationV1.Kill), "ten");
            Assert.AreEqual(1, completed, "ObjectiveCompleted once");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, harris.EntityId, Fithik, 1), "not Harris' to take");
            FullBagWaits(harness, emmons, Fithik, 1, ModuleFiller);
            FailedTurnInPaysNothing(harness, emmons, Fithik, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, emmons, Fithik, 1, AshenFithikInfestationV1.Experience, AshenFithikInfestationV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Fithik), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, AshenFithikInfestationV1.ThreatReduction), "the module chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, emmons, Fithik), "done once");
        }

        /// <summary>An abandon drops the count; taken again, the kills start from none.</summary>
        [TestMethod]
        public void FithikInfestationAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshen(harness, StonewallWaypoint);
            var emmons = Npc(desert, N.Emmons);
            var pools = AshenFithikInfestationV1.Pools.Select(pool => pool.Pool).ToArray();
            Accept(harness, emmons, Fithik);
            Spawn(desert, pools);
            for (var kill = 0; kill < AshenFithikInfestationV1.Kills - 1; kill++)
            {
                if (!Fithiks(desert).Any())
                    Respawn(desert, pools);
                KillOne(harness, desert, Fithiks(desert).First());
            }
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Fithik));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, emmons, Fithik), "offered again after an abandon");
            Accept(harness, emmons, Fithik);
            if (!Fithiks(desert).Any())
                Respawn(desert, pools);
            KillOne(harness, desert, Fithiks(desert).First());
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fithik, AshenFithikInfestationV1.Kill), "one kill, not ten");
        }

        /// <summary>
        /// One report played through: the giver offers it with its four rewards (only the giver; nobody before it is taken);
        /// accepted, the giver and the other set 1 NPCs hear no objective, the receiver only this one; a reload keeps it open;
        /// the receiver's talk completes it once; the giver cannot take it (when another NPC is the receiver); a full bag waits,
        /// a failed save pays nothing; the receiver pays the stack chosen, once; it is not offered again.
        /// </summary>
        private static void PlayReport(uint mission, uint objective, uint giverPool, uint receiverPool, uint[] rewards, int choice,
            uint chosen, uint chosenCount, uint experience, uint credits)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshen(harness);
            var giver = Npc(desert, giverPool);
            var receiver = Npc(desert, receiverPool);
            var others = new[] { N.Ames, N.MasonLinier, N.Ivan, N.Caldwell, N.Harris, N.Yamamoto, N.Emmons, N.DarisLinier }
                .Where(pool => pool != giverPool && pool != receiverPool).Select(pool => Npc(desert, pool)).ToArray();

            var offer = WildernessRanjaGorgeTests.Offer(harness, giver, mission);
            CollectionAssert.AreEqual(rewards, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(), $"{mission} rewards");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, receiver, mission), "not the receiver's to give");
            foreach (var other in others)
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, other, mission), $"not {other.DbId}'s to give");

            Accept(harness, giver, mission);
            Assert.IsEmpty(Topics(harness, giver, mission), "the giver hears no objective");
            foreach (var other in others)
                Assert.IsEmpty(Topics(harness, other, mission), $"{other.DbId} hears no objective");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, mission, objective), "kept at a reload");
            harness.Drain();

            AssertHandInTopic(Conversation(harness, receiver), mission, objective);
            harness.MoveTo(receiver.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, receiver.EntityId, mission, objective, 1));
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == mission && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, giver.EntityId, mission, choice), "not the giver's to take");

            FullBagWaits(harness, receiver, mission, choice, ConsumableFiller);
            FailedTurnInPaysNothing(harness, receiver, mission, choice);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, receiver, mission, choice, experience, credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == mission), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(chosenCount, WildernessRanjaGorgeTests.Held(harness, chosen), "the stack chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, giver, mission), "done once");
        }

        private static IEnumerable<Creature> Fithiks(MapChannel desert) =>
            Alive(desert).Where(creature => creature.DbId == AshenFithikInfestationV1.Fithik && creature.Npc == null);

        private static void KillOne(WildernessRuntimeTestHarness harness, MapChannel desert, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, creature, harness.Client.Player);
        }

        /// <summary>Ashen Desert's spawn pools that hold one of these creatures (World DB).</summary>
        private static uint[] PoolsOfAshen(WildernessRuntimeTestHarness harness, params uint[] creatures) =>
            harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId &&
                    new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }.Any(creatures.Contains) &&
                    row.Mode == SpawnPoolManager.ModeAutomatic)
                .Select(row => row.Id).ToArray();

        /// <summary>Ligo Ashen Desert as the server loads it (as CrucibleOutpostIntrepidTests.EnterCrucible), the player at <paramref name="at"/> (Shadow's Edge Post's waypoint).</summary>
        internal static MapChannel EnterAshen(WildernessRuntimeTestHarness harness, Vector3? at = null)
        {
            var map = harness.Maps.MapChannelArray.TryGetValue(AshenMissionDataV1.MapContextId, out var loaded)
                ? loaded
                : PlateauInstanceMaps.LoadMap(harness, AshenMissionDataV1.MapContextId);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = AshenMissionDataV1.MapContextId;
            map.ClientList.Add(client);
            harness.MoveTo(at ?? ShadowsEdgeWaypoint);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return map;
        }
    }
}
