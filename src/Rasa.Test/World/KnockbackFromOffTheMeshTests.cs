using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Navigation;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A knockback from off the navmesh, and one whose flight crosses a collision mesh
    /// (CrowdControl.KnockbackDestination), on Bootcamp's hill: a hollow rock, its inside a floor
    /// of navmesh joined to nothing, beside a slope too steep for the mesh, where a player hit by
    /// Karem Zul's Tectonic Strike was reported ending up inside the hill.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KnockbackFromOffTheMeshTests
    {
        /// <summary>Where it was reported: on the slope by the rock, off the mesh.</summary>
        private static readonly Vector3 Reported = new(-9.1f, 97.4f, 176.6f);

        /// <summary>On the floor inside the rock.</summary>
        private static readonly Vector3 Inside = new(-16f, 96.7f, 182f);

        [TestMethod]
        public void FromTheSlopeBesideTheRockTheyAreThrownAndNeverIntoIt()
        {
            var map = Bootcamp();
            var nav = map.NavMesh;
            var inside = nav.Nearest(Inside).Value;
            var start = nav.Nearest(Reported).Value;
            Assert.IsTrue(new Vector2(start.X - Reported.X, start.Z - Reported.Z).Length() > 0.3f, $"off the mesh: the nearest walkable point is {start}");
            Assert.AreEqual(false, nav.Connected(start, inside), "the inside of the rock is joined to nothing");

            var thrown = 0;

            for (var step = 0; step < 16; step++)
            {
                var dir = Direction(step);

                Assert.AreEqual(Reported, OldDestination(nav, Reported, dir, 10f), "the way it was: not thrown at all");

                var end = CrowdControl.KnockbackDestination(map, Reported, dir, 10f);

                if (Vector3.Distance(Reported, end) <= 0.1f)
                    continue;

                thrown++;
                Assert.AreEqual(true, nav.Connected(start, end), $"along {dir}: {end} is on the floor they stood by");
                Assert.AreNotEqual(true, nav.Connected(end, inside), $"along {dir}: {end} is inside the rock");
                Assert.IsFalse(map.Cover.GeometryBlocks(Reported + Lift, end + Lift), $"along {dir}: flown through the rock to {end}");
            }

            Assert.IsTrue(thrown >= 4, $"thrown in {thrown} of 16 directions");
        }

        [TestMethod]
        public void NoThrowOverTheHillGoesThroughTheRock()
        {
            var map = Bootcamp();
            var nav = map.NavMesh;
            var throughBefore = 0;
            var throws = 0;

            for (var x = -40f; x <= 10f; x += 1f)
                for (var z = 150f; z <= 210f; z += 1f)
                {
                    if (nav.NearestInColumn(new Vector3(x, 115f, z), 30f) is not Vector3 start ||
                        new Vector2(start.X - x, start.Z - z).Length() > 0.3f)
                        continue;

                    for (var step = 0; step < 16; step++)
                    {
                        var dir = Direction(step);
                        throws++;

                        if (map.Cover.GeometryBlocks(start + Lift, OldDestination(nav, start, dir, 10f) + Lift))
                            throughBefore++;

                        var end = CrowdControl.KnockbackDestination(map, start, dir, 10f);
                        Assert.IsFalse(map.Cover.GeometryBlocks(start + Lift, end + Lift), $"from {start} along {dir}: flown through to {end}");
                        Assert.AreNotEqual(false, nav.Connected(start, end), $"from {start} along {dir}: {end}");
                    }
                }

            Assert.IsTrue(throws > 10000, $"{throws} throws");
            Assert.IsTrue(throughBefore > 0, "the way it was, some flew through the rock");
        }

        /// <summary>From a floor more than a metre above or below the nearest walkable point: not thrown, rather than onto it.</summary>
        [TestMethod]
        public void FromAboveTheRockOrUnderItTheyAreNotThrownOntoAnotherFloor()
        {
            var map = Bootcamp();
            var nav = map.NavMesh;

            foreach (var off in new[] { new Vector3(-16f, 103.5f, 182f), new Vector3(-16f, 93.5f, 176f) })
            {
                var nearest = nav.Nearest(off);
                Assert.IsNotNull(nearest, $"{off} has walkable ground near it");
                Assert.IsTrue(MathF.Abs(nearest.Value.Y - off.Y) > CrowdControl.OffMeshClimb + 0.5f, $"{off}: {nearest}");

                for (var step = 0; step < 16; step++)
                    Assert.AreEqual(off, CrowdControl.KnockbackDestination(map, off, Direction(step), 10f), $"from {off} along {Direction(step)}");
            }
        }

        /// <summary>Karem Zul's throw from the reported spot: the player is moved, and the client sent the throw.</summary>
        [TestMethod]
        public void APlayerOnTheSlopeIsSentTheThrow()
        {
            var bootcamp = Bootcamp();
            var dir = Enumerable.Range(0, 16).Select(Direction)
                .First(d => Vector3.Distance(Reported, CrowdControl.KnockbackDestination(bootcamp, Reported, d, 10f)) > 3f);
            var expected = CrowdControl.KnockbackDestination(bootcamp, Reported, dir, 10f);

            using var world = new WorldTestContext();
            world.Map.NavMesh = bootcamp.NavMesh;
            world.Map.Cover = bootcamp.Cover;
            var victim = Watch(world, Reported);
            var karem = Spawn(world, Reported - dir * 4f);
            WorldTestContext.Drain(victim);

            Assert.IsTrue(PlayerCrowdControl.Knockback(world.Map, victim.Player, karem, 10f));

            Assert.AreEqual(expected, victim.Player.Position);
            var moves = WorldTestContext.Drain(victim).Select(packet => packet.Message).OfType<MoveObjectMessage>()
                .Where(message => message.EntityId == victim.Player.EntityId).Select(message => message.Movement).ToList();
            Assert.AreEqual(1, moves.Count, "thrown");
            Assert.AreEqual(MovementType.Knockback, moves[0].Type);
            Assert.AreEqual(expected, moves[0].Position);
        }

        #region Fixture

        private static readonly Vector3 Lift = new(0f, CrowdControl.FlightHeight, 0f);

        private static Vector3 Direction(int step)
        {
            var angle = step * MathF.PI / 8f;
            return new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        }

        /// <summary>The knockback as it was: walked from where they stood, through anything.</summary>
        private static Vector3 OldDestination(NavMeshQuery nav, Vector3 from, Vector3 dir, float distance)
        {
            var end = from;

            for (var d = 0.5f; d <= distance + 0.001f; d += 0.5f)
            {
                var ground = nav.WalkTo(end, from + dir * d);

                if (ground == null)
                    break;

                end = ground.Value;
            }

            return end;
        }

        /// <summary>Bootcamp's navmesh and cover, on a map of their own.</summary>
        private static MapChannel Bootcamp()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            var navmesh = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."), "navmesh");

            return new MapChannel
            {
                NavMesh = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, "adv_bootcamp")),
                Cover = CoverMesh.Read(CoverMesh.PathFor(navmesh, "adv_bootcamp"))
            };
        }

        private static Creature Spawn(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Karem Zul",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, Vector3 position)
        {
            var client = world.CreateClient();
            client.Player.PlaceAt(position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            var seed = CellManager.Instance.GetCellSeed(position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        #endregion
    }
}
