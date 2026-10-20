using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Inventory.Server;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Structures;

    /// <summary>
    /// Splitting and joining stacks on their way between the footlocker, the clan lockbox and the
    /// pack, and inside the footlocker and the lockbox. The pack's own moves are in
    /// InventoryPlan.Move. The client's quantity chooser drags part of a stack, a ctrl+right-click
    /// takes one, and a plain drag or right-click sends the stack's count (inventorywindow
    /// _StartItemDrag and _GetQuantity, lockboxwindow, clanlockboxwindow); every one of these
    /// moves read the quantity and moved the whole stack, and a stack dropped on a stack of the
    /// same item swapped with it.
    ///
    /// Onto a stack it can join (InventoryPlan.MayJoin: the same template, crafter, colour,
    /// binding and modules) it goes in, as much as fits, and the rest stays where it was. Part of
    /// a stack into an empty slot is a new stack there, carrying what the old one carries. Part of
    /// a stack onto something else is refused. A whole stack that cannot join what is there is
    /// left to the move's own handler, which moves or swaps it as before.
    ///
    /// Each change is one transaction - a join updates both stack sizes, or deletes the emptied
    /// item's rows; a split updates the old stack and creates the new item and its row - and the
    /// lists and clients follow once it has committed: a changed count goes out as SetStackCount,
    /// a stack joined away as InventoryRemoveItem and DestroyPhysicalEntity, and a new stack as
    /// its item data and InventoryAddItem, to every member holding the lockbox for a lockbox
    /// change.
    /// </summary>
    public partial class InventoryManager
    {
        /// <summary>One of the three containers a stack moves between, as seen from the client making the move.</summary>
        private sealed class StackStore
        {
            internal InventoryType Type;
            internal List<ulong> Slots;
            internal uint ClanId;

            /// <summary>The character id an inventory row is written with: the character's for the pack, 0 for the footlocker.</summary>
            internal uint RowCharacterId;

            internal bool IsClan => Type == InventoryType.ClanInventory;
        }

        private static StackStore PackStore(Client client) => new()
        {
            Type = InventoryType.Personal, Slots = client.Player.Inventory.PersonalInventory, RowCharacterId = client.Player.Id
        };

        private static StackStore HomeStore(Client client) => new()
        {
            Type = InventoryType.HomeInventory, Slots = client.Player.Inventory.HomeInventory, RowCharacterId = 0
        };

        private static StackStore ClanStore(Client client) => new()
        {
            Type = InventoryType.ClanInventory, Slots = client.Player.Inventory.ClanInventory, ClanId = client.Player.ClanId
        };

        /// <summary>A footlocker move that joins or splits a stack, carried out; false for a whole-stack move or swap.</summary>
        private bool MoveHomeStack(Client client, uint srcSlot, uint destSlot, int quantity) =>
            TransferStack(client, HomeStore(client), srcSlot, HomeStore(client), destSlot, quantity) != StackTransfer.Whole;

        private enum StackTransfer
        {
            /// <summary>A whole stack that does not join what is there: the handler's own move or swap.</summary>
            Whole,

            /// <summary>Joined into the stack there, or split off into the empty slot.</summary>
            Done,

            /// <summary>Part of a stack dropped on something it cannot join: nothing done.</summary>
            Refused
        }

        /// <summary>
        /// <paramref name="quantity"/> of the stack in <paramref name="fromSlot"/> (0 or less, or
        /// the whole count, for all of it) to <paramref name="toSlot"/>, joining or splitting as
        /// the class remarks say. The caller has checked everything the move needs - rank,
        /// binding, Character Unique, the slots being unlocked and of the right tab.
        /// </summary>
        /// <param name="prepare">Run once the move is known to be a join or a split, before anything changes - the binding of an item passing through the footlocker; false stops it.</param>
        private StackTransfer TransferStack(Client client, StackStore from, uint fromSlot, StackStore to, uint toSlot, long quantity, Func<bool> prepare = null)
        {
            var item = EntityManager.Instance.GetItem(from.Slots[(int)fromSlot]);
            var there = to.Slots[(int)toSlot] == 0 ? null : EntityManager.Instance.GetItem(to.Slots[(int)toSlot]);

            if (item?.ItemTemplate == null || ReferenceEquals(item, there))
                return StackTransfer.Whole;

            var whole = quantity <= 0 || quantity >= item.StackSize;
            var amount = whole ? item.StackSize : (uint)quantity;
            var maximum = StackMaximum(item);

            if (whole && !Joins(item, there, maximum))
                return StackTransfer.Whole;

            if (prepare != null && !prepare())
                return StackTransfer.Refused;

            if (Joins(item, there, maximum))
            {
                JoinStacks(client, from, fromSlot, item, to, there, Math.Min(amount, maximum - there.StackSize));
                return StackTransfer.Done;
            }

            if (whole)
                return StackTransfer.Whole;

            if (there != null)
            {
                Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} dropped part of a {from.Type} stack on another item in {to.Type}; refused.");
                return StackTransfer.Refused;
            }

            SplitStack(client, from, item, to, toSlot, amount);
            return StackTransfer.Done;
        }

        /// <summary>Whether the item can go into that stack, and the stack has room for some of it.</summary>
        private static bool Joins(Item item, Item there, uint maximum) =>
            there != null && there.StackSize < maximum && InventoryPlan.MayJoin(item, there, maximum);

        /// <summary>A quantity as a client sends it, which may arrive as a Python long.</summary>
        private static long Quantity(long quantity) => Math.Clamp(quantity, 0, uint.MaxValue);

        private static uint StackMaximum(Item item) =>
            EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class)?.ItemClassInfo?.StackSize ?? 1;

        /// <summary>
        /// Where part of a stack goes when no slot is named - a ctrl+right-click into the clan
        /// lockbox, or out of it into the pack: the first stack it can join that has room in
        /// <paramref name="joinable"/>, or else the first free slot of <paramref name="free"/>.
        /// -1 for nowhere.
        /// </summary>
        private static int StackDestination(Item item, List<ulong> slots, (int First, int Last) joinable, (int First, int Last) free)
        {
            var maximum = StackMaximum(item);

            for (var slot = joinable.First; slot < joinable.Last && slot < slots.Count; slot++)
            {
                var there = slots[slot] == 0 ? null : EntityManager.Instance.GetItem(slots[slot]);

                if (there != null && !ReferenceEquals(there, item) && there.StackSize < maximum && InventoryPlan.MayJoin(item, there, maximum))
                    return slot;
            }

            for (var slot = free.First; slot < free.Last && slot < slots.Count; slot++)
                if (slots[slot] == 0)
                    return slot;

            return -1;
        }

        private void JoinStacks(Client client, StackStore from, uint fromSlot, Item item, StackStore to, Item there, uint joined)
        {
            var left = item.StackSize - joined;
            var total = there.StackSize + joined;

            using (var unit = _gameUnitOfWorkFactory.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.Items.UpdateItemStackSize(new Item(0, total, 0, 0) { Id = there.Id });

                    if (left > 0)
                        unit.Items.UpdateItemStackSize(new Item(0, left, 0, 0) { Id = item.Id });
                    else
                    {
                        DeleteRow(unit, from, item.Id);
                        unit.Items.DeleteItem(item.Id);
                    }
                });

            there.StackSize = total;
            TellCount(client, to, there);

            if (left > 0)
            {
                item.StackSize = left;
                TellCount(client, from, item);
                return;
            }

            item.StackSize = 0;
            TellRemoved(client, from, fromSlot, item);
            EntityManager.Instance.ReleaseEntity(item.EntityId, EntityType.Item);
        }

        private void SplitStack(Client client, StackStore from, Item item, StackStore to, uint toSlot, uint amount)
        {
            var left = item.StackSize - amount;
            var split = ItemManager.StageItem(item.ItemTemplate, amount, item.Crafter ?? "");

            split.Color = item.Color;
            split.CurrentHitPoints = item.CurrentHitPoints;
            split.BoundCharacterId = item.BoundCharacterId;
            for (var slot = 0; slot < item.ModuleIds.Count; slot++)
                split.SetModule(slot, item.ModuleIds[slot]);

            split.OwnerId = to.RowCharacterId;
            split.OwnerSlotId = toSlot;

            using (var unit = _gameUnitOfWorkFactory.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.Items.UpdateItemStackSize(new Item(0, left, 0, 0) { Id = item.Id });
                    split.Id = unit.Items.CreateItem(split);

                    if (split.Id == 0)
                        throw new GameplayRejectionException("Item creation returned an invalid durable identity.");

                    AddRow(unit, client, to, toSlot, split.Id);
                });

            item.StackSize = left;
            EntityManager.Instance.RegisterEntity(split.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(split.EntityId, split);

            TellCount(client, from, item);
            TellAdded(client, to, toSlot, split);
        }

        private static void AddRow(ICharUnitOfWork unit, Client client, StackStore store, uint slot, uint itemId)
        {
            if (store.IsClan)
                unit.ClanInventories.AddInvItem(store.ClanId, slot, itemId);
            else
                unit.CharacterInventories.AddInvItem(client.AccountEntry.Id, store.RowCharacterId, (uint)store.Type, slot, itemId);
        }

        private static void DeleteRow(ICharUnitOfWork unit, StackStore store, uint itemId)
        {
            if (store.IsClan)
                unit.ClanInventories.DeleteInvItemByItemId(itemId);
            else
                unit.CharacterInventories.DeleteInvItemByItemId(itemId);
        }

        /// <summary>The client making the move, and for the lockbox every other member who holds it.</summary>
        private void ForStoreHolders(Client client, StackStore store, Action<Client> change)
        {
            change(client);

            if (store.IsClan)
                ForClanLockboxHolders(store.ClanId, change, client.Player.Id);
        }

        private void TellCount(Client client, StackStore store, Item item)
        {
            var count = new SetStackCountPacket(item.StackSize);

            ForStoreHolders(client, store, holder => holder.CallMethod(item.EntityId, count));
        }

        private void TellAdded(Client client, StackStore store, uint slot, Item item)
        {
            ForStoreHolders(client, store, holder =>
            {
                Slots(holder.Player, store.Type)[(int)slot] = item.EntityId;
                ItemManager.Instance.SendItemDataToClient(holder, item, false);
                holder.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(store.Type, item.EntityId, slot));
            });
        }

        private void TellRemoved(Client client, StackStore store, uint slot, Item item)
        {
            ForStoreHolders(client, store, holder =>
            {
                var slots = Slots(holder.Player, store.Type);

                if (slots[(int)slot] == item.EntityId)
                    slots[(int)slot] = 0;

                holder.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(store.Type, item.EntityId));
                holder.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(item.EntityId));
            });
        }
    }
}
