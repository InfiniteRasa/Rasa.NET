extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Plateau;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauTrinityBridgeLogosTests;

    /// <summary>
    /// Warrior Algona's Holy Vengeance (1977), played through as a player plays it: taken at Algona by the hospital in the
    /// squad's copy of Velon Hollow (2136), the outer and the inner Bane outpost cleared (their spawn pools all dead in
    /// the copy), the Law of Asperion taken from its crate in the control room, handed to Luminary Sampei at New Velon.
    ///
    /// Why these tests: two objectives are spawn pools cleared in a squad copy, a count no kill tally can make; the book's
    /// crate must give only once the inner outpost is clear; the followers must never fail the mission.
    ///
    /// The shipped Velon Hollow navmesh cuts the hospital and the control room off (a bug report is filed upstream):
    /// there the followers' path check is Inconclusive.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauHolyVengeanceTests
    {
        private const uint HolyVengeance = PlateauHolyVengeanceV1.MissionId;
        private const uint VelonHollow = PlateauHolyVengeanceV1.MapContextId;

        private Func<MissionApplication> _missions;

        [TestInitialize]
        public void KeepTheServersMissions() => _missions = SpawnPoolClearedProgress.Missions;

        [TestCleanup]
        public void PutBackTheServersMissions() => SpawnPoolClearedProgress.Missions = _missions;

        [TestMethod]
        public void HolyVengeanceClearsTheOutpostAndTakesTheLawOfAsperionToSampei()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness);

            var followers = Followers(harness, copy);
            Assert.AreEqual(PlateauHolyVengeanceV1.AlgonaEscort, followers[0].DbId);
            Assert.AreEqual(PlateauTheseOldBonesV1.AlgonaName, followers[0].NameId);
            foreach (var follower in followers)
            {
                Assert.AreEqual(harness.Client.Player.Id, follower.SpawnPool.FollowOwnerCharacterId, "he follows the player");
                Assert.AreNotEqual(TargetCategory.Hostile, follower.TargetCategory);
                AssertStandsOnNavMesh(copy, follower.Position);
            }
            var crate = Crate(harness, copy);
            Assert.AreEqual(PlateauHolyVengeanceV1.CrateClass, (uint)crate.EntityClassId);
            Assert.AreEqual(PlateauHolyVengeanceScene.ReadyState, (uint)crate.StateId);
            AssertStandsOnNavMesh(copy, crate.Position, 0.5f);
            var path = copy.NavMesh.FindPath(ToVector(PlateauHolyVengeanceV1.FollowersAt[0]), crate.Position, out var joined);
            if (path == null || !joined)
                Assert.Inconclusive("No navmesh path from the hospital to the control room: the Velon Hollow navmesh is split (upstream bug report).");

            // The crate waits for the inner outpost.
            Use(harness, copy, crate);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "nothing before 3 is open");
            Assert.AreEqual(PlateauHolyVengeanceScene.ReadyState, (uint)crate.StateId);

            Clear(harness, copy, PlateauHolyVengeanceV1.OuterPools);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Outer));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Inner));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, PlateauHolyVengeanceV1.Recover));
            Clear(harness, copy, PlateauHolyVengeanceV1.InnerPools);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Inner));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Recover));

            Use(harness, copy, crate);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "the Law of Asperion");
            Assert.AreEqual(PlateauHolyVengeanceScene.TakenState, (uint)crate.StateId);
            Use(harness, copy, crate);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "one");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Recover));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Escort));
            Assert.IsTrue(harness.Client.Player.Missions[HolyVengeance].Completeable);

            // They walk back to the hospital and are gone there.
            for (var tick = 0; tick < 1200 && Followers(harness, copy, required: false).Any(follower => follower != null); tick++)
                Tick(harness, copy, 1000);
            Assert.IsTrue(Followers(harness, copy, required: false).All(follower => follower == null),
                "the three are home and gone: " + string.Join(", ", Followers(harness, copy, required: false).Select(follower => follower?.Position.ToString() ?? "gone")));

            Return(harness, plateau);
            Spawn(plateau, PlateauHolyVengeanceV1.Sampei);
            var sampei = RequireNpc(plateau, PlateauHolyVengeanceV1.Sampei);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sampei, HolyVengeance, 1,
                PlateauHolyVengeanceV1.Experience, PlateauHolyVengeanceV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "Sampei keeps the book");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BioLegs), "the second choice");
        }

        /// <summary>A follower killed: nothing fails, and he does not come back.</summary>
        [TestMethod]
        public void HolyVengeanceGoesOnWhenAFollowerDies()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness);
            var algona = Followers(harness, copy)[0];
            algona.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, algona, null);
            Tick(harness, copy, 250);
            var mission = harness.Client.Player.Missions[HolyVengeance];
            Assert.AreEqual(MissionState.Active, mission.State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauHolyVengeanceV1.Escort].State);
            Tick(harness, copy, 1000);
            Assert.IsNull(Followers(harness, copy, required: false)[0], "Algona stays dead");
        }

        /// <summary>The outer outpost already cleared when the mission is taken: 1 is done on the accept.</summary>
        [TestMethod]
        public void HolyVengeanceCountsAnOutpostAlreadyCleared()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoVelonHollow(harness);
            SpawnPoolClearedProgress.Missions = () => harness.Manager;
            KillPools(harness, copy, PlateauHolyVengeanceV1.OuterPools);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
            Accept(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Outer));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Inner));
        }

        private static MapChannel AcceptAtAlgona(WildernessRuntimeTestHarness harness)
        {
            var copy = IntoVelonHollow(harness);
            SpawnPoolClearedProgress.Missions = () => harness.Manager;
            Accept(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Outer));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Inner));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, PlateauHolyVengeanceV1.Recover));
            Tick(harness, copy, 250);
            return copy;
        }

        /// <summary>Algona at the hospital; 1977 offered after These Old Bones and accepted.</summary>
        private static void Accept(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            Spawn(copy, PlateauTheseOldBonesV1.AlgonaPool);
            var algona = RequireNpc(copy, PlateauTheseOldBonesV1.AlgonaPool);
            Assert.IsFalse(Offers(harness, algona, HolyVengeance), "it waits for These Old Bones");
            SeedCompleted(harness, PlateauVillageOfTheDamnedV1.MissionId, PlateauTheseOldBonesV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, algona, HolyVengeance);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauHolyVengeanceV1.GravitonLegs, PlateauHolyVengeanceV1.BioLegs,
                    PlateauHolyVengeanceV1.MechLegs, PlateauHolyVengeanceV1.StealthLegs
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(algona.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, algona.EntityId, HolyVengeance));
            harness.Drain();
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
        }

        private static MapChannel IntoVelonHollow(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(VelonHollow))
                LoadMap(harness, VelonHollow);
            var copy = ThroughTheDoor(harness, VelonHollow);
            Assert.IsTrue(copy.IsSquadInstance);
            // An earlier test's copy can hold the outposts cleared: they are back as after their hour.
            foreach (var pool in copy.SpawnPools.Where(pool => PlateauHolyVengeanceV1.OuterPools.Concat(PlateauHolyVengeanceV1.InnerPools).Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            return copy;
        }

        /// <summary>Every creature of the pools killed in the copy, then the spawn pools' pass tells the player.</summary>
        private static void Clear(WildernessRuntimeTestHarness harness, MapChannel copy, uint[] pools)
        {
            KillPools(harness, copy, pools);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
        }

        /// <summary>
        /// The pools standing in the copy (an earlier test's copy can hold them dead: they are put back as after their
        /// hour, as PlateauVelonHollowBaneTests.RecoverTheArtifacts does), then every creature of them killed.
        /// </summary>
        private static void KillPools(WildernessRuntimeTestHarness harness, MapChannel copy, uint[] pools)
        {
            foreach (var pool in copy.SpawnPools.Where(pool => pools.Contains(pool.DbId)))
            {
                foreach (var straggler in Here(copy).Where(creature => creature.SpawnPool == pool).ToArray())
                    Kill(harness, copy, straggler);
                pool.ClearedAtUtcMs = 0;
            }
            Spawn(copy, pools);
            var standing = Here(copy).Where(creature => pools.Contains(creature.SpawnPool?.DbId ?? 0)).ToArray();
            Assert.IsTrue(standing.Length >= pools.Length, "the pools stand");
            foreach (var creature in standing)
                Kill(harness, copy, creature);
        }

        private static Creature[] Followers(WildernessRuntimeTestHarness harness, MapChannel copy, bool required = true)
        {
            var followers = PlateauHolyVengeanceScene.Followers.Select(role => SceneCreature(harness, copy, role)).ToArray();
            if (required)
                for (var index = 0; index < followers.Length; index++)
                    Assert.IsNotNull(followers[index], $"{PlateauHolyVengeanceScene.Followers[index]} is there");
            return followers;
        }

        private static DynamicObject Crate(WildernessRuntimeTestHarness harness, MapChannel map)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, HolyVengeance).Select(scene => scene.RunId).ToHashSet();
            var crate = map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == HolyVengeance &&
                candidate.SceneActorRole == PlateauHolyVengeanceScene.Crate && candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
            Assert.IsNotNull(crate, "the crate is there");
            return crate;
        }

        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PlateauHolyVengeanceScene.UseArgument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        private static Vector3 ToVector(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);

        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, HolyVengeance).Select(scene => scene.RunId).ToHashSet();
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId));
        }

        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint objective) =>
            harness.Client.Player.Missions[HolyVengeance].Objectives[objective].State;

        private static void Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
        }

        private static void AssertStandsOnNavMesh(MapChannel map, Vector3 position, float tolerance = 1f)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.AreEqual(ground.Value, position.Y, tolerance, $"{position} on the ground");
        }

        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }
    }
}
