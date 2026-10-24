extern alias RasaGame;

using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// The AFS turrets' bursts (Emplacements.Cooldown): 3 or 5 rounds a think apart, then a pause that
    /// keeps the rounds a minute they had; and a turret staying turned to its target between shots.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TurretBurstTests
    {
        private static readonly int[] Sizes = { 3, 5 };

        [TestMethod]
        public void ABurstOfThreeIsThreeRoundsAThinkApartAndThenTheRestOfTheirTime()
        {
            var burst = new Emplacements.Burst();

            Assert.AreEqual(250L, Emplacements.BurstCooldown(burst, 1000, 0.2, Sizes, 250));
            Assert.AreEqual(3, burst.Size);
            Assert.AreEqual(250L, Emplacements.BurstCooldown(burst, 1000, 0.9, Sizes, 250), "the size was picked at its start");
            Assert.AreEqual(2500L, Emplacements.BurstCooldown(burst, 1000, 0.9, Sizes, 250));

            // Three rounds in 3000 ms: as many as one a second.
            Assert.AreEqual(0, burst.Left);
        }

        [TestMethod]
        public void ABurstOfFiveKeepsTheRoundsAMinuteToo()
        {
            var burst = new Emplacements.Burst();
            var total = 0L;

            for (var round = 0; round < 5; round++)
                total += Emplacements.BurstCooldown(burst, 1200, 0.7, Sizes, 250);

            Assert.AreEqual(5, burst.Size);
            Assert.AreEqual(5 * 1200L, total);

            // The next burst is rolled anew.
            Emplacements.BurstCooldown(burst, 1200, 0.1, Sizes, 250);
            Assert.AreEqual(3, burst.Size);
        }

        [TestMethod]
        public void OneRoundBurstsOrNoSizesFireSingly()
        {
            Assert.AreEqual(1000L, Emplacements.BurstCooldown(new Emplacements.Burst(), 1000, 0.5, new[] { 1 }, 250));
            Assert.AreEqual(1000L, Emplacements.BurstCooldown(new Emplacements.Burst(), 1000, 0.5, Array.Empty<int>(), 250));
        }

        [TestMethod]
        public void OnlyTheAfsTurretsFireBurstsAndOnlyWithTheRuleOn()
        {
            var config = ControlPointFields.Config;

            try
            {
                ControlPointFields.Config = new ControlPointDefenceConfig();

                var afs = new Creature { EntityClass = (EntityClasses)11302 };
                var bane = new Creature { EntityClass = (EntityClasses)7482 };
                var soldier = new Creature { EntityClass = EntityClasses.HumanBaseMale };

                Assert.AreEqual(250L, Emplacements.Cooldown(afs, 1000), "the first round of a burst");
                Assert.AreEqual(1000L, Emplacements.Cooldown(bane, 1000));
                Assert.AreEqual(1000L, Emplacements.Cooldown(soldier, 1000));

                ControlPointFields.Config = new ControlPointDefenceConfig { TurretBursts = false };
                Assert.AreEqual(1000L, Emplacements.Cooldown(new Creature { EntityClass = (EntityClasses)4064 }, 1000));
            }
            finally
            {
                ControlPointFields.Config = config;
            }
        }

        [TestMethod]
        public void TheShippedSettingsAreTheDefaultsAndTheBurstSizesAreNotDoubled()
        {
            var config = new RasaGame::Rasa.Config.Config();

            new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), false, false).Build().Bind(config);

            var shipped = config.ControlPointDefence;
            var defaults = new ControlPointDefenceConfig();

            // The binder adds a file's list to a default one: the default is none, and the file's is all there is.
            CollectionAssert.AreEqual(new[] { 3, 5 }, shipped.BurstSizes);
            Assert.IsNull(defaults.BurstSizes);
            CollectionAssert.AreEqual(new[] { 3, 5 }, Emplacements.DefaultBurstSizes);

            Assert.AreEqual((defaults.FieldsFollowOwner, defaults.FieldRebuildSeconds, defaults.FieldRechargeAfterSeconds, defaults.FieldRechargeFullSeconds,
                    defaults.CreaturesStrikeFields, defaults.FieldAlertRange, defaults.Defenders, defaults.TurretBursts, defaults.BurstGapMs),
                (shipped.FieldsFollowOwner, shipped.FieldRebuildSeconds, shipped.FieldRechargeAfterSeconds, shipped.FieldRechargeFullSeconds,
                    shipped.CreaturesStrikeFields, shipped.FieldAlertRange, shipped.Defenders, shipped.TurretBursts, shipped.BurstGapMs));
            Assert.AreEqual((defaults.DefenderNoticeRange, defaults.DefenderNoticeMargin, defaults.DefenderNoticeMax, defaults.TurretNoticeMax, defaults.CallRange, defaults.HoldPostMargin),
                (shipped.DefenderNoticeRange, shipped.DefenderNoticeMargin, shipped.DefenderNoticeMax, shipped.TurretNoticeMax, shipped.CallRange, shipped.HoldPostMargin));
        }

        [TestMethod]
        public void AGunBetweenItsShotsIsTurnedToItsTargetAsItMoves()
        {
            var gun = new Creature { EntityClass = (EntityClasses)4064, LastYaw = 0f };

            // Facing (difX, difZ) is atan2(-x, -z): straight down -Z is 0.
            Assert.IsFalse(Emplacements.KeepsFacing(gun, 0, -10), "already facing it");
            Assert.IsFalse(Emplacements.KeepsFacing(gun, 0.2, -10), "a turn too small to send");
            Assert.IsTrue(Emplacements.KeepsFacing(gun, 5, -10));
            Assert.IsFalse(Emplacements.KeepsFacing(new Creature { EntityClass = EntityClasses.HumanBaseMale }, 5, -10), "no gun");
        }
    }
}
