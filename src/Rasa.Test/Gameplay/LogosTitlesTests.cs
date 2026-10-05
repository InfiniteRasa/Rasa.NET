using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// "Wilderness Mentalist: Discovered all Logos in Wilderness." A character who holds every
    /// Logos of a battlefield has its title; no mission is asked.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LogosTitlesTests
    {
        private const uint WildernessMentalist = 370;
        private const uint DesertMentalist = 430;
        private const uint ThunderheadHistorian = 788;
        private const uint DivideMentalist = 466;

        private const uint Earth = 408;             // on Wilderness, and not one of its twelve
        private const uint Future = 58;             // Guardian Prominence, a Wilderness instance
        private const uint Bomb = 44;               // Quasso Station

        private static readonly uint[] Wilderness = { 1, 2, 6, 9, 10, 23, 24, 28, 38, 49, 53, 56 };
        private static readonly uint[] AshenDesert = { 73, 77, 197, 217, 256 };
        private static readonly uint[] Thunderhead = { 19, 190, 193, 240, 265, 273, 326, 354 };

        [TestCleanup]
        public void ForgetTheShrines() => LogosTitles.Load(null);

        [TestMethod]
        public void ATitleAsksForTheLogosOfItsBattlefieldsOwnMap()
        {
            using var harness = Start();

            // The twelve the recovered Wilderness objective lists (MissionDefinitionCatalog, 1449/8).
            CollectionAssert.AreEquivalent(Wilderness, LogosTitles.LogosOf(WildernessMentalist).ToArray());
            CollectionAssert.AreEquivalent(AshenDesert, LogosTitles.LogosOf(DesertMentalist).ToArray());
            CollectionAssert.AreEquivalent(Thunderhead.Append(Bomb).ToArray(), LogosTitles.LogosOf(ThunderheadHistorian).ToArray(),
                "\"on Thunderhead and Quasso Station\"");

            var counts = LogosTitles.Zones.ToDictionary(zone => zone.Name, zone => LogosTitles.LogosOf(zone.TitleId).Count);
            CollectionAssert.AreEquivalent(
                new Dictionary<string, int>
                {
                    ["Wilderness"] = 12, ["Divide"] = 17, ["Palisades"] = 11, ["Plateau"] = 10, ["Pools"] = 10,
                    ["Mires"] = 10, ["Ashen Desert"] = 5, ["Thunderhead"] = 9, ["Abyss"] = 6, ["Crucible"] = 10
                }.ToArray(),
                counts.ToArray());

            // No instance's, and nobody else's.
            Assert.IsFalse(LogosTitles.LogosOf(WildernessMentalist).Contains(Future));
            Assert.IsFalse(LogosTitles.LogosOf(WildernessMentalist).Contains(Earth));
            Assert.IsFalse(LogosTitles.LogosOf(DivideMentalist).Contains(321u), "Those, in Minos Caverns");

            var all = LogosTitles.Zones.SelectMany(zone => LogosTitles.LogosOf(zone.TitleId)).ToArray();
            Assert.HasCount(all.Length, all.Distinct().ToArray(), "a Logos is one battlefield's");

            Assert.HasCount(10, LogosTitles.Zones.Select(zone => zone.TitleId).Distinct().ToArray());
            Assert.IsEmpty(LogosTitles.LogosOf(365).ToArray(), "Wilderness Bug Zapper is no Logos title");
        }

        [TestMethod]
        public void TheLastLogosOfABattlefieldGivesItsTitle()
        {
            using var harness = Start();

            foreach (var logosId in AshenDesert.Take(4))
                Assert.IsTrue(Learn(harness, logosId));

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "four of five");
            Assert.IsFalse(harness.Client.Player.Titles.Contains(DesertMentalist));

            Assert.IsTrue(Learn(harness, AshenDesert[4]));

            var packets = harness.Drain();
            Assert.AreEqual(DesertMentalist, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<LogosStoneAddedPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "the Logos, then what it completed");
            Assert.IsTrue(harness.Client.Player.Titles.Contains(DesertMentalist));

            using (var unit = harness.Context.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(harness.Client.Player.Id), DesertMentalist);

            // Another battlefield's Logos gives nothing more, and nothing twice.
            Assert.IsTrue(Learn(harness, Thunderhead[0]));
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void WildernessIsItsTwelveWithoutEarthOrTheInstances()
        {
            using var harness = Start();

            // Power is every character's from the start.
            Assert.IsTrue(harness.Client.Player.Logos.Contains(AbilityLogos.Power));

            // Earth and an instance's are neither needed nor enough.
            foreach (var logosId in Wilderness.Where(id => id != AbilityLogos.Power && id != 56).Append(Earth).Append(Future))
                Assert.IsTrue(Learn(harness, logosId));

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "Mind is missing");

            Assert.IsTrue(Learn(harness, 56));

            Assert.AreEqual(WildernessMentalist, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
        }

        [TestMethod]
        public void ThunderheadAsksForQuassoStationsToo()
        {
            using var harness = Start();

            foreach (var logosId in Thunderhead)
                Assert.IsTrue(Learn(harness, logosId));

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());

            Assert.IsTrue(Learn(harness, Bomb));

            Assert.AreEqual(ThunderheadHistorian, harness.Drain().OfType<TitleAddedPacket>().Single().TitleId);
        }

        [TestMethod]
        public void ACharacterWhoHasThemAlreadyIsGivenTheTitleOnComingOntoAMap()
        {
            using var harness = Start();

            // Held from before - as a clone has its source's - with no title for them.
            using (var unit = harness.Context.CreateChar())
                foreach (var logosId in AshenDesert)
                    unit.CharacterLogoses.SetLogos(harness.Client.Player.Id, logosId);

            harness.Client.Player.Logos.AddRange(AshenDesert);
            harness.Drain();

            ManifestationManager.Instance.AssignPlayer(harness.Client);

            var packets = harness.Drain();
            Assert.AreEqual(DesertMentalist, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<TitlesPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "added to the list the client has been given");
            Assert.IsTrue(harness.Client.Player.Titles.Contains(DesertMentalist));

            // The next map: it is in the list, and not gained again.
            ManifestationManager.Instance.AssignPlayer(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void ALogosTakenAwayTakesNoTitleWithIt()
        {
            using var harness = Start();

            foreach (var logosId in AshenDesert)
                Assert.IsTrue(Learn(harness, logosId));

            Assert.AreEqual(1, new CharacterManager(harness.Context, harness.Manager).RemoveLogos(harness.Client, AshenDesert[0]));

            Assert.IsTrue(harness.Client.Player.Titles.Contains(DesertMentalist));

            // Learned again: the title is theirs already.
            harness.Drain();
            Assert.IsTrue(Learn(harness, AshenDesert[0]));
            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void WithNoShrinesKnownNothingIsGiven()
        {
            using var harness = Start();
            LogosTitles.Load(null);

            foreach (var logosId in AshenDesert)
                Assert.IsTrue(Learn(harness, logosId));

            LogosTitles.CatchUp(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<TitleAddedPacket>().ToArray(), "a battlefield with no shrine asks for nothing and gives nothing");
            Assert.IsEmpty(LogosTitles.LogosOf(DesertMentalist).ToArray());
        }

        /// <summary>The harness with the titles' Logos worked out from its world's shrines, as LogosManager.LogosInit does.</summary>
        private static BootcampRuntimeTestHarness.Harness Start()
        {
            var harness = BootcampRuntimeTestHarness.Create();

            LogosTitles.Load(harness.WorldContext.Set<LogosEntry>().AsNoTracking().ToList());
            harness.Drain();
            return harness;
        }

        /// <summary>A Logos learned, as a shrine, a Logos stone and .addlogos all teach one.</summary>
        private static bool Learn(BootcampRuntimeTestHarness.Harness harness, uint logosId) =>
            new CharacterManager(harness.Context, harness.Manager).UpdateCharacter(harness.Client, CharacterUpdate.Logos, logosId);

        private static int IndexOf<T>(IReadOnlyList<PythonPacket> packets) where T : PythonPacket
        {
            for (var i = 0; i < packets.Count; i++)
                if (packets[i] is T)
                    return i;

            return -1;
        }
    }
}
