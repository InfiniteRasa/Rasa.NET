using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    /// <summary>
    /// Bane_supply_depot_door_arrival: map link 41 (Valverde Marshes to the Bane Supply Depot) arrives on the depot's floor,
    /// not on link 39, the depot's way out, where the player fell and was sent back to the Marshes.
    /// </summary>
    [TestClass, DoNotParallelize]
    public class BaneSupplyDepotDoorArrivalTests
    {
        private const string Migration = "20271106000000_Bane_supply_depot_door_arrival";
        private const uint Depot = BaneSupplyDepotDoorArrival.DestMapContextId;
        private const string DepotNavMesh = "adv_foreas_valverde_marshes_banesupplydepot";

        /// <summary>Margin outside every link circle of the depot that the arrival keeps.</summary>
        private const float LinkMargin = 2f;

        private static string Previous(DbContext context)
        {
            var migrations = context.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            var index = migrations.IndexOf(Migration);
            Assert.IsGreaterThan(0, index, "the migration is in the assembly, after another");
            return migrations[index - 1];
        }

        private static Dictionary<uint, string> Snapshot(OrdinaryLootTestDatabase db) =>
            db.Context.MapLinkEntries.AsNoTracking().OrderBy(l => l.Id).ToList().ToDictionary(l => l.Id, l => FormattableString.Invariant(
                $"{l.MapContextId} {l.PosX} {l.PosY} {l.PosZ} {l.Radius} {l.DestMapContextId} {l.DestPosX} {l.DestPosY} {l.DestPosZ} {l.DestRotation} {l.Kind} {l.Enabled} {l.Comment}"));

        private static void Run(OrdinaryLootTestDatabase db, IEnumerable<string> statements)
        {
            foreach (var sql in statements)
                db.Context.Database.ExecuteSqlRaw(sql);
        }

        [TestMethod]
        public void TheMigrationMovesOnlyLinkFortyOnesArrivalAndRollsBack()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            var previous = Previous(db.Context);

            migrator.Migrate(previous);
            var before = Snapshot(db);
            var old = db.Context.MapLinkEntries.AsNoTracking().Single(l => l.Id == BaneSupplyDepotDoorArrival.LinkId);
            Assert.AreEqual((BaneSupplyDepotDoorArrival.OldX, BaneSupplyDepotDoorArrival.OldY, BaneSupplyDepotDoorArrival.Z),
                (old.DestPosX, old.DestPosY, old.DestPosZ), "the seeded arrival (MapLinkPreloader)");

            migrator.Migrate(Migration);
            var after = Snapshot(db);
            CollectionAssert.AreEqual(new[] { BaneSupplyDepotDoorArrival.LinkId },
                before.Keys.Where(id => before[id] != after[id]).ToArray(), "only link 41 changes");
            var moved = db.Context.MapLinkEntries.AsNoTracking().Single(l => l.Id == BaneSupplyDepotDoorArrival.LinkId);
            Assert.AreEqual((-356.0, 149.61, 3.1254), (moved.DestPosX, moved.DestPosY, moved.DestPosZ));
            Assert.AreEqual(old.DestRotation, moved.DestRotation, "rotation kept");
            Assert.AreEqual((old.PosX, old.PosY, old.PosZ, old.Radius, old.DestMapContextId),
                (moved.PosX, moved.PosY, moved.PosZ, moved.Radius, moved.DestMapContextId), "the trigger in the Marshes kept");

            Run(db, BaneSupplyDepotDoorArrival.Up());
            CollectionAssert.AreEqual(after.Values.ToArray(), Snapshot(db).Values.ToArray(), "Up a second time changes nothing");

            Run(db, BaneSupplyDepotDoorArrival.Down());
            Run(db, BaneSupplyDepotDoorArrival.Down());
            CollectionAssert.AreEqual(before.Values.ToArray(), Snapshot(db).Values.ToArray(), "Down twice: the old arrival");

            Run(db, BaneSupplyDepotDoorArrival.Up());
            CollectionAssert.AreEqual(after.Values.ToArray(), Snapshot(db).Values.ToArray(), "Up again");

            migrator.Migrate(previous);
            CollectionAssert.AreEqual(before.Values.ToArray(), Snapshot(db).Values.ToArray(), "rolled back to " + previous);

            // A GM's own arrival for link 41 is left alone, by Up and by Down.
            db.Context.Database.ExecuteSqlRaw($"UPDATE map_link SET dest_pos_x = -360.25 WHERE id = {BaneSupplyDepotDoorArrival.LinkId};");
            var edited = Snapshot(db);
            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(edited.Values.ToArray(), Snapshot(db).Values.ToArray(), "Up leaves a GM edit");
            migrator.Migrate(previous);
            CollectionAssert.AreEqual(edited.Values.ToArray(), Snapshot(db).Values.ToArray(), "Down leaves a GM edit");
        }

        /// <summary>
        /// The new arrival stands on the depot's navmesh (the upstream file), at least LinkMargin outside every link circle
        /// of the depot, and walks to the depot's way out; the old one was inside link 39's circle.
        /// </summary>
        [TestMethod]
        public void TheArrivalIsOnTheDepotNavMeshOutsideEveryLinkAndReachesTheWayOut()
        {
            using var db = new OrdinaryLootTestDatabase();
            db.Context.GetService<IMigrator>().Migrate(Migration);
            var arrival = MapLink.FromEntry(db.Context.MapLinkEntries.AsNoTracking().Single(l => l.Id == BaneSupplyDepotDoorArrival.LinkId));
            Assert.AreEqual(Depot, arrival.DestMapContextId);
            var links = db.Context.MapLinkEntries.AsNoTracking().Where(l => l.MapContextId == Depot).ToList().Select(MapLink.FromEntry).ToList();
            Assert.IsTrue(links.Any(l => l.Id == 39), "link 39, the depot's way out, is on the depot");

            static float Across(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
            var landing = arrival.DestPosition;
            foreach (var link in links)
            {
                Assert.IsFalse(Rasa.Managers.MapLinkManager.Contains(link, landing), $"the arrival is not in link {link.Id}");
                Assert.IsGreaterThanOrEqualTo(link.Radius + LinkMargin, Across(link.Position, landing),
                    $"the arrival is at least {LinkMargin} m outside link {link.Id}'s circle");
            }
            var exit = links.Single(l => l.Id == 39);
            var old = new Vector3((float)BaneSupplyDepotDoorArrival.OldX, (float)BaneSupplyDepotDoorArrival.OldY, (float)BaneSupplyDepotDoorArrival.Z);
            Assert.IsTrue(Rasa.Managers.MapLinkManager.Contains(exit, old), "the old arrival was in link 39");

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Rasa.NET.sln")))
                dir = dir.Parent;
            Assert.IsNotNull(dir, "the repository root");
            var q = TestNavMeshes.Query(Path.Combine(dir.FullName, "navmesh", DepotNavMesh + ".nav"));
            var floor = q.Nearest(landing);
            Assert.IsTrue(floor.HasValue, "the navmesh is near the arrival");
            Assert.IsLessThan(0.3f, Across(floor.Value, landing), $"on the navmesh across, nearest {floor}");
            Assert.IsLessThan(0.5f, Math.Abs(floor.Value.Y - landing.Y), $"on the navmesh in height, nearest {floor}");

            // The depot's floor ends 5.3 m from link 39's centre, short of the door frame: the edge of its circle is what
            // the player walks into, towards the door from the arrival.
            var towardsArrival = Vector3.Normalize(new Vector3(landing.X - exit.Position.X, 0, landing.Z - exit.Position.Z));
            var wayOut = q.Nearest(exit.Position + towardsArrival * (exit.Radius - 0.5f));
            Assert.IsTrue(wayOut.HasValue && Rasa.Managers.MapLinkManager.Contains(exit, wayOut.Value), $"walkable ground in link 39 ({wayOut})");
            Assert.AreEqual(true, q.Reachable(floor.Value, wayOut.Value), "from the arrival the player walks out through link 39");
        }
    }
}
