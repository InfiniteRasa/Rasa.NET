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
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// The Snake Pit's missions, played through as a player plays them: Supply Chief Riggins's Building Materials (1314:
    /// carapaces, thigh bones and sinew from the Bane's corpses) and Dr. Hester's Medicinal Measures (1066: nectar from ten
    /// Mawflowers).
    ///
    /// Why these tests: both NPCs are new on the server, the drops come only from the named creatures at their chance, and
    /// the plants are the player's own scene objects. An NPC off the navmesh, a drop from the wrong creature or on the wrong
    /// side of its chance, a kill counted as a pickup, a plant harvested twice, nectar kept after an abandon, or progress lost
    /// at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsSnakePitTests
    {
        private const uint BuildingMaterials = PoolsBuildingMaterialsV1.MissionId;
        private const uint MedicinalMeasures = PoolsMedicinalMeasuresV1.MissionId;
        private const uint Riggins = PoolsSnakePitNpcsV1.Riggins, Hester = PoolsSnakePitNpcsV1.Hester;
        private const uint TechnicianSergeant = 531091;   // a Thrax of the same pools that is no "Thrax soldier"
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Snake Pit's waypoint (World DB teleporter 48), where a player arrives.</summary>
        private static readonly Vector3 SnakePitWaypoint = new(-941.7f, 919.94f, 367.4f);

        [TestMethod]
        public void RigginsAndHesterStandAtTheSnakePitAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            foreach (var (pool, name, package, at) in new[]
            {
                (Riggins, PoolsSnakePitNpcsV1.RigginsName, PoolsSnakePitNpcsV1.RigginsPackage, PoolsSnakePitNpcsV1.RigginsAt),
                (Hester, PoolsSnakePitNpcsV1.HesterName, PoolsSnakePitNpcsV1.HesterPackage, PoolsSnakePitNpcsV1.HesterAt)
            })
            {
                var npc = Npc(pools, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                Assert.AreEqual(new Vector3((float)at.X, (float)at.Y, (float)at.Z), npc.Position);
                var ground = pools.NavMesh.GroundHeight(npc.Position);
                Assert.IsNotNull(ground, $"navmesh under {pool}");
                Assert.AreEqual(ground.Value, npc.Position.Y, 0.5f, $"{pool} on the ground");
                Assert.IsTrue(pools.NavMesh.FindPath(SnakePitWaypoint, npc.Position, out var complete) != null && complete, $"{pool} is walked to");
            }
        }

        [TestMethod]
        public void BuildingMaterialsTakesTheBanesPartsToRiggins()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, Riggins);
            var bane = PoolsOf(harness, PoolsBuildingMaterialsV1.ThraxSoldier, PoolsBuildingMaterialsV1.HunterTracker,
                PoolsBuildingMaterialsV1.FiendPupHowler);
            Spawn(pools, bane);

            var offer = WildernessRanjaGorgeTests.Offer(harness, riggins, BuildingMaterials);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsBuildingMaterialsV1.BioVest, PoolsBuildingMaterialsV1.GravitonVest,
                    PoolsBuildingMaterialsV1.MechLegs, PoolsBuildingMaterialsV1.StealthVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(Holds(Kill(harness, pools, PoolsBuildingMaterialsV1.ThraxSoldier), PoolsBuildingMaterialsV1.CarapaceTemplate),
                "no drop before the mission");
            harness.MoveTo(riggins.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, BuildingMaterials));
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count(packet => packet.MissionId == BuildingMaterials));

            // Each part only from its creature, and only under its chance.
            Assert.IsFalse(Holds(Kill(harness, pools, TechnicianSergeant), PoolsBuildingMaterialsV1.CarapaceTemplate), "a Technician is no soldier");
            foreach (var (creature, template) in Parts)
            {
                percentile = PoolsBuildingMaterialsV1.DropChance;
                Assert.IsFalse(Holds(Kill(harness, pools, creature), template), $"{creature}: 50 misses");
                percentile = PoolsBuildingMaterialsV1.DropChance - 1;
                var loot = Kill(harness, pools, creature);
                Assert.IsTrue(Holds(loot, template), $"{creature}: 49 drops");
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, template), "a death is not a pickup");
                PickUp(harness, loot);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), $"{template} picked up");
            }
            foreach (var objective in new uint[] { 1, 2, 3 })
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BuildingMaterials, objective));

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBuildingMaterialsV1.CarapaceTemplate), "kept at a reload");

            // The rest, a kill and a pickup each; the dead come back after their respawn time.
            var completed = new List<uint>();
            foreach (var ((creature, template), count, objective) in Parts.Zip(new[]
                {
                    PoolsBuildingMaterialsV1.Carapaces, PoolsBuildingMaterialsV1.Bones, PoolsBuildingMaterialsV1.Sinews
                }, (part, count) => (part, count)).Select((entry, index) => (entry.part, entry.count, (uint)index + 1)))
            {
                while (WildernessRanjaGorgeTests.Held(harness, template) < count)
                {
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BuildingMaterials, objective), $"{objective} open");
                    if (!Living(pools).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                        Respawn(pools, bane);
                    PickUp(harness, Kill(harness, pools, creature));
                    completed.AddRange(harness.Drain().OfType<ObjectiveCompletedPacket>()
                        .Where(packet => packet.MissionId == BuildingMaterials).Select(packet => packet.ObjectiveId));
                }
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, BuildingMaterials, objective), $"{objective} done");
            }
            CollectionAssert.AreEqual(new[] { 1U, 2U, 3U }, completed.ToArray(), "one ObjectiveCompleted each");
            Assert.IsTrue(harness.Client.Player.Missions[BuildingMaterials].Completeable);

            // A turn-in whose save fails pays nothing and keeps the parts.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Building Materials turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, riggins.EntityId, BuildingMaterials, 1));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(PoolsBuildingMaterialsV1.Sinews, WildernessRanjaGorgeTests.Held(harness, PoolsBuildingMaterialsV1.SinewTemplate));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, riggins, BuildingMaterials, 1,
                PoolsBuildingMaterialsV1.Experience, PoolsBuildingMaterialsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == BuildingMaterials));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBuildingMaterialsV1.GravitonVest), "the selected vest");
            foreach (var (_, template) in Parts)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, template), "Riggins keeps the parts");
        }

        /// <summary>A part stays on the corpse while the bag is full; an abandoned mission is offered again.</summary>
        [TestMethod]
        public void BuildingMaterialsPartsWaitForRoomAndTheMissionCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var riggins = Npc(pools, Riggins);
            Spawn(pools, PoolsOf(harness, PoolsBuildingMaterialsV1.FiendPupHowler));
            harness.MoveTo(riggins.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, riggins.EntityId, BuildingMaterials));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, pools, PoolsBuildingMaterialsV1.FiendPupHowler);
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsBuildingMaterialsV1.SinewTemplate), "no room");
            Assert.IsTrue(Holds(loot, PoolsBuildingMaterialsV1.SinewTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBuildingMaterialsV1.SinewTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, BuildingMaterials));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, riggins, BuildingMaterials), "offered again after an abandon");
        }

        [TestMethod]
        public void MedicinalMeasuresTakesTenMawflowersNectarToHester()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hester = Npc(pools, Hester);
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == MedicinalMeasures), "no plant before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, hester, MedicinalMeasures);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            CollectionAssert.AreEquivalent(HesterRewards, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(hester.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hester.EntityId, MedicinalMeasures));
            harness.Drain();

            var plants = Plants(harness, pools);
            for (var index = 0; index < plants.Length; index++)
            {
                harness.MoveTo(plants[index].Position + new Vector3(0, 0, 1));
                Use(harness, pools, plants[index], PoolsMedicinalMeasuresV1.UseArgument, recovers: true);
                Assert.AreEqual((uint)index + 1, WildernessRanjaGorgeTests.Held(harness, PoolsMedicinalMeasuresV1.NectarTemplate));
                Assert.AreEqual((UseObjectState)PoolsMedicinalMeasuresV1.TakenState, plants[index].StateId);
                Assert.IsFalse(plants[index].IsEnabled, $"plant {index + 1} harvested once");
                if (index == 0)
                {
                    Use(harness, pools, plants[0], PoolsMedicinalMeasuresV1.UseArgument, recovers: false);
                    Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMedicinalMeasuresV1.NectarTemplate), "one nectar a plant");
                }
                if (index == 4)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, PoolsMedicinalMeasuresV1.NectarTemplate));
                    harness.Drain();
                }
                if (index < plants.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, MedicinalMeasures, PoolsMedicinalMeasuresV1.Collect));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, MedicinalMeasures, PoolsMedicinalMeasuresV1.Collect));
            Assert.IsTrue(harness.Client.Player.Missions[MedicinalMeasures].Completeable);

            var held = HesterRewards.Select(reward => WildernessRanjaGorgeTests.Held(harness, reward.Template)).ToArray();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hester, MedicinalMeasures, null,
                PoolsMedicinalMeasuresV1.Experience, PoolsMedicinalMeasuresV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == MedicinalMeasures));
            for (var i = 0; i < HesterRewards.Length; i++)
                Assert.AreEqual(held[i] + HesterRewards[i].Quantity, WildernessRanjaGorgeTests.Held(harness, HesterRewards[i].Template));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMedicinalMeasuresV1.NectarTemplate), "Hester takes the nectar");
        }

        /// <summary>An abandon takes the nectar and the plants; taken again, ten new plants stand.</summary>
        [TestMethod]
        public void MedicinalMeasuresAbandonedTakesTheNectarBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hester = Npc(pools, Hester);
            harness.MoveTo(hester.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hester.EntityId, MedicinalMeasures));
            harness.Drain();
            var plants = Plants(harness, pools);
            harness.MoveTo(plants[0].Position + new Vector3(0, 0, 1));
            Use(harness, pools, plants[0], PoolsMedicinalMeasuresV1.UseArgument, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsMedicinalMeasuresV1.NectarTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, MedicinalMeasures));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsMedicinalMeasuresV1.NectarTemplate), "the nectar goes with the mission");

            harness.MoveTo(hester.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hester.EntityId, MedicinalMeasures));
            harness.Drain();
            Assert.IsTrue(Plants(harness, pools).All(plant => plant.IsEnabled), "ten new plants");
        }

        private static readonly (uint Creature, uint Template)[] Parts =
        {
            (PoolsBuildingMaterialsV1.ThraxSoldier, PoolsBuildingMaterialsV1.CarapaceTemplate),
            (PoolsBuildingMaterialsV1.HunterTracker, PoolsBuildingMaterialsV1.BoneTemplate),
            (PoolsBuildingMaterialsV1.FiendPupHowler, PoolsBuildingMaterialsV1.SinewTemplate)
        };

        private static readonly (uint Template, uint Quantity)[] HesterRewards =
        {
            (PoolsMedicinalMeasuresV1.EmpBomb, PoolsMedicinalMeasuresV1.EmpBombs),
            (PoolsMedicinalMeasuresV1.ArmorCharger, PoolsMedicinalMeasuresV1.ArmorChargers),
            (PoolsMedicinalMeasuresV1.EmpGrenade, PoolsMedicinalMeasuresV1.EmpGrenades),
            (PoolsMedicinalMeasuresV1.ConcussionGrenade, PoolsMedicinalMeasuresV1.ConcussionGrenades)
        };

        /// <summary>The ten plants: the player's own, in the field, each on the navmesh, in the five models.</summary>
        private static DynamicObject[] Plants(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            var plants = Enumerable.Range(1, PoolsMedicinalMeasuresV1.Plants.Length).Select(id => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == MedicinalMeasures && obj.SceneActorRole == $"mawflower-{id}" && obj.IsEnabled)).ToArray();
            for (var index = 0; index < plants.Length; index++)
            {
                var at = PoolsMedicinalMeasuresV1.Plants[index];
                Assert.AreEqual(PoolsMedicinalMeasuresV1.PlantClasses[index % 5], (uint)plants[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), plants[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, plants[index].SceneOwnerCharacterId, "the player's own");
                var ground = pools.NavMesh.GroundHeight(plants[index].Position);
                Assert.IsNotNull(ground, $"plant {index + 1} has navmesh under it");
                Assert.AreEqual(ground.Value, at.Y, 0.5f, $"plant {index + 1} on the ground");
            }
            return plants;
        }

        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            return RequireNpc(pools, pool);
        }

        /// <summary>The Pools' spawn pools that hold one of these creatures.</summary>
        private static uint[] PoolsOf(WildernessRuntimeTestHarness harness, params uint[] creatures) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == PoolsMissionDataV1.MapContextId).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(creatures.Contains))
                .Select(row => row.Id).ToArray();

        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel pools, uint kind)
        {
            var creature = Living(pools).FirstOrDefault(candidate => candidate.DbId == kind && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {kind} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(pools, creature, harness.Client.Player);
            Assert.IsTrue(pools.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        /// <summary>These pools' dead come back: their respawn time and more has passed (as PlateauFortDefianceTests).</summary>
        private static void Respawn(MapChannel map, params uint[] pools)
        {
            var all = map.SpawnPools.ToArray();
            try
            {
                map.SpawnPools.Clear();
                map.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 600_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(map, 600_000);
            }
            finally
            {
                map.SpawnPools.Clear();
                map.SpawnPools.AddRange(all);
            }
        }

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);

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
