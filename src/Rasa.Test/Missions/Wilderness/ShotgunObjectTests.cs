using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    /// <summary>
    /// A cone weapon hits the objects a weapon can damage in its cone (MissileManager.AimCone,
    /// PracticeTargetManager.TargetsInCone). A shotgun cannot lock on to anything, and its cone only
    /// ever looked for creatures and enemy players, so Cache of the Day's fuel containers (665),
    /// which a rifle or a pistol destroys, took nothing from it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ShotgunObjectTests
    {
        private const uint CacheOfTheDay = 665;

        /// <summary>Between fuel-1 (-269, 76) and fuel-2 (-265, 76), six metres short of them, facing them.</summary>
        private static readonly Vector3 Stand = new(-267f, 170.3f, 70f);

        /// <summary>Narrow enough to take fuel-1, fuel-2 and fuel-9 (-267, 81) and leave fuel-3 (-261, 76, 45 degrees off) and fuel-10 (-259, 81).</summary>
        private const float HalfAngle = 20f;

        [TestMethod]
        public void AShotgunDamagesEveryFuelContainerInItsConeAndNoOther()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Fire(harness, 40);

            foreach (var (role, obj) in fuel)
                Assert.AreEqual(role is "fuel-1" or "fuel-2" or "fuel-9" ? 60U : 100U, obj.CurrentHitPoints, role);
        }

        [TestMethod]
        public void AShotgunDestroysThemAndTheMissionCountsEach()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Fire(harness, 100);

            foreach (var role in new[] { "fuel-1", "fuel-2", "fuel-9" })
            {
                Assert.AreEqual(0U, fuel[role].CurrentHitPoints, role);
                Assert.AreEqual(UseObjectState.StateDestroyed, fuel[role].StateId, role);
            }

            Assert.AreEqual(3U, harness.Client.Player.Missions[CacheOfTheDay].Objectives[1].Counters[0]);

            // Destroyed, they are no longer in the cone's reach: the next shot takes nothing more.
            Fire(harness, 100);
            Assert.AreEqual(3U, harness.Client.Player.Missions[CacheOfTheDay].Objectives[1].Counters[0]);
            Assert.AreEqual(100U, fuel["fuel-3"].CurrentHitPoints);
        }

        /// <summary>With a creature in the cone the missile flies at the creature, and the containers take the shot all the same.</summary>
        [TestMethod]
        public void WithACreatureInTheConeTheContainersAreStillHit()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            var creature = Spawn(harness, Stand + new Vector3(0f, 0f, 3f));

            Fire(harness, 40);

            Assert.IsTrue(creature.Attributes[Attributes.Health].Current < 5000, "the creature was hit");
            foreach (var role in new[] { "fuel-1", "fuel-2", "fuel-9" })
                Assert.AreEqual(60U, fuel[role].CurrentHitPoints, role);
        }

        /// <summary>Facing away from them, nothing.</summary>
        [TestMethod]
        public void BehindTheShooterTheyAreNotHit()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Fire(harness, 40, rotation: 0);

            Assert.IsTrue(fuel.Values.All(obj => obj.CurrentHitPoints == 100));
        }

        #region Fixture

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(172);
            var npc = harness.Npc(172);
            harness.MoveTo(npc.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.AssignNPCMission(harness.Client, new AssignNPCMissionPacket { NpcEntityId = npc.EntityId, MissionId = CacheOfTheDay });
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[CacheOfTheDay].State);
            return harness;
        }

        private static Dictionary<string, DynamicObject> Fuel(WildernessRuntimeTestHarness harness) =>
            harness.Map.DynamicObjects.Where(obj => obj.SceneMissionId == CacheOfTheDay)
                .ToDictionary(obj => obj.SceneActorRole, obj => obj);

        /// <summary>One shotgun blast from where they stand - facing +z at rotation pi (AbilityManager.FacingOf) - landed.</summary>
        private static void Fire(WildernessRuntimeTestHarness harness, int damage, double rotation = Math.PI)
        {
            harness.Client.SetWorldPosition(Stand, rotation);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Client.Player.Target = 0;
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0, 0), damage, coneHalfAngle: HalfAngle);
            MissileManager.Instance.DoWork(harness.Map, 1000);
        }

        private static Creature Spawn(WildernessRuntimeTestHarness harness, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = harness.Map.MapInfo.MapContextId,
                RuntimeMapChannel = harness.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 5000, 5000, 5000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(harness.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(harness.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        #endregion
    }
}
