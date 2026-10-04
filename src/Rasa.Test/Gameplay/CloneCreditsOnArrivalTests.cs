using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Managers;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    /// <summary>
    /// The client's manifestation is made again on every map and holds no clone credit count
    /// until it is sent one: None, which its attributes window prints as it is and its trainer's
    /// Clone button takes for nothing to spend. So the count goes with the rest of the character
    /// on every arrival, a count of nought included.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CloneCreditsOnArrivalTests
    {
        [TestMethod]
        [DataRow(0U, DisplayName = "no clone credits")]
        [DataRow(2U, DisplayName = "two clone credits")]
        public void ArrivingOnAMapSendsTheCloneCreditsToTheManifestation(uint credits)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            player.CloneCredits = credits;
            harness.Drain();

            ManifestationManager.Instance.AssignPlayer(harness.Client);

            var sent = CloneCreditsSent(harness);
            Assert.AreEqual(player.EntityId, sent.EntityId);
            Assert.AreEqual(credits, ((CloneCreditsPacket)sent.Packet).CloneCredits);

            // The next map: the manifestation there is new as well.
            ManifestationManager.Instance.AssignPlayer(harness.Client);

            Assert.AreEqual(credits, ((CloneCreditsPacket)CloneCreditsSent(harness).Packet).CloneCredits);
        }

        private static CallMethodMessage CloneCreditsSent(BootcampRuntimeTestHarness.Harness harness) =>
            WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Single(message => message.Packet is CloneCreditsPacket);
    }
}
