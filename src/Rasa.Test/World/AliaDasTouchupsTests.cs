using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    /// <summary>Alia Das put right after a look round in game (Alia_das_touchups, AliaDasTouchups).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AliaDasTouchupsTests
    {
        private const string Migration = "20270104000000_Alia_das_touchups";
        private const string Previous = "20270103000000_Turret_facings";

        [TestMethod]
        public void EveryoneLooksAndStandsAsTheGmAsked()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var look in AliaDasTouchups.Looks)
                CollectionAssert.AreEqual(look.Is.OrderBy(s => s.Id).Select(s => $"{s.Id} {s.ClassId} {s.Color}").ToList(),
                    Strings(database, $"SELECT slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {look.Creature} ORDER BY slot_id"), look.Who);

            foreach (var move in AliaDasTouchups.Moves)
                Assert.AreEqual($"{move.Creature} {F(move.ToX)} {F(move.ToY)} {F(move.ToZ)} {F(move.ToRotation)}", PoolText(database, move.Pool), move.Who);

            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {AliaDasTouchups.BradshawPool}"), "Bradshaw taken away");
            Assert.AreEqual($"{AliaDasTouchups.LightTurret} 660.930 288.910 437.110 0.000", PoolText(database, AliaDasTouchups.MeadowTurretPool), "the meadow tripod's turret");

            // Caruso: the armour sign over her head, Bradshaw's armour and her own stock.
            Assert.AreEqual((long)AliaDasTouchups.ArmourPackage, Count(database, $"SELECT package_id AS Value FROM vendor WHERE id = {AliaDasNpcs.Caruso}"));
            var stock = database.Context.Database.SqlQueryRaw<long>($"SELECT item_template_id AS Value FROM vendor_item WHERE id = {AliaDasNpcs.Caruso}").AsEnumerable().ToList();
            Assert.IsTrue(AliaDasTouchups.ArmourStock.All(t => stock.Contains(t)), "Bradshaw's armour");
            Assert.IsTrue(new long[] { 28, 30, 32, 56, 636, 638, 1766 }.All(stock.Contains), "her own stock kept");
            CollectionAssert.AreEquivalent(stock.Where(t => AliaDasTouchups.ArmourStock.Contains((uint)t)).ToList(),
                database.Context.Database.SqlQueryRaw<long>($"SELECT item_template_id AS Value FROM vendor_item WHERE id = {AliaDasTouchups.Bradshaw}").AsEnumerable().ToList(),
                "exactly Bradshaw's");

            var beat = Strings(database, $"SELECT printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM spawnpool_patrol WHERE pool_id = {AliaDasNpcs.CirclingGunnerPool} ORDER BY step");
            CollectionAssert.AreEqual(AliaDasTouchups.CircleBeat.Select(s => $"{F(s.X)} {F(s.Y)} {F(s.Z)}").ToList(), beat);

            foreach (var locker in AliaDasTouchups.Lockers)
                Assert.AreEqual($"{F(locker.ToX)} {F(locker.ToY)} {F(locker.ToZ)} {F(locker.ToRotation)}",
                    Strings(database, $"SELECT printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM footlocker WHERE id = {locker.Id}").Single(), locker.Why);

            foreach (var pad in AliaDasTouchups.Pads)
                Assert.AreEqual($"{AliaDasTouchups.PadClass} 1 {pad.MapContextId} {F(pad.X)} {F(pad.Y)} {F(pad.Z)}",
                    Strings(database, $"SELECT class_id || ' ' || type || ' ' || map_context_id || ' ' || printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM teleporter WHERE id = {pad.Id}").Single(), pad.Description);
        }

        [TestMethod]
        public void EachPadStandsOnItsEndOfTheCellarLink()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            var link = Strings(database, $"SELECT printf('%.3f %.3f', pos_x, pos_z) AS Value FROM map_link WHERE id = {AliaDasCellarLink.ArenaLinkId}").Single();
            var back = Strings(database, $"SELECT printf('%.3f %.3f', pos_x, pos_z) AS Value FROM map_link WHERE id = {AliaDasCellarLink.ReturnLinkId}").Single();

            Assert.AreEqual(link, $"{F(AliaDasTouchups.Pads[0].X)} {F(AliaDasTouchups.Pads[0].Z)}", "Alia Das");
            Assert.AreEqual(back, $"{F(AliaDasTouchups.Pads[1].X)} {F(AliaDasTouchups.Pads[1].Z)}", "the arena");
            Assert.AreEqual(1220u, AliaDasTouchups.Pads[0].MapContextId);
            Assert.AreEqual(20000009u, AliaDasTouchups.Pads[1].MapContextId);
        }

        [TestMethod]
        public void TheCircleIsOnTheGroundHalfwayBetweenTheSandbagsAndTheTents()
        {
            Assert.HasCount(AliaDasNpcs.CircleBeat.Length, AliaDasTouchups.CircleBeat);

            foreach (var step in AliaDasTouchups.CircleBeat)
            {
                var r = Math.Sqrt((step.X - AliaDasNpcs.FlagX) * (step.X - AliaDasNpcs.FlagX) + (step.Z - AliaDasNpcs.FlagZ) * (step.Z - AliaDasNpcs.FlagZ));
                Assert.AreEqual(AliaDasTouchups.CircleRadius, r, 0.001);
                Assert.IsTrue(step.Y < 294.5, $"{step.X}, {step.Z} on the ground, not the sandbags (their top is about 295.5)");
            }

            // The sandbags reach about 5.7 m from the flag, the hospital tent 17 m and the armoury 19 m.
            Assert.IsTrue(AliaDasTouchups.CircleRadius > 5.7 + 4 && AliaDasTouchups.CircleRadius < 17 - 4);

            // The pool stands on the first step, as the old one did.
            var start = AliaDasTouchups.Moves.Single(m => m.Pool == AliaDasNpcs.CirclingGunnerPool);
            Assert.AreEqual((start.ToX, start.ToY, start.ToZ), (AliaDasTouchups.CircleBeat[0].X, AliaDasTouchups.CircleBeat[0].Y, AliaDasTouchups.CircleBeat[0].Z));
        }

        [TestMethod]
        public void TheFootlockersAreClearOfTheCotsAndDressers()
        {
            // prop_hum_footlocker.geo: x -0.41 to 0.41, z -0.05 to 0.71, local z toward (-sin r, -cos r).
            // The cots and dressers from the map (ArchHumGenObjCotV01 1.22 by 0.53, dressers 0.61 by 0.52).
            var furniture = new (string What, double MinX, double MaxX, double MinZ, double MaxZ)[]
            {
                ("barracks cot at 758.09", 756.87, 759.31, 379.17, 380.23),
                ("barracks cot at 761.86", 760.64, 763.08, 379.17, 380.23),
                ("barracks cot at 755.25", 754.72, 755.78, 383.78, 386.22),
                ("barracks dresser at 756.4", 755.79, 757.01, 385.30, 386.27),
                ("cot tent dresser at 866.26", 865.65, 866.87, 398.52, 399.56),
                ("cot tent cot at 869.0", 867.78, 870.22, 398.47, 399.53),
                ("cot tent dresser at 871.55", 870.94, 872.16, 398.50, 399.54),
                ("cot tent cot at 874.71", 873.49, 875.93, 398.48, 399.54),
            };

            foreach (var locker in AliaDasTouchups.Lockers)
            {
                var r = locker.ToRotation;
                var corners = new[] { (-0.41, -0.05), (-0.41, 0.71), (0.41, -0.05), (0.41, 0.71) }
                    .Select(c => (X: locker.ToX - c.Item1 * Math.Cos(r) - c.Item2 * Math.Sin(r), Z: locker.ToZ + c.Item1 * Math.Sin(r) - c.Item2 * Math.Cos(r)))
                    .ToList();
                var (minX, maxX, minZ, maxZ) = (corners.Min(c => c.X), corners.Max(c => c.X), corners.Min(c => c.Z), corners.Max(c => c.Z));

                foreach (var f in furniture)
                    Assert.IsFalse(minX < f.MaxX && maxX > f.MinX && minZ < f.MaxZ && maxZ > f.MinZ, $"{locker.Why}: in the {f.What}");
            }

            // The barracks one stands as the seed's footlocker 1 does: 379.3, facing 0.
            var barracks = AliaDasTouchups.Lockers.Single(l => l.Id == 640001);
            Assert.AreEqual((379.3, 0.0), (barracks.ToZ, barracks.ToRotation));

            // Parallel to its cot (yaw 1.52), a half turn round.
            Assert.AreEqual(1.52 + Math.PI, AliaDasTouchups.Lockers.Single(l => l.Id == 640004).ToRotation, 0.001);
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedAndTheDownPutsBackWhatItChanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM recoloured Maddrey's tunic and moved a footlocker by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature_appearance SET color = 123 WHERE id = {AliaDasNpcs.Maddrey} AND slot_id = 15");
            database.Context.Database.ExecuteSqlRaw("UPDATE footlocker SET pos_x = 1 WHERE id = 640017");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(1, Strings(database, $"SELECT slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Maddrey} AND slot_id = 15 AND color = 123").Count, "the GM's tunic kept");
            Assert.AreEqual(1L, Count(database, "SELECT CAST(pos_x AS INTEGER) AS Value FROM footlocker WHERE id = 640017"), "the GM's footlocker kept");
            Assert.AreEqual((long)AliaDasTouchups.Hair01, Count(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Maddrey} AND slot_id = 14"));

            var once = Snapshot(database);
            foreach (var sql in AliaDasTouchups.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in AliaDasTouchups.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in AliaDasTouchups.Down())
                StringAssert.Contains(down, sql);
        }

        private static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

        private static string PoolText(OrdinaryLootTestDatabase database, uint pool) =>
            Strings(database, $"SELECT creature_1_Id || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM spawnpool WHERE id = {pool}").Single();

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        /// <summary>Every row the migration touches, as text, positions to the millimetre.</summary>
        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var creatures = string.Join(", ", AliaDasTouchups.Looks.Select(l => l.Creature).Append(AliaDasNpcs.Caruso).Distinct());
            var pools = string.Join(", ", AliaDasTouchups.Moves.Select(m => m.Pool).Append(AliaDasTouchups.BradshawPool).Append(AliaDasTouchups.MeadowTurretPool));

            string[] queries =
            {
                $"SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id IN ({creatures})",
                $"SELECT 'pool ' || id || ' ' || creature_1_Id || ' ' || respown_time || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM spawnpool WHERE id IN ({pools})",
                $"SELECT 'pose ' || id || ' ' || pose AS Value FROM spawnpool_pose WHERE id IN ({pools})",
                $"SELECT 'step ' || pool_id || ' ' || step || ' ' || printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM spawnpool_patrol WHERE pool_id = {AliaDasNpcs.CirclingGunnerPool}",
                $"SELECT 'vendor ' || id || ' ' || package_id AS Value FROM vendor WHERE id IN ({creatures})",
                $"SELECT 'stock ' || id || ' ' || item_template_id AS Value FROM vendor_item WHERE id IN ({creatures})",
                "SELECT 'footlocker ' || id || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM footlocker",
                "SELECT 'teleporter ' || id || ' ' || class_id || ' ' || type || ' ' || map_context_id || ' ' || printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM teleporter",
            };

            return queries.SelectMany(sql => database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable()).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }
    }
}
