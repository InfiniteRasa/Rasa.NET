using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesPaludosTests;

    /// <summary>
    /// To-fix rows 1 and 4: a Maw of upstream pool 540037 killed Warrior Dikah (1875's giver) with one hit, 100 hit points
    /// (no creature_stat row). Why these tests: a quest giver within a hostile pool's reach dies and blocks its chain; a giver
    /// without stats has the server's 100 hit points.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesSafeGroundTests
    {
        /// <summary>
        /// Every friendly Marshes NPC pool (760300-760399) stands at least SafeClearance outside every hostile automatic pool's
        /// circle on its map, and the start-up check (ValidatePools) names none of them.
        /// </summary>
        [TestMethod]
        public void TheMarshesNpcsStandClearOfEveryHostilePool()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools.Values.ToList();
            bool Side(uint creature, TargetCategory side) =>
                CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory == side;
            var npcs = pools.Where(pool => pool.DbId is >= 760300 and <= 760399 && pool.SpawnSlot.Count > 0 &&
                pool.SpawnSlot.TrueForAll(slot => Side(slot.CreatureId, TargetCategory.Friendly))).ToList();
            Assert.IsTrue(npcs.Any(pool => pool.DbId == MarshesSentryTowerNpcsV1.Dikah), "Dikah is one of them");
            foreach (var npc in npcs)
                foreach (var hostile in pools.Where(pool => pool.MapContextId == npc.MapContextId && pool.Mode == 0 &&
                             pool.SpawnSlot.Exists(slot => slot.CountMax > 0) &&
                             pool.SpawnSlot.TrueForAll(slot => Side(slot.CreatureId, TargetCategory.Hostile))))
                {
                    var gap = Vector2.Distance(new(npc.Position.X, npc.Position.Z), new(hostile.Position.X, hostile.Position.Z)) - hostile.Radius;
                    Assert.IsGreaterThanOrEqualTo(SpawnPoolManager.SafeClearance, gap, $"NPC pool {npc.DbId} and hostile pool {hostile.DbId}");
                }
            var found = SpawnPoolManager.Instance.PoolsTooClose(new List<(uint, Vector3, string)>()).Select(entry => entry.Message).ToList();
            Assert.IsFalse(found.Any(message => message.Contains("of pool 7603")), string.Join("\n", found));
        }

        /// <summary>
        /// To-fix row 10: the defence scenes' wards (Trapped! 1052, Head East 920) are personal scene creatures, which the
        /// start-up check does not see. Why this test: each ward stands SafeClearance outside every hostile automatic pool's
        /// circle on its map, as the friendly pools must.
        /// </summary>
        [TestMethod]
        public void TheDefenceWardsStandClearOfEveryHostilePool()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools.Values.ToList();
            bool Side(uint creature, TargetCategory side) =>
                CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory == side;
            var revision = MarshesMissionDataV1.Revision;
            var scenes = harness.World.Set<Rasa.Structures.World.MissionSceneBindingEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == revision).ToList()
                .Select(entry => (entry.MissionId, Scene: System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(
                    entry.Bindings, Rasa.Missions.Content.MissionContentCodec.Options)))
                .Where(entry => entry.Scene.Script == "marshes.defence").ToList();
            CollectionAssert.AreEquivalent(new[] { MarshesTrappedV1.MissionId, MarshesHeadEastV2.MissionId },
                scenes.Select(entry => entry.MissionId).ToArray(), "the defence scenes");
            var checkedWards = 0;
            foreach (var (mission, scene) in scenes)
                foreach (var ward in scene.Actors.Values.Where(actor => actor.Role.StartsWith("ward-") && actor.Position != null))
                {
                    checkedWards++;
                    foreach (var hostile in pools.Where(pool => pool.MapContextId == scene.MapContextId && pool.Mode == 0 &&
                                 pool.SpawnSlot.Exists(slot => slot.CountMax > 0) &&
                                 pool.SpawnSlot.TrueForAll(slot => Side(slot.CreatureId, TargetCategory.Hostile))))
                    {
                        var gap = Vector2.Distance(new(ward.Position.X, ward.Position.Z), new(hostile.Position.X, hostile.Position.Z)) - hostile.Radius;
                        Assert.IsGreaterThanOrEqualTo(SpawnPoolManager.SafeClearance, gap, $"{mission} {ward.Role} and hostile pool {hostile.DbId}");
                    }
                }
            Assert.IsGreaterThan(4, checkedWards, "the wards are read");
        }

        /// <summary>Dikah: Ranger Anjuhi's stats (4500 hit points, armour 300), at his new place on the walked-to ground.</summary>
        [TestMethod]
        public void DikahHasAnUpstreamGiversHitPointsAndStandsOnTheGround()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dikah = Npc(marshes, MarshesSentryTowerNpcsV1.Dikah);
            Assert.AreEqual((int)MarshesSafeGroundV1.DikahHitPoints, dikah.Attributes[Attributes.Health].NormalMax, "max hit points");
            Assert.AreEqual((int)MarshesSafeGroundV1.DikahArmor, dikah.Attributes[Attributes.Armor].NormalMax, "armour");
            var at = MarshesSafeGroundV1.At(MarshesSentryTowerNpcsV1.Dikah);
            Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(dikah.Position.X, dikah.Position.Z)));
            AssertOnGround(marshes, dikah.Position, "Dikah");
            Assert.IsLessThan(25f, Vector2.Distance(new(-521, -179), new(dikah.Position.X, dikah.Position.Z)), "near TaRapedia's /loc");
        }

        /// <summary>
        /// To-fix row 11: in the bot's Village Ruins copies Kirii and Rakla (and Qunle) never came into view. Why this test: in
        /// a 1743 copy the pool worker itself (no forced spawn) stands each Ruins NPC at its pool, visible to the player (the
        /// move, Marshes_safe_ground, keeps them out of the Grubbers' reach: a pool killed in a squad's copy stays dead for the
        /// copy's run).
        /// </summary>
        [TestMethod]
        public void TheRuinsNpcsStandInTheCopyAndAreSeen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ruins = MarshesPreoDasTests.IntoRuins(harness);
            Assert.IsNotNull(ruins.SquadState, "a squad's copy");
            for (var second = 0; second < 10; second++)
            {
                SpawnPoolManager.Instance.SpawnPoolWorker(ruins, 1000);
                MarshesPreoDasTests.Tick(harness, ruins, 1000);
            }
            foreach (var pool in new[] { MarshesPreoDasNpcsV1.Elder, MarshesPreoDasNpcsV1.Qunle, MarshesPreoDasVillageNpcsV1.Tola,
                         MarshesPreoDasVillageNpcsV1.Kirii, MarshesPreoDasVillageNpcsV1.Rakla })
            {
                var npc = Rasa.Test.Missions.Plateau.PlateauCampResistanceTests.Living(ruins).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool);
                Assert.IsNotNull(npc, $"{pool} stands in the copy");
                Assert.IsFalse(HiddenCreatures.IsHiddenFrom(npc, harness.Client), $"{pool} is seen");
                Assert.IsLessThan(40f, Vector3.Distance(npc.Position, SpawnPoolManager.Instance.LoadedSpawnPools[pool].Position), $"{pool} by its pool (an earlier test may have walked it)");
            }
        }

        /// <summary>The moved Village Ruins NPCs on the ground, walked to from their old places, their markers with them.</summary>
        [TestMethod]
        public void TheVillageRuinsNpcsAndTheirMarkersMoved()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ruins = MarshesPreoDasTests.IntoRuins(harness);
            foreach (var (pool, before, now) in MarshesSafeGroundV1.Moves.Where(move => move.Pool != MarshesSentryTowerNpcsV1.Dikah))
            {
                // The pool's place (the squad copy outlives a test: its NPC may have been walked by an earlier one).
                var place = SpawnPoolManager.Instance.LoadedSpawnPools[pool].Position;
                Assert.IsLessThan(0.1f, Vector3.Distance(MarshesPreoDasTests.ToVector(now), place), $"{pool}'s pool at {place}");
                Assert.IsNotNull(Npc(ruins, pool), $"{pool} stands");
                AssertOnGround(ruins, place, $"{pool}");
                MarshesPreoDasTests.AssertPath(ruins, MarshesPreoDasTests.ToVector(before), place, $"{pool} from his old place");
            }
            foreach (var marker in MarshesSafeGroundV1.Markers)
            {
                var row = harness.World.MissionIndicatorEntries.Single(entry => entry.MissionId == marker.Mission &&
                    entry.ObjectiveId == marker.Objective && entry.IndicatorId == marker.Indicator);
                var at = MarshesSafeGroundV1.At(marker.Pool);
                Assert.AreEqual((at.X, at.Z), (row.PosX, row.PosZ), $"{marker.Mission}'s marker at its NPC");
            }
        }
    }
}
