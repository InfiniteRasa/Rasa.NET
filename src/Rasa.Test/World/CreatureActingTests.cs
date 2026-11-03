using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A creature starts no attack until its last one has played on the clients (CreatureActing).
    /// The Wilderness Thrax Grenadier threw its grenade (1, 229) every 500 ms against a 1,500 ms
    /// recovery, with its rifle (1, 79) in between, and its clients queued what they could not
    /// play yet and played it out after it died: a body on the ground still firing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureActingTests
    {
        private const long Tick = 100;
        private const uint Grenade = 229, Rifle = 79;
        private const int GrenadeRecovery = 1500, RifleRecovery = 700;

        private static readonly FieldInfo Actions = typeof(AbilityManager).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestMethod]
        public void AGrenadierWaitsForEachAttackToPlayBeforeTheNext()
        {
            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            var player = Watch(world, new Vector3(10, 0, 0));
            var grenadier = Spawn(world);

            try
            {
                Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(grenadier, player.Player.EntityId));
                grenadier.Controller.ActionFighting.Opened = true;

                var attacks = new List<(long At, uint Arg)>();

                for (long now = 0; now < 8000; now += Tick)
                {
                    BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
                    MissileManager.Instance.DoWork(world.Map, Tick);

                    foreach (var windup in Sent(player, grenadier).OfType<PerformWindupPacket>())
                        attacks.Add((now, windup.ActionArgId));
                }

                Assert.IsGreaterThan(4, attacks.Count, "it still fights");
                CollectionAssert.IsSubsetOf(new[] { Grenade, Rifle }, attacks.Select(a => a.Arg).Distinct().ToList(), "both its attacks get their turn");

                for (var i = 1; i < attacks.Count; i++)
                {
                    var played = attacks[i - 1].Arg == Grenade ? GrenadeRecovery : RifleRecovery;
                    Assert.IsGreaterThanOrEqualTo(played, attacks[i].At - attacks[i - 1].At,
                        $"attack {i} ({attacks[i].Arg}) came {attacks[i].At - attacks[i - 1].At} ms after the last ({attacks[i - 1].Arg}), which plays for {played} ms");
                }

                // As fast as they can be shown, and no faster: 8 s of 1,500 and 700 ms attacks.
                Assert.IsLessThanOrEqualTo(8000 / ((GrenadeRecovery + RifleRecovery) / 2) + 1, attacks.Count);
            }
            finally
            {
                world.Map.QueuedMissiles.Clear();
            }
        }

        [TestMethod]
        public void AnAttackPlaysForItsWindupAndItsRecovery()
        {
            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            var grenadier = Spawn(world);

            Assert.AreEqual(GrenadeRecovery, CreatureActing.PlayMsOf(grenadier, grenadier.Actions[0]));
            Assert.AreEqual(RifleRecovery, CreatureActing.PlayMsOf(grenadier, grenadier.Actions[1]));

            // The row's own windup, when it has one, as CreatureWindups takes it.
            var wound = new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = Grenade, WindupTime = 400 };
            Assert.AreEqual(400 + GrenadeRecovery, CreatureActing.PlayMsOf(grenadier, wound));

            // Nothing known of it: nothing to wait for, as before.
            Assert.AreEqual(0, CreatureActing.PlayMsOf(grenadier, new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = 9999 }));
            Assert.AreEqual(0, CreatureActing.PlayMsOf(grenadier, null));
        }

        [TestMethod]
        public void CalledShotArmDrawsAWeaponAttacksRecoveryOutAsTheClientDoes()
        {
            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            var grenadier = Spawn(world);

            grenadier.ActiveEffects[9001] = new GameEffect { EffectId = 9001, AttackRateModifier = 2.0 };

            Assert.AreEqual(2 * GrenadeRecovery, CreatureActing.PlayMsOf(grenadier, grenadier.Actions[0]));
        }

        [TestMethod]
        public void TheTimeLeftCountsDownWithTheThink()
        {
            using var world = new WorldTestContext();
            using var _ = WeaponAttackData();
            var grenadier = Spawn(world);

            CreatureActing.Began(grenadier, grenadier.Actions[0]);
            Assert.IsTrue(CreatureActing.IsActing(grenadier));

            for (long left = GrenadeRecovery; left > Tick; left -= Tick)
            {
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
                Assert.IsTrue(CreatureActing.IsActing(grenadier), $"{left - Tick} ms to go");
            }

            BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
            Assert.IsFalse(CreatureActing.IsActing(grenadier));
            Assert.AreEqual(0, grenadier.ActingMs);
        }

        private static List<object> Sent(Client client, Creature from) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Where(message => message.EntityId == from.EntityId)
                .Select(message => (object)message.Packet)
                .ToList();

        /// <summary>WEAPON_ATTACK's two arguments the Grenadier attacks with, as actiondata.actionArguments has them.</summary>
        private static IDisposable WeaponAttackData()
        {
            var actions = (Dictionary<ActionId, ActionInfo>)Actions.GetValue(AbilityManager.Instance);
            actions.TryGetValue(ActionId.WeaponAttack, out var previous);

            var info = new ActionInfo { ActionId = ActionId.WeaponAttack, Module = "weapons.baseweaponattack" };
            info.Levels[Grenade] = new ActionLevelInfo { ActionId = ActionId.WeaponAttack, Level = Grenade, WindupMs = 0, RecoveryMs = GrenadeRecovery, MaxRange = 40 };
            info.Levels[Rifle] = new ActionLevelInfo { ActionId = ActionId.WeaponAttack, Level = Rifle, WindupMs = 0, RecoveryMs = RifleRecovery, MaxRange = 60 };
            actions[ActionId.WeaponAttack] = info;

            return new Restore(() =>
            {
                if (previous != null)
                    actions[ActionId.WeaponAttack] = previous;
                else
                    actions.Remove(ActionId.WeaponAttack);
            });
        }

        /// <summary>A Thrax Grenadier as the Wilderness has it: the grenade at 500 ms, the rifle at 800.</summary>
        private static Creature Spawn(WorldTestContext world)
        {
            var creature = new Creature
            {
                Name = "Thrax Grenadier",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 10,
                AggroRange = 0,
                RunSpeed = 9,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(600);
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = Grenade, RangeMin = 5, RangeMax = 15, Cooldown = 500, MinDamage = 15, MaxDamage = 32 });
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = Rifle, RangeMin = 1, RangeMax = 15, Cooldown = 800, MinDamage = 10, MaxDamage = 20 });
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, Vector3 at)
        {
            var client = world.CreateClient(x: at.X, z: at.Z);
            client.Player.Position = at;
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            var seed = CellManager.Instance.GetCellSeed(at);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private sealed class Restore : IDisposable
        {
            private readonly Action _undo;

            public Restore(Action undo) => _undo = undo;

            public void Dispose() => _undo();
        }
    }
}
