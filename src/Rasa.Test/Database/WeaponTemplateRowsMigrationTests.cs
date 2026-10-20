using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // Every template of a weapon class has an itemtemplate_weapon row (Weapon_template_rows). The
    // client raised on the first shot or reload of one without, the Cryogenic Shotgun (49286) among them.
    [TestClass]
    [DoNotParallelize]
    public class WeaponTemplateRowsMigrationTests
    {
        private const string Migration = "20261230000000_Weapon_template_rows";
        private const string Previous = "20261229000000_Three_devils";
        private const uint CryogenicShotgun = 49286;
        private const uint SnowballLauncher = 131482;

        private static string WeaponTypes => string.Join(" or ",
            WeaponTemplateStats.Types.Select(type => $"(k.attack_action_id = {type.AttackActionId} and k.weapon_anim_condition_code = {type.AnimCode})"));

        private static string WithoutRow =>
            "select count(*) as Value from itemtemplate_itemclass c join weaponclass k on k.id = c.itemClassId " +
            $"where ({WeaponTypes}) and not exists (select 1 from itemtemplate_weapon w where w.id = c.itemTemplateId)";

        [TestMethod]
        public void EveryWeaponTemplateButTheTenWithNothingToCopyHasARow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(WeaponTemplateRows.Targets.Count + WeaponTemplateRows.WithoutDonor.Count, Count(database, WithoutRow));

            migrator.Migrate(Migration);
            Assert.AreEqual(WeaponTemplateRows.WithoutDonor.Count, Count(database, WithoutRow));
            CollectionAssert.AreEquivalent(new uint[]
            {
                62, 64, 2371, 130397, 130510, 130512, 130514, // TEST_ classes
                122836,                                       // DELETEME_Weapon_Avatar_Rifle
                11433,                                        // Weapon_Human_Redshirt_InjectionGun_EMP
                130502                                        // Weapon_Creature_Loper_Spear
            }, WeaponTemplateRows.WithoutDonor.ToArray());
        }

        [TestMethod]
        public void TheCryogenicShotgunFiresAsTheIceShotgunsDoWithItsOwnMeleeDamage()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var target = WeaponTemplateRows.Targets.Single(target => target.Template == CryogenicShotgun);
            Assert.IsFalse(target.SameClass, "no template of its retired class has a row");

            var shotgun = Row(database, CryogenicShotgun);
            var donor = Row(database, target.Donor);
            for (var i = 0; i < WeaponTemplateRows.Columns.Length; i++)
            {
                if (WeaponTemplateRows.Columns[i] == "alt_max_damage")
                    Assert.AreEqual("0", shotgun[i], "its class deals nothing, in the client's data as in ours");
                else
                    Assert.AreEqual(donor[i], shotgun[i], WeaponTemplateRows.Columns[i]);
            }

            Assert.AreEqual("9", shotgun[System.Array.IndexOf(WeaponTemplateRows.Columns, "tool_type")], "a shotgun");
            Assert.AreEqual("3", shotgun[System.Array.IndexOf(WeaponTemplateRows.Columns, "ammo_per_shot")], "three rounds a shot");
        }

        [TestMethod]
        public void ATemplateGivenItsSiblingsRowHasItColumnForColumn()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var siblings = WeaponTemplateRows.Targets.Where(target => target.SameClass).ToList();
            Assert.IsTrue(siblings.Count > 2000);

            var differ = string.Join(" or ", WeaponTemplateRows.Columns.Select(column => $"t.{column} <> d.{column}"));
            foreach (var chunk in siblings.Chunk(400))
            {
                var pairs = string.Join(" union all ", chunk.Select(target => $"select {target.Template} as template, {target.Donor} as donor"));
                Assert.AreEqual(0, Count(database,
                    $"select count(*) as Value from ({pairs}) p join itemtemplate_weapon t on t.id = p.template " +
                    $"join itemtemplate_weapon d on d.id = p.donor where {differ}"));
            }
        }

        [TestMethod]
        public void ARowAlreadyThereIsKeptToolsAreLeftAloneAndDownTakesTheRowsAway()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Count(database, "select count(*) as Value from itemtemplate_weapon");
            database.Context.Database.ExecuteSqlRaw(
                $"insert into itemtemplate_weapon (id, {string.Join(", ", WeaponTemplateRows.Columns)}) " +
                $"select {CryogenicShotgun}, {string.Join(", ", WeaponTemplateRows.Columns.Select(column => column == "`range`" ? "99" : column))} from itemtemplate_weapon where id = 989");

            migrator.Migrate(Migration);
            Assert.AreEqual("99", Row(database, CryogenicShotgun)[System.Array.IndexOf(WeaponTemplateRows.Columns, "`range`")], "the row put in by hand is kept");
            Assert.AreEqual(before + WeaponTemplateRows.Targets.Count, Count(database, "select count(*) as Value from itemtemplate_weapon"));
            Assert.AreEqual(0, Count(database, $"select count(*) as Value from itemtemplate_weapon where id = {SnowballLauncher}"), "a tool keeps none");

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Count(database, "select count(*) as Value from itemtemplate_weapon"));
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in WeaponTemplateRows.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in WeaponTemplateRows.Down())
                StringAssert.Contains(down, sql);
        }

        private static int Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();

        private static string[] Row(OrdinaryLootTestDatabase database, uint template) =>
            WeaponTemplateRows.Columns.Select(column => database.Context.Database
                    .SqlQueryRaw<string>($"select cast({column} as text) as Value from itemtemplate_weapon where id = {template}")
                    .AsEnumerable().Single())
                .ToArray();
    }
}
