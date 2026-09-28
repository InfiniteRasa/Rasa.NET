using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Packets.Inventory.Server;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public partial class InventoryManager
    {
        internal const int PersonalCategorySize = 50;
        internal const int PersonalCategoryCount = 5;

        internal readonly struct InventoryItemGrant
        {
            internal uint ItemTemplateId { get; }
            internal uint Quantity { get; }
            internal InventoryItemGrant(uint itemTemplateId, uint quantity)
            { ItemTemplateId = itemTemplateId; Quantity = quantity; }
        }

        internal sealed class InventoryGrant : IDisposable
        {
            private readonly Action<Item> _beforeRegister;
            private InventoryPlan _plan;
            internal InventoryGrant(Action<Item> beforeRegister = null) => _beforeRegister = beforeRegister;

            internal void PlanAndSave(Client client, IReadOnlyList<InventoryItemGrant> grants, ICharUnitOfWork unit)
            {
                _plan = InventoryPlan.For(client, unit);
                foreach (var grant in grants)
                    _plan.Grant(grant.ItemTemplateId, grant.Quantity, beforeRegister: _beforeRegister);
            }

            internal void ConvergeRuntime(Client client) => _plan?.ConvergeRuntime(client);
            internal void Publish(Client client) => _plan?.Publish(client);
            public void Dispose() { }
        }

        // One shadow inventory belongs to the character transaction, not to each grant/consume.
        // It is flushed once, then the same final state is published by every participating caller.
        internal sealed class InventoryPlan : ITransactionParticipant
        {
            private sealed class Stack
            {
                internal int Index;
                internal int OriginalIndex;
                internal uint OriginalItemId;
                internal Item Existing;
                internal Item Staged;
                internal LootItem Incoming;
                internal ItemTemplate Template;
                internal uint Count;
                internal uint OriginalCount;
                internal uint Maximum;
                internal MissionItemOwnership Ownership;
                internal MissionItemOwnership OriginalOwnership;
                internal Action<Item> BeforeRegister;
                internal bool Changed => Count != OriginalCount || Existing != null && Index != OriginalIndex;
            }

            private readonly Client _client;
            private readonly Manifestation _player;
            private readonly uint _characterId;
            private readonly uint _accountId;
            private readonly ICharUnitOfWork _unit;
            private readonly List<Stack> _stacks = new();
            private readonly List<LootItem> _loot = new();
            private readonly ulong[] _originalSlots;
            private bool _committed;
            private bool _converged;
            private bool _published;

            internal static InventoryPlan For(Client client, ICharUnitOfWork unit)
            {
                var plan = unit.Enlist(() => new InventoryPlan(client, unit));
                if (!ReferenceEquals(plan._client, client))
                    throw new GameplayRejectionException("An inventory transaction cannot change character.");
                return plan;
            }

            private InventoryPlan(Client client, ICharUnitOfWork unit)
            {
                _client = client;
                _unit = unit;
                if (client?.Player == null || client.AccountEntry == null)
                    throw new GameplayRejectionException("Inventory character ownership changed.");
                _player = client.Player;
                _characterId = _player.Id;
                _accountId = client.AccountEntry.Id;
                if (unit.Characters.Get(_characterId)?.AccountId != _accountId)
                    throw new GameplayRejectionException("Inventory character ownership changed.");
                RequireCharacter();
                var inventory = client.Player.Inventory.PersonalInventory;
                if (inventory.Count != PersonalCategorySize * PersonalCategoryCount)
                    throw new GameplayRejectionException("Personal inventory is not initialized.");
                _originalSlots = inventory.ToArray();
                foreach (var (entityId, index) in inventory.Select((id, slot) => (id, slot)).Where(entry => entry.id != 0))
                {
                    var item = EntityManager.Instance.GetItem(entityId);
                    if (item?.ItemTemplate == null)
                        throw new GameplayRejectionException("An inventory entity is missing its template.");
                    var template = RequireTemplate(item.ItemTemplate.ItemTemplateId);
                    _stacks.Add(new Stack
                    {
                        Index = index, OriginalIndex = index, OriginalItemId = item.Id,
                        Existing = item, Template = template, Count = item.StackSize,
                        OriginalCount = item.StackSize, Ownership = item.MissionOwnership, OriginalOwnership = item.MissionOwnership,
                        Maximum = EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize
                    });
                }
                ValidateSnapshot(initializeOwnership: true);
            }

            private void ValidateSnapshot(bool initializeOwnership = false)
            {
                RequireCharacter();
                var player = _client.Player;
                if (!_originalSlots.SequenceEqual(player.Inventory.PersonalInventory))
                    throw new GameplayRejectionException("Runtime inventory slots changed during planning.");
                var rows = _unit.CharacterInventories.GetItems(_client.AccountEntry.Id);
                var personalRows = rows.Where(row => row.CharacterId == player.Id &&
                    row.InventoryType == (uint)InventoryType.Personal).ToArray();
                if (personalRows.Any(row => row.SlotId >= _originalSlots.Length) ||
                    personalRows.Select(row => row.SlotId).Distinct().Count() != personalRows.Length)
                    throw new GameplayRejectionException("Persisted personal inventory contains invalid or duplicate slots.");
                var personal = personalRows.ToDictionary(row => row.SlotId);
                var owners = _unit.CharacterMissionItems.GetOwned(player.Id);
                var original = _stacks.Where(stack => stack.Existing != null).ToDictionary(stack => stack.OriginalIndex);
                var itemIds = original.Values.Select(stack => stack.OriginalItemId).Distinct().ToArray();
                var savedItems = _unit.Items.GetItems(itemIds).ToDictionary(item => item.ItemId);
                var itemOwners = _unit.CharacterMissionItems.GetOwners(itemIds).ToDictionary(owner => owner.ItemId);
                var rowCounts = rows.GroupBy(row => row.ItemId).ToDictionary(group => group.Key, group => group.Count());
                var seen = new HashSet<uint>();
                for (var index = 0; index < _originalSlots.Length; index++)
                {
                    personal.TryGetValue((uint)index, out var row);
                    original.TryGetValue(index, out var slot);
                    if (slot == null)
                    {
                        if (row != null)
                            throw new GameplayRejectionException("An empty runtime slot is occupied in storage.");
                        continue;
                    }
                    var item = slot.Existing;
                    if (item.Id == 0 || item.Id != slot.OriginalItemId || !seen.Add(item.Id) ||
                        EntityManager.Instance.GetEntityType(item.EntityId) != EntityType.Item ||
                        !ReferenceEquals(EntityManager.Instance.GetItem(item.EntityId), item) ||
                        item.OwnerId != player.Id || item.OwnerSlotId != index || row?.ItemId != item.Id ||
                        rowCounts.GetValueOrDefault(item.Id) != 1)
                        throw new GameplayRejectionException("Runtime inventory no longer matches its durable owner/slot.");
                    savedItems.TryGetValue(item.Id, out var saved);
                    if (item.ItemTemplate?.ItemTemplateId != slot.Template.ItemTemplateId ||
                        item.ItemTemplate.Class != slot.Template.Class ||
                        saved?.ItemTemplateId != slot.Template.ItemTemplateId ||
                        saved.StackSize != slot.OriginalCount || item.StackSize != slot.OriginalCount ||
                        slot.OriginalCount == 0 || slot.OriginalCount > slot.Maximum ||
                        (int)slot.Template.InventoryCategory - 1 != index / PersonalCategorySize)
                        throw new GameplayRejectionException("An inventory stack changed or has invalid template/count data.");
                    itemOwners.TryGetValue(item.Id, out var owner);
                    if (initializeOwnership && slot.Ownership == null && owner != null)
                        slot.Ownership = ToOwnership(owner);
                    if (item.MissionOwnership != slot.OriginalOwnership ||
                        slot.OriginalOwnership != null && slot.OriginalOwnership != slot.Ownership ||
                        owner == null && slot.Ownership != null ||
                        owner != null && (owner.CharacterId != player.Id || owner.MissionId == 0 ||
                            string.IsNullOrWhiteSpace(owner.AssignmentId) || owner.AssignmentId.Length != 32 ||
                            owner.Generation == 0 || string.IsNullOrWhiteSpace(owner.ItemKey) ||
                            owner.Quantity != slot.OriginalCount ||
                            ToOwnership(owner) != slot.Ownership))
                        throw new GameplayRejectionException("Assignment inventory provenance changed.");
                }
                if (owners.Any(owner => !seen.Contains(owner.ItemId)))
                    throw new GameplayRejectionException("An assignment-owned item is missing from personal inventory.");
                foreach (var loot in _loot)
                    ValidateLoot(loot, rows);
            }

            internal static MissionItemOwnership ToOwnership(CharacterMissionItemEntry entry) =>
                new(entry.CharacterId, entry.MissionId, entry.AssignmentId, entry.Generation, entry.ItemKey);

            internal uint OwnedQuantity(MissionItemOwnership owner) =>
                checked((uint)_stacks.Where(stack => stack.Ownership == owner).Sum(stack => (long)stack.Count));

            internal void RequireAssignment(uint missionId, string assignmentId, uint generation,
                IReadOnlyDictionary<string, Rasa.Missions.Definitions.MissionItemBinding> bindings)
            {
                var stacks = _stacks.Where(stack => stack.Count > 0 && stack.Ownership?.AssignmentId == assignmentId).ToArray();
                foreach (var stack in stacks)
                {
                    var owner = stack.Ownership;
                    if (owner.CharacterId != _client.Player.Id || owner.MissionId != missionId || owner.Generation != generation ||
                        !bindings.TryGetValue(owner.ItemKey, out var binding) ||
                        binding.Scope != Rasa.Missions.Scenes.MissionItemScope.AssignmentIssued ||
                        binding.ItemTemplateId != stack.Template.ItemTemplateId)
                        throw new GameplayRejectionException("Mission item ownership no longer matches its assignment, generation or authored binding.");
                }
                foreach (var group in stacks.GroupBy(stack => stack.Ownership.ItemKey))
                    if (group.Sum(stack => (long)stack.Count) > bindings[group.Key].MaximumQuantity)
                        throw new GameplayRejectionException("Mission item ownership exceeds its authored quantity.");
            }

            internal void Remove(MissionItemOwnership owner)
            {
                if (owner == null)
                    throw new GameplayRejectionException("Cleanup requires assignment provenance.");
                foreach (var stack in _stacks.Where(stack => stack.Ownership == owner))
                    stack.Count = 0;
            }

            internal void Consume(uint templateId, uint quantity, MissionItemOwnership owner)
            {
                var available = _stacks.Where(stack => stack.Template.ItemTemplateId == templateId &&
                    stack.Ownership == owner && stack.Count > 0).OrderBy(stack => stack.Index).ToArray();
                if (quantity == 0 || available.Sum(stack => (long)stack.Count) < quantity)
                    throw new GameplayRejectionException("The authored item cost is not present in its required ownership scope.");
                var remaining = quantity;
                foreach (var stack in available)
                {
                    var consumed = Math.Min(remaining, stack.Count);
                    stack.Count -= consumed;
                    remaining -= consumed;
                    if (remaining == 0)
                        break;
                }
            }

            internal MissionProgressEvent ConsumeEntity(ulong entityId, uint quantity)
            {
                var stack = _stacks.SingleOrDefault(entry => entry.Existing?.EntityId == entityId);
                if (stack == null || quantity == 0 || stack.Count < quantity || stack.Ownership != null)
                    throw new GameplayRejectionException("Consumable ownership, slot or quantity changed, or the item belongs to an assignment.");
                stack.Count -= quantity;
                return MissionProgressEvent.ItemConsumed((uint)stack.Template.Class, quantity);
            }

            internal void Move(uint source, uint destination)
            {
                var from = _stacks.SingleOrDefault(stack => stack.Index == source && stack.Count > 0);
                var to = _stacks.SingleOrDefault(stack => stack.Index == destination && stack.Count > 0);
                if (source >= _originalSlots.Length || destination >= _originalSlots.Length || from == null ||
                    destination / PersonalCategorySize != (uint)from.Template.InventoryCategory - 1 ||
                    to != null && source / PersonalCategorySize != (uint)to.Template.InventoryCategory - 1)
                    throw new GameplayRejectionException("Items can move only within their personal inventory category.");
                from.Index = (int)destination;
                if (to != null)
                    to.Index = (int)source;
            }

            internal void Grant(uint templateId, uint quantity, MissionItemOwnership owner = null,
                Action<Item> beforeRegister = null)
            {
                if (quantity == 0)
                    throw new GameplayRejectionException("Invalid inventory item grant quantity.");
                var template = RequireTemplate(templateId);
                var maximum = EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize;
                var start = ((int)template.InventoryCategory - 1) * PersonalCategorySize;
                var remaining = quantity;
                foreach (var stack in _stacks.Where(stack => stack.Template.ItemTemplateId == templateId &&
                    stack.Ownership == owner && stack.Count > 0))
                {
                    var added = Math.Min(remaining, stack.Maximum - stack.Count);
                    stack.Count += added;
                    remaining -= added;
                    if (remaining == 0)
                        break;
                }
                for (var index = start; index < start + PersonalCategorySize && remaining > 0; index++)
                {
                    if (_stacks.Any(stack => stack.Index == index && stack.Count > 0))
                        continue;
                    var added = Math.Min(remaining, maximum);
                    _stacks.Add(new Stack { Index = index, Template = template, Count = added, Maximum = maximum,
                        Ownership = owner, BeforeRegister = beforeRegister });
                    remaining -= added;
                }
                if (remaining != 0)
                    throw new GameplayRejectionException("The complete item grant does not fit its inventory categories.");
            }

            private void ValidateLoot(LootItem loot, IReadOnlyList<CharacterInventoryEntry> rows)
            {
                var item = loot?.Item;
                if (item?.ItemTemplate == null || item.Id == 0 || item.StackSize == 0 || loot.Taken ||
                    loot.EntityId != item.EntityId || loot.ActorId != _client.Player.EntityId || loot.PartyId != 0 ||
                    EntityManager.Instance.GetEntityType(item.EntityId) != EntityType.Item ||
                    !ReferenceEquals(EntityManager.Instance.GetItem(item.EntityId), item) ||
                    loot.ItemTemplateId != item.ItemTemplate.ItemTemplateId || loot.ItemClassId != (uint)item.ItemTemplate.Class ||
                    loot.ItemQuantity != item.StackSize || rows.Any(row => row.ItemId == item.Id) ||
                    Game.Missions.Persistence.MissionItemProtection.IsProtected(item, _unit))
                    throw new GameplayRejectionException("Loot item identity or ownership is invalid.");
                var template = RequireTemplate(item.ItemTemplate.ItemTemplateId);
                var saved = _unit.Items.GetItem(item.Id);
                if (saved?.ItemTemplateId != template.ItemTemplateId || saved.StackSize != item.StackSize ||
                    item.StackSize > EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize)
                    throw new GameplayRejectionException("Loot item durable state is stale.");
            }

            internal void AcceptLoot(LootItem loot, uint? preferredSlot, Action<Item> beforePublication)
            {
                ValidateLoot(loot, _unit.CharacterInventories.GetItems(_client.AccountEntry.Id));
                if (_loot.Any(entry => entry.EntityId == loot.EntityId || entry.Item.Id == loot.Item.Id))
                    throw new GameplayRejectionException("The same loot item cannot be planned twice.");
                _loot.Add(loot);
                var template = RequireTemplate(loot.ItemTemplateId);
                var start = ((int)template.InventoryCategory - 1) * PersonalCategorySize;
                var maximum = EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize;
                var remaining = loot.ItemQuantity;
                foreach (var stack in _stacks.Where(stack => stack.Template.ItemTemplateId == loot.ItemTemplateId &&
                    stack.Ownership == null && stack.Count > 0))
                {
                    var added = Math.Min(remaining, stack.Maximum - stack.Count);
                    stack.Count += added;
                    remaining -= added;
                    if (remaining == 0)
                        break;
                }
                if (remaining == 0)
                    return;
                var available = Enumerable.Range(start, PersonalCategorySize)
                    .Where(index => !_stacks.Any(stack => stack.Index == index && stack.Count > 0)).ToArray();
                if (available.Length == 0)
                    throw new GameplayRejectionException("The complete loot batch does not fit its inventory category.");
                var destination = preferredSlot.HasValue && available.Contains((int)preferredSlot.Value)
                    ? (int)preferredSlot.Value : available[0];
                _stacks.Add(new Stack { Index = destination, Incoming = loot, Template = template,
                    Count = remaining, Maximum = maximum, BeforeRegister = beforePublication });
            }

            public void Prepare()
            {
                ValidateSnapshot();
                foreach (var stack in _stacks.Where(stack => stack.Existing != null && stack.Count == 0))
                {
                    _unit.CharacterMissionItems.Remove(stack.Existing.Id);
                    _unit.CharacterInventories.DeleteInvItemByItemId(stack.Existing.Id);
                    _unit.Items.DeleteItem(stack.Existing.Id);
                }
                foreach (var loot in _loot.Where(loot => !_stacks.Any(stack => stack.Incoming == loot && stack.Count > 0)))
                    _unit.Items.DeleteItem(loot.Item.Id);
                foreach (var stack in _stacks.Where(stack => stack.Count > 0 && stack.Changed))
                {
                    var item = stack.Existing;
                    if (item != null)
                    {
                        if (stack.Ownership != null)
                            _unit.CharacterMissionItems.Remove(item.Id);
                        if (stack.Count != stack.OriginalCount)
                            _unit.Items.UpdateItemStackSize(new Item(0, stack.Count, 0, 0) { Id = item.Id });
                        if (stack.Index != stack.OriginalIndex)
                            _unit.CharacterInventories.MoveInvItem(_client.AccountEntry.Id, _client.Player.Id,
                                (uint)InventoryType.Personal, (uint)stack.Index, item.Id);
                    }
                    else
                    {
                        if (stack.Incoming != null)
                        {
                            item = stack.Incoming.Item;
                            if (item.StackSize != stack.Count)
                                _unit.Items.UpdateItemStackSize(new Item(0, stack.Count, 0, 0) { Id = item.Id });
                        }
                        else
                        {
                            item = stack.Staged = ItemManager.StageItem(stack.Template, stack.Count, "");
                            item.OwnerId = _client.Player.Id;
                            item.OwnerSlotId = (uint)stack.Index;
                            item.MissionOwnership = stack.Ownership;
                            item.Id = _unit.Items.CreateItem(item);
                        }
                        if (item.Id == 0)
                            throw new GameplayRejectionException("Item creation returned an invalid durable identity.");
                        _unit.CharacterInventories.AddInvItem(_client.AccountEntry.Id, _client.Player.Id,
                            (uint)InventoryType.Personal, (uint)stack.Index, item.Id);
                    }
                    if (stack.Ownership is { } owner)
                        _unit.CharacterMissionItems.Save(new CharacterMissionItemEntry
                        {
                            CharacterId = owner.CharacterId, MissionId = owner.MissionId, AssignmentId = owner.AssignmentId,
                            Generation = owner.Generation, ItemKey = owner.ItemKey, ItemId = item.Id, Quantity = stack.Count
                        });
                }
            }

            public void Committed() => _committed = true;

            public void Validate()
            {
                RequireCharacter();
                if (!_originalSlots.SequenceEqual(_client.Player.Inventory.PersonalInventory) ||
                    _unit.Characters.Get(_client.Player.Id)?.AccountId != _client.AccountEntry.Id ||
                    _stacks.Where(stack => stack.Existing != null).Any(stack =>
                        stack.Existing.Id != stack.OriginalItemId || stack.Existing.StackSize != stack.OriginalCount ||
                        stack.Existing.OwnerId != _client.Player.Id || stack.Existing.OwnerSlotId != stack.OriginalIndex ||
                        stack.Existing.MissionOwnership != stack.OriginalOwnership ||
                        !ReferenceEquals(EntityManager.Instance.GetItem(stack.Existing.EntityId), stack.Existing)))
                    throw new GameplayRejectionException("Runtime inventory changed during final persistence.");
                var rows = _unit.CharacterInventories.GetItems(_client.AccountEntry.Id);
                var personal = rows.Where(row => row.CharacterId == _client.Player.Id &&
                    row.InventoryType == (uint)InventoryType.Personal).ToArray();
                var final = _stacks.Where(stack => stack.Count > 0).ToArray();
                if (personal.Length != final.Length || personal.Select(row => row.SlotId).Distinct().Count() != personal.Length)
                    throw new GameplayRejectionException("Final inventory slots do not match the transaction plan.");
                var removed = _stacks.Where(stack => stack.Existing != null && stack.Count == 0)
                    .Select(stack => stack.Existing.Id).Concat(_loot.Where(loot => !final.Any(stack => stack.Incoming == loot))
                        .Select(loot => loot.Item.Id)).Distinct().ToArray();
                var itemIds = final.Select(stack => (stack.Existing ?? stack.Incoming?.Item ?? stack.Staged).Id)
                    .Concat(removed).Distinct().ToArray();
                var savedItems = _unit.Items.GetItems(itemIds).ToDictionary(item => item.ItemId);
                var itemOwners = _unit.CharacterMissionItems.GetOwners(itemIds).ToDictionary(owner => owner.ItemId);
                var personalBySlot = personal.ToDictionary(row => row.SlotId);
                var rowCounts = rows.GroupBy(row => row.ItemId).ToDictionary(group => group.Key, group => group.Count());
                foreach (var stack in final)
                {
                    var item = stack.Existing ?? stack.Incoming?.Item ?? stack.Staged;
                    personalBySlot.TryGetValue((uint)stack.Index, out var row);
                    savedItems.TryGetValue(item.Id, out var saved);
                    itemOwners.TryGetValue(item.Id, out var owner);
                    if (row?.ItemId != item.Id || rowCounts.GetValueOrDefault(item.Id) != 1 ||
                        saved?.ItemTemplateId != stack.Template.ItemTemplateId || saved.StackSize != stack.Count ||
                        (owner == null ? stack.Ownership != null : owner.Quantity != stack.Count || ToOwnership(owner) != stack.Ownership))
                        throw new GameplayRejectionException("Final inventory identity, count or provenance does not match the transaction plan.");
                }
                if (_unit.CharacterMissionItems.GetOwned(_client.Player.Id).Count != final.Count(stack => stack.Ownership != null))
                    throw new GameplayRejectionException("Final assignment ledger does not match the inventory plan.");
                foreach (var itemId in removed)
                    if (savedItems.ContainsKey(itemId) || itemOwners.ContainsKey(itemId) || rowCounts.ContainsKey(itemId))
                        throw new GameplayRejectionException("A consumed item survived final persistence.");
            }

            internal void ConvergeRuntime(Client client)
            {
                if (_converged)
                    return;
                if (!_committed || !ReferenceEquals(client, _client))
                    throw new InvalidOperationException("Only a committed inventory plan can be published to its character.");
                RequireCharacter();
                _converged = true;
                foreach (var loot in _loot)
                {
                    loot.Taken = true;
                    if (!_stacks.Any(stack => stack.Incoming == loot && stack.Count > 0))
                        EntityManager.Instance.ReleaseEntity(loot.EntityId, EntityType.Item);
                }
                foreach (var stack in _stacks.Where(stack => stack.Existing != null &&
                    (stack.Count == 0 || stack.Index != stack.OriginalIndex)))
                {
                    client.Player.Inventory.PersonalInventory[stack.OriginalIndex] = 0;
                    if (stack.Count == 0)
                    {
                        stack.Existing.StackSize = 0;
                        EntityManager.Instance.ReleaseEntity(stack.Existing.EntityId, EntityType.Item);
                    }
                }
                foreach (var stack in _stacks.Where(stack => stack.Existing != null && stack.Count > 0))
                    stack.Existing.MissionOwnership = stack.Ownership;
                foreach (var stack in _stacks.Where(stack => stack.Count > 0 && stack.Changed))
                {
                    if (stack.Existing != null)
                    {
                        stack.Existing.StackSize = stack.Count;
                        stack.Existing.OwnerSlotId = (uint)stack.Index;
                        client.Player.Inventory.PersonalInventory[stack.Index] = stack.Existing.EntityId;
                        continue;
                    }
                    var item = stack.Incoming?.Item ?? stack.Staged;
                    item.OwnerId = client.Player.Id;
                    item.OwnerSlotId = (uint)stack.Index;
                    item.StackSize = stack.Count;
                    if (stack.Incoming == null)
                    {
                        EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                        EntityManager.Instance.RegisterItem(item.EntityId, item);
                    }
                    client.Player.Inventory.PersonalInventory[stack.Index] = item.EntityId;
                    MissionApplication.TryPublish(() => stack.BeforeRegister?.Invoke(item),
                        $"inventory item {item.Id} publication hook");
                }
            }

            internal void Publish(Client client)
            {
                ConvergeRuntime(client);
                if (_published)
                    return;
                _published = true;
                foreach (var stack in _stacks.Where(stack => stack.Changed))
                {
                    var item = stack.Existing ?? stack.Incoming?.Item ?? stack.Staged;
                    if (stack.Count == 0)
                    {
                        MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                            new InventoryRemoveItemPacket(InventoryType.Personal, item.EntityId)), $"item {item.Id} inventory removal");
                        MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientMethodId,
                            new DestroyPhysicalEntityPacket(item.EntityId)), $"item {item.Id} entity removal");
                    }
                    else if (stack.Existing != null)
                    {
                        if (stack.Index != stack.OriginalIndex)
                        {
                            MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                                new InventoryRemoveItemPacket(InventoryType.Personal, item.EntityId)), $"item {item.Id} old slot");
                            MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                                new InventoryAddItemPacket(InventoryType.Personal, item.EntityId, (uint)stack.Index)), $"item {item.Id} new slot");
                        }
                        MissionApplication.TryPublish(() => client.CallMethod(item.EntityId,
                            new SetStackCountPacket(stack.Count)), $"item {item.Id} count");
                    }
                    else
                    {
                        MissionApplication.TryPublish(() => ItemManager.Instance.SendItemDataToClient(client, item, false),
                            $"item {item.Id} entity data");
                        MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                            new InventoryAddItemPacket(InventoryType.Personal, item.EntityId, (uint)stack.Index)), $"item {item.Id} slot");
                    }
                }
            }

            internal static ItemTemplate RequireTemplate(uint id)
            {
                if (!ItemManager.Instance.ItemTemplateItemClass.TryGetValue(id, out var classId) ||
                    !EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var info) ||
                    info.ItemClassInfo == null || info.ItemClassInfo.StackSize == 0 ||
                    !info.ItemTemplates.TryGetValue(id, out var template) || template == null ||
                    template.ItemTemplateId != id || template.Class != classId || template.ItemInfo == null ||
                    (int)template.InventoryCategory < 1 || (int)template.InventoryCategory > PersonalCategoryCount ||
                    template.WeaponInfo != null && info.WeaponClassInfo == null)
                    throw new GameplayRejectionException($"Missing or invalid inventory template {id}.");
                return template;
            }

            public void Dispose()
            {
                if (!_committed)
                    foreach (var stack in _stacks.Where(stack => stack.Staged != null))
                        EntityManager.Instance.FreeEntity(stack.Staged.EntityId);
            }

            private void RequireCharacter()
            {
                if (!ReferenceEquals(_client.Player, _player) || _player.Id != _characterId || _client.AccountEntry?.Id != _accountId)
                    throw new GameplayRejectionException("Inventory character identity changed during the transaction.");
            }
        }
    }
}
