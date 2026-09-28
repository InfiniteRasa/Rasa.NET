using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class PrestigeDeltaTests
    {
        [TestMethod]
        [DataRow(25)]
        [DataRow(-40)]
        [DataRow(0)]
        public void UpdateCreditsCarriesItsDelta(int delta)
        {
            using var reader = new PythonReader(new BinaryReader(new MemoryStream(
                MissionTestContext.Encode(new UpdateCreditsPacket(CurencyType.Prestige, 125, delta)))));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual((int)CurencyType.Prestige, reader.ReadInt());
            Assert.AreEqual(125, reader.ReadInt());
            Assert.AreEqual(delta, reader.ReadInt());
        }

        [TestMethod]
        public void GainingPrestigeSendsWhatWasGained()
        {
            using var context = new WeaponAmmoContext();
            var manager = new CharacterManager(context);
            context.Client.Player.Credits[CurencyType.Prestige] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Prestige = 100;
                database.SaveChanges();
            }

            Assert.IsTrue(manager.UpdateCharacter(context.Client, CharacterUpdate.Prestige, 25));

            var update = Sent(context).Single();
            Assert.AreEqual(CurencyType.Prestige, update.Type);
            Assert.AreEqual(125, update.Amount);
            Assert.AreEqual(25, update.Delta);
        }

        [TestMethod]
        public void SpendingCreditsSendsANegativeDelta()
        {
            using var context = new WeaponAmmoContext();
            var manager = new CharacterManager(context);
            context.Client.Player.Credits[CurencyType.Credits] = 100;
            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Credit = 100;
                database.SaveChanges();
            }

            Assert.IsTrue(manager.UpdateCharacter(context.Client, CharacterUpdate.Credits, -30));

            var update = Sent(context).Single();
            Assert.AreEqual(70, update.Amount);
            Assert.AreEqual(-30, update.Delta);
        }

        [TestMethod]
        public void AMissionRewardFloatsThePrestigeItGave()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var prestige = context.Reward.Currencies[CurencyType.Prestige];
            Assert.IsTrue(prestige > 0, "the fixture's reward has to carry prestige");
            var before = context.Client.Player.Credits[CurencyType.Prestige];

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0, null));

            var update = context.Drain().OfType<UpdateCreditsPacket>().Single(p => p.Type == CurencyType.Prestige);
            Assert.AreEqual(before + prestige, update.Amount);
            Assert.AreEqual(prestige, update.Delta);
        }

        private static UpdateCreditsPacket[] Sent(WeaponAmmoContext context) =>
            WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .OfType<UpdateCreditsPacket>()
                .ToArray();
    }
}
