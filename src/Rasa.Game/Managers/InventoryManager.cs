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

        public void AddItemBySlot(Client client, InventoryType inventoryType, uint entityId, int slotId, bool updateDB)
        {
            var tempItem = EntityManager.Instance.GetItem(entityId);

            if (tempItem == null)
                return;
            // set entityId in slot

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    client.MapClient.Inventory.PersonalInventory[slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.HomeInventory:
                    client.MapClient.Inventory.HomeInventory[slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    client.MapClient.Inventory.EquippedInventory[slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    client.MapClient.Inventory.WeaponDrawer[slotId] = tempItem.EntityId; // update slot
                    break;
                default:
                    Console.WriteLine("Unsuported inventory type");
                    break;
            }
            // send inventoryAddItem
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(inventoryType, tempItem.EntityId, slotId));

            var characterSlot = client.AccountEntry.SelectedSlot;

            if (inventoryType == InventoryType.HomeInventory)
                characterSlot = 0;

            // update item in database
            if (updateDB)
                CharacterInventoryTable.MoveInvItem(client.AccountEntry.Id, characterSlot, (int)inventoryType, slotId, tempItem.ItemId);
        }

        public Item AddItemToInventory(Client client, Item item)
        {
            if (item == null)
                return null;

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);

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
                if (client.MapClient.Inventory.PersonalInventory[itemCategoryOffset + i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(client.MapClient.Inventory.PersonalInventory[itemCategoryOffset + i]);

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
                    client.CallMethod(slotItem.EntityId, new SetStackCountPacket(slotItem.Stacksize));

                    if (item.Stacksize == 0)
                    {
                        // destroy the item
                        EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                        // remove from DB
                        ItemsTable.DeleteItem(item.ItemId);
                        // return the 'new' item instead
                        return slotItem;
                    }

                }

            // item have new stackSize?
            if (stackSizeChanged)
                client.CallMethod(item.EntityId, new SetStackCountPacket(item.Stacksize));

            // find free slot
            for (var i = 0; i < 50; i++)
            {
                if (client.MapClient.Inventory.PersonalInventory[itemCategoryOffset + i] == 0)
                {
                    item.OwnerId = client.AccountEntry.SelectedSlot;
                    item.OwnerSlotId = itemCategoryOffset + i;
                    item.CurrentHitPoints = itemClassInfo.MaxHitPoints;
                    // send data to client
                    ItemManager.Instance.SendItemDataToClient(client, item, false);
                    // add item to empty slot
                    AddItemBySlot(client, InventoryType.Personal, item.EntityId, itemCategoryOffset + i, true);
                    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, item.OwnerId, (int)InventoryType.Personal, item.OwnerSlotId, item.ItemId);
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
            switch (inventoryType)
            {
                case InventoryType.Personal:
                    mapClient.Inventory.PersonalInventory[slotIndex] = 0; // update slot
                    break;
                case InventoryType.HomeInventory:
                    mapClient.Inventory.HomeInventory[slotIndex] = 0; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    mapClient.Inventory.EquippedInventory[slotIndex] = 0; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    mapClient.Inventory.WeaponDrawer[slotIndex] = 0;    // update slot
                    break;
                default:
                    Console.WriteLine("RemoveItemBySlot: Invalid inventoryType{0}/slotIndex{1}\n", inventoryType, slotIndex);
                    break;
            }

            return slotIndex;
        }

        public void HomeInventory_DestroyItem(Client client, HomeInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem((uint)packet.EntityId);

            ReduceStackCount(client, InventoryType.HomeInventory, tempItem, (int)packet.Quantity);

            // ToDo delete item from db? or we sill keep all items
        }

        public void HomeInventory_MoveItem(Client client, HomeInventory_MoveItemPacket packet)
        {
            // remove item
            if (packet.SrcSlot == packet.DestSlot)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 480)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 480)
                return;

            var entityId = client.MapClient.Inventory.HomeInventory[packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.HomeInventory, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.MapClient.Inventory.HomeInventory[packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.HomeInventory, client.MapClient.Inventory.HomeInventory[packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.HomeInventory, entityId, packet.DestSlot, true);
        }

        public void InitForClient(Client client)
        {
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket { InventoryType = (int)InventoryType.Personal, InventorySize = 250 });
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket { InventoryType = (int)InventoryType.HomeInventory, InventorySize = 480 });
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket { InventoryType = (int)InventoryType.WeaponDrawerInventory, InventorySize = 5 });
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket { InventoryType = (int)InventoryType.EquipedInventory, InventorySize = 22 });
            
            InitCharacterInventory(client);
        }

        public void InitCharacterInventory(Client client)
        {
            var getInventoryData = CharacterInventoryTable.GetItems(client.AccountEntry.Id);
            
            // init for server inventory
            for (int i = 0; i < 480; i++)
                client.MapClient.Inventory.HomeInventory.Add(i, 0);

            for (int i = 1; i <= 21; i++)
                client.MapClient.Inventory.EquippedInventory.Add(i, 0);

            foreach (var item in getInventoryData)
            {
                var itemData = ItemsTable.GetItem(item.ItemId);
                var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                if (itemTemplate == null)
                    return;

                var newItem = new Item
                {
                    OwnerId = item.CharacterSlot,
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
                ItemManager.Instance.SendItemDataToClient(client, newItem, false);

                if (item.CharacterSlot == client.AccountEntry.SelectedSlot)
                {
                    if ((InventoryType)item.InventoryType == InventoryType.Personal)
                    {
                        client.MapClient.Inventory.PersonalInventory[item.SlotId] = newItem.EntityId;
                        // make the item appear on the client
                        AddItemBySlot(client, InventoryType.Personal, client.MapClient.Inventory.PersonalInventory[item.SlotId], item.SlotId, false);
                    }
                    else if ((InventoryType)item.InventoryType == InventoryType.EquipedInventory)
                    {
                        client.MapClient.Inventory.EquippedInventory[item.SlotId] = newItem.EntityId;
                        // make the item appear on the client
                        AddItemBySlot(client, InventoryType.EquipedInventory, client.MapClient.Inventory.EquippedInventory[item.SlotId], item.SlotId, false);
                    }
                    else if ((InventoryType)item.InventoryType == InventoryType.WeaponDrawerInventory)
                    {
                        client.MapClient.Inventory.WeaponDrawer[item.SlotId] = newItem.EntityId;
                        client.MapClient.Inventory.EquippedInventory[13] = newItem.EntityId;
                        // make the item appear on the client
                        AddItemBySlot(client, InventoryType.WeaponDrawerInventory, client.MapClient.Inventory.WeaponDrawer[item.SlotId], item.SlotId, false);
                    }
                }
                else if (item.CharacterSlot == 0)
                    if ((InventoryType)item.InventoryType == InventoryType.HomeInventory)
                    {
                        client.MapClient.Inventory.HomeInventory[item.SlotId] = newItem.EntityId;
                        // make the item appear on the client
                        AddItemBySlot(client, InventoryType.HomeInventory, client.MapClient.Inventory.HomeInventory[item.SlotId], item.SlotId, false);
                    }
            }

            PlayerManager.Instance.NotifyEquipmentUpdate(client);
        }

        public void PersonalInventory_DestroyItem(Client client, PersonalInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem((uint)packet.EntityId);

            ReduceStackCount(client, InventoryType.Personal, tempItem, (int)packet.Quantity);

            // ToDo delete item from db? or we sill keep all items
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
                AddItemBySlot(client, InventoryType.Personal, client.MapClient.Inventory.PersonalInventory[packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.Personal, entityId, packet.DestSlot, true);
        }

        public void ReduceStackCount(Client client, InventoryType inventoryType, Item tempItem, int stackDecreaseCount)
        {
            if (tempItem.OwnerId != client.AccountEntry.SelectedSlot)
                return; // item is not on this client's inventory

            var newStackCount = tempItem.Stacksize - stackDecreaseCount;

            if (newStackCount <= 0)
            {

                // destroy item
                EntityManager.Instance.DestroyPhysicalEntity(client, tempItem.EntityId, EntityType.Item);
                client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket { InventoryType = InventoryType.Personal, EntityId = (int)tempItem.EntityId });
                // free slot
                FreeSlotIndex(client.MapClient, inventoryType, tempItem.OwnerSlotId);
                // Update db
                var characterSlot = client.AccountEntry.SelectedSlot;

                if (inventoryType == InventoryType.HomeInventory)
                    characterSlot = 0;

                CharacterInventoryTable.DeleteInvItem(client.AccountEntry.Id, characterSlot, (int)InventoryType.Personal, tempItem.OwnerSlotId);
                // ToDo will we delete items from db, or we will let tham stay, so thay can be retrived
                //ItemsTable.DeleteItem(tempItem.ItemId);
            }
            else
            {
                // update stack count
                tempItem.Stacksize = newStackCount;
                // set stackcount
                client.CallMethod(tempItem.EntityId, new SetStackCountPacket(newStackCount));
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
                case InventoryType.HomeInventory:
                    entityId = client.MapClient.Inventory.HomeInventory[slotIndex];
                    client.MapClient.Inventory.HomeInventory[slotIndex] = 0;
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

            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket { InventoryType = inventoryType, EntityId = (int)entityId });
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
                        CommunicatorManager.Instance.SystemMessage(client, "Level too low, cannot equip item.");
                        return;
                    }
            }

            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[packet.SrcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);

            if (client.MapClient.Inventory.EquippedInventory[packet.DestSlot] != 0)
                RemoveItemBySlot(client, InventoryType.EquipedInventory, packet.DestSlot);

            if (entityIdEquippedItem != 0)
                AddItemBySlot(client, InventoryType.Personal, entityIdEquippedItem, packet.SrcSlot, true);

            if (entityIdInventoryItem != 0)
                AddItemBySlot(client, InventoryType.EquipedInventory, entityIdInventoryItem, packet.DestSlot, true);
            
            // update appearance
            if (itemToEquip == null)
            {
                // remove item graphic if dequipped
                var prevEquippedItem = EntityManager.Instance.GetItem(entityIdEquippedItem);
                var equipableClassInfo = EntityClassManager.Instance.GetEquipableClassInfo(prevEquippedItem);
                PlayerManager.Instance.RemoveAppearanceItem(client, equipableClassInfo.EquipmentSlotId);
            }
            else
                PlayerManager.Instance.SetAppearanceItem(client, itemToEquip);
            
            PlayerManager.Instance.UpdateAppearance(client);
            PlayerManager.Instance.UpdateStatsValues(client, false);
            PlayerManager.Instance.NotifyEquipmentUpdate(client);
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
                        CommunicatorManager.Instance.SystemMessage(client, "Level too low, cannot equip item.");
                        return;
                    }
            }
            
            // swap items on the client and server
            if (client.MapClient.Inventory.PersonalInventory[srcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, srcSlot);
            if (client.MapClient.Inventory.WeaponDrawer[destSlot] != 0)
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, destSlot);
            if (entityIdEquippedItem != 0)
                AddItemBySlot(client, InventoryType.Personal, entityIdEquippedItem, srcSlot, true);
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, entityIdInventoryItem, destSlot, true);

            // Tell client that he have new weapon
            PlayerManager.Instance.NotifyEquipmentUpdate(client);

            if (destSlot == client.MapClient.Inventory.ActiveWeaponDrawer)
                if (itemToEquip == null)
                {
                    // remove item graphic if dequipped
                    var prevEquippedItem = EntityManager.Instance.GetItem(entityIdEquippedItem);
                    var equipableClassInfo = EntityClassManager.Instance.GetEquipableClassInfo(prevEquippedItem);

                    PlayerManager.Instance.RemoveAppearanceItem(client, equipableClassInfo.EquipmentSlotId);
                }
                else
                    PlayerManager.Instance.SetAppearanceItem(client, itemToEquip);

            PlayerManager.Instance.UpdateAppearance(client);
        }

        public void RequestLockboxTabPermissions(Client client)
        {
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.MapClient.Player.LockboxTabs));
        }

        public void RequestMoveItemToHomeInventory(Client client, RequestMoveItemToHomeInventoryPacket packet)
        {
            // remove item
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 480)
                return;

            var entityId = client.MapClient.Inventory.PersonalInventory[packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.MapClient.Inventory.HomeInventory[packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.Personal, client.MapClient.Inventory.HomeInventory[packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.HomeInventory, entityId, packet.DestSlot, true);
        }

        public void RequestTakeItemFromHomeInventory(Client client, RequestTakeItemFromHomeInventoryPacket packet)
        {
            // remove item
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 480)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 250)
                return;

            var entityId = client.MapClient.Inventory.HomeInventory[packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.HomeInventory, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.MapClient.Inventory.PersonalInventory[packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.HomeInventory, client.MapClient.Inventory.PersonalInventory[packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.Personal, entityId, packet.DestSlot, true);
        }

        public void RequestTooltipForItemTemplateId(Client client, uint itemTemplateId)
        {

            var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            var classInfo = EntityClassManager.Instance.GetClassInfo(itemTemplate.Class);

            if (itemTemplate == null)
            {
                Logger.WriteLog(LogType.Error, $"RequestTooltipForItemTemplateId: Unknown itemTemplateId {itemTemplateId}");
                return; // todo: even answer on a unknown template, else the client will continue to spam us with requests
            }
            client.CallMethod(SysEntity.ClientGameUIManagerId, new ItemTemplateTooltipInfoPacket(itemTemplate, classInfo));
        }

        public void RequestTooltipForModuleId(Client client, int moduleId)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo: RequestTooltipForModuleId");
            //var moduleInfo = new ItemModule(moduleId, 1, new ModuleInfo(1, 1, 1, 1, 1, 1, 1, 1, 1));

            //client.SendPacket(12, new ModuleTooltipInfoPacket(moduleInfo));
        }

        public void TransferCreditToLockbox(Client client, int amount)
        {
            /*
             * ToDo:
             * there is some bug with withdraw if withdraw value is less then 256
             * client send positive value, insted of negative one
             * so we will set min transfer value to 500 for now
             * we can take closer look at this later
             */

            //deposit
            if (amount >= 500)
            {
                if (client.MapClient.Player.Credits >= amount)
                {
                    var deposit = client.MapClient.Player.LockboxCredits + amount;

                    PlayerManager.Instance.GainCredits(client, -amount);

                    client.CallMethod(client.MapClient.Player.Actor.EntityId, new LockboxFundsPacket(deposit));

                    client.MapClient.Player.LockboxCredits = (uint)deposit;
                    CharacterLockboxTable.UpdateCredits(client.AccountEntry.Id, (uint)deposit);
                }
                else
                    CommunicatorManager.Instance.SystemMessage(client, "Not enof credit's in inventory\nP.S. Go earn some credits :)");
            }
            // withdraw
            else if (amount < 0)
            {
                if (client.MapClient.Player.LockboxCredits >= -amount)
                {
                    var withdraw = client.MapClient.Player.LockboxCredits + amount;

                    PlayerManager.Instance.GainCredits(client, -amount);
                    client.CallMethod(client.MapClient.Player.Actor.EntityId, new LockboxFundsPacket(withdraw));

                    client.MapClient.Player.LockboxCredits = (uint)withdraw;
                    CharacterLockboxTable.UpdateCredits(client.AccountEntry.Id, (uint)withdraw);
                }
                else
                    CommunicatorManager.Instance.SystemMessage(client, "Not enof credit's in Lockbox\nP.S. Dont be greedy :)");
            }
            else
                CommunicatorManager.Instance.SystemMessage(client, "Minimum transfer value is 500 credits");

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
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, srcEntityId, packet.DestSlot, true);
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, destEntityId, packet.SrcSlot, true);
            }
            else
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, srcEntityId, packet.DestSlot, true);
            }
        }
    }
}
