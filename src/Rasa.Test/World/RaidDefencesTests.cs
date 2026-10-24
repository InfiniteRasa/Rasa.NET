using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.World;

    /// <summary>
    /// The raids and the control points' defences joined (RaidDefences): a raid's gates are the point's
    /// own gate shields, a raider held back by an AFS shield strikes it through the ordinary damage path,
    /// fighting or not, and a shield the raid brings down stands again whole two minutes later while the
    /// raid goes on - and is struck at again.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class RaidDefencesTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint PointId = 4;
        private const uint BanePool = 9611;
        private const uint AfsPool = 9612;
        private const uint FieldRow = 9708950;
        private const EntityClasses HumanGate = (EntityClasses)20000001;   // UsableForceFieldHumOutpostGateV01
        private const EntityClasses ThraxClass = (EntityClasses)3762;
        private const EntityClasses SoldierClass = (EntityClasses)29423;
        private const uint ThraxId = 4_100_000_021;
        private const uint SoldierId = 4_100_000_022;
        private static readonly Vector3 At = new Vector3(100, 0, 100);

        private long _now;
        private long _clock;
        private long _tick;
        private Func<long> _pdNow;
        private Func<long> _ffClock;
        private Func<long> _raidNow;
        private Func<long> _pointNow;
        private Func<long> _utcNow;
        private Func<uint, bool> _known;
        private ControlPointDefenceConfig _defence;
        private RaidConfig _raids;
        private ReinforcementConfig _reinforcements;
        private Func<ControlPoints.Point, IReadOnlyList<ForceFields.Field>> _fieldsOf;
        private Func<MapChannel, Creature, ForceFields.Field, bool> _strikeShield;
        private Func<Creature, bool> _strikesFields;
        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();

        [TestInitialize]
        public void Start()
        {
            _now = 5_000_000;
            _clock = 7_000_000;
            _tick = 10_000_000;
            _pdNow = PlacedDestructibles.Now;
            _ffClock = ForceFields.Clock;
            _raidNow = BaneRaids.Instance.Now;
            _pointNow = ControlPoints.Instance.Now;
            _utcNow = ControlPoints.Instance.UtcNow;
            _known = ControlPoints.Instance.KnownCreature;
            _defence = ControlPointFields.Config;
            _raids = BaneRaids.Instance.Config;
            _reinforcements = Reinforcements.Config;
            _fieldsOf = OutpostWalls.FieldsOf;
            _strikeShield = BaneRaids.Instance.StrikeShield;
            _strikesFields = ControlPointFields.StrikesFields;

            PlacedDestructibles.Now = () => _now;
            PlacedDestructibles.Roll = (min, max) => 50_000;
            ForceFields.Clock = () => _clock;
            ControlPointFields.Config = new ControlPointDefenceConfig();
            BaneRaids.Instance.Reset();
            BaneRaids.Instance.Now = () => _tick;
            BaneRaids.Instance.Config = new RaidConfig();
            Reinforcements.Config = new ReinforcementConfig();
            ControlPoints.Instance.Now = () => _tick;
            ControlPoints.Instance.UtcNow = () => 1_700_000_000_000;
            ControlPoints.Instance.KnownCreature = id => true;

            AddClass(HumanGate, "UsableForceFieldHumOutpostGateV01", AugmentationType.ForceField);
            AddClass(ThraxClass, "Bane_Thrax_Soldier_Pistol", AugmentationType.Creature);
            AddClass(SoldierClass, "Redshirt_Human_Soldier_Light_Male", AugmentationType.Creature);

            CreatureManager.Instance.LoadedCreatures[ThraxId] = new Creature
            {
                DbId = ThraxId, EntityClass = ThraxClass, TargetCategory = TargetCategory.Hostile, WalkSpeed = 5, RunSpeed = 9, Level = 11
            };
            CreatureManager.Instance.LoadedCreatures[SoldierId] = new Creature
            {
                DbId = SoldierId, EntityClass = SoldierClass, TargetCategory = TargetCategory.Friendly, WalkSpeed = 5, RunSpeed = 9, Level = 8
            };
        }

        [TestCleanup]
        public void Restore()
        {
            foreach (var point in ControlPoints.Instance.Points.ToList())
                BaneRaids.Instance.Stop(point.Id);

            BaneRaids.Instance.Reset();
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
            PlacedDestructibles.Now = _pdNow;
            ForceFields.Clock = _ffClock;
            ControlPointFields.Config = _defence;
            BaneRaids.Instance.Now = _raidNow;
            BaneRaids.Instance.Config = _raids;
            Reinforcements.Config = _reinforcements;
            ControlPoints.Instance.Now = _pointNow;
            ControlPoints.Instance.UtcNow = _utcNow;
            ControlPoints.Instance.KnownCreature = _known;
            OutpostWalls.FieldsOf = _fieldsOf;
            BaneRaids.Instance.StrikeShield = _strikeShield;
            ControlPointFields.StrikesFields = _strikesFields;
            CreatureManager.Instance.LoadedCreatures.Remove(ThraxId);
            CreatureManager.Instance.LoadedCreatures.Remove(SoldierId);

            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s => s.SpawnPool != null && Reinforcements.IsTransient(s.SpawnPool)).ToList())
            {
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                EntityManager.Instance.FreeEntity(ship.EntityId);
            }

            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            for (var i = _classes.Count - 1; i >= 0; i--)
            {
                if (_classes[i].Had)
                    classes[_classes[i].Id] = _classes[i].Was;
                else
                    classes.Remove(_classes[i].Id);
            }
        }

        [TestMethod]
        public void JoinedTheRaidsGatesAreThePointsOwnShields()
        {
            using var world = new WorldTestContext();
            var point = Placed(world);

            // A faction field near the point that is not one of its gate shields.
            var stray = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, At + new Vector3(40, 0, 0), (float)(Math.PI / 2), 100);

            try
            {
                Assert.AreEqual(2, OutpostWalls.GatesOf(point).Count, "unjoined: every faction field within 120 m");

                RaidDefences.Join();

                var shield = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;
                CollectionAssert.AreEqual(new[] { shield }, OutpostWalls.FieldsOf(point).ToList(), "joined: the point's own (control_point_link kind 7)");
                Assert.AreEqual(1, OutpostWalls.GatesOf(point).Count);
            }
            finally
            {
                ForceFields.Remove(stray);
            }
        }

        [TestMethod]
        public void ARaiderHeldByAnAfsShieldStrikesItAndItRebuildsWhileTheRaidGoesOn()
        {
            using var world = new WorldTestContext();
            var point = Placed(world);
            world.CreateClient(At.X + 50, At.Z);

            RaidDefences.Join();

            var live = PlacedDestructibles.LiveOf(world.Map, FieldRow);
            var shield = live.Field;
            Assert.AreEqual(ForceFields.Side.A, shield.Side, "the AFS hold the point: their shield");

            var raid = BaneRaids.Instance.Begin(world.Map, point);
            Assert.IsNotNull(raid);
            BaneRaids.Instance.Worker(world.Map);

            // Held back before the gate, outside, not in a fight: a raid's raider all the same.
            var (middle, _) = ControlPointFields.Opening(shield);
            var raider = Raider(world, raid, new Vector3(middle.X, 0, middle.Z - 4));
            Assert.IsTrue(BaneRaids.Instance.IsRaider(raider));
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, raider.Controller.CurrentAction);
            Assert.IsFalse(ControlPointFields.DefaultStrikesFields(raider), "the open world's rule alone would leave it standing");
            Assert.IsTrue(ControlPointFields.StrikesFields(raider), "the raids widened it");

            world.Map.QueuedMissiles.Clear();

            // The raid's breach (BaneRaids.Breach -> StrikeShield = ControlPointFields.Strike).
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(shield.Object.EntityId, world.Map.QueuedMissiles.Single(m => m.Source == raider).TargetEntityId, "it struck the shield");
            Assert.IsTrue(raider.Actions[0].CooldownTimer > 0, "its attack cools down");

            MissileManager.Instance.DoWork(world.Map, 10_000);
            Assert.AreEqual(shield.MaxHealth - 40, shield.Health, "through the ordinary missile path");

            // A step the shield stops (BehaviorManager.UpdateEntityMovement -> ControlPointFields.Blocked) strikes too, not fighting.
            raider.Actions[0].CooldownTimer = 0;
            ControlPointFields.Blocked(world.Map, raider, shield);
            Assert.AreEqual(shield.Object.EntityId, world.Map.QueuedMissiles.Single(m => m.Source == raider).TargetEntityId);
            MissileManager.Instance.DoWork(world.Map, 10_000);
            Assert.AreEqual(shield.MaxHealth - 80, shield.Health);

            // Brought down: back whole two minutes later, the raid on all the while.
            ForceFields.Damage(shield, shield.Health, raider.EntityId);
            Assert.IsTrue(shield.IsDestroyed);
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(_now + ControlPointFields.Config.FieldRebuildSeconds * 1000L, live.RespawnAt);

            raider.Actions[0].CooldownTimer = 0;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsFalse(world.Map.QueuedMissiles.Any(m => m.Source == raider), "nothing to strike while it is down");

            _now += ControlPointFields.Config.FieldRebuildSeconds * 1000L - 1;
            _tick += 30_000;
            PlacedDestructibles.Worker(world.Map);
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsTrue(live.IsDown);
            Assert.AreSame(raid, BaneRaids.Instance.Of(point.Id), "the raid goes on");

            _now += 1;
            PlacedDestructibles.Worker(world.Map);
            var rebuilt = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;
            Assert.IsFalse(rebuilt.IsDestroyed);
            Assert.AreEqual(rebuilt.MaxHealth, rebuilt.Health, "whole");
            Assert.AreEqual(ForceFields.Side.A, rebuilt.Side, "still the AFS's");

            raider.Actions[0].CooldownTimer = 0;
            _tick += 1_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreSame(raid, BaneRaids.Instance.Of(point.Id));
            Assert.AreEqual(rebuilt.Object.EntityId, world.Map.QueuedMissiles.Single(m => m.Source == raider).TargetEntityId, "struck at again");
            world.Map.QueuedMissiles.Clear();
        }

        #region Fixtures

        private void AddClass(EntityClasses id, string name, AugmentationType augmentation)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, (short)(augmentation == AugmentationType.ForceField ? 1 : 0), new List<AugmentationType> { augmentation }, true);
        }

        /// <summary>The Landing Zone, the AFS's, with a Bane and an AFS garrison pool and one gate shield (an AFS row) 40 m south of it.</summary>
        private static ControlPoints.Point Placed(WorldTestContext world)
        {
            world.Map.SpawnPools.Add(Pool(world, BanePool, SpawnPoolManager.ModeControlPoint, At));
            world.Map.SpawnPools.Add(Pool(world, AfsPool, SpawnPoolManager.ModeAutomatic, At + new Vector3(5, 0, 0)));

            var links = new List<ControlPointLinkEntry>
            {
                new() { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindAfsPool, ObjectId = AfsPool },
                new() { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = BanePool },
                new() { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindField, ObjectId = FieldRow }
            };

            ControlPoints.Instance.Load(new[]
            {
                new ControlPointEntry
                {
                    Id = PointId, MapContextId = MapId, Name = "Landing Zone", ClassId = 3814,
                    PosX = At.X, PosY = At.Y, PosZ = At.Z, Rotation = 0, MarkerEntityId = 0, DefaultOwner = ControlPointEntry.OwnerAfs
                }
            }, links, null);
            ControlPoints.Instance.Place(world.Map);

            PlacedDestructibles.Load(new[]
            {
                new WorldDestructibleEntry
                {
                    Id = FieldRow, MapContextId = MapId, ClassId = (uint)HumanGate, PosX = At.X, PosY = 0, PosZ = At.Z - 40, Rotation = 0,
                    HitPoints = 6000, Side = WorldDestructibleEntry.SideAfs, Comment = "fixture"
                }
            });
            PlacedDestructibles.Place(world.Map);

            var point = ControlPoints.Instance.ById(PointId);

            // As the object worker would have it: on the map.
            point.Object.IsInWorld = true;

            return point;
        }

        private static SpawnPool Pool(WorldTestContext world, uint id, short mode, Vector3 at) => new SpawnPool
        {
            DbId = id,
            Mode = mode,
            MapContextId = MapId,
            RuntimeMapChannel = world.Map,
            Position = at,
            Radius = 10,
            RespawnTime = 90_000,
            SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(id == AfsPool ? SoldierId : ThraxId, 1, 3) }
        };

        /// <summary>One of the raid, armed, standing at a spot on the map.</summary>
        private static Creature Raider(WorldTestContext world, BaneRaids.Raid raid, Vector3 at)
        {
            var creature = new Creature
            {
                DbId = ThraxId,
                Name = "Raider",
                EntityClass = ThraxClass,
                TargetCategory = TargetCategory.Hostile,
                SpawnPool = raid.Pool,
                MapContextId = MapId,
                RuntimeMapChannel = world.Map,
                Position = at,
                State = CharacterState.Idle,
                Level = 10,
                WalkSpeed = 5,
                RunSpeed = 9,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = at;
            creature.HomePos.MapContextid = MapId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Actions.Add(new CreatureAction
            {
                ActionId = ActionId.WeaponAttack, ActionArgId = 133, RangeMin = 1, RangeMax = 24, MinDamage = 40, MaxDamage = 40, Cooldown = 1000
            });
            BehaviorManager.StartWandering(creature, false);
            raid.Pool.AliveCreatures++;
            CellManager.Instance.AddToWorld(world.Map, creature);

            return creature;
        }

        #endregion
    }
}
