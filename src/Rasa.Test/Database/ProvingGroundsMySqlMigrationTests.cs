using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class ProvingGroundsMySqlMigrationTests
    {
        [TestMethod]
        public void WorldWideningRunsBeforeTheFrontSquadsAndMatchesTheMySqlModel()
        {
            const string before = "20270110000100_Bootcamp_breach_assault";
            const string widen = "20270110000200_WidenCreatureCommentBeforeFrontSquads";
            const string seed = "20270111000000_Proving_grounds_front_squads";
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrations = context.Database.GetMigrations().ToArray();
            var previousIndex = Array.IndexOf(migrations, before);
            var widenIndex = Array.IndexOf(migrations, widen);
            var seedIndex = Array.IndexOf(migrations, seed);
            Assert.IsTrue(previousIndex >= 0 && previousIndex < widenIndex && widenIndex < seedIndex,
                "Widen creature.comment after the preceding migration but before front-squad inserts.");

            Assert.AreEqual("text", context.Model.FindEntityType(typeof(CreatureEntry))?
                .FindProperty(nameof(CreatureEntry.Comment))?.GetColumnType(),
                "MySQL's runtime model must match the widened comment column.");

            var migration = new Rasa.Migrations.MySqlWorld.WidenCreatureCommentBeforeFrontSquads();
            Assert.HasCount(1, migration.UpOperations);
            Assert.IsInstanceOfType<AlterColumnOperation>(migration.UpOperations.Single());
            var column = (AlterColumnOperation)migration.UpOperations.Single();
            Assert.AreEqual("creature", column.Table);
            Assert.AreEqual("comment", column.Name);
            Assert.AreEqual("varchar(50)", column.OldColumn.ColumnType);
            Assert.AreEqual("text", column.ColumnType);
            Assert.IsFalse(column.IsNullable);
            Assert.IsEmpty(migration.DownOperations, "Never shrink persisted comments and truncate data.");

            Assert.IsTrue(ProvingGroundsFrontSquads.Npcs.Any(npc => npc.Comment.Length > 50),
                "This migration protects actual seeded text, not a synthetic example.");
            Assert.AreEqual(57, ProvingGroundsFrontSquads.Npcs.Single(npc =>
                npc.Id == ProvingGroundsFrontSquads.FieldGunner).Comment.Length);
        }

        [TestMethod]
        public void PartialMySqlFailureCanRetryTheCreateTableAndGuardedSeed()
        {
            // MySQL commits CREATE TABLE independently from the subsequent seed statements.
            // A failed migration can leave spawnpool_draw present without a history row.
            var migration = new Rasa.Migrations.MySqlWorld.Proving_grounds_front_squads();
            Assert.IsInstanceOfType<SqlOperation>(migration.UpOperations.First());
            StringAssert.Contains(((SqlOperation)migration.UpOperations.First()).Sql,
                "CREATE TABLE IF NOT EXISTS `spawnpool_draw`");
            var sql = ProvingGroundsFrontSquads.Up().ToArray();
            Assert.IsTrue(sql.Any(statement => statement.Contains("NOT EXISTS (SELECT 1 FROM creature x")));
            Assert.IsTrue(sql.Any(statement => statement.Contains("NOT EXISTS (SELECT 1 FROM spawnpool x")));
            Assert.IsTrue(sql.Any(statement => statement.Contains("NOT EXISTS (SELECT 1 FROM spawnpool_draw x")));
        }
    }
}
