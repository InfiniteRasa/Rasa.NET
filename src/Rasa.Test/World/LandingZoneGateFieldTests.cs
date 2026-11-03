using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    /// <summary>
    /// The Landing Zone gate's force field is the point's (Landing_zone_gate_field,
    /// LandingZoneGateField, ControlPoints.ApplyFields). The report: the Bane took the Landing
    /// Zone and the field stayed blue.
    /// </summary>
    [TestClass, DoNotParallelize]
    public class LandingZoneGateFieldTests
    {
        private const string Migration = "20270924000000_Landing_zone_gate_field";
        private const string Previous = "20270923000000_Daghdas_urn_building_regions";

        // Either side of the gate, at a walker's height: outside to the north, the courtyard to the south.
        private static readonly Vector3 Outside = new(198, 165, -28), Inside = new(198, 165, -42);

        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _old;

        private ControlPoints.Point Point => ControlPoints.Instance.ById(LandingZoneGateField.Point);
        private PlacedDestructibles.Live Gate => PlacedDestructibles.LiveOf(_world.Map, LandingZoneGateField.Field);

        [TestCleanup]
        public void End()
        {
            if (_world != null)
            {
                if (Point != null)
                    ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
                ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
                PlacedDestructibles.Forget(_world.Map);
                PlacedDestructibles.Load(null);
                PlacedDestructibles.Reset();
                _world.Dispose();
                _world = null;
            }

            if (_old != null)
                ControlPoints.Instance.Config = _old;
        }

        [TestMethod]
        public void UpLinksTheGateFieldToTheLandingZoneOnceAndDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Links(database);
            Assert.IsFalse(before.Any(l => l.Kind == ControlPointLinkEntry.KindForceField), "no point had a field");
            var row = database.Context.WorldDestructibleEntries.AsNoTracking().Single(d => d.Id == LandingZoneGateField.Field);
            Assert.AreEqual((WildernessPostNpcs.HumanGateField, LandingZoneGateField.Map, WorldDestructibleEntry.SideAfs), (row.ClassId, row.MapContextId, row.Side));

            migrator.Migrate(Migration);
            var after = Links(database);
            CollectionAssert.AreEquivalent(before.Append((LandingZoneGateField.Point, ControlPointLinkEntry.KindForceField, LandingZoneGateField.Field)).ToList(), after);

            foreach (var sql in LandingZoneGateField.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEquivalent(after, Links(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before, Links(database), "Down puts the links back as they were");
        }

        [TestMethod]
        public void NoLinkGoesInForAFieldRowThatIsNotTheGatesOrThatAPointHasAlready()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE world_destructible SET class_id = 9572 WHERE id = {LandingZoneGateField.Field}");
            migrator.Migrate(Migration);
            Assert.IsFalse(Links(database).Any(l => l.Kind == ControlPointLinkEntry.KindForceField), "another class: not the gate's field");

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE world_destructible SET class_id = {WildernessPostNpcs.HumanGateField} WHERE id = {LandingZoneGateField.Field}");
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO control_point_link (control_point_id, kind, object_id) VALUES (3, {ControlPointLinkEntry.KindForceField}, {LandingZoneGateField.Field})");
            migrator.Migrate(Migration);
            CollectionAssert.AreEquivalent(new[] { (3u, ControlPointLinkEntry.KindForceField, LandingZoneGateField.Field) },
                Links(database).Where(l => l.Kind == ControlPointLinkEntry.KindForceField).ToList(), "a GM's link stands, and is the only one");

            migrator.Migrate(Previous);
            CollectionAssert.Contains(Links(database), (3u, ControlPointLinkEntry.KindForceField, LandingZoneGateField.Field), "Down leaves the GM's link");
        }

        [TestMethod]
        public void OnMySqlTheLinkGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in LandingZoneGateField.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in LandingZoneGateField.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>The report: the Bane take the Landing Zone, and its gate field is theirs - red, and shut to players.</summary>
        [TestMethod]
        public void TheGateFieldIsTheHoldersBlueForTheAfsAndRedForTheBane()
        {
            Start();
            Assert.IsTrue(Point.Fields.Contains(LandingZoneGateField.Field));
            Assert.IsTrue(Point.HeldByAfs);
            AssertAfs();

            Assert.IsTrue(ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null));
            AssertBane();
            Assert.IsTrue(ForceFields.MayShoot(Gate.Field, _world.Client.Player), "players can shoot their way in");

            Assert.IsTrue(ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null));
            AssertAfs();
        }

        /// <summary>
        /// The Bane held the point over a restart: the points are set down before the fields
        /// (Server: ControlPoints, then PlacedDestructibles), and the field is built from its
        /// row's side - the map worker puts it on the Bane's.
        /// </summary>
        [TestMethod]
        public void AfterARestartWithTheBaneHoldingThePointTheWorkerTurnsTheFieldRed()
        {
            Start(new ControlPointStateEntry { ControlPointId = LandingZoneGateField.Point, Owner = ControlPoints.Bane });
            Assert.IsFalse(Point.HeldByAfs);
            Assert.AreEqual(ForceFields.Side.A, Gate.Field.Side, "built as its row has it");

            ControlPoints.Instance.Worker(_world.Map);
            AssertBane();
        }

        /// <summary>The Bane broke the gate to take the point: it stays down, and is back as theirs.</summary>
        [TestMethod]
        public void AFieldThatIsDownWhenThePointChangesHandsStaysDownAndComesBackTheNewHolders()
        {
            Start();
            Assert.IsTrue(PlacedDestructibles.Destroy(Gate, null));
            Assert.IsTrue(Gate.IsDown);

            Assert.IsTrue(ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null));
            Assert.AreEqual(ForceFields.Side.B, Gate.Field.Side);
            Assert.IsTrue(Gate.IsDown, "still down");
            Assert.IsFalse(ForceFields.StopsPlayer(_world.Map, Outside, Inside));
            Assert.AreEqual(ForceFields.StateOf(Gate.Field), Gate.Object.StateId);

            PlacedDestructibles.Respawn(Gate);
            Assert.IsFalse(Gate.IsDown);
            AssertBane();
        }

        /// <summary>The field is set to a side only when it is not on it: the worker's passes leave it be.</summary>
        [TestMethod]
        public void TheWorkerLeavesAFieldOnTheHoldersSideAlone()
        {
            Start();
            var obj = Gate.Object;

            ControlPoints.Instance.Worker(_world.Map);
            ControlPoints.Instance.Worker(_world.Map);
            Assert.AreSame(obj, Gate.Object);
            Assert.IsTrue(obj.IsInWorld);

            Assert.IsTrue(ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null));
            Assert.AreNotSame(obj, Gate.Object, "put back as the Bane's");
            Assert.AreSame(Gate.Field.Object, Gate.Object);
            obj = Gate.Object;
            ControlPoints.Instance.Worker(_world.Map);
            Assert.AreSame(obj, Gate.Object);
        }

        /// <summary>A point with no field linked leaves the field as its row has it.</summary>
        [TestMethod]
        public void APointWithoutTheLinkLeavesTheFieldAsItsRowHasIt()
        {
            Start(links: false);
            Assert.AreEqual(0, Point.Fields.Count);

            Assert.IsTrue(ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null));
            ControlPoints.Instance.Worker(_world.Map);
            Assert.AreEqual(ForceFields.Side.A, Gate.Field.Side);
        }

        private void Start(ControlPointStateEntry state = null, bool links = true)
        {
            _old = ControlPoints.Instance.Config;
            ControlPoints.Instance.Config = new ControlPointConfig { LandingZoneAssaultEnabled = false };
            _world = WildernessRuntimeTestHarness.Create();
            _world.Map.SpawnPools.Clear();

            using var db = _world.CreateWorld();
            var pointLinks = db.ControlPoints.GetLinks().Where(l => l.ControlPointId == LandingZoneGateField.Point && (links || l.Kind != ControlPointLinkEntry.KindForceField));
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.Id == LandingZoneGateField.Point), pointLinks,
                state == null ? null : new MemoryStore(state));
            PlacedDestructibles.Load(db.WorldDestructibles.Get().Where(d => d.Id == LandingZoneGateField.Field));

            // As the server has it: the points first, then what is placed.
            ControlPoints.Instance.Place(_world.Map);
            PlacedDestructibles.Place(_world.Map);
            Assert.IsNotNull(Gate?.Field);
        }

        private void AssertAfs()
        {
            Assert.AreEqual(ForceFields.Side.A, Gate.Field.Side);
            Assert.AreEqual(UseObjectState.FfStateFactionAIntact, Gate.Object.StateId);
            Assert.AreEqual(TargetCategory.Friendly, Gate.Object.TargetCategory);
            Assert.IsFalse(ForceFields.StopsPlayer(_world.Map, Outside, Inside), "players walk through the AFS's");
            Assert.IsTrue(ForceFields.StopsCategory(Gate.Field.Side, TargetCategory.Hostile), "and the Bane do not");
        }

        private void AssertBane()
        {
            Assert.AreEqual(ForceFields.Side.B, Gate.Field.Side);
            Assert.AreEqual(UseObjectState.FfStateFactionBIntact, Gate.Object.StateId);
            Assert.AreEqual(TargetCategory.Hostile, Gate.Object.TargetCategory);
            Assert.AreSame(Gate.Field.Object, Gate.Object);
            Assert.IsTrue(Gate.Object.IsInWorld);
            Assert.IsTrue(ForceFields.StopsPlayer(_world.Map, Outside, Inside), "players are shut out");
            Assert.IsFalse(ForceFields.StopsCategory(Gate.Field.Side, TargetCategory.Hostile), "the Bane's own squads pass");
        }

        private static List<(uint ControlPointId, byte Kind, uint ObjectId)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.ControlPointLinkEntries.AsNoTracking().ToList()
                .Select(link => (link.ControlPointId, link.Kind, link.ObjectId)).ToList();

        private sealed class MemoryStore : ControlPoints.IStore
        {
            private readonly ControlPointStateEntry _state;

            public MemoryStore(ControlPointStateEntry state) => _state = state;

            public List<ControlPointStateEntry> Load() => new() { _state };

            public void Save(ControlPointStateEntry state) { }
        }
    }
}
