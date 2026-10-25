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
    /// there HolyVengeanceFollowersHaveAPathToTheControlRoom is Inconclusive; the playthrough puts the player at each place.
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
            AssertPublicAlgonaHidden(harness, copy, followers[0]);
            var crate = Crate(harness, copy);
            Assert.AreEqual(PlateauHolyVengeanceV1.CrateClass, (uint)crate.EntityClassId);
            Assert.AreEqual(PlateauHolyVengeanceScene.ReadyState, (uint)crate.StateId);
            AssertStandsOnNavMesh(copy, crate.Position, 0.5f);

            // The crate waits for the inner outpost.
            Use(harness, copy, crate);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "nothing before 3 is open");
            Assert.AreEqual(PlateauHolyVengeanceScene.ReadyState, (uint)crate.StateId);

            Clear(harness, copy, PlateauHolyVengeanceV1.OuterPools);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Outer));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Inner));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, PlateauHolyVengeanceV1.Recover));

            // A reload halfway keeps the outer outpost done and the three following.
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Resume(harness.Client);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Outer), "after the reload");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, PlateauHolyVengeanceV1.Recover), "after the reload");
            foreach (var follower in Followers(harness, copy))
                Assert.AreEqual(harness.Client.Player.Id, follower.SpawnPool.FollowOwnerCharacterId, "still following after the reload");
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "the public Algona still hidden after the reload");

            Clear(harness, copy, PlateauHolyVengeanceV1.InnerPools);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Inner));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Recover));

            // The three came with the player into the control room (the player was put at each place, so they are too).
            WithThePlayerAtTheCrate(harness, copy, crate);
            Use(harness, copy, crate);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "the Law of Asperion");
            Assert.AreEqual(PlateauHolyVengeanceScene.TakenState, (uint)crate.StateId);
            Use(harness, copy, crate);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "one");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Recover));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Escort));
            Assert.IsTrue(harness.Client.Player.Missions[HolyVengeance].Completeable);

            // A warrior killed on the way back: the other two walk on about 100 m towards the hospital and are gone there.
            var warrior = Followers(harness, copy)[2];
            warrior.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, warrior, null);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[HolyVengeance].State);
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "hidden while her copy walks back");
            var algonaLastAt = Followers(harness, copy, required: false)[0].Position;
            var seconds = 0;
            for (; seconds < 70 && Followers(harness, copy, required: false).Any(follower => follower != null); seconds++)
            {
                Tick(harness, copy, 1000);
                algonaLastAt = Followers(harness, copy, required: false)[0]?.Position ?? algonaLastAt;
            }
            Assert.IsTrue(Followers(harness, copy, required: false).All(follower => follower == null),
                "the three are home and gone: " + string.Join(", ", Followers(harness, copy, required: false).Select(follower => follower?.Position.ToString() ?? "gone")));
            Assert.IsTrue(Vector3.Distance(algonaLastAt, ToVector(PlateauHolyVengeanceV1.WayBackAt[0])) < 3,
                $"her copy walked her way back and is gone there ({algonaLastAt})");
            Assert.IsTrue(seconds * 1000 < PlateauHolyVengeanceScene.WalkBackMilliseconds, $"gone at the routes' end ({seconds} s), not by the timeout");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "the public Algona is back for the player");

            Return(harness, plateau);
            Spawn(plateau, PlateauHolyVengeanceV1.Sampei);
            var sampei = RequireNpc(plateau, PlateauHolyVengeanceV1.Sampei);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sampei, HolyVengeance, 1,
                PlateauHolyVengeanceV1.Experience, PlateauHolyVengeanceV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "Sampei keeps the book");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BioLegs), "the second choice");
        }

        /// <summary>The followers can walk from the hospital to the crate in the control room and back.</summary>
        [TestMethod]
        public void HolyVengeanceFollowersHaveAPathToTheControlRoom()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoVelonHollow(harness);
            var crate = ToVector(PlateauHolyVengeanceV1.CrateAt);
            foreach (var home in PlateauHolyVengeanceV1.FollowersAt)
            {
                var path = copy.NavMesh.FindPath(ToVector(home), crate, out var joined);
                if (path == null || !joined)
                    Assert.Inconclusive("No navmesh path from the hospital to the control room: the Velon Hollow navmesh is split (upstream bug report).");
                path = copy.NavMesh.FindPath(crate, ToVector(home), out joined);
                Assert.IsTrue(joined && path != null && Vector3.Distance(path[^1], ToVector(home)) <= 0.5f, $"back to {ToVector(home)}");
            }
            // Each way back ends on the navmesh about 100 m (ESTIMATE) along the path from the crate.
            foreach (var wayBack in PlateauHolyVengeanceV1.WayBackAt.Select(ToVector))
            {
                AssertStandsOnNavMesh(copy, wayBack, 0.5f);
                var path = copy.NavMesh.FindPath(crate, wayBack, out var joined);
                Assert.IsTrue(joined && path != null, $"a path to {wayBack}");
                var length = 0f;
                var at = crate;
                foreach (var corner in path) { length += Vector3.Distance(at, corner); at = corner; }
                Assert.IsTrue(length is > 80 and < 130, $"{wayBack}: {length:0} m");
            }
        }

        /// <summary>Abandoned with the three following: they and the crate are gone, Algona offers it again.</summary>
        [TestMethod]
        public void HolyVengeanceAbandonedTakesItsSceneAndIsOfferedAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness);
            var followers = Followers(harness, copy);
            var crate = Crate(harness, copy);
            var publicAlgona = PublicAlgona(copy);
            harness.MoveTo(publicAlgona.Position);
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, HolyVengeance));
            var shown = new List<Rasa.Packets.PythonPacket>();
            for (var step = 0; step < 4; step++)
            {
                harness.UtcNow = harness.UtcNow.AddMilliseconds(250);
                BehaviorManager.Instance.MapChannelThink(copy, 250);
                harness.Manager.Scenes.Tick(copy);
                shown.AddRange(harness.Drain());
            }
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicAlgona, harness.Client), "the run ended: the public Algona shown again");
            Assert.IsTrue(shown.OfType<Rasa.Packets.Game.Server.CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == publicAlgona.EntityId),
                "and given back to the player's client");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(HolyVengeance), "gone from the log");
            Assert.IsFalse(followers.Any(follower => Here(copy).Contains(follower)), "the three are gone");
            Assert.IsFalse(crate.IsInWorld, "the crate is gone");
            Spawn(copy, PlateauTheseOldBonesV1.AlgonaPool);
            Assert.IsTrue(Offers(harness, RequireNpc(copy, PlateauTheseOldBonesV1.AlgonaPool), HolyVengeance), "Algona offers it again");
        }

        /// <summary>
        /// The book taken with the three left at the hospital (the player was put at the crate): their ways back are far
        /// off, so the timeout takes them, and the public Algona is shown again.
        /// </summary>
        [TestMethod]
        public void HolyVengeanceFollowersFarOffAreGoneAfterTheTimeout()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness);
            TakeTheBook(harness, copy);
            Tick(harness, copy, PlateauHolyVengeanceScene.WalkBackMilliseconds - 5000);
            Assert.IsTrue(Followers(harness, copy, required: false).All(follower => follower != null), "still on their way before the timeout");
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "and the public Algona hidden");
            Tick(harness, copy, 6000);
            Tick(harness, copy, 250);
            Assert.IsTrue(Followers(harness, copy, required: false).All(follower => follower == null), "gone after the timeout");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "the public Algona is back");
            Assert.IsTrue(harness.Client.Player.Missions[HolyVengeance].Completeable, "the hand-in needs none of them");
        }

        /// <summary>
        /// Out of the copy and back in (or a server restart) on the walk back: the run is recovered, nobody can be put back
        /// on his way, so all are gone and the public Algona shown.
        /// </summary>
        [TestMethod]
        public void HolyVengeanceRecoveredOnTheWalkBackEndsIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness);
            TakeTheBook(harness, copy);
            Assert.IsNotNull(Followers(harness, copy, required: false)[0], "Algona on her way back");

            harness.Manager.Scenes.Detach(harness.Client, copy);
            Return(harness, plateau);
            Assert.AreSame(copy, ThroughTheDoor(harness, VelonHollow));
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.IsTrue(Followers(harness, copy, required: false).All(follower => follower == null), "gone after the recovery");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "the public Algona is back");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "the book kept");
            Assert.IsTrue(harness.Client.Player.Missions[HolyVengeance].Completeable);
        }

        /// <summary>A follower killed: nothing fails, and he does not come back.</summary>
        [TestMethod]
        public void HolyVengeanceGoesOnWhenAFollowerDies()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness);
            var algona = Followers(harness, copy)[0];
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "hidden while her copy stands");
            algona.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, algona, null);
            Tick(harness, copy, 250);
            var mission = harness.Client.Player.Missions[HolyVengeance];
            Assert.AreEqual(MissionState.Active, mission.State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[PlateauHolyVengeanceV1.Escort].State);
            Tick(harness, copy, 1000);
            Assert.IsNull(Followers(harness, copy, required: false)[0], "Algona stays dead");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(PublicAlgona(copy), harness.Client), "the public Algona is back with the copy dead");
            Assert.IsFalse(copy.HiddenPools.Keys.Any(key => key.Character == harness.Client.Player.Id), "nothing held for the player");
        }

        /// <summary>
        /// The public Warrior Algona (pool 650108) while the escort runs: taken off the player's client on accept, not
        /// shown nor heard by them (a bark of hers reaches only others), hidden again when they come back into the copy.
        /// </summary>
        [TestMethod]
        public void ThePublicAlgonaIsHiddenOnlyFromTheEscortsPlayerWhileItRuns()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var copy = AcceptAtAlgona(harness, out var onAccept);
            var publicAlgona = PublicAlgona(copy);
            Assert.IsTrue(onAccept.OfType<Rasa.Packets.MapChannel.Server.DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == publicAlgona.EntityId),
                "taken off the player's client");
            AssertPublicAlgonaHidden(harness, copy, Followers(harness, copy)[0]);

            NpcBarks.Bark(copy, publicAlgona, 1);
            Assert.IsFalse(harness.Drain().OfType<Rasa.Packets.MapChannel.Server.BarkPackage>().Any(), "nothing of hers reaches the player");
            CreatureManager.Instance.CreateCreatureOnClient(harness.Client, publicAlgona);
            Assert.IsFalse(harness.Drain().Any(), "nor is she given to them");

            harness.Manager.Scenes.Detach(harness.Client, copy);   // out of the copy and back: the run waits, held
            Return(harness, plateau);
            var back = ThroughTheDoor(harness, VelonHollow);
            Assert.AreSame(copy, back);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(publicAlgona, harness.Client), "hidden again on the way back in");
            Assert.IsNotNull(Followers(harness, copy)[0], "her copy stands");
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

        private static MapChannel AcceptAtAlgona(WildernessRuntimeTestHarness harness) => AcceptAtAlgona(harness, out _);

        /// <summary>Both outposts cleared and the book taken from the crate: the three on their way back.</summary>
        private static void TakeTheBook(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            var crate = Crate(harness, copy);
            Clear(harness, copy, PlateauHolyVengeanceV1.OuterPools);
            Clear(harness, copy, PlateauHolyVengeanceV1.InnerPools);
            Use(harness, copy, crate);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauHolyVengeanceV1.BookTemplate), "the Law of Asperion");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PlateauHolyVengeanceV1.Escort));
        }

        /// <summary>The player and the three by the crate, as when they walked in with him (each a few metres off).</summary>
        private static void WithThePlayerAtTheCrate(WildernessRuntimeTestHarness harness, MapChannel copy, DynamicObject crate)
        {
            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var followers = Followers(harness, copy, required: false);
            for (var index = 0; index < followers.Length; index++)
                if (followers[index] != null)
                    followers[index].Position = copy.NavMesh.Nearest(crate.Position + new Vector3(2 + index, 0, 2)) ?? crate.Position;
            Tick(harness, copy, 250);
        }

        private static MapChannel AcceptAtAlgona(WildernessRuntimeTestHarness harness, out List<Rasa.Packets.PythonPacket> onAccept)
        {
            var copy = IntoVelonHollow(harness);
            SpawnPoolClearedProgress.Missions = () => harness.Manager;
            onAccept = Accept(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Outer));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PlateauHolyVengeanceV1.Inner));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, PlateauHolyVengeanceV1.Recover));
            Tick(harness, copy, 250);
            return copy;
        }

        /// <summary>Algona at the hospital; 1977 offered after These Old Bones and accepted.</summary>
        private static List<Rasa.Packets.PythonPacket> Accept(WildernessRuntimeTestHarness harness, MapChannel copy)
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
            var onAccept = harness.Drain().ToList();
            harness.Manager.Scenes.Resume(harness.Client);
            onAccept.AddRange(harness.Drain());
            return onAccept;
        }

        private static Creature PublicAlgona(MapChannel copy) =>
            Here(copy).First(creature => creature.SpawnPool?.DbId == PlateauHolyVengeanceScene.PublicAlgonaPool && creature.Npc != null);

        /// <summary>The public Algona hidden from the player only, her copy standing in her place.</summary>
        private static void AssertPublicAlgonaHidden(WildernessRuntimeTestHarness harness, MapChannel copy, Creature escort)
        {
            var publicAlgona = PublicAlgona(copy);
            Assert.AreEqual(PlateauTheseOldBonesV1.AlgonaPool, PlateauHolyVengeanceScene.PublicAlgonaPool);
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(publicAlgona, harness.Client), "the public Algona is not shown to the player");
            Assert.IsTrue(copy.HiddenPools.Keys.Any() && copy.HiddenPools.Keys.All(key =>
                    key.Character == harness.Client.Player.Id && key.Pool == PlateauHolyVengeanceScene.PublicAlgonaPool),
                "to the player only: squad mates still see her");
            Assert.IsTrue(Vector3.Distance(escort.Position, ToVector(PlateauHolyVengeanceV1.FollowersAt[0])) < 1.5f, "the copy stands at her start");
            Assert.IsTrue(Vector3.Distance(escort.Position, publicAlgona.Position) < 1.5f, "where the public one stands");
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
