using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    /// <summary>
    /// The human gate force fields in their archways' slots (Gate_field_nooks, GateFieldNooks). The
    /// report: the gate fields were not centred where the gate has a nook for them.
    /// </summary>
    [TestClass, DoNotParallelize]
    public class GateFieldNooksTests
    {
        private const string Migration = "20270925000000_Gate_field_nooks";
        private const string Previous = "20270924000000_Landing_zone_gate_field";

        // The posts' slots in arch_hum_outpost_fence_entrance_v03/_v04.geo, gate-local metres:
        // their floors at x 1.5 and 14.5 (the posts' inner faces at 2 and 14), their sides at z -1.613 and -1.300.
        private const float SlotFloorLeft = 1.5f, SlotFloorRight = 14.5f, PostLeft = 2f, PostRight = 14f;
        private const float SlotBack = -1.613f, SlotFront = -1.300f;

        [TestMethod]
        public void EachFieldStandsInItsGatesSlotsFacingTheGatesWay()
        {
            var field = ForceFields.ClassOf(WildernessPostNpcs.HumanGateField.ToString());
            Assert.HasCount(4, GateFieldNooks.Gates);
            CollectionAssert.AreEquivalent(new uint[] { 640016, 640101, 640102, 640103 }, GateFieldNooks.Gates.Select(g => g.Row).ToArray());

            foreach (var gate in GateFieldNooks.Gates)
            {
                var local = Local(gate, gate.To);
                Assert.AreEqual(GateFieldNooks.NookX, local.X, 0.001, gate.Name);
                Assert.AreEqual(0, local.Y, 0.001, gate.Name);
                Assert.AreEqual(GateFieldNooks.NookZ, local.Z, 0.001, gate.Name);
                Assert.IsTrue(local.Z > SlotBack && local.Z < SlotFront, $"{gate.Name}: between the slot's sides");
                Assert.AreEqual(0, Turn(gate.To.Rotation - gate.Arch.Yaw), 0.0002, $"{gate.Name}: the gate's way");

                // The sheet's ends: past the posts' faces, short of the slots' floors.
                Assert.IsTrue(local.X + field.Min.X > SlotFloorLeft && local.X + field.Min.X < PostLeft, gate.Name);
                Assert.IsTrue(local.X + field.Max.X < SlotFloorRight && local.X + field.Max.X > PostRight, gate.Name);

                // Where the seed had it: in the gateway's middle on the fence line, half a metre into each post.
                var seed = Local(gate, gate.From);
                Assert.AreEqual(GateFieldNooks.NookX, seed.X, 0.03, gate.Name);
                Assert.AreEqual(0, seed.Z, 0.03, gate.Name);
            }
        }

        [TestMethod]
        public void UpMovesTheFourFieldsIntoTheirSlotsAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rows(database);
            foreach (var gate in GateFieldNooks.Gates)
                AssertAt(before, gate.Row, gate.From, "seeded");

            migrator.Migrate(Migration);
            var after = Rows(database);
            foreach (var gate in GateFieldNooks.Gates)
                AssertAt(after, gate.Row, gate.To, "moved");
            CollectionAssert.AreEquivalent(before.Keys.ToArray(), after.Keys.ToArray());
            foreach (var row in before.Where(r => GateFieldNooks.Gates.All(g => g.Row != r.Key)))
                Assert.AreEqual(row.Value, after[row.Key], $"{row.Key} untouched");

            foreach (var sql in GateFieldNooks.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEquivalent(after.ToArray(), Rows(database).ToArray(), "Up twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before.ToArray(), Rows(database).ToArray(), "Down puts them back");
        }

        [TestMethod]
        public void AFieldMovedByHandStaysWhereItWasBothWays()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE world_destructible SET pos_x = pos_x + 1 WHERE id = 640101");
            database.Context.Database.ExecuteSqlRaw("UPDATE world_destructible SET rotation = 0 WHERE id = 640103");
            var before = Rows(database);

            migrator.Migrate(Migration);
            var after = Rows(database);
            Assert.AreEqual(before[640101], after[640101]);
            Assert.AreEqual(before[640103], after[640103]);
            AssertAt(after, 640016, GateFieldNooks.Gates[0].To, "the others still move");

            database.Context.Database.ExecuteSqlRaw("UPDATE world_destructible SET pos_y = 164 WHERE id = 640016");
            migrator.Migrate(Previous);
            Assert.AreEqual(164.0, Rows(database)[640016].Y, "moved again after Up: Down leaves it");
            AssertAt(Rows(database), 640102, GateFieldNooks.Gates[2].From, "the others go back");
        }

        [TestMethod]
        public void OnMySqlTheFieldsMoveAndMoveBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in GateFieldNooks.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in GateFieldNooks.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>Still across the gateway: the Landing Zone's field stops a crossing through the gate, wherever in it.</summary>
        [TestMethod]
        public void InItsSlotTheLandingZoneFieldStillClosesTheWholeGateway()
        {
            var gate = GateFieldNooks.Gates[0];
            var field = ForceFields.ClassOf(WildernessPostNpcs.HumanGateField.ToString());
            var at = new Vector3((float)gate.To.X, (float)gate.To.Y, (float)gate.To.Z);

            for (var x = 192.5f; x <= 203.5f; x += 1f)
                Assert.IsTrue(ForceFields.Crosses(field, at, (float)gate.To.Rotation, new Vector3(x, 165, -28), new Vector3(x, 165, -42)), $"x {x}");
        }

        private static Vector3 Local(GateFieldNooks.Gate gate, (double X, double Y, double Z, double Rotation) at)
        {
            var turn = Quaternion.CreateFromYawPitchRoll((float)gate.Arch.Yaw, 0, 0);
            var offset = new Vector3((float)(at.X - gate.Arch.X), (float)(at.Y - gate.Arch.Y), (float)(at.Z - gate.Arch.Z));
            return Vector3.Transform(offset, Quaternion.Inverse(turn));
        }

        private static double Turn(double angle) => Math.IEEERemainder(angle, 2 * Math.PI);

        private static void AssertAt(System.Collections.Generic.Dictionary<uint, (double X, double Y, double Z, double R)> rows, uint id,
            (double X, double Y, double Z, double Rotation) at, string what)
        {
            var row = rows[id];
            Assert.AreEqual(at.X, row.X, 0.0005, $"{id} {what}");
            Assert.AreEqual(at.Y, row.Y, 0.0005, $"{id} {what}");
            Assert.AreEqual(at.Z, row.Z, 0.0005, $"{id} {what}");
            Assert.AreEqual(at.Rotation, row.R, 0.00005, $"{id} {what}");
        }

        private static System.Collections.Generic.Dictionary<uint, (double X, double Y, double Z, double R)> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.WorldDestructibleEntries.AsNoTracking().ToList()
                .ToDictionary(d => d.Id, d => ((double)d.PosX, (double)d.PosY, (double)d.PosZ, (double)d.Rotation));
    }
}
