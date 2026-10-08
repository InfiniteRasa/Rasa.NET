using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    // The Power bar on the player's own client, against the server's.
    //
    // Nothing tells the client what its Power is from second to second. It is given a value, an
    // amount and a period, and counts up by itself (client/augmentations/actorattribute.py
    // _EvaluatePredictedRefresh); the server counts the same way (ActorManager.Regenerate). And
    // it is the client's own figure that decides whether an ability is asked for at all:
    // CheckConsumables refuses with "not enough power" and sends nothing, so a client that has
    // fallen behind stays behind until something else sends it the bar.
    //
    // These tests give a copy of the client's arithmetic (ClientBar) the packets the server
    // sent, and hold the two figures against each other a second at a time.
    [TestClass]
    [DoNotParallelize]
    public class PowerBarRefreshTests
    {
        private const int MechAuraPercent = 50;
        private const int RegenerationWaveTypeId = 10000019;        // REGENERATIONWAVE, as AbilityManager has it

        #region A period

        [TestMethod]
        public void ThePowerBarGoesOnRefillingIntoAFightAndOutOfIt()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var bar = Arrive(client, power: 10);
            var now = 0;

            Assert.IsTrue(Rate(client) > 0, "power regenerates");
            Together(context.World, client, bar, ref now, 5, "before the fight");

            // The player's own shot, or one that hits them.
            ManifestationManager.Instance.EnterCombat(client);
            Receive(client, bar, now);
            Together(context.World, client, bar, ref now, 5, "in the fight");

            ManifestationManager.Instance.ExitCombat(client);
            Receive(client, bar, now);
            Together(context.World, client, bar, ref now, 5, "after it");

            Assert.AreEqual(10 + 15 * Rate(client), bar.At(now), "fifteen seconds of it, at the one rate");
        }

        [TestMethod]
        public void AnAttributePointLeavesTheBarCounting()
        {
            using var context = new WeaponAmmoContext();
            var client = context.Client;

            client.Player.Level = 2;
            client.Player.Race = Race.Human;
            client.Player.Attributes = Enum.GetValues<Attributes>().ToDictionary(id => id, id => new ActorAttributes(id, 0, 0, 0, 0, 0));

            using (var database = context.Open())
            {
                database.CharacterEntries.Single().Level = 2;
                database.SaveChanges();
            }

            var manager = new ManifestationManager(context);

            manager.UpdateStatsValues(client, true);
            WorldTestContext.Drain(client);

            var bar = Arrive(client, power: 10);
            var now = 0;

            Assert.IsTrue(Rate(client) > 0, "power regenerates");
            Together(context.World, client, bar, ref now, 3, "before");

            // The attributes window's Apply: the stats again, and an AttributeInfo by itself.
            manager.AllocateAttributePoints(client, new AllocateAttributePointsPacket { Mind = 1 });

            Assert.AreEqual(1, client.Player.SpentMind, "spent");
            Receive(client, bar, now);
            Together(context.World, client, bar, ref now, 5, "after the point");
        }

        [TestMethod]
        public void PowerHasAPeriodWheneverTheStatsAreWorkedOut()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var power = client.Player.Attributes[Attributes.Power];

            // What every AttributeInfo carries - a level, a piece of armor, an attribute point,
            // a player made on a client - is the attribute as the stats left it.
            Assert.AreEqual(CombatRegen.RegenPeriodSeconds, power.RefreshPeriod);

            client.Player.InCombat = true;
            ManifestationManager.Instance.UpdateStatsValues(client, false);
            Assert.AreEqual(CombatRegen.RegenPeriodSeconds, power.RefreshPeriod, "the same in combat: it is health that slows");

            client.Player.InCombat = false;
            ManifestationManager.Instance.UpdateStatsValues(client, true);
            Assert.AreEqual(CombatRegen.RegenPeriodSeconds, power.RefreshPeriod);

            // And for attributes that have never been worked out before, in a fight.
            var other = context.CreateClient(20);

            other.Player.InCombat = true;
            Assert.AreEqual(0, other.Player.Attributes[Attributes.Power].RefreshPeriod, "as they are made");
            ManifestationManager.Instance.UpdateStatsValues(other, true);
            Assert.AreEqual(CombatRegen.RegenPeriodSeconds, other.Player.Attributes[Attributes.Power].RefreshPeriod);
        }

        #endregion

        #region A rate

        [TestMethod]
        public void ARateAnEffectMakesIsKeptThroughAFight()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var bar = Arrive(client, power: 10);
            var now = 0;

            MechAura(context.World, client);
            Receive(client, bar, now);
            Assert.IsTrue(GameEffectManager.RegenAmount(client.Player, client.Player.Attributes[Attributes.Power]) > Rate(client), "the aura quickens it");
            Together(context.World, client, bar, ref now, 5, "with the aura on");

            ManifestationManager.Instance.EnterCombat(client);
            Receive(client, bar, now);
            Together(context.World, client, bar, ref now, 5, "in the fight");

            ManifestationManager.Instance.ExitCombat(client);
            Receive(client, bar, now);
            Together(context.World, client, bar, ref now, 5, "after it");
        }

        [TestMethod]
        public void AttributeInfoCarriesTheRatesTheEffectsMakeAndLeavesTheAttributesAsTheyAre()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var player = client.Player;
            var held = player.Attributes;
            var map = context.World.Map;

            // As a piece of armor gives it (ManifestationManager.ApplyRegenPeriod).
            held[Attributes.Armor].RefreshAmount = 8;
            held[Attributes.Power].Current = 10;

            var health = held[Attributes.Health].RefreshAmount;
            var power = held[Attributes.Power].RefreshAmount;

            Assert.IsTrue(health > 0 && power > 0);

            // Regeneration Wave on health and power, and Graviton armor's own on armor.
            GameEffectManager.Instance.Attach(map, player, Effect(map, player, RegenerationWaveTypeId, wave => wave.RegenPercent = 400));
            GameEffectManager.Instance.Attach(map, player, Effect(map, player, ArmorSkills.GravitonVisibleTypeId, graviton => graviton.ArmorRegenPercent = 50));

            var sent = new AttributeInfoPacket(player).ActorAttributes;

            CollectionAssert.AreEqual(held.Keys.ToList(), sent.Keys.ToList(), "all of them, in their order");

            Assert.AreEqual(5 * health, sent[Attributes.Health].RefreshAmount);
            Assert.AreEqual(5 * power, sent[Attributes.Power].RefreshAmount);
            Assert.AreEqual(12, sent[Attributes.Armor].RefreshAmount);

            foreach (var id in new[] { Attributes.Health, Attributes.Power, Attributes.Armor })
            {
                Assert.AreEqual(held[id].RefreshPeriod, sent[id].RefreshPeriod, $"{id}: the period it has");
                Assert.AreEqual(held[id].Current, sent[id].Current);
                Assert.AreEqual(held[id].CurrentMax, sent[id].CurrentMax);
                Assert.AreEqual(held[id].NormalMax, sent[id].NormalMax);
            }

            // The attributes keep their own rates: the stats work those out again from nothing,
            // and the effects are applied to what they leave.
            Assert.AreEqual(health, held[Attributes.Health].RefreshAmount);
            Assert.AreEqual(power, held[Attributes.Power].RefreshAmount);
            Assert.AreEqual(8, held[Attributes.Armor].RefreshAmount);

            foreach (var id in held.Keys.Where(id => id != Attributes.Health && id != Attributes.Armor && id != Attributes.Power))
                Assert.AreSame(held[id], sent[id], $"{id} regenerates by nothing");
        }

        [TestMethod]
        public void ARateAnEffectMakesIsKeptWhenAnAbilityIsPaidFor()
        {
            using var context = new ProgressionTestContext();
            var client = Player(context, 20);
            var bar = Arrive(client, power: 60);
            var now = 0;
            var ability = new ActionLevelInfo { ActionId = ActionId.AaRecruitLightning, Level = 1 };

            ability.Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 40 });

            MechAura(context.World, client);
            Receive(client, bar, now);

            AbilityManager.TakeCosts(client, client.Player, ability);
            Receive(client, bar, now);

            Assert.AreEqual(20, bar.At(now), "paid");
            Together(context.World, client, bar, ref now, 5, "after paying");
        }

        #endregion

        #region Fixture

        /// <summary>
        /// The client's copy of one attribute (client/augmentations/actorattribute.py), with the
        /// clock in the caller's hands: what the bar shows, and what CheckConsumables reads.
        /// </summary>
        private sealed class ClientBar
        {
            private double _current;
            private double _lastUpdate;
            private int _currentMax;
            private int _refreshAmount;
            private int _refreshPeriod;

            /// <summary>Actor.Recv_AttributeInfo: a new ActorAttribute, with the period it was sent.</summary>
            internal void Info(ActorAttributes sent, double now)
            {
                _currentMax = sent.CurrentMax;
                _refreshAmount = sent.RefreshAmount;
                _refreshPeriod = sent.RefreshPeriod;
                SetCurrent(sent.Current, now);
            }

            /// <summary>
            /// Actor.UpdateAttribute, from Recv_UpdatePower and Recv_UpdateAttributes: Update, and
            /// then SetRefreshRate with the amount sent and a period of 1.
            /// </summary>
            internal void Update(ActorAttributes sent, double now)
            {
                _currentMax = sent.CurrentMax;
                SetCurrent(sent.Current, now);
                Evaluate(now);
                _refreshAmount = sent.RefreshAmount;
                _refreshPeriod = 1;
            }

            /// <summary>ActorAttribute.current.</summary>
            internal int At(double now)
            {
                Evaluate(now);

                return (int)_current;
            }

            /// <summary>_EvaluatePredictedRefresh.</summary>
            private void Evaluate(double now)
            {
                if (_refreshPeriod == 0)
                    return;

                var delta = (now - _lastUpdate) * _refreshAmount / _refreshPeriod;

                if (Math.Abs(delta) < 0.01)
                    return;

                SetCurrent(_current + delta, now);
            }

            /// <summary>_SetCurrent.</summary>
            private void SetCurrent(double value, double now)
            {
                _current = Math.Min(Math.Max(value, 0), _currentMax);
                _lastUpdate = now;
            }
        }

        /// <summary>A player of that level on the map, their stats worked out.</summary>
        private static Client Player(ProgressionTestContext context, byte level)
        {
            var client = context.CreateClient(level);
            var map = context.World.Map;
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);

            client.Player.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            client.Player.State = CharacterState.Normal;
            ManifestationManager.Instance.UpdateStatsValues(client, true);
            WorldTestContext.Drain(client);

            return client;
        }

        /// <summary>
        /// The bar as the client has it once it is in the world with that much Power: the
        /// AttributeInfo its player is made with, and the UpdateAttributes that follows it
        /// (ManifestationManager.AssignPlayer).
        /// </summary>
        private static ClientBar Arrive(Client client, int power)
        {
            var bar = new ClientBar();
            var attribute = client.Player.Attributes[Attributes.Power];

            Assert.IsTrue(attribute.CurrentMax >= 100, "room to count in");
            attribute.Current = power;

            bar.Info(new AttributeInfoPacket(client.Player).ActorAttributes[Attributes.Power], 0);
            bar.Update(new UpdateAttributesPacket(client.Player.Attributes, 0).AttributeDataList[Attributes.Power], 0);

            return bar;
        }

        /// <summary>What the client was sent about its own Power since it was last read, taken in.</summary>
        private static void Receive(Client client, ClientBar bar, double now)
        {
            foreach (var call in WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>())
            {
                if (call.EntityId != client.Player.EntityId)
                    continue;

                switch (call.Packet)
                {
                    case AttributeInfoPacket info:
                        bar.Info(info.ActorAttributes[Attributes.Power], now);
                        break;
                    case UpdatePowerPacket update:
                        bar.Update(update.Power, now);
                        break;
                    case UpdateAttributesPacket update:
                        bar.Update(update.AttributeDataList[Attributes.Power], now);
                        break;
                }
            }
        }

        /// <summary>That many seconds, the server's and the client's, with the two bars the same at each.</summary>
        private static void Together(WorldTestContext world, Client client, ClientBar bar, ref int now, int seconds, string when)
        {
            var power = client.Player.Attributes[Attributes.Power];

            for (var second = 1; second <= seconds; second++)
            {
                ActorManager.Instance.Regenerate(world.Map);
                now++;
                Receive(client, bar, now);

                Assert.IsTrue(power.Current < power.CurrentMax, "still counting");
                Assert.AreEqual(power.Current, bar.At(now), $"{when}, second {second}: the server has {power.Current}");
            }
        }

        /// <summary>What Power gains a second with nothing on the player.</summary>
        private static int Rate(Client client) => client.Player.Attributes[Attributes.Power].RefreshAmount;

        /// <summary>The Mech armor skill's aura, as ManifestationManager.SyncArmorSkills puts it on.</summary>
        private static void MechAura(WorldTestContext world, Client client)
        {
            var effect = Effect(world.Map, client.Player, ArmorSkills.MechAuraTypeId, aura => aura.PowerRegenPercent = MechAuraPercent);

            effect.IsSkillPassive = true;
            effect.AllowDetach = false;
            GameEffectManager.Instance.Attach(world.Map, client.Player, effect);
        }

        /// <summary>A standing effect of the player's own, of that type, that does that and nothing else.</summary>
        private static GameEffect Effect(MapChannel map, Manifestation player, int typeId, Action<GameEffect> does)
        {
            var effect = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(map),
                EffectLevel = 1,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                ExpiresTick = long.MaxValue,
                AnnounceOnAttach = true
            };

            does(effect);

            return effect;
        }

        #endregion
    }
}
