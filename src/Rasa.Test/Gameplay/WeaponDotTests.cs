using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    // An injection gun's or a net gun's hit put nothing on what it hit: the client's
    // INJECTION_GUN_*_EFFECT and NET_GUN_DOT_EFFECT were never seen, and the guns did all their
    // damage at once. An injection gun's shot is now half on the hit and half over 6 s in three
    // ticks; a net gun's three quarters on the hit and a quarter over its 3 s hold (WeaponDots).
    [TestClass]
    [DoNotParallelize]
    public class WeaponDotTests
    {
        #region The shot

        [TestMethod]
        [DataRow(DamageType.Fire, WeaponDots.InjectionFireTypeId)]
        [DataRow(DamageType.Ice, WeaponDots.InjectionIceTypeId)]
        [DataRow(DamageType.Virulent, WeaponDots.InjectionViralTypeId)]
        [DataRow(DamageType.EMP, WeaponDots.InjectionEmpTypeId)]
        [DataRow(DamageType.Electrical, WeaponDots.InjectionElectricTypeId)]
        [DataRow(DamageType.Sonic, WeaponDots.InjectionSonicTypeId)]
        public void AnInjectionGunsShotIsHalfOnTheHitAndHalfInItsElementsDose(DamageType damageType, int typeId)
        {
            using var context = Gun(WeaponSkills.InjectionGuns, damageType);

            var shot = Fire(context);

            Assert.AreEqual(50, shot.DamageA, "a tenth of a thousand, drawn this moment: half of it");
            Assert.AreEqual(50, shot.DotDamage);
            Assert.AreEqual(typeId, shot.Dot.TypeId);
            Assert.AreEqual(6000, shot.Dot.DurationMs);
            Assert.AreEqual(3, shot.Dot.Ticks);
            Assert.AreEqual(0, shot.RootMs);
        }

        [TestMethod]
        public void ANetGunsShotIsThreeQuartersOnTheHitAndAQuarterOverItsHold()
        {
            using var context = Gun(WeaponSkills.NetGuns, DamageType.Fire);

            var shot = Fire(context);

            Assert.AreEqual(75, shot.DamageA);
            Assert.AreEqual(25, shot.DotDamage);
            Assert.AreEqual(WeaponDots.NetGunTypeId, shot.Dot.TypeId);
            Assert.AreEqual(CrowdControl.NetGunRootMs, shot.Dot.DurationMs);
            Assert.AreEqual(CrowdControl.NetGunRootMs, shot.RootMs, "and it still holds");
        }

        [TestMethod]
        public void AnyOtherGunKeepsAllOfItsDamageOnTheHit()
        {
            using var context = Gun(WeaponSkills.Firearms, DamageType.Fire);

            var shot = Fire(context);

            Assert.AreEqual(100, shot.DamageA);
            Assert.AreEqual(0, shot.DotDamage);
            Assert.IsNull(shot.Dot);
        }

        #endregion

        #region The hit

        [TestMethod]
        public void AnInjectionGunsHitLeavesItsDoseTickingForTheRest()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            var shot = Shoot(world, shooter, creature, WeaponDots.Of(WeaponSkills.InjectionGuns, DamageType.Fire), 300);
            var now = Environment.TickCount64;

            Assert.AreEqual(9900, Health(creature), "the hit");

            var dose = creature.ActiveEffects.Values.Single(e => e.TypeId == WeaponDots.InjectionFireTypeId);
            Assert.AreEqual(100, dose.TickDamageMin);
            Assert.AreEqual(100, dose.TickDamageMax);
            Assert.AreEqual(DamageType.Fire, dose.TickDamageType);
            Assert.AreEqual(2000, dose.TickIntervalMs);
            Assert.AreEqual(6000, dose.ExpiresTick - now, 50);
            Assert.AreSame(shooter.Player, dose.Source);
            Assert.IsFalse(dose.IsBuff);
            Assert.AreEqual(3, WeaponDots.TicksLeft(dose));

            CollectionAssert.Contains(shot.Args.HitData.Single().TargetEffectIds, (uint)WeaponDots.InjectionFireTypeId, "announced by the hit");

            var shift = dose.NextTickTick - Environment.TickCount64 + 1;
            dose.NextTickTick -= shift;
            dose.ExpiresTick -= shift;
            GameEffectManager.Instance.DoWork(world.Map, 100);

            Assert.AreEqual(9800, Health(creature), "a tick");
            Assert.AreEqual(2, WeaponDots.TicksLeft(dose));
        }

        [TestMethod]
        public void ItDealsAllThreeTicksTheLastOnItsExpiry()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            Shoot(world, shooter, creature, WeaponDots.Of(WeaponSkills.InjectionGuns, DamageType.Fire), 300);
            var dose = creature.ActiveEffects.Values.Single(e => e.TypeId == WeaponDots.InjectionFireTypeId);

            for (var tick = 0; tick < 3; tick++)
            {
                var now = Environment.TickCount64;
                var shift = dose.NextTickTick - now + 1;
                dose.NextTickTick -= shift;
                dose.ExpiresTick -= shift;
                GameEffectManager.Instance.DoWork(world.Map, 100);
            }

            Assert.AreEqual(9600, Health(creature), "the hit and three ticks: all of the 400");
            Assert.IsFalse(creature.ActiveEffects.ContainsKey(dose.EffectId), "and it is gone");
        }

        [TestMethod]
        public void ASecondHitStartsItAgainWithWhatWasStillToCome()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var spec = WeaponDots.Of(WeaponSkills.InjectionGuns, DamageType.Fire);

            Shoot(world, shooter, creature, spec, 300);
            var first = creature.ActiveEffects.Values.Single(e => e.TypeId == WeaponDots.InjectionFireTypeId);

            // One of its three ticks has been dealt: 200 still to come.
            first.NextTickTick += first.TickIntervalMs;

            Shoot(world, shooter, creature, spec, 300);

            var second = creature.ActiveEffects.Values.Single(e => e.TypeId == WeaponDots.InjectionFireTypeId);
            Assert.AreNotSame(first, second);
            Assert.AreEqual(167, second.TickDamageMin, "500 over three ticks");
            Assert.AreEqual(3, WeaponDots.TicksLeft(second));
        }

        [TestMethod]
        public void AnotherShootersDoseTakesUpWhatTheFirstHadLeft()
        {
            using var world = new WorldTestContext();
            var one = Watch(world);
            var other = Watch(world, 2);
            var creature = Spawn(world, new Vector3(0, 0, -5));
            var spec = WeaponDots.Of(WeaponSkills.InjectionGuns, DamageType.Fire);

            Shoot(world, one, creature, spec, 300);
            Shoot(world, other, creature, spec, 300);

            var dose = creature.ActiveEffects.Values.Single(e => e.TypeId == WeaponDots.InjectionFireTypeId);
            Assert.AreEqual(200, dose.TickDamageMin, "one effect of a type: all 600, none lost");
            Assert.AreSame(other.Player, dose.Source);
        }

        [TestMethod]
        public void AShotThatMissesLeavesNothing()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            Shoot(world, shooter, creature, WeaponDots.Of(WeaponSkills.InjectionGuns, DamageType.Fire), 300, missed: true);

            Assert.AreEqual(10000, Health(creature));
            Assert.IsFalse(creature.ActiveEffects.Values.Any(e => e.TypeId == WeaponDots.InjectionFireTypeId));
        }

        [TestMethod]
        public void ANetGunsHitHoldsAndBurnsForTheSameThreeSeconds()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world);
            var creature = Spawn(world, new Vector3(0, 0, -5));

            Shoot(world, shooter, creature, WeaponDots.Of(WeaponSkills.NetGuns, DamageType.Electrical), 90, rootMs: CrowdControl.NetGunRootMs, damageType: DamageType.Electrical);

            Assert.IsTrue(creature.ActiveEffects.Values.Any(e => e.TypeId == CrowdControl.NetGunRootTypeId));

            var burn = creature.ActiveEffects.Values.Single(e => e.TypeId == WeaponDots.NetGunTypeId);
            Assert.AreEqual(30, burn.TickDamageMin);
            Assert.AreEqual(1000, burn.TickIntervalMs);
            Assert.AreEqual(DamageType.Electrical, burn.TickDamageType);
        }

        [TestMethod]
        public void TheHitKeepsAtLeastOneDamage()
        {
            var spec = WeaponDots.Of(WeaponSkills.InjectionGuns, DamageType.Fire);

            var one = 1;
            Assert.AreEqual(0, WeaponDots.Split(ref one, spec));
            Assert.AreEqual(1, one);

            var three = 3;
            Assert.AreEqual(2, WeaponDots.Split(ref three, spec), "half of 3, rounded to even, is 2");
            Assert.AreEqual(1, three);

            var none = 100;
            Assert.AreEqual(0, WeaponDots.Split(ref none, null));
            Assert.AreEqual(100, none);
        }

        #endregion

        #region Fixture

        private static WeaponAmmoContext Gun(int skillId, DamageType damageType)
        {
            var context = new WeaponAmmoContext(clip: 20);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];

            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 20, MinDamage = 1000, MaxDamage = 1000, DamageType = (byte)damageType, WeaponAnimConditionCode = 1
            });

            context.Weapon.ItemTemplate.EquipableInfo = new EquipableInfo(skillId, 0);

            var victim = SpawnFor(context.World, new Vector3(0, 0, -3));
            context.Client.Player.Target = victim.EntityId;
            Accuracy.Reset(context.Client.Player, context.Weapon.ItemTemplate.WeaponInfo.AimRate, Environment.TickCount64);

            return context;
        }

        private static Missile Fire(WeaponAmmoContext context)
        {
            Assert.IsTrue(new ManifestationManager(context).PlayerTryFireWeapon(context.Client), "fired");

            return context.World.Map.QueuedMissiles.Single();
        }

        private static Missile Shoot(WorldTestContext world, Client shooter, Creature target, WeaponDots.Spec dot, int dotDamage, int rootMs = 0, bool missed = false, DamageType damageType = DamageType.Fire)
        {
            var missile = new Missile
            {
                Source = shooter.Player,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = 100,
                DamageType = damageType,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = 0,
                RootMs = rootMs,
                Dot = dot,
                DotDamage = dotDamage,
                Missed = missed
            };

            MissileManager.Instance.MissileTrigger(world.Map, missile);
            WorldTestContext.Drain(shooter);

            return missile;
        }

        private static int Health(Creature creature) => creature.Attributes[Attributes.Health].Current;

        private static Client Watch(WorldTestContext world, float x = 0)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        /// <summary>A hostile creature with ten thousand health and no armour, in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, Vector3 position)
        {
            var creature = SpawnFor(world, position);
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            return creature;
        }

        private static Creature SpawnFor(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 5000, 5000, 5000, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        #endregion
    }
}
