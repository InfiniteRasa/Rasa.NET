extern alias RasaGame;

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

    /// <summary>
    /// Supply Chief Riggins's Death Patrol line, played through as a player plays it: Death Patrol (1315: dog tags and clips
    /// from 22 fallen soldiers), its repeat More Clips and Tags (1321) and The Thirsty Hermit (1328: Hester's "medicine" for
    /// the Hermit on Fractured Butte).
    ///
    /// Why these tests: the bodies are the player's own scene objects, each searched once, any 16 of them, with the
    /// tags on random bodies but all eight by the 16th; 1321 must come back after each
    /// turn-in; 1328 needs both of its requirements and runs in the order 2, 3, 1. A body searched twice, tags kept after the
    /// turn-in, 1321 offered before Death Patrol or only once, 1328 offered with one requirement, the medicine kept after the
    /// Hermit's talk, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsDeathPatrolTests
    {
        private const uint DeathPatrol = PoolsDeathPatrolV1.MissionId;
        private const uint MoreClips = PoolsMoreClipsAndTagsV1.MissionId;
        private const uint ThirstyHermit = PoolsThirstyHermitV1.MissionId;
        private const uint Tags = PoolsDeathPatrolV1.Tags, Clips = PoolsDeathPatrolV1.Clips;

        [TestMethod]
        public void DeathPatrolSearchesTheFallenForTagsAndClips()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, riggins, MoreClips), "More Clips and Tags waits for Death Patrol");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == DeathPatrol), "no body before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, riggins, DeathPatrol);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsDeathPatrolV1.PhotonicBlade, PoolsDeathPatrolV1.AreaHealingDisc, PoolsDeathPatrolV1.SonicPistol,
                    PoolsDeathPatrolV1.PulseStaff
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(riggins.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, DeathPatrol));
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count(packet => packet.MissionId == DeathPatrol));

            // Any 16 of the 22: the first six are left. Each body gives a clip, some a tag, the eighth tag by the 16th.
            var bodies = Bodies(harness, pools, DeathPatrol, PoolsDeathPatrolV1.Bodies);
            const int skipped = 6;
            var tags = 0U;
            for (var index = skipped; index < skipped + PoolsDeathPatrolV1.ClipCount; index++)
            {
                var searched = (uint)(index - skipped + 1);
                Search(harness, pools, bodies[index]);
                var held = WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.TagTemplate);
                Assert.IsTrue(held == tags || held == tags + 1, $"at most one tag a body ({searched})");
                tags = held;
                Assert.IsTrue(tags + (PoolsDeathPatrolV1.ClipCount - searched) >= PoolsDeathPatrolV1.TagCount,
                    $"the tags still in reach after {searched}");
                Assert.AreEqual(searched, WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate), "a clip a body");
                Assert.IsFalse(bodies[index].IsEnabled, $"body {index + 1} searched once");
                if (searched == 1)
                {
                    Use(harness, pools, bodies[index], PoolsDeathPatrolV1.UseArgument, recovers: false);
                    Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate), "nothing more from a searched body");
                }
                Assert.AreEqual(tags >= PoolsDeathPatrolV1.TagCount ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                    Objective(harness, DeathPatrol, Tags), $"tags after {searched}");
                if (searched == 8)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((tags, 8U), (WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.TagTemplate),
                        WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate)), "kept at a reload");
                    harness.Drain();
                }
                if (searched < PoolsDeathPatrolV1.ClipCount)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, DeathPatrol, Clips));
            }
            Assert.AreEqual(PoolsDeathPatrolV1.TagCount, tags, "eight tags by the 16th body");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, DeathPatrol, Clips));
            Assert.IsTrue(harness.Client.Player.Missions[DeathPatrol].Completeable);

            // The search is over: a body left gives nothing.
            harness.MoveTo(bodies[0].Position + new Vector3(0, 0, 1));
            Use(harness, pools, bodies[0], PoolsDeathPatrolV1.UseArgument, recovers: false);
            Assert.AreEqual((8U, 16U), (WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.TagTemplate),
                WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate)), "nothing after the 16th body");
            harness.Drain();

            // A turn-in whose save fails pays nothing and keeps the tags and clips.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Death Patrol turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, riggins.EntityId, DeathPatrol, 2));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(16U, WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, riggins, DeathPatrol, 2,
                PoolsDeathPatrolV1.Experience, PoolsDeathPatrolV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == DeathPatrol));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.SonicPistol), "the selected pistol");
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.TagTemplate),
                WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate)), "Riggins takes the tags and clips");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, riggins, MoreClips), "More Clips and Tags follows");
        }

        /// <summary>More Clips and Tags: 24 bodies of its own, the reward, and offered again at once.</summary>
        [TestMethod]
        public void MoreClipsAndTagsRepeatsAfterEachTurnIn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            SeedPoolsCompleted(harness, DeathPatrol);

            for (var run = 1; run <= 2; run++)
            {
                var offer = WildernessRanjaGorgeTests.Offer(harness, riggins, MoreClips);
                Assert.AreEqual(0, offer.SelectableReward.Count, $"run {run}: no item");
                harness.MoveTo(riggins.Position);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, MoreClips), $"run {run}");
                harness.Drain();
                var bodies = Bodies(harness, pools, MoreClips, PoolsMoreClipsAndTagsV1.Bodies);
                // Its own two bodies count: the last 16 of the 24.
                for (var index = bodies.Length - (int)PoolsMoreClipsAndTagsV1.ClipCount; index < bodies.Length; index++)
                    Search(harness, pools, bodies[index]);
                Assert.AreEqual((PoolsMoreClipsAndTagsV1.TagCount, PoolsMoreClipsAndTagsV1.ClipCount),
                    (WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.TagTemplate),
                     WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate)), $"run {run}");
                Assert.IsTrue(harness.Client.Player.Missions[MoreClips].Completeable, $"run {run}");
                WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, riggins, MoreClips, null,
                    PoolsMoreClipsAndTagsV1.Experience, PoolsMoreClipsAndTagsV1.Credits);
                harness.Drain();
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate), $"run {run}: clips taken");
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, riggins, MoreClips), $"offered again after run {run}");
            }
        }

        /// <summary>An abandon takes the tags, clips and bodies; taken again, the bodies are all new.</summary>
        [TestMethod]
        public void DeathPatrolAbandonedTakesTheTagsBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            harness.MoveTo(riggins.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, DeathPatrol));
            harness.Drain();
            Search(harness, pools, Bodies(harness, pools, DeathPatrol, PoolsDeathPatrolV1.Bodies)[0]);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, DeathPatrol));
            harness.Drain();
            Assert.AreEqual((0U, 0U), (WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.TagTemplate),
                WildernessRanjaGorgeTests.Held(harness, PoolsDeathPatrolV1.ClipTemplate)), "they go with the mission");
            harness.MoveTo(riggins.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, DeathPatrol));
            harness.Drain();
            Assert.IsTrue(Bodies(harness, pools, DeathPatrol, PoolsDeathPatrolV1.Bodies).All(body => body.IsEnabled), "22 new bodies");
        }

        [TestMethod]
        public void TheThirstyHermitTakesHestersMedicineToTheHermit()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, PoolsSnakePitNpcsV1.Riggins);
            var hester = Npc(pools, PoolsSnakePitNpcsV1.Hester);
            var hermit = Npc(pools, PlateauBaileyNpcsV1.HermitPool);

            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, riggins, ThirstyHermit), "it waits for both");
            SeedPoolsCompleted(harness, DeathPatrol);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, riggins, ThirstyHermit), "Death Patrol alone is not enough");
            SeedPoolsCompleted(harness, PoolsMedicinalMeasuresV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, riggins, ThirstyHermit);
            CollectionAssert.AreEquivalent(HermitRewards, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(riggins.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, ThirstyHermit));
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, ThirstyHermit, 2), Objective(harness, ThirstyHermit, 3), Objective(harness, ThirstyHermit, 1)));

            // The cabinet gives nothing before Hester; the Hermit does not hear it yet.
            var cabinet = pools.DynamicObjects.Single(obj => obj.SceneMissionId == ThirstyHermit && obj.SceneActorRole == "cabinet");
            Assert.AreEqual(PoolsThirstyHermitV1.CabinetClass, (uint)cabinet.EntityClassId);
            Assert.AreEqual(new Vector3(PoolsThirstyHermitV1.CabinetAt.X, PoolsThirstyHermitV1.CabinetAt.Y, PoolsThirstyHermitV1.CabinetAt.Z), cabinet.Position);
            var ground = pools.NavMesh.GroundHeight(cabinet.Position);
            Assert.IsNotNull(ground);
            Assert.AreEqual(ground.Value, cabinet.Position.Y, 0.5f, "the cabinet on the ground");
            harness.MoveTo(cabinet.Position + new Vector3(0, 0, 1));
            Use(harness, pools, cabinet, PoolsThirstyHermitV1.UseArgument, recovers: false);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsThirstyHermitV1.MedicineTemplate));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, hermit.EntityId, ThirstyHermit, 1, 1), "not yet the Hermit's");

            Talk(harness, hester, ThirstyHermit, 2);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, ThirstyHermit, 3));
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, ThirstyHermit, 3), "kept at a reload");
            harness.Drain();

            harness.MoveTo(cabinet.Position + new Vector3(0, 0, 1));
            Use(harness, pools, cabinet, PoolsThirstyHermitV1.UseArgument, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsThirstyHermitV1.MedicineTemplate), "the medicine");
            Assert.IsFalse(cabinet.IsEnabled, "the cabinet is opened once");
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, ThirstyHermit, 3), Objective(harness, ThirstyHermit, 1)));

            Talk(harness, hermit, ThirstyHermit, 1);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsThirstyHermitV1.MedicineTemplate), "the Hermit takes it");
            Assert.IsTrue(harness.Client.Player.Missions[ThirstyHermit].Completeable);

            var held = HermitRewards.Select(reward => WildernessRanjaGorgeTests.Held(harness, reward.Template)).ToArray();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hermit, ThirstyHermit, null,
                PoolsThirstyHermitV1.Experience, PoolsThirstyHermitV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ThirstyHermit));
            for (var i = 0; i < HermitRewards.Length; i++)
                Assert.AreEqual(held[i] + HermitRewards[i].Quantity, WildernessRanjaGorgeTests.Held(harness, HermitRewards[i].Template));
        }

        private static readonly (uint Template, uint Quantity)[] HermitRewards =
        {
            (PoolsThirstyHermitV1.MedPack, PoolsThirstyHermitV1.MedPacks),
            (PoolsThirstyHermitV1.AdrenalineBooster, PoolsThirstyHermitV1.AdrenalineBoosters),
            (PoolsThirstyHermitV1.ConcussionGrenade, PoolsThirstyHermitV1.ConcussionGrenades),
            (PoolsThirstyHermitV1.FragGrenade, PoolsThirstyHermitV1.FragGrenades)
        };

        /// <summary>The mission's bodies: the player's own, at their places, each on the navmesh, of the corpse class.</summary>
        private static DynamicObject[] Bodies(WildernessRuntimeTestHarness harness, MapChannel pools, uint mission,
            (string Name, Rasa.Missions.Scenes.ScenePosition At)[] places)
        {
            var bodies = Enumerable.Range(1, places.Length).Select(id => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == mission && obj.SceneActorRole == $"body-{id}" && obj.IsEnabled)).ToArray();
            for (var index = 0; index < bodies.Length; index++)
            {
                var at = places[index].At;
                Assert.AreEqual(PoolsDeathPatrolV1.KitClass, (uint)bodies[index].EntityClassId);
                var corpse = pools.DynamicObjects.Single(obj => obj.SceneMissionId == mission && obj.SceneActorRole == $"corpse-{index + 1}");
                Assert.AreEqual(PoolsDeathPatrolV1.BodyClasses[index % PoolsDeathPatrolV1.BodyClasses.Length], (uint)corpse.EntityClassId);
                Assert.AreEqual(bodies[index].Position, corpse.Position, "the corpse under its kit");
                Assert.AreEqual(harness.Client.Player.Id, corpse.SceneOwnerCharacterId, "the corpse the player's own too");
                Assert.IsFalse(corpse.IsEnabled, "the corpse takes no target: the click goes to the kit");
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), bodies[index].Position, places[index].Name);
                Assert.AreEqual(harness.Client.Player.Id, bodies[index].SceneOwnerCharacterId, "the player's own");
                var ground = pools.NavMesh.GroundHeight(bodies[index].Position);
                Assert.IsNotNull(ground, $"{places[index].Name} has navmesh under it");
                Assert.AreEqual(ground.Value, at.Y, 0.5f, $"{places[index].Name} on the ground");
            }
            return bodies;
        }

        private static void Search(WildernessRuntimeTestHarness harness, MapChannel pools, DynamicObject body)
        {
            harness.MoveTo(body.Position + new Vector3(0, 0, 1));
            Use(harness, pools, body, PoolsDeathPatrolV1.UseArgument, recovers: true);
            harness.Drain();
        }

        /// <summary>The NPC of the pool (RequireNpc's, without its check that the creature id is the pool id: the Hermit's are not).</summary>
        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            var npc = Living(pools).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"Pool {pool} must stand a real public NPC in the Pools.");
            return npc;
        }

        /// <summary>The NPC hears the objective (only that one), and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        /// <summary>Completed Pools missions in the character's history (as PlateauCampResistanceTests.SeedCompleted).</summary>
        private static void SeedPoolsCompleted(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed)
                        unit.CharacterMissions.Runtime.Add(new Rasa.Structures.Char.CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = System.Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = PoolsMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            WildernessRanjaGorgeTests.Reload(harness);
        }

        /// <summary>A use of the object: the request, then its recovery if one is due (as PlateauFortDefianceTests).</summary>
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
