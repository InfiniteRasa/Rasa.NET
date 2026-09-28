using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Missions.Definitions;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class AccountRewardItemTests
    {
        private const uint LogosFistTemplate = 111120;     // "Logos Fist" Emote: 464 level 1, flag 388, /logosfist (2, 48)
        private const uint RocketTemplate = 122834;        // Soyuz ISS Model Rocket: 509 level 1, family 1902
        private const uint GameStopRocketTemplate = 122843; // 509 level 3, family 1920

        [TestMethod]
        public void TheEmoteItemSetsItsFlagUsesItselfUpAndTellsTheClient()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, LogosFistTemplate);
            var manager = CreateManager(harness, 464, 1, LogosFistTemplate);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, Request(464, 1, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardEmoteItem);
            Land(harness, manager, pending);

            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[388]);
            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(1U, unit.CharacterFlags.GetValue(harness.Client.Player.Id, 388));
                Assert.IsNull(unit.Items.GetItem(item.Id), "The emote item is used up.");
            }

            var packets = harness.Drain();
            Assert.IsTrue(packets.OfType<PlayerFlagsPacket>().Single().PlayerFlagIds.Contains(388U));
            Assert.IsTrue(packets.OfType<AbilityRecoveryPacket>().Any(p => p.ActionId == ActionId.AccountrewardEmoteItem));
        }

        [TestMethod]
        public void AnEmoteAlreadyKnownIsRefusedAndTheItemKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, LogosFistTemplate);
            var manager = CreateManager(harness, 464, 1, LogosFistTemplate);
            harness.Client.Player.PlayerFlags = new Dictionary<uint, uint>(harness.Client.Player.PlayerFlags) { [388] = 1 };

            manager.RequestPerformAbility(harness.Client, Request(464, 1, item.EntityId));

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.AccountrewardEmoteItem));
            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id));
        }

        [TestMethod]
        public void AnEmoteLearnedDuringTheWindupStopsTheSecondItem()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, LogosFistTemplate);
            var manager = CreateManager(harness, 464, 1, LogosFistTemplate);

            manager.RequestPerformAbility(harness.Client, Request(464, 1, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardEmoteItem);
            harness.Client.Player.PlayerFlags = new Dictionary<uint, uint>(harness.Client.Player.PlayerFlags) { [388] = 1 };
            Land(harness, manager, pending);

            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id));
        }

        [TestMethod]
        [DataRow(RocketTemplate, 1U, "vfx_emote_model_rocket_v01")]
        [DataRow(GameStopRocketTemplate, 3U, "vfx_emote_model_rocket_v03_gamespot")]
        public void TheRocketFliesInFrontOfThePlayerIsKeptAndComesDown(uint template, uint level, string package)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var item = Grant(harness, template);
            var manager = CreateManager(harness, 509, level, template);
            var player = harness.Client.Player;
            var before = harness.BootcampMap.DynamicObjects.Count;

            manager.RequestPerformAbility(harness.Client, Request(509, level, item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.VisualByActor);
            Land(harness, manager, pending);

            var rocket = EmitterObjects(harness).Single();
            var emitter = (MapEmitter)rocket.ObjectData;
            Assert.AreEqual(package, FxPackages.NameOf(emitter.PackageId));
            Assert.IsTrue(emitter.IsOn);
            Assert.AreEqual(3.3f, System.Numerics.Vector3.Distance(player.Position, rocket.Position), 0.01f);
            Assert.IsTrue(System.Numerics.Vector3.Dot(AbilityManager.FacingOf(player), rocket.Position - player.Position) > 0, "In front.");

            using (var unit = harness.Context.CreateChar())
                Assert.IsNotNull(unit.Items.GetItem(item.Id), "The rocket is kept.");
            Assert.AreEqual(1U, item.StackSize);

            // One in the air at a time.
            manager.RequestPerformAbility(harness.Client, Request(509, level, item.EntityId));
            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.VisualByActor));

            manager.ModelRocketWorker(harness.BootcampMap, Environment.TickCount64 + 5_000);
            Assert.HasCount(1, EmitterObjects(harness).ToArray(), "Up for DURATION (10 s).");
            manager.ModelRocketWorker(harness.BootcampMap, Environment.TickCount64 + 11_000);
            Assert.IsEmpty(EmitterObjects(harness).ToArray());

            manager.RequestPerformAbility(harness.Client, Request(509, level, item.EntityId));
            Assert.IsTrue(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.VisualByActor), "Launchable again once down.");
        }

        [TestMethod]
        public void EveryRocketLevelHasAPackageTheClientKnows()
        {
            foreach (var packageId in AbilityManager.ModelRocketPackages.Values)
                Assert.IsTrue(FxPackages.Names.ContainsKey(packageId), packageId.ToString());
            Assert.AreEqual(AbilityManager.ModelRocketPackages.Count, AbilityManager.ModelRocketPackages.Values.Distinct().Count());
            Assert.HasCount(9, AbilityManager.ModelRocketPackages.ToArray());
        }

        [TestMethod]
        public void ARewardEmoteWantsItsFlagAndAPlainOneDoesNot()
        {
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>());
            var watcher = context.CreateAdditionalClient(2);
            Clear(context, watcher);

            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 48 });
            Assert.IsEmpty(MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().ToArray(), "/logosfist without flag 388.");

            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 2 });
            Assert.HasCount(1, MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().ToArray(), "/clap needs no flag.");

            Clear(context, watcher);
            context.Client.Player.PlayerFlags[388] = 1;
            GestureManager.Instance.RequestGesture(context.Client, new RequestGesturePacket { GestureId = 48 });
            Assert.AreEqual(48U, MissionTestContext.Drain(watcher).OfType<PerformWindupPacket>().Single().ActionArgId);

            Assert.IsTrue(Gestures.TryGetEmoteFlag(36, out var airGuitar));
            Assert.AreEqual(20000005U, airGuitar);
            Assert.IsFalse(Gestures.TryGetEmoteFlag(2, out _));
        }

        private static void Clear(MissionTestContext context, Client watcher)
        {
            context.Drain();
            MissionTestContext.Drain(watcher);
            context.Map.PerformRecovery.RemoveAll(action => action.Actor == context.Client.Player);
        }

        private static IEnumerable<DynamicObject> EmitterObjects(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.DynamicObjectType == DynamicObjectType.Emitter)
                .Distinct();

        /// <summary>Loads the item template's class the way the harness loads the Bootcamp loot (LoadBootcampLootContent).</summary>
        private static void LoadTemplate(BootcampRuntimeTestHarness.Harness harness, uint template)
        {
            var world = harness.WorldContext;
            var link = world.Set<ItemTemplateItemClassEntry>().AsNoTracking().Single(row => row.ItemTemplateId == template);
            var data = world.Set<ItemTemplateEntry>().AsNoTracking().Single(row => row.Id == template);
            var entry = world.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == link.ItemClass);
            var classId = (EntityClasses)entry.Id;
            EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var previous);
            var entityClass = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                entry.ClassCollisionRole, entry.AugList.Split(',')
                    .Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0)
            {
                ItemClassInfo = new ItemClassInfo(world.Set<ItemClassEntry>().AsNoTracking().Single(row => row.Id == link.ItemClass)),
                ItemTemplates = previous == null ? new Dictionary<uint, ItemTemplate>() : new Dictionary<uint, ItemTemplate>(previous.ItemTemplates)
            };
            entityClass.ItemTemplates[template] = new ItemTemplate(link)
            {
                QualityId = data.QualityId,
                InventoryCategory = (InventoryCategory)data.InventoryCategory,
                HasSellableFlag = data.HasSellableFlag != 0,
                BuyPrice = data.BuyPrice,
                SellPrice = data.SellPrice
            };
            EntityClassManager.Instance.LoadedEntityClasses[classId] = entityClass;
            ItemManager.Instance.ItemTemplateItemClass[template] = classId;
        }

        private static Item Grant(BootcampRuntimeTestHarness.Harness harness, uint template)
        {
            LoadTemplate(harness, template);
            using (var unit = harness.Context.CreateChar())
            using (var grant = new InventoryManager.InventoryGrant())
            {
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(template, 1) }, unit));
                grant.Publish(harness.Client);
            }

            return harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(candidate => candidate.ItemTemplateId == template);
        }

        private static void Land(BootcampRuntimeTestHarness.Harness harness, AbilityManager manager, ActionData pending)
        {
            harness.BootcampMap.PerformRecovery.Remove(pending);
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                manager.PerformRecovery(harness.BootcampMap, pending);
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        private static RequestPerformAbilityPacket Request(int actionId, uint level, ulong itemId)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt(actionId);
                writer.WriteInt((int)level);
                writer.WriteNoneStruct();
                writer.WriteULong(itemId);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestPerformAbilityPacket();
            packet.Read(reader);
            return packet;
        }

        /// <summary>An AbilityManager with the one action level loaded from the seeded world tables, and the item that performs it.</summary>
        private static AbilityManager CreateManager(BootcampRuntimeTestHarness.Harness harness, uint actionId, uint levelId, uint template)
        {
            var manager = (AbilityManager)Activator.CreateInstance(typeof(AbilityManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { harness.Context, harness.Manager }, null);
            var row = harness.WorldContext.Set<ActionEntry>().Single(entry => entry.Id == actionId);
            var level = harness.WorldContext.Set<ActionLevelEntry>().Single(entry => entry.ActionId == actionId && entry.Level == levelId);
            var action = new ActionInfo { ActionId = (ActionId)row.Id, Name = row.Name, Module = row.Module };
            var info = new ActionLevelInfo
            {
                ActionId = action.ActionId, Level = levelId, WindupMs = level.WindupMs,
                RecoveryMs = level.RecoveryMs, ReuseMs = level.ReuseMs, StartReuseOnPerform = level.StartReuseOnPerform != 0
            };
            foreach (var property in harness.WorldContext.ActionPropertyEntries.Where(entry => entry.ActionId == actionId && entry.Level == levelId))
                info.Properties[(AbilityProperty)property.PropertyId] = property.Value;
            foreach (var requirement in harness.WorldContext.Set<ActionItemRequirementEntry>().Where(entry => entry.ActionId == actionId && entry.Level == levelId))
                info.ItemRequirements.Add(new ActionItemRequirement { ItemClass = (EntityClasses)requirement.ItemClassId, Quantity = requirement.Quantity });
            action.Levels[levelId] = info;
            ((Dictionary<ActionId, ActionInfo>)typeof(AbilityManager).GetField("_actions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager))[action.ActionId] = action;
            ((Dictionary<uint, (ActionId ActionId, uint Level)>)typeof(AbilityManager).GetField("_itemTemplateActions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager))[template] = ((ActionId)actionId, levelId);
            return manager;
        }
    }
}
