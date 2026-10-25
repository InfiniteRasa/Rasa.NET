using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Structures.Char;

    /// <summary>
    /// <see cref="ChatMarkup"/>: the client shows chat as rich text, so a player's markup is
    /// escaped and a GM's lines carry a colored tag.
    /// </summary>
    [TestClass]
    public class ChatMarkupTests
    {
        [TestMethod]
        public void APlayersMarkupIsBrokenUpButStillReads()
        {
            Assert.AreEqual("< font color=\"Chat_Yellow\">[GM]< /font> hi",
                ChatMarkup.Neutralize("<font color=\"Chat_Yellow\">[GM]</font> hi"));
            Assert.AreEqual("fish & chips <3 and a < b", ChatMarkup.Neutralize("fish & chips <3 and a < b"));
            Assert.AreEqual("< a action=\"x\">", ChatMarkup.Neutralize("<a action=\"x\">"));
            Assert.IsNull(ChatMarkup.Neutralize(null));
        }

        [TestMethod]
        [DataRow((byte)0, false)]
        [DataRow((byte)GmLevel.Observer, false)]
        [DataRow((byte)GmLevel.GameMaster, true)]
        [DataRow((byte)GmLevel.Admin, true)]
        [DataRow((byte)50, true)]
        public void AGmsLineIsTaggedPlainWhereThereIsABubbleAndYellowWhereNot(byte level, bool gm)
        {
            var client = ClientAt(level);

            Assert.AreEqual(gm ? ChatMarkup.GmOpen + "< b>hi" + ChatMarkup.GmClose : "< b>hi", ChatMarkup.Outgoing(client, "<b>hi"));
            Assert.AreEqual(gm ? ChatMarkup.PlainGmTag + "< b>hi" : "< b>hi", ChatMarkup.OutgoingWithBubble(client, "<b>hi"));
        }

        private static Client ClientAt(byte level)
        {
            var client = new Client(null, new ClientPacketHandler());
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client, new GameAccountEntry { Level = level });
            return client;
        }
    }
}
