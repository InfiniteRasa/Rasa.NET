using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Crafting.Client;
    using Rasa.Packets.Crafting.Server;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    // The crafting station's module pages (KraftwerksManager.Modules) - salvage, extraction,
    // integration, upgrade - and the client's rules they go by (ItemModules).
    [TestClass]
    [DoNotParallelize]
    public class ModuleCraftingTests
    {
        private const string WorldBefore = "20261120000000_Add_item_modules";
        private const string Migration = "20261121000000_Add_module_crafting";

        private const uint Mimeogel = ItemModules.MimeogelTemplateId;
        private const uint MimeogelClass = ItemModules.MimeogelClassId;
        private const uint ModuleItemClass = 29879;

        private const uint Body1 = 100060;          // Armor Module: Body Bonus [1], item template 122963
        private const uint Body2 = 100061;          // [2], item template 122964
        private const uint Body5 = 900032;          // [5], item template 122967
        private const uint Mind5 = 900036;          // Armor Module: Mind Bonus [5]
        private const uint Spirit1 = 100076;
        private const uint Health3 = 100002;
        private const uint Crit1 = 100056;          // Weapon Module: Crit Hit Bonus [1]
        private const uint Crit5 = 900051;
        private const uint StealArmor1 = 900260;

        private const uint Body1Item = 122963;
        private const uint Body2Item = 122964;
        private const uint Body5Item = 122967;

        // One class of each set, and a piece of salvage with the module it is of.
        private const uint ArmorClass = 6470;
        private const uint WeaponClass = 6048;
        private const uint ToolClass = 7614;
        private const uint Salvage = 41637;
        private const uint SalvageModule = 900776;

        // What the client's own shared/craftingnew.py makes of the client's own data, run as it
        // is: GetModuleIntegrationMimeogelCost and GetEquipmentTotalSalvageValue for a piece of
        // armor at each of these level requirements, each quality 1 (Mission) to 7 (Junk), and
        // each of four sets of modules already in it - then, for the cost, a Body Bonus module of
        // each strength 1 to 5.
        private static readonly int[] Levels = { 0, 1, 4, 5, 9, 10, 14, 23, 37, 49, 50 };

        private static readonly uint[][] PresentForCost =
        {
            new uint[0],
            new uint[] { Mind5 },
            new uint[] { Mind5, 0, Spirit1 },
            new uint[] { Mind5, Spirit1, Health3 }
        };

        private static readonly uint[][] PresentForSalvage =
        {
            new uint[0],
            new uint[] { Body1 },
            new uint[] { Body1, 0, Body5 },
            new uint[] { Mind5, Spirit1, Health3, StealArmor1 }
        };

        private static readonly uint[] ClientCosts =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 2,
            3, 4, 1, 3, 4, 6, 8, 2, 5, 8, 11, 14, 0, 0, 1, 1, 2, 1, 3, 4, 6, 8, 3, 6, 9, 12, 16, 5,
            11, 16, 22, 28, 0, 1, 1, 2, 3, 2, 4, 7, 9, 12, 4, 9, 14, 19, 24, 8, 16, 25, 33, 42, 0, 1, 2, 3,
            4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56, 1, 2, 3, 4, 5, 4, 8, 12, 16, 20, 8, 16,
            24, 32, 40, 14, 28, 42, 56, 70, 0, 0, 0, 0, 1, 0, 1, 2, 3, 4, 1, 3, 4, 6, 8, 2, 5, 8, 11, 14,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 2,
            3, 4, 1, 3, 4, 6, 8, 2, 5, 8, 11, 14, 0, 0, 1, 1, 2, 1, 3, 4, 6, 8, 3, 6, 9, 12, 16, 5,
            11, 16, 22, 28, 0, 1, 1, 2, 3, 2, 4, 7, 9, 12, 4, 9, 14, 19, 24, 8, 16, 25, 33, 42, 0, 1, 2, 3,
            4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56, 1, 2, 3, 4, 5, 4, 8, 12, 16, 20, 8, 16,
            24, 32, 40, 14, 28, 42, 56, 70, 0, 0, 0, 0, 1, 0, 1, 2, 3, 4, 1, 3, 4, 6, 8, 2, 5, 8, 11, 14,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 2,
            3, 4, 1, 3, 4, 6, 8, 2, 5, 8, 11, 14, 0, 0, 1, 1, 2, 1, 3, 4, 6, 8, 3, 6, 9, 12, 16, 5,
            11, 16, 22, 28, 0, 1, 1, 2, 3, 2, 4, 7, 9, 12, 4, 9, 14, 19, 24, 8, 16, 25, 33, 42, 0, 1, 2, 3,
            4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56, 1, 2, 3, 4, 5, 4, 8, 12, 16, 20, 8, 16,
            24, 32, 40, 14, 28, 42, 56, 70, 0, 0, 0, 0, 1, 0, 1, 2, 3, 4, 1, 3, 4, 6, 8, 2, 5, 8, 11, 14,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 1, 3, 4,
            6, 8, 3, 6, 9, 12, 16, 5, 11, 16, 22, 28, 0, 1, 2, 3, 4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11,
            22, 33, 44, 56, 1, 2, 3, 4, 6, 4, 9, 14, 19, 24, 9, 19, 28, 38, 48, 16, 33, 50, 67, 84, 1, 3, 4, 6,
            8, 6, 12, 19, 25, 32, 12, 25, 38, 51, 64, 22, 44, 67, 89, 112, 2, 4, 6, 8, 10, 8, 16, 24, 32, 40, 16, 32,
            48, 64, 80, 28, 56, 84, 112, 140, 0, 0, 1, 1, 2, 1, 3, 4, 6, 8, 3, 6, 9, 12, 16, 5, 11, 16, 22, 28,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 1, 3, 4,
            6, 8, 3, 6, 9, 12, 16, 5, 11, 16, 22, 28, 0, 1, 2, 3, 4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11,
            22, 33, 44, 56, 1, 2, 3, 4, 6, 4, 9, 14, 19, 24, 9, 19, 28, 38, 48, 16, 33, 50, 67, 84, 1, 3, 4, 6,
            8, 6, 12, 19, 25, 32, 12, 25, 38, 51, 64, 22, 44, 67, 89, 112, 2, 4, 6, 8, 10, 8, 16, 24, 32, 40, 16, 32,
            48, 64, 80, 28, 56, 84, 112, 140, 0, 0, 1, 1, 2, 1, 3, 4, 6, 8, 3, 6, 9, 12, 16, 5, 11, 16, 22, 28,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 3, 6, 9,
            12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56, 1, 3, 4, 6, 8, 6, 12, 19, 25, 32, 12, 25, 38, 51, 64, 22,
            44, 67, 89, 112, 2, 4, 7, 9, 12, 9, 19, 28, 38, 48, 19, 38, 57, 76, 96, 33, 67, 100, 134, 168, 3, 6, 9, 12,
            16, 12, 25, 38, 51, 64, 25, 51, 76, 102, 128, 44, 89, 134, 179, 224, 4, 8, 12, 16, 20, 16, 32, 48, 64, 80, 32, 64,
            96, 128, 160, 56, 112, 168, 224, 280, 0, 1, 2, 3, 4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 3, 6, 9,
            12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56, 1, 3, 4, 6, 8, 6, 12, 19, 25, 32, 12, 25, 38, 51, 64, 22,
            44, 67, 89, 112, 2, 4, 7, 9, 12, 9, 19, 28, 38, 48, 19, 38, 57, 76, 96, 33, 67, 100, 134, 168, 3, 6, 9, 12,
            16, 12, 25, 38, 51, 64, 25, 51, 76, 102, 128, 44, 89, 134, 179, 224, 4, 8, 12, 16, 20, 16, 32, 48, 64, 80, 32, 64,
            96, 128, 160, 56, 112, 168, 224, 280, 0, 1, 2, 3, 4, 3, 6, 9, 12, 16, 6, 12, 19, 25, 32, 11, 22, 33, 44, 56,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 6, 9, 12, 16, 12, 25, 38,
            51, 64, 25, 51, 76, 102, 128, 44, 89, 134, 179, 224, 6, 12, 19, 25, 32, 25, 51, 76, 102, 128, 51, 102, 153, 204, 256, 89,
            179, 268, 358, 448, 9, 19, 28, 38, 48, 38, 76, 115, 153, 192, 76, 153, 230, 307, 384, 134, 268, 403, 537, 672, 12, 25, 38, 51,
            64, 51, 102, 153, 204, 256, 102, 204, 307, 409, 512, 179, 358, 537, 716, 896, 16, 32, 48, 64, 80, 64, 128, 192, 256, 320, 128, 256,
            384, 512, 640, 224, 448, 672, 896, 1120, 3, 6, 9, 12, 16, 12, 25, 38, 51, 64, 25, 51, 76, 102, 128, 44, 89, 134, 179, 224,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25, 50, 75, 100, 125, 100, 200, 300,
            400, 500, 200, 400, 600, 800, 1000, 350, 700, 1050, 1400, 1750, 50, 100, 150, 200, 250, 200, 400, 600, 800, 1000, 400, 800, 1200, 1600, 2000, 700,
            1400, 2100, 2800, 3500, 75, 150, 225, 300, 375, 300, 600, 900, 1200, 1500, 600, 1200, 1800, 2400, 3000, 1050, 2100, 3150, 4200, 5250, 100, 200, 300, 400,
            500, 400, 800, 1200, 1600, 2000, 800, 1600, 2400, 3200, 4000, 1400, 2800, 4200, 5600, 7000, 125, 250, 375, 500, 625, 500, 1000, 1500, 2000, 2500, 1000, 2000,
            3000, 4000, 5000, 1750, 3500, 5250, 7000, 8750, 25, 50, 75, 100, 125, 100, 200, 300, 400, 500, 200, 400, 600, 800, 1000, 350, 700, 1050, 1400, 1750,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 100, 200, 300, 400, 500, 400, 800, 1200,
            1600, 2000, 800, 1600, 2400, 3200, 4000, 1400, 2800, 4200, 5600, 7000, 200, 400, 600, 800, 1000, 800, 1600, 2400, 3200, 4000, 1600, 3200, 4800, 6400, 8000, 2800,
            5600, 8400, 11200, 14000, 300, 600, 900, 1200, 1500, 1200, 2400, 3600, 4800, 6000, 2400, 4800, 7200, 9600, 12000, 4200, 8400, 12600, 16800, 21000, 400, 800, 1200, 1600,
            2000, 1600, 3200, 4800, 6400, 8000, 3200, 6400, 9600, 12800, 16000, 5600, 11200, 16800, 22400, 28000, 500, 1000, 1500, 2000, 2500, 2000, 4000, 6000, 8000, 10000, 4000, 8000,
            12000, 16000, 20000, 7000, 14000, 21000, 28000, 35000, 100, 200, 300, 400, 500, 400, 800, 1200, 1600, 2000, 800, 1600, 2400, 3200, 4000, 1400, 2800, 4200, 5600, 7000,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 200, 400, 600, 800, 1000, 800, 1600, 2400,
            3200, 4000, 1600, 3200, 4800, 6400, 8000, 2800, 5600, 8400, 11200, 14000, 400, 800, 1200, 1600, 2000, 1600, 3200, 4800, 6400, 8000, 3200, 6400, 9600, 12800, 16000, 5600,
            11200, 16800, 22400, 28000, 600, 1200, 1800, 2400, 3000, 2400, 4800, 7200, 9600, 12000, 4800, 9600, 14400, 19200, 24000, 8400, 16800, 25200, 33600, 42000, 800, 1600, 2400, 3200,
            4000, 3200, 6400, 9600, 12800, 16000, 6400, 12800, 19200, 25600, 32000, 11200, 22400, 33600, 44800, 56000, 1000, 2000, 3000, 4000, 5000, 4000, 8000, 12000, 16000, 20000, 8000, 16000,
            24000, 32000, 40000, 14000, 28000, 42000, 56000, 70000, 200, 400, 600, 800, 1000, 800, 1600, 2400, 3200, 4000, 1600, 3200, 4800, 6400, 8000, 2800, 5600, 8400, 11200, 14000
        };

        private static readonly uint[] ClientSalvage =
        {
            0, 2, 12, 20, 0, 2, 12, 20, 5, 7, 17, 25, 10, 12, 22, 30, 25, 27, 37, 45, 100, 102, 112, 120, 0, 2, 12, 20,
            0, 2, 12, 20, 0, 2, 12, 20, 5, 7, 17, 25, 10, 12, 22, 30, 25, 27, 37, 45, 100, 102, 112, 120, 0, 2, 12, 20,
            0, 2, 12, 20, 0, 2, 12, 20, 5, 7, 17, 25, 10, 12, 22, 30, 25, 27, 37, 45, 100, 102, 112, 120, 0, 2, 12, 20,
            0, 2, 12, 20, 0, 2, 12, 20, 5, 7, 17, 25, 10, 12, 22, 30, 25, 27, 37, 45, 100, 102, 112, 120, 0, 2, 12, 20,
            0, 2, 12, 20, 0, 2, 12, 20, 5, 7, 17, 25, 10, 12, 22, 30, 25, 27, 37, 45, 100, 102, 112, 120, 0, 2, 12, 20,
            0, 4, 24, 40, 0, 4, 24, 40, 10, 14, 34, 50, 20, 24, 44, 60, 50, 54, 74, 90, 200, 204, 224, 240, 0, 4, 24, 40,
            0, 4, 24, 40, 0, 4, 24, 40, 10, 14, 34, 50, 20, 24, 44, 60, 50, 54, 74, 90, 200, 204, 224, 240, 0, 4, 24, 40,
            0, 8, 48, 80, 0, 8, 48, 80, 20, 28, 68, 100, 40, 48, 88, 120, 100, 108, 148, 180, 400, 408, 448, 480, 0, 8, 48, 80,
            0, 14, 84, 140, 0, 14, 84, 140, 35, 49, 119, 175, 70, 84, 154, 210, 175, 189, 259, 315, 700, 714, 784, 840, 0, 14, 84, 140,
            0, 18, 108, 180, 0, 18, 108, 180, 45, 63, 153, 225, 90, 108, 198, 270, 225, 243, 333, 405, 900, 918, 1008, 1080, 0, 18, 108, 180,
            0, 20, 120, 200, 0, 20, 120, 200, 50, 70, 170, 250, 100, 120, 220, 300, 250, 270, 370, 450, 1000, 1020, 1120, 1200, 0, 20, 120, 200
        };

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Rasa.Logger.Config == null)
                Rasa.Logger.UpdateConfig(new Rasa.Logger.LoggerConfig());
        }

        [TestInitialize]
        public void LoadTheSeed()
        {
            ItemModules.Load(ItemModuleSeed.Classes, ItemModuleSeed.Effects);
            ItemModules.LoadCrafting(
                ModuleCraftingSeed.Items.Select(row => new ModuleItemEntry { Id = row.ItemTemplateId, ModuleId = row.ModuleId, Strength = row.Strength }),
                ModuleCraftingSeed.ModifiableClasses.Select(row => new ModifiableClassEntry { Id = row.ClassId, ClassSetId = row.ClassSetId }));
        }

        /// <summary>An item for the rules to look at: a template of that class, quality and level requirement, and the modules in it.</summary>
        private static Item Thing(uint templateId, uint classId, LootQuality quality = LootQuality.Normal, int level = 0, uint stack = 1, params uint[] modules)
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = templateId, ItemClass = classId }) { QualityId = (int)quality };

            if (level > 0)
                template.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = level;

            var item = new Item { ItemTemplate = template, ItemTemplateId = templateId, StackSize = stack };

            for (var slot = 0; slot < modules.Length; slot++)
                item.SetModule(slot, modules[slot]);

            return item;
        }

        private static Item Armor(LootQuality quality, int level, params uint[] modules) => Thing(990402, ArmorClass, quality, level, 1, modules);

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

        [TestMethod]
        public void TheSeedIsTheClientsModuleItemsAndTheClassesThatTakeModules()
        {
            var items = ModuleCraftingSeed.Items;
            var modules = ItemModuleSeed.Classes.ToDictionary(row => row.Id);

            Assert.AreEqual(5380, items.Count);
            Assert.AreEqual(5380, items.Select(row => row.ItemTemplateId).Distinct().Count());
            Assert.IsTrue(items.All(row => modules.ContainsKey(row.ModuleId) && row.Strength >= 1 && row.Strength <= 5));

            // The item a module names as its own is an item of that module, of the module's strength.
            var byTemplate = items.ToDictionary(row => row.ItemTemplateId);

            foreach (var module in modules.Values.Where(module => module.ItemTemplateId != 0))
                Assert.AreEqual((module.Id, module.Level), (byTemplate[module.ItemTemplateId].ModuleId, byTemplate[module.ItemTemplateId].Strength), $"module {module.Id}");

            // 330 are the modules a station puts in, one item each; the rest are salvage.
            Assert.AreEqual(330, items.Count(row => modules[row.ModuleId].VariantId != 0));
            Assert.AreEqual(5050, items.Count(row => modules[row.ModuleId].ClassSetId == 1274));
            Assert.IsTrue(items.Where(row => modules[row.ModuleId].ClassSetId == 1274).All(row => modules[row.ModuleId].SalvageGain > 0));

            // No item template is also a module's id: the client's integration cost looks one up
            // as the other, and what it charges depends on never finding it.
            Assert.IsFalse(items.Any(row => modules.ContainsKey(row.ItemTemplateId)));

            // The classes that take modules: three sets, no class in two.
            Assert.AreEqual(2541, ModuleCraftingSeed.Armor.Count);
            Assert.AreEqual(2256, ModuleCraftingSeed.Weapons.Count);
            Assert.AreEqual(160, ModuleCraftingSeed.Tools.Count);

            var modifiable = ModuleCraftingSeed.ModifiableClasses.ToList();

            Assert.AreEqual(4957, modifiable.Count);
            Assert.AreEqual(4957, modifiable.Select(row => row.ClassId).Distinct().Count());

            // Every module a station puts in is for one of them, and no module item is itself one.
            Assert.IsTrue(modules.Values.Where(module => module.VariantId != 0 && module.ItemTemplateId != 0)
                .All(module => module.ClassSetId == ModuleCraftingSeed.ArmorClassSet || module.ClassSetId == ModuleCraftingSeed.WeaponClassSet || module.ClassSetId == ModuleCraftingSeed.ToolClassSet));
            Assert.IsFalse(modifiable.Any(row => row.ClassId == ModuleItemClass || row.ClassId == MimeogelClass));
        }

        [TestMethod]
        public void AMigratedWorldHasTheCraftingTablesAndOneTakenBackHasNeither()
        {
            const string tables = "select name from sqlite_master where type = 'table' and name in ('module_item', 'modifiable_class') order by name";
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);

            try
            {
                using var context = (SqliteWorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                migrator.Migrate(WorldBefore);
                Assert.AreEqual(0, Rows(context, tables).Count);
                Assert.AreEqual(Migration, context.Database.GetPendingMigrations().First());

                migrator.Migrate(Migration);
                CollectionAssert.AreEqual(new[] { "modifiable_class", "module_item" }, Rows(context, tables));

                var repository = new ItemModuleRepository(context);
                var items = repository.GetModuleItems();
                var classes = repository.GetModifiableClasses();

                CollectionAssert.AreEquivalent(
                    ModuleCraftingSeed.Items.Select(row => (row.ItemTemplateId, row.ModuleId, row.Strength)).ToList(),
                    items.Select(row => (row.Id, row.ModuleId, row.Strength)).ToList());
                CollectionAssert.AreEquivalent(
                    ModuleCraftingSeed.ModifiableClasses.ToList(),
                    classes.Select(row => (row.Id, row.ClassSetId)).ToList());

                // Every module item is an item template of this world, and Mimeogel is what the client says it is.
                var templates = Rows(context, "select itemTemplateId, itemClassId from itemtemplate_itemclass").Select(row => row.Split('|')).ToDictionary(row => uint.Parse(row[0]), row => uint.Parse(row[1]));

                Assert.IsTrue(items.All(row => templates.ContainsKey(row.Id)));
                Assert.AreEqual(MimeogelClass, templates[Mimeogel]);
                Assert.AreEqual(ModuleItemClass, templates[Body1Item]);

                // All but a handful of the classes are item classes here; the rest are classes the client has and this world does not.
                var itemClasses = Rows(context, "select id from itemclass").Select(uint.Parse).ToHashSet();

                Assert.AreEqual(7, classes.Count(row => !itemClasses.Contains(row.Id)));
                Assert.IsTrue(itemClasses.Contains(ArmorClass) && itemClasses.Contains(WeaponClass) && itemClasses.Contains(ToolClass));

                // What the game server makes of them.
                ItemModules.LoadCrafting(items, classes);
                Assert.AreEqual(ModuleCraftingSeed.ToolClassSet, ItemModules.ClassSetOf(Thing(1, ToolClass)));
                Assert.IsTrue(ItemModules.TryGetModuleItem(Thing(Body5Item, ModuleItemClass), out var module, out var strength));
                Assert.AreEqual((Body5, 5u), (module.ModuleId, strength));

                migrator.Migrate(WorldBefore);
                Assert.AreEqual(0, Rows(context, tables).Count);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void AnItemIsSalvagedForWhatTheClientSaysItIs()
        {
            var next = 0;

            foreach (var level in Levels)
                for (var quality = 1; quality <= 7; quality++)
                    foreach (var present in PresentForSalvage)
                        Assert.AreEqual(ClientSalvage[next++], ItemModules.SalvageValue(Armor((LootQuality)quality, level, present)),
                            $"level {level}, quality {quality}, modules {string.Join(",", present)}");

            Assert.AreEqual(ClientSalvage.Length, next);

            // Read out: a Rare piece is worth 10 and its two modules 2 and 10, four times over at level 23.
            Assert.AreEqual(88u, ItemModules.SalvageValue(Armor(LootQuality.Rare, 23, Body1, 0, Body5)));
            Assert.AreEqual(0u, ItemModules.SalvageValue(Armor(LootQuality.Normal, 50)), "a Normal item with no module is worth nothing");
            Assert.AreEqual(100u, ItemModules.SalvageValue(Armor(LootQuality.Legendary, 4)), "below level 5 counts once");

            // A weapon and a tool are salvaged as armor is.
            Assert.AreEqual(50u, ItemModules.SalvageValue(Thing(990401, WeaponClass, LootQuality.Epic, 10)));
            Assert.AreEqual(50u, ItemModules.SalvageValue(Thing(990403, ToolClass, LootQuality.Epic, 10)));

            // A module, and a piece of salvage, for the module's own value - each of the stack.
            Assert.AreEqual(2u, ItemModules.SalvageValue(Thing(Body1Item, ModuleItemClass)));
            Assert.AreEqual(14u, ItemModules.SalvageValue(Thing(Body1Item, ModuleItemClass, stack: 7)));
            Assert.AreEqual(10u, ItemModules.SalvageValue(Thing(Body5Item, ModuleItemClass)));
            Assert.IsTrue(ItemModules.TryGet(SalvageModule, out var salvage));
            Assert.AreEqual(50u, salvage.SalvageGain);
            Assert.AreEqual(150u, ItemModules.SalvageValue(Thing(Salvage, 25304, stack: 3)));

            // Anything else for nothing: Mimeogel itself, an item of no class that takes modules.
            Assert.AreEqual(0u, ItemModules.SalvageValue(Thing(Mimeogel, MimeogelClass, stack: 10)));
            Assert.AreEqual(0u, ItemModules.SalvageValue(Thing(28, 990301, LootQuality.Epic, 50)));
            Assert.AreEqual(0u, ItemModules.SalvageValue(null));
        }

        [TestMethod]
        public void AModuleIsPutInForWhatTheClientSaysItCosts()
        {
            var bodies = new[] { Body1Item, Body2Item, 122965u, 122966u, Body5Item };
            var next = 0;

            foreach (var level in Levels)
                for (var quality = 1; quality <= 7; quality++)
                    foreach (var present in PresentForCost)
                        for (var strength = 1; strength <= 5; strength++)
                            Assert.AreEqual(ClientCosts[next++], ItemModules.IntegrationCost(Thing(bodies[strength - 1], ModuleItemClass), Armor((LootQuality)quality, level, present)),
                                $"level {level}, quality {quality}, modules {string.Join(",", present)}, strength {strength}");

            Assert.AreEqual(ClientCosts.Length, next);

            // Read out. Rare is 3; one module in the item is 2^1 + 2; level 23 is int(1000 * 2^(4 - 10) + 0.5) = 16; strength 1 is a fifth.
            Assert.AreEqual(38u, ItemModules.IntegrationCost(Thing(Body1Item, ModuleItemClass), Armor(LootQuality.Rare, 23, 0, 0, Mind5)), "int(3 * 4 * 16 * 0.2)");
            Assert.AreEqual(0u, ItemModules.IntegrationCost(Thing(Body1Item, ModuleItemClass), Armor(LootQuality.Normal, 1)), "int(1 * 1 * 1 * 0.2): the weakest module in the lowest item is free");
            Assert.AreEqual(200u, ItemModules.IntegrationCost(Thing(Body1Item, ModuleItemClass), Armor(LootQuality.Normal, 50)));
            Assert.AreEqual(70000u, ItemModules.IntegrationCost(Thing(Body5Item, ModuleItemClass), Armor(LootQuality.Legendary, 50, Mind5, Spirit1, Health3)), "5 * (8 + 6) * 1000 * 1");
            Assert.AreEqual(0u, ItemModules.IntegrationCost(Thing(Body1Item, ModuleItemClass), Armor(LootQuality.Mission, 50)), "a mission's item is charged nothing");

            // What cannot be put in costs nothing.
            Assert.AreEqual(0u, ItemModules.IntegrationCost(Thing(Body5Item, ModuleItemClass), Armor(LootQuality.Epic, 50, Body1)));
        }

        [TestMethod]
        public void AModuleGoesIntoAnItemOfItsKindThatHasNoneOfItsKindAndAnEmptySlot()
        {
            var body = Thing(Body1Item, ModuleItemClass);
            var crit = Thing(123158, ModuleItemClass);

            Assert.IsTrue(ItemModules.TryGetModuleItem(crit, out var critModule, out _));
            Assert.AreEqual(Crit1, critModule.ModuleId);

            Assert.AreEqual(ItemModules.IntegrationProblem.None, ItemModules.CanIntegrate(body, Armor(LootQuality.Normal, 10)));
            Assert.AreEqual(ItemModules.IntegrationProblem.None, ItemModules.CanIntegrate(body, Armor(LootQuality.Normal, 10, Mind5, Spirit1, Health3)));
            Assert.AreEqual(ItemModules.IntegrationProblem.None, ItemModules.CanIntegrate(crit, Thing(990401, WeaponClass)));

            // An armor module and a weapon, a weapon module and a tool.
            Assert.AreEqual(ItemModules.IntegrationProblem.WrongKindOfItem, ItemModules.CanIntegrate(body, Thing(990401, WeaponClass)));
            Assert.AreEqual(ItemModules.IntegrationProblem.WrongKindOfItem, ItemModules.CanIntegrate(crit, Thing(990403, ToolClass)));
            Assert.AreEqual(ItemModules.IntegrationProblem.WrongKindOfItem, ItemModules.CanIntegrate(Thing(Salvage, 25304), Armor(LootQuality.Normal, 10)), "salvage is a module of no kind of item");

            // One of a kind: any strength of it.
            Assert.AreEqual(ItemModules.IntegrationProblem.SameKindPresent, ItemModules.CanIntegrate(body, Armor(LootQuality.Normal, 10, 0, Body5)));
            Assert.AreEqual(ItemModules.IntegrationProblem.SameKindPresent, ItemModules.CanIntegrate(Thing(Body5Item, ModuleItemClass), Armor(LootQuality.Normal, 10, Body1)));

            Assert.AreEqual(ItemModules.IntegrationProblem.NoEmptySlot, ItemModules.CanIntegrate(body, Armor(LootQuality.Normal, 10, Mind5, Spirit1, Health3, 100008)));
            Assert.AreEqual(ItemModules.IntegrationProblem.NotModifiable, ItemModules.CanIntegrate(body, Thing(28, 990301)));
            Assert.AreEqual(ItemModules.IntegrationProblem.NotModifiable, ItemModules.CanIntegrate(body, body), "a module takes no module");
            Assert.AreEqual(ItemModules.IntegrationProblem.NotAModule, ItemModules.CanIntegrate(Thing(Mimeogel, MimeogelClass), Armor(LootQuality.Normal, 10)));
            Assert.AreEqual(ItemModules.IntegrationProblem.NotAModule, ItemModules.CanIntegrate(Armor(LootQuality.Normal, 10), Armor(LootQuality.Normal, 10)));
        }

        [TestMethod]
        public void AModuleIsUpgradedToTheNextStrengthOfItself()
        {
            var modules = ItemModuleSeed.Classes.ToDictionary(row => row.Id);
            var upgradeable = 0;

            foreach (var row in ModuleCraftingSeed.Items)
            {
                var upgrade = ItemModules.UpgradeOf(Thing(row.ItemTemplateId, ModuleItemClass));
                var module = modules[row.ModuleId];

                if (module.VariantId == 0 || module.Level == 5)
                {
                    Assert.IsNull(upgrade, $"item template {row.ItemTemplateId}");
                    continue;
                }

                Assert.IsNotNull(upgrade, $"item template {row.ItemTemplateId}");
                Assert.AreEqual((module.VariantId, module.ClassSetId, module.Level + 1), (upgrade.VariantId, upgrade.ClassSetId, upgrade.Level));
                Assert.AreEqual(module.Level switch { 1 => 1u, 2 => 5u, 3 => 20u, _ => 100u }, module.UpgradeCost);
                Assert.AreEqual(module.Level + 2, ItemModules.QualityOfModuleItem(upgrade), "drawn one quality above its strength");
                upgradeable++;
            }

            Assert.AreEqual(264, upgradeable, "66 kinds, strengths 1 to 4");
            Assert.AreEqual(Body2, ItemModules.UpgradeOf(Thing(Body1Item, ModuleItemClass)).ModuleId);
            Assert.IsNull(ItemModules.UpgradeOf(Thing(Mimeogel, MimeogelClass)));
            Assert.IsNull(ItemModules.UpgradeOf(Armor(LootQuality.Epic, 10)));
        }

        /// <summary>A player at a crafting station, with the item templates the pages need.</summary>
        private sealed class Bench : IDisposable
        {
            private const uint WeaponTemplate = 990401;
            private const uint ArmorTemplate = 990402;
            private const uint OtherTemplate = 990404;

            private readonly BootcampRuntimeTestHarness.Harness _harness;
            private uint _nextSlot = 10;

            internal KraftwerksManager Station { get; }
            internal DynamicObject Object { get; }
            internal Client Client => _harness.Client;
            internal MissionTestContext Context => _harness.Context;

            internal Bench()
            {
                _harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
                var native = new ItemModuleRepository(_harness.WorldContext);
                ItemModules.Load(native.GetModuleClasses(), native.GetModuleEffects());
                ItemModules.LoadCrafting(native.GetModuleItems(), native.GetModifiableClasses());
                Station = new KraftwerksManager(_harness.Context);
                Object = new DynamicObject
                {
                    DynamicObjectType = DynamicObjectType.Kraftwerks,
                    Position = Client.Player.Position,
                    MapContextId = Client.Player.MapContextId
                };
                EntityManager.Instance.RegisterDynamicObject(Object);

                Template(Mimeogel, MimeogelClass, InventoryCategory.Crafting, 50000);
                Template(Body1Item, ModuleItemClass, InventoryCategory.Crafting, 5000);
                Template(Body2Item, ModuleItemClass, InventoryCategory.Crafting, 5000);
                Template(122965, ModuleItemClass, InventoryCategory.Crafting, 5000);    // Armor Module: Body Bonus [3]
                Template(Body5Item, ModuleItemClass, InventoryCategory.Crafting, 5000);
                Template(123158, ModuleItemClass, InventoryCategory.Crafting, 5000);    // Weapon Module: Crit Hit Bonus [1]
                _harness.Drain();
            }

            private ItemTemplate Template(uint templateId, uint classId, InventoryCategory category, uint stackSize)
            {
                _harness.Context.AddRewardTemplate(templateId, classId);

                var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
                var template = classInfo.ItemTemplates[templateId];

                classInfo.ItemClassInfo.StackSize = stackSize;
                classInfo.ItemClassInfo.MaxHitPoints = 100;
                template.InventoryCategory = category;
                template.QualityId = (int)LootQuality.Normal;

                return template;
            }

            /// <summary>An item in the player's pack: a row, a slot, and the entity for them.</summary>
            private Item Give(ItemTemplate template, uint stack, uint? slot = null)
            {
                var item = ItemManager.StageItem(template, stack, "");
                var packSlot = slot ?? _nextSlot++;

                item.OwnerId = Client.Player.Id;
                item.OwnerSlotId = packSlot;

                using (var unit = _harness.Context.CreateChar())
                {
                    item.Id = unit.Items.CreateItem(item);
                    unit.CharacterInventories.AddInvItem(Client.AccountEntry.Id, Client.Player.Id, (uint)InventoryType.Personal, packSlot, item.Id);
                }

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                Client.Player.Inventory.PersonalInventory[(int)packSlot] = item.EntityId;

                return item;
            }

            internal Item GiveStack(uint templateId, uint stack, uint? slot = null) =>
                Give(EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[templateId]].ItemTemplates[templateId], stack, slot ?? 100 + _nextSlot++);

            internal Item GiveGear(uint classId, LootQuality quality, int level, params uint[] modules)
            {
                var template = Template(classId == WeaponClass ? WeaponTemplate : classId == ArmorClass ? ArmorTemplate : OtherTemplate, classId, InventoryCategory.Equipment, 1);

                template.QualityId = (int)quality;
                template.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = level;

                var item = Give(template, 1);

                for (var slot = 0; slot < modules.Length; slot++)
                    item.SetModule(slot, modules[slot]);

                using (var unit = _harness.Context.CreateChar())
                    unit.Items.UpdateModules(item);

                return item;
            }

            internal uint Carried(uint classId) => InventoryManager.Instance.CountItemsByClass(Client, (EntityClasses)classId);

            internal bool InPack(Item item) => Client.Player.Inventory.PersonalInventory.Contains(item.EntityId);

            internal uint[] Saved(Item item)
            {
                using var unit = _harness.Context.CreateChar();

                return unit.Items.GetItem(item.Id).Modules;
            }

            /// <summary>The name the item's row has for its tooltip's "Modified By".</summary>
            internal string SavedCrafter(Item item)
            {
                using var unit = _harness.Context.CreateChar();

                return unit.Items.GetItem(item.Id).CrafterName;
            }

            internal void SetCrafter(Item item, string name)
            {
                item.Crafter = name;
                using var unit = Context.CreateChar();
                unit.Items.UpdateCrafter(item);
            }

            /// <summary>The one item of the template in the pack.</summary>
            internal Item Only(uint templateId) => Client.Player.Inventory.PersonalInventory
                .Where(entityId => entityId != 0)
                .Select(entityId => EntityManager.Instance.GetItem(entityId))
                .Single(item => item.ItemTemplate.ItemTemplateId == templateId);

            /// <summary>What a request sent the client.</summary>
            internal IReadOnlyList<PythonPacket> After(Action<KraftwerksManager, ulong> request)
            {
                _harness.Drain();
                request(Station, Object.EntityId);

                return _harness.Drain();
            }

            internal IReadOnlyList<PythonPacket> Route(ClientPythonPacket packet)
            {
                var singleton = typeof(KraftwerksManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                var previous = singleton.GetValue(null);
                singleton.SetValue(null, Station);
                _harness.Drain();
                try
                {
                    var handler = new ClientPacketHandler();
                    handler.RegisterClient(Client);
                    new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(handler, packet);
                    return _harness.Drain();
                }
                finally
                {
                    singleton.SetValue(null, previous);
                }
            }

            internal string[] DurableSnapshot()
            {
                using var context = Context.Open();
                return Rows(context, "select item_id, item_template_id, stack_size, module_1, module_2, module_3, module_4, crafter_name from items order by item_id")
                    .Concat(Rows(context, "select * from character_inventory order by item_id")).ToArray();
            }

            internal string RuntimeSnapshot() => string.Join("|", Client.Player.Inventory.PersonalInventory) + "\n" +
                string.Join("\n", Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                    .Select(id => EntityManager.Instance.GetItem(id))
                    .Select(item => $"{item.Id}:{item.StackSize}:{item.OwnerId}:{item.OwnerSlotId}:{string.Join(",", item.ModuleIds)}:{item.Crafter}")) +
                $"\nmodulesChanged={Client.Player.ModulesChanged}\n" +
                string.Join("\n", Client.Player.Attributes.OrderBy(entry => entry.Key)
                    .Select(entry => $"{entry.Key}:{entry.Value.Current}:{entry.Value.CurrentMax}"));

            internal IReadOnlyList<PythonPacket> Drain() => _harness.Drain();

            internal List<CraftingJob> JobBag()
            {
                var field = typeof(KraftwerksManager).GetField("_jobs", BindingFlags.NonPublic | BindingFlags.Instance);
                var bags = (Dictionary<(ulong, uint), List<CraftingJob>>)field.GetValue(Station);
                var key = (Object.EntityId, Client.Player.Id);
                if (!bags.TryGetValue(key, out var jobs))
                    bags[key] = jobs = new List<CraftingJob>();
                return jobs;
            }

            internal void FillCraftingBag()
            {
                for (uint slot = 100; slot < 150; slot++)
                    if (Client.Player.Inventory.PersonalInventory[(int)slot] == 0)
                        GiveStack(Body5Item, 5000, slot);
            }

            public void Dispose()
            {
                EntityManager.Instance.DynamicObjects.Remove(Object.EntityId);
                _harness.Dispose();
            }
        }

        private static IReadOnlyList<CraftingJob> Jobs(IReadOnlyList<PythonPacket> sent) => sent.OfType<CraftingStatusPacket>().Last().Jobs;

        private static bool Succeeded(IReadOnlyList<PythonPacket> sent) =>
            sent.OfType<CraftingResultPacket>().Single().Opcode == GameOpcode.CraftingSuccess;

        /// <summary>The third value of an ItemInfo: the crafter name the client shows as "Modified By", or null for None.</summary>
        private static string CrafterSent(ItemInfoPacket packet)
        {
            using var stream = new MemoryStream();
            using var writer = new Rasa.Memory.PythonWriter(new BinaryWriter(stream));

            packet.Write(writer);
            stream.Position = 0;

            using var reader = new Rasa.Memory.PythonReader(new BinaryReader(stream));

            reader.ReadTuple();
            reader.SkipValue();
            reader.SkipValue();

            if (reader.PeekType() != Rasa.Memory.PythonType.String)
                return null;

            return reader.ReadString();
        }

        private static string Refusal(IReadOnlyList<PythonPacket> sent)
        {
            Assert.AreEqual(GameOpcode.CraftingFailure, sent.OfType<CraftingResultPacket>().Single().Opcode);

            return sent.OfType<SystemMessagePacket>().SingleOrDefault()?.TextMessage;
        }

        [TestMethod]
        public void IntegrationTargetWriteFailureRollsBackStockFeeTargetAndPublication()
        {
            using var bench = new Bench();
            var armor = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var modules = bench.GiveStack(Body1Item, 2);
            var fee = bench.GiveStack(Mimeogel, 100);
            var before = bench.DurableSnapshot();
            var slots = bench.Client.Player.Inventory.PersonalInventory.ToArray();
            bench.Client.Player.ModulesChanged = false;
            var hit = false;
            bench.Context.BeforeSave = context =>
            {
                if (context.ChangeTracker.Entries<Rasa.Structures.Char.ItemEntry>()
                    .Any(entry => entry.Entity.ItemId == armor.Id && entry.Entity.Module1 == Body1))
                {
                    hit = true;
                    throw new DbUpdateException("owned fixture target write failure");
                }
            };
            IReadOnlyList<PythonPacket> sent = Array.Empty<PythonPacket>();
            Exception failure = null;
            try
            {
                sent = bench.Route(new RequestIntegrateItemPacket
                {
                    KraftwerksId = bench.Object.EntityId, TargetItemId = armor.EntityId,
                    ModuleItemId = modules.EntityId, Slot = 0, CraftingAction = 4
                });
            }
            catch (Exception error) { failure = error; }
            bench.Context.BeforeSave = null;

            Console.WriteLine($"Target failure reached={hit}; runtime stock={modules.StackSize}, fee={fee.StackSize}, modules={string.Join(",", armor.ModuleIds)}, crafter={armor.Crafter}; escaped={failure?.GetType().Name}");
            Assert.IsTrue(hit, "the real target write must be reached");
            Assert.AreEqual(string.Join("\n", before), string.Join("\n", bench.DurableSnapshot()),
                "input, fee, modules and crafter must all roll back");
            CollectionAssert.AreEqual(slots, bench.Client.Player.Inventory.PersonalInventory.ToArray());
            Assert.AreEqual(2u, modules.StackSize);
            Assert.AreEqual(100u, fee.StackSize);
            CollectionAssert.AreEqual(new uint[4], armor.ModuleIds.ToArray());
            Assert.AreEqual("", armor.Crafter);
            Assert.IsFalse(bench.Client.Player.ModulesChanged);
            Assert.IsNull(failure, "expected storage failures are answered, not thrown out of CLIENTREQUEST");
            Assert.AreEqual(GameOpcode.CraftingFailure, sent.OfType<CraftingResultPacket>().Single().Opcode);
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.IsFalse(sent.OfType<ItemInfoPacket>().Any());
            Assert.IsFalse(sent.OfType<DisplayClientMessagePacket>().Any(packet => packet.MsgId == PlayerMessage.PmCraftingSuccess));
        }

        private static ClientPythonPacket ModuleRequest(Bench bench, uint page, Item input, Item target) => page switch
        {
            KraftwerksManager.SalvagePage => new RequestSalvageItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = input.EntityId, CraftingAction = page },
            KraftwerksManager.ExtractionPage => new RequestExtractModulePacket
            { KraftwerksId = bench.Object.EntityId, ItemId = target.EntityId, Slot = 0, CraftingAction = page },
            KraftwerksManager.IntegrationPage => new RequestIntegrateItemPacket
            { KraftwerksId = bench.Object.EntityId, TargetItemId = target.EntityId, ModuleItemId = input.EntityId, Slot = 0, CraftingAction = page },
            _ => new RequestUpgradeItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = input.EntityId, CraftingAction = page }
        };

        [TestMethod]
        [DataRow(2u, 1, false)]
        [DataRow(2u, 1, true)]
        [DataRow(2u, 2, false)]
        [DataRow(2u, 2, true)]
        [DataRow(3u, 1, false)]
        [DataRow(3u, 1, true)]
        [DataRow(3u, 2, false)]
        [DataRow(3u, 2, true)]
        [DataRow(4u, 1, false)]
        [DataRow(4u, 1, true)]
        [DataRow(4u, 2, false)]
        [DataRow(4u, 2, true)]
        [DataRow(4u, 3, false)]
        [DataRow(4u, 3, true)]
        [DataRow(4u, 4, false)]
        [DataRow(4u, 4, true)]
        [DataRow(5u, 1, false)]
        [DataRow(5u, 1, true)]
        [DataRow(5u, 2, false)]
        [DataRow(5u, 2, true)]
        public void EveryModuleWritePhaseRollsBackAndCanRetry(uint page, int phase, bool afterWrite)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23,
                page == KraftwerksManager.ExtractionPage ? Body1 : 0);
            var input = page == KraftwerksManager.SalvagePage ? target : bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            bench.Client.Player.ModulesChanged = false;
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var saves = 0;
            var hit = false;
            Action<Rasa.Context.Char.SqliteCharContext> probe = context =>
            {
                Assert.IsNotNull(context.Database.CurrentTransaction, "every crafting write must join its owning transaction");
                Assert.AreEqual(runtime, bench.RuntimeSnapshot(), "runtime state cannot be published before commit");
                Assert.AreEqual(0, bench.Drain().Count, "no inventory, module, status or success packet before commit");
                if (++saves == phase)
                {
                    hit = true;
                    throw new DbUpdateException($"owned fixture {page}/{phase}/{afterWrite}");
                }
            };
            if (afterWrite) bench.Context.AfterSave = probe;
            else bench.Context.BeforeSave = probe;
            var request = ModuleRequest(bench, page, input, target);
            var sent = bench.Route(request);
            bench.Context.BeforeSave = null;
            bench.Context.AfterSave = null;

            Assert.IsTrue(hit, "the selected persistence phase must be reached");
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.IsNotNull(Refusal(sent));
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.IsFalse(sent.OfType<ItemInfoPacket>().Any());
            Assert.IsFalse(sent.OfType<InventoryRemoveItemPacket>().Any());

            sent = bench.Route(request);
            Assert.IsTrue(Succeeded(sent));
            Assert.AreEqual(page == KraftwerksManager.IntegrationPage ? 0 : 1, Jobs(sent).Count);
            Assert.AreEqual(page == KraftwerksManager.SalvagePage ? 0u :
                page == KraftwerksManager.ExtractionPage ? 2u : 1u, input.StackSize);
            if (page == KraftwerksManager.IntegrationPage || page == KraftwerksManager.ExtractionPage)
            {
                Assert.IsTrue(bench.Client.Player.ModulesChanged);
                CollectionAssert.AreEqual(target.ModuleIds.ToArray(), bench.Saved(target));
                Assert.AreEqual(target.Crafter, bench.SavedCrafter(target));
            }
        }

        [TestMethod]
        [DataRow(1, false)]
        [DataRow(1, true)]
        [DataRow(2, false)]
        [DataRow(2, true)]
        [DataRow(3, false)]
        [DataRow(3, true)]
        [DataRow(4, false)]
        [DataRow(4, true)]
        [DataRow(5, false)]
        [DataRow(5, true)]
        [DataRow(6, false)]
        [DataRow(6, true)]
        public void WholeInputAndFeeDeletionFailuresRollBackAndRetry(int phase, bool afterWrite)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var input = bench.GiveStack(Body1Item, 1);
            var cost = ItemModules.IntegrationCost(input, target);
            Assert.AreEqual(9u, cost);
            var fee = bench.GiveStack(Mimeogel, cost);
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var saves = 0;
            var hit = false;
            Action<Rasa.Context.Char.SqliteCharContext> probe = context =>
            {
                Assert.IsNotNull(context.Database.CurrentTransaction);
                Assert.AreEqual(runtime, bench.RuntimeSnapshot());
                if (++saves == phase)
                {
                    hit = true;
                    throw new DbUpdateException("owned fixture complete input/fee deletion");
                }
            };
            if (afterWrite) bench.Context.AfterSave = probe;
            else bench.Context.BeforeSave = probe;
            var request = ModuleRequest(bench, KraftwerksManager.IntegrationPage, input, target);
            var sent = bench.Route(request);
            bench.Context.BeforeSave = null;
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.IsTrue(Succeeded(bench.Route(request)));
            Assert.IsFalse(bench.InPack(input));
            Assert.IsFalse(bench.InPack(fee));
            using var unit = bench.Context.CreateChar();
            Assert.IsNull(unit.Items.GetItem(input.Id));
            Assert.IsNull(unit.Items.GetItem(fee.Id));
            CollectionAssert.AreEqual(new[] { Body1, 0u, 0u, 0u }, bench.Saved(target));
        }

        [TestMethod]
        [DataRow(1, false, false)]
        [DataRow(1, true, false)]
        [DataRow(2, false, false)]
        [DataRow(2, true, false)]
        [DataRow(1, false, true)]
        [DataRow(1, true, true)]
        public void ModuleOutputClaimFailureRollsBackAndRetryDoesNotDuplicate(int phase, bool afterWrite, bool merge)
        {
            using var bench = new Bench();
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            if (merge)
                bench.GiveStack(Body2Item, 3);
            var sent = bench.Route(ModuleRequest(bench, KraftwerksManager.UpgradePage, input, null));
            Assert.IsTrue(Succeeded(sent));
            var job = Jobs(sent).Single();
            var before = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var hit = false;
            var saves = 0;
            Action<Rasa.Context.Char.SqliteCharContext> probe = context =>
            {
                Assert.IsNotNull(context.Database.CurrentTransaction);
                Assert.AreEqual(runtime, bench.RuntimeSnapshot());
                Assert.AreEqual(1u, job.Count);
                if (++saves == phase)
                {
                    hit = true;
                    throw new DbUpdateException("owned fixture output saved but not committed");
                }
            };
            if (afterWrite) bench.Context.AfterSave = probe;
            else bench.Context.BeforeSave = probe;
            Exception failure = null;
            try
            {
                sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
                { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            }
            catch (Exception error) { failure = error; }
            bench.Context.BeforeSave = null;
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(before, bench.DurableSnapshot(), "a failed output claim cannot leave an orphan item");
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(1u, job.Count);
            Assert.IsNull(failure, "output storage failure must notify the client and retain the job");
            Assert.IsNotNull(Refusal(sent));
            Assert.AreSame(job, Jobs(sent).Single());

            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.AreEqual(merge ? 4u : 1u, bench.Only(Body2Item).StackSize);
            var claimed = bench.DurableSnapshot();
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            CollectionAssert.AreEqual(claimed, bench.DurableSnapshot());
            Assert.AreEqual(0, Jobs(sent).Count);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NewModuleOutputReceiptMustMatchItsModuleAndCrafterData(bool changeCrafter)
        {
            using var bench = new Bench();
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            var job = Jobs(bench.Route(ModuleRequest(bench, KraftwerksManager.UpgradePage, input, null))).Single();
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var saves = 0;
            var hit = false;
            bench.Context.AfterSave = context =>
            {
                if (++saves != 2)
                    return;
                bench.Context.AfterSave = null;
                hit = true;
                context.Database.ExecuteSqlRaw(changeCrafter
                    ? $"update items set crafter_name = 'Wrong maker' where item_template_id = {Body2Item}"
                    : $"update items set module_1 = {Mind5} where item_template_id = {Body2Item}");
            };
            var sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(1u, job.Count);
            Assert.IsNotNull(Refusal(sent));
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(0, Jobs(sent).Count);
            var output = bench.Only(Body2Item);
            CollectionAssert.AreEqual(new uint[4], bench.Saved(output));
            Assert.AreEqual("", bench.SavedCrafter(output));
        }

        [TestMethod]
        [DataRow("input-count")]
        [DataRow("fee-count")]
        [DataRow("input-slot")]
        [DataRow("input-owner")]
        [DataRow("target-slot")]
        [DataRow("target-owner")]
        [DataRow("target-inventory")]
        [DataRow("target-module")]
        [DataRow("target-crafter")]
        public void DurableStateChangedAfterTheWindowOpenedIsRefused(string change)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var input = bench.GiveStack(Body1Item, 2);
            var fee = bench.GiveStack(Mimeogel, 100);
            var sql = change switch
            {
                "input-count" => $"update items set stack_size = 1 where item_id = {input.Id}",
                "fee-count" => $"update items set stack_size = 0 where item_id = {fee.Id}",
                "input-slot" => $"update character_inventory set slot_id = 145 where item_id = {input.Id}",
                "input-owner" => $"update character_inventory set character_id = 1 where item_id = {input.Id}",
                "target-slot" => $"update character_inventory set slot_id = 40 where item_id = {target.Id}",
                "target-owner" => $"update character_inventory set character_id = 1 where item_id = {target.Id}",
                "target-inventory" => $"update character_inventory set invenotry_type = 2 where item_id = {target.Id}",
                "target-module" => $"update items set module_2 = {Mind5} where item_id = {target.Id}",
                _ => $"update items set crafter_name = 'Somebody' where item_id = {target.Id}"
            };
            using (var context = bench.Context.Open())
                context.Database.ExecuteSqlRaw(sql);
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var sent = bench.Route(ModuleRequest(bench, KraftwerksManager.IntegrationPage, input, target));
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot(), "do not overwrite the competing durable state");
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(0, Jobs(sent).Count);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InputOrFeeChangingAfterPlanningRollsBack(bool changeFee)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var input = bench.GiveStack(Body1Item, 2);
            var fee = bench.GiveStack(Mimeogel, 100);
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var reads = 0;
            var hit = false;
            bench.Context.BeforeQuery = context =>
            {
                if (context.Database.CurrentTransaction == null)
                    return;
                // Prepare's second batched item read is after all stock/fee/target planning.
                bench.Context.BeforeCommand = sql =>
                {
                    if (!sql.Contains("\"items\"") || !sql.Contains("json_each") || ++reads != 2)
                        return;
                    bench.Context.BeforeCommand = null;
                    hit = true;
                    context.Database.ExecuteSqlRaw($"update items set stack_size = 0 where item_id = {(changeFee ? fee.Id : input.Id)}");
                };
                bench.Context.BeforeQuery = null;
            };
            var sent = bench.Route(ModuleRequest(bench, KraftwerksManager.IntegrationPage, input, target));
            bench.Context.BeforeQuery = null;
            bench.Context.BeforeCommand = null;
            Assert.IsTrue(hit, "the post-planning inventory revalidation read must be reached");
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.IsTrue(Succeeded(bench.Route(ModuleRequest(bench, KraftwerksManager.IntegrationPage, input, target))));
        }

        [TestMethod]
        [DataRow("module")]
        [DataRow("crafter")]
        [DataRow("quantity")]
        [DataRow("slot")]
        public void FinalTargetReceiptMismatchRollsBackInsteadOfPublishing(string change)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var hit = false;
            bench.Context.AfterSave = context =>
            {
                if (!context.ChangeTracker.Entries<Rasa.Structures.Char.ItemEntry>()
                    .Any(entry => entry.Entity.ItemId == target.Id && entry.Entity.CrafterName == bench.Client.Player.FamilyName))
                    return;
                bench.Context.AfterSave = null;
                hit = true;
                context.Database.ExecuteSqlRaw(change switch
                {
                    "module" => $"update items set module_4 = {Mind5} where item_id = {target.Id}",
                    "crafter" => $"update items set crafter_name = 'Intruder' where item_id = {target.Id}",
                    "quantity" => $"update items set stack_size = 2 where item_id = {target.Id}",
                    _ => $"update character_inventory set slot_id = 40 where item_id = {target.Id}"
                });
            };
            var sent = bench.Route(ModuleRequest(bench, KraftwerksManager.IntegrationPage, input, target));
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.IsTrue(Succeeded(bench.Route(ModuleRequest(bench, KraftwerksManager.IntegrationPage, input, target))));
        }

        [TestMethod]
        [DataRow(2u, false)]
        [DataRow(3u, false)]
        [DataRow(4u, false)]
        [DataRow(5u, false)]
        [DataRow(2u, true)]
        [DataRow(3u, true)]
        [DataRow(5u, true)]
        public void LateStationOrJobAdmissionChangeRollsBack(uint page, bool fillJobs)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23,
                page == KraftwerksManager.ExtractionPage ? Body1 : 0);
            var input = page == KraftwerksManager.SalvagePage ? target : bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var hit = false;
            bench.Context.AfterSave = _ =>
            {
                bench.Context.AfterSave = null;
                hit = true;
                if (fillJobs)
                    for (var index = 0; index < KraftwerksManager.MaxJobsPerStation; index++)
                        bench.JobBag().Add(new CraftingJob { ResultItemId = (ulong)index + 1000, FinishTick = 0, Count = 1 });
                else
                    EntityManager.Instance.DynamicObjects.Remove(bench.Object.EntityId);
            };
            var sent = bench.Route(ModuleRequest(bench, page, input, target));
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            if (fillJobs)
                bench.JobBag().Clear();
            else
                EntityManager.Instance.DynamicObjects[bench.Object.EntityId] = bench.Object;
            Assert.IsTrue(Succeeded(bench.Route(ModuleRequest(bench, page, input, target))));
        }

        [TestMethod]
        public void FullJobBagRefusesOutputPagesButStillAllowsIntegration()
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23, Spirit1);
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            for (var index = 0; index < KraftwerksManager.MaxJobsPerStation; index++)
                bench.JobBag().Add(new CraftingJob { ResultItemId = (ulong)index + 1000, FinishTick = 0, Count = 1 });
            var durable = bench.DurableSnapshot();
            foreach (var page in new[] { KraftwerksManager.SalvagePage, KraftwerksManager.UpgradePage })
            {
                var sent = bench.Route(ModuleRequest(bench, page, input, target));
                Assert.IsNotNull(Refusal(sent));
                CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            }
            var extract = bench.Route(new RequestExtractModulePacket
            { KraftwerksId = bench.Object.EntityId, ItemId = target.EntityId, Slot = 0, CraftingAction = 3 });
            Assert.IsNotNull(Refusal(extract));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            var integration = bench.Route(new RequestIntegrateItemPacket
            {
                KraftwerksId = bench.Object.EntityId, TargetItemId = target.EntityId,
                ModuleItemId = input.EntityId, Slot = 1, CraftingAction = 4
            });
            Assert.IsTrue(Succeeded(integration));
            Assert.AreEqual(KraftwerksManager.MaxJobsPerStation, Jobs(integration).Count);
        }

        [TestMethod]
        public void FullInventoryRetainsModuleOutputUntilItCanBeClaimed()
        {
            using var bench = new Bench();
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            var job = Jobs(bench.Route(ModuleRequest(bench, KraftwerksManager.UpgradePage, input, null))).Single();
            bench.FillCraftingBag();
            var durable = bench.DurableSnapshot();
            var sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(GameOpcode.CraftingFailure, sent.OfType<CraftingResultPacket>().Single().Opcode);
            Assert.AreEqual(PlayerMessage.PmInventoryFull, sent.OfType<DisplayClientMessagePacket>().Single().MsgId);
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(1u, job.Count);

            var blockerId = bench.Client.Player.Inventory.PersonalInventory[149];
            var blocker = EntityManager.Instance.GetItem(blockerId);
            InventoryManager.Instance.ReduceStackCount(bench.Client, InventoryType.Personal, blocker, blocker.StackSize);
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(0u, job.Count);
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.AreEqual(1u, bench.Only(Body2Item).StackSize);
            var after = bench.DurableSnapshot();
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            CollectionAssert.AreEqual(after, bench.DurableSnapshot(), "duplicate claims cannot create another output");
        }

        [TestMethod]
        public void PartialSalvageOutputClaimKeepsOnlyTheUnclaimedRemainder()
        {
            using var bench = new Bench();
            var input = bench.GiveStack(Body1Item, 7);
            var fee = bench.GiveStack(Mimeogel, 49999);
            var job = Jobs(bench.Route(ModuleRequest(bench, KraftwerksManager.SalvagePage, input, null))).Single();
            Assert.AreEqual(14u, job.Count);
            bench.FillCraftingBag();
            var sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(13u, job.Count);
            Assert.AreSame(job, Jobs(sent).Single());
            Assert.AreEqual(50000u, fee.StackSize);
            var after = bench.DurableSnapshot();
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            CollectionAssert.AreEqual(after, bench.DurableSnapshot());
            Assert.AreEqual(13u, job.Count);
            InventoryManager.Instance.ReduceStackCount(bench.Client, InventoryType.Personal, fee, 13);
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.AreEqual(50000u, fee.StackSize);
        }

        [TestMethod]
        [DataRow(false, 1, false)]
        [DataRow(false, 1, true)]
        [DataRow(false, 2, false)]
        [DataRow(false, 2, true)]
        [DataRow(true, 1, false)]
        [DataRow(true, 1, true)]
        public void ExternalSetAndClearKeepTheirThrowingContractWithoutRuntimeMutation(bool clear, int phase, bool afterWrite)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23, Spirit1, Mind5);
            var durable = bench.DurableSnapshot();
            bench.Client.Player.ModulesChanged = false;
            var runtime = bench.RuntimeSnapshot();
            var saves = 0;
            var hit = false;
            Action<Rasa.Context.Char.SqliteCharContext> probe = context =>
            {
                Assert.IsNotNull(context.Database.CurrentTransaction);
                Assert.AreEqual(runtime, bench.RuntimeSnapshot());
                if (++saves == phase)
                {
                    hit = true;
                    throw new DbUpdateException("owned fixture external module write");
                }
            };
            if (afterWrite) bench.Context.AfterSave = probe;
            else bench.Context.BeforeSave = probe;
            Exception failure = null;
            void Change()
            {
                if (clear)
                    ItemModules.Clear(bench.Client, target, bench.Context);
                else
                    ItemModules.Set(bench.Client, target, 0, Body1, bench.Context, "Modifier");
            }
            try { Change(); }
            catch (Exception error) { failure = error; }
            bench.Context.BeforeSave = null;
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            Assert.IsInstanceOfType<DbUpdateException>(failure);
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(0, bench.Drain().Count);
            Change();
            CollectionAssert.AreEqual(target.ModuleIds.ToArray(), bench.Saved(target));
            Assert.AreEqual(clear ? 0u : Body1, target.ModuleIds[0]);
            Assert.AreEqual(clear ? "" : "Modifier", target.Crafter);
            Assert.AreEqual(target.Crafter, bench.SavedCrafter(target));
            Assert.IsTrue(bench.Client.Player.ModulesChanged);
            Assert.AreEqual(1, bench.Drain().OfType<ItemInfoPacket>().Count());
        }

        [TestMethod]
        [DataRow(2u, "input")]
        [DataRow(3u, "target")]
        [DataRow(4u, "target")]
        [DataRow(4u, "input")]
        [DataRow(5u, "input")]
        [DataRow(3u, "fee")]
        [DataRow(4u, "fee")]
        [DataRow(5u, "fee")]
        public void AssignmentProtectedInputsTargetsAndFeesCannotBeUsed(uint page, string protectedRole)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23,
                page == KraftwerksManager.ExtractionPage ? Body1 : 0);
            var input = bench.GiveStack(Body1Item, 2);
            var fee = bench.GiveStack(Mimeogel, 100);
            var item = protectedRole == "target" ? target : protectedRole == "fee" ? fee : input;
            using (var unit = bench.Context.CreateChar())
                unit.CharacterMissionItems.Save(new Rasa.Structures.Char.CharacterMissionItemEntry
                {
                    CharacterId = bench.Client.Player.Id, MissionId = 1990,
                    AssignmentId = Guid.NewGuid().ToString("N"), Generation = 1,
                    ItemKey = "protected-crafting-fixture", ItemId = item.Id, Quantity = item.StackSize
                });
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var sent = bench.Route(ModuleRequest(bench, page, input, target));
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(0, Jobs(sent).Count);
        }

        [TestMethod]
        public void WornItemBonusesChangeOnlyAfterSetOrClearCommits()
        {
            using var bench = new Bench();
            var gear = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var packSlot = gear.OwnerSlotId;
            var wornSlot = (uint)EquipmentData.Helmet;
            using (var unit = bench.Context.CreateChar())
                unit.CharacterInventories.MoveInvItem(bench.Client.AccountEntry.Id, bench.Client.Player.Id,
                    (uint)InventoryType.EquipedInventory, wornSlot, gear.Id);
            bench.Client.Player.Inventory.PersonalInventory[(int)packSlot] = 0;
            gear.OwnerSlotId = wornSlot;
            bench.Client.Player.Inventory.EquippedInventory[(int)wornSlot] = gear.EntityId;
            ManifestationManager.Instance.UpdateStatsValues(bench.Client, true);
            var body = bench.Client.Player.Attributes[Attributes.Body].CurrentMax;
            bench.Client.Player.ModulesChanged = false;
            bench.Drain();
            bench.Context.AfterSave = _ => throw new DbUpdateException("owned fixture worn module");
            Exception failure = null;
            try { ItemModules.Set(bench.Client, gear, 0, Body1, bench.Context); }
            catch (Exception error) { failure = error; }
            bench.Context.AfterSave = null;
            Assert.IsInstanceOfType<DbUpdateException>(failure);
            Assert.AreEqual(0, ItemModuleBonuses.Of(bench.Client.Player).Body);
            ItemModuleBonuses.Worker(bench.Client.Player.MapChannel);
            Assert.AreEqual(body, bench.Client.Player.Attributes[Attributes.Body].CurrentMax);
            Assert.IsFalse(bench.Client.Player.ModulesChanged);
            ItemModules.Set(bench.Client, gear, 0, Body1, bench.Context);
            Assert.AreEqual(3, ItemModuleBonuses.Of(bench.Client.Player).Body);
            Assert.AreEqual(body, bench.Client.Player.Attributes[Attributes.Body].CurrentMax, "existing next-tick semantics");
            ItemModuleBonuses.Worker(bench.Client.Player.MapChannel);
            Assert.AreEqual(body + 3, bench.Client.Player.Attributes[Attributes.Body].CurrentMax);
            Assert.IsFalse(bench.Client.Player.ModulesChanged);
            ItemModules.Clear(bench.Client, gear, bench.Context);
            ItemModuleBonuses.Worker(bench.Client.Player.MapChannel);
            Assert.AreEqual(body, bench.Client.Player.Attributes[Attributes.Body].CurrentMax);
            CollectionAssert.AreEqual(new uint[4], bench.Saved(gear));
        }

        [TestMethod]
        public void ExternalModuleWriteRejectsAStaleUnregisteredItemInstance()
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23);
            var stale = new Item(target.ItemTemplateId, target.StackSize, target.CurrentHitPoints, target.Color)
            {
                Id = target.Id, ItemTemplate = target.ItemTemplate,
                OwnerId = target.OwnerId, OwnerSlotId = target.OwnerSlotId
            };
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            Exception failure = null;
            try { ItemModules.Set(bench.Client, stale, 0, Body1, bench.Context); }
            catch (Exception error) { failure = error; }
            Assert.IsNotNull(failure, "a noncanonical item instance cannot publish a module edit to the player's inventory");
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreEqual(0, bench.Drain().Count);
        }

        [TestMethod]
        [DataRow(2u)]
        [DataRow(3u)]
        [DataRow(4u)]
        [DataRow(5u)]
        public void PreflightProtectionReadFailureIsAnExplicitCraftingFailure(uint page)
        {
            using var bench = new Bench();
            var target = bench.GiveGear(ArmorClass, LootQuality.Rare, 23,
                page == KraftwerksManager.ExtractionPage ? Body1 : 0);
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var hit = false;
            bench.Context.BeforeQuery = _ =>
            {
                hit = true;
                throw new SqliteException("owned fixture protection read failure", 5);
            };
            Exception failure = null;
            IReadOnlyList<PythonPacket> sent = Array.Empty<PythonPacket>();
            try { sent = bench.Route(ModuleRequest(bench, page, input, target)); }
            catch (Exception error) { failure = error; }
            bench.Context.BeforeQuery = null;
            Assert.IsTrue(hit);
            Assert.IsNull(failure, "a known persistence failure must not escape CLIENTREQUEST");
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.IsNotNull(Refusal(sent));
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.IsTrue(Succeeded(bench.Route(ModuleRequest(bench, page, input, target))));
        }

        [TestMethod]
        [DataRow("quantity")]
        [DataRow("template")]
        [DataRow("station")]
        public void LateOutputClaimOrStationChangesRollBackTheNewItem(string change)
        {
            using var bench = new Bench();
            var input = bench.GiveStack(Body1Item, 2);
            bench.GiveStack(Mimeogel, 100);
            var job = Jobs(bench.Route(ModuleRequest(bench, KraftwerksManager.UpgradePage, input, null))).Single();
            var durable = bench.DurableSnapshot();
            var runtime = bench.RuntimeSnapshot();
            var hit = false;
            bench.Context.AfterSave = _ =>
            {
                bench.Context.AfterSave = null;
                hit = true;
                if (change == "quantity") job.Count = 2;
                else if (change == "template") job.ResultItemTemplateId = Body1Item;
                else EntityManager.Instance.DynamicObjects.Remove(bench.Object.EntityId);
            };
            var sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            bench.Context.AfterSave = null;
            Assert.IsTrue(hit);
            Assert.IsNotNull(Refusal(sent));
            CollectionAssert.AreEqual(durable, bench.DurableSnapshot());
            Assert.AreEqual(runtime, bench.RuntimeSnapshot());
            Assert.AreSame(job, Jobs(sent).Single());
            job.Count = 1;
            job.ResultItemTemplateId = Body2Item;
            EntityManager.Instance.DynamicObjects[bench.Object.EntityId] = bench.Object;
            sent = bench.Route(new RequestRetrieveFinishedCraftItemPacket
            { KraftwerksId = bench.Object.EntityId, ItemId = job.ResultItemId });
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.AreEqual(1u, bench.Only(Body2Item).StackSize);
        }

        [TestMethod]
        public void SalvageDestroysTheItemAndLeavesItsMimeogelToBeTaken()
        {
            using var bench = new Bench();

            var armor = bench.GiveGear(ArmorClass, LootQuality.Rare, 23, Body1, 0, Body5);
            var sent = bench.After((station, id) => station.RequestSalvageItem(bench.Client, new RequestSalvageItemPacket { KraftwerksId = id, ItemId = armor.EntityId, CraftingAction = 2 }));

            Assert.IsTrue(Succeeded(sent));
            Assert.IsFalse(bench.InPack(armor));
            Assert.IsTrue(sent.OfType<InventoryRemoveItemPacket>().Any());

            var job = Jobs(sent).Single();

            Assert.AreEqual((MimeogelClass, Mimeogel, 88u, KraftwerksManager.SalvagePage), (job.ResultClassId, job.ResultItemTemplateId, job.Count, job.CraftingPage));
            Assert.IsTrue(job.IsFinished);
            Assert.AreEqual(0.0, job.TimeLeftSeconds);
            Assert.AreEqual(0u, bench.Carried(MimeogelClass), "not until it is taken");

            sent = bench.After((station, id) => station.RequestRetrieveAllFinishedItems(bench.Client, new RequestRetrieveAllFinishedItemsPacket { KraftwerksId = id }));
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.AreEqual(88u, bench.Carried(MimeogelClass));
            Assert.AreEqual("", bench.Only(Mimeogel).Crafter, "salvaged, not made: nobody's name on it");

            // A stack of modules is salvaged whole.
            var modules = bench.GiveStack(Body1Item, 7);

            sent = bench.After((station, id) => station.RequestSalvageItem(bench.Client, new RequestSalvageItemPacket { KraftwerksId = id, ItemId = modules.EntityId, CraftingAction = 2 }));
            Assert.AreEqual(14u, Jobs(sent).Single().Count);
            Assert.IsFalse(bench.InPack(modules));

            // What is worth nothing is refused and kept.
            var plain = bench.GiveGear(WeaponClass, LootQuality.Normal, 50);

            sent = bench.After((station, id) => station.RequestSalvageItem(bench.Client, new RequestSalvageItemPacket { KraftwerksId = id, ItemId = plain.EntityId, CraftingAction = 2 }));
            Assert.AreEqual("That item cannot be salvaged.", Refusal(sent));
            Assert.IsTrue(bench.InPack(plain));
            Assert.AreEqual(1, Jobs(sent).Count, "the Mimeogel of the modules is still waiting");

            // An item that is not in the pack is not the window's request: failed, with nothing said.
            sent = bench.After((station, id) => station.RequestSalvageItem(bench.Client, new RequestSalvageItemPacket { KraftwerksId = id, ItemId = 987654321, CraftingAction = 2 }));
            Assert.IsNull(Refusal(sent));
            sent = bench.After((station, id) => station.RequestSalvageItem(bench.Client, new RequestSalvageItemPacket { KraftwerksId = id, ItemId = 0, CraftingAction = 2 }));
            Assert.IsNull(Refusal(sent));

            // A station out of reach is not answered at all.
            bench.Object.Position = bench.Client.Player.Position + new System.Numerics.Vector3(50, 0, 0);
            sent = bench.After((station, id) => station.RequestSalvageItem(bench.Client, new RequestSalvageItemPacket { KraftwerksId = id, ItemId = plain.EntityId, CraftingAction = 2 }));
            Assert.AreEqual(0, sent.Count);
        }

        [TestMethod]
        public void ExtractionTakesTheModuleOutOfTheSlotForItsFeeAndMakesItAnItem()
        {
            using var bench = new Bench();

            var weapon = bench.GiveGear(WeaponClass, LootQuality.Uncommon, 12, Crit1, 0, Crit5);

            RequestExtractModulePacket Extract(ulong id, uint slot) => new RequestExtractModulePacket { KraftwerksId = id, ItemId = weapon.EntityId, Slot = slot, CraftingAction = 3 };

            // The fee first.
            var sent = bench.After((station, id) => station.RequestExtractModule(bench.Client, Extract(id, 0)));

            Assert.AreEqual("You need 1 Mimeomech and have 0.", Refusal(sent));
            CollectionAssert.AreEqual(new uint[] { Crit1, 0, Crit5, 0 }, weapon.ModuleIds.ToArray());

            bench.GiveStack(Mimeogel, 10);
            sent = bench.After((station, id) => station.RequestExtractModule(bench.Client, Extract(id, 0)));

            Assert.IsTrue(Succeeded(sent));
            Assert.AreEqual(9u, bench.Carried(MimeogelClass));
            Assert.IsTrue(bench.InPack(weapon), "the item stays where it is");
            CollectionAssert.AreEqual(new uint[] { 0, 0, Crit5, 0 }, weapon.ModuleIds.ToArray());
            CollectionAssert.AreEqual(new uint[] { 0, 0, Crit5, 0 }, bench.Saved(weapon));
            Assert.AreSame(weapon, sent.OfType<ItemInfoPacket>().Single().Item, "and the client is told what it carries now");
            Assert.AreEqual("", weapon.Crafter, "taking a module out puts nobody's name on the item");
            Assert.AreEqual("", bench.SavedCrafter(weapon));

            var job = Jobs(sent).Single();

            Assert.AreEqual((ModuleItemClass, 123158u, 1u, KraftwerksManager.ExtractionPage, 2u), (job.ResultClassId, job.ResultItemTemplateId, job.Count, job.CraftingPage, job.QualityId));

            // An Exceptional module cannot be taken out, and an empty slot has none.
            sent = bench.After((station, id) => station.RequestExtractModule(bench.Client, Extract(id, 2)));
            Assert.AreEqual("That module cannot be extracted.", Refusal(sent));

            sent = bench.After((station, id) => station.RequestExtractModule(bench.Client, Extract(id, 1)));
            Assert.AreEqual("There is no module there to extract.", Refusal(sent));

            sent = bench.After((station, id) => station.RequestExtractModule(bench.Client, Extract(id, 4)));
            Assert.AreEqual("There is no module there to extract.", Refusal(sent));

            CollectionAssert.AreEqual(new uint[] { 0, 0, Crit5, 0 }, bench.Saved(weapon));
            Assert.AreEqual(9u, bench.Carried(MimeogelClass));

            // Taken, the module is an item again.
            sent = bench.After((station, id) => station.RequestRetrieveFinishedCraftItem(bench.Client, new RequestRetrieveFinishedCraftItemPacket { KraftwerksId = id, ItemId = job.ResultItemId }));
            Assert.AreEqual(0, Jobs(sent).Count);
            Assert.AreEqual(1u, bench.Only(123158).StackSize);
        }

        [TestMethod]
        public void IntegrationPutsOneModuleIntoTheSlotForTheClientsFee()
        {
            using var bench = new Bench();

            var armor = bench.GiveGear(ArmorClass, LootQuality.Rare, 23, 0, 0, Mind5);
            var modules = bench.GiveStack(Body1Item, 2);
            var mimeogel = bench.GiveStack(Mimeogel, 37);

            RequestIntegrateItemPacket Integrate(ulong id, Item target, Item module, uint slot) =>
                new RequestIntegrateItemPacket { KraftwerksId = id, TargetItemId = target.EntityId, ModuleItemId = module.EntityId, Slot = slot, CraftingAction = 4 };

            Assert.AreEqual(38u, ItemModules.IntegrationCost(modules, armor));

            // One short.
            var sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, armor, modules, 0)));

            Assert.AreEqual("You need 38 Mimeomech and have 37.", Refusal(sent));
            bench.GiveStack(Mimeogel, 63);

            // Into a full slot, and one there is not.
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, armor, modules, 2)));
            Assert.AreEqual("That module slot is not empty.", Refusal(sent));
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, armor, modules, 4)));
            Assert.AreEqual("That module slot is not empty.", Refusal(sent));

            Assert.AreEqual(2u, modules.StackSize);
            Assert.AreEqual(100u, bench.Carried(MimeogelClass));
            CollectionAssert.AreEqual(new uint[] { 0, 0, Mind5, 0 }, bench.Saved(armor));
            Assert.AreEqual("", bench.SavedCrafter(armor), "a refusal puts no name on it");

            // Into an empty one: one module of the stack and the fee are gone, and the item has it.
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, armor, modules, 1)));

            Assert.IsTrue(Succeeded(sent));
            Assert.AreEqual(1u, modules.StackSize);
            Assert.AreEqual(62u, bench.Carried(MimeogelClass));
            CollectionAssert.AreEqual(new uint[] { 0, Body1, Mind5, 0 }, armor.ModuleIds.ToArray());
            CollectionAssert.AreEqual(new uint[] { 0, Body1, Mind5, 0 }, bench.Saved(armor));
            Assert.AreSame(armor, sent.OfType<ItemInfoPacket>().Single().Item);
            Assert.AreEqual(0, Jobs(sent).Count, "nothing is made, so nothing waits");

            // The item is the player's work now: the name its tooltip shows as "Modified By",
            // on the item, in its row, and in the ItemInfo the client was sent.
            var name = bench.Client.Player.FamilyName;

            Assert.IsFalse(string.IsNullOrEmpty(name));
            Assert.AreEqual(name, armor.Crafter);
            Assert.AreEqual(name, bench.SavedCrafter(armor));
            Assert.AreEqual(name, CrafterSent(sent.OfType<ItemInfoPacket>().Single()));
            Assert.AreEqual(PlayerMessage.PmCraftingSuccess, sent.OfType<DisplayClientMessagePacket>().Single().MsgId);

            // A second of the kind is refused, and so is a module for another kind of item.
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, armor, modules, 0)));
            Assert.AreEqual("The item already has a module of that kind.", Refusal(sent));

            var weapon = bench.GiveGear(WeaponClass, LootQuality.Normal, 1);

            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, weapon, modules, 0)));
            Assert.AreEqual("That module is for another kind of item.", Refusal(sent));

            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, weapon, mimeogel, 0)));
            Assert.AreEqual("That is not a module.", Refusal(sent));

            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, modules, modules, 0)));
            Assert.AreEqual("That item takes no modules.", Refusal(sent));

            Assert.AreEqual(1u, modules.StackSize);
            Assert.AreEqual(62u, bench.Carried(MimeogelClass));

            // The last of a stack is used up with it, and a fee of nothing is no bar: the
            // weakest weapon module into a level 1 weapon.
            var crit = bench.GiveStack(123158, 1);

            Assert.AreEqual(0u, ItemModules.IntegrationCost(crit, weapon));
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, weapon, crit, 3)));
            Assert.IsTrue(Succeeded(sent));
            Assert.IsFalse(bench.InPack(crit));
            CollectionAssert.AreEqual(new uint[] { 0, 0, 0, Crit1 }, bench.Saved(weapon));
            Assert.AreEqual(62u, bench.Carried(MimeogelClass));
            Assert.AreEqual(name, bench.SavedCrafter(weapon));

            // And whoever made an item, or modified it before, gives way to whoever modifies it now.
            var theirs = bench.GiveGear(ToolClass, LootQuality.Normal, 1);

            bench.SetCrafter(theirs, "Somebody");
            Assert.AreEqual(ItemModules.IntegrationProblem.WrongKindOfItem, ItemModules.CanIntegrate(modules, theirs));
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, theirs, modules, 0)));
            Assert.AreEqual("That module is for another kind of item.", Refusal(sent));
            Assert.AreEqual("Somebody", theirs.Crafter, "not by a refusal");

            var made = bench.GiveGear(ArmorClass, LootQuality.Normal, 1);

            bench.SetCrafter(made, "Somebody");
            sent = bench.After((station, id) => station.RequestIntegrateItem(bench.Client, Integrate(id, made, modules, 0)));
            Assert.IsTrue(Succeeded(sent));
            Assert.AreEqual(name, made.Crafter);
            Assert.AreEqual(name, bench.SavedCrafter(made));
        }

        [TestMethod]
        public void UpgradeMakesOneModuleTheNextStrengthForItsFee()
        {
            using var bench = new Bench();

            var modules = bench.GiveStack(Body1Item, 2);

            RequestUpgradeItemPacket Upgrade(ulong id, Item item) => new RequestUpgradeItemPacket { KraftwerksId = id, ItemId = item.EntityId, CraftingAction = 5 };

            var sent = bench.After((station, id) => station.RequestUpgradeItem(bench.Client, Upgrade(id, modules)));

            Assert.AreEqual("You need 1 Mimeomech and have 0.", Refusal(sent));
            bench.GiveStack(Mimeogel, 5);

            sent = bench.After((station, id) => station.RequestUpgradeItem(bench.Client, Upgrade(id, modules)));
            Assert.IsTrue(Succeeded(sent));
            Assert.AreEqual(1u, modules.StackSize);
            Assert.AreEqual(4u, bench.Carried(MimeogelClass));

            var job = Jobs(sent).Single();

            Assert.AreEqual((ModuleItemClass, Body2Item, 1u, KraftwerksManager.UpgradePage, 3u), (job.ResultClassId, job.ResultItemTemplateId, job.Count, job.CraftingPage, job.QualityId));

            sent = bench.After((station, id) => station.RequestRetrieveAllFinishedItems(bench.Client, new RequestRetrieveAllFinishedItemsPacket { KraftwerksId = id }));
            Assert.AreEqual(0, Jobs(sent).Count);

            var upgraded = bench.Only(Body2Item);

            Assert.AreEqual(1u, upgraded.StackSize);
            Assert.AreEqual("", upgraded.Crafter);

            // The next step is 5, and there are 4.
            sent = bench.After((station, id) => station.RequestUpgradeItem(bench.Client, Upgrade(id, upgraded)));
            Assert.AreEqual("You need 5 Mimeomech and have 4.", Refusal(sent));

            // What is no module, and a module at its last strength, cannot be.
            var exceptional = bench.GiveStack(Body5Item, 1);

            sent = bench.After((station, id) => station.RequestUpgradeItem(bench.Client, Upgrade(id, exceptional)));
            Assert.AreEqual("That module cannot be upgraded.", Refusal(sent));

            var armor = bench.GiveGear(ArmorClass, LootQuality.Epic, 10);

            sent = bench.After((station, id) => station.RequestUpgradeItem(bench.Client, Upgrade(id, armor)));
            Assert.AreEqual("That module cannot be upgraded.", Refusal(sent));
            Assert.IsTrue(bench.InPack(exceptional) && bench.InPack(armor));
            Assert.AreEqual(4u, bench.Carried(MimeogelClass));
        }
    }
}
