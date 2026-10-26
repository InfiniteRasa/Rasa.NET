using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// A Critical Death window is opened at a damage type the creature's skeleton has animations
    /// for (CritDeathManager.LevelFor, CritDeathAnimations): the client plays nothing for one it
    /// has none for.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CritDeathAnimationTests
    {
        private const uint ThraxSoldier = 3762;       // Bane_Thrax_Soldier_Pistol, mesh 30407
        private const uint HominisMachina = 3868;     // Bane_Hominis_Machina, mesh 15868
        private const uint Treeback = 6038;           // Creature_Treeback_Adult, mesh 16803
        private const uint Maw = 6332;                // Creature_Maw, mesh 17298

        private static readonly (uint ClassId, string Name, uint Mesh)[] Classes =
        {
            (ThraxSoldier, "Bane_Thrax_Soldier_Pistol", 30407),
            (HominisMachina, "Bane_Hominis_Machina", 15868),
            (Treeback, "Creature_Treeback_Adult", 16803),
            (Maw, "Creature_Maw", 17298),
        };

        [TestMethod]
        public void EachSkeletonHasTheTypesTheClientAnimates()
        {
            // Every type but EMP on a Thrax; every type on a Machina; no Physical or Sonic on a
            // Treeback; no Physical or Ice on a Maw.
            CollectionAssert.AreEqual(new[] { DamageType.Physical, DamageType.Fire, DamageType.Ice, DamageType.Virulent, DamageType.Laser, DamageType.Sonic, DamageType.Electrical },
                (System.Collections.ICollection)CritDeathAnimations.For(30407));
            CollectionAssert.AreEqual(new[] { DamageType.Physical, DamageType.Fire, DamageType.Ice, DamageType.Virulent, DamageType.EMP, DamageType.Laser, DamageType.Sonic, DamageType.Electrical },
                (System.Collections.ICollection)CritDeathAnimations.For(15868));
            CollectionAssert.AreEqual(new[] { DamageType.Fire, DamageType.Ice, DamageType.Virulent, DamageType.Laser, DamageType.Electrical },
                (System.Collections.ICollection)CritDeathAnimations.For(16803));
            CollectionAssert.AreEqual(new[] { DamageType.Fire, DamageType.Virulent, DamageType.Laser, DamageType.Sonic, DamageType.Electrical },
                (System.Collections.ICollection)CritDeathAnimations.For(17298));

            // A Stalker's skeleton has none; a mesh the client does not list is unknown.
            Assert.AreEqual(0, CritDeathAnimations.For(12591).Count);
            Assert.IsNull(CritDeathAnimations.For(776));
        }

        [TestMethod]
        [DataRow(ThraxSoldier, DamageType.EMP, DamageType.Physical, DisplayName = "EMP on a Thrax: Physical")]
        [DataRow(ThraxSoldier, DamageType.Fire, DamageType.Fire, DisplayName = "Fire on a Thrax: Fire")]
        [DataRow(ThraxSoldier, DamageType.Virulent, DamageType.Virulent, DisplayName = "Virulent on a Thrax: Virulent")]
        [DataRow(HominisMachina, DamageType.EMP, DamageType.EMP, DisplayName = "EMP on a Machina: EMP")]
        [DataRow(HominisMachina, DamageType.Virulent, DamageType.Virulent, DisplayName = "Virulent on a Machina: Virulent")]
        [DataRow(Treeback, DamageType.Physical, DamageType.Fire, DisplayName = "Physical on a Treeback, which has none: its first, Fire")]
        [DataRow(Treeback, DamageType.Sonic, DamageType.Fire, DisplayName = "Sonic on a Treeback: Fire")]
        [DataRow(Maw, DamageType.Ice, DamageType.Fire, DisplayName = "Ice on a Maw: Fire")]
        [DataRow(Maw, DamageType.Sonic, DamageType.Sonic, DisplayName = "Sonic on a Maw: Sonic")]
        public void TheWindowIsAtATypeTheSkeletonHas(uint classId, DamageType hit, DamageType level)
        {
            var saved = Register();
            try
            {
                var creature = new Creature { EntityClass = (EntityClasses)classId };

                Assert.AreEqual((uint)level, CritDeathManager.LevelFor(hit, creature));
            }
            finally
            {
                Restore(saved);
            }
        }

        [TestMethod]
        public void AClassTheServerHasNoMeshForKeepsTheTypeItWasHitWith()
        {
            var creature = new Creature { EntityClass = (EntityClasses)999_999 };

            Assert.AreEqual((uint)DamageType.EMP, CritDeathManager.LevelFor(DamageType.EMP, creature));
            Assert.AreEqual((uint)DamageType.Physical, CritDeathManager.LevelFor((DamageType)0, creature));
        }

        [TestMethod]
        public void AnEmpStunOnAThraxNearDeathOpensAPhysicalWindow()
        {
            var saved = Register();
            try
            {
                using var world = new WorldTestContext();
                var opener = world.CreateClient(x: 200);
                var thrax = new Creature
                {
                    Name = "Thrax",
                    TargetCategory = TargetCategory.Hostile,
                    MapContextId = world.Map.MapInfo.MapContextId,
                    RuntimeMapChannel = world.Map,
                    Position = new Vector3(10, 0, 0),
                    EntityClass = (EntityClasses)ThraxSoldier,
                    State = CharacterState.Idle,
                    Level = 1,
                    AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
                };
                thrax.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 5, 2, 5);
                EntityManager.Instance.RegisterEntity(thrax.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(thrax);
                EntityManager.Instance.RegisterActor(thrax.EntityId, thrax);
                thrax.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
                var seed = CellManager.Instance.GetCellSeed(thrax.Position);
                thrax.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
                CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(thrax);

                Stuns.Apply(world.Map, thrax, opener.Player, Stuns.StunTypeId, 3000, DamageType.EMP);

                Assert.AreEqual(CharacterState.Dying, thrax.State, "the window opens");
                Assert.AreEqual((uint)DamageType.Physical, CritDeathManager.PreDeathOf(thrax).EffectLevel);
            }
            finally
            {
                Restore(saved);
            }
        }

        private static Dictionary<EntityClasses, EntityClass> Register()
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var saved = new Dictionary<EntityClasses, EntityClass>();

            foreach (var (classId, name, mesh) in Classes)
            {
                var key = (EntityClasses)classId;
                saved[key] = classes.TryGetValue(key, out var existing) ? existing : null;
                classes[key] = new EntityClass(classId, name, mesh, 2, new List<AugmentationType> { AugmentationType.Creature }, true);
            }

            return saved;
        }

        private static void Restore(Dictionary<EntityClasses, EntityClass> saved)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            foreach (var (key, entityClass) in saved)
            {
                if (entityClass == null)
                    classes.Remove(key);
                else
                    classes[key] = entityClass;
            }
        }
    }
}
