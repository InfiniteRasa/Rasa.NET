using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    // PvPEnabled (783), which a clan control point is given with: the client's
    // ClanControlPoint.Recv_PvPEnabled(isPvPEnabled). The client keeps the value and reads it
    // nowhere, so what is held to here is that it goes out, once, to the right kind of object,
    // in the shape the client unpacks.
    [TestClass]
    [DoNotParallelize]
    public class ClanControlPointTests
    {
        private const EntityClasses PvePoint = DynamicObjectManager.PveClanControlPointClass;
        private const EntityClasses OtherPoint = (EntityClasses)3814;

        [TestMethod]
        [DataRow(true, (byte)0x01)]
        [DataRow(false, (byte)0x02)]
        public void ThePacketIsOneBoolean(bool value, byte marshalled)
        {
            var packet = new PvPEnabledPacket(value);
            var bytes = MissionTestContext.Encode(packet);

            Assert.AreEqual(783, (int)packet.Opcode);

            using var stream = new MemoryStream(bytes);
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(marshalled, bytes[stream.Position], "True or False, not None");
            Assert.AreEqual(value, reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ThePveTestPointIsNotAPvPOne()
        {
            Assert.AreEqual(29329, (int)PvePoint);
            Assert.IsFalse(DynamicObjectManager.IsPvPClanControlPoint(PvePoint));
            Assert.IsTrue(DynamicObjectManager.IsPvPClanControlPoint((EntityClasses)29330), "what the client holds until told");

            Assert.IsTrue(DynamicObjectManager.IsClanControlPoint(Class(PvePoint, AugmentationType.ClanControlPoint)));
            Assert.IsFalse(DynamicObjectManager.IsClanControlPoint(Class(OtherPoint, AugmentationType.ControlPoint)));
            Assert.IsFalse(DynamicObjectManager.IsClanControlPoint(Class(OtherPoint)));
            Assert.IsFalse(DynamicObjectManager.IsClanControlPoint(null));
        }

        [TestMethod]
        public void AClanControlPointIsGivenWithItsFlagAfterUsableInfo()
        {
            using var world = new WorldTestContext();
            using var classes = new Classes(Class(PvePoint, AugmentationType.ClanControlPoint));

            var onlooker = Arrive(world, 12, 12);
            Drain(onlooker);

            var point = Place(world, PvePoint);

            try
            {
                // Placed in front of a client, and found there by one who arrives.
                foreach (var client in new[] { onlooker, Arrive(world, 14, 12) })
                {
                    var data = Drain(client).OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == point.EntityId).EntityData;
                    var flag = data.OfType<PvPEnabledPacket>().Single();

                    Assert.IsFalse(flag.IsPvPEnabled);
                    Assert.IsTrue(data.IndexOf(data.OfType<UsableInfoPacket>().Single()) < data.IndexOf(flag));
                }

                Assert.AreEqual(0, Drain(onlooker).OfType<PvPEnabledPacket>().Count(), "with the entity, and not again");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, point);
            }
        }

        [TestMethod]
        public void AnyOtherObjectIsNot()
        {
            using var world = new WorldTestContext();
            using var classes = new Classes(Class(OtherPoint, AugmentationType.ControlPoint));

            var point = Place(world, OtherPoint);

            try
            {
                var data = Drain(Arrive(world, 12, 12)).OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == point.EntityId).EntityData;

                Assert.AreEqual(1, data.OfType<UsableInfoPacket>().Count());
                Assert.AreEqual(0, data.OfType<PvPEnabledPacket>().Count());
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, point);
            }
        }

        #region Fixture

        private static EntityClass Class(EntityClasses id, params AugmentationType[] augmentations)
        {
            return new EntityClass((uint)id, "fixture", 0, 0, augmentations.ToList(), true);
        }

        private static DynamicObject Place(WorldTestContext world, EntityClasses classId)
        {
            var point = new DynamicObject
            {
                Position = new Vector3(10, 0, 10),
                MapContextId = world.Map.MapInfo.MapContextId,
                EntityClassId = classId
            };

            CellManager.Instance.AddToWorld(world.Map, point);
            point.IsInWorld = true;

            return point;
        }

        private static Client Arrive(WorldTestContext world, float x, float z)
        {
            var client = world.CreateClient(x, z);

            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(client);

            return client;
        }

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        /// <summary>Entity classes put in the loaded table for a test, and what was there put back.</summary>
        private sealed class Classes : System.IDisposable
        {
            private readonly Dictionary<EntityClasses, EntityClass> _previous = new();

            public Classes(params EntityClass[] classes)
            {
                var loaded = EntityClassManager.Instance.LoadedEntityClasses;

                foreach (var entityClass in classes)
                {
                    var id = (EntityClasses)entityClass.ClassId;

                    _previous[id] = loaded.TryGetValue(id, out var was) ? was : null;
                    loaded[id] = entityClass;
                }
            }

            public void Dispose()
            {
                var loaded = EntityClassManager.Instance.LoadedEntityClasses;

                foreach (var (id, was) in _previous)
                {
                    if (was == null)
                        loaded.Remove(id);
                    else
                        loaded[id] = was;
                }
            }
        }

        #endregion
    }
}
