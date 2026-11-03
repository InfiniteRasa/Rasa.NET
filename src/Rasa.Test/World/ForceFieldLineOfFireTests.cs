using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Navigation;
    using Rasa.Structures;

    /// <summary>
    /// An AFS force field across a gate keeps the Bane out and their fire with them: a creature
    /// pushed against the sheet is not carried through it, nor put through it when its run home
    /// times out, its shots and its sight stop at it, and a launcher's splash reaches nobody on
    /// the far side of it or of a wall.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ForceFieldLineOfFireTests
    {
        [TestMethod]
        public void AStepFromAgainstTheSheetIsLetBackOutButNotOnThrough()
        {
            using var world = new WorldTestContext();
            var creature = Spawn(world, 10, -0.4f);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                var against = new Vector3(10, 0, -0.4f);

                Assert.IsTrue(ForceFields.Stops(world.Map, creature, against, new Vector3(10, 0, 0.6f)), "on through");
                Assert.IsTrue(ForceFields.Stops(world.Map, creature, against, new Vector3(10, 0, 0.1f)), "over the middle");
                Assert.IsFalse(ForceFields.Stops(world.Map, creature, against, new Vector3(10, 0, -1.4f)), "back out");
                Assert.IsFalse(ForceFields.Stops(world.Map, creature, against, new Vector3(11, 0, -0.5f)), "along it");
                Assert.IsFalse(ForceFields.Stops(world.Map, creature, new Vector3(25, 0, -0.4f), new Vector3(25, 0, 0.6f)), "past its end");
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void ACrowdAgainstAGateDoesNotPushItsOwnThroughIt()
        {
            using var world = new WorldTestContext();
            var front = Spawn(world, 10, -0.35f);
            var behind = Spawn(world, 10, -0.6f);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                for (var i = 0; i < 100; i++)
                {
                    BehaviorManager.Instance.MapChannelThink(world.Map, 250);
                    Assert.IsTrue(front.Position.Z < 0f && behind.Position.Z < 0f, $"pushed through: {front.Position}, {behind.Position}");
                }
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void ARunHomeThatRunsOutOfTimeDoesNotPutItAcrossAField()
        {
            using var world = new WorldTestContext();
            var held = Spawn(world, 10, -5f);
            var free = Spawn(world, 40, -5f);
            world.CreateClient(x: 25, z: -20);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                // Each one's home the far side of where it is - for the first, behind the gate's
                // field: a control point's attacker, sent home to the outpost it assaults. Its time
                // to get there is up at once, as it is for one held at the field.
                foreach (var creature in new[] { held, free })
                {
                    creature.RunSpeed = 5f;
                    creature.HomePos.Position = creature.Position + new Vector3(0, 0, 10);
                    BehaviorManager.Instance.Leash(world.Map, creature);
                    creature.Controller.ActionReturning.TimeoutMs = 0;
                }

                BehaviorManager.Instance.MapChannelThink(world.Map, 250);

                Assert.IsTrue(held.Position.Z < 0f, $"put through the field: {held.Position}");
                Assert.AreEqual(5f, free.Position.Z, 0.01f, "put home, with nothing in the way");
                Assert.AreNotEqual(BehaviorManager.BehaviorActionReturning, held.Controller.CurrentAction, "and done with the run");
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void AFieldStopsFireBothWaysWhoeverItLetsThrough()
        {
            using var world = new WorldTestContext();
            var bane = Spawn(world, 10, -5f);
            var player = world.CreateClient(x: 10, z: 5).Player;
            var outside = new Vector3(10, 1.5f, -5);
            var inside = new Vector3(10, 1.5f, 5);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                Assert.IsTrue(ForceFields.Blocks(world.Map, bane, outside, inside), "the Bane's shot in");
                Assert.IsTrue(ForceFields.Blocks(world.Map, player, inside, outside), "nor do the defenders shoot out");
                Assert.IsFalse(ForceFields.Blocks(world.Map, bane, new Vector3(30, 1.5f, -5), new Vector3(30, 1.5f, 5)), "beside it");
                Assert.IsFalse(ForceFields.Blocks(world.Map, bane, new Vector3(10, 15f, -5), new Vector3(10, 15f, 5)), "over it");

                ForceFields.SetSide(field, ForceFields.Side.B);
                Assert.IsTrue(ForceFields.Blocks(world.Map, bane, outside, inside), "a Bane field the same");
                Assert.IsTrue(ForceFields.Blocks(world.Map, player, inside, outside));

                field.Health = 0;
                Assert.IsFalse(ForceFields.Blocks(world.Map, player, inside, outside), "down");
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void TheBaneDoNotSeeThroughAnAfsFieldToShoot()
        {
            using var world = new WorldTestContext();
            var bane = Spawn(world, 10, -5f);
            var player = world.CreateClient(x: 10, z: 5).Player;

            Assert.IsTrue(BehaviorManager.Sees(world.Map, bane, player), "no field");

            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                Assert.IsFalse(BehaviorManager.Sees(world.Map, bane, player));

                field.Health = 0;
                Assert.IsTrue(BehaviorManager.Sees(world.Map, bane, player), "the field down");
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void ASplashStopsAtAWallAndAtAFieldThatStopsTheShooter()
        {
            using var world = new WorldTestContext();
            var bane = Spawn(world, 10, -10f);
            var hit = world.CreateClient(x: 10, z: -2).Player;
            var behindWall = world.CreateClient(x: 13, z: -2).Player;
            var behindField = world.CreateClient(x: 10, z: 2).Player;
            var inTheOpen = world.CreateClient(x: 8, z: -3).Player;
            var blast = Cover.Middle(hit);

            // A wall from x 11.5 to 12, 0 to 4 m high, between the one hit and the one beside it.
            world.Map.Cover = CoverMesh.Build(new float[] { 11.5f, 0, -10, 11.5f, 4, -10, 11.5f, 4, 10, 11.5f, 0, 10 }, new[] { 0, 1, 2, 0, 2, 3 });

            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                Assert.IsTrue(MissileManager.InBlast(world.Map, bane, blast, inTheOpen));
                Assert.IsFalse(MissileManager.InBlast(world.Map, bane, blast, behindWall), "the wall");
                Assert.IsFalse(MissileManager.InBlast(world.Map, bane, blast, behindField), "the field");

                field.Health = 0;
                Assert.IsTrue(MissileManager.InBlast(world.Map, bane, blast, behindField), "the field down");
            }
            finally
            {
                ForceFields.Remove(field);
                world.Map.Cover = null;
            }
        }

        /// <summary>A hostile creature standing in the map's cells, idle, so a think does nothing but keep it apart from its neighbours.</summary>
        private static Creature Spawn(WorldTestContext world, float x, float z)
        {
            var creature = new Creature
            {
                Name = "Bane",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, z),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            creature.HomePos.Position = creature.Position;
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }
    }
}
