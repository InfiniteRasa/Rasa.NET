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
    /// The shields in the gates of the control points' outposts (ControlPointFields): placed fields
    /// linked to their point, its holder's on the point's own channel, back whole when it changes
    /// hands and two minutes after they are brought down, powering up when left alone, struck at by
    /// the fighting creatures they stop, calling their side to whoever hits them; and what the raids
    /// are told.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ControlPointFieldsTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint PointId = 7;
        private const uint FieldRow = 9708900;
        private const uint OtherRow = 9708901;
        private const EntityClasses BaneGate = (EntityClasses)9572;   // UsableForceFieldBaneOutpostWallMajorV01
        private const EntityClasses HumanGate = (EntityClasses)20000001;

        private long _now;
        private long _clock;
        private Func<long> _pdNow;
        private Func<long> _ffClock;
        private Func<uint, bool> _known;
        private ControlPointDefenceConfig _config;
        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();
        private readonly List<Creature> _registered = new();

        [TestInitialize]
        public void Start()
        {
            _now = 5_000_000;
            _clock = 7_000_000;
            _pdNow = PlacedDestructibles.Now;
            _ffClock = ForceFields.Clock;
            _known = ControlPoints.Instance.KnownCreature;
            _config = ControlPointFields.Config;

            PlacedDestructibles.Now = () => _now;
            PlacedDestructibles.Roll = (min, max) => 50_000;
            ForceFields.Clock = () => _clock;
            ControlPoints.Instance.KnownCreature = id => true;
            ControlPointFields.Config = new ControlPointDefenceConfig();

            AddClass(BaneGate, "UsableForceFieldBaneOutpostWallMajorV01");
            AddClass(HumanGate, "UsableForceFieldHumOutpostGateV01");
        }

        [TestCleanup]
        public void Restore()
        {
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            ControlPoints.Instance.KnownCreature = _known;
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
            PlacedDestructibles.Now = _pdNow;
            ForceFields.Clock = _ffClock;
            ControlPointFields.Config = _config;
            ControlPointFields.StrikesFields = ControlPointFields.DefaultStrikesFields;

            foreach (var creature in _registered)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
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
        public void APointsShieldIsItsHoldersOnThePointsOwnChannel()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);

            var live = PlacedDestructibles.LiveOf(world.Map, FieldRow);
            var other = PlacedDestructibles.LiveOf(world.Map, OtherRow);

            // The row says the AFS's; the Bane hold the point.
            Assert.AreEqual(ForceFields.Side.B, live.Field.Side);
            Assert.AreEqual(PointId, live.Field.ControlPointId);
            Assert.AreSame(point, ControlPointFields.PointOf(live.Field));
            Assert.AreSame(point, ControlPointFields.PointFor(world.Map, FieldRow));
            CollectionAssert.AreEqual(new[] { live.Field }, ControlPointFields.Of(point).ToList());
            CollectionAssert.AreEqual(new[] { live.Field }, ControlPointFields.Of(PointId).ToList());
            Assert.IsTrue(ControlPointFields.AnyUp(point) && ControlPointFields.AllUp(point));

            // A field of no point is as its row has it.
            Assert.AreEqual((ForceFields.Side.A, 0u), (other.Field.Side, other.Field.ControlPointId));
            Assert.IsNull(ControlPointFields.PointOf(other.Field));
        }

        [TestMethod]
        public void WithTheRuleOffAShieldStandsOnItsRowsSide()
        {
            ControlPointFields.Config = new ControlPointDefenceConfig { FieldsFollowOwner = false };

            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);
            var live = PlacedDestructibles.LiveOf(world.Map, FieldRow);

            Assert.AreEqual(ForceFields.Side.A, live.Field.Side);

            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            Assert.AreEqual(ForceFields.Side.A, PlacedDestructibles.LiveOf(world.Map, FieldRow).Field.Side, "left as it is");
        }

        [TestMethod]
        public void ACopyOfTheMapHasTheRowsAsTheyAre()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerBane);

            using var copy = new WorldTestContext();
            copy.Map.IsPrivateInstance = true;

            try
            {
                PlacedDestructibles.Place(copy.Map);
                var live = PlacedDestructibles.LiveOf(copy.Map, FieldRow);

                Assert.AreEqual((ForceFields.Side.A, 0u), (live.Field.Side, live.Field.ControlPointId));
                Assert.IsNull(ControlPointFields.PointFor(copy.Map, FieldRow));
            }
            finally
            {
                PlacedDestructibles.Forget(copy.Map);
                copy.Map.IsPrivateInstance = false;
            }
        }

        [TestMethod]
        public void WhenThePointChangesHandsItsShieldsStandWholeOnTheNewSide()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);
            var live = PlacedDestructibles.LiveOf(world.Map, FieldRow);
            var rebuilt = new List<(ForceFields.Field, ControlPoints.Point)>();
            Action<ForceFields.Field, ControlPoints.Point> onRebuilt = (f, p) => rebuilt.Add((f, p));

            ForceFields.Damage(live.Field, live.Field.MaxHealth, 0);
            PlacedDestructibles.Worker(world.Map);
            Assert.IsTrue(live.IsDown);
            Assert.AreNotEqual(0L, live.RespawnAt);

            ControlPointFields.Rebuilt += onRebuilt;

            try
            {
                Assert.IsTrue(ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null));
            }
            finally
            {
                ControlPointFields.Rebuilt -= onRebuilt;
            }

            Assert.AreEqual(ForceFields.Side.A, live.Field.Side);
            Assert.AreEqual(live.Field.MaxHealth, live.Field.Health);
            Assert.AreEqual(UseObjectState.FfStateFactionAIntact, live.Field.Object.StateId);
            Assert.AreSame(live.Field.Object, live.Object, "the placement keeps the object that is on the map");
            Assert.AreSame(live, PlacedDestructibles.LiveOf(live.Field.Object));
            Assert.AreEqual(0L, live.RespawnAt);
            Assert.AreEqual(1, rebuilt.Count);
            Assert.AreSame(point, rebuilt[0].Item2);
            Assert.AreEqual(TargetCategory.Friendly, live.Field.Object.TargetCategory);
        }

        [TestMethod]
        public void ADestroyedShieldStandsAgainWholeTwoMinutesLater()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerAfs);
            var live = PlacedDestructibles.LiveOf(world.Map, FieldRow);
            var broken = new List<ulong>();
            var rebuilt = 0;
            Action<ForceFields.Field, ControlPoints.Point, ulong> onBroken = (f, p, by) => broken.Add(by);
            Action<ForceFields.Field, ControlPoints.Point> onRebuilt = (f, p) => rebuilt++;

            ControlPointFields.Broken += onBroken;
            ControlPointFields.Rebuilt += onRebuilt;

            try
            {
                ForceFields.Damage(live.Field, live.Field.MaxHealth - 1, 4242);
                Assert.IsEmpty(broken, "still standing");

                ForceFields.Damage(live.Field, 10, 4243);
                CollectionAssert.AreEqual(new[] { 4243ul }, broken);
                Assert.IsFalse(ControlPointFields.AnyUp(point));

                PlacedDestructibles.Worker(world.Map);
                Assert.AreEqual(_now + 120_000, live.RespawnAt, "two minutes, not the roll");
                Assert.AreEqual(120_000L, ControlPointFields.RebuildsInMs(live.Field));

                _now += 119_999;
                PlacedDestructibles.Worker(world.Map);
                Assert.IsTrue(live.IsDown);
                Assert.AreEqual(1L, ControlPointFields.RebuildsInMs(live.Field));

                _now += 1;
                PlacedDestructibles.Worker(world.Map);
                Assert.IsFalse(live.IsDown);
                Assert.AreEqual(live.Field.MaxHealth, live.Field.Health, "whole");
                Assert.AreEqual(UseObjectState.FfStateFactionAIntact, live.Field.Object.StateId);
                Assert.AreEqual(1, rebuilt);
                Assert.AreEqual(0L, ControlPointFields.RebuildsInMs(live.Field));
            }
            finally
            {
                ControlPointFields.Broken -= onBroken;
                ControlPointFields.Rebuilt -= onRebuilt;
            }

            // Another placed field keeps its 45-60 s.
            var other = PlacedDestructibles.LiveOf(world.Map, OtherRow);
            ForceFields.Damage(other.Field, other.Field.MaxHealth, 0);
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(_now + 50_000, other.RespawnAt);
        }

        [TestMethod]
        public void ADamagedShieldLeftAloneForAMinutePowersUpOverFiveMinutes()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerAfs);
            var field = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;

            ForceFields.Damage(field, 3000, 0);
            Assert.AreEqual(3000, field.Health);

            // Under fire: nothing back.
            Assert.AreEqual(0, ControlPointFields.Recharge(field, _clock + 59_999, 10_000));

            // A minute alone: a tenth of five minutes is a tenth of its health.
            Assert.AreEqual(600, ControlPointFields.Recharge(field, _clock + 60_000, 30_000));
            Assert.AreEqual(3600, field.Health);

            // Not past whole.
            Assert.AreEqual(2400, ControlPointFields.Recharge(field, _clock + 600_000, 300_000));
            Assert.AreEqual(field.MaxHealth, field.Health);

            // One that is down waits for its rebuild.
            ForceFields.Damage(field, field.MaxHealth, 0);
            Assert.AreEqual(0, ControlPointFields.Recharge(field, _clock + 900_000, 300_000));

            // The worker runs it by the time since its last pass.
            ForceFields.Repair(field, 0);
            ForceFields.Damage(field, 3000, 0);
            _clock += 60_000;
            ControlPointFields.Worker(world.Map);
            _clock += 30_000;
            ControlPointFields.Worker(world.Map);
            Assert.AreEqual(3600, field.Health);
        }

        [TestMethod]
        public void TheGateItClosesIsTheMiddleOfItsSheetAndStraightThrough()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerAfs);

            // The Bane minor entrance's sheet hangs to one side of its origin: -10.85 to 2.85.
            var minor = new ForceFields.Field
            {
                Class = ForceFields.ClassOf("baneminor"),
                Object = new DynamicObject { Position = new Vector3(100, 0, 100), Rotation = 0 }
            };

            var (middle, through) = ControlPointFields.Opening(minor);

            Assert.AreEqual(100 - 4.0f, middle.X, 0.01f);
            Assert.AreEqual(100f, middle.Z, 0.01f);
            Assert.AreEqual(1f, through.Z, 0.001f);
        }

        [TestMethod]
        public void AFightingCreatureTheShieldStopsStrikesAtItThroughAMissile()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerAfs);
            var field = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;
            var raider = Creature(world, TargetCategory.Hostile, field.Position + new Vector3(0, 0, -3));
            raider.Actions.Add(Attack(ActionId.WeaponAttack, 133, 40, 40));

            // Not fighting: it stands at the field.
            ControlPointFields.Blocked(world.Map, raider, field);
            Assert.IsEmpty(world.Map.QueuedMissiles);

            raider.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            ControlPointFields.Blocked(world.Map, raider, field);

            var missile = world.Map.QueuedMissiles.Single();
            Assert.AreEqual(field.Object.EntityId, missile.TargetEntityId);
            Assert.IsTrue(raider.Actions[0].CooldownTimer >= 1000, "its attack cools down as against an enemy");

            // On cooldown: no second strike.
            ControlPointFields.Blocked(world.Map, raider, field);
            Assert.AreEqual(1, world.Map.QueuedMissiles.Count);

            MissileManager.Instance.DoWork(world.Map, 10_000);
            Assert.AreEqual(field.MaxHealth - 40, field.Health, "the ordinary missile path took it");

            // A friendly creature does not strike its own side's field, nor at any field of no point.
            var friend = Creature(world, TargetCategory.Friendly, field.Position + new Vector3(0, 0, -3));
            friend.Actions.Add(Attack(ActionId.WeaponAttack, 133, 40, 40));
            friend.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            Assert.IsFalse(ControlPointFields.Strike(world.Map, friend, field));

            // With the rule off, nobody strikes.
            raider.Actions[0].CooldownTimer = 0;
            ControlPointFields.Config = new ControlPointDefenceConfig { CreaturesStrikeFields = false };
            ControlPointFields.Blocked(world.Map, raider, field);
            Assert.IsEmpty(world.Map.QueuedMissiles);
        }

        [TestMethod]
        public void InItsFightThinkACreatureHeldBackByTheShieldStrikesAtIt()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerAfs);
            var field = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;
            var raider = Creature(world, TargetCategory.Hostile, field.Position + new Vector3(0, 5, -2));
            var soldier = Creature(world, TargetCategory.Friendly, field.Position + new Vector3(0, 5, 8));
            raider.Actions.Add(Attack(ActionId.WeaponAttack, 133, 40, 40, range: 2));

            BehaviorManager.Instance.SetActionFighting(raider, soldier.EntityId);

            for (var think = 0; think < 8 && world.Map.QueuedMissiles.Count == 0; think++)
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);

            Assert.AreEqual(field.Object.EntityId, world.Map.QueuedMissiles.Single().TargetEntityId, "it struck the shield in its way");
            Assert.IsTrue(raider.Position.Z < field.Position.Z, "and is still on its side of it");
            world.Map.QueuedMissiles.Clear();
        }

        [TestMethod]
        public void TheRaidsMayWidenWhoStrikesAtAShield()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerAfs);
            var field = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;
            var raider = Creature(world, TargetCategory.Hostile, field.Position + new Vector3(0, 0, -3));
            raider.Actions.Add(Attack(ActionId.WeaponAttack, 133, 40, 40));

            ControlPointFields.StrikesFields = creature => creature == raider || ControlPointFields.DefaultStrikesFields(creature);
            ControlPointFields.Blocked(world.Map, raider, field);

            Assert.AreEqual(1, world.Map.QueuedMissiles.Count, "a raider walking to the gate strikes it");
            world.Map.QueuedMissiles.Clear();
        }

        [TestMethod]
        public void TheAttackAShieldIsStruckWithIsTheBestReadyOneThatHits()
        {
            var creature = new Creature();
            var weak = Attack(ActionId.WeaponAttack, 133, 5, 10);
            var strong = Attack(ActionId.WeaponAttack, 134, 30, 50);
            var heal = Attack(ShieldDrone.HealAction, 1, 100, 200);

            creature.Actions.AddRange(new[] { weak, strong, heal });
            Assert.AreSame(strong, ControlPointFields.AttackFor(creature));

            strong.CooldownTimer = 500;
            Assert.AreSame(weak, ControlPointFields.AttackFor(creature));

            weak.CooldownTimer = 500;
            Assert.IsNull(ControlPointFields.AttackFor(creature));
        }

        [TestMethod]
        public void SomeoneHittingAShieldCallsItsSideNearItToArms()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerAfs);
            var field = PlacedDestructibles.LiveOf(world.Map, FieldRow).Field;
            var raider = Creature(world, TargetCategory.Hostile, field.Position + new Vector3(0, 0, -3));
            var near = Creature(world, TargetCategory.Friendly, field.Position + new Vector3(10, 0, 10));
            var far = Creature(world, TargetCategory.Friendly, field.Position + new Vector3(60, 0, 70));
            near.Actions.Add(Attack(ActionId.WeaponAttack, 133, 5, 10));
            far.Actions.Add(Attack(ActionId.WeaponAttack, 133, 5, 10));

            Assert.AreEqual(1, ControlPointFields.Hit(field, raider));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, near.Controller.CurrentAction);
            Assert.AreEqual(raider.EntityId, near.Controller.ActionFighting.TargetEntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, far.Controller.CurrentAction, "out of 80 m");

            // Not again within the second and a half.
            Assert.AreEqual(0, ControlPointFields.Hit(field, raider));
        }

        #region Fixtures

        private static CreatureAction Attack(ActionId actionId, uint argId, uint min, uint max, double range = 24, long cooldown = 1000) => new()
        {
            ActionId = actionId, ActionArgId = argId, RangeMin = 1, RangeMax = range, MinDamage = min, MaxDamage = max, Cooldown = cooldown
        };

        private void AddClass(EntityClasses id, string name)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, 1, new List<AugmentationType> { AugmentationType.ForceField }, true);
        }

        /// <summary>The point loaded and set down on the world's map with one shield (an AFS row) and one placed field of no point.</summary>
        private static ControlPoints.Point Placed(WorldTestContext world, byte owner)
        {
            var links = new List<ControlPointLinkEntry> { new() { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindField, ObjectId = FieldRow } };

            ControlPoints.Instance.Load(new[] { Entry(owner) }, links, null);
            ControlPoints.Instance.Place(world.Map);

            PlacedDestructibles.Load(new[]
            {
                Row(FieldRow, HumanGate, 10, 20, WorldDestructibleEntry.SideAfs, 6000),
                Row(OtherRow, BaneGate, 200, 200, WorldDestructibleEntry.SideAfs, 900)
            });
            PlacedDestructibles.Place(world.Map);

            return ControlPoints.Instance.ById(PointId);
        }

        private static ControlPointEntry Entry(byte defaultOwner) => new()
        {
            Id = PointId, MapContextId = MapId, Name = "Retread Outpost", ClassId = 3814, PosX = 10, PosY = 0, PosZ = 40, Rotation = 0, DefaultOwner = defaultOwner
        };

        private static WorldDestructibleEntry Row(uint id, EntityClasses classId, float x, float z, uint side, uint hitPoints) => new()
        {
            Id = id, MapContextId = MapId, ClassId = (uint)classId, PosX = x, PosY = 0, PosZ = z, Rotation = 0,
            HitPoints = hitPoints, Side = side, Comment = "fixture"
        };

        private Creature Creature(WorldTestContext world, TargetCategory category, Vector3 at)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = at,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 10,
                RunSpeed = 6,
                WalkSpeed = 3,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = at;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            var seed = CellManager.Instance.GetCellSeed(at);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _registered.Add(creature);

            return creature;
        }

        #endregion
    }
}
