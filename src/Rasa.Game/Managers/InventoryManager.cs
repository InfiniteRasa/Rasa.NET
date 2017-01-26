using System;

namespace Rasa.Managers
{    
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class InventoryManager
    {
        private static InventoryManager _instance;
        private static readonly object InstanceLock = new object();

        public static InventoryManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new InventoryManager();
                    }
                }

                return _instance;
            }
        }

        private InventoryManager()
        {
        }

        public void AddItemBySlot(MapChannelClient mapClient, InventoryType inventoryType, uint itemId, int slotId, bool updateDB)
        {
            var tempItem = EntityManager.Instance.GetItem(itemId);
            if (tempItem == null)
                return;
            // set entityId in slot
            

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    tempItem.OwnerSlotId = slotId;
                    mapClient.Inventory.PersonalInventory[slotId] = itemId; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    tempItem.OwnerSlotId = slotId + 250;
                    mapClient.Inventory.EquippedInventory[slotId] = itemId; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    tempItem.OwnerSlotId = slotId + 250 + 22;
                    mapClient.Inventory.WeaponDrawer[slotId] = itemId; // update slot
                    break;
                default:
                    Console.WriteLine("Unsuported inventory type");
                    break;
            }
            // update item location
            tempItem.OwnerId = mapClient.Player.Actor.EntityId;
            // send inventoryAddItem
            mapClient.Player.Client.SendPacket(9, new InventoryAddItemPacket { Type = inventoryType, EntityId = itemId, SlotId = slotId });
            // update item in database
            if (updateDB)
                CharacterInventoryTable.MoveInvItem(mapClient.Player.CharacterId, tempItem.OwnerSlotId, tempItem.ItemId);
        }

        public void AddItemToInventory(MapChannelClient mapClient, int itemTemplateId, int quantity)
        {
            var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return; // invalid itemTemplateId
            // get item category offset
            var itemCategoryOffset = (itemTemplate.InventoryCategory - 1);
            if (itemCategoryOffset < 0 || itemCategoryOffset >= 5)
            {
                Console.WriteLine("AddItemToInventory: The item inventory category {0} is invalid", itemCategoryOffset);
                return;
            }
            itemCategoryOffset *= 50;
            // see if we can merge the item into an already existing item
            for (var i = 0; i < 50; i++)
            {
                if (mapClient.Inventory.PersonalInventory[itemCategoryOffset + i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(mapClient.Inventory.PersonalInventory[itemCategoryOffset + i]);
                    // same item template?
                    if (slotItem.ItemTemplate.ItemTemplateId != itemTemplateId)
                        continue;
                    // calculate how many items we can add to the stack
                    var stackAdd = slotItem.ItemTemplate.Stacksize - slotItem.Stacksize;
                    if (stackAdd == 0)
                        continue;
                    // add item to existing stack
                    var stackMove = Math.Min(stackAdd, quantity);
                    slotItem.Stacksize += stackMove;
                    // notify client of changed stack count
                    mapClient.Player.Client.SendPacket(slotItem.EntityId, new SetStackCountPacket { StackSize = slotItem.Stacksize });
                    // update item in database
                    ItemsTable.UpdateItemStackSize(slotItem.ItemId, slotItem.Stacksize);
                    CommunicatorManager.Instance.SystemMessage(mapClient, "You got item with id:" + itemTemplateId + " (" + stackMove + ").");
                    quantity -= stackMove;
                    if (quantity > 0)
                    {
                        // call function again with what's left of stack size
                        AddItemToInventory(mapClient, itemTemplateId, quantity);
                        return;
                    }
                    break;
                }
            }
            // we need to crate new item
            if (quantity > 0)
                for (var i = 0; i < 50; i++)
                {
                    // check item stacksize
                    if (itemTemplate.Stacksize < quantity)
                    {
                        // find empty slot
                        if (mapClient.Inventory.PersonalInventory[itemCategoryOffset + i] == 0)
                        {
                            var newItem = ItemManager.Instance.CreateFromTemplateId(mapClient.Player.CharacterId, itemCategoryOffset + i, itemTemplateId, itemTemplate.Stacksize);
                            newItem.ItemTemplate.CrafterName = mapClient.Player.Actor.Name;
                            // send data to client
                            ItemManager.Instance.SendItemDataToClient(mapClient, newItem);
                            // add item to empty slot
                            AddItemBySlot(mapClient, InventoryType.Personal, newItem.EntityId, itemCategoryOffset + i, true);
                            CommunicatorManager.Instance.SystemMessage(mapClient, "You got item with id:" + itemTemplateId + " (" + itemTemplate.Stacksize + ").");
                            quantity -= itemTemplate.Stacksize;
                        }
                    }
                    else
                        // find empty slot
                        if (mapClient.Inventory.PersonalInventory[itemCategoryOffset + i] == 0)
                    {
                        var newItem = ItemManager.Instance.CreateFromTemplateId(mapClient.Player.CharacterId, itemCategoryOffset + i, itemTemplateId, quantity);
                        newItem.ItemTemplate.CrafterName = mapClient.Player.Actor.Name;
                        // send data to client
                        ItemManager.Instance.SendItemDataToClient(mapClient, newItem);
                        // add item to empty slot
                        AddItemBySlot(mapClient, InventoryType.Personal, newItem.EntityId, itemCategoryOffset + i, true);
                        CommunicatorManager.Instance.SystemMessage(mapClient, "You got item with id:" + itemTemplateId + " (" + quantity + ").");
                        break;
                    }
                }
        }

        public Item CurrentWeapon(MapChannelClient mapClient)
        {
            return EntityManager.Instance.GetItem(mapClient.Inventory.WeaponDrawer[mapClient.Inventory.ActiveWeaponDrawer]);
        }

        public int FreeSlotIndex(MapChannelClient mapClient, InventoryType inventoryType, int slotIndex)
        {
            var slotId = 0;
            switch (inventoryType)
            {
                case InventoryType.Personal:
                    mapClient.Inventory.PersonalInventory[slotIndex] = 0; // update slot
                    slotId = slotIndex;
                    break;
                case InventoryType.EquipedInventory:
                    mapClient.Inventory.EquippedInventory[slotIndex] = 0; // update slot
                    slotId = slotIndex + 250;
                    break;
                case InventoryType.WeaponDrawerInventory:
                    mapClient.Inventory.WeaponDrawer[slotIndex] = 0;    // update slot
                    slotId = slotIndex + 250 + 22;
                    break;
                default:
                    Console.WriteLine("RemoveItemBySlot: Invalid inventoryType{0}/slotIndex{1}\n", inventoryType, slotIndex);
                    break;
            }
            return slotId;
        }

        public void InitForClient(MapChannelClient mapClient)
        {
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.Personal, InventorySize = 250 });
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.WeaponDrawerInventory, InventorySize = 5 });
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.EquipedInventory, InventorySize = 22 });
            
            InitCharacterInventory(mapClient);
        }

        public void InitCharacterInventory(MapChannelClient mapClient)
        {
            
            var getInventoryData = CharacterInventoryTable.GetItem(mapClient.Player.CharacterId);
            foreach (var item in getInventoryData)
            {
                var itemData = ItemsTable.GetItem(item.ItemId);
                var tempItem = ItemManager.Instance.GetItemFromTemplateId(item.ItemId, mapClient.Player.CharacterId, item.SlotId, itemData.EntityClassId, itemData.StackSize);
                tempItem.ItemId = item.ItemId;
                
                if (item.SlotId < 250)
                    mapClient.Inventory.PersonalInventory[item.SlotId] = tempItem.EntityId;
                else if (249 < item.SlotId && item.SlotId < 272)
                    mapClient.Inventory.EquippedInventory[item.SlotId - 250] = tempItem.EntityId;
                else if (271 < item.SlotId)
                {
                    tempItem.WeaponAmmoCount = itemData.AmmoCount;
                    mapClient.Inventory.WeaponDrawer[item.SlotId - 250 - 22] = tempItem.EntityId;
                }
            }
            // send inventory data - personal inventory
            for (var i = 0; i < 250; i++)
            {
                if (mapClient.Inventory.PersonalInventory[i] == 0)
                    continue;

                var slotItem = EntityManager.Instance.GetItem(mapClient.Inventory.PersonalInventory[i]);
                ItemManager.Instance.SendItemDataToClient(mapClient, slotItem);
                // make the item appear on the client
                AddItemBySlot(mapClient, InventoryType.Personal, mapClient.Inventory.PersonalInventory[i], i, false);
            }
            
            // send inventory data - equipped inventory
            for (var i = 0; i < 22; i++)
            {
                if (mapClient.Inventory.EquippedInventory[i] == 0)
                    continue;
                // item in slot present
                // get item handle
                var slotItem = EntityManager.Instance.GetItem(mapClient.Inventory.EquippedInventory[i]);
                ItemManager.Instance.SendItemDataToClient(mapClient, slotItem);
                // make the item appear on the client
               AddItemBySlot(mapClient, InventoryType.EquipedInventory, mapClient.Inventory.EquippedInventory[i], i, false);
            }

            // send inventory data - weapon drawer
            for (var i = 0; i < 5; i++)
            {
                if (mapClient.Inventory.WeaponDrawer[i] == 0)
                    continue;
                // item in slot present
                // get item handle
                var slotItem = EntityManager.Instance.GetItem(mapClient.Inventory.WeaponDrawer[i]);
                ItemManager.Instance.SendItemDataToClient(mapClient, slotItem);
                // make the item appear on the client
                AddItemBySlot(mapClient, InventoryType.WeaponDrawerInventory, mapClient.Inventory.WeaponDrawer[i], i, false);
                // update appearance
                PlayerManager.Instance.SetAppearanceItem(mapClient.Player, slotItem.ItemTemplate.ClassId, 0);
            }

            PlayerManager.Instance.UpdateAppearance(mapClient);
            NotifyEquipmentUpdate(mapClient);
        }

        public void NotifyEquipmentUpdate(MapChannelClient mapClient)
        {
            mapClient.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new EquipmentInfoPacket { WeaponDrawer = mapClient.Inventory.WeaponDrawer[mapClient.Inventory.ActiveWeaponDrawer] });
        }

        public void PersonalInventory_DestroyItem(Client client, PersonalInventory_DestroyItemPacket packet)
        {
            if (packet.ItemId == 0)
                return;
            var tempItem = EntityManager.Instance.GetItem((uint)packet.ItemId);
            ReduceStackCount(client.MapClient, tempItem, (int)packet.Quantity);
        }

        public void PersonalInventory_MoveItem(Client client, PersonalInventory_MoveItemPacket packet)
        {
            // remove item
            if (packet.SrcSlot == packet.DestSlot)
                return;
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;
            if (packet.DestSlot < 0 || packet.DestSlot >= 250)
                return;
            var entityId = client.MapClient.Inventory.PersonalInventory[packet.SrcSlot];
            if (entityId == 0)
                return;

            RemoveItemBySlot(client.MapClient, InventoryType.Personal, entityId, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.MapClient.Inventory.PersonalInventory[packet.DestSlot] != 0)
            {
                AddItemBySlot(client.MapClient, InventoryType.Personal, client.MapClient.Inventory.PersonalInventory[packet.DestSlot], packet.SrcSlot, true);
            }
            AddItemBySlot(client.MapClient, InventoryType.Personal, entityId, packet.DestSlot, true);
        }

        public void ReduceStackCount(MapChannelClient mapClient, Item tempItem, int stackDecreaseCount)
        {
            if (tempItem.OwnerId != mapClient.Player.Actor.EntityId)
                return; // item is not on this client's inventory
            var newStackCount = tempItem.Stacksize - stackDecreaseCount;
            if (newStackCount <= 0)
            {
                // destroy item
                EntityManager.Instance.DestroyPhysicalEntity(mapClient, tempItem.EntityId, EntityType.Item);
                mapClient.Player.Client.SendPacket(9, new InventoryRemoveItemPacket { InventoryType = InventoryType.Personal, EntityId = (int)tempItem.EntityId });
                // free slot
                FreeSlotIndex(mapClient, InventoryType.Personal, tempItem.OwnerSlotId);
                // Update db
                CharacterInventoryTable.DeleteInvItem(mapClient.Player.CharacterId, tempItem.OwnerSlotId);
                // ToDo will we delete items from db, or we will let tham stay, so thay can be retrived
                //ItemsTable.DeleteItem(tempItem.ItemId);
            }
            else
            {
                // update stack count
                tempItem.Stacksize = newStackCount;
                // set stackcount
                mapClient.Player.Client.SendPacket(tempItem.EntityId, new SetStackCountPacket { StackSize = newStackCount });
                // update stack count in database
                ItemsTable.UpdateItemStackSize(tempItem.ItemId, tempItem.Stacksize);
            }
        }

        public void RemoveItemBySlot(MapChannelClient mapClient, InventoryType inventoryType, uint entityId, int slotIndex)
        {
            Item tempItem = EntityManager.Instance.GetItem(entityId);
            var slotId = FreeSlotIndex(mapClient, inventoryType, slotIndex);
            // send inventoryRemoveItem
            mapClient.Player.Client.SendPacket(9, new InventoryRemoveItemPacket { InventoryType = inventoryType, EntityId = (int)entityId });
            // update item in database
            //CharacterInventoryTable.MoveInvItem(mapClient.Player.CharacterId, slotId, tempItem.ItemId);
        }

        public void RequestEquipArmor(Client client, RequestEquipArmorPacket packet)
        {
            if (packet.SrcInventory != 1)
            {
                Console.WriteLine("Unsupported inventory");
                return;
            }
            if (packet.SrcSlot < 0 || packet.SrcSlot > 50)
                return;
            if (packet.DestSlot < 0 || packet.DestSlot > 22)
                return;
            uint entityIdEquippedItem = 0;
            uint entityIdInventoryItem = 0;
            entityIdEquippedItem = client.MapClient.Inventory.EquippedInventory[packet.DestSlot]; // the old equipped item (can be none)
            entityIdInventoryItem = client.MapClient.Inventory.PersonalInventory[packet.SrcSlot]; // the new equipped item (can be none)
            // can we equip the item?
            Item itemToEquip = null;
            if (entityIdInventoryItem != 0)
            {
                itemToEquip = EntityManager.Instance.GetItem(entityIdInventoryItem);
                // min level criteria met?
                if (itemToEquip != null && client.MapClient.Player.Level < itemToEquip.ItemTemplate.ReqLevel)
                {
                    // level too low, cannot equip item
                    return;
                }
            }
            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[packet.SrcSlot] != 0)
                RemoveItemBySlot(client.MapClient, InventoryType.Personal, entityIdInventoryItem, packet.SrcSlot);
            if (client.MapClient.Inventory.EquippedInventory[packet.DestSlot] != 0)
                RemoveItemBySlot(client.MapClient, InventoryType.EquipedInventory, entityIdEquippedItem, packet.DestSlot);
            if (entityIdEquippedItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.Personal, entityIdEquippedItem, packet.SrcSlot, true);
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.EquipedInventory, entityIdInventoryItem, packet.DestSlot, true);
            // update appearance
            if (itemToEquip == null)
            {
                // remove item graphic if dequipped
                var prevEquippedItem = EntityManager.Instance.GetItem(entityIdEquippedItem);
                PlayerManager.Instance.RemoveAppearanceItem(client.MapClient.Player, prevEquippedItem.ItemTemplate.ClassId);
            }
            else
            {
                PlayerManager.Instance.SetAppearanceItem(client.MapClient.Player, itemToEquip.ItemTemplate.ClassId, -2139062144);
            }
            PlayerManager.Instance.UpdateAppearance(client.MapClient);
            // update stats
            PlayerManager.Instance.UpdateStatsValues(client.MapClient, false);
        }

        public void RequestEquipWeapon(Client client, RequestEquipWeaponPacket packet)
        {
            var srcSlot = packet.SrcSlot;
            var invType = packet.InventoryType;
            var destSlot = packet.DestSlot;
            if (invType != 1)
            {
                Console.WriteLine("unsuported inventory");
                return;
            }
            if (srcSlot < 0 || srcSlot > 50)
                return;
            if (destSlot < 0 || destSlot >= 5)
                return;
            // equip item
            var entityIdEquippedItem = client.MapClient.Inventory.WeaponDrawer[destSlot]; // the old equipped item (can be none)
            var entityIdInventoryItem = client.MapClient.Inventory.PersonalInventory[srcSlot]; // the new equipped item (can be none)
            // can we equip the item?
            Item itemToEquip = null;
            if (entityIdInventoryItem != 0)
            {
                itemToEquip =  EntityManager.Instance.GetItem(entityIdInventoryItem);
                // min level criteria met?
                if (itemToEquip != null && client.MapClient.Player.Level < itemToEquip.ItemTemplate.ReqLevel)
                {
                    // level too low, cannot equip item
                    return;
                }
            }
            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[srcSlot] != 0)
                RemoveItemBySlot(client.MapClient, InventoryType.Personal, entityIdInventoryItem, srcSlot);
            if (client.MapClient.Inventory.WeaponDrawer[destSlot] != 0)
                RemoveItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, entityIdEquippedItem, destSlot);
            if (entityIdEquippedItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.Personal, entityIdEquippedItem, srcSlot, true);
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, entityIdInventoryItem, destSlot, true);
            // update appearance
            if (destSlot == client.MapClient.Inventory.ActiveWeaponDrawer)
            {
                NotifyEquipmentUpdate(client.MapClient);
                if (itemToEquip == null)
                {
                    // remove item graphic if dequipped
                    var prevEquippedItem = EntityManager.Instance.GetItem(entityIdEquippedItem);
                    PlayerManager.Instance.RemoveAppearanceItem(client.MapClient.Player, prevEquippedItem.ItemTemplate.ClassId);
                }
                else
                {
                    PlayerManager.Instance.SetAppearanceItem(client.MapClient.Player, itemToEquip.ItemTemplate.ClassId, -2139062144);
                }
                PlayerManager.Instance.UpdateAppearance(client.MapClient);
            }
        }

        public void RequestWeaponReload(MapChannelClient mapClient, Item weapon, bool tellSelf)
        {
            // find and eat a piece of ammunition
            var ammoClassId = weapon.ItemTemplate.Weapon.AmmoClassId;
            bool foundAmmo = false;
            for (var i = 0; i < 50; i++)
            {
                if (mapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;
                var weaponAmmo = EntityManager.Instance.GetItem(mapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);
                //if (!itemAmmo)
                    //return;
                if (weaponAmmo.ItemTemplate.ClassId == ammoClassId)
                {
                    // consume one piece of ammo
                    foundAmmo = true;
                    break;
                }
            }
            if (foundAmmo == false)
                return; // no ammo found -> ToDo: Tell the client?
            // send action start
            var reloadActionId = 134;
            var reloadActionArgId = 1;// todo: Use correct argId depending on weapon type
            // ToDo write netMgr_cellDomain_pythonAddMethodCallRaw and netMgr_cellDomain_pythonAddMethodCallRawIgnoreSelf
            /*if (tellSelf)
            {
                // if item_recv_RequestWeaponReload() is not called by client, then the server issued the weapon reload request
                // tell all clients about the action start
                netMgr_cellDomain_pythonAddMethodCallRaw(client, client->player->actor->entityId, PerformWindup, pym_getData(&pms), pym_getLen(&pms));
            }
            else
            {
                // if the client sent the request, tell everyone but the client to start the reload action
                // In this case, the action is created on the client without actually having to trigger it via Recv_PerformWindup
                netMgr_cellDomain_pythonAddMethodCallRawIgnoreSelf(client, client->player->actor->entityId, PerformWindup, pym_getData(&pms), pym_getLen(&pms));
            }*/
            mapClient.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new PerformWindupPacket { ActionId = reloadActionId, ActionArgId = reloadActionArgId });
            // start reload action
            //actor_startActionOnEntity(client->mapChannel, client->player->actor, 0, reloadActionId, reloadActionArgId, 1500, 0, _cb_item_recv_RequestWeaponReload_actionUpdate);
        }

        public void RequestTooltipForItemTemplateId(Client client, int itemTemplateId)
        {

            var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return; // todo: even answer on a unknown template, else the client will continue to spam us with requests


            client.SendPacket(12, new ItemTemplateTooltipInfoPacket {ItemTemplate = itemTemplate});
        }

        public void WeaponDrawerInventory_MoveItem(Client client, WeaponDrawerInventory_MoveItemPacket packet)
        {
            var srcEquippedItem = client.MapClient.Inventory.WeaponDrawer[packet.SrcSlot];
            var destEquippedItem = client.MapClient.Inventory.WeaponDrawer[packet.DestSlot];
            // swap items on the client and server
            if (destEquippedItem != 0)
            {
                RemoveItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, destEquippedItem, packet.SrcSlot);
                RemoveItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEquippedItem, packet.DestSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEquippedItem, packet.DestSlot, true);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, destEquippedItem, packet.SrcSlot, true);
            }
            else
            {
                RemoveItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEquippedItem, packet.SrcSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEquippedItem, packet.DestSlot, true);
            }
        }
    }
}
