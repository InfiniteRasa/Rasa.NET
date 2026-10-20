using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Missions;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// A squad's group bonus (KillShares.GroupPartOf, GroupModOf), by the client's rule of 8% a
    /// member (XP_MOD_PER_PARTY_MEMBER), and the multipliers on an experience line as the client
    /// prints them (XPInfo; manifestation.py Recv_ExperienceChanged).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GroupExperienceBonusTests
    {
        [TestMethod]
        [DataRow(1, 100, 1.00, 0)]
        [DataRow(2, 92, 1.84, 84)]
        [DataRow(3, 84, 2.52, 152)]
        [DataRow(4, 76, 3.04, 204)]
        [DataRow(5, 68, 3.40, 240)]
        [DataRow(6, 60, 3.60, 260)]
        public void EachSharerIsPaidEightPercentLessForEveryOther(int sharers, int percent, double mod, int bonus)
        {
            Assert.AreEqual(percent, KillShares.GroupPercentOf(sharers));
            Assert.AreEqual(mod, KillShares.GroupModOf(sharers), 1e-9);
            Assert.AreEqual((uint)percent, KillShares.GroupPartOf(100, sharers));

            // The chat line's " (+N% Group Bonus)": int(groupMod * 100) - 100 of what it reads.
            Assert.AreEqual(bonus, (int)(PythonWriter.AsRead(XPInfo.ForTruncation(mod)) * 100) - 100);
        }

        [TestMethod]
        [DataRow(1000u, 2, 920u)]
        [DataRow(99u, 2, 91u)]
        [DataRow(1u, 6, 1u)]
        [DataRow(0u, 6, 0u)]
        [DataRow(uint.MaxValue, 1, uint.MaxValue)]
        [DataRow(100u, 0, 100u)]
        public void APartIsRoundedDownAndNeverNothing(uint experience, int sharers, uint part)
        {
            Assert.AreEqual(part, KillShares.GroupPartOf(experience, sharers));
        }

        [TestMethod]
        public void EveryMultiplierIsPrintedAsTheWholePercentItIs()
        {
            // Sent as they are, single-precision would print 2.52 as +151% and 1.1 as +11%.
            Assert.AreEqual(151, (int)(PythonWriter.AsRead(2.52) * 100) - 100);
            Assert.AreEqual(11, (int)Math.Ceiling(PythonWriter.AsRead(1.1) * 100) - 100);

            // Streaks: x2 to x10.
            for (var streak = 2; streak <= 10; streak++)
                Assert.AreEqual(streak * 100 - 100, (int)(PythonWriter.AsRead(XPInfo.ForTruncation(streak)) * 100) - 100, $"x{streak}");

            // Boosters, which the client rounds up: every whole percent to 2000.
            for (var percent = 1; percent <= 2000; percent++)
            {
                var read = PythonWriter.AsRead(XPInfo.ForCeiling((100.0 + percent) / 100));
                Assert.AreEqual(percent, (int)Math.Ceiling(read * 100) - 100, $"+{percent}%");
            }

            // Nothing over 1 is 1: the one byte, and nothing said.
            Assert.AreEqual(1, XPInfo.ForTruncation(1));
            Assert.AreEqual(1, XPInfo.ForCeiling(1));
            Assert.AreEqual(1, XPInfo.ForTruncation(1.004));
        }

        [TestMethod]
        public void ASquadsLineSaysItsBaseItsGroupBonusAndItsBooster()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));
            var boosted = context.Client;
            var mate = context.CreateAdditionalClient(2);
            var third = context.CreateAdditionalClient(3);
            var manifestations = new ManifestationManager(context);

            Assert.IsNotNull(ExperienceBoost.Put(context.Map, boosted.Player, 50, 1, 60_000, announce: false));
            MissionTestContext.Drain(boosted);

            Assert.AreEqual(84u, KillShares.AwardExperience(manifestations, new[] { boosted, mate, third }, boosted, 100));

            var holders = Line(boosted);
            Assert.AreEqual(126u, holders.Gained, "84, half again");
            Assert.AreEqual(33u, holders.BaseGained);
            Assert.AreEqual(2.52, holders.GroupMod, 1e-9);
            Assert.AreEqual(1.5, holders.BoosterMod, 1e-9);
            Assert.AreEqual(1, holders.StreakMod);

            foreach (var other in new[] { mate, third })
            {
                var line = Line(other);
                Assert.AreEqual(84u, line.Gained);
                Assert.AreEqual(33u, line.BaseGained);
                Assert.AreEqual(2.52, line.GroupMod, 1e-9);
                Assert.AreEqual(1, line.BoosterMod);
            }

            // Alone: the whole kill, its own base, and no group bonus to say.
            KillShares.AwardExperience(manifestations, new[] { mate }, mate, 100);
            var alone = Line(mate);
            Assert.AreEqual(100u, alone.Gained);
            Assert.AreEqual(100u, alone.BaseGained);
            Assert.AreEqual(1, alone.GroupMod);
        }

        [TestMethod]
        public void TheModsGoOutAsTheClientIsToReadThem()
        {
            var bytes = MissionTestContext.Encode(new ExperienceChangedPacket(new XPInfo(500, 184, 50) { GroupMod = 1.84, BoosterMod = 1.1 }));

            foreach (var mod in new[] { 1.845f, 1.095f })
            {
                var wanted = new byte[] { 0x3F }.Concat(BitConverter.GetBytes(mod)).ToArray();
                Assert.IsTrue(Enumerable.Range(0, bytes.Length - wanted.Length + 1).Any(i => bytes.Skip(i).Take(wanted.Length).SequenceEqual(wanted)), $"{mod}");
            }
        }

        private static XPInfo Line(Rasa.Game.Client client) =>
            MissionTestContext.Drain(client).OfType<ExperienceChangedPacket>().Single().XPInfo;
    }
}
