using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Context;
    using Rasa.Context.Char;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.Char.Items;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    // Item modules (ItemModules): what an item carries in its four module slots, the world
    // database's module_class and module_effect, and what the client is told of them.
    [TestClass]
    [DoNotParallelize]
    public class ItemModuleTests
    {
        private const string WorldBefore = "20261119000000_Place_marked_teleporters";
        private const string CharBefore = "20261118000000_Oneoff_titles";
        private const string Migration = "20261120000000_Add_item_modules";

        private const uint Body1 = 100060;          // Armor Module: Body Bonus [1]
        private const uint Health1 = 100084;        // Armor Module: Health Bonus [1]
        private const uint Body5 = 900032;          // Armor Module: Body Bonus [5]
        private const uint FireResist1 = 100036;    // Armor Module: Resist Fire [1]
        private const uint StealArmor1 = 900260;    // Weapon Module: Steal Armor [1]
        private const uint FireDebuff1 = 900240;    // Weapon Module: Debuff Fire Resist [1]
        private const uint ScoutSuit = 900348;      // Set: Scout Suit Mk I

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Rasa.Logger.Config == null)
                Rasa.Logger.UpdateConfig(new Rasa.Logger.LoggerConfig());
        }

        [TestInitialize]
        public void LoadTheSeed() => ItemModules.Load(ItemModuleSeed.Classes, ItemModuleSeed.Effects);

        private static List<string> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static void InTemporaryDatabase<TContext>(Action<TContext, IMigrator> test) where TContext : RasaDbContextBase
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = (TContext)PersistenceIntegrationTests.CreateContext(typeof(TContext), Path.Combine(directory, "database"));
                test(context, context.GetService<IMigrator>());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheSeedIsTheClientsModulesAndTheEffectsItsTextGivesTheValuesOf()
        {
            var classes = ItemModuleSeed.Classes;
            var effects = ItemModuleSeed.Effects;

            Assert.AreEqual(867, classes.Count);
            Assert.AreEqual(867, classes.Select(row => row.Id).Distinct().Count());
            Assert.IsTrue(classes.All(row => row.Id != 0 && row.Comment != null && row.Comment.Length <= 64));

            // The bonus modules: 66 kinds at five strengths that are an item, and 46 that are not.
            var bonus = classes.Where(row => row.VariantId != 0).ToList();
            var held = bonus.Where(row => row.ItemTemplateId != 0).ToList();

            Assert.AreEqual(376, bonus.Count);
            Assert.AreEqual(330, held.Count);
            Assert.IsTrue(bonus.All(row => row.VariantId >= 1 && row.VariantId <= 54));

            var kinds = held.GroupBy(row => (row.VariantId, row.ClassSetId)).ToList();

            Assert.AreEqual(66, kinds.Count);
            Assert.IsTrue(kinds.All(kind => kind.Select(row => row.Level).OrderBy(level => level).SequenceEqual(new uint[] { 1, 2, 3, 4, 5 })));
            Assert.IsTrue(held.All(row => row.ClassSetId == 212 || row.ClassSetId == 1212 || row.ClassSetId == 1273), "armor, weapons, tools");
            Assert.IsTrue(bonus.Where(row => row.ItemTemplateId == 0).All(row => row.Level == 0 && row.ClassSetId == 0));

            // An upgrade is the same kind, one strength up.
            var byId = classes.ToDictionary(row => row.Id);

            foreach (var row in classes.Where(row => row.UpgradeModuleId != 0))
            {
                var upgrade = byId[row.UpgradeModuleId];

                Assert.AreEqual((row.VariantId, row.ClassSetId, row.Level + 1), (upgrade.VariantId, upgrade.ClassSetId, upgrade.Level), $"module {row.Id}");
            }

            Assert.AreEqual(264, classes.Count(row => row.UpgradeModuleId != 0), "four of each kind's five");
            Assert.AreEqual(60, classes.Count(row => row.VariantId == 0 && row.ClassSetId == 1274), "the ones that are only a salvage value");

            // The effects: one each for 58 of the kinds, none for anything else.
            Assert.AreEqual(290, effects.Count);
            Assert.AreEqual(290, effects.Select(row => row.Id).Distinct().Count());
            Assert.AreEqual(290, effects.Select(row => row.ModuleId).Distinct().Count());
            Assert.IsTrue(effects.All(row => byId[row.ModuleId].ItemTemplateId != 0 && row.SetLevel == 0 && row.Arg2 == null && row.Arg3 == null && row.Arg4 == null));
            Assert.AreEqual(58, effects.Select(row => (byId[row.ModuleId].VariantId, byId[row.ModuleId].ClassSetId)).Distinct().Count());

            // The eight "Debuff ... Resist" kinds are the ones left out: their line needs a
            // length of time the client does not have.
            var without = held.Where(row => effects.All(effect => effect.ModuleId != row.Id)).ToList();

            Assert.AreEqual(40, without.Count);
            Assert.IsTrue(without.All(row => row.Comment.StartsWith("Weapon Module: Debuff ") && row.Comment.Contains(" Resist [")));
            Assert.AreEqual(8, without.Select(row => row.VariantId).Distinct().Count());

            // What a description says is what its row has. "Base Bonus: 1 / Level Bonus: 0.05 per
            // item level", "Extra Bonus: 8 (doubles every 8 item levels)", "Base Bonus: 3",
            // "Base Bonus: 6 / Extra Bonus: 5 (doubles every 8 item levels)".
            ModuleEffectEntry Of(uint moduleId) => effects.Single(row => row.ModuleId == moduleId);

            Assert.AreEqual((330u, 1.0, 0.05, 0.0, (int?)null), (Of(Body1).EffectId, Of(Body1).FlatValue, Of(Body1).LinearValue, Of(Body1).ExpValue, Of(Body1).Arg1));
            Assert.AreEqual((336u, 0.0, 0.0, 8.0), (Of(Health1).EffectId, Of(Health1).FlatValue, Of(Health1).LinearValue, Of(Health1).ExpValue));
            Assert.AreEqual((413u, 3.0, 0.0, 0.0, (int?)DamageType.Fire), (Of(FireResist1).EffectId, Of(FireResist1).FlatValue, Of(FireResist1).LinearValue, Of(FireResist1).ExpValue, Of(FireResist1).Arg1));
            Assert.AreEqual((323u, 6.0, 0.0, 5.0), (Of(StealArmor1).EffectId, Of(StealArmor1).FlatValue, Of(StealArmor1).LinearValue, Of(StealArmor1).ExpValue));

            // A resistance is the one effect with an argument, and the argument is what is resisted.
            Assert.IsTrue(effects.All(row => (row.EffectId == 413) == (row.Arg1 != null)));
            CollectionAssert.AreEquivalent(new int?[] { 1, 2, 3, 4, 5, 6, 7, 13, 8, 9, 14, 15, 20 }, effects.Where(row => row.EffectId == 413).Select(row => row.Arg1).Distinct().ToList());

            Assert.AreEqual("Armor Module: Body Bonus [1]", byId[Body1].Comment);
            Assert.AreEqual("Set: Scout Suit Mk I", byId[ScoutSuit].Comment);
        }

        [TestMethod]
        public void TheAmountIsWorkedOutFromTheNumbersTheClientReads()
        {
            ItemModule Module(uint moduleId)
            {
                Assert.IsTrue(ItemModules.TryGet(moduleId, out var module), $"module {moduleId}");
                return module;
            }

            // Doubling every eight levels, from the level's own bonus at level 1.
            var health = Module(Health1).Effects.Single();

            Assert.AreEqual(8, health.Amount(1));
            Assert.AreEqual(16, health.Amount(9));
            Assert.AreEqual(32, health.Amount(17));
            Assert.AreEqual(12, health.Amount(5), "8 * 2^(4/8) = 11.3, rounded up");
            Assert.AreEqual(8, health.Amount(0), "an item with no level requirement: 8 * 2^(-1/8) = 7.3");

            // A flat bonus is itself at every level, and a reduction stays one.
            Assert.AreEqual(3, Module(FireResist1).Effects.Single().Amount(1));
            Assert.AreEqual(3, Module(FireResist1).Effects.Single().Amount(50));
            Assert.AreEqual(-10, Module(100052).Effects.Single().Amount(30), "Weapon Module: Threat Reduction [1]");
            Assert.AreEqual(11, Module(StealArmor1).Effects.Single().Amount(1), "6 and 5");

            // 1 and 0.05 a level: 1.05 at level 1, rounded up.
            var body = Module(Body1).Effects.Single();

            Assert.AreEqual(2, body.Amount(1));
            Assert.AreEqual(2, body.Amount(19), "1.95");
            Assert.AreEqual(3, body.Amount(21), "2.05");

            // The client has 0.05 as the single-precision number it was sent, 0.0500000007, so
            // twenty levels of it are a hair over 1 and the whole a hair over 2: it shows 3
            // where the arithmetic of the description gives 2. The server says what the client
            // shows.
            Assert.AreEqual(0.05f, body.LinearValue);
            Assert.AreNotEqual(0.05, body.LinearValue);
            Assert.AreEqual(3, body.Amount(20));

            // And that is the number read back off the wire, for every effect there is.
            foreach (var module in ItemModules.All.Where(module => module.Effects.Count > 0))
            {
                using var stream = new MemoryStream();
                using var writer = new PythonWriter(new BinaryWriter(stream));

                new ModuleTooltipInfoPacket(module.ModuleId, module).Write(writer);
                stream.Position = 0;

                using var reader = new PythonReader(new BinaryReader(stream));

                reader.ReadTuple();
                reader.ReadUInt();
                reader.ReadUInt();
                Assert.AreEqual(1, reader.ReadList());
                reader.ReadTuple();
                reader.ReadUInt();
                reader.ReadUInt();

                var flat = reader.ReadDouble();
                var linear = reader.ReadDouble();
                var exp = reader.ReadDouble();

                foreach (var level in new[] { 0, 1, 5, 8, 9, 10, 20, 25, 40, 50 })
                    Assert.AreEqual((int)Math.Ceiling(flat + linear * level + exp * Math.Pow(2, (level - 1) / 8.0)), module.Effects[0].Amount(level), $"module {module.ModuleId} at level {level}");
            }
        }

        [TestMethod]
        public void TheTooltipAnswerIsTheModulesEffectsAndAnEmptyListForOneWithNone()
        {
            static PythonReader Answer(uint moduleId, out MemoryStream stream)
            {
                ItemModules.TryGet(moduleId, out var module);
                stream = new MemoryStream();

                var writer = new PythonWriter(new BinaryWriter(stream));

                new ModuleTooltipInfoPacket(moduleId, module).Write(writer);
                stream.Position = 0;

                return new PythonReader(new BinaryReader(stream));
            }

            // (moduleId, moduleLevel, [(effectId, setLevel, flat, linear, exp, arg1, arg2, arg3, arg4)])
            using (var reader = Answer(FireResist1, out var stream))
            {
                Assert.AreEqual(3, reader.ReadTuple());
                Assert.AreEqual(FireResist1, reader.ReadUInt());
                Assert.AreEqual(1u, reader.ReadUInt(), "its strength");
                Assert.AreEqual(1, reader.ReadList());
                Assert.AreEqual(9, reader.ReadTuple());
                Assert.AreEqual(413u, reader.ReadUInt(), "MODULE_RESIST_EFFECT: \"Resist: $damageType%(arg1)s %(amount)s\"");
                Assert.AreEqual(0u, reader.ReadUInt());
                Assert.AreEqual(3.0, reader.ReadDouble());
                Assert.AreEqual(0.0, reader.ReadDouble());
                Assert.AreEqual(0.0, reader.ReadDouble());
                Assert.AreEqual((int)DamageType.Fire, reader.ReadInt());
                reader.ReadNoneStruct();
                reader.ReadNoneStruct();
                reader.ReadNoneStruct();
                Assert.AreEqual(stream.Length, stream.Position);
            }

            using (var reader = Answer(Body5, out var stream))
            {
                Assert.AreEqual(3, reader.ReadTuple());
                Assert.AreEqual(Body5, reader.ReadUInt());
                Assert.AreEqual(5u, reader.ReadUInt());
                Assert.AreEqual(1, reader.ReadList());
                Assert.AreEqual(9, reader.ReadTuple());
                Assert.AreEqual(330u, reader.ReadUInt(), "LOOT_BODY_AMOUNT: \"Body: %(amount)s\"");
                Assert.AreEqual(0u, reader.ReadUInt());
                Assert.AreEqual(5.0, reader.ReadDouble());
                Assert.AreEqual(0.25, reader.ReadDouble());
                Assert.AreEqual(0.0, reader.ReadDouble());
                reader.ReadNoneStruct();
                reader.ReadNoneStruct();
                reader.ReadNoneStruct();
                reader.ReadNoneStruct();
                Assert.AreEqual(stream.Length, stream.Position);
            }

            // A module with no effect known, a set, and an id that is no module at all: answered,
            // so the client stops asking, with nothing to draw.
            foreach (var (moduleId, level) in new[] { (FireDebuff1, 1u), (ScoutSuit, 0u), (5u, 0u) })
            {
                using var reader = Answer(moduleId, out var stream);

                Assert.AreEqual(3, reader.ReadTuple());
                Assert.AreEqual(moduleId, reader.ReadUInt());
                Assert.AreEqual(level, reader.ReadUInt());
                Assert.AreEqual(0, reader.ReadList());
                Assert.AreEqual(stream.Length, stream.Position);
            }
        }

        [TestMethod]
        public void ItemInfoNamesTheSlotsUpToTheLastFullOne()
        {
            var classInfo = new EntityClass(90001, "test_rifle", 0, 0, new List<AugmentationType> { AugmentationType.Item }, false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { Id = 90001, MaxHitPoints = 100, StackSize = 1 })
            };

            List<uint?> LootModuleIds(Item item)
            {
                using var stream = new MemoryStream();
                using var writer = new PythonWriter(new BinaryWriter(stream));

                new ItemInfoPacket(item, classInfo).Write(writer);
                stream.Position = 0;

                using var reader = new PythonReader(new BinaryReader(stream));

                Assert.AreEqual(15, reader.ReadTuple());

                for (var i = 0; i < 8; i++)
                    reader.SkipValue();

                Assert.AreEqual(0, reader.ReadList(), "classModuleIds");

                var moduleIds = new List<uint?>();

                for (var i = reader.ReadList(); i > 0; i--)
                {
                    if (reader.PeekType() == PythonType.Int)
                        moduleIds.Add(reader.ReadUInt());
                    else
                    {
                        reader.ReadNoneStruct();
                        moduleIds.Add(null);
                    }
                }

                for (var i = 0; i < 5; i++)
                    reader.SkipValue();

                Assert.AreEqual(stream.Length, stream.Position);

                return moduleIds;
            }

            var item = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 90002, ItemClass = 90001 }),
                ItemTemplateId = 90002,
                StackSize = 1,
                CurrentHitPoints = 100
            };

            CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, item.ModuleIds.ToArray());
            Assert.AreEqual(0, LootModuleIds(item).Count, "an item with no module is sent the empty list it always was");

            item.SetModule(0, Body1);
            CollectionAssert.AreEqual(new uint?[] { Body1 }, LootModuleIds(item));

            // The list is by slot: an empty slot before a full one is None, and the empty ones
            // after the last full one are not sent.
            item.SetModule(2, FireResist1);
            CollectionAssert.AreEqual(new uint?[] { Body1, null, FireResist1 }, LootModuleIds(item));

            item.SetModule(0, 0);
            CollectionAssert.AreEqual(new uint?[] { null, null, FireResist1 }, LootModuleIds(item));

            item.SetModule(3, StealArmor1);
            item.SetModule(1, Health1);
            CollectionAssert.AreEqual(new uint?[] { null, Health1, FireResist1, StealArmor1 }, LootModuleIds(item));

            Assert.AreEqual(0, ItemModules.FreeSlot(item));
            item.SetModule(0, Body5);
            Assert.AreEqual(-1, ItemModules.FreeSlot(item), "all four full");
        }

        [TestMethod]
        public void AMigratedWorldHasTheModulesAndOneTakenBackHasNeitherTable()
        {
            const string tables = "select name from sqlite_master where type = 'table' and name in ('module_class', 'module_effect') order by name";

            InTemporaryDatabase<SqliteWorldContext>((context, migrator) =>
            {
                migrator.Migrate(WorldBefore);
                Assert.AreEqual(0, Rows(context, tables).Count);
                Assert.AreEqual(Migration, context.Database.GetPendingMigrations().First());

                migrator.Migrate(Migration);
                CollectionAssert.AreEqual(new[] { "module_class", "module_effect" }, Rows(context, tables));

                var repository = new ItemModuleRepository(context);
                var classes = repository.GetModuleClasses();
                var effects = repository.GetModuleEffects();

                string Class(ModuleClassEntry row) => $"{row.Id}|{row.VariantId}|{row.Level}|{row.ClassSetId}|{row.ItemTemplateId}|{row.ItemClassId}|{row.ExtractCost}|{row.IntegrateCost}|{row.SalvageGain}|{row.UpgradeCost}|{row.UpgradeModuleId}|{row.Comment}";
                string Effect(ModuleEffectEntry row) => $"{row.Id}|{row.ModuleId}|{row.EffectId}|{row.SetLevel}|{row.FlatValue:R}|{row.LinearValue:R}|{row.ExpValue:R}|{row.Arg1}|{row.Arg2}|{row.Arg3}|{row.Arg4}";

                CollectionAssert.AreEquivalent(ItemModuleSeed.Classes.Select(Class).ToList(), classes.Select(Class).ToList());
                CollectionAssert.AreEqual(ItemModuleSeed.Effects.Select(Effect).ToList(), effects.Select(Effect).ToList(), "in the order of their ids");

                // A set's name with a letter outside ASCII, and one with a quote in it.
                Assert.AreEqual("Set: La R\u00e9sistance", classes.Single(row => row.Id == 900370).Comment);
                Assert.AreEqual("Karem Zul's Vest", classes.Single(row => row.Id == 700000).Comment);

                // Every module that is an item is an item template of this world, of the class the row says.
                var templates = Rows(context, "select itemTemplateId, itemClassId from itemtemplate_itemclass").Select(row => row.Split('|')).ToDictionary(row => uint.Parse(row[0]), row => uint.Parse(row[1]));

                foreach (var row in classes.Where(row => row.ItemTemplateId != 0))
                    Assert.AreEqual(row.ItemClassId, templates[row.ItemTemplateId], $"module {row.Id}");

                // What the game server makes of them.
                ItemModules.Load(classes, effects);
                Assert.AreEqual(867, ItemModules.Count);
                Assert.AreEqual(290, ItemModules.All.Sum(module => module.Effects.Count));

                migrator.Migrate(WorldBefore);
                Assert.AreEqual(0, Rows(context, tables).Count);
            });
        }

        [TestMethod]
        public void AnItemsRowHasFourSlotsAndTheItemsThereWereStartWithThemEmpty()
        {
            const string columns = "select name from pragma_table_info('items') where name like 'module_%' order by name";
            const string row = "select item_id, item_template_id, stack_size, current_hp, color, ammo_count, bound_character_id, crafter_name from items order by item_id";

            InTemporaryDatabase<SqliteCharContext>((context, migrator) =>
            {
                migrator.Migrate(CharBefore);
                Assert.AreEqual(0, Rows(context, columns).Count);
                context.Database.ExecuteSqlRaw("insert into items (item_id, item_template_id, stack_size, current_hp, color, ammo_count, bound_character_id, crafter_name, created_at) values (7, 28, 1, 55, 2139062144, 12, 3, 'Atomsk', '2026-01-01 00:00:00')");

                var before = Rows(context, row);

                migrator.Migrate(Migration);
                CollectionAssert.AreEqual(new[] { "module_1", "module_2", "module_3", "module_4" }, Rows(context, columns));
                CollectionAssert.AreEqual(before, Rows(context, row), "the item is as it was");

                var repository = new ItemRepository(context);

                CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, repository.GetItem(7).Modules);

                // The four slots are written together, and nothing else of the row with them.
                var item = new Item { Id = 7 };

                item.SetModule(0, Body1);
                item.SetModule(2, FireResist1);
                repository.UpdateModules(item);
                context.ChangeTracker.Clear();

                CollectionAssert.AreEqual(new uint[] { Body1, 0, FireResist1, 0 }, repository.GetItem(7).Modules);
                CollectionAssert.AreEqual(before, Rows(context, row));

                item.SetModule(0, 0);
                item.SetModule(3, StealArmor1);
                repository.UpdateModules(item);
                context.ChangeTracker.Clear();
                CollectionAssert.AreEqual(new uint[] { 0, 0, FireResist1, StealArmor1 }, repository.GetItem(7).Modules);

                // An item made with modules is saved with them, and one made without with none.
                var made = new Item { ItemTemplateId = 28, StackSize = 1, Crafter = "" };

                made.SetModule(1, Health1);
                made.Id = repository.CreateItem(made);
                context.ChangeTracker.Clear();
                CollectionAssert.AreEqual(new uint[] { 0, Health1, 0, 0 }, repository.GetItem(made.Id).Modules);

                var plain = repository.CreateItem(new Item { ItemTemplateId = 28, StackSize = 1, Crafter = "" });

                context.ChangeTracker.Clear();
                CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, repository.GetItem(plain).Modules);

                // Taken back, the columns are gone and the items are still there.
                migrator.Migrate(CharBefore);
                Assert.AreEqual(0, Rows(context, columns).Count);
                Assert.AreEqual(before[0], Rows(context, row)[0]);
                Assert.AreEqual(3, Rows(context, row).Count);
            });
        }

        [TestMethod]
        public void AGameMasterPutsModulesOnAnItemAndTheyAreKeptWithIt()
        {
            const uint rifle = 990311;
            const uint mimeogel = 990312;

            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var factoryBefore = Rasa.Game.Server.GameUnitOfWorkFactory;

            Rasa.Game.Server.GameUnitOfWorkFactory = harness.Context;

            try
            {
                harness.Client.AccountEntry.Level = (byte)GmLevel.Admin;

                var commands = new ChatCommandsManager(null);

                commands.RegisterChatCommands();

                List<string> Say(string command)
                {
                    harness.Drain();
                    commands.ProcessCommand(harness.Client, command);

                    return harness.Drain().OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
                }

                // An item of the player's: a template for it - a level 20 weapon, or something
                // that stacks - a row, a place in the pack, and the entity for them.
                void Give(uint templateId, uint classId, uint packSlot, bool weapon)
                {
                    harness.Context.AddRewardTemplate(templateId, classId);

                    var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
                    var template = classInfo.ItemTemplates[templateId];

                    classInfo.ItemClassInfo.StackSize = weapon ? 1u : 50u;
                    classInfo.ItemClassInfo.MaxHitPoints = 100;
                    classInfo.EquipableClassInfo = weapon ? new EquipableClassInfo(EquipmentData.Weapon) : null;
                    template.InventoryCategory = weapon ? InventoryCategory.Equipment : InventoryCategory.Crafting;

                    if (weapon)
                        template.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = 20;

                    var item = ItemManager.StageItem(template, 1, "");
                    var owner = harness.Client;

                    item.OwnerId = owner.Player.Id;
                    item.OwnerSlotId = packSlot;

                    using (var unit = harness.Context.CreateChar())
                    {
                        item.Id = unit.Items.CreateItem(item);
                        unit.CharacterInventories.AddInvItem(owner.AccountEntry.Id, owner.Player.Id, (uint)InventoryType.Personal, packSlot, item.Id);
                    }

                    EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                    EntityManager.Instance.RegisterItem(item.EntityId, item);
                    owner.Player.Inventory.PersonalInventory[(int)packSlot] = item.EntityId;
                }

                (Item Item, int PackSlot) Held(uint templateId)
                {
                    var pack = harness.Client.Player.Inventory.PersonalInventory;

                    for (var slot = 0; slot < pack.Count; slot++)
                    {
                        var item = pack[slot] != 0 ? EntityManager.Instance.GetItem(pack[slot]) : null;

                        if (item != null && item.ItemTemplate.ItemTemplateId == templateId)
                            return (item, slot + 1);
                    }

                    Assert.Fail($"no item of template {templateId} in the pack");
                    return default;
                }

                uint[] Saved(Item item)
                {
                    using var unit = harness.Context.CreateChar();

                    return unit.Items.GetItem(item.Id).Modules;
                }

                Give(rifle, 990301, 40, true);
                Give(mimeogel, 990302, 140, false);

                var (weapon, slot) = Held(rifle);

                Assert.IsNotNull(EntityClassManager.Instance.GetClassInfo(weapon.ItemTemplate.Class).EquipableClassInfo, "the test's rifle is one");
                Assert.IsTrue(Say($".module {slot}").Single().EndsWith("no modules"));

                // Into the first empty slot, saved, and the item's ItemInfo sent again.
                harness.Drain();
                commands.ProcessCommand(harness.Client, $".module {slot} add {Body1}");

                var sent = harness.Drain();

                Assert.AreSame(weapon, sent.OfType<ItemInfoPacket>().Single().Item);
                CollectionAssert.AreEqual(new uint[] { Body1, 0, 0, 0 }, weapon.ModuleIds.ToArray(), "the item");
                CollectionAssert.AreEqual(new uint[] { Body1, 0, 0, 0 }, Saved(weapon), "its row");

                var shown = sent.OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();

                Assert.AreEqual(20, ItemModules.LevelOf(weapon));
                Assert.AreEqual(2, shown.Count);
                Assert.AreEqual($"{slot}: item template {rifle}, item level 20", shown[0]);
                Assert.AreEqual($"  slot 1: {Body1} Armor Module: Body Bonus [1] - effect 330: +3 at item level 20", shown[1]);

                // Into a slot named; not into a full one; not a module there is not.
                Say($".module {slot} add {FireDebuff1} 3");
                CollectionAssert.AreEqual(new uint[] { Body1, 0, FireDebuff1, 0 }, Saved(weapon), "a slot named");

                Assert.IsTrue(Say($".module {slot} add {Health1} 3").Single().Contains($"Slot 3 holds module {FireDebuff1}"));
                Assert.IsTrue(Say($".module {slot} add 5").Single().StartsWith("There is no module 5"));
                Assert.IsTrue(Say($".module {slot} add {Health1} 5").Single().StartsWith("usage:"));
                CollectionAssert.AreEqual(new uint[] { Body1, 0, FireDebuff1, 0 }, Saved(weapon), "refusals change nothing");

                Assert.IsTrue(Say($".module {slot}").Last().EndsWith("Debuff Fire Resist [1] - no effect known"));

                // Taken out of a slot.
                Assert.IsTrue(Say($".module {slot} remove 2").Single().Contains("Slot 2 is empty"));
                Say($".module {slot} remove 1");
                CollectionAssert.AreEqual(new uint[] { 0, 0, FireDebuff1, 0 }, weapon.ModuleIds.ToArray(), "the item, a slot emptied");
                CollectionAssert.AreEqual(new uint[] { 0, 0, FireDebuff1, 0 }, Saved(weapon), "its row, a slot emptied");

                // What stacks takes no module, and neither does a slot with nothing in it.
                var (gel, gelSlot) = Held(mimeogel);

                Assert.IsTrue(Say($".module {gelSlot} add {Body1}").Single().Contains("takes no modules"));
                CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, gel.ModuleIds.ToArray(), "what stacks");
                Assert.IsTrue(Say(".module 250").Single().StartsWith("You have no item there"));

                // The search is by what the client calls a module, every word of it.
                var found = Say(".module find body bonus armor");

                Assert.AreEqual(6, found.Count);
                Assert.AreEqual($"{Body1} Armor Module: Body Bonus [1]", found[0]);
                Assert.AreEqual("5 modules match.", found[5]);
                Assert.IsTrue(Say(".module find debuff fire [1]").First().EndsWith("no effect known"));

                // The item's row is the item: a new login reads the slots back.
                harness.ReconnectFromSelection();

                var (again, _) = Held(rifle);

                Assert.AreNotSame(weapon, again);
                Assert.AreEqual(weapon.Id, again.Id);
                CollectionAssert.AreEqual(new uint[] { 0, 0, FireDebuff1, 0 }, again.ModuleIds.ToArray(), "read back at login");

                harness.Client.AccountEntry.Level = (byte)GmLevel.Admin;
                Say($".module {slot} add {Health1} 1");
                CollectionAssert.AreEqual(new uint[] { Health1, 0, FireDebuff1, 0 }, Saved(again), "its row, after the login");
                Say($".module {slot} remove all");
                CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, again.ModuleIds.ToArray(), "the item, emptied");
                CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, Saved(again), "its row, emptied");

                // And a player is not a game master.
                harness.Client.AccountEntry.Level = (byte)GmLevel.Player;
                Say($".module {slot} add {Body1}");
                CollectionAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, Saved(again), "a player changes nothing");
            }
            finally
            {
                Rasa.Game.Server.GameUnitOfWorkFactory = factoryBefore;
            }
        }
    }
}
