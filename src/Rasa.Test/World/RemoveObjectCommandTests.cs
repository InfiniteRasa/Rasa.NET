using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// .removeobj takes what it names off the map on the server (ChatCommandsManager.RemoveFromMap).
    /// It used to send a destroy for the id to every client on the map and leave the entity where
    /// it was: in its cells, its tables and its pool, back for the next player to walk by.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class RemoveObjectCommandTests
    {
        private const EntityClasses ObjectClass = EntityClasses.UsableCrSpawnerHumDropshipV01;

        [TestMethod]
        public void APlainObjectGoesFromTheServerAndFromWhoeverSawIt()
        {
            using var world = new WorldTestContext();
            world.AddClass(ObjectClass);
            var watcher = PlayerDeathTests.Player(world, 0, 0);
            var obj = Place(world);
            WorldTestContext.Drain(watcher);

            Assert.AreEqual($"Removed object {obj.EntityId}.", ChatCommandsManager.RemoveFromMap(world.Map, obj.EntityId));

            Assert.IsFalse(EntityManager.Instance.TryGetObject(obj.EntityId, out _), "out of the entity tables");
            Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.DynamicObjectList.Contains(obj)), "out of the cells");
            Assert.IsTrue(Destroyed(watcher).Contains(obj.EntityId), "gone from the screen of whoever saw it");
        }

        [TestMethod]
        public void AnObjectASystemKeepsIsRefused()
        {
            using var world = new WorldTestContext();
            world.AddClass(ObjectClass);
            var watcher = PlayerDeathTests.Player(world, 0, 0);
            var waypoint = Place(world, DynamicObjectType.Waypoint);
            WorldTestContext.Drain(watcher);

            try
            {
                StringAssert.Contains(ChatCommandsManager.RemoveFromMap(world.Map, waypoint.EntityId), "Waypoint");

                Assert.IsTrue(EntityManager.Instance.TryGetObject(waypoint.EntityId, out _), "left as it was");
                Assert.IsFalse(Destroyed(watcher).Any(), "and nothing sent");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, waypoint);
            }
        }

        [TestMethod]
        public void ALivingCreatureOfAPoolGoesAndThePoolMayFillItsPlace()
        {
            using var world = new WorldTestContext();
            var watcher = PlayerDeathTests.Player(world, 0, 0);
            var pool = new SpawnPool { SpawnSlot = new List<SpawnPoolSlot>(), AliveCreatures = 1 };
            var creature = Spawn(world, pool);
            WorldTestContext.Drain(watcher);

            StringAssert.StartsWith(ChatCommandsManager.RemoveFromMap(world.Map, creature.EntityId), $"Removed creature {creature.EntityId}");

            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(creature.EntityId), "out of the entity tables");
            Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature)), "out of the cells");
            Assert.IsTrue(Destroyed(watcher).Contains(creature.EntityId));
            Assert.AreEqual(0, (int)pool.AliveCreatures, "no longer counted alive");
            Assert.AreEqual(0, (int)pool.DeadCreatures, "nor as a corpse waiting");
        }

        [TestMethod]
        public void APlayerAnItemOrNothingIsRefused()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0);
            var other = PlayerDeathTests.Player(world, 5, 0);
            WorldTestContext.Drain(other);

            StringAssert.Contains(ChatCommandsManager.RemoveFromMap(world.Map, player.Player.EntityId), "is a player");
            Assert.IsTrue(EntityManager.Instance.Players.ContainsKey(player.Player.EntityId));
            Assert.IsFalse(Destroyed(other).Any(), "nothing sent");

            Assert.AreEqual("Nothing has entity id 987654321.", ChatCommandsManager.RemoveFromMap(world.Map, 987654321));
        }

        private static DynamicObject Place(WorldTestContext world, DynamicObjectType type = 0)
        {
            var obj = new DynamicObject
            {
                Position = new Vector3(0, 0, 5),
                MapContextId = world.Map.MapInfo.MapContextId,
                EntityClassId = ObjectClass,
                DynamicObjectType = type
            };

            CellManager.Instance.AddToWorld(world.Map, obj);
            Assert.IsTrue(EntityManager.Instance.TryGetObject(obj.EntityId, out _));

            return obj;
        }

        private static Creature Spawn(WorldTestContext world, SpawnPool pool)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, 5),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                SpawnPool = pool,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static List<ulong> Destroyed(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>()
                .Select(m => m.Packet).OfType<DestroyPhysicalEntityPacket>().Select(p => p.EntityId).ToList();
    }
}
