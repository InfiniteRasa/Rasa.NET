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

            /// <summary>The modules each item of the grant is made with, by slot; null for none.</summary>
            internal IReadOnlyList<uint> ModuleIds { get; }

            internal InventoryItemGrant(uint itemTemplateId, uint quantity, IReadOnlyList<uint> moduleIds = null)
            { ItemTemplateId = itemTemplateId; Quantity = quantity; ModuleIds = moduleIds; }
        }

        internal sealed class InventoryGrant : IDisposable
        {
            private readonly Action<Item> _beforeRegister;
            private InventoryPlan _plan;
            internal InventoryGrant(Action<Item> beforeRegister = null) => _beforeRegister = beforeRegister;

            internal void PlanAndSave(Client client, IReadOnlyList<InventoryItemGrant> grants, ICharUnitOfWork unit)
            {
                _plan = InventoryPlan.For(client, unit);

                // Reward items: what the backpack cannot take goes to the inbox rather than
                // refusing the turn-in.
                foreach (var grant in grants)
                    _plan.Grant(grant.ItemTemplateId, grant.Quantity, beforeRegister: _beforeRegister, overflowToInbox: true,
                        modules: grant.ModuleIds);
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

                /// <summary>For a stack split off another (Move with a quantity): the item whose crafter, colour, condition, binding and modules the new one carries.</summary>
                internal Item SplitFrom;

                /// <summary>For a new stack (Grant with a crafter): the name it is made with.</summary>
                internal string Crafter;

                /// <summary>For a new stack (Grant with modules): the modules it is made with, by slot; null for none.</summary>
                internal IReadOnlyList<uint> Modules;

                /// <summary>The modules the stack's item has or will have, by slot; null or all 0 for none.</summary>
                internal IReadOnlyList<uint> ModulesHeld => Existing != null ? Existing.ModuleIds : Incoming != null ? Incoming.Item?.ModuleIds
                    : SplitFrom != null ? SplitFrom.ModuleIds : Modules;

                /// <summary>The maker's name the stack's item has or will have: "" for none.</summary>
                internal string MadeBy => (Existing != null ? Existing.Crafter : Incoming != null ? Incoming.Item?.Crafter
                    : SplitFrom != null ? SplitFrom.Crafter : Crafter) ?? "";
                internal bool Changed => Count != OriginalCount || Existing != null && Index != OriginalIndex;
            }

            /// <summary>A new stack that did not fit the backpack, for the inbox slot given (overflowToInbox).</summary>
            private sealed class InboxStack
            {
                internal ItemTemplate Template;
                internal uint Count;
                internal uint Slot;
                internal Item Staged;
                internal Action<Item> BeforeRegister;
                internal string Crafter;
                internal IReadOnlyList<uint> Modules;
            }

            private readonly List<InboxStack> _inbox = new();
            private List<uint> _freeInboxSlots;

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

            internal uint UnownedQuantity(uint templateId) =>
                checked((uint)_stacks.Where(stack => stack.Ownership == null && stack.Template.ItemTemplateId == templateId)
                    .Sum(stack => (long)stack.Count));

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

            /// <summary>
            /// A pack move (PersonalInventory_MoveItem), of the whole stack or of
            /// <paramref name="quantity"/> of it - the client's quantity chooser drags part of a
            /// stack, and it sends the stack's count for a plain drag (inventory.AddItemToPersonalInventory).
            /// Onto a stack it can join (MayJoin) it goes into that stack, as much as fits, and the
            /// rest stays where it was. Otherwise the whole stack moves, or swaps with what is
            /// there; part of a stack goes only to an empty slot, as a new stack carrying what the
            /// old one carries. 0 is the whole stack.
            /// </summary>
            internal void Move(uint source, uint destination, uint quantity = 0)
            {
                var from = _stacks.SingleOrDefault(stack => stack.Index == source && stack.Count > 0);
                var to = _stacks.SingleOrDefault(stack => stack.Index == destination && stack.Count > 0);
                if (source >= _originalSlots.Length || destination >= _originalSlots.Length || from == null ||
                    destination / PersonalCategorySize != (uint)from.Template.InventoryCategory - 1 ||
                    to != null && source / PersonalCategorySize != (uint)to.Template.InventoryCategory - 1)
                    throw new GameplayRejectionException("Items can move only within their personal inventory category.");

                var whole = quantity == 0 || quantity >= from.Count;
                var amount = whole ? from.Count : quantity;

                if (to != null && MayJoin(from, to))
                {
                    var joined = Math.Min(amount, to.Maximum - to.Count);

                    if (joined > 0)
                    {
                        to.Count += joined;
                        from.Count -= joined;
                        return;
                    }
                }

                if (whole)
                {
                    from.Index = (int)destination;
                    if (to != null)
                        to.Index = (int)source;
                    return;
                }

                if (to != null)
                    throw new GameplayRejectionException("Part of a stack goes only to an empty slot or onto a stack it can join.");

                from.Count -= amount;
                _stacks.Add(new Stack
                {
                    Index = (int)destination, Template = from.Template, Count = amount, Maximum = from.Maximum,
                    Ownership = from.Ownership, SplitFrom = from.Existing
                });
            }

            /// <summary>
            /// Whether one stack can go into another: the same template, a stack of more than one,
            /// not Character Unique (two of them in one stack would be two held), the same
            /// assignment (or none), and two items alike in everything an item keeps of its own -
            /// crafter, colour, binding, modules - so that nothing is lost or gained by the one
            /// becoming part of the other.
            /// </summary>
            internal static bool MayJoin(Item from, Item to, uint maximum) =>
                from != null && to != null && maximum > 1 &&
                from.ItemTemplateId == to.ItemTemplateId &&
                !(from.ItemTemplate?.HasCharacterUniqueFlag ?? false) &&
                from.MissionOwnership == to.MissionOwnership &&
                from.BoundCharacterId == to.BoundCharacterId &&
                (from.Crafter ?? "") == (to.Crafter ?? "") &&
                from.Color == to.Color &&
                from.ModuleIds.SequenceEqual(to.ModuleIds);

            private static bool MayJoin(Stack from, Stack to) =>
                !ReferenceEquals(from, to) && from.Template.ItemTemplateId == to.Template.ItemTemplateId &&
                from.Ownership == to.Ownership && MayJoin(from.Existing, to.Existing, to.Maximum);

            /// <summary>
            /// New items of a template, all of them or none: onto stacks of it first, then into free
            /// slots of its category, then (overflowToInbox) the inbox. With a crafter, they are made
            /// with that name and join only stacks that carry the same one; without, any stack of
            /// the template.
            /// </summary>
            internal void Grant(uint templateId, uint quantity, MissionItemOwnership owner = null,
                Action<Item> beforeRegister = null, bool overflowToInbox = false, string crafter = null,
                IReadOnlyList<uint> modules = null)
            {
                if (quantity == 0)
                    throw new GameplayRejectionException("Invalid inventory item grant quantity.");
                var template = RequireTemplate(templateId);
                var maximum = EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize;
                var start = ((int)template.InventoryCategory - 1) * PersonalCategorySize;
                var remaining = quantity;
                // Only onto a stack of items with the same modules: a reward's Titan Rifle does not
                // join a plain one, nor a plain one a Titan.
                foreach (var stack in _stacks.Where(stack => stack.Template.ItemTemplateId == templateId &&
                    stack.Ownership == owner && stack.Count > 0 && (crafter == null || stack.MadeBy == crafter) &&
                    SameModules(stack.ModulesHeld, modules)))
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
                        Ownership = owner, BeforeRegister = beforeRegister, Crafter = crafter, Modules = modules });
                    remaining -= added;
                }

                // Never an item bound to an assignment: those have to be carried.
                if (remaining != 0 && overflowToInbox && owner == null)
                {
                    _freeInboxSlots ??= FreeInboxSlots(_unit, _accountId, _characterId);

                    while (remaining > 0 && _freeInboxSlots.Count > 0)
                    {
                        var added = Math.Min(remaining, maximum);
                        _inbox.Add(new InboxStack { Template = template, Count = added, Slot = _freeInboxSlots[0],
                            BeforeRegister = beforeRegister, Crafter = crafter, Modules = modules });
                        _freeInboxSlots.RemoveAt(0);
                        remaining -= added;
                    }
                }

                if (remaining != 0)
                    throw new GameplayRejectionException(overflowToInbox
                        ? "The complete item grant fits neither its inventory categories nor the inbox."
                        : "The complete item grant does not fit its inventory categories.");
            }

            /// <summary>Puts the modules given, by slot, in a new item's slots.</summary>
            private static void SetModules(Item item, IReadOnlyList<uint> modules)
            {
                for (var slot = 0; slot < ItemModules.Slots && slot < modules.Count; slot++)
                    item.SetModule(slot, modules[slot]);
            }

            /// <summary>Whether two items' module slots hold the same modules; null is four empty slots.</summary>
            private static bool SameModules(IReadOnlyList<uint> first, IReadOnlyList<uint> second)
            {
                for (var slot = 0; slot < ItemModules.Slots; slot++)
                {
                    var a = first != null && slot < first.Count ? first[slot] : 0;
                    var b = second != null && slot < second.Count ? second[slot] : 0;

                    if (a != b)
                        return false;
                }

                return true;
            }

            /// <summary>
            /// How many of a template a Grant with the same crafter could take: room on its stacks
            /// and in free slots of its category, and with overflowToInbox the free inbox slots.
            /// </summary>
            internal uint GrantCapacity(uint templateId, string crafter = null, bool overflowToInbox = false)
            {
                var template = RequireTemplate(templateId);
                var maximum = EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize;
                var start = ((int)template.InventoryCategory - 1) * PersonalCategorySize;
                var free = Enumerable.Range(start, PersonalCategorySize)
                    .Count(index => !_stacks.Any(stack => stack.Index == index && stack.Count > 0));
                var capacity = (ulong)free * maximum;
                foreach (var stack in _stacks.Where(stack => stack.Template.ItemTemplateId == templateId &&
                    stack.Ownership == null && stack.Count > 0 && (crafter == null || stack.MadeBy == crafter) &&
                    SameModules(stack.ModulesHeld, null)))
                    capacity += stack.Maximum - stack.Count;
                if (overflowToInbox)
                {
                    _freeInboxSlots ??= FreeInboxSlots(_unit, _accountId, _characterId);
                    capacity += (ulong)_freeInboxSlots.Count * maximum;
                }
                return (uint)Math.Min(uint.MaxValue, capacity);
            }

            private void ValidateLoot(LootItem loot, IReadOnlyList<CharacterInventoryEntry> rows)
            {
                var item = loot?.Item;
                if (item?.ItemTemplate == null || item.Id == 0 || item.StackSize == 0 || loot.Taken ||
                    loot.EntityId != item.EntityId || !loot.MayTake(_client.Player.EntityId) ||
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

            /// <summary>
            /// A corpse's item into the backpack: merged into stacks of its template, the rest in a
            /// free slot of its category. False, with nothing planned, when it does not all fit:
            /// it stays on the corpse, whole, and the items after it are still tried.
            /// </summary>
            internal bool AcceptLoot(LootItem loot, uint? preferredSlot, Action<Item> beforePublication)
            {
                ValidateLoot(loot, _unit.CharacterInventories.GetItems(_client.AccountEntry.Id));
                if (_loot.Any(entry => entry.EntityId == loot.EntityId || entry.Item.Id == loot.Item.Id))
                    throw new GameplayRejectionException("The same loot item cannot be planned twice.");
                var template = RequireTemplate(loot.ItemTemplateId);
                var start = ((int)template.InventoryCategory - 1) * PersonalCategorySize;
                var maximum = EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemClassInfo.StackSize;
                var merges = _stacks.Where(stack => stack.Template.ItemTemplateId == loot.ItemTemplateId &&
                    stack.Ownership == null && stack.Count > 0).ToList();
                var room = merges.Aggregate(0L, (total, stack) => total + Math.Max(0L, (long)stack.Maximum - stack.Count));
                var available = Enumerable.Range(start, PersonalCategorySize)
                    .Where(index => !_stacks.Any(stack => stack.Index == index && stack.Count > 0)).ToArray();
                if (room < loot.ItemQuantity && available.Length == 0)
                    return false;
                _loot.Add(loot);
                var remaining = loot.ItemQuantity;
                foreach (var stack in merges)
                {
                    var added = Math.Min(remaining, stack.Maximum - stack.Count);
                    stack.Count += added;
                    remaining -= added;
                    if (remaining == 0)
                        break;
                }
                if (remaining == 0)
                    return true;
                var destination = preferredSlot.HasValue && available.Contains((int)preferredSlot.Value)
                    ? (int)preferredSlot.Value : available[0];
                _stacks.Add(new Stack { Index = destination, Incoming = loot, Template = template,
                    Count = remaining, Maximum = maximum, BeforeRegister = beforePublication });
                return true;
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
                            item = stack.Staged = ItemManager.StageItem(stack.Template, stack.Count, stack.SplitFrom?.Crafter ?? stack.Crafter ?? "");
                            if (stack.SplitFrom is { } original)
                            {
                                item.Color = original.Color;
                                item.CurrentHitPoints = original.CurrentHitPoints;
                                item.BoundCharacterId = original.BoundCharacterId;
                                for (var slot = 0; slot < original.ModuleIds.Count; slot++)
                                    item.SetModule(slot, original.ModuleIds[slot]);
                            }
                            else if (stack.Modules != null)
                                SetModules(item, stack.Modules);
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

                foreach (var box in _inbox)
                {
                    var item = box.Staged = ItemManager.StageItem(box.Template, box.Count, box.Crafter ?? "");
                    if (box.Modules != null)
                        SetModules(item, box.Modules);
                    item.OwnerId = _characterId;
                    item.OwnerSlotId = box.Slot;
                    item.Id = _unit.Items.CreateItem(item);
                    if (item.Id == 0)
                        throw new GameplayRejectionException("Item creation returned an invalid durable identity.");
                    _unit.CharacterInventories.AddInvItem(_accountId, _characterId,
                        (uint)InventoryType.InboxInventory, box.Slot, item.Id);
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
                    if (stack.Staged != null && (!saved.Modules.SequenceEqual(item.ModuleIds) ||
                        (saved.CrafterName ?? "") != (item.Crafter ?? "")))
                        throw new GameplayRejectionException("Created item modules or crafter do not match the transaction plan.");
                }
                if (_unit.CharacterMissionItems.GetOwned(_client.Player.Id).Count != final.Count(stack => stack.Ownership != null))
                    throw new GameplayRejectionException("Final assignment ledger does not match the inventory plan.");
                foreach (var itemId in removed)
                    if (savedItems.ContainsKey(itemId) || itemOwners.ContainsKey(itemId) || rowCounts.ContainsKey(itemId))
                        throw new GameplayRejectionException("A consumed item survived final persistence.");

                if (_inbox.Count > 0)
                {
                    var inboxItems = _unit.Items.GetItems(_inbox.Select(box => box.Staged.Id).ToArray()).ToDictionary(item => item.ItemId);

                    foreach (var box in _inbox)
                    {
                        var boxRows = rows.Where(row => row.ItemId == box.Staged.Id).ToArray();
                        inboxItems.TryGetValue(box.Staged.Id, out var saved);
                        if (boxRows.Length != 1 || boxRows[0].CharacterId != _characterId ||
                            boxRows[0].InventoryType != (uint)InventoryType.InboxInventory || boxRows[0].SlotId != box.Slot ||
                            saved?.ItemTemplateId != box.Template.ItemTemplateId || saved.StackSize != box.Count ||
                            (saved.CrafterName ?? "") != (box.Crafter ?? "") || !saved.Modules.SequenceEqual(box.Staged.ModuleIds))
                            throw new GameplayRejectionException("Final inbox identity, slot, count, crafter or modules does not match the transaction plan.");
                    }
                }
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
                foreach (var box in _inbox)
                {
                    var item = box.Staged;
                    EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                    EntityManager.Instance.RegisterItem(item.EntityId, item);
                    if (!client.Player.Inventory.InboxItems.Contains(item.EntityId))
                        client.Player.Inventory.InboxItems.Add(item.EntityId);
                    MissionApplication.TryPublish(() => box.BeforeRegister?.Invoke(item),
                        $"inbox item {item.Id} publication hook");
                }
            }

            internal void Publish(Client client)
            {
                ConvergeRuntime(client);
                if (_published)
                    return;
                _published = true;
                var changed = _stacks.Where(stack => stack.Changed).ToList();

                // Every removal before any addition. The client takes an item out of its pack by
                // the slot it has it in (inventory._RemoveEntityFromMappings), so a removal sent
                // after something else has been added to that slot takes that out instead: a swap
                // of slots L < H sent Remove(L) Add(L→H) Remove(H) Add(H→L), and the third took
                // the item that had just gone to H off the client's pack until the next relog.
                foreach (var stack in changed)
                {
                    var item = stack.Existing ?? stack.Incoming?.Item ?? stack.Staged;
                    if (stack.Count == 0)
                        MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                            new InventoryRemoveItemPacket(InventoryType.Personal, item.EntityId)), $"item {item.Id} inventory removal");
                    else if (stack.Existing != null && stack.Index != stack.OriginalIndex)
                        MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                            new InventoryRemoveItemPacket(InventoryType.Personal, item.EntityId)), $"item {item.Id} old slot");
                }

                foreach (var stack in changed)
                {
                    var item = stack.Existing ?? stack.Incoming?.Item ?? stack.Staged;
                    if (stack.Count == 0)
                    {
                        MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientMethodId,
                            new DestroyPhysicalEntityPacket(item.EntityId)), $"item {item.Id} entity removal");
                    }
                    else if (stack.Existing != null)
                    {
                        if (stack.Index != stack.OriginalIndex)
                            MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                                new InventoryAddItemPacket(InventoryType.Personal, item.EntityId, (uint)stack.Index)), $"item {item.Id} new slot");
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
                foreach (var box in _inbox)
                {
                    var item = box.Staged;
                    MissionApplication.TryPublish(() => ItemManager.Instance.SendItemDataToClient(client, item, false),
                        $"inbox item {item.Id} entity data");
                    MissionApplication.TryPublish(() => client.CallMethod(SysEntity.ClientInventoryManagerId,
                        new AddInboxItemPacket(item.EntityId)), $"inbox item {item.Id}");
                }
                if (_inbox.Count > 0)
                    MissionApplication.TryPublish(() => TellSentToInbox(client, _inbox.Count), "inbox notice");
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
                {
                    foreach (var stack in _stacks.Where(stack => stack.Staged != null))
                        EntityManager.Instance.FreeEntity(stack.Staged.EntityId);
                    foreach (var box in _inbox.Where(box => box.Staged != null))
                        EntityManager.Instance.FreeEntity(box.Staged.EntityId);
                }
            }

            private void RequireCharacter()
            {
                if (!ReferenceEquals(_client.Player, _player) || _player.Id != _characterId || _client.AccountEntry?.Id != _accountId)
                    throw new GameplayRejectionException("Inventory character identity changed during the transaction.");
            }
        }
    }
}
