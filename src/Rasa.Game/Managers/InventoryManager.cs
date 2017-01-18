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
        private static void AddItemBySlot(MapChannelClient mapClient, InventoryType inventoryType, uint itemId, int slotId)
        {
            var tempItem = EntityManager.GetItem(itemId);
            if (tempItem == null)
                return;
            // set entityId in slot
            if (inventoryType == InventoryType.Personal)
            {
                mapClient.Inventory.PersonalInventory[slotId] = itemId; // update slot
                tempItem.OwnerSlotId = slotId;
            }
            else if (inventoryType == InventoryType.EquipedInventory)
            {
                mapClient.Inventory.EquippedInventory[slotId] = itemId; // update slot
                tempItem.OwnerSlotId = slotId + 250;
            }
            else if (inventoryType == InventoryType.WeaponDrawerInventory)
            {
                mapClient.Inventory.WeaponDrawer[slotId] = itemId; // update slot
                tempItem.OwnerSlotId = slotId + 250 + 22;
            }
            else
            {
                return;
            }
            // update item location
            tempItem.OwnerId = mapClient.Player.Actor.EntityId;
            // send inventoryAddItem
            mapClient.Player.Client.SendPacket(9, new InventoryAddItemPacket { Type = inventoryType, EntityId = itemId, SlotId = slotId });
            // update item in database
            ItemsTable.UpdateItem(tempItem.EntityId, tempItem.OwnerId, tempItem.OwnerSlotId, tempItem.Stacksize);
        }

        public static Item CurrentWeapon(MapChannelClient mapClient)
        {
            return EntityManager.GetItem(mapClient.Inventory.WeaponDrawer[mapClient.Inventory.ActiveWeaponDrawer]);
        }

        public static void InitForClient(MapChannelClient mapClient)
        {
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.Personal, InventorySize = 250 });
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.WeaponDrawerInventory, InventorySize = 5 });
            mapClient.Client.SendPacket( 9, new InventoryCreatePacket { InventoryType = (int)InventoryType.EquipedInventory, InventorySize = 22 });
            
            InitCharacterInventory(mapClient);
        }

        public static void InitCharacterInventory(MapChannelClient mapClient)
        {
            // load personal inventory
            
            for (var i = 0; i < 250; i++)
            {
                var itemData = ItemsTable.GetItem(mapClient.Player.CharacterId, i);
                if (itemData.Count == 0)
                    continue;
                Item tempItem = new Item();
                tempItem = ItemManager.GetItemFromTemplateId(itemData[0], mapClient.Player.CharacterId, i, (int)itemData[1], (int)itemData[2]);                
                mapClient.Inventory.PersonalInventory[i] = tempItem.EntityId;
            }
            // load equipped inventory
            for (var i = 0; i < 22; i++)
            {
                var itemData = ItemsTable.GetItem(mapClient.Player.CharacterId, i + 250);
                if (itemData.Count == 0)
                    continue;
                Item tempItem = new Item();
                tempItem = ItemManager.GetItemFromTemplateId(itemData[0], mapClient.Player.CharacterId, i + 250, (int)itemData[1], (int)itemData[2]);
                mapClient.Inventory.EquippedInventory[i] = tempItem.EntityId;
            }

            // load weapon drawer and ammo
            for (var i = 0; i < 5; i++)
            {
                var itemData = ItemsTable.GetItem(mapClient.Player.CharacterId, i + 250 + 22);
                if (itemData.Count == 0)
                    continue;
                Item tempItem = new Item();
                tempItem = ItemManager.GetItemFromTemplateId(itemData[0], mapClient.Player.CharacterId, i + 250 + 22, (int)itemData[1], (int)itemData[2]);
                // get ammo
                var ammomData = ItemsTable.GetItem(mapClient.Player.CharacterId, i + 250 + 22 + 5);
                if (ammomData.Count == 0)
                    tempItem.WeaponAmmoCount = 0;
                else
                    tempItem.WeaponAmmoCount = (int)ammomData[2];
                mapClient.Inventory.WeaponDrawer[i] = tempItem.EntityId;
            }
            // send inventory data - personal inventory
            for (var i = 0; i < 250; i++)
            {
                if (mapClient.Inventory.PersonalInventory[i] == 0)
                    continue;
                // item in slot present
                // get item handle
                var slotItem = EntityManager.GetItem(mapClient.Inventory.PersonalInventory[i]);
                ItemManager.SendItemDataToClient(mapClient, slotItem);
                // make the item appear on the client
                AddItemBySlot(mapClient, InventoryType.Personal, mapClient.Inventory.PersonalInventory[i], i);
            }
            
            // send inventory data - equipped inventory
            for (var i = 0; i < 22; i++)
            {
                if (mapClient.Inventory.EquippedInventory[i] == 0)
                    continue;
                // item in slot present
                // get item handle
                var slotItem = EntityManager.GetItem(mapClient.Inventory.EquippedInventory[i]);
                ItemManager.SendItemDataToClient(mapClient, slotItem);
                // make the item appear on the client
               AddItemBySlot(mapClient, InventoryType.EquipedInventory, mapClient.Inventory.EquippedInventory[i], i);
            }

            // send inventory data - weapon drawer
            for (var i = 0; i < 5; i++)
            {
                if (mapClient.Inventory.WeaponDrawer[i] == 0)
                    continue;
                // item in slot present
                // get item handle
                var slotItem = EntityManager.GetItem(mapClient.Inventory.WeaponDrawer[i]);
                ItemManager.SendItemDataToClient(mapClient, slotItem);
                // make the item appear on the client
                AddItemBySlot(mapClient, InventoryType.WeaponDrawerInventory, mapClient.Inventory.WeaponDrawer[i], i);
                // update appearance
                PlayerManager.SetAppearanceItem(mapClient.Player, slotItem.ItemTemplate.ClassId, -2139062144);
            }

            PlayerManager.UpdateAppearance(mapClient);
            NotifyEquipmentUpdate(mapClient);
        }

        public static void NotifyEquipmentUpdate(MapChannelClient mapClient)
        {
            mapClient.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new EquipmentInfoPacket { WeaponDrawer = mapClient.Inventory.WeaponDrawer[mapClient.Inventory.ActiveWeaponDrawer] });
        }

        public static void RemoveItemBySlot(Client client, InventoryType inventoryType, int slotIndex)
        {
            // get entityId from slot
            uint entityId = 0;
            //var slotType = 0;
            var tempItem = new Item();
            if (inventoryType == InventoryType.Personal)
            {
                entityId = client.MapClient.Inventory.PersonalInventory[slotIndex];
                tempItem = EntityManager.GetItem(client.MapClient.Inventory.PersonalInventory[slotIndex]);
                client.MapClient.Inventory.PersonalInventory[slotIndex] = 0; // update slot
            }
            else if (inventoryType == InventoryType.EquipedInventory)
            {
                entityId = client.MapClient.Inventory.EquippedInventory[slotIndex];
                tempItem = EntityManager.GetItem(client.MapClient.Inventory.EquippedInventory[slotIndex]);
                client.MapClient.Inventory.EquippedInventory[slotIndex] = 0; // update slot
                //slotType = 250;
            }
            else if (inventoryType == InventoryType.WeaponDrawerInventory)
            {
                entityId = client.MapClient.Inventory.WeaponDrawer[slotIndex];
                tempItem = EntityManager.GetItem(client.MapClient.Inventory.WeaponDrawer[slotIndex]);
                client.MapClient.Inventory.WeaponDrawer[slotIndex] = 0; // update slot
                //slotType = 250 + 22;
            }
            if (entityId == 0)
            {
                Console.WriteLine("RemoveItemBySlot: Invalid inventoryType{0}/slotIndex{1}\n", inventoryType, slotIndex);
                return;
            }
            // send inventoryRemoveItem
            client.SendPacket(9, new InventoryRemoveItemPacket { InventoryType = (int)inventoryType, EntityId = (int)entityId });
            // update item in database
            ItemsTable.UpdateItem(tempItem.EntityId, client.MapClient.Player.CharacterId, -1, tempItem.Stacksize);
        }

        public static void RequestEquipWeapon(Client client, RequestEquipWeaponPacket packet)
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
            uint entityIdEquippedItem = 0;
            uint entityIdInventoryItem = 0;
            entityIdEquippedItem = client.MapClient.Inventory.WeaponDrawer[destSlot]; // the old equipped item (can be none)
            entityIdInventoryItem = client.MapClient.Inventory.PersonalInventory[srcSlot]; // the new equipped item (can be none)
            // can we equip the item?
            Item itemToEquip = null;
            if (entityIdInventoryItem != 0)
            {
                itemToEquip =  EntityManager.GetItem(entityIdInventoryItem);
                // min level criteria met?
                if (itemToEquip != null && client.MapClient.Player.Level < itemToEquip.ItemTemplate.ReqLevel)
                {
                    // level too low, cannot equip item
                    return;
                }
            }
            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[srcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, srcSlot);
            if (client.MapClient.Inventory.WeaponDrawer[destSlot] != 0)
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, destSlot);
            if (entityIdEquippedItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.Personal, entityIdEquippedItem, srcSlot);
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, entityIdInventoryItem, destSlot);
            // update appearance
            if (destSlot == client.MapClient.Inventory.ActiveWeaponDrawer)
            {
                NotifyEquipmentUpdate(client.MapClient);
                if (itemToEquip == null)
                {
                    // remove item graphic if dequipped
                    var prevEquippedItem = EntityManager.GetItem(entityIdEquippedItem);
                    PlayerManager.RemoveAppearanceItem(client.MapClient.Player, prevEquippedItem.ItemTemplate.ClassId);
                }
                else
                {
                    PlayerManager.SetAppearanceItem(client.MapClient.Player, itemToEquip.ItemTemplate.ClassId, -2139062144);
                }
                PlayerManager.UpdateAppearance(client.MapClient);
            }
        }

        public static void RequestWeaponReload(MapChannelClient mapClient, Item weapon, bool tellSelf)
        {
            // find and eat a piece of ammunition
            var ammoClassId = weapon.ItemTemplate.Weapon.AmmoClassId;
            bool foundAmmo = false;
            for (var i = 0; i < 50; i++)
            {
                if (mapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;
                var weaponAmmo = EntityManager.GetItem(mapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);
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
                return; // no ammo found -> Todo: Tell the client?
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

        public static void RequestTooltipForItemTemplateId(Client client, int itemTemplateId)
        {

            var itemTemplate = ItemManager.GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return; // todo: even answer on a unknown template, else the client will continue to spam us with requests


            client.SendPacket(12, new ItemTemplateTooltipInfoPacket {ItemTemplate = itemTemplate});
        }

        public static void WeaponDrawerInventory_MoveItem(Client client, WeaponDrawerInventory_MoveItemPacket packet)
        {
            var srcEquippedItem = client.MapClient.Inventory.WeaponDrawer[packet.SrcSlot];
            var destEquippedItem = client.MapClient.Inventory.WeaponDrawer[packet.DestSlot];
            // swap items on the client and server
            if (destEquippedItem != 0)
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.DestSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEquippedItem, packet.DestSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, destEquippedItem, packet.SrcSlot);
            }
            else
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEquippedItem, packet.DestSlot);
            }
        }
        /*void item_recv_PersonalInventoryDestroyItem(mapChannelClient_t* client, uint8* pyString, sint32 pyStringLen)
        {
            pyUnmarshalString_t pums;
            pym_init(&pums, pyString, pyStringLen);
            if (!pym_unpackTuple_begin(&pums))
                return;
            unsigned long long entityId = pym_unpackInt(&pums);
            if (pums.unpackErrorEncountered)
                return;
            if (entityId == NULL)
                return;
            item_t* item = (item_t*)entityMgr_get(entityId);
            inventory_reduceStackCount(client, item, item->stacksize);
        }*/
    }
}
