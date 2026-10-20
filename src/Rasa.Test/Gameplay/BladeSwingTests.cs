using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    // A blade's attack (itemtemplate_weapon.attack_type Melee) was fired as a shot: it hit for
    // the bead - a tenth drawn this moment, 82% standing - could miss a moving target, and was cut
    // by cover. It is a swing now, as the alternate attack is: the whole of its damage, never
    // rolled for a miss, and through cover.
    [TestClass]
    [DoNotParallelize]
    public class BladeSwingTests
    {
        private const int BladeDamage = 1000;

        [TestCleanup]
        public void Cleanup() => ToHit.Roll = Stuns.Roll;

        [TestMethod]
        public void ABladeDrawnThisMomentSwingsForAllOfItsDamage()
        {
            using var context = Weapon(AttackType.Melee, out var victim);

            var swing = Fire(context);

            Assert.IsTrue(swing.IsMelee);
            Assert.AreEqual(BladeDamage, swing.DamageA, "no bead to build");
        }

        [TestMethod]
        public void AnAimedGunInTheSameHandsStillGoesByItsBead()
        {
            using var context = Weapon(AttackType.Ranged, out var victim);

            var shot = Fire(context);

            Assert.IsFalse(shot.IsMelee);
            Assert.AreEqual(BladeDamage / 10, shot.DamageA, "drawn this moment: a tenth");
        }

        [TestMethod]
        public void ASwingAtSomethingRunningIsNotRolledForAMiss()
        {
            using var context = Weapon(AttackType.Melee, out var victim);
            victim.Controller.LastMovement = new Movement(victim.Position, 6f, Movement.FastTurn, Vector2.Zero);
            victim.IsRunning = true;

            var rolled = new List<int>();
            ToHit.Roll = percent => { rolled.Add(percent); return true; };

            var swing = Fire(context);

            Assert.AreNotEqual(true, swing.Missed);
            Assert.AreEqual(0, rolled.Count, "a swing has no to-hit roll");
        }

        [TestMethod]
        [DataRow(AttackType.Melee, 50.0)]
        [DataRow(AttackType.Ranged, 20.0)]
        public void ASwingHasNoRecoilAndAShotDoes(AttackType attackType, double bead)
        {
            using var context = Weapon(attackType, out var victim);
            var player = context.Client.Player;
            var info = context.Weapon.ItemTemplate.WeaponInfo;
            info.AimRate = 0;
            info.RecoilAmount = 30;

            var now = System.Environment.TickCount64;
            Accuracy.Reset(player, info.AimRate, now);
            player.AccuracyValue = 50;

            Fire(context);

            Assert.AreEqual(bead, player.AccuracyValue, 1e-9);
        }

        #region Fixture

        /// <summary>The fixture's weapon at a thousand damage with the given attack type, aimed at a creature three metres in front.</summary>
        private static WeaponAmmoContext Weapon(AttackType attackType, out Creature victim)
        {
            var context = new WeaponAmmoContext(clip: 20);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];

            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 20, MinDamage = BladeDamage, MaxDamage = BladeDamage, DamageType = 1, WeaponAnimConditionCode = 1
            });

            var info = context.Weapon.ItemTemplate.WeaponInfo;
            info.AttackType = attackType;
            info.Range = attackType == AttackType.Melee ? 4u : 80u;

            victim = Spawn(context.World, new Vector3(0, 0, -3));
            context.Client.Player.Target = victim.EntityId;
            Accuracy.Reset(context.Client.Player, info.AimRate, System.Environment.TickCount64);

            return context;
        }

        private static Missile Fire(WeaponAmmoContext context)
        {
            var manager = new ManifestationManager(context);

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client), "fired");

            return context.World.Map.QueuedMissiles.Single();
        }

        private static Creature Spawn(WorldTestContext world, Vector3 position)
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
