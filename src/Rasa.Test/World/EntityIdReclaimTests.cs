using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// An entity id goes back to the pool only once nothing has it, and comes out of the pool again
    /// when an entity is registered under it: an object taken out of the world and put back keeps
    /// its id to itself, and the next object made draws another (EntityManager.FreeEntity,
    /// Reclaim). A teleporter taken out and put back used to leave its id in the pool, and the next
    /// object to draw it - an ambient figure, a context's player - was refused by DynamicObjects.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EntityIdReclaimTests
    {
        private const int Draws = 3000;

        [TestMethod]
        public void AnObjectTakenOutAndPutBackKeepsItsIdToItself()
        {
            using var world = new WorldTestContext();
            var entities = EntityManager.Instance;
            var pad = new DynamicObject { DynamicObjectType = DynamicObjectType.Waypoint, Position = new Vector3(10, 0, 10) };
            var drawn = new List<ulong>();
            DynamicObject next = null;

            try
            {
                CellManager.Instance.AddToWorld(world.Map, pad);
                CellManager.Instance.RemoveFromWorld(world.Map, pad);
                CellManager.Instance.AddToWorld(world.Map, pad);

                Assert.AreSame(pad, entities.DynamicObjects[pad.EntityId]);

                next = new DynamicObject { Position = new Vector3(12, 0, 10) };
                Assert.AreNotEqual(pad.EntityId, next.EntityId);
                CellManager.Instance.AddToWorld(world.Map, next);

                drawn.AddRange(Enumerable.Range(0, Draws).Select(_ => entities.GetEntityId));
                CollectionAssert.DoesNotContain(drawn, pad.EntityId);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, pad);
                if (next != null)
                    CellManager.Instance.RemoveFromWorld(world.Map, next);
                foreach (var id in drawn)
                    entities.FreeEntity(id);
            }
        }

        [TestMethod]
        public void AnIdAnObjectOrCreatureStillHasIsNotFreed()
        {
            var entities = EntityManager.Instance;
            var obj = new DynamicObject();
            var creature = new Creature();
            var drawn = new List<ulong>();

            entities.RegisterDynamicObject(obj);
            entities.RegisterCreature(creature);

            try
            {
                entities.FreeEntity(obj.EntityId);
                entities.FreeEntity(creature.EntityId);

                drawn.AddRange(Enumerable.Range(0, Draws).Select(_ => entities.GetEntityId));
                CollectionAssert.DoesNotContain(drawn, obj.EntityId);
                CollectionAssert.DoesNotContain(drawn, creature.EntityId);
            }
            finally
            {
                entities.UnregisterDynamicObject(obj.EntityId);
                entities.UnregisterCreature(creature.EntityId);
                entities.FreeEntity(obj.EntityId);
                entities.FreeEntity(creature.EntityId);
                foreach (var id in drawn)
                    entities.FreeEntity(id);
            }
        }

        [TestMethod]
        public void TheOtherFreedIdsAreDrawnInTheOrderTheyWereFreed()
        {
            var entities = EntityManager.Instance;

            // Whatever earlier tests left in the pool first, so these three are the ones drawn next.
            var earlier = new List<ulong>();
            var fresh = entities.AllocateUnrecycledEntityId();
            ulong id;
            while ((id = entities.GetEntityId) < fresh)
                earlier.Add(id);
            earlier.Add(id);

            var a = entities.AllocateUnrecycledEntityId();
            var b = entities.AllocateUnrecycledEntityId();
            var c = entities.AllocateUnrecycledEntityId();
            var obj = new DynamicObject { EntityId = b };

            entities.FreeEntity(a);
            entities.FreeEntity(b);
            entities.FreeEntity(c);
            entities.RegisterDynamicObject(obj);

            try
            {
                Assert.AreEqual(a, entities.GetEntityId);
                Assert.AreEqual(c, entities.GetEntityId);
                Assert.AreNotEqual(b, entities.GetEntityId);
            }
            finally
            {
                entities.UnregisterDynamicObject(b);
                foreach (var e in earlier.Append(a).Append(b).Append(c))
                    entities.FreeEntity(e);
            }
        }

        [TestMethod]
        public void ATeleporterPutBackByTheWorkerIsNotDrawnByTheNextObject()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entities = EntityManager.Instance;
            var pad = harness.Map.Teleporters.Values.First(t => t.IsInWorld);
            var drawn = new List<ulong>();

            CellManager.Instance.RemoveFromWorld(harness.Map, pad);
            pad.IsInWorld = false;
            pad.RespawnTime = 0;
            harness.Objects.DynamicObjectWorker(harness.Map, 0);

            try
            {
                Assert.IsTrue(pad.IsInWorld);
                Assert.AreSame(pad, entities.DynamicObjects[pad.EntityId]);

                drawn.AddRange(Enumerable.Range(0, Draws).Select(_ => entities.GetEntityId));
                CollectionAssert.DoesNotContain(drawn, pad.EntityId);
            }
            finally
            {
                foreach (var id in drawn)
                    entities.FreeEntity(id);
            }
        }
    }
}
