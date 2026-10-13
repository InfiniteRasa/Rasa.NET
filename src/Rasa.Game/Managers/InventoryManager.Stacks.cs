using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Inventory.Server;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Splitting and joining stacks inside the footlocker (HomeInventory_MoveItem). The pack's are
    /// in InventoryPlan.Move. The client's quantity chooser drags part of a stack and sends that
    /// quantity with the move; a plain drag sends the stack's count. The quantity used to be read
    /// and never used, and a drop onto a stack of the same item swapped the two.
    ///
    /// Onto a stack it can join (InventoryPlan.MayJoin: the same template, crafter, colour,
    /// binding and modules) it goes in, as much as fits, and the rest stays where it was. Part of
    /// a stack dropped in an empty slot is a new stack there, carrying what the old one carries.
    /// Part of a stack dropped on something else is refused. A whole stack that cannot join what
    /// is there moves or swaps, as before (HomeInventory_MoveItem).
    /// </summary>
    public partial class InventoryManager
    {
        /// <summary>
        /// A footlocker move that joins or splits a stack, carried out; false when the move is
        /// not one, and the ordinary whole-stack move or swap is to be made.
        /// </summary>
        private bool MoveHomeStack(Client client, uint srcSlot, uint destSlot, int quantity)
        {
            var home = client.Player.Inventory.HomeInventory;
            var item = EntityManager.Instance.GetItem(home[(int)srcSlot]);
            var there = home[(int)destSlot] == 0 ? null : EntityManager.Instance.GetItem(home[(int)destSlot]);

            if (item?.ItemTemplate == null)
                return false;

            var whole = quantity <= 0 || quantity >= item.StackSize;
            var amount = whole ? item.StackSize : (uint)quantity;
            var maximum = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class)?.ItemClassInfo?.StackSize ?? 1;

            if (there != null && InventoryPlan.MayJoin(item, there, maximum))
            {
                var joined = Math.Min(amount, maximum - Math.Min(maximum, there.StackSize));

                if (joined > 0)
                {
                    JoinHomeStacks(client, srcSlot, item, there, joined);
                    return true;
                }
            }

            if (whole)
                return false;

            if (there != null)
            {
                Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} dropped part of a footlocker stack on another item; refused.");
                return true;
            }

            SplitHomeStack(client, item, destSlot, amount);
            return true;
        }

        /// <summary><paramref name="joined"/> of the item in <paramref name="srcSlot"/> into the stack <paramref name="there"/>; an item left with none is deleted.</summary>
        private void JoinHomeStacks(Client client, uint srcSlot, Item item, Item there, uint joined)
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
                        unit.CharacterInventories.DeleteInvItemByItemId(item.Id);
                        unit.Items.DeleteItem(item.Id);
                    }
                });

            there.StackSize = total;
            client.CallMethod(there.EntityId, new SetStackCountPacket(total));

            if (left > 0)
            {
                item.StackSize = left;
                client.CallMethod(item.EntityId, new SetStackCountPacket(left));
                return;
            }

            item.StackSize = 0;
            client.Player.Inventory.HomeInventory[(int)srcSlot] = 0;
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(InventoryType.HomeInventory, item.EntityId));
            client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(item.EntityId));
            EntityManager.Instance.ReleaseEntity(item.EntityId, EntityType.Item);
        }

        /// <summary><paramref name="amount"/> of the item, less than all of it, as a new stack in the empty slot <paramref name="destSlot"/>.</summary>
        private void SplitHomeStack(Client client, Item item, uint destSlot, uint amount)
        {
            var left = item.StackSize - amount;
            var split = ItemManager.StageItem(item.ItemTemplate, amount, item.Crafter ?? "");

            split.Color = item.Color;
            split.CurrentHitPoints = item.CurrentHitPoints;
            split.BoundCharacterId = item.BoundCharacterId;
            for (var slot = 0; slot < item.ModuleIds.Count; slot++)
                split.SetModule(slot, item.ModuleIds[slot]);

            // The footlocker is the account's: its rows carry character id 0 (AddItemBySlot).
            split.OwnerId = 0;
            split.OwnerSlotId = destSlot;

            using (var unit = _gameUnitOfWorkFactory.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.Items.UpdateItemStackSize(new Item(0, left, 0, 0) { Id = item.Id });
                    split.Id = unit.Items.CreateItem(split);

                    if (split.Id == 0)
                        throw new GameplayRejectionException("Item creation returned an invalid durable identity.");

                    unit.CharacterInventories.AddInvItem(client.AccountEntry.Id, 0, (uint)InventoryType.HomeInventory, destSlot, split.Id);
                });

            item.StackSize = left;
            EntityManager.Instance.RegisterEntity(split.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(split.EntityId, split);
            client.Player.Inventory.HomeInventory[(int)destSlot] = split.EntityId;

            client.CallMethod(item.EntityId, new SetStackCountPacket(left));
            ItemManager.Instance.SendItemDataToClient(client, split, false);
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(InventoryType.HomeInventory, split.EntityId, destSlot));
        }
    }
}
