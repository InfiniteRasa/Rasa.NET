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
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// The supply chain, played through as a player plays it: The Supply Master (1322: Nakrinx's four bodyguards killed,
    /// then his talk), Eastern Listening Post (1323: the lost package in the crater, two striders by it), Smash and Grab
    /// (1324: three stores emptied and three data servers destroyed at the Thrax Barracks) and Spoils of War (1326: back to
    /// Snake).
    ///
    /// Why these tests: Nakrinx walks a long beat with four guards as one NPC squad, and must not be drawn into their
    /// fights; his talk opens only through a server-only kill count; Smash and Grab runs six objectives on one scene and
    /// its ammo also counts for One Man's Trash. A guard off the navmesh, Nakrinx in a fight, the talk open too early, a
    /// store given twice, a server that does not count, an item kept after an abandon, or progress lost at a reload would
    /// each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsSupplyChainTests
    {
        private const uint SupplyMaster = PoolsSupplyMasterV1.MissionId;
        private const uint ListeningPost = PoolsEasternListeningPostV1.MissionId;
        private const uint SmashAndGrab = PoolsSmashAndGrabV1.MissionId;
        private const uint Spoils = PoolsSpoilsOfWarV1.MissionId;
        private const uint Trash = PoolsOneMansTrashV1.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Barracks' east gate, Nakrinx's beat's start.</summary>
        private static readonly Vector3 Gate = new(545.6f, 680f, 464f);

        /// <summary>The crater's east rim, the way down to the crate.</summary>
        private static readonly Vector3 Rim = new(244.4f, 684.6f, -142.8f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void NakrinxLeadsHisFourBodyguardsOnABeatOnTheNavmesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var (nakrinx, guards) = Patrol(harness, pools);
            var squad = nakrinx.SquadMember.Squad;

            Assert.AreSame(nakrinx, squad.Leader.Creature, "Nakrinx leads");
            Assert.IsTrue(S.IsBystander(nakrinx), "a bystander");
            Assert.AreEqual(TargetCategory.Neutral, nakrinx.TargetCategory, "he seeks no one");
            Assert.IsEmpty(nakrinx.Actions, "no attack");
            Assert.IsNotNull(nakrinx.Npc, "he can talk");
            Assert.HasCount((int)PoolsSupplyNpcsV1.Bodyguards, guards);
            foreach (var guard in guards)
            {
                Assert.IsFalse(S.IsBystander(guard), "a guard is none");
                Assert.AreSame(nakrinx, S.TetherOf(guard, out var tether), "a guard is tethered to him");
                Assert.AreEqual(30f, tether);
                Assert.AreEqual(TargetCategory.Hostile, guard.TargetCategory);
                Assert.IsNotEmpty(guard.Actions, "a guard attacks");
            }

            var beat = PoolsSupplyNpcsV1.Out;
            Assert.HasCount(15, beat, "15 corners out, walked back the same way (path mode Back)");
            Assert.AreEqual(S.PathMode.Back, S.Paths[PoolsSupplyNpcsV1.Squad].Mode);
            for (var step = 0; step < beat.Length; step++)
            {
                var at = new Vector3((float)beat[step].X, (float)beat[step].Y, (float)beat[step].Z);
                var ground = pools.NavMesh.GroundHeight(at);
                Assert.IsNotNull(ground, $"navmesh under step {step}");
                Assert.AreEqual(ground.Value, at.Y, 0.5f, $"step {step} on the ground");
                if (step == beat.Length - 1)
                    break;
                var next = beat[step + 1];
                Assert.IsTrue(pools.NavMesh.FindPath(at, new Vector3((float)next.X, (float)next.Y, (float)next.Z), out var complete) != null && complete,
                    $"step {step} to the next is walked");
            }
            Assert.AreEqual(Gate, S.Spawns[PoolsSupplyNpcsV1.Squad].Position, "sent at the Barracks' gate");

            // A minute on the beat: the guards keep up with him, on the ground.
            var start = nakrinx.Position;
            var farthest = 0f;
            for (var tick = 0; tick < 240; tick++)
            {
                PlateauGiantKillerSquadTests.Step(harness, pools, 250);
                farthest = System.Math.Max(farthest, Vector3.Distance(start, nakrinx.Position));
                if (tick < 8)
                    continue;
                foreach (var guard in guards)
                    Assert.IsLessThan(14f, Spacing.AcrossGround(guard.Position, nakrinx.Position),
                        $"tick {tick}: guard at {guard.Position}, Nakrinx at {nakrinx.Position}");
            }
            Assert.IsGreaterThan(20f, farthest, $"Nakrinx walked {farthest:0} m");
        }

        /// <summary>A guard's fight takes the other guards in, not Nakrinx (NpcSquads, its leader a bystander).</summary>
        [TestMethod]
        public void TheGuardsFightWithoutNakrinx()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var (nakrinx, guards) = Patrol(harness, pools);
            harness.MoveTo(guards[0].Position + new Vector3(5, 0, 0));
            harness.Drain();

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(guards[0], harness.Client.Player.EntityId));
            for (var tick = 0; tick < 4; tick++)
                PlateauGiantKillerSquadTests.Step(harness, pools, 250);

            foreach (var guard in guards)
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guard.Controller.CurrentAction, "each guard fights");
            Assert.AreEqual(BehaviorManager.BehaviorActionSquad, nakrinx.Controller.CurrentAction, "Nakrinx does not fight");

            // He holds his place while they fight, so they fight by him (while one of them fights: the fight may end
            // when the player falls, and then the squad goes on): no goal and no route, he does not walk. What moves him
            // at all is upstream's keep-apart push (BehaviorManager.CreatureThink: a creature of his cell within 1 m
            // pushes him 0.3 m, picked at random), when a guard stands on him; a few such pushes stay well under 2 m.
            var held = nakrinx.Position;
            var fought = 0;
            for (var tick = 0; tick < 40 && guards.Any(guard => guard.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting); tick++)
            {
                PlateauGiantKillerSquadTests.Step(harness, pools, 250);
                if (guards.Any(guard => guard.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting))
                {
                    fought++;
                    Assert.IsNull(nakrinx.SquadMember.Goal, $"Nakrinx is sent nowhere (tick {tick})");
                    Assert.IsEmpty(nakrinx.Controller.Path, $"Nakrinx walks no route (tick {tick})");
                    Assert.IsLessThan(2f, Vector3.Distance(held, nakrinx.Position), $"Nakrinx holds (tick {tick})");
                }
            }
            Assert.IsGreaterThan(4, fought, "a fight of some length");
        }

        /// <summary>A shot at Nakrinx does not start a fight: he has nothing to fight with, and his guards are not drawn in for him.</summary>
        [TestMethod]
        public void AShotAtNakrinxStartsNoFight()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var (nakrinx, guards) = Patrol(harness, pools);
            harness.MoveTo(nakrinx.Position + new Vector3(20, 0, 0));
            harness.Drain();

            Threat.FromDamage(nakrinx, harness.Client.Player, 10);
            for (var tick = 0; tick < 4; tick++)
                PlateauGiantKillerSquadTests.Step(harness, pools, 250);

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, nakrinx.Controller.CurrentAction, "Nakrinx");
            Assert.IsTrue(guards.All(guard => guard.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting), "his guards");
        }

        [TestMethod]
        public void TheSupplyMasterTalksOnceHisFourBodyguardsAreDead()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            var (nakrinx, guards) = Patrol(harness, pools);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, riggins, SupplyMaster), "not before Death Patrol");
            SeedCompleted(harness, PoolsDeathPatrolV1.MissionId);

            var offer = WildernessRanjaGorgeTests.Offer(harness, riggins, SupplyMaster);
            WildernessRanjaGorgeTests.AssertSelectable(offer, SupplyMaster,
                (PoolsSupplyMasterV1.StealAdrenaline, 29879), (PoolsSupplyMasterV1.StealArmor, 29879),
                (PoolsSupplyMasterV1.StealHealth, 29879), (PoolsSupplyMasterV1.StealPower, 29879));
            Accept(harness, riggins, SupplyMaster);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.NotAssigned, MissionObjectiveState.Inactive),
                (Objective(harness, SupplyMaster, PoolsSupplyMasterV1.Bodyguards), Objective(harness, SupplyMaster, PoolsSupplyMasterV1.Interrogate),
                    Objective(harness, SupplyMaster, PoolsSupplyMasterV1.Return)));

            for (var index = 0; index < guards.Length; index++)
            {
                Assert.IsEmpty(Topics(harness, nakrinx, SupplyMaster), $"after {index} kills he does not talk");
                var sent = Kill(harness, pools, guards[index]);
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[SupplyMaster].Objectives[PoolsSupplyMasterV1.Bodyguards].Counters[0]);
                Assert.IsFalse(sent.OfType<UpdateObjectiveCounterPacket>().Any(packet => packet.MissionId == SupplyMaster),
                    "no counter the client has no name for (to-fix #6: 'Missing translation for missionobjective ID 1322/3/0')");
                Assert.IsFalse(sent.OfType<ObjectiveCompletedPacket>().Any(packet => packet.MissionId == SupplyMaster &&
                        packet.ObjectiveId == PoolsSupplyMasterV1.Bodyguards),
                    "no banner for an objective the client has no name for (to-fix #7: 'Missing translation for missionobjective ID 1322/3')");
                Assert.AreEqual(index == guards.Length - 1, sent.OfType<ObjectiveActivatedPacket>().Any(packet =>
                    packet.MissionId == SupplyMaster && packet.ObjectiveId == PoolsSupplyMasterV1.Interrogate), "the talk opens at the fourth");
                if (index == 1)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(2U, harness.Client.Player.Missions[SupplyMaster].Objectives[PoolsSupplyMasterV1.Bodyguards].Counters[0], "kept at a reload");
                    harness.Drain();
                }
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, SupplyMaster, PoolsSupplyMasterV1.Bodyguards), Objective(harness, SupplyMaster, PoolsSupplyMasterV1.Interrogate)));
            Assert.IsFalse(harness.Client.Player.Missions[SupplyMaster].Completeable);

            Talk(harness, nakrinx, SupplyMaster, PoolsSupplyMasterV1.Interrogate);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SupplyMaster, PoolsSupplyMasterV1.Return), "the way back shown");
            Assert.IsTrue(harness.Client.Player.Missions[SupplyMaster].Completeable, "the guide is not required");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, riggins, SupplyMaster, 1,
                PoolsSupplyMasterV1.Experience, PoolsSupplyMasterV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == SupplyMaster));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSupplyMasterV1.StealArmor), "the selected module");
        }

        /// <summary>Nakrinx's own death is not one of the four.</summary>
        [TestMethod]
        public void NakrinxsDeathDoesNotCount()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            var (nakrinx, _) = Patrol(harness, pools);
            SeedCompleted(harness, PoolsDeathPatrolV1.MissionId);
            Accept(harness, riggins, SupplyMaster);

            Kill(harness, pools, nakrinx);
            Assert.AreEqual(0U, harness.Client.Player.Missions[SupplyMaster].Objectives[PoolsSupplyMasterV1.Bodyguards].Counters[0]);
        }

        /// <summary>Kills before the accept do not count; an abandon resets the count.</summary>
        [TestMethod]
        public void TheSupplyMastersCountStartsAtTheAcceptAndAnAbandonResetsIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            var (_, guards) = Patrol(harness, pools);
            SeedCompleted(harness, PoolsDeathPatrolV1.MissionId);

            Kill(harness, pools, guards[0]);
            Accept(harness, riggins, SupplyMaster);
            Assert.AreEqual(0U, harness.Client.Player.Missions[SupplyMaster].Objectives[PoolsSupplyMasterV1.Bodyguards].Counters[0]);
            Kill(harness, pools, guards[1]);
            Assert.AreEqual(1U, harness.Client.Player.Missions[SupplyMaster].Objectives[PoolsSupplyMasterV1.Bodyguards].Counters[0]);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, SupplyMaster));
            harness.Drain();
            Accept(harness, riggins, SupplyMaster);
            Assert.AreEqual(0U, harness.Client.Player.Missions[SupplyMaster].Objectives[PoolsSupplyMasterV1.Bodyguards].Counters[0], "from 0");
        }

        [TestMethod]
        public void EasternListeningPostTakesThePackageFromTheCraterToChester()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var snake = Npc(pools, PlateauSnakePitV1.Snake);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, snake, ListeningPost), "not before The Supply Master");
            SeedCompleted(harness, SupplyMaster);

            var offer = WildernessRanjaGorgeTests.Offer(harness, snake, ListeningPost);
            CollectionAssert.AreEquivalent(new[]
            {
                (PoolsEasternListeningPostV1.EmpBomb, 2U), (PoolsEasternListeningPostV1.ArmorCharger, 2U),
                (PoolsEasternListeningPostV1.FireGrenade, 4U), (PoolsEasternListeningPostV1.EmpGrenade, 4U)
            }, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Accept(harness, snake, ListeningPost);
            harness.Manager.Scenes.Tick(pools);
            harness.Drain();

            var crate = pools.DynamicObjects.Single(obj => obj.SceneMissionId == ListeningPost && obj.SceneActorRole == "crate");
            var at = PoolsEasternListeningPostV1.CrateAt;
            Assert.AreEqual(PoolsEasternListeningPostV1.CrateClass, (uint)crate.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), crate.Position);
            Assert.AreEqual(harness.Client.Player.Id, crate.SceneOwnerCharacterId, "the player's own");
            AssertOnTheGround(pools, crate.Position, "the crate");
            AssertWalkedTo(pools, Rim, crate.Position, "the crate");
            Assert.IsLessThan(8f, Vector2.Distance(new(at.X, at.Z), new(240.4f, -141.2f)), "by TaRapedia's point");
            for (var index = 0; index < PoolsEasternListeningPostV1.StridersAt.Length; index++)
            {
                var strider = SceneCreature(pools, PoolsEasternListeningPostV1.StriderRole(index));
                Assert.IsNotNull(strider, $"strider {index + 1}");
                Assert.AreEqual(PoolsEasternListeningPostV1.Strider, strider.DbId);
                AssertOnTheGround(pools, strider.Position, $"strider {index + 1}");
            }

            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            Use(harness, pools, crate, recovers: true);
            Assert.AreEqual(1U, Package(harness));
            Assert.IsFalse(crate.IsEnabled, "opened once");
            Use(harness, pools, crate, recovers: false);
            Assert.AreEqual(1U, Package(harness), "one package");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, Package(harness), "kept at a reload");
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[ListeningPost].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, chester, ListeningPost, null,
                PoolsEasternListeningPostV1.Experience, PoolsEasternListeningPostV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ListeningPost));
            Assert.AreEqual(0U, Package(harness), "Chester takes it");
            Assert.AreEqual(4U, WildernessRanjaGorgeTests.Held(harness, PoolsEasternListeningPostV1.EmpGrenade));
        }

        [TestMethod]
        public void EasternListeningPostAbandonedTakesThePackageCrateAndStriders()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var snake = Npc(pools, PlateauSnakePitV1.Snake);
            SeedCompleted(harness, SupplyMaster);
            Accept(harness, snake, ListeningPost);
            harness.Manager.Scenes.Tick(pools);
            var crate = pools.DynamicObjects.Single(obj => obj.SceneMissionId == ListeningPost && obj.SceneActorRole == "crate");
            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            Use(harness, pools, crate, recovers: true);
            Assert.AreEqual(1U, Package(harness));
            var striders = Enumerable.Range(0, 2).Select(index => SceneCreature(pools, PoolsEasternListeningPostV1.StriderRole(index))).ToArray();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, ListeningPost));
            harness.Manager.Scenes.Tick(pools);
            harness.Drain();
            Assert.AreEqual(0U, Package(harness), "the package goes");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == ListeningPost && obj.IsInWorld), "the crate goes");
            Assert.IsFalse(striders.Any(strider => Living(pools).Contains(strider)), "the striders go");
            Accept(harness, snake, ListeningPost);
            harness.Manager.Scenes.Tick(pools);
            Assert.IsTrue(pools.DynamicObjects.Single(obj => obj.SceneMissionId == ListeningPost && obj.IsInWorld).IsEnabled, "a new crate");
        }

        /// <summary>The three stores in any order, the three servers destroyed; each counts once, the items go to Chester.</summary>
        [TestMethod]
        public void SmashAndGrabEmptiesThreeStoresAndDestroysThreeServers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, chester, SmashAndGrab), "not before Eastern Listening Post");
            SeedCompleted(harness, ListeningPost);

            var offer = WildernessRanjaGorgeTests.Offer(harness, chester, SmashAndGrab);
            WildernessRanjaGorgeTests.AssertSelectable(offer, SmashAndGrab,
                (PoolsSmashAndGrabV1.LaserChaingun, 27054), (PoolsSmashAndGrabV1.IceNetGun, 27089),
                (PoolsSmashAndGrabV1.ElectricPolarityGun, 27143), (PoolsSmashAndGrabV1.LaserRifle, 27216));
            Accept(harness, chester, SmashAndGrab);
            harness.Manager.Scenes.Tick(pools);
            harness.Drain();

            var stores = Stores(pools);
            var servers = Servers(pools);
            for (var index = 0; index < stores.Length; index++)
            {
                var (_, _, _, storeClass, at) = PoolsSmashAndGrabV1.Stores[index];
                Assert.AreEqual(storeClass, (uint)stores[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), stores[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, stores[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, stores[index].Position, stores[index].SceneActorRole);
                AssertWalkedTo(pools, Gate, stores[index].Position, stores[index].SceneActorRole);
                var server = PoolsSmashAndGrabV1.Servers[index].At;
                Assert.AreEqual(PoolsSmashAndGrabV1.ServerClass, (uint)servers[index].EntityClassId);
                Assert.AreEqual(new Vector3(server.X, server.Y, server.Z), servers[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, servers[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, servers[index].Position, servers[index].SceneActorRole);
                AssertWalkedTo(pools, Gate, servers[index].Position, servers[index].SceneActorRole);
                Assert.IsLessThan(10f, Vector3.Distance(stores[index].Position, servers[index].Position), "a server by its store");
            }

            // The paste, the ammo, the capacitors.
            var done = 0;
            foreach (var index in new[] { 2, 0, 1 })
            {
                var (objective, template, _, _, _) = PoolsSmashAndGrabV1.Stores[index];
                harness.MoveTo(stores[index].Position + new Vector3(0, 0, 1));
                Use(harness, pools, stores[index], recovers: true);
                done++;
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), $"item {template}");
                Assert.IsFalse(stores[index].IsEnabled, "emptied once");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, objective));
                Use(harness, pools, stores[index], recovers: false);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), "one of each");
                if (done == 1)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, objective), "kept at a reload");
                    harness.Drain();
                }
            }
            Assert.IsFalse(harness.Client.Player.Missions[SmashAndGrab].Completeable, "the servers too");

            for (var index = 0; index < servers.Length; index++)
            {
                var objective = PoolsSmashAndGrabV1.Servers[index].Objective;
                Damage(harness, pools, servers[index], 99);
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SmashAndGrab, objective), "99 is not enough");
                Damage(harness, pools, servers[index], 1);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, objective), $"server {index + 1}");
                Assert.AreEqual(PoolsSmashAndGrabV1.DestroyedState, (uint)servers[index].StateId, "destroyed");
                foreach (var other in PoolsSmashAndGrabV1.Servers.Skip(index + 1))
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SmashAndGrab, other.Objective), "its own count only");
            }
            Assert.IsTrue(harness.Client.Player.Missions[SmashAndGrab].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, chester, SmashAndGrab, 3,
                PoolsSmashAndGrabV1.Experience, PoolsSmashAndGrabV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == SmashAndGrab));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.LaserRifle), "the selected rifle");
            foreach (var store in PoolsSmashAndGrabV1.Stores)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, store.Template), "Chester keeps them");
        }

        /// <summary>
        /// Objective 7's link: with One Man's Trash not taken, the Barracks' ammo counts for Smash and Grab only, and 562 is
        /// not started.
        /// </summary>
        [TestMethod]
        public void SmashAndGrabsAmmoWithoutOneMansTrashCountsOnlyForItself()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            SeedCompleted(harness, ListeningPost);
            Accept(harness, chester, SmashAndGrab);
            harness.Manager.Scenes.Tick(pools);
            var stores = Stores(pools);
            harness.MoveTo(stores[0].Position + new Vector3(0, 0, 1));
            Use(harness, pools, stores[0], recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, PoolsSmashAndGrabV1.Ammo));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.Stores[0].Template));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Trash), "562 is not started");
        }

        /// <summary>
        /// Objective 7's link: with One Man's Trash active, the Barracks' ammo completes its ammo too. 562's own ammo crate
        /// then gives nothing; 562 is turned in at Buckley and Smash and Grab keeps its ammo.
        /// </summary>
        [TestMethod]
        public void SmashAndGrabsAmmoCountsForOneMansTrash()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            SeedCompleted(harness, PoolsCultDividedV1.MissionId, ListeningPost);

            Accept(harness, chester, SmashAndGrab);
            harness.Manager.Scenes.Tick(pools);
            var stores = Stores(pools);
            harness.MoveTo(stores[1].Position + new Vector3(0, 0, 1));
            Use(harness, pools, stores[1], recovers: true);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Trash), "the capacitors are not ammo");

            Accept(harness, buckley, Trash);
            harness.Manager.Scenes.Tick(pools);
            harness.MoveTo(stores[0].Position + new Vector3(0, 0, 1));
            Use(harness, pools, stores[0], recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, PoolsSmashAndGrabV1.Ammo));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Trash, PoolsOneMansTrashV1.Ammo), "562's ammo too");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trash, PoolsOneMansTrashV1.Rations), "not the rations");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.Stores[0].Template), "one ammo crate");

            var trashCrate = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Trash && obj.IsInWorld &&
                obj.SceneActorRole == PoolsOneMansTrashV1.CrateRole(0));
            harness.MoveTo(trashCrate.Position + new Vector3(0, 0, 1));
            Use(harness, pools, trashCrate, recovers: false);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.Stores[0].Template), "562's crate gives nothing more");

            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, new[] { MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 2) }));
            harness.Drain();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, buckley, Trash, 0,
                PoolsOneMansTrashV1.Experience, PoolsOneMansTrashV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.Stores[0].Template), "Smash and Grab keeps its ammo");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, PoolsSmashAndGrabV1.Ammo));
        }

        [TestMethod]
        public void SmashAndGrabAbandonedTakesItsItemsStoresAndServers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            SeedCompleted(harness, ListeningPost);
            Accept(harness, chester, SmashAndGrab);
            harness.Manager.Scenes.Tick(pools);
            var stores = Stores(pools);
            harness.MoveTo(stores[2].Position + new Vector3(0, 0, 1));
            Use(harness, pools, stores[2], recovers: true);
            Damage(harness, pools, Servers(pools)[0], 100);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.Stores[2].Template));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, SmashAndGrab));
            harness.Manager.Scenes.Tick(pools);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.Stores[2].Template), "the paste goes");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == SmashAndGrab && obj.IsInWorld), "the stores and servers go");
            Accept(harness, chester, SmashAndGrab);
            harness.Manager.Scenes.Tick(pools);
            Assert.IsTrue(Stores(pools).All(store => store.IsEnabled), "three new stores");
            Assert.IsTrue(Servers(pools).All(server => (uint)server.StateId == PoolsSmashAndGrabV1.IntactState), "three new servers");
        }

        [TestMethod]
        public void SpoilsOfWarEndsAtSnake()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            var snake = Npc(pools, PlateauSnakePitV1.Snake);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, chester, Spoils), "not before Smash and Grab");
            SeedCompleted(harness, SmashAndGrab);

            var offer = WildernessRanjaGorgeTests.Offer(harness, chester, Spoils);
            CollectionAssert.AreEquivalent(new[]
            {
                (PoolsSpoilsOfWarV1.MedPack, 2U), (PoolsSpoilsOfWarV1.PowerCharger, 2U), (PoolsSpoilsOfWarV1.IceGrenade, 4U),
                (PoolsSpoilsOfWarV1.FireGrenade, 4U)
            }, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Accept(harness, chester, Spoils);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, chester, Spoils), "accepted once");

            Talk(harness, snake, Spoils, PoolsSpoilsOfWarV1.Report);
            Assert.IsTrue(harness.Client.Player.Missions[Spoils].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, snake, Spoils, null,
                PoolsSpoilsOfWarV1.Experience, PoolsSpoilsOfWarV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Spoils));
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, PoolsSpoilsOfWarV1.PowerCharger));
        }

        /// <summary>
        /// A full bag: the crate and a store wait at their use; a turn-in whose save fails, or with a full bag, pays nothing
        /// until it can.
        /// </summary>
        [TestMethod]
        public void FullBagsAndFailedSavesWait()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var snake = Npc(pools, PlateauSnakePitV1.Snake);
            var chester = Npc(pools, PoolsSupplyNpcsV1.Chester);
            SeedCompleted(harness, SupplyMaster);
            Accept(harness, snake, ListeningPost);
            harness.Manager.Scenes.Tick(pools);

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            var crate = pools.DynamicObjects.Single(obj => obj.SceneMissionId == ListeningPost && obj.SceneActorRole == "crate");
            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            Use(harness, pools, crate, recovers: true);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, ListeningPost, PoolsEasternListeningPostV1.Collect));
            Assert.IsTrue(crate.IsEnabled, "the crate waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, pools, crate, recovers: true);
            Assert.AreEqual(1U, Package(harness), "with room it gives");

            FailedTurnInPaysNothing(harness, chester, ListeningPost, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, chester, ListeningPost, null,
                PoolsEasternListeningPostV1.Experience, PoolsEasternListeningPostV1.Credits);

            // The package's slot is free again: the first store takes it, the second waits.
            Accept(harness, chester, SmashAndGrab);
            harness.Manager.Scenes.Tick(pools);
            var stores = Stores(pools);
            harness.MoveTo(stores[0].Position + new Vector3(0, 0, 1));
            Use(harness, pools, stores[0], recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, PoolsSmashAndGrabV1.Ammo));
            harness.MoveTo(stores[1].Position + new Vector3(0, 0, 1));
            Use(harness, pools, stores[1], recovers: true);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SmashAndGrab, PoolsSmashAndGrabV1.Capacitors));
            Assert.IsTrue(stores[1].IsEnabled, "the store waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, pools, stores[1], recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SmashAndGrab, PoolsSmashAndGrabV1.Capacitors), "with room it gives");

            // The rest by its events; then the turn-in.
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, new[] { 3U, 4U, 5U, 6U }
                .Select(sequence => MissionProgressEvent.Scenario(SmashAndGrab, PoolsSmashAndGrabV1.StoreScenario, sequence)).ToArray()));
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[SmashAndGrab].Completeable);
            FailedTurnInPaysNothing(harness, chester, SmashAndGrab, 0);
            FullBagWaits(harness, chester, SmashAndGrab, 0, PoolsSmashAndGrabV1.LaserChaingun);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, chester, SmashAndGrab, 0,
                PoolsSmashAndGrabV1.Experience, PoolsSmashAndGrabV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == SmashAndGrab));
            Assert.AreEqual(50U, WildernessRanjaGorgeTests.Held(harness, PoolsSmashAndGrabV1.LaserChaingun), "49 fillers and the selected chaingun");
        }

        /// <summary>Nakrinx and his four guards, sent as their squad (the squads' worker on the Pools).</summary>
        private static (Creature Nakrinx, Creature[] Guards) Patrol(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            S.Init(harness);
            Assert.IsTrue(S.Spawns.ContainsKey(PoolsSupplyNpcsV1.Squad), "the squad is in the world database");
            S.Worker(pools);
            harness.Drain();
            var pack = S.On(pools)[PoolsSupplyNpcsV1.Squad].Members.Select(member => member.Creature).ToArray();
            var nakrinx = pack.Single(creature => creature.DbId == PoolsSupplyNpcsV1.Nakrinx);
            return (nakrinx, pack.Where(creature => creature.DbId == PoolsSupplyNpcsV1.Bodyguard).ToArray());
        }

        private static DynamicObject[] Stores(MapChannel pools) =>
            Enumerable.Range(0, PoolsSmashAndGrabV1.Stores.Length).Select(index => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == SmashAndGrab && obj.IsInWorld && obj.SceneActorRole == PoolsSmashAndGrabV1.StoreRole(index))).ToArray();

        private static DynamicObject[] Servers(MapChannel pools) =>
            Enumerable.Range(0, PoolsSmashAndGrabV1.Servers.Length).Select(index => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == SmashAndGrab && obj.IsInWorld && obj.SceneActorRole == PoolsSmashAndGrabV1.ServerRole(index))).ToArray();

        private static Creature SceneCreature(MapChannel pools, string role) => Living(pools)
            .SingleOrDefault(creature => creature.SpawnPool?.SceneRunId != null && creature.SpawnPool.SceneActorRole == role);

        private static uint Package(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, PoolsEasternListeningPostV1.PackageTemplate);

        /// <summary>A turn-in whose save fails pays nothing; the mission stays to be turned in (as PoolsRetreadChainTests).</summary>
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

        /// <summary>A full bag and inbox: the turn-in pays nothing; one slot freed, the caller turns it in.</summary>
        private static void FullBagWaits(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice, uint filler)
        {
            WildernessAliaBranchesTests.FillCategory(harness, filler, 50);
            harness.FillInbox();
            harness.MoveTo(receiver.Position);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice), "no room");
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            WildernessAliaBranchesTests.FreeOneSlot(harness, filler);
            harness.Drain();
        }

        /// <summary>The objectives of <paramref name="mission"/> that <paramref name="npc"/> offers to hear now.</summary>
        private static uint[] Topics(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value)
                ? ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => (uint)topic.ObjectiveId).ToArray()
                : System.Array.Empty<uint>();

        private static void AssertOnTheGround(MapChannel pools, Vector3 position, string what)
        {
            var ground = pools.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        private static void AssertWalkedTo(MapChannel pools, Vector3 from, Vector3 position, string what) =>
            Assert.IsTrue(pools.NavMesh.FindPath(from, position, out var complete) != null && complete, $"{what} is walked to");

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        /// <summary>The NPC hears the objective, and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            CollectionAssert.AreEqual(new[] { objective }, Topics(harness, npc, mission), $"{mission}.{objective} topic");
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            var npc = Living(pools).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"Pool {pool} must stand a real public NPC in the Pools.");
            return npc;
        }

        /// <summary>A use of the object: the request, then its recovery if one is due (as PoolsRetreadChainTests).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, bool recovers)
        {
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PoolsSmashAndGrabV1.UseArgument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), map.PerformRecovery.Count, $"use of {obj.SceneActorRole}");
            if (!recovers)
                return;
            var recovery = map.PerformRecovery.Last();
            map.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(map, recovery);
        }

        /// <summary>A weapon hit on the piece (as PoolsBaneChainTests).</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel pools, DynamicObject piece, int damage)
        {
            harness.MoveTo(piece.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = piece.EntityId };
            MissileManager.Instance.MissileLaunch(pools, action, damage);
            MissileManager.Instance.DoWork(pools, 1000);
        }

        /// <summary>The creature dies to the player; the scenes see it (as PoolsBaneChainTests).</summary>
        /// <summary>The creature killed by the player; what the client was sent.</summary>
        private static IReadOnlyList<object> Kill(WildernessRuntimeTestHarness harness, MapChannel pools, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(pools, creature, harness.Client.Player);
            harness.Manager.Scenes.Tick(pools);
            return harness.Drain().Cast<object>().ToList();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;
    }
}
