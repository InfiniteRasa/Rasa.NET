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
using Rasa.Packets.MapChannel.Client;
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
    using B = AshenBoneyardNpcsV1;
    using Job = AshenXanxlessJobV1;
    using Fallen = AshenFallenSoldiersV1;
    using Stalking = AshenStalkingTheStalkersV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 4, the Boneyard, played through as a player plays it: Lieutenant Jan Blood's Xanxless Job (1262: six
    /// Xanx), Captain Akio Yamamoto's Fallen Soldiers (1263: six dog tags from the fallen) and Lieutenant Gabriel Infanger's
    /// Stalking the Stalkers (1272: one Desert Stalker). With them the set's places: the Xanx and Stalker pools in the Boneyard,
    /// their first spawns and their respawns.
    ///
    /// Why these tests: the Xanx pools and the Stalker are new, and the Boneyard has a second floor under parts of it (Ashoka's
    /// lower level): a creature that comes down, or back, on the wrong floor is out of a player's sight (Ashen to-fix row 1:
    /// the respawns of an upstream pool stood 6 m under the client's floor). 1262 counts a World DB row, 1263 six personal
    /// bodies each searched once, 1272 one kill of a new row. A creature no player reaches, a marker that misses its target, an
    /// offer at the wrong NPC, a kill or a body that counts twice or not at all, progress lost at a reload or kept after an
    /// abandon, or a turn-in that pays twice or on a full bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenBoneyardTests
    {
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint Fithik = 540018;                // Fithik - Ashen Desert (World DB)
        private const uint ThraxSoldier = 531008;          // Thrax Soldier - Ashen Desert (World DB, the Boneyard's pools)
        private const int Respawns = 6;                    // the rounds of respawns sampled after the first spawn
        private const float FloorReach = 1;                // a creature stands within this of the client's floor
        private const float PoolHeight = 2;                // and within this of its pool's height (not on a floor under it)

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly uint[] XanxPools = B.XanxPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] StalkerPools = B.StalkerPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Each Xanx pool puts down three living Xanx (540020, hostile), each Stalker pool one Desert Stalker (hostile, the client's
        /// name); at the first spawn and after each of six rounds of kills and respawns, every one of them stands on the navmesh,
        /// within 1 m of the client's floor (no client floor more than 1 m over it, where a player would stand and not see it),
        /// within 2 m of its pool's height (not on Ashoka's lower level under the Boneyard), walked to from the Ashoka Settlement
        /// waypoint and inside a marker of its mission.
        /// </summary>
        [TestMethod]
        public void TheBoneyardCreaturesComeDownAndBackOnTheFloorPlayersWalk()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable().Where(row => row.ContentRevision == AshenMissionDataV1.Revision).ToList();
            var pools = XanxPools.Concat(StalkerPools).ToArray();
            Spawn(desert, pools);
            foreach (var poolId in pools)
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, desert.SpawnPools.Single(loaded => loaded.DbId == poolId)), $"{poolId} has walkable ground");

            var failures = new List<string>();
            for (var round = 0; round <= Respawns; round++)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool != null && pools.Contains(candidate.SpawnPool.DbId)).Distinct().ToList();
                foreach (var poolId in XanxPools)
                    Assert.AreEqual((int)B.XanxPerPool, here.Count(one => one.SpawnPool.DbId == poolId), $"round {round}: pool {poolId} has three");
                foreach (var poolId in StalkerPools)
                    Assert.AreEqual(1, here.Count(one => one.SpawnPool.DbId == poolId), $"round {round}: pool {poolId} has one");
                var near = F.Near(geometry, here.Select(one => one.Position).ToList(), 1);
                for (var i = 0; i < here.Count; i++)
                {
                    var one = here[i];
                    var label = FormattableString.Invariant($"round {round}: pool {one.SpawnPool.DbId}'s {one.DbId} at {one.Position}");
                    var stalker = StalkerPools.Contains(one.SpawnPool.DbId);
                    Assert.AreEqual(stalker ? B.Stalker : B.Xanx, one.DbId, label);
                    Assert.AreEqual(TargetCategory.Hostile, one.TargetCategory, label);
                    if (stalker)
                        Assert.AreEqual(B.StalkerName, one.NameId, $"{label}: a Class XI Desert Stalker");
                    if (!nav.IsOnMesh(one.Position))
                        failures.Add($"{label}: off the navmesh");
                    var path = nav.FindPath(nav.Nearest(Ashoka) ?? Ashoka, one.Position, out var complete);
                    if (path == null || !complete)
                        failures.Add($"{label}: no walk from the Ashoka Settlement waypoint");
                    var floor = F.FloorAt(near[i], one.Position, one.Position.Y - 3, one.Position.Y + 8);
                    if (floor is not { } y || MathF.Abs(y - one.Position.Y) > FloorReach)
                        failures.Add(FormattableString.Invariant($"{label}: the client's floor at {floor}"));
                    // The floor window above sees 3 m down and 8 m up: a creature on Ashoka's lower level (33 m under the
                    // Boneyard floor) would pass it. Each stands at its pool's height (the Boneyard floor), give or take PoolHeight.
                    if (MathF.Abs(one.Position.Y - one.SpawnPool.Position.Y) > PoolHeight)
                        failures.Add(FormattableString.Invariant($"{label}: {one.Position.Y - one.SpawnPool.Position.Y:0.00} m from its pool's height"));
                    var mission = stalker ? Stalking.MissionId : Job.MissionId;
                    if (!markers.Any(marker => marker.MissionId == mission &&
                            Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)) <= marker.Radius))
                        failures.Add($"{label}: outside the {mission} markers");
                }
                if (round == Respawns)
                    break;
                foreach (var one in here)
                {
                    one.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
                }
                Respawn(desert, pools);
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Xanxless Job: Blood offers it (not Yamamoto, not Infanger); accepted, Blood hears no objective; a marker on each Xanx
        /// pool, on Ashen Desert; a Fithik and a Thrax Soldier do not count; each Xanx counts once, kept at a reload; the sixth
        /// completes it once and makes it completeable once, a seventh changes nothing; a full bag waits, a failed save pays
        /// nothing; Blood pays the item chosen, once.
        /// </summary>
        [TestMethod]
        public void XanxlessJobKillsSixXanx()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var blood = Npc(desert, B.Blood);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            var infanger = Npc(desert, B.Infanger);
            Spawn(desert, XanxPools);

            var offer = WildernessRanjaGorgeTests.Offer(harness, blood, Job.MissionId);
            CollectionAssert.AreEqual(new[] { Job.BioLegs, Job.MechLegs, Job.GravitonBoots, Job.MotorAssistHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Job.MissionId), "not Yamamoto's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Job.MissionId), "not Infanger's to give");
            Accept(harness, blood, Job.MissionId);
            Assert.IsEmpty(Topics(harness, blood, Job.MissionId), "Blood hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Job.MissionId].ObjectivesList.Single().IndicatorList;
            Assert.HasCount(B.XanxPools.Length, markers, "a marker on each Xanx pool");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            KillOne(harness, desert, Fithik, spawnFrom: PoolsOf(harness, Fithik));
            KillOne(harness, desert, ThraxSoldier, spawnFrom: PoolsOf(harness, ThraxSoldier));
            harness.Drain();
            Assert.AreEqual(0U, Count(harness), "a Fithik and a Thrax Soldier are no Xanx");

            var completed = 0;
            var completeable = 0;
            for (var kill = 1; kill <= Job.Kills + 1; kill++)
            {
                if (kill == 4)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(3U, Count(harness), "kept at a reload");
                    harness.Drain();
                }
                KillOne(harness, desert, B.Xanx);
                var packets = harness.Drain();
                completed += packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Job.MissionId);
                completeable += packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Job.MissionId && packet.IsCompleteable);
                if (kill < Job.Kills)
                    Assert.IsFalse(harness.Client.Player.Missions[Job.MissionId].Completeable, $"{kill} of 6: not completeable");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Job.MissionId, Job.Kill), "six Xanx");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, yamamoto.EntityId, Job.MissionId, 1), "not Yamamoto's to take");
            FullBagWaits(harness, blood, Job.MissionId, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, blood, Job.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, blood, Job.MissionId, 1, Job.Experience, Job.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Job.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Job.MechLegs), "the legs chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Job.MissionId), "done once");
        }

        /// <summary>An abandon of Xanxless Job drops the count; taken again, the kills start from none.</summary>
        [TestMethod]
        public void XanxlessJobAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var blood = Npc(desert, B.Blood);
            Spawn(desert, XanxPools);
            Accept(harness, blood, Job.MissionId);
            for (var kill = 0; kill < 4; kill++)
                KillOne(harness, desert, B.Xanx);
            harness.Drain();
            Assert.AreEqual(4U, Count(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Job.MissionId));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, blood, Job.MissionId), "offered again after an abandon");
            Accept(harness, blood, Job.MissionId);
            KillOne(harness, desert, B.Xanx);
            harness.Drain();
            Assert.AreEqual(1U, Count(harness), "one kill, not five");
        }

        /// <summary>
        /// Fallen Soldiers: no fallen before the mission; Yamamoto offers it (not Blood, not Infanger); accepted, Yamamoto hears no
        /// objective; six fallen of the player's own (Fallen); a full bag leaves the tag on the body; one use each, a tag each,
        /// a second use of a searched body nothing; kept at a reload (a searched body stays searched); the sixth tag completes
        /// 1 once and makes it completeable once; a full bag waits at the turn-in, a failed save pays nothing and keeps the tags;
        /// Yamamoto pays the weapon chosen, once, and takes the six tags. In every packet the test drains (all those of the
        /// searches), none for the hidden objective 2: no ObjectiveCompleted, UpdateObjectiveCounter or UpdateObjectiveItemCounter.
        /// </summary>
        [TestMethod]
        public void FallenSoldiersTakesSixDogTagsToYamamoto()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var log = new List<object>();
            IReadOnlyList<object> Drain()
            {
                var drained = harness.Drain();
                log.AddRange(drained);
                return drained;
            }
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            var blood = Npc(desert, B.Blood);
            var infanger = Npc(desert, B.Infanger);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Fallen.MissionId), "no fallen before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, yamamoto, Fallen.MissionId);
            CollectionAssert.AreEqual(new[] { Fallen.LaserTorqueshell, Fallen.KineticStaff, Fallen.VirulentLeechGun, Fallen.IncendiaryShotgun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Fallen.MissionId), "not Blood's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Fallen.MissionId), "not Infanger's to give");
            Accept(harness, yamamoto, Fallen.MissionId);
            Assert.IsEmpty(Topics(harness, yamamoto, Fallen.MissionId), "Yamamoto hears no objective");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Fallen.MissionId].ObjectivesList.Single().IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");

            var corpses = Bodies(harness, desert);
            foreach (var corpse in corpses)
                Assert.IsLessThanOrEqualTo((float)Fallen.FieldRadius, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(corpse.Position.X, corpse.Position.Z)),
                    $"{corpse.SceneActorRole} inside the marker");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Search(harness, desert, corpses[0], log);
            Assert.AreEqual(0U, Tags(harness), "no room");
            Assert.IsTrue(corpses[0].IsEnabled, "the body waits");
            Assert.AreEqual((UseObjectState)Fallen.ClosedState, corpses[0].StateId);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            var completed = 0;
            var completeable = 0;
            for (var index = 0; index < Fallen.Tags; index++)
            {
                if (index == 3)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Drain();
                    Assert.AreEqual(3U, Tags(harness), "kept at a reload");
                    corpses = Bodies(harness, desert);
                    Assert.IsFalse(corpses[2].IsEnabled, "a searched body stays searched");
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fallen.MissionId, Fallen.Collect), $"open before tag {index + 1}");
                Search(harness, desert, corpses[index], log);
                Assert.AreEqual((uint)index + 1, Tags(harness), $"tag {index + 1}");
                Assert.AreEqual((UseObjectState)Fallen.OpenedState, corpses[index].StateId);
                Assert.IsFalse(corpses[index].IsEnabled, "searched once");
                if (index == 0)
                {
                    TrySearch(harness, desert, corpses[0], log);
                    Assert.AreEqual(1U, Tags(harness), "a body gives one tag");
                }
                var drained = Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Fallen.MissionId && packet.ObjectiveId == Fallen.Collect);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Fallen.MissionId && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Fallen.MissionId, Fallen.Collect), "six tags");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");

            Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, blood.EntityId, Fallen.MissionId, 0), "not Blood's to take");
            Drain();
            FullBagWaits(harness, yamamoto, Fallen.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, yamamoto, Fallen.MissionId, 0);
            Assert.AreEqual(Fallen.Tags, Tags(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, yamamoto, Fallen.MissionId, 0, Fallen.Experience, Fallen.Credits);
            Assert.AreEqual(1, Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Fallen.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Fallen.LaserTorqueshell), "the rifle chosen");
            Assert.AreEqual(0U, Tags(harness), "Yamamoto takes the tags");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Fallen.MissionId), "done once");
            Drain();
            Assert.IsTrue(log.OfType<ObjectiveCompletedPacket>().Any(packet => packet.MissionId == Fallen.MissionId), "the log holds the run's packets");
            Assert.AreEqual(0, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Fallen.MissionId && packet.ObjectiveId == Fallen.Tally) +
                log.OfType<UpdateObjectiveCounterPacket>().Count(packet => packet.MissionId == Fallen.MissionId && packet.ObjectiveId == Fallen.Tally) +
                log.OfType<UpdateObjectiveItemCounterPacket>().Count(packet => packet.MissionId == Fallen.MissionId && packet.ObjectiveId == Fallen.Tally),
                "nothing of the hidden objective 2 goes to the client");
        }

        /// <summary>An abandon takes the tags and the fallen; taken again, six whole bodies and no tag counted.</summary>
        [TestMethod]
        public void FallenSoldiersAbandonedTakesTheTagsAndTheFallen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            Accept(harness, yamamoto, Fallen.MissionId);
            var corpses = Bodies(harness, desert);
            Search(harness, desert, corpses[0]);
            Search(harness, desert, corpses[1]);
            Assert.AreEqual(2U, Tags(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Fallen.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Tags(harness), "the tags go");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Fallen.MissionId && obj.IsInWorld), "the fallen go");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, yamamoto, Fallen.MissionId), "offered again after an abandon");
            Accept(harness, yamamoto, Fallen.MissionId);
            Assert.IsTrue(Bodies(harness, desert).All(corpse => corpse.IsEnabled), "six whole bodies");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fallen.MissionId, Fallen.Collect));
        }

        /// <summary>
        /// Stalking the Stalkers: Infanger offers it (not Blood, not Yamamoto); abandoned before the kill it is offered again;
        /// accepted, Infanger hears no objective; a marker on each Stalker pool, on Ashen Desert; a Xanx does not count; a Desert
        /// Stalker's death completes it once (ObjectiveCompleted and MissionCompleteable once), kept at a reload; a second
        /// Stalker changes nothing; a full bag waits, a failed save pays nothing; Infanger pays the weapon chosen, once.
        /// </summary>
        [TestMethod]
        public void StalkingTheStalkersDestroysADesertStalker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var infanger = Npc(desert, B.Infanger);
            var blood = Npc(desert, B.Blood);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            Spawn(desert, StalkerPools.Concat(XanxPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, infanger, Stalking.MissionId);
            CollectionAssert.AreEqual(new[] { Stalking.PulseDisperser, Stalking.CryogenicInjector, Stalking.IncendiaryNetGun, Stalking.ElectricPistol },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, blood, Stalking.MissionId), "not Blood's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, yamamoto, Stalking.MissionId), "not Yamamoto's to give");
            Accept(harness, infanger, Stalking.MissionId);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Stalking.MissionId));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, infanger, Stalking.MissionId), "offered again after an abandon");
            Accept(harness, infanger, Stalking.MissionId);
            Assert.IsEmpty(Topics(harness, infanger, Stalking.MissionId), "Infanger hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Stalking.MissionId].ObjectivesList.Single().IndicatorList;
            Assert.HasCount(B.StalkerPools.Length, markers, "a marker on each Stalker pool");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            KillOne(harness, desert, B.Xanx);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Stalking.MissionId, Stalking.Destroy), "a Xanx is no Stalker");
            KillOne(harness, desert, B.Stalker);
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Stalking.MissionId), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Stalking.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Stalking.MissionId, Stalking.Destroy), "kept at a reload");
            harness.Drain();
            KillOne(harness, desert, B.Stalker);
            Assert.AreEqual(0, harness.Drain().OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Stalking.MissionId), "the second Stalker changes nothing");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, blood.EntityId, Stalking.MissionId, 2), "not Blood's to take");
            FullBagWaits(harness, infanger, Stalking.MissionId, 2, WeaponFiller);
            FailedTurnInPaysNothing(harness, infanger, Stalking.MissionId, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, infanger, Stalking.MissionId, 2, Stalking.Experience, Stalking.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Stalking.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Stalking.IncendiaryNetGun), "the net gun chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, infanger, Stalking.MissionId), "done once");
        }

        /// <summary>1262's count.</summary>
        private static uint Count(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Job.MissionId].Objectives[Job.Kill].Counters[0];

        private static uint Tags(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, Fallen.TagTemplate);

        /// <summary>The six fallen, in their order: the player's own, of their classes and name, on the ground, walked to from the Ashoka Settlement waypoint.</summary>
        private static DynamicObject[] Bodies(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            harness.Manager.Scenes.Tick(desert);
            var corpses = new DynamicObject[Fallen.CorpsesAt.Length];
            for (var index = 0; index < corpses.Length; index++)
            {
                var corpse = desert.DynamicObjects.Single(obj => obj.SceneMissionId == Fallen.MissionId &&
                    obj.SceneActorRole == Fallen.CorpseRole(index) && obj.IsInWorld);
                var at = Fallen.CorpsesAt[index];
                Assert.AreEqual(Fallen.CorpseClasses[index % Fallen.CorpseClasses.Length], (uint)corpse.EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), corpse.Position);
                Assert.AreEqual(harness.Client.Player.Id, corpse.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(desert, corpse.Position, $"body {index + 1}");
                Assert.IsTrue(desert.NavMesh.FindPath(desert.NavMesh.Nearest(Ashoka) ?? Ashoka, corpse.Position, out var complete) != null && complete,
                    $"body {index + 1} is walked to");
                corpses[index] = corpse;
            }
            return corpses;
        }

        private static void Search(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject corpse, List<object> log = null)
        {
            harness.MoveTo(corpse.Position + new Vector3(0, 0, 1));
            log?.AddRange(harness.Drain());
            Use(harness, desert, corpse, Fallen.UseArgument, recovers: true);
        }

        /// <summary>A use the server may refuse at once or after its recovery: the caller checks that nothing came of it.</summary>
        private static void TrySearch(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject corpse, List<object> log = null)
        {
            harness.MoveTo(corpse.Position + new Vector3(0, 0, 1));
            log?.AddRange(harness.Drain());
            var count = desert.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = corpse.EntityId, ActionId = ActionId.UseObject, ActionArgId = Fallen.UseArgument
            });
            if (desert.PerformRecovery.Count > count)
            {
                var recovery = desert.PerformRecovery.Last();
                desert.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(desert, recovery);
            }
            log?.AddRange(harness.Drain());
        }

        /// <summary>A kill of a living creature of this row, the set's pools respawned when none is left (or <paramref name="spawnFrom"/> spawned first).</summary>
        private static void KillOne(WildernessRuntimeTestHarness harness, MapChannel desert, uint creature, uint[] spawnFrom = null)
        {
            if (spawnFrom != null)
                Spawn(desert, spawnFrom);
            if (!Alive(desert).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                Respawn(desert, XanxPools.Concat(StalkerPools).Concat(spawnFrom ?? Array.Empty<uint>()).ToArray());
            var one = Alive(desert).First(candidate => candidate.DbId == creature && candidate.Npc == null);
            harness.MoveTo(one.Position);
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
        }

        /// <summary>One automatic Ashen Desert pool that holds this creature (World DB), none of the set's.</summary>
        private static uint[] PoolsOf(WildernessRuntimeTestHarness harness, uint creature) =>
            harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId &&
                    new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }.Contains(creature) &&
                    row.Mode == SpawnPoolManager.ModeAutomatic && !XanxPools.Contains(row.Id) && !StalkerPools.Contains(row.Id))
                .Select(row => row.Id).Take(1).ToArray();

        private static string MapName(WildernessRuntimeTestHarness harness) =>
            harness.World.MapInfoEntries.AsNoTracking().Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName;
    }
}
