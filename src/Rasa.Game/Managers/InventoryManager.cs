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

        public void AddItemBySlot(MapChannelClient mapClient, InventoryType inventoryType, uint entityId, int slotId, bool updateDB)
        {
            var tempItem = EntityManager.Instance.GetItem(entityId);
            if (tempItem == null)
                return;
            // set entityId in slot
            var destSlot = 0;

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    destSlot = slotId;
                    mapClient.Inventory.PersonalInventory[slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    destSlot = 250 + slotId;
                    mapClient.Inventory.EquippedInventory[slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    destSlot = 250 + 22 + slotId;
                    mapClient.Inventory.WeaponDrawer[slotId] = tempItem.EntityId; // update slot
                    break;
                default:
                    Console.WriteLine("Unsuported inventory type");
                    break;
            }
            // send inventoryAddItem
            mapClient.Player.Client.SendPacket(9, new InventoryAddItemPacket { Type = inventoryType, EntityId = tempItem.EntityId, SlotId = slotId });
            // update item in database
            if (updateDB)
                CharacterInventoryTable.MoveInvItem(mapClient.Player.CharacterId, destSlot, tempItem.ItemId);
        }

        public Item AddItemToInventory(MapChannelClient mapClient, Item item)
        {
            if (item == null)
                return null;

            var itemClassInfo = EntityClassManager.Instance.LoadedEntityClasses[item.ItemTemplate.ClassId].ItemClassInfo;

            // get item category offset
            var itemCategoryOffset = (int)item.ItemTemplate.InventoryCategory - 1;
            if (itemCategoryOffset < 0 || itemCategoryOffset >= 5)
            {
                Logger.WriteLog(LogType.Error, $"AddItemToInventory: ItemTemplateId = {item.ItemTemplate.ItemTemplateId} inventory category = {item.ItemTemplate.InventoryCategory} is invalid");
                return null;
            }

            itemCategoryOffset *= 50;
            var stackSizeChanged = false;
            // see if we can merge the item into an already existing item
            for (var i = 0; i < 50; i++)
                if (mapClient.Inventory.PersonalInventory[itemCategoryOffset + i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(mapClient.Inventory.PersonalInventory[itemCategoryOffset + i]);

                    // same item template?
                    if (slotItem.ItemTemplate.ItemTemplateId != item.ItemTemplate.ItemTemplateId)
                        continue;

                    // calculate how many items we can add to the stack
                    var stackAdd = itemClassInfo.StackSize - slotItem.Stacksize;
                    if (stackAdd == 0)
                        continue;

                    // add item to existing stack
                    var stackMove = Math.Min(stackAdd, item.Stacksize);
                    slotItem.Stacksize += stackMove;
                    ItemsTable.UpdateItemStackSize(slotItem.ItemId, slotItem.Stacksize);

                    // remove stack's from source item
                    item.Stacksize -= stackMove;
                    stackSizeChanged = true;

                    // notify client of changed stack count
                    mapClient.Player.Client.SendPacket(slotItem.EntityId, new SetStackCountPacket(slotItem.Stacksize));

                    if (item.Stacksize == 0)
                    {
                        // destroy the item
                        EntityManager.Instance.DestroyPhysicalEntity(mapClient.Client, item.EntityId, EntityType.Item);
                        // remove from DB
                        ItemsTable.DeleteItem(item.ItemId);
                        // return the 'new' item instead
                        return slotItem;
                    }

                }

            // item have new stackSize?
            if (stackSizeChanged)
                mapClient.Client.SendPacket(item.EntityId, new SetStackCountPacket(item.Stacksize));

            // find free slot
            for (var i = 0; i < 50; i++)
            {
                if (mapClient.Inventory.PersonalInventory[itemCategoryOffset + i] == 0)
                {
                    item.OwnerId = mapClient.Player.CharacterId;
                    item.OwnerSlotId = itemCategoryOffset + i;
                    item.CurrentHitPoints = itemClassInfo.MaxHitPoints;
                    // send data to client
                    ItemManager.Instance.SendItemDataToClient(mapClient, item, false);
                    // add item to empty slot
                    AddItemBySlot(mapClient, InventoryType.Personal, item.EntityId, itemCategoryOffset + i, true);
                    CharacterInventoryTable.AddInvItem(item.OwnerId, item.OwnerSlotId, item.ItemId);
                    return item;
                }
            }

            return null;
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
            var getInventoryData = CharacterInventoryTable.GetItems(mapClient.Player.CharacterId);
            // init equiped inventory
            for (int i = 0; i < 21; i++)
                mapClient.Inventory.EquippedInventory.Add(i + 1, 0);

            foreach (var item in getInventoryData)
            {
                var itemData = ItemsTable.GetItem(item.ItemId);
                var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                if (itemTemplate == null)
                    return;

                var newItem = new Item
                {
                    OwnerId = mapClient.Player.CharacterId,
                    OwnerSlotId = item.SlotId,
                    ItemTemplate = itemTemplate,
                    Stacksize = itemData.StackSize,
                    CurrentHitPoints = itemData.CurrentHitPoints,
                    Color = itemData.Color,
                    ItemId = item.ItemId,
                    CrafterName = itemData.CrafterName
                };

                // check if item is weapon
                if (newItem.ItemTemplate.WeaponInfo != null)
                    newItem.CurrentAmmo = itemData.AmmoCount;

                // register item
                EntityManager.Instance.RegisterEntity(newItem.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(newItem.EntityId, newItem);
                
                // fill invenoty slot
                ItemManager.Instance.SendItemDataToClient(mapClient, newItem, false);

                if (item.SlotId < 250)
                {
                    mapClient.Inventory.PersonalInventory[item.SlotId] = newItem.EntityId;
                    // make the item appear on the client
                    AddItemBySlot(mapClient, InventoryType.Personal, mapClient.Inventory.PersonalInventory[item.SlotId], item.SlotId, false);
                }
                else if (249 < item.SlotId && item.SlotId < 272)
                {
                    mapClient.Inventory.EquippedInventory[item.SlotId - 250] = newItem.EntityId;
                    // make the item appear on the client
                    AddItemBySlot(mapClient, InventoryType.EquipedInventory, mapClient.Inventory.EquippedInventory[item.SlotId - 250], item.SlotId - 250, false);
                }
                else if (271 < item.SlotId)
                {
                    mapClient.Inventory.WeaponDrawer[item.SlotId - 250 - 22] = newItem.EntityId;
                    mapClient.Inventory.EquippedInventory[13] = newItem.EntityId;
                    // make the item appear on the client
                    AddItemBySlot(mapClient, InventoryType.WeaponDrawerInventory, mapClient.Inventory.WeaponDrawer[item.SlotId - 250 - 22], item.SlotId - 250 - 22, false);
                }
            }

            PlayerManager.Instance.NotifyEquipmentUpdate(mapClient);
        }

        public void PersonalInventory_DestroyItem(Client client, PersonalInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;
            var tempItem = EntityManager.Instance.GetItem((uint)packet.EntityId);
            ReduceStackCount(client, tempItem, (int)packet.Quantity);
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

            RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.MapClient.Inventory.PersonalInventory[packet.DestSlot] != 0)
                AddItemBySlot(client.MapClient, InventoryType.Personal, client.MapClient.Inventory.PersonalInventory[packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client.MapClient, InventoryType.Personal, entityId, packet.DestSlot, true);
        }

        public void ReduceStackCount(Client client, Item tempItem, int stackDecreaseCount)
        {
            if (tempItem.OwnerId != client.MapClient.Player.CharacterId)
                return; // item is not on this client's inventory
            var newStackCount = tempItem.Stacksize - stackDecreaseCount;
            if (newStackCount <= 0)
            {
                // destroy item
                EntityManager.Instance.DestroyPhysicalEntity(client, tempItem.EntityId, EntityType.Item);
                client.SendPacket(9, new InventoryRemoveItemPacket { InventoryType = InventoryType.Personal, EntityId = (int)tempItem.EntityId });
                // free slot
                FreeSlotIndex(client.MapClient, InventoryType.Personal, tempItem.OwnerSlotId);
                // Update db
                CharacterInventoryTable.DeleteInvItem(client.MapClient.Player.CharacterId, tempItem.OwnerSlotId);
                // ToDo will we delete items from db, or we will let tham stay, so thay can be retrived
                //ItemsTable.DeleteItem(tempItem.ItemId);
            }
            else
            {
                // update stack count
                tempItem.Stacksize = newStackCount;
                // set stackcount
                client.SendPacket(tempItem.EntityId, new SetStackCountPacket(newStackCount));
                // update stack count in database
                ItemsTable.UpdateItemStackSize(tempItem.ItemId, tempItem.Stacksize);
            }
        }

        public void RemoveItemBySlot(Client client, InventoryType inventoryType, int slotIndex)
        {
            var entityId = 0U;

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    entityId = client.MapClient.Inventory.PersonalInventory[slotIndex];
                    client.MapClient.Inventory.PersonalInventory[slotIndex] = 0;
                    break;
                case InventoryType.EquipedInventory:
                    entityId = client.MapClient.Inventory.EquippedInventory[slotIndex];
                    client.MapClient.Inventory.EquippedInventory[slotIndex] = 0;
                    break;
                case InventoryType.WeaponDrawerInventory:
                    entityId = client.MapClient.Inventory.WeaponDrawer[slotIndex];
                    client.MapClient.Inventory.WeaponDrawer[slotIndex] = 0;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"RemoveItemBySlot: Unsuported Inventory type {inventoryType}");
                    return;
            }

            client.SendPacket(9, new InventoryRemoveItemPacket { InventoryType = inventoryType, EntityId = (int)entityId });
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
            var entityIdEquippedItem = client.MapClient.Inventory.EquippedInventory[packet.DestSlot]; // the old equipped item (can be none)
            var entityIdInventoryItem = client.MapClient.Inventory.PersonalInventory[packet.SrcSlot]; // the new equipped item (can be none)
            // can we equip the item?
            Item itemToEquip = null;
            if (entityIdInventoryItem != 0)
            {
                itemToEquip = EntityManager.Instance.GetItem(entityIdInventoryItem);
                // min level criteria met?
                if (itemToEquip != null && itemToEquip.ItemTemplate.ItemInfo.Requirements.ContainsKey(RequirementsType.ReqXpLevel))
                    if (client.MapClient.Player.Level < itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevel])
                    {
                        // level too low, cannot equip item
                        CommunicatorManager.Instance.SystemMessage(client.MapClient, "Level too low, cannot equip item.");
                        return;
                    }
            }
            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[packet.SrcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);
            if (client.MapClient.Inventory.EquippedInventory[packet.DestSlot] != 0)
                RemoveItemBySlot(client, InventoryType.EquipedInventory, packet.DestSlot);
            if (entityIdEquippedItem != 0)
            {
                AddItemBySlot(client.MapClient, InventoryType.Personal, entityIdEquippedItem, packet.SrcSlot, true);
            }
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.EquipedInventory, entityIdInventoryItem, packet.DestSlot, true);
            // update appearance
            if (itemToEquip == null)
            {
                // remove item graphic if dequipped
                var prevEquippedItem = EntityManager.Instance.GetItem(entityIdEquippedItem);
                var equipableClassInfo = EntityClassManager.Instance.LoadedEntityClasses[prevEquippedItem.ItemTemplate.ClassId].EquipableClassInfo;
                PlayerManager.Instance.RemoveAppearanceItem(client.MapClient.Player, equipableClassInfo.EquipmentSlotId);
            }
            else
                PlayerManager.Instance.SetAppearanceItem(client.MapClient.Player, itemToEquip);
            
            PlayerManager.Instance.UpdateAppearance(client.MapClient);
            PlayerManager.Instance.UpdateStatsValues(client.MapClient, false);
            PlayerManager.Instance.NotifyEquipmentUpdate(client.MapClient);
        }

        public void RequestEquipWeapon(Client client, RequestEquipWeaponPacket packet)
        {
            var srcSlot = packet.SrcSlot;
            var invType = packet.InventoryType;
            var destSlot = packet.DestSlot;
            if (invType != InventoryType.Personal)
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
                itemToEquip = EntityManager.Instance.GetItem(entityIdInventoryItem);

                if (itemToEquip != null && itemToEquip.ItemTemplate.ItemInfo.Requirements.ContainsKey(RequirementsType.ReqXpLevel))
                    if (client.MapClient.Player.Level < itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevel])
                    {
                        // level too low, cannot equip item
                        CommunicatorManager.Instance.SystemMessage(client.MapClient, "Level too low, cannot equip item.");
                        return;
                    }
            }
            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[srcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, srcSlot);
            if (client.MapClient.Inventory.WeaponDrawer[destSlot] != 0)
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, destSlot);
            if (entityIdEquippedItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.Personal, entityIdEquippedItem, srcSlot, true);
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, entityIdInventoryItem, destSlot, true);
            
            // Tell client that he have new weapon
            PlayerManager.Instance.NotifyEquipmentUpdate(client.MapClient);

            if (destSlot == client.MapClient.Inventory.ActiveWeaponDrawer)
            {
                if (itemToEquip == null)
                {
                    // remove item graphic if dequipped
                    var prevEquippedItem = EntityManager.Instance.GetItem(entityIdEquippedItem);
                    var equipableClassInfo = EntityClassManager.Instance.LoadedEntityClasses[prevEquippedItem.ItemTemplate.ClassId].EquipableClassInfo;
                    PlayerManager.Instance.RemoveAppearanceItem(client.MapClient.Player, equipableClassInfo.EquipmentSlotId);
                }
                else
                {
                    PlayerManager.Instance.SetAppearanceItem(client.MapClient.Player, itemToEquip);
                }
                PlayerManager.Instance.UpdateAppearance(client.MapClient);
            }
        }

        public void RequestTooltipForItemTemplateId(Client client, int itemTemplateId)
        {

            var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[itemTemplate.ClassId];

            if (itemTemplate == null)
            {
                Logger.WriteLog(LogType.Error, $"RequestTooltipForItemTemplateId: Unknown itemTemplateId {itemTemplateId}");
                return; // todo: even answer on a unknown template, else the client will continue to spam us with requests
            }
            client.SendPacket(12, new ItemTemplateTooltipInfoPacket(itemTemplate, classInfo));
        }

        public void RequestTooltipForModuleId(Client client, int moduleId)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo: RequestTooltipForModuleId");
            //var moduleInfo = new ItemModule(moduleId, 1, new ModuleInfo(1, 1, 1, 1, 1, 1, 1, 1, 1));

            //client.SendPacket(12, new ModuleTooltipInfoPacket(moduleInfo));
        }

        public void WeaponDrawerInventory_MoveItem(Client client, WeaponDrawerInventory_MoveItemPacket packet)
        {
            var srcEntityId = client.MapClient.Inventory.WeaponDrawer[packet.SrcSlot];
            var destEntityId = client.MapClient.Inventory.WeaponDrawer[packet.DestSlot];
            // swap items on the client and server
            if (destEntityId != 0)
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.DestSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEntityId, packet.DestSlot, true);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, destEntityId, packet.SrcSlot, true);
            }
            else
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                AddItemBySlot(client.MapClient, InventoryType.WeaponDrawerInventory, srcEntityId, packet.DestSlot, true);
            }
        }
    }
}
