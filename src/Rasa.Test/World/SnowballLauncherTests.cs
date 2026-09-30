using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    // The Snowball Launcher (template 131482, class 30548) fires TOOL_NERFWEAPON (527/1) through
    // RequestToolAction. It has no itemtemplate_weapon row and no ammo class, and its snowball
    // does nothing to what it hits: the throw and the splat are the whole of it.
    [TestClass]
    [DoNotParallelize]
    public class SnowballLauncherTests
    {
        private const EntityClasses SnowballLauncherClass = (EntityClasses)30548;

        [TestMethod]
        public void SnowballThrownAtAPlayerIsResolvedAtThemAndChangesNothing()
        {
            using var world = new WorldTestContext();
            var thrower = Arm(world, world.CreateClient());
            var target = world.CreateClient(10, 0);
            target.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 80, 100, 100, 0, 0);
            CellManager.Instance.AddToWorld(thrower);
            CellManager.Instance.AddToWorld(target);
            var health = target.Player.Attributes[Attributes.Health].Current;
            WorldTestContext.Drain(thrower);
            WorldTestContext.Drain(target);

            var recovery = Throw(world, thrower, target.Player.EntityId);

            Assert.AreEqual(1, recovery.Hits.Count);
            Assert.AreEqual(target.Player.EntityId, recovery.Hits[0].EntityId);
            Assert.AreEqual(health, target.Player.Attributes[Attributes.Health].Current);
            Assert.IsTrue(MissionTestContext.Drain(target).OfType<ToolActionRecoveryPacket>()
                .Any(packet => packet.ActionId == ActionId.ToolNerfweapon));
        }

        [TestMethod]
        public void SnowballThrownAtNothingLandsWhereAimedNotOnTheThrower()
        {
            using var world = new WorldTestContext();
            var thrower = Arm(world, world.CreateClient());
            var onlooker = world.CreateClient(5, 0);
            CellManager.Instance.AddToWorld(thrower);
            CellManager.Instance.AddToWorld(onlooker);
            WorldTestContext.Drain(onlooker);

            var recovery = Throw(world, thrower, null);

            Assert.AreEqual(0, recovery.Hits.Count, "A blind shot has no hits; the client splats it where it was aimed.");
            var windup = MissionTestContext.Drain(onlooker).OfType<PerformWindupPacket>()
                .Single(packet => packet.ActionId == ActionId.ToolNerfweapon);
            Assert.AreEqual(0UL, windup.Arg);
        }

        [TestMethod]
        public void SnowballBeyondFortyMetresIsRefused()
        {
            using var world = new WorldTestContext();
            var thrower = Arm(world, world.CreateClient());
            var target = world.CreateClient(60, 0);
            CellManager.Instance.AddToWorld(thrower);
            CellManager.Instance.AddToWorld(target);

            ToolActionManager.Instance.RequestToolAction(thrower, Request(target.Player.EntityId));

            Assert.AreEqual(0, world.Map.PerformRecovery.Count);
        }

        private static Client Arm(WorldTestContext world, Client client)
        {
            world.AddClass(SnowballLauncherClass);
            EntityClassManager.Instance.LoadedEntityClasses[SnowballLauncherClass].WeaponClassInfo =
                new WeaponClassInfo(new WeaponClassEntry
                {
                    Id = 30548,
                    AttackActionId = (uint)ActionId.ToolNerfweapon,
                    AttackActionArgId = 1,
                    AmmoClassId = 0,
                    ClipSize = 8
                });
            var launcher = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                {
                    ItemTemplateId = 131482,
                    ItemClass = (uint)SnowballLauncherClass
                })
            };
            Assert.IsNull(launcher.ItemTemplate.WeaponInfo);
            EntityManager.Instance.RegisterEntity(launcher.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(launcher.EntityId, launcher);
            client.Player.Inventory.EquippedInventory[13] = launcher.EntityId;
            client.Player.WeaponReady = true;
            return client;
        }

        private static ToolActionRecoveryPacket Throw(WorldTestContext world, Client thrower, ulong? targetId)
        {
            ToolActionManager.Instance.RequestToolAction(thrower, Request(targetId));

            var action = world.Map.PerformRecovery.Single();
            Assert.AreEqual(ActionId.ToolNerfweapon, action.ActionId);
            Assert.AreEqual(targetId ?? 0, action.TargetId);
            world.Map.PerformRecovery.Clear();
            WorldTestContext.Drain(thrower);

            lock (Server.Clients)
                Server.Clients.Add(thrower);
            try
            {
                ToolActionManager.Instance.PerformRecovery(world.Map, action);
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(thrower);
            }

            return MissionTestContext.Drain(thrower).OfType<ToolActionRecoveryPacket>().Single();
        }

        private static RequestToolActionPacket Request(ulong? targetId)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
            {
                writer.WriteTuple(3);
                writer.WriteInt((int)ActionId.ToolNerfweapon);
                writer.WriteUInt(1);
                if (targetId.HasValue)
                    writer.WriteULong(targetId.Value);
                else
                    writer.WriteNoneStruct();
            }

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestToolActionPacket();
            packet.Read(reader);
            return packet;
        }
    }
}
