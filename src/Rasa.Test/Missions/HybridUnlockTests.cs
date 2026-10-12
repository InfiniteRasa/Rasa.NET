using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures.Char;

    /// <summary>
    /// Hybrid races unlocked by their missions (HybridUnlocks): 1861 the Forean, 1851 the Brann,
    /// 1899 the Thrax; offered to everyone while GameDataConfig.AlwaysUnlockHybrids is on.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class HybridUnlockTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            HybridUnlocks.Load(null);
            CharacterManager.LoadEnabledRaces(null);
        }

        [TestMethod]
        public void EveryoneIsOfferedEveryRaceUntilTheSettingIsTurnedOff()
        {
            var account = new GameAccountEntry { Id = 1 };

            HybridUnlocks.Load(null);
            Assert.IsTrue(HybridUnlocks.AlwaysUnlocked, "on unless set off");
            CollectionAssert.AreEqual(new[] { Race.Human, Race.Forean, Race.Brann, Race.Thrax }, HybridUnlocks.OfferedTo(account).ToArray());

            HybridUnlocks.Load(false);
            CollectionAssert.AreEqual(new[] { Race.Human }, HybridUnlocks.OfferedTo(account).ToArray());
            Assert.IsFalse(HybridUnlocks.IsOfferedTo(account, Race.Brann));

            account.HybridUnlocks = (byte)(HybridUnlocks.Bit(Race.Brann) | HybridUnlocks.Bit(Race.Thrax));
            CollectionAssert.AreEqual(new[] { Race.Human, Race.Brann, Race.Thrax }, HybridUnlocks.OfferedTo(account).ToArray());
            Assert.IsTrue(HybridUnlocks.IsOfferedTo(account, Race.Brann));

            // EnabledRaces is the ceiling either way: an unlocked race the server does not offer
            // is not offered.
            CharacterManager.LoadEnabledRaces(new[] { 1, 2, 4 });
            CollectionAssert.AreEqual(new[] { Race.Human, Race.Thrax }, HybridUnlocks.OfferedTo(account).ToArray());

            HybridUnlocks.Load(true);
            CollectionAssert.AreEqual(new[] { Race.Human, Race.Forean, Race.Thrax }, HybridUnlocks.OfferedTo(account).ToArray());
            Assert.IsFalse(HybridUnlocks.IsOfferedTo(account, Race.Brann));

            Assert.AreEqual(1, HybridUnlocks.Bit(Race.Forean));
            Assert.AreEqual(2, HybridUnlocks.Bit(Race.Brann));
            Assert.AreEqual(4, HybridUnlocks.Bit(Race.Thrax));
            Assert.AreEqual(0, HybridUnlocks.Bit(Race.Human));
        }

        [TestMethod]
        [DataRow(1861u, Race.Forean, PlayerMessage.PmHybridForeanOpen)]
        [DataRow(1851u, Race.Brann, PlayerMessage.PmHybridBrannOpen)]
        [DataRow(1899u, Race.Thrax, PlayerMessage.PmHybridBaneOpen)]
        public void TurningInTheMissionUnlocksItsHybridForTheAccountAndSaysSo(uint missionId, Race race, PlayerMessage message)
        {
            HybridUnlocks.Load(false);
            using var context = MissionTestContext.WithCompletableMission(missionId);

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, missionId, 0, null));

            Assert.AreEqual(HybridUnlocks.Bit(race), Stored(context));
            Assert.AreEqual(HybridUnlocks.Bit(race), context.Client.AccountEntry.HybridUnlocks);
            Assert.IsTrue(HybridUnlocks.IsOfferedTo(context.Client.AccountEntry, race));

            var packets = context.Drain();
            var told = packets.OfType<DisplayClientMessagePacket>().Where(packet => packet.MsgId == message).ToList();
            Assert.HasCount(1, told);
            Assert.IsTrue(packets.IndexOf(told[0]) > packets.FindIndex(packet => packet is MissionRewardedPacket), "after the reward");
        }

        [TestMethod]
        public void WithEveryRaceOfferedTheUnlockIsKeptAndNothingIsSaid()
        {
            using var context = MissionTestContext.WithCompletableMission(1861);

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 1861, 0, null));

            Assert.AreEqual(HybridUnlocks.Bit(Race.Forean), Stored(context), "kept for when the setting is turned off");
            Assert.IsFalse(context.Drain().OfType<DisplayClientMessagePacket>().Any(packet => packet.MsgId == PlayerMessage.PmHybridForeanOpen));
        }

        [TestMethod]
        public void AnotherMissionUnlocksNothingAndAnUnlockIsOnlyToldOnce()
        {
            HybridUnlocks.Load(false);

            using (var other = MissionTestContext.WithCompletableMission(429))
            {
                Assert.IsTrue(other.Manager.CompleteOfferedMission(other.Client, other.Receiver.EntityId, 429, 0, null));
                Assert.AreEqual(0, Stored(other));
                Assert.IsFalse(other.Drain().OfType<DisplayClientMessagePacket>().Any());
            }

            // A second character of an account that has the Thrax already.
            using var context = MissionTestContext.WithCompletableMission(1899);
            using (var db = context.Open())
            {
                db.GameAccountEntries.Find(context.Client.AccountEntry.Id).HybridUnlocks = HybridUnlocks.Bit(Race.Thrax);
                db.SaveChanges();
            }

            context.Client.AccountEntry.HybridUnlocks = HybridUnlocks.Bit(Race.Thrax);

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 1899, 0, null));
            Assert.AreEqual(HybridUnlocks.Bit(Race.Thrax), Stored(context));
            Assert.IsFalse(context.Drain().OfType<DisplayClientMessagePacket>().Any(packet => packet.MsgId == PlayerMessage.PmHybridBaneOpen));
        }

        private static byte Stored(MissionTestContext context)
        {
            using var db = context.Open();
            return db.GameAccountEntries.Find(context.Client.AccountEntry.Id).HybridUnlocks;
        }
    }
}
