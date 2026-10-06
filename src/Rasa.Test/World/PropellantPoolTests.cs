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
    using Rasa.Structures.World;
    using Rasa.Test.Gameplay;

    /// <summary>
    /// The pool a propellant gun leaves: "an area damage effect for a moderate amount of time when
    /// fired at the ground for extended periods or on a semi-random basis. Any enemies who linger
    /// within the affected area will take damage over time equal to 1/7 of initial damage".
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PropellantPoolTests
    {
        private const int Never = 99;
        private const int Always = 0;
        private long _now;

        [TestInitialize]
        public void Start()
        {
            PropellantPools.Reset();
            _now = 1_000_000;
            PropellantPools.Now = () => _now;
            PropellantPools.RollPercent = () => Never;
            PropellantPools.Pick = _ => 0;
        }

        [TestCleanup]
        public void End() => PropellantPools.Reset();

        [TestMethod]
        [DataRow(70, 10)]
        [DataRow(76, 10)]
        [DataRow(7, 1)]
        [DataRow(3, 1)]
        [DataRow(0, 0)]
        public void ATickIsASeventhOfThePulse(int pulse, int tick)
        {
            Assert.AreEqual(tick, PropellantPools.TickDamageOf(pulse));
        }

        [TestMethod]
        public void APulseLeavesAPoolOnTheRollAndNoneWithoutIt()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Ice, 10f, new List<Actor>());

            Assert.IsEmpty(PropellantPools.Of(shooter.Player));
            Assert.IsEmpty(Sent(shooter).OfType<GameEffectAttachedPacket>().ToArray());

            PropellantPools.RollPercent = () => PropellantPools.ChancePercent - 1;
            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Ice, 10f, new List<Actor>());

            var pool = PropellantPools.Of(shooter.Player).Single();
            Assert.AreEqual(10, pool.TickDamage);
            Assert.AreEqual(DamageType.Ice, pool.DamageType);

            var attached = Sent(shooter).OfType<GameEffectAttachedPacket>().Single();
            Assert.AreEqual(PropellantPools.PoolTypeId, attached.EffectTypeId);
            Assert.AreEqual((uint)DamageType.Ice, attached.EffectLevel, "the ice pool's FX");
            Assert.AreEqual(shooter.Player.EntityId, attached.SourceId);

            PropellantPools.RollPercent = () => PropellantPools.ChancePercent;
            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Ice, 10f, new List<Actor>());
            Assert.HasCount(1, PropellantPools.Of(shooter.Player), "the roll has to come in under the chance");
        }

        [TestMethod]
        public void AHeldTriggerLeavesOneForCertainAndLettingGoStartsTheCountAgain()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);

            for (var pulse = 1; pulse < PropellantPools.SustainedPulses; pulse++)
                PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());

            Assert.IsEmpty(PropellantPools.Of(shooter.Player));

            // Let go a pulse short, and the next hold starts from nothing.
            PropellantPools.Stopped(shooter.Player);

            for (var pulse = 1; pulse < PropellantPools.SustainedPulses; pulse++)
                PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());

            Assert.IsEmpty(PropellantPools.Of(shooter.Player));

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());

            Assert.HasCount(1, PropellantPools.Of(shooter.Player));

            // And the pool it left starts the count again too.
            _now += PropellantPools.DurationMs + PropellantPools.FadeMs + 1000;
            PropellantPools.Worker(world.Map);
            PropellantPools.Worker(world.Map);
            Assert.IsEmpty(PropellantPools.Of(shooter.Player));

            for (var pulse = 1; pulse < PropellantPools.SustainedPulses; pulse++)
                PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());

            Assert.IsEmpty(PropellantPools.Of(shooter.Player));
        }

        [TestMethod]
        public void ThePoolLandsUnderATargetThePulseHitOrOnTheGroundAhead()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var near = Spawn(world, new Vector3(2, 0, -4), TargetCategory.Hostile);
            var far = Spawn(world, new Vector3(-30, 0, -40), TargetCategory.Hostile);
            PropellantPools.RollPercent = () => Always;

            // With nothing hit: six tenths of the way along the gun's reach, the way they face (rotation 0 is -Z).
            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
            Assert.AreEqual(new Vector3(0, 0, -6), PropellantPools.Of(shooter.Player).Single().Position);

            // Under one of those hit, well clear of the first.
            PropellantPools.Pick = count => count - 1;
            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor> { near, far });

            var pools = PropellantPools.Of(shooter.Player);
            Assert.HasCount(2, pools);
            Assert.AreEqual(far.Position, pools[1].Position);
        }

        [TestMethod]
        public void AnEnemyWhoLingersTakesASeventhEachSecondSevenTimesAndNobodyElseDoes()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var inside = Spawn(world, new Vector3(0, 0, -6), TargetCategory.Hostile);
            var edge = Spawn(world, new Vector3(PropellantPools.Radius - 0.5f, 0, -6), TargetCategory.Hostile);
            var outside = Spawn(world, new Vector3(PropellantPools.Radius + 0.5f, 0, -6), TargetCategory.Hostile);
            var friend = Spawn(world, new Vector3(0, 0, -7), TargetCategory.Friendly);
            PropellantPools.RollPercent = () => Always;

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
            var attached = Sent(shooter).OfType<GameEffectAttachedPacket>().Single();
            PropellantPools.RollPercent = () => Never;

            // Nothing before its first second is up.
            _now += PropellantPools.TickIntervalMs - 1;
            PropellantPools.Worker(world.Map);
            Assert.AreEqual(1000, Health(inside));
            Assert.IsEmpty(Sent(shooter).OfType<GameEffectTickPacket>().ToArray());

            _now += 1;
            PropellantPools.Worker(world.Map);

            Assert.AreEqual(990, Health(inside));
            Assert.AreEqual(990, Health(edge));
            Assert.AreEqual(1000, Health(outside));
            Assert.AreEqual(1000, Health(friend));

            var tick = Sent(shooter).OfType<GameEffectTickPacket>().Single();
            Assert.AreEqual(attached.EffectId, tick.EffectId);
            Assert.AreEqual(GameEffectTickPacket.TickKind.Damage, tick.Kind);
            CollectionAssert.AreEquivalent(new[] { inside.EntityId, edge.EntityId }, tick.Entries.Select(entry => entry.EntityId).ToArray());
            Assert.IsTrue(tick.Entries.All(entry => entry.Amount == 10 && entry.DamageType == DamageType.Fire && !entry.IsCritical));

            // The worker runs twice a second: a tick is by the pool's clock, not the worker's.
            _now += PropellantPools.TickIntervalMs / 2;
            PropellantPools.Worker(world.Map);
            Assert.AreEqual(990, Health(inside));

            // The one at the edge walks out; the other stays to the end.
            edge.Position = outside.Position;

            for (var second = 2; second <= 12; second++)
            {
                _now = 1_000_000 + second * PropellantPools.TickIntervalMs;
                PropellantPools.Worker(world.Map);
            }

            Assert.AreEqual(1000 - 7 * 10, Health(inside), "seven ticks: the pulse once more");
            Assert.AreEqual(990, Health(edge));
            Assert.IsEmpty(PropellantPools.Of(shooter.Player));

            var after = Sent(shooter);
            Assert.AreEqual(attached.EffectId, after.OfType<GameEffectDetachedPacket>().Single().EffectId);
            Assert.HasCount(6, after.OfType<GameEffectTickPacket>().ToArray());
        }

        [TestMethod]
        public void ATickKillsWhatItFinishes()
        {
            using var world = new WorldTestContext();
            // Not in a cell's client list, so the kill pays this fixture nothing; the watcher sees the ticks.
            var shooter = Watch(world, 0, listed: false);
            var watcher = Watch(world, 2);
            var weak = Spawn(world, new Vector3(0, 0, -6), TargetCategory.Hostile, health: 15);
            PropellantPools.RollPercent = () => Always;

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
            PropellantPools.RollPercent = () => Never;
            Sent(watcher);

            _now += PropellantPools.TickIntervalMs;
            PropellantPools.Worker(world.Map);
            Assert.AreEqual(5, Health(weak));

            _now += PropellantPools.TickIntervalMs;
            PropellantPools.Worker(world.Map);

            Assert.AreEqual(0, Health(weak));
            Assert.AreEqual(CharacterState.Dead, weak.State);
            var ticks = Sent(watcher).OfType<GameEffectTickPacket>().ToArray();
            Assert.HasCount(2, ticks);
            Assert.IsFalse(ticks[0].Entries.Single().DeathBlow);
            Assert.IsTrue(ticks[1].Entries.Single().DeathBlow);

            // And a dead one is not burnt again.
            _now += PropellantPools.TickIntervalMs;
            PropellantPools.Worker(world.Map);
            Assert.IsEmpty(Sent(watcher).OfType<GameEffectTickPacket>().ToArray());
            Assert.HasCount(1, PropellantPools.Of(shooter.Player), "the pool burns on");
        }

        [TestMethod]
        public void APulseOntoTheShootersOwnPoolFeedsIt()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var victim = Spawn(world, new Vector3(0, 0, -6), TargetCategory.Hostile);
            PropellantPools.RollPercent = () => Always;

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
            Sent(shooter);

            // Five seconds on, another lands within two radii of it: no second pool, and no second effect.
            for (var second = 1; second <= 5; second++)
            {
                _now += PropellantPools.TickIntervalMs;
                PropellantPools.Worker(world.Map);
            }

            PropellantPools.OnPulse(world.Map, shooter.Player, 140, DamageType.Fire, 10f, new List<Actor> { victim });

            var pool = PropellantPools.Of(shooter.Player).Single();
            Assert.AreEqual(20, pool.TickDamage, "as hot as the hotter of the two");
            Assert.IsEmpty(Sent(shooter).OfType<GameEffectAttachedPacket>().ToArray());

            // A weaker pulse does not cool it.
            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor> { victim });
            Assert.AreEqual(20, PropellantPools.Of(shooter.Player).Single().TickDamage);
            PropellantPools.RollPercent = () => Never;

            // It burns seven seconds from when it was fed, one tick a second throughout.
            var before = Health(victim);
            Assert.AreEqual(1000 - 5 * 10, before);

            for (var second = 1; second <= 12; second++)
            {
                _now += PropellantPools.TickIntervalMs;
                PropellantPools.Worker(world.Map);
            }

            Assert.AreEqual(before - 7 * 20, Health(victim));
            Assert.IsEmpty(PropellantPools.Of(shooter.Player));
        }

        [TestMethod]
        public void APoolOfAnotherKindOrAnotherShooterIsNotFed()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var other = Watch(world, 1);
            PropellantPools.RollPercent = () => Always;

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Ice, 10f, new List<Actor>());
            PropellantPools.OnPulse(world.Map, other.Player, 70, DamageType.Fire, 10f, new List<Actor>());

            Assert.HasCount(2, PropellantPools.Of(shooter.Player));
            Assert.HasCount(1, PropellantPools.Of(other.Player));
        }

        [TestMethod]
        public void AShooterHasThreeAndTheOldestGoesForAFourth()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            PropellantPools.RollPercent = () => Always;
            var attached = new List<int>();

            for (var pool = 0; pool < PropellantPools.MaxPerShooter + 1; pool++)
            {
                // Each well clear of the last.
                shooter.Player.Position = new Vector3(pool * 20, 0, 0);
                PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
                _now += 100;

                if (pool < PropellantPools.MaxPerShooter)
                    attached.Add(Sent(shooter).OfType<GameEffectAttachedPacket>().Single().EffectId);
            }

            var pools = PropellantPools.Of(shooter.Player);
            Assert.HasCount(PropellantPools.MaxPerShooter, pools);
            Assert.IsFalse(pools.Any(pool => pool.Position.X == 0), "the first is the one that went");

            var sent = Sent(shooter);
            Assert.AreEqual(attached[0], sent.OfType<GameEffectDetachedPacket>().Single().EffectId);
            Assert.HasCount(1, sent.OfType<GameEffectAttachedPacket>().ToArray());
        }

        [TestMethod]
        public void ThePoolsGoWithAShooterWhoLeavesTheMap()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var watcher = Watch(world, 2);
            var victim = Spawn(world, new Vector3(0, 0, -6), TargetCategory.Hostile);
            PropellantPools.RollPercent = () => Always;

            PropellantPools.OnPulse(world.Map, shooter.Player, 70, DamageType.Fire, 10f, new List<Actor>());
            var attached = Sent(watcher).OfType<GameEffectAttachedPacket>().Single();

            shooter.Player.MapChannel = null;
            _now += PropellantPools.TickIntervalMs;
            PropellantPools.Worker(world.Map);

            Assert.AreEqual(1000, Health(victim));
            Assert.IsEmpty(PropellantPools.Of(shooter.Player));
            Assert.AreEqual(attached.EffectId, Sent(watcher).OfType<GameEffectDetachedPacket>().Single().EffectId);
        }

        [TestMethod]
        public void FiringAPropellantGunLeavesThePoolOfItsPulse()
        {
            using var context = new WeaponAmmoContext(clip: 100);
            var world = context.World;
            var shooter = context.Client;
            foreach (var attribute in new[] { Attributes.Armor, Attributes.Power, Attributes.Regen })
                shooter.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            var victim = Spawn(world, new Vector3(0, 0, -5), TargetCategory.Hostile);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            var previous = weaponClass.WeaponClassInfo;
            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = (uint)ActionId.WeaponFlamethrower, AttackActionArgId = 1,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 100, MinDamage = 700, MaxDamage = 700, DamageType = (byte)DamageType.Fire, WeaponAnimConditionCode = 1
            });

            try
            {
                var manager = new ManifestationManager(context);
                PropellantPools.RollPercent = () => Always;

                Assert.IsTrue(manager.PlayerTryFireWeapon(shooter));

                // The gun's own effect on the shooter, and the pool's under the one it hit.
                var attached = Sent(shooter).OfType<GameEffectAttachedPacket>().ToArray();
                Assert.HasCount(1, attached.Where(packet => packet.EffectTypeId == ConstantFire.PropellantTypeId).ToArray());
                var pool = attached.Single(packet => packet.EffectTypeId == PropellantPools.PoolTypeId);
                Assert.AreEqual((uint)DamageType.Fire, pool.EffectLevel);

                var left = PropellantPools.Of(shooter.Player).Single();
                Assert.AreEqual(victim.Position, left.Position);
                // Fired with no bead the pulse is a tenth of the gun's 700 (Accuracy), and the pool a seventh of that.
                Assert.AreEqual(PropellantPools.TickDamageOf(70), left.TickDamage, "a seventh of the pulse as fired");

                // Held without the roll: the fifth pulse since that pool feeds it rather than leaving another.
                PropellantPools.RollPercent = () => Never;

                for (var pulse = 0; pulse < PropellantPools.SustainedPulses; pulse++)
                {
                    shooter.Player.NextShotAt = 0;
                    Assert.IsTrue(manager.PlayerTryFireWeapon(shooter));
                }

                Assert.HasCount(1, PropellantPools.Of(shooter.Player));
                Assert.IsEmpty(Sent(shooter).OfType<GameEffectAttachedPacket>().ToArray());

                manager.StopAutoFire(shooter);
                Assert.HasCount(1, PropellantPools.Of(shooter.Player), "the pool outlasts the trigger");

                var before = Health(victim);
                _now += PropellantPools.TickIntervalMs;
                PropellantPools.Worker(world.Map);
                Assert.AreEqual(before - left.TickDamage, Health(victim));
            }
            finally
            {
                ConstantFire.Stop(shooter, release: false);
                weaponClass.WeaponClassInfo = previous;
            }
        }

        private static int Health(Creature creature) => creature.Attributes[Attributes.Health].Current;

        private static PythonPacket[] Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToArray();

        /// <summary>A creature with a thousand health and no armour, in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, Vector3 position, TargetCategory category, int health = 1000)
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
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, health, health, health, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client standing in the map's cells, so what is sent near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x, bool listed = true)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            if (listed)
                CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
