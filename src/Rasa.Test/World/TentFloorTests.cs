using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Colonel Bosley and Supply Sergeant Lipschitz were reported standing on top of their tents
    /// at Camp Resistance; they and five others like them are put on the tent's floor
    /// (Tent_floor_npcs).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TentFloorTests
    {
        private const string Migration = "20270128000000_Tent_floor_npcs";
        private const string Before = "20270127000000_Boss_frame_flags";

        private const uint Bosley = 510176;
        private const uint Lipschitz = 500283;

        [TestMethod]
        public void TheMigrationPutsEachOnItsTentsFloorAndDownPutsItBackOnTheRoof()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            Assert.AreEqual(7, TentFloors.Pools.Count);
            Assert.AreEqual(7, TentFloors.Pools.Distinct().Count());
            Assert.AreEqual(7, TentFloors.WorldUp.Length);
            Assert.AreEqual(7, TentFloors.WorldDown.Length);

            void AssertAt(params (uint Pool, double Y)[] expected)
            {
                harness.World.ChangeTracker.Clear();
                var rows = harness.World.Set<SpawnPoolEntry>().AsNoTracking().ToDictionary(entry => entry.Id);

                foreach (var (pool, y) in expected)
                {
                    Assert.IsTrue(rows.TryGetValue(pool, out var row), $"spawnpool {pool} is not in the world.");
                    Assert.AreEqual(y, row.PosY, 0.005, $"spawnpool {pool}");
                }
            }

            // The migrated world: on the floor, 5.5 m under where they stood.
            AssertAt(TentFloors.Corrected().ToArray());
            AssertAt((Bosley, 442.43), (Lipschitz, 442.43));

            var xz = harness.World.Set<SpawnPoolEntry>().AsNoTracking().Single(entry => entry.Id == Bosley);
            Assert.AreEqual((-632.3, -232.5), (Math.Round(xz.PosX, 2), Math.Round(xz.PosZ, 2)), "only the height changes");

            var migrator = harness.World.GetService<IMigrator>();

            migrator.Migrate(Before);
            AssertAt(TentFloors.Original().ToArray());
            AssertAt((Bosley, 447.95), (Lipschitz, 448.0));

            migrator.Migrate(Migration);
            AssertAt(TentFloors.Corrected().ToArray());

            // A second run of the statements changes nothing.
            foreach (var statement in TentFloors.WorldUp)
                Assert.AreEqual(0, harness.World.Database.ExecuteSqlRaw(statement), statement);
        }

        [TestMethod]
        public void ARowChangedSinceIsLeftAlone()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var migrator = harness.World.GetService<IMigrator>();

            migrator.Migrate(Before);
            harness.World.Database.ExecuteSqlRaw($"update spawnpool set pos_y = 450 where id = {Bosley};");
            migrator.Migrate(Migration);

            harness.World.ChangeTracker.Clear();
            Assert.AreEqual(450, harness.World.Set<SpawnPoolEntry>().AsNoTracking().Single(entry => entry.Id == Bosley).PosY, 0.000001);
            Assert.AreEqual(442.43, harness.World.Set<SpawnPoolEntry>().AsNoTracking().Single(entry => entry.Id == Lipschitz).PosY, 0.000001);
        }
    }
}
