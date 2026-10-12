using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    // A shot's damage follows the bead: a tenth with none, all of it at a crouched full bead
    // (Accuracy). A shotgun or a propellant gun has no bead to build - the client's cone targeting
    // clears the target, and with no target the ceiling is 0 - so every blast did a tenth of its
    // damage. It does what a full bead from the same stance does now.
    [TestClass]
    [DoNotParallelize]
    public class ConeWeaponDamageTests
    {
        private const int GunDamage = 1000;

        [TestMethod]
        public void AShotgunStandingDoesWhatAStandingFullBeadDoes()
        {
            using var context = Shotgun(out var victim);

            Assert.AreEqual(820, Fire(context), "82%, not a tenth");
        }

        [TestMethod]
        public void AShotgunCrouchedDoesAllOfIt()
        {
            using var context = Shotgun(out var victim);
            context.Client.Player.IsCrouching = true;

            Assert.AreEqual(1000, Fire(context));
        }

        [TestMethod]
        public void AShotgunNeedsNoTimeToAim()
        {
            using var context = Shotgun(out var victim);

            // Drawn this moment: an aimed weapon's bead would be at nothing.
            Accuracy.Reset(context.Client.Player, context.Weapon.ItemTemplate.WeaponInfo.AimRate, System.Environment.TickCount64);

            Assert.AreEqual(820, Fire(context));
        }

        [TestMethod]
        public void AnAimedWeaponStillGoesByItsBead()
        {
            using var context = Shotgun(out var victim);
            context.Weapon.ItemTemplate.WeaponInfo.ToolType = ToolType.Rifle;

            // Aimed at the creature the moment it was drawn: no bead yet, a tenth.
            context.Client.Player.Target = victim.EntityId;
            Accuracy.Reset(context.Client.Player, context.Weapon.ItemTemplate.WeaponInfo.AimRate, System.Environment.TickCount64);

            Assert.AreEqual(100, Fire(context));
        }

        [TestMethod]
        public void TheConeShareIsTheStancesFullBead()
        {
            var player = new Manifestation();

            Assert.AreEqual(0.82, Accuracy.ConeDamageFactor(player), 1e-9);

            player.IsCrouching = true;
            Assert.AreEqual(1.0, Accuracy.ConeDamageFactor(player), 1e-9);
        }

        #region Fixture

        /// <summary>The fixture's gun as a shotgun of a thousand damage, and a creature five metres in front.</summary>
        private static WeaponAmmoContext Shotgun(out Creature victim)
        {
            var context = new WeaponAmmoContext(clip: 20);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            var info = weaponClass.WeaponClassInfo;

            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 20, MinDamage = GunDamage, MaxDamage = GunDamage, DamageType = 1, WeaponAnimConditionCode = 1
            });

            context.Weapon.ItemTemplate.WeaponInfo.ToolType = ToolType.Shotgun;
            victim = Spawn(context.World, new Vector3(0, 0, -5));

            return context;
        }

        /// <summary>One shot, and the damage it carries before anything on the far end takes from it.</summary>
        private static int Fire(WeaponAmmoContext context)
        {
            var manager = new ManifestationManager(context);

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client), "fired");

            return context.World.Map.QueuedMissiles.Single().DamageA;
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
