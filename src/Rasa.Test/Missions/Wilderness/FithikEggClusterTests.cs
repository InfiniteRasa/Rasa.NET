using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Services.Preloader.Missions.Wilderness;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// A Fithik egg cluster hatches when a player walks onto it (FithikEggClusters), and the Ranja
    /// clusters start in their idle state with a hatchling to bring out (Ranja_egg_clusters_hatch).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FithikEggClusterTests
    {
        private const string Before = "20261208000000_Add_world_wormholes";
        private const string Migration = "20261209000000_Ranja_egg_clusters_hatch";

        private long _now;

        [TestInitialize]
        public void Start()
        {
            _now = 10_000_000;
            FithikEggClusters.Reset();
            FithikEggClusters.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            FithikEggClusters.Reset();
            FithikEggClusters.Now = () => Environment.TickCount64;
        }

        [TestMethod]
        public void WalkingOntoAClusterHatchesItAndItGrowsBackOnlyOnceWhatCameOutIsDead()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var egg = Egg(harness, harness.Client.Player.Position + new Vector3(6, 0, 0));
            harness.Drain();

            // Near it is not on it.
            FithikEggClusters.Worker(harness.Map);
            Assert.AreEqual(UseObjectState.CsStateIdle, egg.StateId);

            harness.MoveTo(egg.Position + new Vector3(1, 0, 0));
            harness.Drain();
            FithikEggClusters.Worker(harness.Map);

            Assert.AreEqual(UseObjectState.CsStateSpawn, egg.StateId, "Do Spawn");
            Assert.AreEqual(UseObjectState.CsStateSpawn, harness.Drain().OfType<ForceStatePacket>().Single().State);
            Assert.IsEmpty(Hatchlings(harness).ToArray(), "nothing out until the hatching is done");

            _now += FithikEggClusters.HatchMs;
            FithikEggClusters.Worker(harness.Map);

            var brood = Hatchlings(harness).ToList();
            Assert.HasCount(FithikEggClusters.Hatchlings, brood);
            Assert.AreEqual(UseObjectState.CsStateEnd, egg.StateId, "End Spawn");

            var packets = harness.Drain();
            Assert.AreEqual(UseObjectState.CsStateEnd, packets.OfType<ForceStatePacket>().Single().State);
            Assert.HasCount(FithikEggClusters.Hatchlings, packets.OfType<PerformRecoveryPacket>()
                .Where(p => p.ActionId == FithikEggClusters.BirthAction && p.PerformType == PerformType.TwoArgs).ToArray(), "each one born");
            Assert.IsFalse(packets.OfType<AbilityRecoveryPacket>().Any(p => p.ActionId == FithikEggClusters.BirthAction), "not in an ability's shape");

            foreach (var hatchling in brood)
            {
                Assert.AreEqual(TargetCategory.Hostile, hatchling.TargetCategory);
                Assert.IsTrue(FithikEggClusters.IsHatching(hatchling), "climbing out");
                Assert.IsTrue(hatchling.Hate.Ranked().Any(grudge => grudge.Key == harness.Client.Player.EntityId), "after whoever set it off");
                Assert.IsLessThan(3f, Vector2.Distance(new Vector2(hatchling.Position.X, hatchling.Position.Z), new Vector2(egg.Position.X, egg.Position.Z)));
            }

            // Held where it came out while it climbs out, though its target is beside it.
            var where = brood.Select(hatchling => hatchling.Position).ToList();
            harness.Tick(1000);
            harness.Tick(1000);
            CollectionAssert.AreEqual(where, brood.Select(hatchling => hatchling.Position).ToList(), "held while it is born");

            _now += FithikEggClusters.HatchedMs;
            FithikEggClusters.Worker(harness.Map);

            Assert.AreEqual(UseObjectState.CsStateIdle, egg.StateId);
            Assert.AreEqual(UseObjectState.CsStateIdle, harness.Drain().OfType<UsePacket>().Single().CurState, "Use, so it grows back");
            Assert.IsTrue(brood.All(FithikEggClusters.IsHatching), "still climbing out: CREATURE_BIRTH is 7.2 s");

            // Idle again, with the player still on it and its brood alive: it does not hatch.
            _now += FithikEggClusters.GrowMs + FithikEggClusters.RearmMs;
            FithikEggClusters.Worker(harness.Map);
            FithikEggClusters.Worker(harness.Map);
            Assert.AreEqual(UseObjectState.CsStateIdle, egg.StateId);
            Assert.IsFalse(brood.Any(FithikEggClusters.IsHatching), "out");

            foreach (var hatchling in brood)
                Kill(harness, hatchling);

            FithikEggClusters.Worker(harness.Map);
            FithikEggClusters.Worker(harness.Map);
            Assert.AreEqual(UseObjectState.CsStateSpawn, egg.StateId, "its brood dead and its time up, it hatches again");
        }

        [TestMethod]
        public void AClusterDestroyedWhileItHatchesStopsWhereItIs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var egg = Egg(harness, harness.Client.Player.Position);
            FithikEggClusters.Worker(harness.Map);
            Assert.AreEqual(UseObjectState.CsStateSpawn, egg.StateId);

            egg.StateId = UseObjectState.StateDestroyed;
            _now += FithikEggClusters.HatchMs;
            FithikEggClusters.Worker(harness.Map);

            Assert.IsEmpty(Hatchlings(harness).ToArray());
            Assert.AreEqual(UseObjectState.StateDestroyed, egg.StateId);
        }

        [TestMethod]
        public void AHatchlingFightingNothingIsTakenAwayWhenItsTimeIsUp()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var egg = Egg(harness, harness.Client.Player.Position);
            FithikEggClusters.Worker(harness.Map);
            _now += FithikEggClusters.HatchMs;
            FithikEggClusters.Worker(harness.Map);

            var brood = Hatchlings(harness).ToList();
            brood[0].Hate.Clear();
            brood[1].Hate.Clear();

            _now += FithikEggClusters.LifetimeMs;
            FithikEggClusters.Worker(harness.Map);

            Assert.HasCount(1, Hatchlings(harness).ToArray(), "the one still fighting stays");
        }

        /// <summary>
        /// A brood taken away rather than killed - its time up with nothing to fight, or taken off
        /// the map - is gone: the cluster hatches again once its time is up.
        /// </summary>
        [TestMethod]
        public void AClusterWhoseBroodWasTakenAwayHatchesAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var egg = Egg(harness, harness.Client.Player.Position);
            FithikEggClusters.Worker(harness.Map);
            _now += FithikEggClusters.HatchMs;
            FithikEggClusters.Worker(harness.Map);

            var brood = Hatchlings(harness).ToList();
            Assert.HasCount(FithikEggClusters.Hatchlings, brood);

            // Two run out their time fighting nothing; the third is taken off the map.
            brood[0].Hate.Clear();
            brood[1].Hate.Clear();
            _now += FithikEggClusters.HatchedMs;
            FithikEggClusters.Worker(harness.Map);
            _now += FithikEggClusters.GrowMs;
            FithikEggClusters.Worker(harness.Map);
            _now += FithikEggClusters.LifetimeMs;
            FithikEggClusters.Worker(harness.Map);
            CellManager.Instance.RemoveCreatureFromWorld(harness.Map, brood[2]);

            Assert.IsEmpty(Hatchlings(harness).ToArray(), "none of them left");
            Assert.IsTrue(brood.All(hatchling => hatchling.State != CharacterState.Dead), "and none of them killed");

            FithikEggClusters.Worker(harness.Map);
            FithikEggClusters.Worker(harness.Map);

            Assert.AreEqual(UseObjectState.CsStateSpawn, egg.StateId, "its brood gone and its time up, it hatches again");
        }

        [TestMethod]
        public void TheRanjaClustersStartIdleAndTheHatchlingHasItsRowsAndDownPutsThemBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                AssertStates(world, RanjaEggClusterHatching.IdleState);

                var hatchling = world.CreatureEntries.AsNoTracking().Single(row => row.Id == RanjaEggClusterHatching.HatchlingCreatureId);
                var bite = world.CreatureActionEntries.AsNoTracking().Single(row => row.Id == hatchling.Action1);
                Assert.AreEqual((21499u, 0u), (hatchling.ClassId, hatchling.Faction), "Bane_Fithik_Wingless_EggCluster, hostile");
                Assert.AreEqual((174u, 34u), (bite.ActionId, bite.ActionArgId), "Weapon_Creature_Fithik's bite");
                Assert.IsTrue(world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == hatchling.Id));

                world.GetService<IMigrator>().Migrate(Before);

                AssertStates(world, 0);
                Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => row.Id == RanjaEggClusterHatching.HatchlingCreatureId));
                Assert.IsFalse(world.CreatureActionEntries.AsNoTracking().Any(row => row.Id == RanjaEggClusterHatching.HatchlingAction));

                world.GetService<IMigrator>().Migrate(Migration);

                AssertStates(world, RanjaEggClusterHatching.IdleState);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "values (73101, 'Fithik Hatchling weapon 174/34', 174, 34, 0.5, 3.0, 1500, 0, 8, 12, 1);");
            StringAssert.Contains(up, "values (552001, 'Fithik Hatchling - Ranja egg clusters', 21499, 0, 8, 400, 0, 9, 5, 73101, 0, 0, 0, 0, 0, 0, 0);");
            StringAssert.Contains(up, "\"initialObjectState\":187");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "delete from creature where id = 552001;");
            StringAssert.Contains(down, "\"initialObjectState\":0");
        }

        private static void AssertStates(WorldContext world, uint state)
        {
            using var command = world.Database.GetDbConnection().CreateCommand();
            world.Database.OpenConnection();
            command.CommandText = "select bindings from mission_scene_binding where mission_id = 860";
            var bindings = (string)command.ExecuteScalar();

            using var document = JsonDocument.Parse(bindings);
            var actors = document.RootElement.GetProperty("actors").EnumerateObject().ToList();

            Assert.HasCount(4, actors);
            foreach (var actor in actors)
            {
                Assert.AreEqual(10180u, actor.Value.GetProperty("templateId").GetUInt32());
                Assert.AreEqual(state, actor.Value.GetProperty("initialObjectState").GetUInt32(), actor.Name);
            }
        }

        /// <summary>An egg cluster set down on the map as the Ranja scene sets one down, in its idle state.</summary>
        private static DynamicObject Egg(WildernessRuntimeTestHarness harness, Vector3 position)
        {
            var egg = DynamicObjectManager.Instance.CreateScenarioDynamicObject(harness.Map, FithikEggClusters.EggClusterClass,
                position, 0, $"test:egg:{Guid.NewGuid():N}", true);
            egg.StateId = UseObjectState.CsStateIdle;
            harness.Map.DynamicObjects.Add(egg);
            CellManager.Instance.AddToWorld(harness.Map, egg);
            egg.IsInWorld = true;
            return egg;
        }

        private static System.Collections.Generic.IEnumerable<Creature> Hatchlings(WildernessRuntimeTestHarness harness) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.DbId == RanjaEggClusterHatching.HatchlingCreatureId && creature.State != CharacterState.Dead);

        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(harness.Map, creature, null);
        }
    }
}
