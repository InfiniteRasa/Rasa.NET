using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using Rasa.Test.TestSupport;

    /// <summary>
    /// The missions' radiation and toxic gas (MissionHazards): the volumes' shapes, the two missions' zones, the damage
    /// through armour, resistances and immunities and DamageScale, the gas's share of maximum health, a mission's zone only
    /// for those who hold the mission, whom they spare, the effect lingering a little and then running out, a death in
    /// them, and the GM command that places, lists, switches and removes them.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionHazardsTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map

        private HazardsConfig _config;
        private Func<int, int, int> _roll;
        private Func<IEnumerable<(uint, uint, Vector3, string)>> _hospitals;
        private Func<uint, bool> _safe;
        private Action<Client, CharacterTeleporterEntry> _persist;

        [TestInitialize]
        public void Setup()
        {
            _config = MissionHazards.Config;
            _roll = MissionHazards.Roll;
            MissionHazards.Config = new HazardsConfig();
            MissionHazards.Roll = (min, max) => max;
            MissionHazards.ClearPlaced();

            // Somewhere to be offered after a death (PlayerDeathTests).
            _hospitals = Hospitals.Source;
            _safe = Hospitals.IsSafeZone;
            _persist = Hospitals.Persist;
            Hospitals.Source = () => new[] { (103u, MapId, new Vector3(100, 0, 0), "Hospital: Alia Das") };
            Hospitals.IsSafeZone = id => false;
            Hospitals.Persist = (client, entry) => { };
            Hospitals.Reset();
        }

        [TestCleanup]
        public void TearDown()
        {
            MissionHazards.Config = _config;
            MissionHazards.Roll = _roll;
            MissionHazards.ClearPlaced();
            Hospitals.Source = _hospitals;
            Hospitals.IsSafeZone = _safe;
            Hospitals.Persist = _persist;
            Hospitals.Reset();
        }

        // ---- shapes

        private static HazardVolume Circle(Vector3 at, float radius, float minY, float maxY) => new HazardVolume
        {
            Id = 1, MapContextId = MapId, Kind = HazardVolume.HazardKind.Radiation, Shape = HazardVolume.HazardShape.Circle,
            Position = at, Radius = radius, MinY = minY, MaxY = maxY, EffectTypeId = 200, EffectLevel = 1, TickMs = 2000
        };

        private static HazardVolume Box(float halfX, float halfZ, float rotation) => new HazardVolume
        {
            Id = 1, MapContextId = MapId, Kind = HazardVolume.HazardKind.ToxicGas, Shape = HazardVolume.HazardShape.Box,
            Position = Vector3.Zero, HalfX = halfX, HalfZ = halfZ, Rotation = rotation, MinY = -1, MaxY = 1, EffectTypeId = 423, TickMs = 2000
        };

        [TestMethod]
        public void ACircleHoldsItsRadiusBetweenItsHeights()
        {
            var circle = Circle(new Vector3(10, 50, 10), 5, 45, 60);

            Assert.IsTrue(circle.Contains(new Vector3(10, 50, 10)));
            Assert.IsTrue(circle.Contains(new Vector3(13, 45, 14)), "5 m out, on the floor");
            Assert.IsFalse(circle.Contains(new Vector3(14, 50, 14)), "5.7 m out");
            Assert.IsFalse(circle.Contains(new Vector3(10, 44.9f, 10)), "under it");
            Assert.IsFalse(circle.Contains(new Vector3(10, 60.1f, 10)), "over it");
            Assert.AreEqual(0, circle.Distance(new Vector3(12, 0, 10)));
            Assert.AreEqual(5, circle.Distance(new Vector3(20, 0, 10)), 1e-4);
        }

        [TestMethod]
        public void ABoxHoldsItsRectangleAndTurns()
        {
            var box = Box(10, 2, 0);

            Assert.IsTrue(box.Contains(new Vector3(9.9f, 0, 1.9f)));
            Assert.IsFalse(box.Contains(new Vector3(10.1f, 0, 0)));
            Assert.IsFalse(box.Contains(new Vector3(0, 0, 2.1f)));
            Assert.AreEqual(3, box.Distance(new Vector3(0, 0, 5)), 1e-4);

            // A quarter turn: the box's own x runs along world -z.
            var quarter = Box(10, 2, MathF.PI / 2);

            Assert.IsTrue(quarter.Contains(new Vector3(0, 0, 9)));
            Assert.IsTrue(quarter.Contains(new Vector3(1.9f, 0, -9)));
            Assert.IsFalse(quarter.Contains(new Vector3(9, 0, 0)), "its long side is not along x any more");
        }

        // ---- the missions' zones

        [TestMethod]
        public void TheZonesAreMislaidPlansRadiationAndChambersOfHorrorsGasOnTheirMapsGround()
        {
            Assert.HasCount(2, MissionHazards.Zones);

            var radiation = MissionHazards.Zones.Single(z => z.Kind == HazardVolume.HazardKind.Radiation);
            Assert.AreEqual(1497u, radiation.MapContextId, "Plateau");
            Assert.AreEqual(965u, radiation.MissionId);
            Assert.AreEqual(200, radiation.EffectTypeId, "RADIATION_POISON");
            Assert.AreEqual(1u, radiation.EffectLevel);
            Assert.AreEqual(DamageType.Environmental, radiation.DamageType);

            var gas = MissionHazards.Zones.Single(z => z.Kind == HazardVolume.HazardKind.ToxicGas);
            Assert.AreEqual(2028u, gas.MapContextId, "Abyss");
            Assert.AreEqual(1344u, gas.MissionId);
            Assert.AreEqual(423, gas.EffectTypeId, "SCRIPTED_PERCENT_DOT");
            Assert.AreEqual(2u, gas.EffectLevel, "its toxic gas tooltip");
            Assert.AreEqual(DamageType.Virulent, gas.DamageType);
            Assert.AreEqual(4.0, gas.PercentMaxHealth);

            // Each stands where the server's own walkable ground is, within its heights.
            var navmeshes = Path.Combine(RepositoryRoot(), "navmesh");

            foreach (var (zone, map) in new[] { (radiation, "adv_foreas_valverde_plateau"), (gas, "adv_arieki_torden_abyss") })
            {
                var nav = TestNavMeshes.Query(NavMeshFile.PathFor(navmeshes, map));
                var ground = nav.NearestInColumn(zone.Position, 30f);

                Assert.IsNotNull(ground, $"{zone.Comment}: walkable ground under its middle");
                Assert.IsTrue(zone.Contains(ground.Value), $"{zone.Comment}: the ground {ground.Value} is inside it (y {zone.MinY}..{zone.MaxY})");
            }
        }

        // ---- the hurt

        [TestMethod]
        public void RadiationHurtsThroughArmourEveryTwoSecondsAsEnvironmentalWithNobodysName()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;
            MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, null);

            MissionHazards.Worker(world.Map);

            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.RadiationEffectTypeId);
            Assert.AreEqual(1u, effect.EffectLevel);
            Assert.IsTrue(effect.Environmental, "the world's: Cure neither keeps it off nor takes it off");
            Assert.IsFalse(effect.IsBuff);
            Assert.AreEqual(0ul, effect.SourceId);
            Assert.AreEqual(2000, effect.TickIntervalMs);
            MissionTestContext.Drain(client);

            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(1000 - 105, player.Attributes[Attributes.Health].Current, "the roll's most, 105");
            var entry = MissionTestContext.Drain(client).OfType<GameEffectTickPacket>().Single().Entries.Single();
            Assert.AreEqual(105, entry.Amount);
            Assert.AreEqual(DamageType.Environmental, entry.DamageType);
            Assert.IsFalse(entry.DeathBlow);

            // The next is two seconds on, not at the next pass.
            GameEffectManager.Instance.DoWork(world.Map, 500);
            Assert.AreEqual(1000 - 105, player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ArmourTakesItFirstAndDamageScaleScalesIt()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0).Player;
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 30, 30, 30, 0, 0);
            MissionHazards.Config = new HazardsConfig { DamageScale = 200 };
            MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, null);

            MissionHazards.Worker(world.Map);
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(0, player.Attributes[Attributes.Armor].Current, "armour first");
            Assert.AreEqual(1000 - (210 - 30), player.Attributes[Attributes.Health].Current, "twice 105, 30 of it on the armour");
        }

        [TestMethod]
        public void TheToxicGasTakesAShareOfMaximumHealthAndResistancesAndImmunitiesCount()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 5000, 5000, 5000, 0, 0);
            MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.ToxicGas, 10, null);

            MissionHazards.Worker(world.Map);
            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.ToxicGasEffectTypeId);
            Assert.AreEqual(2u, effect.EffectLevel);
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(4800, player.Attributes[Attributes.Health].Current, "4 % of 5000");
            Assert.AreEqual(DamageType.Virulent, MissionTestContext.Drain(client).OfType<GameEffectTickPacket>().Single().Entries.Single().DamageType);

            // A resistance to Virulent (Hazmat Armor's, say) takes its share off.
            player.ResistanceData = new List<ResistanceData> { new ResistanceData(DamageType.Virulent, 25) };
            effect.NextTickTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);

            var expected = (int)Math.Round(200 * GameEffectManager.ResistMultiplier(25));
            Assert.IsLessThan(200, expected);
            Assert.AreEqual(4800 - expected, player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(200 - expected, MissionTestContext.Drain(client).OfType<GameEffectTickPacket>().Single().Entries.Single().Resisted);

            // Immune to it: nothing, and "Immune" in the tick.
            player.DamageImmunities.Add(DamageType.Virulent);
            effect.NextTickTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(4800 - expected, player.Attributes[Attributes.Health].Current);
            Assert.IsTrue(MissionTestContext.Drain(client).OfType<GameEffectTickPacket>().Single().Entries.Single().WasImmune);
        }

        // ---- who it is for

        [TestMethod]
        public void AMissionsZoneIsOnlyForThoseWhoHoldTheMission()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0).Player;
            var volume = MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, MissionHazards.MislaidPlansMissionId);

            Assert.IsEmpty(MissionHazards.At(new[] { volume }, player), "without the mission");
            MissionHazards.Worker(world.Map);
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == MissionHazards.RadiationEffectTypeId));

            player.Missions[965] = new MissionLog(965, MissionState.Active, false);
            Assert.HasCount(1, MissionHazards.At(new[] { volume }, player));

            MissionHazards.Worker(world.Map);
            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.RadiationEffectTypeId);

            // Turned in: no longer, and what is on comes off at its next tick without a hurt.
            player.Missions[965].State = MissionState.Completed;
            Assert.IsEmpty(MissionHazards.At(new[] { volume }, player));
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.IsFalse(player.ActiveEffects.ContainsKey(effect.EffectId));
            Assert.AreEqual(1000, player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void TheDeadTheImmuneAndGmsWhoMayNotDieAreSpared()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;
            MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, null);

            void Untouched(string why)
            {
                MissionHazards.Worker(world.Map);
                Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == MissionHazards.RadiationEffectTypeId), why);
            }

            player.State = CharacterState.Dead;
            Untouched("dead");
            player.State = CharacterState.Dying;
            Untouched("dying");
            player.State = CharacterState.Idle;

            player.ImmuneToAllDamage = true;
            Untouched(".immune all");
            player.ImmuneToAllDamage = false;

            PlayerDeathTests.GameMaster(client);
            Untouched("a GM who may not die");
            player.AllowDeath = true;
            MissionHazards.Worker(world.Map);
            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == MissionHazards.RadiationEffectTypeId), "a GM who said .allowdeath is touched");

            // A creature carrying the effect is not the volumes' to hurt: it loses it.
            var creature = new Creature { MapContextId = MapId, RuntimeMapChannel = world.Map, Position = Vector3.Zero, State = CharacterState.Idle };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.RadiationEffectTypeId);
            effect.OnTick(world.Map, creature, effect);
            Assert.AreEqual(100, creature.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void SwitchedOffNothingHurtsAndWhatIsOnComesOff()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;
            var volume = MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, null);

            MissionHazards.Config = new HazardsConfig { Enabled = false };
            MissionHazards.Worker(world.Map);
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == MissionHazards.RadiationEffectTypeId), "the section off");

            MissionHazards.Config = new HazardsConfig();
            MissionHazards.Worker(world.Map);
            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.RadiationEffectTypeId);

            // The GM's switch: off, and off at the next tick.
            MissionHazards.GmCommand(client, new[] { ".hazard", volume.Id.ToString(), "off" });
            Assert.IsFalse(volume.Enabled);
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.IsFalse(player.ActiveEffects.ContainsKey(effect.EffectId));
            Assert.AreEqual(1000, player.Attributes[Attributes.Health].Current);
            MissionHazards.Worker(world.Map);
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == MissionHazards.RadiationEffectTypeId));
        }

        [TestMethod]
        public void ItLingersALittleAfterLeavingAndThenRunsOut()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0).Player;
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, null);

            var before = Environment.TickCount64;
            MissionHazards.Worker(world.Map);
            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.RadiationEffectTypeId);
            Assert.IsTrue(effect.ExpiresTick >= before + MissionHazards.LingerMs && effect.ExpiresTick <= Environment.TickCount64 + MissionHazards.LingerMs);

            // Still in it a while later: the same effect, its end pushed out.
            effect.ExpiresTick = Environment.TickCount64 + 100;
            MissionHazards.Worker(world.Map);
            Assert.AreSame(effect, player.ActiveEffects.Values.Single(e => e.TypeId == MissionHazards.RadiationEffectTypeId));
            Assert.IsGreaterThan(Environment.TickCount64 + MissionHazards.LingerMs - 1000, effect.ExpiresTick);

            // Out of it: nothing pushes it out any more, it still hurts while it lasts, and then it is gone.
            player.Position = new Vector3(50, 0, 0);
            MissionHazards.Worker(world.Map);
            GameEffectManager.Instance.DoWork(world.Map, 500);
            Assert.AreEqual(100000 - 105, player.Attributes[Attributes.Health].Current, "the linger still hurts");

            effect.ExpiresTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);
            Assert.IsFalse(player.ActiveEffects.ContainsKey(effect.EffectId));
            MissionHazards.Worker(world.Map);
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == MissionHazards.RadiationEffectTypeId));
        }

        [TestMethod]
        public void AHurtThatTakesTheLastOfTheirHealthKillsWithNoKiller()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;
            player.Attributes[Attributes.Health].Current = 10;
            MissionHazards.Place(MapId, Vector3.Zero, HazardVolume.HazardKind.Radiation, 10, null);

            MissionHazards.Worker(world.Map);
            MissionTestContext.Drain(client);
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(CharacterState.Dead, player.State);
            var packets = MissionTestContext.Drain(client);
            Assert.IsTrue(packets.OfType<GameEffectTickPacket>().Single().Entries.Single().DeathBlow);
            Assert.AreEqual(0UL, packets.OfType<PlayerDeadPacket>().Single().SourceId, "no killer");

            var left = player.ActiveEffects.Values.Where(e => e.TypeId == MissionHazards.RadiationEffectTypeId).ToList();
            MissionHazards.Worker(world.Map);
            CollectionAssert.AreEqual(left, player.ActiveEffects.Values.Where(e => e.TypeId == MissionHazards.RadiationEffectTypeId).ToList(), "the dead are not touched again");
        }

        // ---- the GM command

        [TestMethod]
        public void TheGmPlacesListsSwitchesAndRemovesVolumes()
        {
            using var world = new WorldTestContext();
            var gm = PlayerDeathTests.Player(world, 5, 5);

            MissionHazards.GmCommand(gm, new[] { ".hazard", "place", "toxic", "15", "1344" });

            var placed = MissionHazards.OnMap(MapId, gm.Player.Position).Single();
            Assert.AreEqual(MissionHazards.FirstPlacedId, placed.Id);
            Assert.AreEqual(HazardVolume.HazardKind.ToxicGas, placed.Kind);
            Assert.AreEqual(15f, placed.Radius);
            Assert.AreEqual(1344u, placed.MissionId);
            Assert.IsTrue(placed.Placed);
            Assert.IsTrue(placed.Contains(gm.Player.Position));

            MissionHazards.GmCommand(gm, new[] { ".hazard", "place", "radiation" });
            Assert.HasCount(2, MissionHazards.OnMap(MapId, gm.Player.Position));
            Assert.AreEqual(10f, MissionHazards.Find(MissionHazards.FirstPlacedId + 1).Radius, "10 m by default");
            Assert.IsNull(MissionHazards.Find(MissionHazards.FirstPlacedId + 1).MissionId, "everyone's");

            // Wrong words change nothing.
            MissionHazards.GmCommand(gm, new[] { ".hazard", "place", "lava" });
            MissionHazards.GmCommand(gm, new[] { ".hazard", "place", "radiation", "-3" });
            MissionHazards.GmCommand(gm, new[] { ".hazard", "99", "on" });
            Assert.HasCount(2, MissionHazards.OnMap(MapId, gm.Player.Position));

            // The list is said, not changed.
            WorldTestContext.Drain(gm);
            MissionHazards.GmCommand(gm, new[] { ".hazard" });
            Assert.IsNotEmpty(WorldTestContext.Drain(gm));
            Assert.HasCount(2, MissionHazards.OnMap(MapId, gm.Player.Position));

            // A mission's zone is switched, not removed.
            MissionHazards.GmCommand(gm, new[] { ".hazard", "1", "off" });
            Assert.IsFalse(MissionHazards.Find(1).Enabled);
            MissionHazards.GmCommand(gm, new[] { ".hazard", "remove", "1" });
            Assert.IsNotNull(MissionHazards.Find(1));
            MissionHazards.GmCommand(gm, new[] { ".hazard", "1", "on" });
            Assert.IsTrue(MissionHazards.Find(1).Enabled);

            MissionHazards.GmCommand(gm, new[] { ".hazard", "remove", MissionHazards.FirstPlacedId.ToString() });
            Assert.IsNull(MissionHazards.Find(MissionHazards.FirstPlacedId));
            Assert.HasCount(1, MissionHazards.OnMap(MapId, gm.Player.Position));
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
