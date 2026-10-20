using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;

    // A player message the client presents itself - the avatar's voice line, the banner across
    // the screen, the error sound - only does so when it comes as DisplaySystemMessage;
    // DisplayClientMessage is a chat line and nothing more (CommunicatorManager.ShowPlayerMessage).
    [TestClass]
    [DoNotParallelize]
    public class PlayerMessageChannelTests
    {
        /// <summary>
        /// The ids of manifestation.py kPlayerMessageVOs and gameui.py
        /// kPlayerMessageDisplayBigText, by number as generated/client/playermessage has them.
        /// </summary>
        private static readonly uint[] ClientIds =
        {
            // kPlayerMessageVOs
            26, 456, 292, 601, 891, 591, 590, 607, 395, 274, 673, 916, 931, 471, 472, 998, 88, 122, 478, 14, 131, 124,
            // kPlayerMessageDisplayBigText (673, 26, 456, 601, 274, 292 are above)
            95, 460, 291, 461, 1024, 20000001, 831, 1445, 1444, 1474, 1473, 1476, 1475
        };

        [TestMethod]
        public void TheIdsAreTheClientsOwn()
        {
            CollectionAssert.AreEquivalent(ClientIds, CommunicatorManager.ClientPresented.Select(id => (uint)id).ToArray());
        }

        [TestMethod]
        [DataRow(PlayerMessage.PmInventoryFull)]
        [DataRow(PlayerMessage.PmYourFootlockerIsFull)]
        [DataRow(PlayerMessage.PmTradeNotEnoughRoom)]
        [DataRow(PlayerMessage.PmTargetOutOfRange)]
        [DataRow(PlayerMessage.PmBattlegroundBegins)]
        public void OneTheClientPresentsGoesAsASystemMessage(PlayerMessage message)
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);

            CommunicatorManager.ShowPlayerMessage(client, message, new Dictionary<string, string> { ["x"] = "1" });

            var calls = Calls(client);
            var shown = (DisplaySystemMessagePacket)calls.Single().Packet;
            Assert.AreEqual((ulong)SysEntity.ClientMethodId, calls.Single().EntityId);
            Assert.AreEqual(message, shown.MsgId);
            Assert.AreEqual("1", shown.Args["x"]);
        }

        [TestMethod]
        public void AnyOtherIsAChatLine()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);

            CommunicatorManager.ShowPlayerMessage(client, PlayerMessage.PmTradeCancelled);

            var calls = Calls(client);
            Assert.AreEqual((ulong)SysEntity.CommunicatorId, calls.Single().EntityId);
            Assert.AreEqual(PlayerMessage.PmTradeCancelled, ((DisplayClientMessagePacket)calls.Single().Packet).MsgId);
        }

        private static List<CallMethodMessage> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();
    }
}
