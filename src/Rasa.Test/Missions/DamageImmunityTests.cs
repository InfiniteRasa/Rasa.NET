using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class DamageImmunityTests
    {
        [TestMethod]
        public void AnActorImmuneToAllTakesNothingAndTheHitSaysSo()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var health = Prepare(player, 1000);
            player.ImmuneToAllDamage = true;

            var taken = ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var outcome, DamageType.Laser);

            Assert.AreEqual(0, taken);
            Assert.IsTrue(outcome.Immune);
            Assert.AreEqual(0, outcome.Delivered);
            Assert.AreEqual(0, outcome.Absorbed);
            Assert.AreEqual(1000, health.Current);
        }

        [TestMethod]
        public void ATypeImmunityAnswersOnlyItsType()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var health = Prepare(player, 1000);
            player.DamageImmunities.Add(DamageType.Fire);

            ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var fire, DamageType.Fire);
            Assert.IsTrue(fire.Immune);
            Assert.AreEqual(1000, health.Current);

            ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var laser, DamageType.Laser);
            Assert.IsFalse(laser.Immune);
            Assert.AreEqual(100, laser.Delivered);
            Assert.AreEqual(900, health.Current);
        }

        [TestMethod]
        public void AnUntypedHitCountsAsPhysical()
        {
            Assert.IsTrue(DamageImmunity.IsImmune(Player(DamageType.Physical), 0));
            Assert.IsFalse(DamageImmunity.IsImmune(Player(DamageType.Fire), 0));
        }

        [TestMethod]
        public void AnEffectCanGrantImmunityWhileItIsOn()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var health = Prepare(player, 1000);
            var ward = new GameEffect { EffectId = 880002, TypeId = 10000056, IsBuff = true, AllowDetach = true };
            ward.ImmuneDamageTypes.Add(DamageType.Laser);
            GameEffectManager.Instance.Attach(harness.BootcampMap, player, ward);

            ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var warded, DamageType.Laser);
            Assert.IsTrue(warded.Immune);
            Assert.AreEqual(1000, health.Current);

            GameEffectManager.Instance.DettachEffect(harness.BootcampMap, player, ward);
            ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var bare, DamageType.Laser);
            Assert.IsFalse(bare.Immune);
            Assert.AreEqual(900, health.Current);
        }

        [TestMethod]
        public void AnImmuneTargetDrainsNoShield()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            Prepare(player, 1000);
            var shield = new GameEffect { EffectId = 880003, TypeId = 10000056, IsBuff = true, AllowDetach = true, AbsorbPercent = 100, AbsorbPool = new AbsorbPool { Remaining = 500 } };
            GameEffectManager.Instance.Attach(harness.BootcampMap, player, shield);
            player.ImmuneToAllDamage = true;

            ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null, out var outcome);

            Assert.IsTrue(outcome.Immune);
            Assert.AreEqual(500, shield.AbsorbPool.Remaining);
        }

        [TestMethod]
        public void ACallerWithNoHitRecordStillHasImmuneShown()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            Prepare(player, 1000);
            player.ImmuneToAllDamage = true;
            harness.Drain();

            Assert.AreEqual(0, ActorManager.Instance.Damage(harness.BootcampMap, player, 100, null));

            var failed = harness.Drain().OfType<GameEffectAttachFailedPacket>().Single();
            Assert.AreEqual(GameEffectAttachFailedPacket.FailReason.Immune, failed.Reason);
        }

        [TestMethod]
        public void RawInfoCarriesWasImmune()
        {
            var packet = new GameEffectAnnounceDamagePacket(7);
            packet.Hits.Add(new TickEntry { EntityId = 5, WasImmune = true, DamageType = DamageType.Fire });

            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet))));
            Assert.AreEqual(3, reader.ReadTuple());
            reader.SkipValue();                 // effectId
            reader.SkipValue();                 // method name
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            reader.SkipValue();                 // entityId
            Assert.AreEqual(12, reader.ReadTuple());
            for (var i = 0; i < 5; i++)
                reader.SkipValue();             // damageType, reflected, filtered, absorbed, resisted
            Assert.AreEqual(0L, reader.ReadLong());
            reader.SkipValue();                 // isCrit
            reader.SkipValue();                 // deathBlow
            reader.SkipValue();                 // coverModifier
            Assert.AreEqual(1, reader.ReadInt());
        }

        [TestMethod]
        public void AWeaponHitOnAnImmuneCreatureShowsImmuneAsItLands()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true, worldMigration: BootcampCaveInBreachTests.BridgeThraxStood);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var thrax = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).First(creature => creature.DbId == 510216);
            harness.MovePlayerTo(thrax);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var health = thrax.Attributes[Attributes.Health].Current;
            thrax.ImmuneToAllDamage = true;
            harness.Drain();

            MissileManager.Instance.MissileLaunch(harness.BootcampMap,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 1, thrax.EntityId, 0), 10000);
            MissileManager.Instance.DoWork(harness.BootcampMap, 1000);

            Assert.AreNotEqual(CharacterState.Dead, thrax.State);
            Assert.AreEqual(health, thrax.Attributes[Attributes.Health].Current);
            var packets = harness.Drain();
            var hit = packets.OfType<WeaponAttackRecovery>()
                .SelectMany(recovery => recovery.Missile.Args.HitData)
                .Single(data => data.EntityId == thrax.EntityId);
            Assert.AreEqual(1, hit.WasImune);
            Assert.AreEqual(0L, hit.FinalAmt);
            Assert.IsFalse(packets.OfType<GameEffectAttachFailedPacket>().Any(),
                "the hit carries it; no separate announcement");
        }

        private static Manifestation Player(DamageType immuneTo)
        {
            var player = new Manifestation();
            player.DamageImmunities.Add(immuneTo);
            return player;
        }

        private static ActorAttributes Prepare(Manifestation player, int health)
        {
            var bar = player.Attributes[Attributes.Health];
            bar.CurrentMax = health;
            bar.Current = health;
            player.Attributes[Attributes.Armor].Current = 0;
            return bar;
        }
    }
}
