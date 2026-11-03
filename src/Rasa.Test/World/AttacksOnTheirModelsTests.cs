using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;

    using A = Rasa.Services.Preloader.AttacksOnTheirModels;
    using M = Rasa.Services.Preloader.MagmaCavernsContent;

    /// <summary>
    /// Ember's, the Magmonix', the Pacified Thrax' and Queen Kardash's attacks on animations
    /// their models have (AttacksOnTheirModels); CreatureAttackAnimationTests holds the whole
    /// world to it. Down puts the old ones back, and Up again changes nothing more.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AttacksOnTheirModelsTests
    {
        private const string Migration = "20271022000000_Attacks_on_their_models", Previous = "20271021000000_Instance_creature_kits";

        [TestMethod]
        public void TheAttacksMoveOnToTheirModelsAndDownPutsThemBack()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            AssertActions(database, before: true);

            migrator.Migrate(Migration);
            AssertActions(database, before: false);

            foreach (var sql in A.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            AssertActions(database, before: false);

            migrator.Migrate(Previous);
            AssertActions(database, before: true);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id IN ({A.EmberStomp}, {A.SubjectRifle})"));
        }

        // It was 20271021000000, Instance_creature_kits' timestamp: a World that ran it under that id
        // runs it again under this one, changing nothing, and loses the old id's history row.
        [TestMethod]
        public void AWorldThatRanItUnderItsOldIdRunsItAgainAsItIsAndForgetsTheOldId()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            foreach (var sql in A.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{A.OldId}', '10.0.0');");
            AssertActions(database, before: false);

            migrator.Migrate(Migration);
            AssertActions(database, before: false);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM __EFMigrationsHistory WHERE MigrationId = '{A.OldId}'"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM __EFMigrationsHistory WHERE MigrationId = '{Migration}'"));
        }

        private static void AssertActions(Rasa.Test.Database.OrdinaryLootTestDatabase database, bool before)
        {
            foreach (var (now, was) in A.Changed)
            {
                var row = before ? was : now;
                Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id = {row.Id} " +
                    $"AND action_id = {row.ActionId} AND action_arg_id = {row.ArgId} AND damage_type = {row.DamageType}"), $"{row.Description}");
            }

            foreach (var (creature, slot, now, was) in A.Slots)
                Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id = {creature} AND {slot} = {(before ? was : now)}"),
                    $"creature {creature} {slot}");

            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id = {M.Ember} AND action1 = {A.EmberHowl}"));
        }

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();
    }
}
