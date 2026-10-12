using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    // A dead player's effects while the map's effect worker runs.
    //
    // A death ends the player's effects but for what PlayerDeath keeps: Rez Trauma, the skill
    // passives, an aura's effect that belongs to its owner, and a Hominis Machina's morph while
    // its Self Revive is unspent. The worker runs every tick of the map; it used to take
    // everything off anything dead on its next pass, without a word to the client, so all of
    // that was gone a moment after the death.
    [TestClass]
    [DoNotParallelize]
    public class DeadPlayerEffectsTests
    {
        private const int Fixture = 10000901;

        #region What PlayerDeath keeps

        [TestMethod]
        public void RezTraumaStacksWithTheWorkerRunningWhileDead()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.Level = 5;

            Kill(world, client);
            Work(world);
            PlayerDeath.ReviveMe(client, null);
            Work(world);

            Kill(world, client);
            Work(world);

            Assert.IsNotNull(Trauma(client), "still on them, dead");
            Assert.AreEqual(1, Trauma(client).Stacks);

            PlayerDeath.ReviveMe(client, null);

            Assert.AreEqual(2, Trauma(client).Stacks, "two deaths' worth");
            Assert.AreEqual(-40, Trauma(client).PrimaryAttributesPercent);
            Assert.AreEqual(-40, GameEffectManager.AttributePercentOf(client.Player, Attributes.Body));
        }

        [TestMethod]
        public void LeavingTheGameDeadSavesEveryDeathsWorth()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.Level = 5;

            Kill(world, client);
            PlayerDeath.ReviveMe(client, null);
            Kill(world, client);
            Work(world);

            // The logout revives them at their hospital first, then saves (RelogVitals).
            PlayerDeath.PlayerLeaving(client);

            Assert.AreEqual(2, RelogVitals.Capture(client.Player, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).RezTraumaStacks);
        }

        [TestMethod]
        public void TheClientIsToldOfNothingGoingFromTheDead()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.Level = 5;

            Kill(world, client);
            PlayerDeath.ReviveMe(client, null);
            Kill(world, client);
            var kept = client.Player.ActiveEffects.Keys.ToList();
            WorldTestContext.Drain(client);

            Work(world);

            CollectionAssert.AreEquivalent(kept, client.Player.ActiveEffects.Keys.ToList());
            Assert.AreEqual(0, Sent(client).OfType<GameEffectDetachedPacket>().Count());
        }

        [TestMethod]
        public void AHominisMachinaCanStillGetUpAfterTheWorkerHasRun()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 20);
            var player = client.Player;

            Morph(world, client);
            Kill(world, client);
            Work(world);

            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == AbilityManager.PolymorphTypeId), "the morph, with its button");
            Assert.IsTrue(PlayerDeath.SelfRevive(client));
            Assert.AreNotEqual(CharacterState.Dead, player.State);
        }

        [TestMethod]
        public void ASkillPassiveOutlastsADeath()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;

            GameEffectManager.Instance.Attach(world.Map, player, new GameEffect
            {
                TypeId = ItemModuleBonuses.MovementTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                SourceId = player.EntityId,
                Source = player,
                IsSkillPassive = true,
                ServerOnly = true,
                AnnounceOnAttach = false,
                AnnounceToNewcomers = false,
                MovementModifierPercent = 110
            });
            Assert.AreEqual(1.10, player.MovementSpeed, 1e-9);

            Kill(world, client);
            Work(world);
            PlayerDeath.ReviveMe(client, null);
            Work(world);

            Assert.AreEqual(1, player.ActiveEffects.Values.Count(e => e.TypeId == ItemModuleBonuses.MovementTypeId));
            Assert.AreEqual(1.10, player.MovementSpeed, 1e-9, "what the move check measures against");
        }

        #endregion

        #region While dead

        [TestMethod]
        public void NothingTicksOnTheDead()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var ticks = 0;

            // An aura's effect on them, which its owner's aura looks after and a death leaves.
            var aura = new GameEffect { TypeId = Fixture, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), ServerOnly = true };
            var held = new GameEffect
            {
                TypeId = Fixture + 1,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                ServerOnly = true,
                Parent = aura,
                TickIntervalMs = 1000,
                NextTickTick = Environment.TickCount64,
                OnTick = (_, _, _) => ticks++
            };
            aura.Children.Add(held);
            GameEffectManager.Instance.Attach(world.Map, client.Player, held);

            Kill(world, client);
            Work(world);

            Assert.IsTrue(client.Player.ActiveEffects.ContainsKey(held.EffectId), "its owner's to take off");
            Assert.AreEqual(0, ticks);

            PlayerDeath.ReviveMe(client, null);
            Work(world);

            Assert.AreEqual(1, ticks, "back to ticking once up");
        }

        [TestMethod]
        public void WhatIsKeptStillRunsOutWhileDead()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.Level = 5;

            Kill(world, client);
            PlayerDeath.ReviveMe(client, null);
            Kill(world, client);
            var trauma = Trauma(client);
            WorldTestContext.Drain(client);

            trauma.ExpiresTick = Environment.TickCount64 - 1;
            Work(world);

            Assert.IsNull(Trauma(client), "its time is up");
            Assert.AreEqual(trauma.EffectId, Sent(client).OfType<GameEffectDetachedPacket>().Single().EffectId, "and the client told");
            Assert.AreEqual(0, GameEffectManager.AttributePercentOf(client.Player, Attributes.Body));
        }

        [TestMethod]
        public void WhatRunsOutWhileDeadEndsAsItWouldAlive()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var ended = 0;
            var aura = new GameEffect { TypeId = Fixture, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), ServerOnly = true };
            var held = new GameEffect
            {
                TypeId = Fixture + 1,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                ServerOnly = true,
                Parent = aura,
                ExpiresTick = Environment.TickCount64 + 60000,
                OnExpired = (_, _, _) => ended++
            };
            aura.Children.Add(held);
            GameEffectManager.Instance.Attach(world.Map, client.Player, held);

            Kill(world, client);
            held.ExpiresTick = Environment.TickCount64 - 1;
            Work(world);

            Assert.IsFalse(client.Player.ActiveEffects.ContainsKey(held.EffectId));
            Assert.AreEqual(1, ended, "its own ending, as on the living");
            Assert.AreEqual(0, aura.Children.Count);
        }

        [TestMethod]
        public void ADeadPlayerNoLongerOnTheMapIsClearedFromItAsBefore()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            client.Player.Level = 5;

            Kill(world, client);
            PlayerDeath.ReviveMe(client, null);
            Kill(world, client);
            client.Player.MapContextId = world.Map.MapInfo.MapContextId + 1;

            Work(world);

            Assert.AreEqual(0, client.Player.ActiveEffects.Count);
            Assert.IsFalse(world.Map.ActorsWithEffects.Contains(client.Player));
        }

        [TestMethod]
        public void AnEffectPutOnTheDeadIsTakenOffAndTheClientTold()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);

            Kill(world, client);

            // Nothing a death keeps: on them after it, from whatever does not check.
            var late = new GameEffect { TypeId = Fixture, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), ExpiresTick = Environment.TickCount64 + 60000 };
            GameEffectManager.Instance.Attach(world.Map, client.Player, late);
            WorldTestContext.Drain(client);

            Work(world);

            Assert.IsFalse(client.Player.ActiveEffects.ContainsKey(late.EffectId));
            Assert.AreEqual(late.EffectId, Sent(client).OfType<GameEffectDetachedPacket>().Single().EffectId);
        }

        [TestMethod]
        public void ADeadCreatureStillLosesItsEffects()
        {
            using var world = new WorldTestContext();
            var creature = new Creature
            {
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                EntityClass = EntityClasses.HumanBaseMale,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Position = Vector3.Zero
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);

            GameEffectManager.Instance.Attach(world.Map, creature, new GameEffect
            {
                TypeId = Fixture,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                ServerOnly = true,
                IsSkillPassive = true
            });

            creature.State = CharacterState.Dead;
            Work(world);

            Assert.AreEqual(0, creature.ActiveEffects.Count);
            Assert.IsFalse(world.Map.ActorsWithEffects.Contains(creature));
        }

        #endregion

        #region Fixture

        private static void Kill(WorldTestContext world, Client client)
        {
            client.Player.Attributes[Attributes.Health].Current = 0;
            Assert.IsTrue(PlayerDeath.AtZero(world.Map, client.Player, null));
        }

        /// <summary>One pass of the map's effect worker.</summary>
        private static void Work(WorldTestContext world) => GameEffectManager.Instance.DoWork(world.Map, 100);

        private static GameEffect Trauma(Client client) =>
            client.Player.ActiveEffects.Values.SingleOrDefault(e => e.TypeId == PlayerDeath.RezSicknessTypeId);

        private static List<PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message)
            .OfType<CallMethodMessage>()
            .Select(message => message.Packet)
            .ToList();

        /// <summary>A Hominis Machina's morph, with Self Revive in its drawer (as PvpCorpseTests).</summary>
        private static void Morph(WorldTestContext world, Client client)
        {
            var player = client.Player;

            player.MorphWeapon = new MorphWeaponItem();
            player.MorphAbilities = new List<(ActionId, uint)> { (ActionId.PolySelfRes, 1u) };

            GameEffectManager.Instance.Attach(world.Map, player, new GameEffect
            {
                TypeId = AbilityManager.PolymorphTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                Source = player,
                SourceId = player.EntityId,
                IsBuff = true,
                ServerOnly = true,
                ExpiresTick = Environment.TickCount64 + 120000,
                OnDetached = (map, actor, e) =>
                {
                    player.MorphWeapon = null;
                    player.MorphAbilities = new List<(ActionId, uint)>();
                }
            });
            Assert.IsTrue(AbilityManager.CanSelfRevive(player));
        }

        #endregion
    }
}
