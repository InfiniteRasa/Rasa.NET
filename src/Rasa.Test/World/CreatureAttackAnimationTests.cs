using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.TestSupport;

    /// <summary>
    /// Every creature's attacks are ones the client animates on its model (Fix_creature_attack_animations):
    /// each windup and recovery animation family of the attack's action and argument (action_level,
    /// the client's actionArguments) has an animation on the skeleton of the creature class's mesh.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureAttackAnimationTests
    {
        private const string Before = "20261204000000_Fix_bootcamp_soldier_weapon_attacks";
        private const string Migration = "20261205000000_Fix_creature_attack_animations";

        /// <summary>
        /// Creature abilities kept although the model does not animate them (CreatureAttackAnimations):
        /// the client still attaches their effects, and the server runs each as a mechanic.
        /// Class id and action id.
        /// </summary>
        private static readonly HashSet<(uint Class, uint Action)> KeptAbilities = new()
        {
            (7078, 475),    // Creature_Atta_Harvester_Standard: CR_ATTA_HARVESTER_ACID_SPIT (the Atta Soldier's spit, 1378)
            (10164, 471),   // Creature_Granitour_Young: CR_GRANITOUR_MELEE (the adult's bite, 1334)
            (24085, 278),   // Bane_Thrax_Machina_Boss: CR_THRAX_SHRAPNEL
            (24085, 451),   // Bane_Thrax_Machina_Boss: CR_THRAX_FORCE_BLAST
            (24085, 458),   // Bane_Thrax_Machina_Boss: CR_THRAX_TECTONIC_STRIKE
            (21882, 488),   // Bane_Thrax_Grenadier_Boss: CR_THRAX_NECROMITE
        };

        /// <summary>
        /// Creatures whose attack is the one their held weapon (creature_appearance slot 13) attacks
        /// with in the client's weaponclass: the AFS rifle soldiers' Weapon_Avatar_Rifle_Physical_UNC_01_to_05
        /// and Warrior Aprika's Weapon_Creature_NPC_Forean_GooGun.
        /// </summary>
        private static readonly Dictionary<uint, (uint Weapon, uint Action, uint Arg)> HeldWeapons = new()
        {
            [510217] = (27220, 1, 134),
            [510227] = (27220, 1, 134),
            [43] = (10530, 1, 116),
        };

        [TestMethod]
        public void EveryCreaturesAttacksAnimateOnItsModelAndDownPutsTheOldOnesBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                AssertEveryAttackAnimates(world);
                AssertEachFiresTheWeaponItHolds(world);

                var migrator = world.GetService<IMigrator>();
                migrator.Migrate(Before);

                var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
                var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);

                Assert.IsFalse(actions.Keys.Any(id => id >= CreatureAttackAnimations.IdMin && id <= CreatureAttackAnimations.IdMax));
                Assert.AreEqual(2u, creatures[1].Action1, "the Winged Fithik on the AFS pistol, as it was");
                Assert.AreEqual(8u, creatures[630076].Action1, "the Bane mortar on the AFS turret's gun, as it was");
                Assert.AreEqual(17u, creatures[630010].Action1, "Milpas on the spear's ranged attack, as it was");
                Assert.AreEqual((17u, 6u, 5u), (creatures[50].Action1, creatures[50].Action2, creatures[50].Action3));
                Assert.AreEqual((174u, 48u), (actions[53002].ActionId, actions[53002].ActionArgId));
                Assert.AreEqual((1u, 1u), (actions[33].ActionId, actions[33].ActionArgId));
                Assert.AreEqual((174u, 17u), (actions[55033].ActionId, actions[55033].ActionArgId));
                Assert.AreEqual(2u, creatures[43].Action1);
                Assert.AreEqual(CreatureAttackAnimations.AprikaSpear,
                    world.CreatureAppearanceEntries.AsNoTracking().Single(row => row.Id == 43 && row.SlotId == 13).ClassId, "Aprika's spear, as it was");

                migrator.Migrate(Migration);

                AssertEveryAttackAnimates(world);
                AssertEachFiresTheWeaponItHolds(world);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static void AssertEveryAttackAnimates(WorldContext world)
        {
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
            var levels = world.ActionLevelEntries.AsNoTracking().ToList()
                .GroupBy(row => (row.ActionId, row.Level)).ToDictionary(group => group.Key, group => group.First());
            var meshes = world.EntityClassEntries.AsNoTracking().ToDictionary(row => row.Id, row => row.MeshId);
            var problems = new List<string>();

            foreach (var creature in world.CreatureEntries.AsNoTracking().ToList())
            {
                var slots = new[] { creature.Action1, creature.Action2, creature.Action3, creature.Action4,
                    creature.Action5, creature.Action6, creature.Action7, creature.Action8 }.Where(id => id != 0).ToList();

                if (slots.Count == 0)
                    continue;

                if (!ClientAnimationSkeletons.MeshSkeleton.TryGetValue(meshes[creature.ClassId], out var skeleton))
                {
                    problems.Add($"creature {creature.Id} ({creature.Comment}): mesh {meshes[creature.ClassId]} of class {creature.ClassId} is not in ClientAnimationSkeletons (animationdata.meshSkeleton)");
                    continue;
                }

                var animated = ClientAnimationSkeletons.SkeletonFamilies[skeleton];

                foreach (var slot in slots)
                {
                    var action = actions[slot];

                    if (!levels.TryGetValue((action.ActionId, action.ActionArgId), out var level))
                    {
                        problems.Add($"creature {creature.Id} ({creature.Comment}): action {slot} is {action.ActionId}/{action.ActionArgId}, which the client has no row for");
                        continue;
                    }

                    if (KeptAbilities.Contains((creature.ClassId, action.ActionId)))
                        continue;

                    foreach (var family in new[] { level.WindupAnimFamilyId, level.RecoveryAnimFamilyId })
                        if (family is uint id && !ClientAnimationSkeletons.FamiliesOnNoSkeleton.Contains(id) && !animated.Contains(id))
                            problems.Add($"creature {creature.Id} ({creature.Comment}): action {slot} ({action.ActionId}/{action.ActionArgId}) plays family {id}, which skeleton {skeleton} has no animation for");
                }
            }

            Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
        }

        private static void AssertEachFiresTheWeaponItHolds(WorldContext world)
        {
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);

            foreach (var (id, expected) in HeldWeapons)
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == id);
                var weapon = world.CreatureAppearanceEntries.AsNoTracking().Single(row => row.Id == id && row.SlotId == 13).ClassId;
                var action = actions[creature.Action1];

                Assert.AreEqual(expected, (weapon, action.ActionId, action.ActionArgId), $"creature {id}: held weapon, and the pair of its attack {action.Id}");
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "update creature_action set action_id = 174, action_arg_id = 46 where id = 53002;");
            StringAssert.Contains(up, "update creature_action set action_id = 1, action_arg_id = 190 where id = 33;");
            StringAssert.Contains(up, "values (72004, 'Wilderness Bane mortar weapon 411/1', 411, 1, 0.0, 50.0, 400, 400, 10, 20, 13);");
            StringAssert.Contains(up, "update creature set action1 = 72003 where id = 510217;");
            StringAssert.Contains(up, "update creature set action1 = 6 where id = 630010;");
            StringAssert.Contains(up, "update creature_appearance set Class_id = 10530 where id = 43 and slot_id = 13 and Class_id = 10532;");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "update creature set action1 = 17 where id = 630010 and action1 = 6;");
            StringAssert.Contains(down, "delete from creature_action where id between 72001 and 72999;");
            StringAssert.Contains(down, "update creature_action set action_id = 174, action_arg_id = 17 where id = 55033;");
        }
    }
}
