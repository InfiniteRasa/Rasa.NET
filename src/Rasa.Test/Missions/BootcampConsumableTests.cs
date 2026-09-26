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
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampConsumableTests
    {
        [TestMethod]
        [DataRow(2U, false)]
        [DataRow(2U, true)]
        [DataRow(1U, false)]
        [DataRow(1U, true)]
        public void NativeMedpackRequestConsumesOneItemAndHealsAfterReconnect(uint count, bool reconnect)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using (var unit = harness.Context.CreateChar())
            using (var grant = new InventoryManager.InventoryGrant())
            {
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(44917, count) }, unit));
                grant.Publish(harness.Client);
            }
            if (reconnect)
                harness.ReconnectFresh();
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(id => EntityManager.Instance.GetItem(id))
                .Single(candidate => candidate.ItemTemplate.ItemTemplateId == 44917);
            var manager = CreateMedpackManager(harness);
            harness.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 100, 0, 0);
            var packet = ReadMedpackRequest(item.EntityId);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, packet);

            var pending = harness.BootcampMap.PerformRecovery.SingleOrDefault(action => action.ActionId == (ActionId)419);
            Assert.IsNotNull(pending, "A real medpack item must be accepted, not refused as an unsupported ability.");
            Assert.AreEqual(item.EntityId, pending.ItemId);
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                manager.PerformRecovery(harness.BootcampMap, pending);
                harness.BootcampMap.PerformRecovery.Remove(pending);
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
            Assert.AreEqual(count - 1, item.StackSize, "The source item also appears in action_item_requirement; consume it only once.");
            using (var unit = harness.Context.CreateChar())
            {
                if (count == 1)
                {
                    Assert.IsNull(unit.Items.GetItem(item.Id));
                    Assert.IsFalse(harness.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId));
                }
                else
                    Assert.AreEqual(count - 1, unit.Items.GetItem(item.Id).StackSize);
            }
            var effect = harness.Client.Player.ActiveEffects.Values.Single(candidate => candidate.TypeId == 280);
            Assert.AreEqual(1000, effect.TickIntervalMs);
            Assert.AreEqual(60, effect.TickHealMin);
            Assert.AreEqual(60, effect.TickHealMax);
            effect.NextTickTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(harness.BootcampMap, 1000);
            Assert.AreEqual(160, harness.Client.Player.Attributes[Attributes.Health].Current);
            Assert.IsTrue(harness.Drain().OfType<AbilityRecoveryPacket>().Any());
        }

        [TestMethod]
        [DataRow("database", 1U)]
        [DataRow("database", 2U)]
        [DataRow("moved", 2U)]
        [DataRow("interrupted", 2U)]
        public void FailedMedpackLandingDoesNotConsumeOrHeal(string failure, uint count)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using (var unit = harness.Context.CreateChar())
            using (var grant = new InventoryManager.InventoryGrant())
            {
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(44917, count) }, unit));
                grant.Publish(harness.Client);
            }
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(candidate => candidate.ItemTemplateId == 44917);
            var manager = CreateMedpackManager(harness);
            manager.RequestPerformAbility(harness.Client, ReadMedpackRequest(item.EntityId));
            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)419);
            if (failure == "database")
                harness.Context.AfterSave = _ => throw new DbUpdateException("Injected consumable save failure.");
            if (failure == "moved")
                harness.Client.Player.Inventory.PersonalInventory[(int)item.OwnerSlotId] = 0;
            if (failure == "interrupted")
                pending.IsInrerrupted = true;
            var health = harness.Client.Player.Attributes[Attributes.Health].Current;
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                manager.PerformRecovery(harness.BootcampMap, pending);
            }
            finally
            {
                harness.Context.AfterSave = null;
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
            using var verify = harness.Context.CreateChar();
            Assert.AreEqual(count, verify.Items.GetItem(item.Id).StackSize);
            Assert.AreEqual(count, item.StackSize);
            Assert.AreEqual(health, harness.Client.Player.Attributes[Attributes.Health].Current);
            Assert.IsFalse(harness.Client.Player.ActiveEffects.Values.Any(effect => effect.TypeId == 280));
            Assert.IsFalse(harness.Client.Player.ActionReuseUntil.ContainsKey((ActionId)419));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ForeignOrNonexistentSourceItemCannotAuthorizeAMedpack(bool highId)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using (var unit = harness.Context.CreateChar())
            using (var grant = new InventoryManager.InventoryGrant())
            {
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(44917, 1) }, unit));
                grant.Publish(harness.Client);
            }
            var item = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(candidate => candidate.ItemTemplateId == 44917);
            var source = highId ? (1UL << 32) | item.EntityId : item.EntityId;
            if (!highId)
                item.OwnerId++;
            var manager = CreateMedpackManager(harness);
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ReadMedpackRequest(source));

            Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == (ActionId)419));
            Assert.IsTrue(harness.Drain().OfType<UserActionFailedPacket>().Any());
            Assert.AreEqual(1U, item.StackSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AbilityIngredientsSkipProtectedFirstStackAndConsumeTheUnboundCost(bool clearRuntimeOwnership)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            void Grant(params InventoryManager.InventoryItemGrant[] items)
            {
                using var grant = new InventoryManager.InventoryGrant();
                using (var unit = harness.Context.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client, items, unit));
                grant.Publish(harness.Client);
            }
            Item[] Inventory() => harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).ToArray();
            Grant(new InventoryManager.InventoryItemGrant(28, 3));
            var protectedIngredient = Inventory().Single(item => item.ItemTemplateId == 28);
            var assignmentId = Guid.NewGuid().ToString("N");
            protectedIngredient.MissionOwnership = new MissionItemOwnership(harness.Client.Player.Id, 901, assignmentId, 1, "survey-ammo");
            using (var unit = harness.Context.CreateChar())
                unit.CharacterMissionItems.Save(new CharacterMissionItemEntry
                {
                    CharacterId = harness.Client.Player.Id, MissionId = 901, AssignmentId = assignmentId,
                    Generation = 1, ItemKey = "survey-ammo", ItemId = protectedIngredient.Id, Quantity = 3
                });
            Grant(new InventoryManager.InventoryItemGrant(44917, 2), new InventoryManager.InventoryItemGrant(28, 4));
            var source = Inventory().Single(item => item.ItemTemplateId == 44917);
            var unbound = Inventory().Single(item => item.ItemTemplateId == 28 && item != protectedIngredient);
            Assert.IsTrue(protectedIngredient.OwnerSlotId < unbound.OwnerSlotId);
            if (clearRuntimeOwnership)
                protectedIngredient.MissionOwnership = null;
            var manager = CreateMedpackManager(harness,
                new ActionItemRequirement { ItemClass = (EntityClasses)3147, Quantity = 2 });
            harness.Drain();

            manager.RequestPerformAbility(harness.Client, ReadMedpackRequest(source.EntityId));

            var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == (ActionId)419);
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try { manager.PerformRecovery(harness.BootcampMap, pending); }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }

            Assert.AreEqual(2U, unbound.StackSize, "Landing must choose the available unbound ingredients, not reject the protected first stack.");
            Assert.AreEqual(1U, source.StackSize, "The source also satisfies its own item requirement exactly once.");
            Assert.AreEqual(3U, protectedIngredient.StackSize);
            Assert.AreEqual(assignmentId, protectedIngredient.MissionOwnership.AssignmentId);
            Assert.IsTrue(harness.Client.Player.ActiveEffects.Values.Any(effect => effect.TypeId == 280));
            Assert.IsTrue(harness.Drain().OfType<AbilityRecoveryPacket>().Any());
            using var verify = harness.Context.CreateChar();
            Assert.AreEqual(2U, verify.Items.GetItem(unbound.Id).StackSize);
            Assert.AreEqual(1U, verify.Items.GetItem(source.Id).StackSize);
            Assert.AreEqual(3U, verify.Items.GetItem(protectedIngredient.Id).StackSize);
            Assert.AreEqual(assignmentId, verify.CharacterMissionItems.GetOwner(protectedIngredient.Id).AssignmentId);
            Assert.AreEqual(3U, verify.CharacterMissionItems.GetOwner(protectedIngredient.Id).Quantity);
        }

        private static RequestPerformAbilityPacket ReadMedpackRequest(ulong itemId)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt(419);
                writer.WriteInt(1);
                writer.WriteNoneStruct();
                writer.WriteULong(itemId);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestPerformAbilityPacket();
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return packet;
        }

        private static AbilityManager CreateMedpackManager(BootcampRuntimeTestHarness.Harness harness,
            params ActionItemRequirement[] additionalRequirements)
        {
            var manager = (AbilityManager)Activator.CreateInstance(typeof(AbilityManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { harness.Context, harness.Manager }, null);
            var row = harness.WorldContext.Set<ActionEntry>().Single(entry => entry.Id == 419);
            var level = harness.WorldContext.Set<ActionLevelEntry>().Single(entry => entry.ActionId == 419 && entry.Level == 1);
            var action = new ActionInfo { ActionId = (ActionId)row.Id, Name = row.Name, Module = row.Module };
            var info = new ActionLevelInfo
            {
                ActionId = action.ActionId, Level = 1, WindupMs = level.WindupMs,
                RecoveryMs = level.RecoveryMs, ReuseMs = level.ReuseMs, StartReuseOnPerform = level.StartReuseOnPerform != 0
            };
            foreach (var property in harness.WorldContext.ActionPropertyEntries.Where(entry => entry.ActionId == 419 && entry.Level == 1))
                info.Properties[(AbilityProperty)property.PropertyId] = property.Value;
            foreach (var requirement in harness.WorldContext.Set<ActionItemRequirementEntry>().Where(entry => entry.ActionId == 419 && entry.Level == 1))
                info.ItemRequirements.Add(new ActionItemRequirement { ItemClass = (EntityClasses)requirement.ItemClassId, Quantity = requirement.Quantity });
            info.ItemRequirements.AddRange(additionalRequirements);
            action.Levels[1] = info;
            ((Dictionary<ActionId, ActionInfo>)typeof(AbilityManager).GetField("_actions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager))[action.ActionId] = action;
            ((Dictionary<uint, (ActionId ActionId, uint Level)>)typeof(AbilityManager).GetField("_itemTemplateActions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager))[44917] = ((ActionId)419, 1);
            return manager;
        }
    }
}
