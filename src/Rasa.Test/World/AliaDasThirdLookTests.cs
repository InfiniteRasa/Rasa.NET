using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Alia Das after a third look round (Alia_das_third_look, AliaDasThirdLook), and the CELLAR
    /// pad at Alia Das shown on to whoever is by it when it is put in the world.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AliaDasThirdLookTests
    {
        private const string Migration = "20270107000000_Alia_das_third_look";
        private const string Previous = "20270106000000_Local_teleporter_links";

        [TestMethod]
        public void TheyLookAsTheGmAsked()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var look in AliaDasThirdLook.Looks)
                CollectionAssert.AreEqual(look.Is.OrderBy(s => s.Id).Select(s => $"{s.Id} {s.ClassId} {s.Color}").ToList(),
                    Strings(database, $"SELECT slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {look.Creature} ORDER BY slot_id"), look.Who);

            // The client's own colour, which leaves a texture as painted: (0, 0, 0, 255).
            Assert.AreEqual(0xFF000000u, AliaDasThirdLook.Unhued);
            Assert.AreEqual(0xFFFFFFFFu, AliaDasThirdLook.PureWhite);
        }

        [TestMethod]
        public void ThePadIsShownOnToWhoeverIsByItWhenItIsPutInTheWorld()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pad = harness.Map.Teleporters[990];
            Assert.AreEqual((EntityClasses)AliaDasSecondLook.HumanTeleporter, pad.EntityClassId);

            // Taken out again, and put back with a player beside it: as on a map whose worker has
            // just started, the first player on it already there.
            CellManager.Instance.RemoveFromWorld(harness.Map, pad);
            pad.IsInWorld = false;
            pad.StateId = 0;
            pad.RespawnTime = 0;
            harness.MoveTo(pad.Position + new Vector3(2, 0, 0));
            harness.Drain();

            harness.Objects.DynamicObjectWorker(harness.Map, 0);

            var made = harness.Drain().OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == pad.EntityId);
            // Its own mesh with the blue field: USE_TS_STATE_0. In USE_TS_STATE_1 the client swaps
            // in arch_hum_teleporter_disabled.geo (DynamicObjectManager.TeleporterRunningState).
            Assert.AreEqual(UseObjectState.TsState0, made.EntityData.OfType<UsableInfoPacket>().Single().CurState, "on, with its field");

            Assert.AreEqual(UseObjectState.TsState0, pad.StateId, "and so to whoever comes later");
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedAndTheDownPutsBackWhatItChanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM recoloured Salter's helmet by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature_appearance SET color = 123 WHERE id = {AliaDasNpcs.Salter} AND slot_id = 1");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual($"{AliaDasSecondLook.Medium1Helm} 123",
                Strings(database, $"SELECT Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Salter} AND slot_id = 1").Single(), "the GM's helmet kept");
            Assert.AreEqual($"{AliaDasThirdLook.Mech1Torso} {AliaDasThirdLook.Unhued}",
                Strings(database, $"SELECT Class_id || ' ' || color AS Value FROM creature_appearance WHERE id = {AliaDasNpcs.Salter} AND slot_id = 15").Single());
            Assert.AreEqual(0, Strings(database, $"SELECT slot_id AS Value FROM creature_appearance WHERE id = {AliaDasTouchups.FieldMedic} AND slot_id IN (2, 16)").Count,
                "the Field Medic's second trousers and boots off");

            var once = Snapshot(database);
            foreach (var sql in AliaDasThirdLook.Up())
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

            foreach (var sql in AliaDasThirdLook.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in AliaDasThirdLook.Down())
                StringAssert.Contains(down, sql);
        }

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Strings(database, "SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance " +
                    $"WHERE id IN ({string.Join(", ", AliaDasThirdLook.Looks.Select(l => l.Creature))})")
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
