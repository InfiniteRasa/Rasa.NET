using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;
using Rasa.Test.Database;

namespace Rasa.Test.Missions.Plateau
{
    /// <summary>
    /// The report: every use of a Soyuz debris piece on the Plateau was refused ("Rejected stale or unauthorized
    /// scene object use"). The client sends a use with its class's usabledata.lookup argument, 1 for the debris
    /// classes 29783 and 29784, and the scene asked for 3 (Plateau_soyuz_debris_use, PlateauSoyuzSalvageV1).
    /// </summary>
    [TestClass, DoNotParallelize]
    public class PlateauSoyuzDebrisUseTests
    {
        private const string Migration = "20271008000000_Plateau_soyuz_debris_use";
        private const string Previous = "20271007000000_Marshes_logos_element";

        /// <summary>
        /// The argument the 1.16.5.0 client sends to use an object of each class a mission scene binds a use to:
        /// usabledata.lookup[class][0], or 1 where it gives none (client/augmentations/usable.py, Usable.__init__).
        /// </summary>
        private static readonly Dictionary<uint, uint> ClientUseArgument = new()
        {
            [4231] = 1, [6137] = 1, [7334] = 1, [7467] = 1, [7468] = 1, [7636] = 1, [7827] = 3, [7837] = 3, [7847] = 3, [7870] = 1,
            [9478] = 1, [10127] = 1, [10512] = 3, [11078] = 3, [11096] = 3, [11114] = 3, [11116] = 3, [11309] = 1,
            [12997] = 1, [20631] = 1, [20786] = 3, [20823] = 1, [21568] = 3, [21824] = 1, [21841] = 3, [21997] = 3, [22105] = 1, [22518] = 3,
            [22519] = 3, [22520] = 3, [22521] = 3, [22522] = 3, [22987] = 3, [24918] = 1, [26721] = 1, [29148] = 1, [29282] = 1,
            [29286] = 1, [29287] = 1, [29783] = 1, [29784] = 1,
            // Crucible (usabledata.lookup, 1.16.5.0): 7007 (3, 0, 0, 0, 0.0); the others' first entry is None.
            [7007] = 3, [7865] = 1, [7931] = 1, [7934] = 1, [10749] = 1, [24980] = 1, [24989] = 1, [24990] = 1, [24995] = 1,
            [26578] = 1, [26580] = 1
        };

        [TestMethod]
        public void TheDebrisClassesTakeTheClientsUseArgument()
        {
            Assert.AreEqual(1U, PlateauSoyuzSalvageV1.UseArgument);
            foreach (var piece in PlateauSoyuzSalvageV1.Pieces)
                Assert.AreEqual(ClientUseArgument[piece.Class], PlateauSoyuzSalvageV1.UseArgument, piece.Role);
        }

        [TestMethod]
        public void UpGivesTheThreePiecesTheClientsArgumentAndDownPutsTheSeededOneBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Bindings(database);
            CollectionAssert.AreEqual(new uint[] { 3, 3, 3 }, Arguments(before), "seeded with 3");

            migrator.Migrate(Migration);
            var after = Bindings(database);
            CollectionAssert.AreEqual(new uint[] { 1, 1, 1 }, Arguments(after));
            Assert.AreEqual(before.Replace("\"actionArgId\":3", "\"actionArgId\":1"), after, "only the arguments change");

            // ExecuteSqlRaw reads braces as its own placeholders: the JSON's are doubled.
            foreach (var sql in PlateauSoyuzSalvageV1.RepairUseUp())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            Assert.AreEqual(after, Bindings(database), "Up twice");

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Bindings(database), "Down puts the 3 back");
        }

        [TestMethod]
        public void APieceWhoseUseWasChangedByHandIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE mission_scene_binding SET bindings = REPLACE(bindings, {0}, {1}) WHERE mission_id = {2}",
                PlateauSoyuzSalvageV1.UseActionJson(2, 3), PlateauSoyuzSalvageV1.UseActionJson(2, 7), PlateauSoyuzSalvageV1.MissionId);

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new uint[] { 1, 7, 1 }, Arguments(Bindings(database)));
            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new uint[] { 3, 7, 3 }, Arguments(Bindings(database)));
        }

        [TestMethod]
        public void OnMySqlTheArgumentsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in PlateauSoyuzSalvageV1.RepairUseUp())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in PlateauSoyuzSalvageV1.RepairUseDown())
                StringAssert.Contains(down, sql);
        }

        /// <summary>
        /// Every object a mission scene binds a use to asks for the argument the client sends for its class. A
        /// class not in the table is a new one: look its argument up in the client's usabledata.lookup and add it.
        /// </summary>
        [TestMethod]
        public void EverySceneObjectsUseTakesTheArgumentTheClientSendsForItsClass()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();
            var uses = 0;

            foreach (var row in database.Context.Set<MissionSceneBindingEntry>().AsNoTracking().ToList())
            {
                using var json = JsonDocument.Parse(row.Bindings);
                if (!json.RootElement.TryGetProperty("actors", out var actors))
                    continue;

                foreach (var actor in actors.EnumerateObject())
                {
                    if (!actor.Value.TryGetProperty("useAction", out var use) || use.ValueKind != JsonValueKind.Object)
                        continue;

                    var classId = actor.Value.GetProperty("templateId").GetUInt32();
                    var argument = use.GetProperty("actionArgId").GetUInt32();
                    Assert.IsTrue(ClientUseArgument.TryGetValue(classId, out var client),
                        $"mission {row.MissionId} {actor.Name}: class {classId} is not in the table; add its usabledata.lookup argument");
                    Assert.AreEqual(client, argument, $"mission {row.MissionId} {actor.Name}: class {classId}");
                    uses++;
                }
            }

            Assert.IsTrue(uses > 100, $"{uses} scene objects with a use");
        }

        private static string Bindings(OrdinaryLootTestDatabase database) =>
            database.Context.Set<MissionSceneBindingEntry>().AsNoTracking().Single(row => row.MissionId == PlateauSoyuzSalvageV1.MissionId).Bindings;

        private static uint[] Arguments(string bindings)
        {
            using var json = JsonDocument.Parse(bindings);
            var actors = json.RootElement.GetProperty("actors");
            return PlateauSoyuzSalvageV1.Pieces
                .Select(piece => actors.GetProperty(piece.Role).GetProperty("useAction").GetProperty("actionArgId").GetUInt32()).ToArray();
        }
    }
}
