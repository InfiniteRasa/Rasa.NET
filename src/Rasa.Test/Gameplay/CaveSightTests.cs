using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;
    using Rasa.Test.World;

    /// <summary>
    /// Lines of sight under the ground (Managers.Sight, appsettings.json's Sight.UnderTerrain). The cover mesh's
    /// terrain is the whole heightmap, so two actors in a cave are, to it, deep inside the hill: with the key off
    /// every answer is what it always was (nothing is seen further than a grid cell), with it on a line that has an
    /// end underground - by the map, not by the actor's word - is tested against the collision alone.
    ///
    /// The cave here is a tunnel under a hill. The hill is 30 m high and ends at z = 40; the tunnel runs along z at
    /// the height of the ground outside, 8 m wide and 5 m high, shut at z = -6 and open at z = 46, with a wall half
    /// across it at z = 20 (the half of the negative x), a shelf of rock 2 m over the floor behind that wall and a
    /// crate 1.5 m high against its other side at z = 28 to 30. The navmesh is stood in for: the tunnel's floor
    /// under the hill, a body's width off its walls and off the crate, is underground floor, and nowhere else is.
    /// The last tests are on the Wilderness's own files, at the Pinhole Falls Caverns.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CaveSightTests
    {
        private static readonly Vector3 Eye = new(0f, 1.62f, 0f);

        /// <summary>In the tunnel, 12 m along it from the player at the origin: nothing of the tunnel between them.</summary>
        private static readonly Vector3 Along = new(0f, 0f, 12f);

        /// <summary>In the tunnel, behind the wall half across it.</summary>
        private static readonly Vector3 BehindTheWall = new(-3f, 0f, 30f);

        /// <summary>On the hill, over the tunnel.</summary>
        private static readonly Vector3 OnTheHill = new(0f, 30f, 12f);

        /// <summary>Outside, in front of the tunnel's mouth.</summary>
        private static readonly Vector3 AtTheMouth = new(0f, 0f, 50f);

        /// <summary>Inside the hill, beside the tunnel: under the height grid and on no floor.</summary>
        private static readonly Vector3 InTheRock = new(20f, 10f, 0f);
        private static readonly Vector3 InTheRockToo = new(20f, 10f, 12f);

        /// <summary>What a ranged hit on a target wholly behind cover still does.</summary>
        private const double Least = Cover.MinModifier;

        // ---- the cover mesh itself

        [TestMethod]
        public void UnderTheHeightGridTheCoverMeshBlocksEveryLineLongerThanACell()
        {
            var mesh = Tunnel();
            var chest = Along + new Vector3(0, 1.4f, 0);

            Assert.IsTrue(mesh.TerrainBlocks(Eye, chest), "the grid is 28 m over the line");
            Assert.IsFalse(mesh.GeometryBlocks(Eye, chest), "nothing of the tunnel is in the way");
            Assert.IsTrue(mesh.Blocked(Eye, chest));
            Assert.IsFalse(mesh.Blocked(Eye, new Vector3(0, 1.4f, 1.5f)), "a line shorter than a cell is not sampled");

            Assert.IsFalse(Sight.Blocked(mesh, Eye, chest, withoutTerrain: true));
            Assert.IsTrue(Sight.Blocked(mesh, Eye, chest, withoutTerrain: false));
            Assert.IsTrue(Sight.Blocked(mesh, Eye, new Vector3(0, 1.4f, -10f), withoutTerrain: true), "the wall that shuts it");
        }

        // ---- who is underground

        [TestMethod]
        public void UndergroundIsUnderTheGridByItsDepthAndOnFloorTheMapHasThere()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var asked = 0;

            Cave(world, mesh, () =>
            {
                var floor = Sight.UndergroundFloor;

                Sight.UndergroundFloor = (map, feet) =>
                {
                    asked++;
                    return floor(map, feet);
                };

                Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, Vector3.Zero), "in the tunnel");
                Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, Along));
                Assert.AreEqual(2, asked);

                Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, InTheRock), "under the grid, on no floor");
                Assert.AreEqual(3, asked, "the map was asked, and said no");

                Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, OnTheHill), "on the hill over the tunnel");
                Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, AtTheMouth), "outside");
                Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, new Vector3(0, 30f - Sight.Depth + 0.1f, 12f)), "a dip in the hill, not that deep");
                Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, new Vector3(500f, 0, 0)), "off the grid");
                Assert.AreEqual(3, asked, "on the surface the navmesh is not asked");

                Assert.IsFalse(Sight.IsUnderground(null, mesh, Vector3.Zero), "no map");
                Assert.IsFalse(Sight.IsUnderground(world.Map, null, Vector3.Zero), "no cover file");
            });
        }

        [TestMethod]
        public void APlaceInTheRockBesideTheTunnelIsWithinReachOfItsFloorAndIsNotInIt()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var inTunnel = AddCreature(world, Along);
            var atMouth = AddCreature(world, AtTheMouth);

            // 0.8 m into the rock behind the tunnel's side: under the grid, and 1.4 m from the navmesh's floor.
            var inWall = new Vector3(4.8f, 0f, 12f);
            var byWall = new Vector3(3.9f, 0f, 12f);

            try
            {
                Cave(world, mesh, () =>
                {
                    Assert.IsTrue(Sight.UndergroundFloor(world.Map, inWall).HasValue, "the navmesh alone would take it for the tunnel");
                    Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, inWall), "the tunnel's side is between it and that floor");
                    Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, byWall), "against the wall from the inside");
                    Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, byWall + new Vector3(0, 2f, 0)), "and at the top of a jump there");

                    // The same over the tunnel: in the rock on a shelf 2 m over the floor, within reach of the floor under it.
                    var overShelf = new Vector3(-2.5f, 2.5f, 24f);

                    Assert.IsTrue(Sight.UndergroundFloor(world.Map, overShelf).HasValue);
                    Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, overShelf), "the shelf is between it and the air over that floor");
                    Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, new Vector3(-2.5f, 0f, 24f)), "under the shelf");

                    // A client that says it stands there sees nothing of the hill's outside through the ground, nor into the tunnel.
                    player.Position = inWall;
                    Assert.IsFalse(Sight.WithoutTerrain(world.Map, mesh, player.Position, atMouth.Position));
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, atMouth), "the hill");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, atMouth, player));
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, inTunnel), "the tunnel's side");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, inTunnel, player));
                    Assert.IsFalse(BehaviorManager.Sees(mesh, inTunnel, player));
                });
            }
            finally
            {
                Remove(world, inTunnel, atMouth);
            }
        }

        [TestMethod]
        public void StandingOnSomethingInTheTunnelIsStandingInTheTunnel()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var creature = AddCreature(world, Along);
            var onTheHill = AddCreature(world, OnTheHill);

            // On the crate: 1.5 m over the floor, which the navmesh has only beside the crate, 1.4 m away.
            var onCrate = new Vector3(2.8f, 1.5f, 29f);

            try
            {
                Cave(world, mesh, () =>
                {
                    Assert.AreEqual(new Vector3(1.4f, 0f, 29f), Sight.UndergroundFloor(world.Map, onCrate).Value, "the floor beside the crate");
                    Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, onCrate));
                    Assert.IsTrue(Sight.IsUnderground(world.Map, mesh, new Vector3(3.3f, 1.5f, 29f)), "at its far side, by the floor at the crate's end");
                    Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, new Vector3(3.3f, 0.2f, 29f)), "inside the crate, though: its end is in the way");

                    player.Position = onCrate;
                    Assert.AreEqual(1.0, Cover.Modifier(world.Map, player, creature), "it shoots down the tunnel from there");
                    Assert.IsTrue(BehaviorManager.Sees(mesh, creature, player), "and is shot at");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, onTheHill), "the roof still hides the hill");
                });
            }
            finally
            {
                Remove(world, creature, onTheHill);
            }
        }

        [TestMethod]
        public void WithTheKeyOffNothingIsEverLeftOutAndTheMapIsNotAsked()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();

            Cave(world, mesh, () =>
            {
                Sight.Config = new SightConfig();
                Sight.UndergroundFloor = (map, feet) => throw new InvalidOperationException("asked with the key off");

                Assert.IsFalse(Sight.WithoutTerrain(world.Map, mesh, Vector3.Zero, Along));
                Assert.IsFalse(Sight.WithoutTerrain(world.Map, mesh, Vector3.Zero));
            });
        }

        [TestMethod]
        public void AMapWithNoNavmeshHasNoUndergroundFloor()
        {
            using var world = new WorldTestContext();

            Assert.IsNull(world.Map.NavMesh);
            Assert.IsNull(Sight.NavMeshFloor(world.Map, Vector3.Zero));
            Assert.IsNull(Sight.NavMeshFloor(null, Vector3.Zero));
        }

        // ---- what a ranged hit does, and whether a creature shoots

        [TestMethod]
        public void InTheTunnelARangedHitDoesTheLeastTodayAndAllOfItWithTheKey()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var creature = AddCreature(world, Along);

            try
            {
                Cave(world, mesh, () =>
                {
                    Sight.Config = new SightConfig();
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, creature), "today: wholly behind terrain");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, creature, player));
                    Assert.IsFalse(BehaviorManager.Sees(mesh, creature, player), "today: it never shoots");

                    Sight.Config = new SightConfig { UnderTerrain = true };
                    Assert.AreEqual(1.0, Cover.Modifier(world.Map, player, creature), "nothing of the tunnel is between them");
                    Assert.AreEqual(1.0, Cover.Modifier(world.Map, creature, player));
                    Assert.IsTrue(BehaviorManager.Sees(mesh, creature, player));
                });
            }
            finally
            {
                Remove(world, creature);
            }
        }

        [TestMethod]
        public void TheTunnelsOwnWallAndItsRoofStillBlock()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var behind = AddCreature(world, BehindTheWall);
            var above = AddCreature(world, OnTheHill);

            try
            {
                Cave(world, mesh, () =>
                {
                    Assert.IsTrue(Sight.WithoutTerrain(world.Map, mesh, player.Position, behind.Position), "both are in the tunnel");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, behind), "the wall half across it");
                    Assert.IsFalse(BehaviorManager.Sees(mesh, behind, player));

                    Assert.IsTrue(Sight.WithoutTerrain(world.Map, mesh, player.Position, above.Position), "one of them is");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, above), "the roof");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, above, player));
                    Assert.IsFalse(BehaviorManager.Sees(mesh, above, player));
                });
            }
            finally
            {
                Remove(world, behind, above);
            }
        }

        [TestMethod]
        public void OneEndUndergroundIsEnoughFromTheMouthIntoTheTunnel()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient(AtTheMouth.X, AtTheMouth.Z).Player;
            var creature = AddCreature(world, Along);

            try
            {
                Cave(world, mesh, () =>
                {
                    Sight.Config = new SightConfig();
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, creature), "today the hill is in the way");

                    Sight.Config = new SightConfig { UnderTerrain = true };
                    Assert.IsFalse(Sight.IsUnderground(world.Map, mesh, player.Position), "the player is outside");
                    Assert.AreEqual(1.0, Cover.Modifier(world.Map, player, creature));
                    Assert.IsTrue(BehaviorManager.Sees(mesh, creature, player));
                });
            }
            finally
            {
                Remove(world, creature);
            }
        }

        [TestMethod]
        public void AnEndUnderTheGridThatIsOnNoUndergroundFloorGainsNothing()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var inRock = AddCreature(world, InTheRockToo);
            var onHill = AddCreature(world, new Vector3(20f, 30f, 12f));
            var inTunnel = AddCreature(world, Along);

            try
            {
                Cave(world, mesh, () =>
                {
                    // A client can say it stands anywhere: inside the hill, say.
                    player.Position = InTheRock;

                    Assert.IsFalse(Sight.WithoutTerrain(world.Map, mesh, player.Position, inRock.Position));
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, inRock), "terrain, as before");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, onHill), "it does not see out of the hill");
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, onHill, player), "nor is it seen in it");
                    Assert.IsFalse(BehaviorManager.Sees(mesh, onHill, player));

                    // The same in the tunnel on a map whose navmesh has the flag nowhere: nobody is underground.
                    Sight.UndergroundFloor = (map, feet) => null;
                    player.Position = Vector3.Zero;
                    Assert.AreEqual(Least, Cover.Modifier(world.Map, player, inTunnel));
                    Assert.IsFalse(BehaviorManager.Sees(mesh, inTunnel, player));
                });
            }
            finally
            {
                Remove(world, inRock, onHill, inTunnel);
            }
        }

        [TestMethod]
        public void AShotAtASelectedTargetInTheTunnelIsNoLongerOneAtSomethingBehindCover()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var creature = AddCreature(world, Along);
            var behind = AddCreature(world, BehindTheWall);
            var checks = WeaponChecks.Config;

            try
            {
                WeaponChecks.Config = new WeaponChecksConfig { Facing = MovementChecksConfig.Off, Range = MovementChecksConfig.Off, Sight = MovementChecksConfig.Refuse };

                Cave(world, mesh, () =>
                {
                    Sight.Config = new SightConfig();
                    Assert.AreEqual(WeaponChecks.Check.Sight, WeaponChecks.Judge(player, creature, 20f, mesh).Value.Check, "today");

                    Sight.Config = new SightConfig { UnderTerrain = true };
                    Assert.IsNull(WeaponChecks.Judge(player, creature, 20f, mesh));
                    Assert.AreEqual(WeaponChecks.Check.Sight, WeaponChecks.Judge(player, behind, 40f, mesh).Value.Check, "behind the tunnel's wall");
                });
            }
            finally
            {
                WeaponChecks.Config = checks;
                Remove(world, creature, behind);
            }
        }

        // ---- the game master's report

        [TestMethod]
        public void TheLosReportSaysWhenAnEndIsUndergroundAndWhatThatLeaves()
        {
            using var world = new WorldTestContext();
            var mesh = Tunnel();
            var player = world.CreateClient().Player;
            var creature = AddCreature(world, Along);
            var behind = AddCreature(world, BehindTheWall);

            try
            {
                Cave(world, mesh, () =>
                {
                    Sight.Config = new SightConfig();
                    var today = LosReport.Lines(player, creature.EntityId);

                    CollectionAssert.Contains(today, "- Is blocked by Terrain");
                    CollectionAssert.Contains(today, "- Body points: 0/6 clear, 6 behind terrain, 0 behind geometry");
                    CollectionAssert.Contains(today, "- Your shots at it: damage x0.10");
                    CollectionAssert.Contains(today, "- Its shots at you: damage x0.10");
                    CollectionAssert.Contains(today, "- It does not see you and will not shoot");
                    Assert.IsFalse(today.Any(line => line.Contains("underground")), "nothing new is said with the key off");

                    Sight.Config = new SightConfig { UnderTerrain = true };
                    var keyed = LosReport.Lines(player, creature.EntityId);

                    CollectionAssert.Contains(keyed, "- An end is underground: Terrain is not counted (Sight.UnderTerrain)");
                    CollectionAssert.Contains(keyed, "- Has clear LOS on server");
                    CollectionAssert.Contains(keyed, "- Body points: 6/6 clear, 0 behind terrain, 0 behind geometry");
                    CollectionAssert.Contains(keyed, "- Your shots at it: damage x1.00");
                    CollectionAssert.Contains(keyed, "- Its shots at you: damage x1.00");
                    CollectionAssert.Contains(keyed, "- It sees you and will shoot");

                    var wall = LosReport.Lines(player, behind.EntityId);

                    CollectionAssert.Contains(wall, "- Is blocked by collision geometry");
                    CollectionAssert.Contains(wall, "- Body points: 0/6 clear, 0 behind terrain, 6 behind geometry");
                    CollectionAssert.Contains(wall, "- It does not see you and will not shoot");
                });
            }
            finally
            {
                Remove(world, creature, behind);
            }
        }

        [TestMethod]
        public void TheReportOnALineIsWordForWordTheOldOneUnlessToldAnEndIsUnderground()
        {
            var mesh = Tunnel();
            var points = new[] { Along + new Vector3(0, 1.7f, 0), Along + new Vector3(0, 0.4f, 0) };

            CollectionAssert.AreEqual(
                new[] { "- Is blocked by Terrain", "- Body points: 0/2 clear, 2 behind terrain, 0 behind geometry" },
                LosReport.Describe(mesh, Eye, Along + new Vector3(0, 0.9f, 0), points));
            CollectionAssert.AreEqual(
                new[] { "- An end is underground: Terrain is not counted (Sight.UnderTerrain)", "- Has clear LOS on server", "- Body points: 2/2 clear, 0 behind terrain, 0 behind geometry" },
                LosReport.Describe(mesh, Eye, Along + new Vector3(0, 0.9f, 0), points, withoutTerrain: true));
        }

        // ---- the Pinhole cavern, on the map's own files

        /// <summary>The three egg-layer posts WildernessElohPinholePlacementTests pins as underground.</summary>
        private static readonly Vector3[] Posts =
        {
            new(330f, 203.8f, 526f), new(338.5f, 213.1f, 552f), new(364f, 212f, 535f)
        };

        private static bool Wilderness(WorldTestContext world, out CoverMesh cover, out NavMeshQuery nav)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            var coverPath = directory == null ? "" : Path.Combine(directory.FullName, "navmesh", "adv_foreas_concordia_wilderness.cover");
            var navPath = directory == null ? "" : NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), "adv_foreas_concordia_wilderness");

            cover = null;
            nav = null;

            if (!File.Exists(coverPath) || !File.Exists(navPath))
                return false;

            world.Map.Cover = cover = CoverMesh.Read(coverPath);
            world.Map.NavMesh = nav = TestNavMeshes.Query(navPath);

            return true;
        }

        [TestMethod]
        public void AtThePinholePostsNothingIsSeenBeyondACellTodayAndTheCavernIsSeenWithTheKey()
        {
            using var world = new WorldTestContext();

            if (!Wilderness(world, out var cover, out var nav))
                Assert.Inconclusive("The Wilderness navmesh and cover are not in navmesh/.");

            var config = Sight.Config;
            var actors = new List<Creature>();
            int pairs = 0, clearWithTheKey = 0;

            try
            {
                foreach (var post in Posts)
                {
                    var feet = nav.Nearest(post).Value;
                    var here = AddCreature(world, feet);

                    actors.Add(here);
                    Assert.IsTrue(cover.TerrainHeight(feet.X, feet.Z).Value - feet.Y > 50f, "the height grid is far over the post");
                    Assert.IsTrue(Sight.NavMeshFloor(world.Map, feet).HasValue, "the navmesh has underground floor there");
                    Assert.IsTrue(Sight.IsUnderground(world.Map, cover, feet));

                    foreach (var distance in new[] { 3f, 6f })
                        for (var i = 0; i < 16; i++)
                        {
                            var probe = feet + new Vector3(MathF.Cos(i * MathF.PI / 8), 0, MathF.Sin(i * MathF.PI / 8)) * distance;
                            var there = nav.Nearest(probe);

                            // Somewhere to stand that far off, on the cavern's floor.
                            if (!there.HasValue || Math.Abs(there.Value.Y - feet.Y) > 3f || new Vector2(there.Value.X - probe.X, there.Value.Z - probe.Z).Length() > 1f)
                                continue;

                            var other = AddCreature(world, there.Value);
                            var eye = Cover.EyeOf(here);
                            var points = Cover.SamplePoints(other, eye);
                            var behindGeometry = points.Count(point => cover.GeometryBlocks(eye, point));

                            actors.Add(other);
                            pairs++;

                            Sight.Config = new SightConfig();
                            Assert.AreEqual(Least, Cover.Modifier(world.Map, here, other), $"today, {distance} m from {post}");
                            Assert.IsFalse(BehaviorManager.Sees(cover, here, other));

                            Sight.Config = new SightConfig { UnderTerrain = true };
                            Assert.AreEqual(Cover.ModifierFrom((points.Length - behindGeometry) / (double)points.Length), Cover.Modifier(world.Map, here, other), 1e-9,
                                "with the key the cavern's own collision is all that counts");
                            Assert.AreEqual(behindGeometry < points.Length, BehaviorManager.Sees(cover, here, other));

                            if (behindGeometry == 0)
                                clearWithTheKey++;
                        }
                }

                Console.WriteLine($"{pairs} pairs 3 m and 6 m apart at the three posts; with the key {clearWithTheKey} are wholly in the clear");
                Assert.IsGreaterThanOrEqualTo(48, pairs, "the cavern has room around its posts");
                Assert.IsGreaterThanOrEqualTo(pairs * 3 / 4, clearWithTheKey, "and most of it is open");
            }
            finally
            {
                Sight.Config = config;
                Remove(world, actors.ToArray());
            }
        }

        [TestMethod]
        public void TheHillOverTheCavernAndTheRockInsideItAreNotUnderground()
        {
            using var world = new WorldTestContext();

            if (!Wilderness(world, out var cover, out var nav))
                Assert.Inconclusive("The Wilderness navmesh and cover are not in navmesh/.");

            foreach (var post in Posts)
            {
                var feet = nav.Nearest(post).Value;
                var surface = cover.TerrainHeight(feet.X, feet.Z).Value;

                Assert.IsTrue(Vector3.Distance(feet, nav.UndergroundFloor(feet, Sight.FloorAcross, Sight.FloorBelow, Sight.FloorAbove).Value) < 0.05f, "the floor it stands on");
                Assert.IsTrue(nav.UndergroundFloor(feet + new Vector3(0, 2f, 0), Sight.FloorAcross, Sight.FloorBelow, Sight.FloorAbove).HasValue, "at the top of a jump");
                Assert.IsTrue(nav.UndergroundFloor(feet - new Vector3(0, 0.5f, 0), Sight.FloorAcross, Sight.FloorBelow, Sight.FloorAbove).HasValue, "a client's floor a little under the navmesh's");
                Assert.IsTrue(Sight.IsUnderground(world.Map, cover, feet + new Vector3(0, 2f, 0)));
                Assert.IsTrue(Sight.IsUnderground(world.Map, cover, feet - new Vector3(0, 0.5f, 0)));

                // On the hill: not under the grid, whatever is below.
                Assert.IsFalse(Sight.IsUnderground(world.Map, cover, new Vector3(feet.X, surface, feet.Z)));
                Assert.IsFalse(Sight.IsUnderground(world.Map, cover, new Vector3(feet.X, surface - 1f, feet.Z)));

                // Over the post - in the air of the cavern or in the rock over it, where a client can say it stands -
                // 6 m up, and halfway to the grass: under the grid, and on no floor. The navmesh's own wide look
                // takes the first of these for the cavern.
                Assert.IsTrue(nav.IsUnderground(feet + new Vector3(0, 6f, 0)), "eight metres down is within the wide look's reach");
                Assert.IsFalse(Sight.IsUnderground(world.Map, cover, feet + new Vector3(0, 6f, 0)), $"6 m over the floor at {post}");
                Assert.IsFalse(Sight.IsUnderground(world.Map, cover, new Vector3(feet.X, (feet.Y + surface) * 0.5f, feet.Z)), $"halfway up to the hill at {post}");
            }

            // Somewhere on the surface with a navmesh under it.
            var open = nav.Nearest(new Vector3(850f, 339.5f, 30f)).Value;

            Assert.IsNull(nav.UndergroundFloor(open, Sight.FloorAcross, Sight.FloorBelow, Sight.FloorAbove));
            Assert.IsFalse(Sight.IsUnderground(world.Map, cover, open));
        }

        [TestMethod]
        public void FromThePinholePostsTheHillIsBehindTheCavernsOwnCollision()
        {
            using var world = new WorldTestContext();

            if (!Wilderness(world, out var cover, out var nav))
                Assert.Inconclusive("The Wilderness navmesh and cover are not in navmesh/.");

            // With the key a line from the cavern to the surface has the ground left out of it too: what is between
            // them then is the cavern's own shell, which the cover file has. Straight up from each post, and to the
            // hill all around it.
            int lines = 0, open = 0;

            foreach (var post in Posts)
            {
                var feet = nav.Nearest(post).Value;
                var eye = feet + new Vector3(0, Cover.PlayerHeight * Cover.EyeFactor, 0);
                var roof = cover.TerrainHeight(feet.X, feet.Z).Value;

                Assert.IsTrue(cover.GeometryBlocks(eye, new Vector3(feet.X, roof + 1f, feet.Z)), $"the cavern has a roof over {post}");

                foreach (var radius in new[] { 15f, 30f, 60f, 90f })
                    for (var i = 0; i < 16; i++)
                    {
                        var x = feet.X + MathF.Cos(i * MathF.PI / 8) * radius;
                        var z = feet.Z + MathF.Sin(i * MathF.PI / 8) * radius;
                        var ground = cover.TerrainHeight(x, z);

                        if (!ground.HasValue)
                            continue;

                        var chest = new Vector3(x, ground.Value + 1f, z);

                        Assert.IsTrue(cover.TerrainBlocks(eye, chest), "today the hill itself is in the way");
                        lines++;

                        if (!cover.GeometryBlocks(eye, chest))
                            open++;
                    }
            }

            Console.WriteLine($"{lines} lines from the three posts to the hill 15 to 90 m around them; the cavern's collision leaves {open} open");
            Assert.AreEqual(0, open, "nobody on the hill is seen from the posts through the ground");
        }

        [TestMethod]
        public void OnTheSurfaceTheKeyChangesNoAnswer()
        {
            using var world = new WorldTestContext();

            if (!Wilderness(world, out var cover, out var nav))
                Assert.Inconclusive("The Wilderness navmesh and cover are not in navmesh/.");

            var config = Sight.Config;
            var floor = Sight.UndergroundFloor;
            var actors = new List<Creature>();
            var asked = 0;
            var compared = 0;

            try
            {
                Sight.UndergroundFloor = (map, feet) =>
                {
                    asked++;
                    return floor(map, feet);
                };

                // A ring of places 9 m around four spots of the surface, each against the middle and against the next.
                foreach (var centre in new[] { new Vector3(850f, 339.5f, 30f), new Vector3(-337.7f, 167.3f, -241.2f), new Vector3(-260f, 173f, 270f), new Vector3(180f, 164.4f, -22f) })
                {
                    var middle = AddCreature(world, nav.Nearest(centre).Value);

                    actors.Add(middle);

                    for (var i = 0; i < 12; i++)
                    {
                        var at = nav.Nearest(middle.Position + new Vector3(MathF.Cos(i * MathF.PI / 6), 0, MathF.Sin(i * MathF.PI / 6)) * 9f);

                        if (!at.HasValue)
                            continue;

                        var other = AddCreature(world, at.Value);
                        var before = actors[^1];

                        actors.Add(other);

                        foreach (var (from, to) in new[] { (middle, other), (other, middle), (before, other) })
                        {
                            Sight.Config = new SightConfig();
                            var off = (Cover.Modifier(world.Map, from, to), BehaviorManager.Sees(cover, from, to));

                            Sight.Config = new SightConfig { UnderTerrain = true };
                            var on = (Cover.Modifier(world.Map, from, to), BehaviorManager.Sees(cover, from, to));

                            Assert.AreEqual(off, on, $"{from.Position} to {to.Position}");
                            compared++;
                        }
                    }
                }

                Console.WriteLine($"{compared} lines on the surface compared, the navmesh asked {asked} times");
                Assert.IsGreaterThanOrEqualTo(100, compared);
            }
            finally
            {
                Sight.Config = config;
                Sight.UndergroundFloor = floor;
                Remove(world, actors.ToArray());
            }
        }

        // ---- the fixture

        /// <summary>
        /// Runs <paramref name="body"/> with the key on, the map's cover set to <paramref name="mesh"/> and the tunnel's
        /// floor under the hill for the map's underground floor, and puts everything back.
        /// </summary>
        private static void Cave(WorldTestContext world, CoverMesh mesh, Action body)
        {
            var config = Sight.Config;
            var floor = Sight.UndergroundFloor;

            try
            {
                world.Map.Cover = mesh;
                Sight.Config = new SightConfig { UnderTerrain = true };
                Sight.UndergroundFloor = (map, feet) => ReferenceEquals(map, world.Map) ? TunnelFloor(feet) : null;

                body();
            }
            finally
            {
                world.Map.Cover = null;
                Sight.Config = config;
                Sight.UndergroundFloor = floor;
            }
        }

        /// <summary>
        /// The tunnel's floor under the hill as a navmesh has it: a body's width (0.6 m) off the walls, and found from as
        /// far as the game looks for it (Sight.FloorAcross to the side, FloorBelow under the feet, FloorAbove over them).
        /// The place on it nearest to the feet, or null.
        /// </summary>
        private static Vector3? TunnelFloor(Vector3 feet)
        {
            var on = new Vector3(Math.Clamp(feet.X, -3.4f, 3.4f), 0f, Math.Clamp(feet.Z, -5.4f, 39.9f));

            // No floor under the crate, nor within a body's width of it: the nearest floor is beside it.
            if (on.X > 1.4f && on.Z > 27.4f && on.Z < 30.6f)
            {
                var beside = new[] { new Vector3(1.4f, 0f, on.Z), new Vector3(on.X, 0f, 27.4f), new Vector3(on.X, 0f, 30.6f) };

                on = beside.OrderBy(place => Vector3.DistanceSquared(place, new Vector3(feet.X, 0f, feet.Z))).First();
            }

            if (new Vector2(on.X - feet.X, on.Z - feet.Z).Length() > Sight.FloorAcross)
                return null;

            return on.Y - feet.Y >= -Sight.FloorBelow && on.Y - feet.Y <= Sight.FloorAbove ? on : (Vector3?)null;
        }

        /// <summary>The hill as a height grid; the tunnel's walls and roof, the wall half across it, the shelf and the crate as collision.</summary>
        private static CoverMesh Tunnel()
        {
            var vertices = new List<float>();
            var indices = new List<int>();

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var first = vertices.Count / 3;

                foreach (var v in new[] { a, b, c, d })
                {
                    vertices.Add(v.X);
                    vertices.Add(v.Y);
                    vertices.Add(v.Z);
                }

                indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            Quad(new(-4, 0, -6), new(-4, 0, 46), new(-4, 5, 46), new(-4, 5, -6));     // one side
            Quad(new(4, 0, -6), new(4, 0, 46), new(4, 5, 46), new(4, 5, -6));         // the other
            Quad(new(-4, 0, -6), new(4, 0, -6), new(4, 5, -6), new(-4, 5, -6));       // shut at this end
            Quad(new(-4, 5, -6), new(4, 5, -6), new(4, 5, 46), new(-4, 5, 46));       // the roof
            Quad(new(-4, 0, 20), new(-1, 0, 20), new(-1, 5, 20), new(-4, 5, 20));     // the wall half across it
            Quad(new(-4, 2, 22), new(-1, 2, 22), new(-1, 2, 26), new(-4, 2, 26));     // a shelf of rock behind it, 2 m up
            Quad(new(2, 1.5f, 28), new(4, 1.5f, 28), new(4, 1.5f, 30), new(2, 1.5f, 30));   // a crate against the side: its top,
            Quad(new(2, 0, 28), new(2, 0, 30), new(2, 1.5f, 30), new(2, 1.5f, 28));         // its face to the tunnel
            Quad(new(2, 0, 28), new(4, 0, 28), new(4, 1.5f, 28), new(2, 1.5f, 28));         // and its two ends
            Quad(new(2, 0, 30), new(4, 0, 30), new(4, 1.5f, 30), new(2, 1.5f, 30));

            var mesh = CoverMesh.Build(vertices, indices);
            const int size = 51;
            var heights = new float[size * size];

            // From (-50, -50) every 2 m: the hill is 30 m high up to z = 40, the ground beyond is level with the tunnel's floor.
            for (var row = 0; row < size; row++)
                for (var column = 0; column < size; column++)
                    heights[row * size + column] = -50f + row * 2f < 40f ? 30f : 0f;

            mesh.SetTerrain(-50f, -50f, 2f, size, size, heights);

            return mesh;
        }

        private static Creature AddCreature(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = world.Map.MapInfo.MapContextId,
                Position = position,
                State = CharacterState.Normal,
                TargetCategory = TargetCategory.Hostile,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0),
                    [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };

            CellManager.Instance.AddToWorld(world.Map, creature);

            return creature;
        }

        private static void Remove(WorldTestContext world, params Creature[] creatures)
        {
            foreach (var creature in creatures)
                CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);
        }
    }
}
