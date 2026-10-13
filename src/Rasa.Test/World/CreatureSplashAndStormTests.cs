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
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A creature's launcher splashes as a player's does (Splash), and a creature's lightning
    /// leaves a storm when its argument has one (CreatureLightning).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureSplashAndStormTests
    {
        private readonly List<Creature> _creatures = new List<Creature>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();
        }

        [TestMethod]
        public void ACreaturesRocketsMortarsAndBaneGrenadesAreLaunchers()
        {
            Assert.AreEqual(Splash.DefaultRadius, Splash.RadiusOf(Row(ActionId.WeaponRocketlauncher, 11)), "the NeoBot's missile");
            Assert.AreEqual(Splash.DefaultRadius, Splash.RadiusOf(Row(ActionId.WeaponRocketlauncher, 17)), "the AFS Mech's missiles");
            Assert.AreEqual(Splash.DefaultRadius, Splash.RadiusOf(Row(ActionId.WeaponGroundtarget, 1)), "the Bane Mortar");
            Assert.AreEqual(Splash.DefaultRadius, Splash.RadiusOf(Row(ActionId.WeaponAttack, 229)), "the Bane Grenade");

            Assert.AreEqual(0f, Splash.RadiusOf(Row(ActionId.WeaponAttack, 79)), "a Thrax rifle");
            Assert.AreEqual(0f, Splash.RadiusOf(Row(ActionId.WeaponMachinegun, 11)));
            Assert.AreEqual(0f, Splash.RadiusOf((CreatureAction)null));

            using var world = new WorldTestContext();
            var target = Watch(world, 0);
            var bot = Spawn(world, TargetCategory.Hostile, new Vector3(0, 0, -15));

            MissileManager.Instance.MissileLaunch(world.Map, new ActionData(bot, ActionId.WeaponRocketlauncher, 11, target.Player.EntityId, 0), 100,
                splashRadius: Splash.RadiusOf(Row(ActionId.WeaponRocketlauncher, 11)), creatureAction: Row(ActionId.WeaponRocketlauncher, 11));

            var missile = world.Map.QueuedMissiles.Single();
            Assert.AreEqual(Splash.DefaultRadius, missile.SplashRadius);
            Assert.AreEqual(50, missile.SplashDamage);
            world.Map.QueuedMissiles.Clear();
        }

        [TestMethod]
        public void ACreaturesRocketSplashesWhatItMayFightAroundItsTargetForHalf()
        {
            using var world = new WorldTestContext();
            var target = Watch(world, 0);
            var near = Watch(world, 4);
            var far = Watch(world, 6);
            var bot = Spawn(world, TargetCategory.Hostile, new Vector3(0, 0, -15));
            var guard = Spawn(world, TargetCategory.Friendly, new Vector3(0, 0, 3));
            var ally = Spawn(world, TargetCategory.Hostile, new Vector3(-3, 0, 0));

            MissileManager.Instance.MissileTrigger(world.Map, Rocket(bot, target.Player, 100));

            Assert.AreEqual(9950, near.Player.Attributes[Attributes.Health].Current, "half, 4 m from the target");
            Assert.AreEqual(10000, far.Player.Attributes[Attributes.Health].Current, "6 m is outside the 5");
            Assert.AreEqual(9950, guard.Attributes[Attributes.Health].Current, "a creature of the other side");
            Assert.AreEqual(10000, ally.Attributes[Attributes.Health].Current, "not one of its own");
            Assert.AreEqual(10000, bot.Attributes[Attributes.Health].Current);

            var recovery = Sent(target).OfType<WeaponAttackRecovery>().Single();
            CollectionAssert.AreEquivalent(new[] { target.Player.EntityId, near.Player.EntityId, guard.EntityId }, recovery.Missile.Args.HitEntities.ToList());
            Assert.AreEqual(50, recovery.Missile.Args.HitData.Single(hit => hit.EntityId == near.Player.EntityId).FinalAmt);
        }

        [TestMethod]
        public void ARocketThatGoesWideStillSplashesAndAShotWithoutSplashDoesNot()
        {
            using var world = new WorldTestContext();
            var target = Watch(world, 0);
            var near = Watch(world, 3);
            var bot = Spawn(world, TargetCategory.Hostile, new Vector3(0, 0, -15));

            var wide = Rocket(bot, target.Player, 100);
            wide.Missed = true;
            MissileManager.Instance.MissileTrigger(world.Map, wide);

            Assert.AreEqual(10000, target.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(9950, near.Player.Attributes[Attributes.Health].Current);

            var rifle = Rocket(bot, target.Player, 100);
            rifle.ActionId = ActionId.WeaponAttack;
            rifle.ActionArgId = 79;
            rifle.SplashRadius = 0;
            MissileManager.Instance.MissileTrigger(world.Map, rifle);

            Assert.AreEqual(9950, near.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ABrannsBossBoltLeavesAStormThatTicksOnItsTargetAndWhatIsAroundIt()
        {
            using var world = new WorldTestContext();
            var target = Watch(world, 0);
            var near = Watch(world, 5);
            var far = Watch(world, 12);
            var brann = Spawn(world, TargetCategory.Hostile, new Vector3(0, 0, -10));

            WithBrannLightning(() =>
            {
                var storm = Bolt(world, brann, target.Player);

                Assert.IsNotNull(storm);
                Assert.IsFalse(storm.IsBuff);
                Assert.IsTrue(storm.AnnounceOnAttach);
                Assert.AreEqual(2000, storm.TickIntervalMs);
                Assert.IsTrue(storm.ExpiresTick - Environment.TickCount64 is > 5000 and <= 6000, "EFFECT_DURATION_MS 6000");
                Assert.AreSame(brann, storm.Source);

                var before = (target: Health(target), near: Health(near), far: Health(far));
                Sent(target);

                storm.OnTick(world.Map, target.Player, storm);

                Assert.IsTrue(before.target - Health(target) is >= 60 and <= 90, "EFFECT_DAMAGE 60-90 on its holder");
                Assert.IsTrue(before.near - Health(near) is >= 60 and <= 90, "and on a player 5 m from it");
                Assert.AreEqual(before.far, Health(far), "12 m is outside the storm");

                var tick = Sent(target).OfType<GameEffectTickPacket>().Single();
                Assert.AreEqual(GameEffectTickPacket.TickKind.Storm, tick.Kind);
                Assert.AreEqual(target.Player.EntityId, tick.Entries.Single().EntityId);
                Assert.AreEqual(near.Player.EntityId, tick.ArcEntries.Single().EntityId);
            });
        }

        [TestMethod]
        public void TheStormIsTheCreaturesAndEndsWhenItHasGoneAndALesserBoltHasNone()
        {
            using var world = new WorldTestContext();
            var target = Watch(world, 0);
            var brann = Spawn(world, TargetCategory.Hostile, new Vector3(0, 0, -10));

            WithBrannLightning(() =>
            {
                Assert.IsNull(Bolt(world, brann, target.Player, level: 4), "argument 4 has no storm");

                var storm = Bolt(world, brann, target.Player);
                var health = Health(target);

                EntityManager.Instance.UnregisterCreature(brann.EntityId);
                storm.OnTick(world.Map, target.Player, storm);

                Assert.AreEqual(health, Health(target));
                Assert.IsFalse(target.Player.ActiveEffects.ContainsKey(storm.EffectId));
            });

            // The row puts its own numbers on the argument's: twice the bolt, twice the storm.
            var info = BrannLevel5();
            Assert.AreEqual((120, 180), CreatureLightning.StormDamageOf(new CreatureAction { MinDamage = 800, MaxDamage = 1000 }, info));
            Assert.AreEqual((60, 90), CreatureLightning.StormDamageOf(null, info));
        }

        /// <summary>A bolt of CR_BRANN_LIGHTNING at the level landing on the player; the storm it leaves, if any.</summary>
        private static GameEffect Bolt(WorldTestContext world, Creature brann, Manifestation player, uint level = 5)
        {
            var row = new CreatureAction { ActionId = ActionId.CrBrannLightning, ActionArgId = level, MinDamage = 400, MaxDamage = 500 };
            var missile = new Missile
            {
                Source = brann,
                TargetActor = player,
                TargetEntityId = player.EntityId,
                ActionId = ActionId.CrBrannLightning,
                ActionArgId = level,
                AreaDamage = 450,
                DamageA = 450,
                DamageType = DamageType.Electrical,
                CreatureAction = row
            };

            CreatureLightning.Extras(world.Map, missile, new HitData { EntityId = player.EntityId });

            return player.ActiveEffects.Values.LastOrDefault(effect => effect.TypeId == AbilityManager.LightningStormTypeId && effect.Source == brann);
        }

        /// <summary>CR_BRANN_LIGHTNING's arguments 4 and 5 as the world database has them, for the length of the body.</summary>
        private static void WithBrannLightning(Action body)
        {
            var actions = (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;
            actions.TryGetValue(ActionId.CrBrannLightning, out var previous);

            var action = new ActionInfo { ActionId = ActionId.CrBrannLightning, Name = "CR_BRANN_LIGHTNING", Module = CreatureLightning.Module };
            var four = new ActionLevelInfo { ActionId = ActionId.CrBrannLightning, Level = 4, MaxRange = 30 };
            foreach (var (property, value) in new[] { (AbilityProperty.DamageAmountMin, 94), (AbilityProperty.DamageAmountMax, 156), (AbilityProperty.DamageScaleType, 2),
                         (AbilityProperty.DamageType, 13), (AbilityProperty.ArcRadius, 20), (AbilityProperty.ArcDamage, 50) })
                four.Properties[property] = value;
            action.Levels[4] = four;
            action.Levels[5] = BrannLevel5();
            actions[ActionId.CrBrannLightning] = action;

            try
            {
                body();
            }
            finally
            {
                if (previous != null)
                    actions[ActionId.CrBrannLightning] = previous;
                else
                    actions.Remove(ActionId.CrBrannLightning);
            }
        }

        private static ActionLevelInfo BrannLevel5()
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrBrannLightning, Level = 5, MaxRange = 30 };

            foreach (var (property, value) in new[]
                     {
                         (AbilityProperty.DamageAmountMin, 400), (AbilityProperty.DamageAmountMax, 500), (AbilityProperty.PercentageChance, 100),
                         (AbilityProperty.DamageScaleType, 2), (AbilityProperty.DamageType, 13), (AbilityProperty.ExtraDamageType, 7),
                         (AbilityProperty.ExtraDamagePercent, 100), (AbilityProperty.StunChance, 50), (AbilityProperty.StunDuration, 3),
                         (AbilityProperty.ArcRadius, 30), (AbilityProperty.ArcDamage, 350), (AbilityProperty.EffectDurationMs, 6000),
                         (AbilityProperty.EffectIntervalMs, 2000), (AbilityProperty.EffectDamageMin, 60), (AbilityProperty.EffectDamageMax, 90)
                     })
                info.Properties[property] = value;

            return info;
        }

        private static CreatureAction Row(ActionId actionId, uint argId) =>
            new CreatureAction { ActionId = actionId, ActionArgId = argId, MinDamage = 10, MaxDamage = 15 };

        /// <summary>A rocket from the creature that hits its target, as MissileLaunch queues it.</summary>
        private static Missile Rocket(Creature source, Manifestation target, int damage)
        {
            var row = Row(ActionId.WeaponRocketlauncher, 11);

            return new Missile
            {
                Source = source,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = damage,
                AreaDamage = damage,
                DamageType = DamageType.Physical,
                ActionId = row.ActionId,
                ActionArgId = row.ActionArgId,
                CreatureAction = row,
                SplashRadius = Splash.RadiusOf(row),
                SplashDamage = Splash.DamageOf(damage),
                Missed = false
            };
        }

        private static int Health(Client client) => client.Player.Attributes[Attributes.Health].Current;

        private Creature Spawn(WorldTestContext world, TargetCategory category, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        /// <summary>A player with ten thousand health and no armour, in the map's cells, so what is sent near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            foreach (var attribute in new[] { Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();
    }
}
