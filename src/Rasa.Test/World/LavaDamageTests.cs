using System;
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

    /// <summary>
    /// Lava (LavaDamage): the lakes the maps place, and Burning on whoever stands in one - 500 to
    /// 1000 every three seconds, as the client's constants have it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LavaDamageTests
    {
        private const string LavaMap = "adv_arieki_torden_plains";
        private long _now;
        private int _roll;

        [TestInitialize]
        public void Start()
        {
            _now = 1_000_000;
            _roll = 700;
            LavaDamage.Now = () => _now;
            LavaDamage.Roll = (min, max) => _roll;
        }

        [TestCleanup]
        public void End() => LavaDamage.Reset();

        #region The lakes

        [TestMethod]
        public void TheMapsPlaceAHundredAndFortyThreeLakesOnFourteenMaps()
        {
            Assert.AreEqual(14, LavaSurfaces.ByMap.Count);
            Assert.AreEqual(143, LavaSurfaces.ByMap.Values.Sum(lakes => lakes.Length));
            Assert.IsTrue(LavaSurfaces.ByMap.Keys.All(name => name == name.ToLowerInvariant()), "map names as the server has them");
        }

        [TestMethod]
        public void EveryLakeIsASquareOfThirtyTwoSixtyFourOrAHundredAndTwentyEightMetres()
        {
            foreach (var (map, lakes) in LavaSurfaces.ByMap)
                foreach (var lake in lakes)
                {
                    var sides = new[]
                    {
                        Vector2.Distance(new Vector2(lake.X1, lake.Z1), new Vector2(lake.X2, lake.Z2)),
                        Vector2.Distance(new Vector2(lake.X2, lake.Z2), new Vector2(lake.X3, lake.Z3)),
                        Vector2.Distance(new Vector2(lake.X3, lake.Z3), new Vector2(lake.X4, lake.Z4)),
                        Vector2.Distance(new Vector2(lake.X4, lake.Z4), new Vector2(lake.X1, lake.Z1))
                    };

                    Assert.IsTrue(sides.All(side => MathF.Abs(side - sides[0]) < 0.05f), $"{map}: a lake's sides are {string.Join(", ", sides)}");
                    Assert.IsTrue(new[] { 32f, 64f, 128f }.Any(size => MathF.Abs(sides[0] - size) < 0.05f), $"{map}: a lake {sides[0]} m across");
                }
        }

        [TestMethod]
        public void InLavaIsInsideALakesFootprintWithFeetAtItsSurface()
        {
            var lake = LavaSurfaces.ByMap[LavaMap][0];
            var middle = Middle(lake);

            Assert.IsTrue(LavaDamage.InLava(LavaMap, middle), "standing on it");
            Assert.IsTrue(LavaDamage.InLava(LavaMap, middle + new Vector3(0, LavaDamage.Above - 0.01f, 0)));
            Assert.IsTrue(LavaDamage.InLava(LavaMap, middle - new Vector3(0, LavaDamage.Below - 0.01f, 0)));
            Assert.IsFalse(LavaDamage.InLava(LavaMap, middle + new Vector3(0, LavaDamage.Above + 0.05f, 0)), "the shore, a little higher");
            Assert.IsFalse(LavaDamage.InLava(LavaMap, middle + new Vector3(0, 1.5f, 0)), "in the air over it");
            Assert.IsFalse(LavaDamage.InLava(LavaMap, middle - new Vector3(0, LavaDamage.Below + 0.05f, 0)), "under it");
            Assert.IsFalse(LavaDamage.InLava(LavaMap, middle - new Vector3(0, 20f, 0)), "a cave below it");

            var outside = new Vector3(Math.Min(Math.Min(lake.X1, lake.X2), Math.Min(lake.X3, lake.X4)) - 300f, lake.SurfaceY, middle.Z);
            Assert.IsFalse(LavaSurfaces.ByMap[LavaMap].Any(other => other.Covers(outside.X, outside.Z)));
            Assert.IsFalse(LavaDamage.InLava(LavaMap, outside), "at its height, off to one side");

            Assert.IsFalse(LavaDamage.InLava("adv_foreas_concordia_wilderness", middle), "a map with no lava");
            Assert.IsFalse(LavaDamage.InLava(null, middle));
        }

        #endregion

        #region Burning

        [TestMethod]
        public void SteppingIntoLavaBurnsAtOnce()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake));

            LavaDamage.Worker(world.Map);

            var sent = Sent(client);
            var attached = sent.OfType<GameEffectAttachedPacket>().Single();
            Assert.AreEqual(LavaDamage.EffectTypeId, attached.EffectTypeId);
            Assert.AreEqual(1u, attached.EffectLevel, "the level its FX is at");
            Assert.AreEqual(0UL, attached.SourceId, "from nobody");
            Assert.IsTrue(attached.IsDebuff);
            Assert.IsNull(attached.Duration, "it lasts as long as they stay");

            var tick = sent.OfType<GameEffectTickPacket>().Single();
            Assert.AreEqual(attached.EffectId, tick.EffectId);
            Assert.AreEqual(GameEffectTickPacket.TickKind.Damage, tick.Kind);

            var hit = tick.Entries.Single();
            Assert.AreEqual(client.Player.EntityId, hit.EntityId);
            Assert.AreEqual(700, hit.Amount);
            Assert.AreEqual(DamageType.Fire, hit.DamageType);
            Assert.IsFalse(hit.DeathBlow);

            Assert.AreEqual(300, client.Player.Attributes[Attributes.Health].Current);
            Assert.IsNotNull(LavaDamage.BurningOn(client.Player));
            Assert.IsTrue(LavaDamage.BurningOn(client.Player).Environmental);
        }

        [TestMethod]
        [DataRow(1, 500)]
        [DataRow(500, 500)]
        [DataRow(1000, 1000)]
        [DataRow(5000, 1000)]
        public void ABurnIsBetweenTheClientsMinimumAndMaximum(int rolled, int taken)
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake), health: 5000);
            _roll = rolled;

            LavaDamage.Worker(world.Map);

            Assert.AreEqual(5000 - taken, client.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void TheRollItAsksForIsFiveHundredToAThousand()
        {
            using var world = Lava(out var lake);
            Stand(world, Middle(lake));
            (int Min, int Max) asked = default;
            LavaDamage.Roll = (min, max) => { asked = (min, max); return min; };

            LavaDamage.Worker(world.Map);

            Assert.AreEqual((500, 1000), asked);
            Assert.AreEqual(3000, LavaDamage.IntervalMs);
        }

        [TestMethod]
        public void ItBurnsAgainEveryThreeSecondsAndNotBefore()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake), health: 5000);

            LavaDamage.Worker(world.Map);
            Assert.AreEqual(1, Ticks(client));

            for (var i = 0; i < 5; i++)
            {
                _now += 500;
                LavaDamage.Worker(world.Map);
            }

            Assert.AreEqual(0, Ticks(client), "two and a half seconds on");

            _now += 500;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(1, Ticks(client), "three seconds on");
            Assert.AreEqual(5000 - 1400, client.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1, client.Player.ActiveEffects.Values.Count(effect => effect.TypeId == LavaDamage.EffectTypeId), "one Burning, not one a burn");
        }

        [TestMethod]
        public void AMoveIntoLavaBurnsWithoutWaitingForTheWorker()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake) + new Vector3(0, 5f, 0));

            LavaDamage.OnMove(client);
            Assert.AreEqual(0, Ticks(client), "over it");

            client.Player.Position = Middle(lake);
            LavaDamage.OnMove(client);
            Assert.AreEqual(1, Ticks(client));

            LavaDamage.Worker(world.Map);
            LavaDamage.OnMove(client);
            Assert.AreEqual(0, Ticks(client), "the Move and the worker share the one clock");
        }

        [TestMethod]
        public void SteppingOutAndBackInNeitherHastensNorDodgesTheNextBurn()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake), health: 5000);
            var shore = Middle(lake) + new Vector3(0, 2f, 0);

            LavaDamage.Worker(world.Map);
            Sent(client);

            // Out, and back in a second later: nothing yet.
            client.Player.Position = shore;
            _now += 1000;
            LavaDamage.Worker(world.Map);
            client.Player.Position = Middle(lake);
            _now += 100;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(0, Ticks(client));

            // Out over the moment it was due, and in again after it: it burns as they touch.
            client.Player.Position = shore;
            _now += 2500;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(0, Ticks(client), "not in it");
            client.Player.Position = Middle(lake);
            _now += 100;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(1, Ticks(client));
        }

        [TestMethod]
        public void BurningComesOffASecondAfterLeavingTheLava()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake), health: 5000);

            LavaDamage.Worker(world.Map);
            Sent(client);
            client.Player.Position = Middle(lake) + new Vector3(0, 2f, 0);

            _now += LavaDamage.LeaveGraceMs - 1;
            LavaDamage.Worker(world.Map);
            Assert.IsNotNull(LavaDamage.BurningOn(client.Player), "a jump's worth of time");
            Assert.IsEmpty(Sent(client).OfType<GameEffectDetachedPacket>().ToArray());

            _now += 1;
            LavaDamage.Worker(world.Map);
            Assert.IsNull(LavaDamage.BurningOn(client.Player));
            Assert.HasCount(1, Sent(client).OfType<GameEffectDetachedPacket>().ToArray());

            _now += 10_000;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(0, Ticks(client), "out of it, nothing burns");
            Assert.AreEqual(5000 - 700, client.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ArmourTakesTheBurnBeforeHealth()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake));
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 400, 400, 400, 0, 0);

            LavaDamage.Worker(world.Map);

            Assert.AreEqual(0, client.Player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(700, client.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void TheBurnThatTakesTheLastHealthKillsAndIsTheLast()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake), health: 600);

            LavaDamage.Worker(world.Map);

            Assert.AreEqual(CharacterState.Dead, client.Player.State);
            Assert.IsTrue(Sent(client).OfType<GameEffectTickPacket>().Single().Entries.Single().DeathBlow);

            _now += 60_000;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(0, Ticks(client), "the dead do not burn");
            Assert.IsNull(LavaDamage.BurningOn(client.Player));
        }

        [TestMethod]
        public void AnImmunityToFireStopsTheBurnAndTheClientIsToldSo()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake));
            client.Player.DamageImmunities.Add(DamageType.Fire);

            LavaDamage.Worker(world.Map);

            var hit = Sent(client).OfType<GameEffectTickPacket>().Single().Entries.Single();
            Assert.IsTrue(hit.WasImmune);
            Assert.AreEqual(1000, client.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void WhatKeepsDebuffsOffDoesNotKeepLavaOffAndCureDoesNotTakeItAway()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake));
            var guard = new GameEffect { TypeId = 181, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), BlocksDebuffs = true };
            GameEffectManager.Instance.Attach(world.Map, client.Player, guard);
            Assert.IsTrue(GameEffectManager.DebuffsBlocked(client.Player));
            Sent(client);

            LavaDamage.Worker(world.Map);

            var sent = Sent(client);
            Assert.IsEmpty(sent.OfType<GameEffectAttachFailedPacket>().ToArray(), "no \"Immune\"");
            Assert.HasCount(1, sent.OfType<GameEffectAttachedPacket>().ToArray());
            Assert.AreEqual(300, client.Player.Attributes[Attributes.Health].Current);
            Assert.IsFalse(AbilityManager.DebuffsOn(client.Player).Any(effect => effect.TypeId == LavaDamage.EffectTypeId));
        }

        [TestMethod]
        public void AnOrdinaryDebuffIsStillKeptOffAndStillCured()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake) + new Vector3(0, 5f, 0));
            var slow = new GameEffect { TypeId = 12345, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), IsBuff = false };
            GameEffectManager.Instance.Attach(world.Map, client.Player, slow);

            Assert.IsTrue(AbilityManager.DebuffsOn(client.Player).Contains(slow));

            var guard = new GameEffect { TypeId = 181, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), BlocksDebuffs = true };
            GameEffectManager.Instance.Attach(world.Map, client.Player, guard);
            var second = new GameEffect { TypeId = 12346, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), IsBuff = false };
            GameEffectManager.Instance.Attach(world.Map, client.Player, second);

            Assert.IsFalse(client.Player.ActiveEffects.ContainsKey(second.EffectId));
        }

        [TestMethod]
        public void AMapWithNoLavaIsLeftAlone()
        {
            using var world = new WorldTestContext();
            var lake = LavaSurfaces.ByMap[LavaMap][0];
            var client = Stand(world, Middle(lake));

            LavaDamage.Worker(world.Map);
            LavaDamage.OnMove(client);

            Assert.IsEmpty(Sent(client));
            Assert.AreEqual(1000, client.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void SomeoneStandingBesideTheLakeSeesTheBurn()
        {
            using var world = Lava(out var lake);
            var client = Stand(world, Middle(lake));
            var watcher = Stand(world, Middle(lake) + new Vector3(3f, 4f, 0));

            LavaDamage.Worker(world.Map);

            Assert.HasCount(1, Sent(watcher).OfType<GameEffectTickPacket>().ToArray());
            Assert.AreEqual(1000, watcher.Player.Attributes[Attributes.Health].Current, "above it, and not burnt");
        }

        #endregion

        #region Rivers, slopes and falls

        // Worked out apart from the server, from the client's meshes and .map files: the middle
        // of one triangle of lava as it is drawn, on a flat river, on a slope and on a fall.
        private const string RiverMap = "adv_arieki_ligo_burningsteps";
        private static readonly Vector3 OnARiver = new Vector3(-482.870f, 248.000f, 379.113f);
        private static readonly Vector3 OnASlope = new Vector3(-626.667f, 252.827f, 395.443f);
        private static readonly Vector3 SlopeNormal = new Vector3(0.424f, 0.906f, 0f);
        private const string FallMap = "adv_arieki_ligo_ashendesert_baneconscriptfacility";
        private static readonly Vector3 OnAFall = new Vector3(135.430f, 88.851f, -223.160f);

        /// <summary>A lava mesh's floor is this far under the lava as it is drawn.</summary>
        private const float Floor = 0.14f;

        [TestMethod]
        public void TheMapsPlaceFourHundredAndFiftyEightPiecesOfFlowingLavaOnTwelveMaps()
        {
            var flows = LavaFlows.ByMap.Values.SelectMany(placed => placed).ToList();

            Assert.AreEqual(12, LavaFlows.ByMap.Count);
            Assert.AreEqual(458, flows.Count);
            Assert.AreEqual(47, LavaFlows.Shapes.Count);
            Assert.AreEqual(369, flows.Count(flow => flow.Shape.StartsWith("terra_arieki_lava_river_")), "rivers, slopes and falls");
            Assert.AreEqual(85, flows.Count(flow => flow.Shape.StartsWith("terra_arieki_ligo_lava_wall_")), "cliff walls with a fall");
            Assert.AreEqual(2, flows.Count(flow => flow.Shape.StartsWith("terra_arieki_cavern_rocky_room_lava_")));
            Assert.AreEqual(2, flows.Count(flow => flow.Shape.StartsWith("arch_brann_wall_staal_detention_lava_")));
            Assert.IsFalse(LavaFlows.Shapes.Keys.Any(name => name.Contains("lake") || name.Contains("goo")), "the lakes are LavaSurfaces, and goo is not lava");
            Assert.IsTrue(LavaFlows.ByMap.Keys.All(name => name == name.ToLowerInvariant()));
        }

        [TestMethod]
        public void EveryPlacedPieceHasItsShapeAndEveryTriangleItsCorners()
        {
            foreach (var flow in LavaFlows.ByMap.Values.SelectMany(placed => placed))
            {
                Assert.IsTrue(LavaFlows.Shapes.ContainsKey(flow.Shape), flow.Shape);
                Assert.IsTrue(flow.Scale > 0);
                Assert.IsTrue(MathF.Abs(flow.Rotation.Length() - 1f) < 1e-4f);
            }

            foreach (var (name, shape) in LavaFlows.Shapes)
            {
                Assert.AreEqual(0, shape.Corners.Length % 3, name);
                Assert.AreEqual(0, shape.Triangles.Length % 3, name);
                Assert.IsTrue(shape.Triangles.Length > 0, name);
                Assert.IsTrue(shape.Triangles.All(corner => corner < shape.Corners.Length / 3), name);
            }
        }

        [TestMethod]
        public void EveryTriangleOfLavaIsLavaAtItsOwnMiddle()
        {
            foreach (var (map, flows) in LavaFlows.ByMap)
            {
                var triangles = 0;

                foreach (var flow in flows)
                {
                    var shape = LavaFlows.Shapes[flow.Shape];

                    for (var i = 0; i + 2 < shape.Triangles.Length; i += 3)
                    {
                        var middle = (Corner(flow, shape, shape.Triangles[i]) + Corner(flow, shape, shape.Triangles[i + 1]) + Corner(flow, shape, shape.Triangles[i + 2])) / 3f;

                        triangles++;
                        Assert.IsTrue(LavaDamage.InLava(map, middle), $"{map}: {flow.Shape} at {middle}");
                    }
                }

                Assert.AreEqual(triangles, LavaFlowFields.TriangleCount(map), map);
            }
        }

        [TestMethod]
        public void AFlatRiverIsLavaAtItsSurfaceAndAtItsFloor()
        {
            Assert.IsFalse(LavaSurfaces.ByMap[RiverMap].Any(lake => lake.Covers(OnARiver.X, OnARiver.Z) && MathF.Abs(lake.SurfaceY - OnARiver.Y) < 2f), "a river, with no lake under it");

            Assert.IsTrue(LavaDamage.InLava(RiverMap, OnARiver), "the lava as it is drawn");
            Assert.IsTrue(LavaDamage.InLava(RiverMap, OnARiver - new Vector3(0, Floor, 0)), "where feet are, on its floor");
            Assert.IsTrue(LavaDamage.InLava(RiverMap, OnARiver + new Vector3(0, LavaDamage.Above - 0.01f, 0)));
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnARiver + new Vector3(0, LavaDamage.Above + 0.05f, 0)), "the bank, a little higher");
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnARiver + new Vector3(0, 3f, 0)), "a bridge over it");
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnARiver - new Vector3(0, LavaDamage.Below + 0.05f, 0)), "under it");
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnARiver + new Vector3(0, 40f, 300f)), "nowhere near it");
        }

        [TestMethod]
        public void ASlopeIsLavaAlongItsRiseAtTheHeightItHasThere()
        {
            Assert.IsTrue(LavaDamage.InLava(RiverMap, OnASlope));
            Assert.IsTrue(LavaDamage.InLava(RiverMap, OnASlope - SlopeNormal * Floor), "on its floor");
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnASlope + new Vector3(0, 0.5f, 0)), "over it");
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnASlope - new Vector3(0, 1f, 0)), "under it");

            // A step up the slope is higher by the slope's rise, and still lava.
            var uphill = Vector3.Normalize(new Vector3(-SlopeNormal.X, 0, -SlopeNormal.Z)) * 0.5f;
            var rise = -(SlopeNormal.X * uphill.X + SlopeNormal.Z * uphill.Z) / SlopeNormal.Y;

            Assert.IsTrue(rise > 0.2f, "it climbs");
            Assert.IsTrue(LavaDamage.InLava(RiverMap, OnASlope + uphill + new Vector3(0, rise, 0)));
            Assert.IsFalse(LavaDamage.InLava(RiverMap, OnASlope + uphill + new Vector3(0, rise + 0.5f, 0)));
        }

        [TestMethod]
        public void AFallBurnsWhoeverIsWithinReachOfItsSheet()
        {
            Assert.IsTrue(LavaDamage.InLava(FallMap, OnAFall), "in the sheet");
            Assert.IsTrue(LavaDamage.InLava(FallMap, OnAFall + new Vector3(LavaDamage.Reach - 0.1f, 0, 0)), "against it");
            Assert.IsTrue(LavaDamage.InLava(FallMap, OnAFall - new Vector3(LavaDamage.Reach - 0.1f, 0, 0)), "against it from the other side");
            Assert.IsFalse(LavaDamage.InLava(FallMap, OnAFall + new Vector3(LavaDamage.Reach + 0.4f, 0, 0)), "a step back from it");
            Assert.IsFalse(LavaDamage.InLava(FallMap, OnAFall - new Vector3(LavaDamage.Reach + 0.4f, 0, 0)));
        }

        [TestMethod]
        public void StandingOnARiverBurnsAsALakeDoes()
        {
            using var world = new WorldTestContext();
            world.Map.MapInfo.MapName = RiverMap;
            var client = Stand(world, OnARiver - new Vector3(0, Floor, 0));

            LavaDamage.Worker(world.Map);

            var sent = Sent(client);
            Assert.AreEqual(LavaDamage.EffectTypeId, sent.OfType<GameEffectAttachedPacket>().Single().EffectTypeId);
            Assert.AreEqual(700, sent.OfType<GameEffectTickPacket>().Single().Entries.Single().Amount);
            Assert.AreEqual(300, client.Player.Attributes[Attributes.Health].Current);

            client.Player.Position = OnARiver + new Vector3(0, 3f, 0);
            _now += LavaDamage.IntervalMs;
            LavaDamage.Worker(world.Map);
            Assert.AreEqual(0, Ticks(client), "on the bridge over it");
        }

        [TestMethod]
        public void AMapWithRiversAndNoLakesIsWatchedToo()
        {
            var map = LavaFlows.ByMap.Keys.First(name => !LavaSurfaces.ByMap.ContainsKey(name));
            var flow = LavaFlows.ByMap[map].First(placed => placed.Shape.Contains("straight"));
            var shape = LavaFlows.Shapes[flow.Shape];
            var middle = (Corner(flow, shape, shape.Triangles[0]) + Corner(flow, shape, shape.Triangles[1]) + Corner(flow, shape, shape.Triangles[2])) / 3f;

            using var world = new WorldTestContext();
            world.Map.MapInfo.MapName = map;
            var client = Stand(world, middle - new Vector3(0, Floor, 0));

            LavaDamage.OnMove(client);

            Assert.AreEqual(1, Ticks(client), map);
        }

        private static Vector3 Corner(LavaFlow flow, LavaShape shape, int corner) =>
            flow.ToWorld(new Vector3(shape.Corners[corner * 3], shape.Corners[corner * 3 + 1], shape.Corners[corner * 3 + 2]));

        #endregion

        #region Helpers

        /// <summary>A world on a map that has lava, and the first of its lakes.</summary>
        private static WorldTestContext Lava(out WaterSurface lake)
        {
            var world = new WorldTestContext();
            world.Map.MapInfo.MapName = LavaMap;
            lake = LavaSurfaces.ByMap[LavaMap][0];
            return world;
        }

        private static Vector3 Middle(WaterSurface lake) =>
            new Vector3((lake.X1 + lake.X3) / 2, lake.SurfaceY, (lake.Z1 + lake.Z3) / 2);

        /// <summary>A living player at a place, with this much health and no armour, in the map's cells.</summary>
        private static Client Stand(WorldTestContext world, Vector3 at, int health = 1000)
        {
            var client = world.CreateClient(at.X, at.Z);
            client.Player.Position = at;
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, health, health, health, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static PythonPacket[] Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToArray();

        private static int Ticks(Client client) => Sent(client).OfType<GameEffectTickPacket>().Count();

        #endregion
    }
}
