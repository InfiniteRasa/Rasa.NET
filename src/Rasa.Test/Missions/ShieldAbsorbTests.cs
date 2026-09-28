using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class ShieldAbsorbTests
    {
        private const int ShieldExtenderSource = 10000056;  // SHIELD_EXTENDER_SOURCE

        [TestMethod]
        public void AShieldTakesItsShareOfAbilityDamageAndSaysHowMuch()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var health = Prepare(player, 1000);
            var shield = Shield(harness, percent: 40, pool: 500);

            var taken = ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var outcome, DamageType.Laser);

            Assert.AreEqual(40, outcome.Absorbed);
            Assert.AreEqual(60, outcome.Delivered);
            Assert.IsFalse(outcome.Immune);
            Assert.AreEqual(60, taken);
            Assert.AreEqual(940, health.Current);
            Assert.AreEqual(460, shield.AbsorbPool.Remaining);
            Assert.IsTrue(player.ActiveEffects.ContainsKey(shield.EffectId));
        }

        [TestMethod]
        public void AHitTheShieldTakesAllOfLeavesHealthAloneAndAnEmptiedPoolEndsTheShield()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var health = Prepare(player, 1000);
            var shield = Shield(harness, percent: 100, pool: 100);

            Assert.AreEqual(0, ActorManager.Instance.Damage(harness.BootcampMap, player, 30, null, out var first));
            Assert.AreEqual(30, first.Absorbed);
            Assert.AreEqual(0, first.Delivered);
            Assert.AreEqual(1000, health.Current);

            var taken = ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var second);

            Assert.AreEqual(70, second.Absorbed, "the pool had 70 left");
            Assert.AreEqual(30, taken);
            Assert.AreEqual(970, health.Current);
            Assert.IsFalse(player.ActiveEffects.ContainsKey(shield.EffectId), "an emptied pool ends the shield");
        }

        [TestMethod]
        public void AShieldDoesNotSoftenAFall()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var health = Prepare(player, 1000);
            var shield = Shield(harness, percent: 100, pool: 500);
            const float drop = 16f;     // 6 m past the safe drop: 30% of maximum health
            var expected = FallDamage.DamageFor(drop, health.CurrentMax);
            Assert.IsTrue(expected > 0, "the fixture's drop has to hurt");

            var taken = FallDamage.Apply(harness.BootcampMap, player, drop);

            Assert.AreEqual(expected, taken);
            Assert.AreEqual(1000 - expected, health.Current);
            Assert.AreEqual(500, shield.AbsorbPool.Remaining);
            Assert.IsTrue(player.ActiveEffects.ContainsKey(shield.EffectId));
        }

        [TestMethod]
        public void RawInfoCarriesTheAbsorbedShareBesideWhatGotThrough()
        {
            var packet = new GameEffectAnnounceDamagePacket(7);
            packet.Hits.Add(new TickEntry { EntityId = 5, Amount = 60, Absorbed = 40, Resisted = 3, DamageType = DamageType.Laser });

            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet))));
            Assert.AreEqual(3, reader.ReadTuple());
            reader.SkipValue();                 // effectId
            reader.SkipValue();                 // method name
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            reader.SkipValue();                 // entityId
            Assert.AreEqual(12, reader.ReadTuple());
            reader.SkipValue();                 // damageType
            reader.SkipValue();                 // reflected
            reader.SkipValue();                 // filtered
            Assert.AreEqual(40U, reader.ReadUInt());
            Assert.AreEqual(3U, reader.ReadUInt());
            Assert.AreEqual(60L, reader.ReadLong());
        }

        private static ActorAttributes Prepare(Manifestation player, int health)
        {
            var bar = player.Attributes[Attributes.Health];
            bar.CurrentMax = health;
            bar.Current = health;
            player.Attributes[Attributes.Armor].Current = 0;
            return bar;
        }

        private static GameEffect Shield(BootcampRuntimeTestHarness.Harness harness, int percent, int pool)
        {
            var shield = new GameEffect
            {
                EffectId = 880001,
                TypeId = ShieldExtenderSource,
                IsBuff = true,
                AllowDetach = true,
                AbsorbPercent = percent,
                AbsorbPool = new AbsorbPool { Remaining = pool }
            };

            GameEffectManager.Instance.Attach(harness.BootcampMap, harness.Client.Player, shield);
            Assert.IsTrue(harness.Client.Player.ActiveEffects.ContainsKey(shield.EffectId));
            return shield;
        }
    }
}
