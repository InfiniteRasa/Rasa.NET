using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Context.World;
    using Rasa.Services.Preloader;

    /// <summary>
    /// The constant-fire weapons marked autofire, windup -1 (Constant_fire_autofire,
    /// ConstantFireAutofire): propellant guns, chainguns and the Series 2/3 laser pistols, leech
    /// guns and polarity guns; every other weapon keeps its windup.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ConstantFireAutofireTests
    {
        private const string Before = "20270124000000_Weapon_windup_signed";
        private const string Migration = "20270125000000_Constant_fire_autofire";

        private static readonly (uint Template, string Weapon, int Windup)[] Samples =
        {
            (130286, "Weapon_Avatar_MachineGun_v2_Physical_CMN_27_to_29", -1),
            (1357, "Weapon_Avatar_PropellantGun_Fire_CMN_32_to_36", -1),
            (1285, "Weapon_Avatar_LeechGun_Physical_CMN_14_to_18", -1),
            (1679, "Weapon_Avatar_PolarityGun_Electric_CMN_33_to_37", -1),
            (130325, "Weapon_Avatar_Pistol_v2_Laser_CMN_21_to_23", -1),
            (130398, "Weapon_Avatar_Pistol_v3_laser_CMN_31_to_33", -1),
            (165, "Weapon_Avatar_Rifle_Physical_CMN_11_to_15", 800),
            (145, "Weapon_Avatar_Pistol_Physical_CMN_01_to_04", 800),
            (166, "Weapon_Avatar_Shotgun_v1_Physical_CMN_12_to_16", 800),
        };

        [TestMethod]
        public void TheConstantFireWeaponsAreAutofireAndNoOthers()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var (template, weapon, windup) in Samples)
                Assert.AreEqual(windup, Windup(database, template), weapon);

            // Every row by its class's attack action: -1 for the four constant-fire actions, 800 for the rest.
            var rows = Strings(database,
                "SELECT wc.attack_action_id || ' ' || w.windup AS Value FROM itemtemplate_weapon w " +
                "JOIN itemtemplate_itemclass ic ON ic.itemTemplateId = w.id JOIN weaponclass wc ON wc.id = ic.itemClassId");
            var byAction = rows.Select(row => row.Split(' ')).GroupBy(parts => uint.Parse(parts[0]))
                .ToDictionary(group => group.Key, group => group.Select(parts => int.Parse(parts[1])).Distinct().ToList());

            foreach (var action in ConstantFireAutofire.ConstantFireActions)
                CollectionAssert.AreEqual(new[] { -1 }, byAction[action], $"action {action}");
            foreach (var (action, windups) in byAction.Where(pair => !ConstantFireAutofire.ConstantFireActions.Contains(pair.Key)))
                CollectionAssert.AreEqual(new[] { 800 }, windups, $"action {action}");

            // Each group is there in numbers, not one stray row.
            foreach (var action in ConstantFireAutofire.ConstantFireActions)
                Assert.IsGreaterThan(100, rows.Count(row => row.StartsWith($"{action} ")), $"action {action}");
        }

        [TestMethod]
        public void AGameMastersWindupIsLeftAloneTheUpRunsTwiceAndTheDownPutsThePlaceholderBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            database.Context.Database.ExecuteSqlRaw("UPDATE itemtemplate_weapon SET windup = 650 WHERE id = 1357;");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(650, Windup(database, 1357), "a propellant gun a game master retimed");
            Assert.AreEqual(-1, Windup(database, 1359));
            var once = Snapshot(database);

            foreach (var sql in ConstantFireAutofire.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Before);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void TheLoadedTemplateCarriesTheAutofireWindup()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            var row = ((WorldContext)database.Context).ItemTemplateWeaponEntries.AsNoTracking().Single(entry => entry.Id == 130286);
            Assert.AreEqual(Rasa.Structures.World.ItemTemplateWeaponEntry.AutofireWindup, row.Windup);
            Assert.AreEqual(-1, new Rasa.Structures.WeaponInfo(row).Windup, "what ItemTemplateTooltipInfo sends");
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            foreach (var sql in ConstantFireAutofire.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ConstantFireAutofire.Down())
                StringAssert.Contains(down, sql);
        }

        private static int Windup(OrdinaryLootTestDatabase database, uint template) =>
            (int)database.Context.Database.SqlQueryRaw<long>($"SELECT windup AS Value FROM itemtemplate_weapon WHERE id = {template}").AsEnumerable().Single();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Strings(database, "SELECT id || ' ' || windup AS Value FROM itemtemplate_weapon").OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
