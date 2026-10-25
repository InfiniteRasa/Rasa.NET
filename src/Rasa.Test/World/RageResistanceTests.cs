using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class RageResistanceTests
    {
        [TestMethod]
        [DataRow(3u)]
        [DataRow(5u)]
        public void RageAddsTenToDisplayedResistsWithoutApplyingItTwice(uint pump)
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.ResistanceData.Add(new ResistanceData(DamageType.Fire, 20));

            var rage = Cast(world, client, pump);
            Assert.AreEqual(10, rage.ResistModifier, "the seeded Rage pump's bonus");
            var shown = SentResistance(client);
            Assert.AreEqual(30, Value(shown, DamageType.Fire));
            Assert.AreEqual(10, Value(shown, DamageType.Physical));
            Assert.AreEqual(10, Value(shown, DamageType.Electrical));
            Assert.AreEqual(20, client.Player.ResistanceData.Single().ResistanceAmmount, "base resistance stays separate");
            Assert.AreEqual(62, GameEffectManager.ApplyResist(client.Player, 100, out var resisted, DamageType.Fire));
            Assert.AreEqual(38, resisted, "equipment plus Rage, counted once");

            GameEffectManager.Instance.DettachEffect(world.Map, client.Player, rage);
            var ended = SentResistance(client);
            Assert.AreEqual(20, Value(ended, DamageType.Fire));
            Assert.AreEqual(0, Value(ended, DamageType.Physical));
            Assert.AreEqual(71, GameEffectManager.ApplyResist(client.Player, 100, out _, DamageType.Fire));
        }

        [TestMethod]
        public void ExpirySendsTheBaseResistancesAgain()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var rage = Cast(world, client, 3);
            SentResistance(client);
            rage.ExpiresTick = Environment.TickCount64 - 1;

            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(0, SentResistance(client).Count);
            Assert.IsFalse(client.Player.ActiveEffects.ContainsKey(rage.EffectId));
        }

        [TestMethod]
        public void SquadAuraAddsAndRemovesItsResistanceBonus()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 5, 0);
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate);
            var rage = Cast(world, lead, 5);
            WorldTestContext.Drain(mate);

            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(10, Value(SentResistance(mate), DamageType.Fire));
            Assert.AreEqual(83, GameEffectManager.ApplyResist(mate.Player, 100, out _, DamageType.Fire));
            GameEffectManager.Instance.DettachEffect(world.Map, lead.Player, rage);
            Assert.AreEqual(0, SentResistance(mate).Count);
        }

        [TestMethod]
        public void TypedBonusesStackInTheDisplayWithoutChangingBaseResistance()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.ResistanceData.Add(new ResistanceData(DamageType.Fire, 20));
            Cast(world, client, 3);
            var shield = new GameEffect
            {
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                TypeId = 10000999, EffectLevel = 1, IsBuff = true,
                ResistDamageType = DamageType.Fire, ResistModifier = 5
            };
            GameEffectManager.Instance.Attach(world.Map, client.Player, shield);
            var shown = SentResistance(client);
            Assert.AreEqual(35, Value(shown, DamageType.Fire));
            Assert.AreEqual(10, Value(shown, DamageType.Ice));
            Assert.AreEqual(59, GameEffectManager.ApplyResist(client.Player, 100, out _, DamageType.Fire));

            GameEffectManager.Instance.DettachEffect(world.Map, client.Player, shield);
            Assert.AreEqual(30, Value(SentResistance(client), DamageType.Fire));
        }

        // Use the production pump properties rather than duplicating the reported +10 in the fixture.
        private sealed class RageProperties : ActionPropertyPreloader
        {
            internal IEnumerable<object[]> Properties => GetRows();
        }

        private static GameEffect Cast(WorldTestContext world, Client client, uint pump)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.AaSoldierRage, Level = pump };
            foreach (var row in new RageProperties().Properties.Where(row => Convert.ToUInt32(row[1]) == 307 && Convert.ToUInt32(row[2]) == pump))
                info.Properties[(AbilityProperty)Convert.ToInt32(row[3])] = Convert.ToInt32(row[4]);
            typeof(AbilityManager).GetMethod("AttachRage", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { world.Map, client.Player, info });
            return client.Player.ActiveEffects.Values.Single(effect => effect.TypeId == 236);
        }

        private static List<ResistanceData> SentResistance(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<ResistanceDataPacket>().Last().ResistanceData;

        private static int Value(List<ResistanceData> values, DamageType type) =>
            values.Where(value => value.ResistanceType == type).Sum(value => value.ResistanceAmmount);
    }
}
