using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    /// <summary>
    /// A runtime harness builds a whole world: a map channel, its managers, creatures and a
    /// navmesh query. Once it is disposed nothing static may keep that world reachable, or every
    /// test that builds one adds its world to the test host for the rest of the run (#132: the
    /// suite in one process grew past 12 GB).
    /// </summary>
    [TestClass]
    public class RuntimeHarnessRetentionTests
    {
        [TestMethod]
        public void DisposedWildernessHarnessLeavesItsWorldCollectable()
        {
            var world = CreateAndDisposeWildernessHarness();

            AssertCollected(world.Map, "map channel");
            AssertCollected(world.Maps, "map channel manager");
            AssertCollected(world.Manager, "mission application");
        }

        [TestMethod]
        public void DisposedBootcampHarnessLeavesItsWorldCollectable()
        {
            var world = CreateAndDisposeBootcampHarness();

            AssertCollected(world.Map, "map channel");
            AssertCollected(world.Maps, "map channel manager");
        }

        // Not inlined, so no local of the caller's frame holds the harness or its world.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference Map, WeakReference Maps, WeakReference Manager) CreateAndDisposeWildernessHarness()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld();
            for (var tick = 0; tick < 8; tick++)
                harness.Tick();
            return (new WeakReference(harness.Map), new WeakReference(harness.Maps),
                new WeakReference(harness.Manager));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference Map, WeakReference Maps) CreateAndDisposeBootcampHarness()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            return (new WeakReference(harness.BootcampMap), new WeakReference(harness.Maps));
        }

        private static void AssertCollected(WeakReference reference, string what)
        {
            for (var attempt = 0; attempt < 3 && reference.IsAlive; attempt++)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }
            Assert.IsFalse(reference.IsAlive, $"A disposed harness's {what} is still reachable.");
        }
    }
}
