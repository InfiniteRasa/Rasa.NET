using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Context.World;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    /// <summary>
    /// The Proving Grounds' pistol and machine gun soldiers attack with their weapons' own action
    /// and argument, which is what the client plays the shot by (Fix_bootcamp_soldier_weapon_attacks).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampSoldierWeaponTests
    {
        private const string Before = "20261203000000_Add_bootcamp_cave_in_breach";
        private const string Migration = "20261204000000_Fix_bootcamp_soldier_weapon_attacks";

        /// <summary>The client's weaponclass: a weapon class's attack action and argument (fields 1 and 2).</summary>
        private static readonly Dictionary<uint, (uint Action, uint Arg)> WeaponClassAttack = new()
        {
            [6271] = (1, 133),      // Weapon_Human_Redshirt_Pistol_Physical
            [20535] = (149, 1),     // Weapon_Human_Redshirt_MachineGun_Physical
        };

        /// <summary>The soldiers: the three escorts, the sandbag post's Infantrymen, the Field Gunner.</summary>
        private static readonly uint[] Soldiers = { 510213, 510214, 510215, BootcampBaseNpcs.PistolInfantrymanId, BootcampBaseNpcs.FieldGunnerId };

        [TestMethod]
        public void EachSoldierAttacksWithItsWeaponsActionAndArgumentAndDownPutsThemBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                AssertMatched(world);

                var migrator = world.GetService<IMigrator>();
                migrator.Migrate(Before);

                var pistol = world.CreatureActionEntries.AsNoTracking().Single(row => row.Id == BootcampSoldierWeapons.Pistol);
                Assert.AreEqual((1u, 1u), (pistol.ActionId, pistol.ActionArgId), "the Thrax pistol's attack, as it was");
                Assert.IsFalse(world.CreatureActionEntries.AsNoTracking().Any(row => row.Id == BootcampSoldierWeapons.MachineGun));
                Assert.AreEqual(2u, world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.FieldGunnerId).Action1);

                migrator.Migrate(Migration);

                AssertMatched(world);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static void AssertMatched(WorldContext world)
        {
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
            var creatures = world.CreatureEntries.AsNoTracking().Where(row => Soldiers.Contains(row.Id)).ToList();
            var weapons = world.CreatureAppearanceEntries.AsNoTracking()
                .Where(row => Soldiers.Contains(row.Id) && row.SlotId == 13).ToDictionary(row => row.Id, row => row.ClassId);

            Assert.HasCount(Soldiers.Length, creatures);

            foreach (var creature in creatures)
            {
                var action = actions[creature.Action1];
                var weapon = weapons[creature.Id];

                Assert.AreEqual(WeaponClassAttack[weapon], (action.ActionId, action.ActionArgId),
                    $"creature {creature.Id} holds {weapon} and attacks with action {action.Id}");
            }

            var machineGun = actions[BootcampSoldierWeapons.MachineGun];
            Assert.AreEqual(1u, machineGun.DamageType, "physical");
            Assert.AreEqual(20.0, machineGun.RangeMax, 0.0001);
            Assert.AreEqual(BootcampSoldierWeapons.MachineGun,
                world.CreatureEntries.AsNoTracking().Single(row => row.Id == BootcampBaseNpcs.FieldGunnerId).Action1);
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "update creature_action set action_arg_id = 133 where id = 510218;");
            StringAssert.Contains(up, "values (510219, 'Bootcamp Field Gunner machine gun', 149, 1, 1, 20, 1000, 0, 10, 15, 1);");
            StringAssert.Contains(up, "update creature set action1 = 510219 where id = 400008;");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "update creature_action set action_arg_id = 1 where id = 510218;");
            StringAssert.Contains(down, "delete from creature_action where id = 510219;");
        }
    }
}
