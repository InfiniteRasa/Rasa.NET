using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Minion.Client;
    using Rasa.Packets.Minion.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    // MinionGo: the minion is sent to a point the client picked, within MaxGoDistance of its
    // master, and anchored there. A point the client cannot have picked - NaN, an infinity - is
    // refused as out of reach, and the minion keeps what it was doing.
    [TestClass]
    [DoNotParallelize]
    public class MinionGoTests
    {
        [TestMethod]
        public void APointInReachIsTheMinionsAnchor()
        {
            using var world = new WorldTestContext();
            var master = Master(world);
            var bot = Summon(world.Map, master);

            try
            {
                MinionManager.Instance.Go(master, new MinionGoPacket { Destination = new Vector3(10, 0, 5) });

                Assert.IsTrue(bot.Controller.ActionFollow.HasAnchor);
                Assert.AreEqual(new Vector3(10, 0, 5), bot.Controller.ActionFollow.Anchor);
                Assert.IsTrue(Ack(master).Success);
            }
            finally
            {
                Dismiss(master, bot);
            }
        }

        [TestMethod]
        [DataRow(float.NaN, 0f, 0f)]
        [DataRow(0f, float.NaN, 0f)]
        [DataRow(0f, 0f, float.PositiveInfinity)]
        [DataRow(float.NegativeInfinity, 0f, 0f)]
        public void APointThatIsNoPointIsRefusedAndTheMinionKeepsItsAnchor(float x, float y, float z)
        {
            using var world = new WorldTestContext();
            var master = Master(world);
            var bot = Summon(world.Map, master);

            try
            {
                MinionManager.Instance.Go(master, new MinionGoPacket { Destination = new Vector3(10, 0, 5) });
                WorldTestContext.Drain(master);

                MinionManager.Instance.Go(master, new MinionGoPacket { Destination = new Vector3(x, y, z) });

                Assert.AreEqual(new Vector3(10, 0, 5), bot.Controller.ActionFollow.Anchor, "kept");
                var ack = Ack(master);
                Assert.IsFalse(ack.Success);
                Assert.AreEqual(PlayerMessage.PmMinionTargetTooFarAway, ack.MessageId);
            }
            finally
            {
                Dismiss(master, bot);
            }
        }

        private static MinionAckPacket Ack(Client master) =>
            WorldTestContext.Drain(master).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<MinionAckPacket>().Single();

        private static Client Master(WorldTestContext world)
        {
            var master = world.CreateClient();
            master.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            return master;
        }

        private static Creature Summon(MapChannel mapChannel, Client master)
        {
            var bot = new Creature
            {
                Name = "Spotter",
                TargetCategory = TargetCategory.Friendly,
                MapContextId = mapChannel.MapInfo.MapContextId,
                RuntimeMapChannel = mapChannel,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            bot.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);

            CellManager.Instance.AddToWorld(mapChannel, bot);
            MinionManager.Instance.Adopt(master, bot);
            WorldTestContext.Drain(master);
            return bot;
        }

        private static void Dismiss(Client master, Creature bot)
        {
            MinionManager.Instance.DismissAll(master);
            EntityManager.Instance.UnregisterEntity(bot.EntityId);
            EntityManager.Instance.UnregisterCreature(bot.EntityId);
            EntityManager.Instance.FreeEntity(bot.EntityId);
        }
    }
}
