using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Game.Missions.Persistence;
    using Packets.Communicator.Server;
    using Packets.Clan.Client;
    using Packets.Clan.Server;
    using Packets.Inventory.Client;
    using Packets.Inventory.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public partial class InventoryManager
    {
        /*    Inventory Packets:
         *      Done:
         *  - AddBuybackItem
         *  - InventoryAddItem
         *  - InventoryCreate
         *  - InventoryRemoveItem
         *  - LockboxTabPermissions
         *  - RemoveBuybackItem
         *  
         *      ToDo:
         *  - AddAuctionItem
         *  - AddInboxItem
         *  - AddOverflowItem
         *  - AddWagerItem
         *  - InventoryDestroy
         *  - InventoryMoveFailed
         *  - InventoryReload
         *  - RemoveAuctionItem
         *  - RemoveInboxItem
         *  - RemoveOverflowItem
         *  - RemoveWagerItem
         *  - ResetAuctionInventory
         *  - ResetBuybackInventory
         *  - ResetInboxInventory
         *  - ResetOverflowInventory
         *  - ResetWagerInventory
         *  
         *    Inventory Handlers:
         *  - ClanLockbox_DepositItemInSlot         => implemented
         *  - ClanLockbox_DepositItemInTab          => implemented
         *  - ClanLockbox_DestroyItem               => implemented
         *  - ClanLockbox_MoveItem                  => implemented
         *  - ClanLockbox_WithdrawItem              => implemented
         *  - HomeInventory_DestroyItem             => implemented
         *  - HomeInventory_MoveItem                => implemented
         *  - OverflowTransfer                      => ToDo
         *  - PersonalInventory_DestroyItem         => implemented
         *  - PersonalInventory_MoveItem            => implemented
         *  - PurchaseClanLockboxTab                => ToDo
         *  - PurchaseLockboxTab                    => implemented
         *  - RequestEquipArmor                     => implemented
         *  - RequestEquipWeapon                    => implemented
         *  - RequestLockboxTabPermissions          => implemented
         *  - RequestMoveItemToHomeInventory        => implemented
         *  - RequestTakeItemFromHomeInventory      => implemented
         *  - RequestTakeItemFromInboxInventory     => done
         *  - TransferCreditToLockbox               => implemented
         *  - WeaponDrawerInventory_MoveItem        => implemented
         */

        private static InventoryManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly ManifestationManager _currencyManager;
        private readonly CharacterManager _characterManager;
        private readonly MissionApplication _missionManager;
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
                            _instance = new InventoryManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        internal InventoryManager(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            MissionApplication missionManager = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _currencyManager = new ManifestationManager(gameUnitOfWorkFactory);
            _characterManager = new CharacterManager(gameUnitOfWorkFactory);
            _missionManager = missionManager;
        }

        #region Handlers

        private bool IsProtected(Item item) => MissionItemProtection.IsProtected(item, _gameUnitOfWorkFactory);
        private bool HasProtected(params ulong[] entityIds) =>
            entityIds.Where(id => id != 0).Any(id => IsProtected(EntityManager.Instance.GetItem(id)));
        private static List<ulong> Slots(Manifestation player, InventoryType inventoryType) => inventoryType switch
        {
            InventoryType.Personal => player.Inventory.PersonalInventory,
            InventoryType.HomeInventory => player.Inventory.HomeInventory,
            InventoryType.EquipedInventory => player.Inventory.EquippedInventory,
            InventoryType.WeaponDrawerInventory => player.Inventory.WeaponDrawer,
            InventoryType.ClanInventory => player.Inventory.ClanInventory,
            _ => null
        };

        public void HomeInventory_DestroyItem(Client client, HomeInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(packet.EntityId);

            // An id that is not an item used to be passed on and dereferenced.
            if (tempItem == null)
                return;

            ReduceStackCount(client, InventoryType.HomeInventory, tempItem, packet.Quantity);

            // ToDo delete item from db? or we sill keep all items
        }

        public void HomeInventory_MoveItem(Client client, HomeInventory_MoveItemPacket packet)
        {
            // remove item
            if (packet.SrcSlot == packet.DestSlot)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= LockboxTab.TotalSlots)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= LockboxTab.TotalSlots)
                return;

            // The source may sit in a tab that is no longer unlocked - nothing takes items out of
            // a tab, so moving them down out of one has to keep working. Only the destination is
            // gated.
            if (!HomeSlotIsUnlocked(client, packet.DestSlot))
                return;

            var entityId = client.Player.Inventory.HomeInventory[(int)packet.SrcSlot];

            if (entityId == 0 || HasProtected(entityId, client.Player.Inventory.HomeInventory[(int)packet.DestSlot]))
                return;

            RemoveItemBySlot(client, InventoryType.HomeInventory, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.HomeInventory[(int)packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.HomeInventory, client.Player.Inventory.HomeInventory[(int)packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.HomeInventory, entityId, packet.DestSlot, true);
        }

        /// <summary>
        /// Whether the character may put something in that home inventory slot. All 480 slots were
        /// addressable whatever the player had paid for, so the tabs bought nothing at all: the
        /// client hides the locked ones and the server believed whatever slot arrived.
        /// The client never sends one of these - it resolves the slot inside the tab itself and
        /// answers PmYourFootlockerIsFull when it cannot - so a refusal here is logged, not
        /// explained.
        /// </summary>
        private static bool HomeSlotIsUnlocked(Client client, uint slot)
        {
            var unlocked = LockboxTab.UnlockedSlots(client.Player.LockboxTabs);

            if (slot < unlocked)
                return true;

            Logger.WriteLog(LogType.Security, $"{client.AccountEntry.FamilyName} tried home inventory slot {slot} holding {client.Player.LockboxTabs} lockbox tab(s) ({unlocked} slots).");
            return false;
        }

        public void PersonalInventory_DestroyItem(Client client, PersonalInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(packet.EntityId);

            // An id that is not an item used to be passed on and dereferenced.
            if (tempItem == null)
                return;

            ReduceStackCount(client, InventoryType.Personal, tempItem, packet.Quantity);

            // ToDo delete item from db? or we sill keep all items
        }

        public void PersonalInventory_MoveItem(Client client, PersonalInventory_MoveItemPacket packet)
        {
            // remove item
            if (packet.SrcSlot == packet.DestSlot)
                return;

            // Every slot check in this file is against the list's size, exclusive: several
            // used to be inclusive, so the index one past the end passed and the list threw.
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
            {
                Logger.WriteLog(LogType.Debug, $"SrcSlot out of range => {packet.SrcSlot}");
                return;
            }

            if (packet.DestSlot < 0 || packet.DestSlot >= 250)
            {
                Logger.WriteLog(LogType.Debug, $"DestSlot out of range => {packet.DestSlot}");
                return;
            }

            var entityId = client.Player.Inventory.PersonalInventory[packet.SrcSlot];

            if (entityId == 0)
                return;
            lock (client.SyncRoot)
            {
                try
                {
                    InventoryPlan plan = null;
                    using (var unit = _gameUnitOfWorkFactory.CreateChar())
                        unit.ExecuteTransaction(() =>
                        {
                            plan = InventoryPlan.For(client, unit);
                            plan.Move((uint)packet.SrcSlot, (uint)packet.DestSlot);
                        });
                    plan.Publish(client);
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error, $"Inventory item move rejected: {error.Message}");
                }
            }
        }

        /// <summary>
        /// Unlocks the next home lockbox tab, at the price the client quoted for it.
        ///
        /// Everything here was taken on trust: the tab id went straight into the character's tab
        /// count, the funds were checked only by the client, and the charge was
        /// <c>LossCredits(client, 100000)</c> - which, before that method's sign meant anything,
        /// *paid* the player. Tabs 2 to 5 handed out 100 K, 1 M, 10 M and 100 M credits, and the
        /// client's own affordability check was the only thing standing in front of it.
        /// </summary>
        public void PurchaseLockboxTab(Client client, PurchaseLockboxTabPacket packet)
        {
            var owned = Math.Max(client.Player.LockboxTabs, LockboxTab.FreeTab);

            // Tabs are bought one at a time, in order, and never twice: the client's own
            // LockboxTabCanBePurchased wants this tab locked and the one below it unlocked, so
            // anything else is a client that has been made to say something it would not say.
            // Without this, tab 5 could be bought for 100 M instead of the 111.1 M the four cost
            // together - or a tab already owned re-bought, or a lower one set to lose the rest.
            if (!LockboxTab.Exists(packet.TabId) || packet.TabId != owned + 1)
            {
                Logger.WriteLog(LogType.Security, $"{client.AccountEntry.FamilyName} asked to buy lockbox tab {packet.TabId} while holding {owned}.");
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmFootlockerPurchaseTabCannotBuy, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            // Checked here as well as in LossCredits so the player is told why, rather than the
            // purchase just not happening.
            var price = LockboxTab.Price(packet.TabId);

            if (client.Player.Credits[CurencyType.Credits] < price)
            {
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInsufficientFunds, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            if (!_currencyManager.LossCredits(client, price))
                return;

            // update Player
            client.Player.LockboxTabs = packet.TabId;
            // update Db
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterLockboxes.UpdatePurashedTabs(client.AccountEntry.Id, packet.TabId);
            // send data to client
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(packet.TabId));
        }

        public void RequestEquipArmor(Client client, RequestEquipArmorPacket packet)
        {
            if (packet.SrcInventory != InventoryType.Personal)
            {
                Logger.WriteLog(LogType.Debug, $"Unsupported inventory => {packet.SrcInventory}");
                return;
            }

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 50)
            {
                Logger.WriteLog(LogType.Debug, $"SrcSlot out of range => {packet.SrcSlot}");
                return;
            }

            // The list has 22 entries; the old check let 22 through.
            if (packet.DestSlot >= client.Player.Inventory.EquippedInventory.Count)
            {
                Logger.WriteLog(LogType.Debug, $"DestSlot out of range => {packet.DestSlot}");
                return;
            }

            var entityIdEquippedItem = client.Player.Inventory.EquippedInventory[(int)packet.DestSlot]; // the old equipped item (can be none)
            var entityIdInventoryItem = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot]; // the new equipped item (can be none)
            if (HasProtected(entityIdEquippedItem, entityIdInventoryItem))
                return;

            // Nothing coming in and nothing going out: the dequip path below would have looked
            // up the class of an item that is not there.
            if (entityIdInventoryItem == 0 && entityIdEquippedItem == 0)
                return;

            // can we equip the item
            var itemToEquip = EntityManager.Instance.GetItem(entityIdInventoryItem);

            if (entityIdInventoryItem != 0 && itemToEquip == null)
            {
                Logger.WriteLog(LogType.Error, $"RequestEquipArmor: slot {packet.SrcSlot} holds entity {entityIdInventoryItem} but no item is registered for it.");
                return;
            }

            if (itemToEquip != null)
            {
                // The item goes in the slot its class says it is for, and nowhere else. The slot
                // index in the equipped list is the equipment slot id. Nothing checked this, so
                // any category-0 item could be put in any of the 22 slots - the same chest piece
                // in all of them - and UpdateStatsValues sums ArmorValue over every slot.
                var equipable = EntityClassManager.Instance.GetEquipableClassInfo(itemToEquip);

                if (equipable == null || (uint) equipable.EquipmentSlotId != packet.DestSlot)
                {
                    Logger.WriteLog(LogType.Security,
                        $"AccountId = {client.AccountEntry.Id} tried to equip {itemToEquip.ItemTemplate.Class} in slot {packet.DestSlot}"
                        + (equipable == null ? ", which is not equipment." : $", which is for {equipable.EquipmentSlotId}."));
                    return;
                }

                if (!ValidateItemEquip(client, itemToEquip))
                    return;
            }

            // swap items on the client and server
            if (client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);

            if (client.Player.Inventory.EquippedInventory[(int)packet.DestSlot] != 0)
                RemoveItemBySlot(client, InventoryType.EquipedInventory, packet.DestSlot);

            if (entityIdEquippedItem != 0)
                AddItemBySlot(client, InventoryType.Personal, entityIdEquippedItem, packet.SrcSlot, true);

            if (entityIdInventoryItem != 0)
                AddItemBySlot(client, InventoryType.EquipedInventory, entityIdInventoryItem, packet.DestSlot, true);

            // update appearance
            if (itemToEquip == null)
            {
                // The slot being cleared is the one the item was taken out of: an equipped index
                // is the equipment slot id, which is what the check above holds incoming items
                // to. Reading it back off the outgoing item threw for anything that is not
                // equipment, and an active weapon that is not one can sit in index 13.
                ManifestationManager.Instance.RemoveAppearanceItem(client, (EquipmentData)packet.DestSlot);
            }
            else
                ManifestationManager.Instance.SetAppearanceItem(client, itemToEquip);

            ManifestationManager.Instance.UpdateAppearance(client);
            ManifestationManager.Instance.UpdateStatsValues(client, false);
            ManifestationManager.Instance.NotifyEquipmentUpdate(client);

            // Send Data to client
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));

            if (itemToEquip != null &&
                client.Player.Inventory.EquippedInventory[(int)packet.DestSlot] == entityIdInventoryItem)
                RecordEquippedItemProgress(client, itemToEquip);
        }

        public void RequestEquipWeapon(Client client, RequestEquipWeaponPacket packet)
        {
            var srcSlot = packet.SrcSlot;
            var invType = packet.InventoryType;
            var destSlot = packet.DestSlot;

            if (invType != InventoryType.Personal)
            {
                Logger.WriteLog(LogType.Debug, $"Unsupported inventory => {invType}");
                return;
            }

            // Both slots arrive as uint, so only the upper bound is worth testing. Weapons come
            // out of the equipment half of the personal inventory, the same fifty slots the
            // armor handler reads from.
            if (srcSlot >= 50)
                return;

            // The drawer has five slots; the old check spelled that as a literal.
            if (destSlot >= client.Player.Inventory.WeaponDrawer.Count)
                return;

            // equip item
            var entityIdEquippedItem = client.Player.Inventory.WeaponDrawer[(int)destSlot]; // the old equipped item (can be none)
            var entityIdInventoryItem = client.Player.Inventory.PersonalInventory[(int)srcSlot]; // the new equipped item (can be none)
            if (HasProtected(entityIdEquippedItem, entityIdInventoryItem))
                return;

            // Nothing coming in and nothing going out: there is no swap to make, and the dequip
            // path below would clear an appearance slot that nothing had filled.
            if (entityIdInventoryItem == 0 && entityIdEquippedItem == 0)
                return;

            // can we equip the item
            var itemToEquip = EntityManager.Instance.GetItem(entityIdInventoryItem);

            if (entityIdInventoryItem != 0 && itemToEquip == null)
            {
                Logger.WriteLog(LogType.Error, $"RequestEquipWeapon: slot {srcSlot} holds entity {entityIdInventoryItem} but no item is registered for it.");
                return;
            }

            if (itemToEquip != null)
            {
                // Only a weapon goes in the weapon drawer. This is the rule the client draws the
                // drawer by - weapondrawerwindow._IsEntityWeapon accepts a drop only where
                // equipableClassEquipmentSlot[classId] is WEAPON - and nothing on this side
                // checked it: ValidateItemEquip asks about level, attributes, race and skill,
                // none of which a consumable or an ammo stack carries, so any of them passed and
                // was written into the drawer and its character_inventory row. Arming that slot
                // then dereferenced an EquipableClassInfo the class does not have.
                var equipable = EntityClassManager.Instance.GetEquipableClassInfo(itemToEquip);

                if (equipable == null || equipable.EquipmentSlotId != EquipmentData.Weapon)
                {
                    Logger.WriteLog(LogType.Security,
                        $"AccountId = {client.AccountEntry.Id} tried to put {itemToEquip.ItemTemplate.Class} in weapon drawer slot {destSlot}"
                        + (equipable == null ? ", which is not equipment." : $", which is for {equipable.EquipmentSlotId}."));
                    return;
                }

                if (!ValidateItemEquip(client, itemToEquip))
                    return;
            }

            // swap items on the client and server
            if (client.Player.Inventory.PersonalInventory[(int)srcSlot] != 0)
                RemoveItemBySlot(client, InventoryType.Personal, srcSlot);
            if (client.Player.Inventory.WeaponDrawer[(int)destSlot] != 0)
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, destSlot);
            if (entityIdEquippedItem != 0)
                AddItemBySlot(client, InventoryType.Personal, entityIdEquippedItem, srcSlot, true);
            if (entityIdInventoryItem != 0)
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, entityIdInventoryItem, destSlot, true);

            if (destSlot == client.Player.ActiveWeapon)
                if (itemToEquip == null)
                {
                    // The slot being cleared is the weapon slot - this is the weapon drawer, so
                    // it can only ever have been that one. It used to be read back off the item
                    // coming out, which threw for an item that is not equipment: a drawer that
                    // had been loaded with junk before the check above existed could not be
                    // emptied from the active slot without disconnecting the player.
                    RemoveItemBySlot(client, InventoryType.EquipedInventory, 13);
                    ManifestationManager.Instance.RemoveAppearanceItem(client, EquipmentData.Weapon);

                    // we dont have weapon, set weaponReady to false
                    if (client.Player.WeaponReady)
                        ManifestationManager.Instance.WeaponReady(client, false);
                }
                else
                    ManifestationManager.Instance.SetAppearanceItem(client, itemToEquip);

            // Tell client that he have new weapon
            ManifestationManager.Instance.NotifyEquipmentUpdate(client);

            ManifestationManager.Instance.UpdateAppearance(client);

            if (itemToEquip != null &&
                client.Player.Inventory.WeaponDrawer[(int)destSlot] == entityIdInventoryItem)
                RecordEquippedItemProgress(client, itemToEquip);
        }

        public void RequestLockboxTabPermissions(Client client)
        {
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.Player.LockboxTabs));
        }

        public void RequestMoveItemToClanLockbox(Client client, RequestMoveItemToClanLockboxPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDO: RequestMoveItemToClanLockboxPacket");
        }

        public void RequestMoveItemToHomeInventory(Client client, RequestMoveItemToHomeInventoryPacket packet)
        {
            // remove item
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= LockboxTab.TotalSlots)
                return;

            if (!HomeSlotIsUnlocked(client, packet.DestSlot))
                return;

            var entityId = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot];

            if (entityId == 0 || HasProtected(entityId, client.Player.Inventory.HomeInventory[(int)packet.DestSlot]))
                return;

            RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.HomeInventory[(int)packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.Personal, client.Player.Inventory.HomeInventory[(int)packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.HomeInventory, entityId, packet.DestSlot, true);
        }

        public void ClanLockbox_DepositItemInSlot(Client client, ClanLockbox_DepositItemInSlotPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 500)
                return;

            if (!ClanSlotIsUnlocked(client, (uint)packet.DestSlot))
                return;

            var entityId = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot];

            if (entityId == 0 || HasProtected(entityId, client.Player.Inventory.ClanInventory[(int)packet.DestSlot]))
                return;

            // If DestSlot is not empty, move current item to SrcSlot (item swap)
            bool wasSwap = client.Player.Inventory.ClanInventory[(int)packet.DestSlot] != 0;

            // A swap hands whatever is in that slot to the depositor, which is a withdrawal
            // however it is spelled - and the only one that was not held to the withdraw rank.
            // Decided before anything moves: the item used to leave the pack first.
            if (wasSwap && !CanTakeFromClanLockbox(client))
                return;

            var depositedItem = EntityManager.Instance.GetItem(entityId);

            // Looked up before the slot is emptied rather than after: an entity the slot names
            // that the EntityManager does not have left the pack and arrived nowhere.
            if (depositedItem == null)
                return;

            RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // Rows are found by item id throughout: the character id they were written with
            // is not always this character's, and a delete by slot that misses leaves a row
            // pointing at an item that has moved on.
            if (wasSwap)
            {
                unitOfWork.CharacterInventories.DeleteInvItemByItemId(depositedItem.Id);
                AddItemBySlot(client, InventoryType.Personal, client.Player.Inventory.ClanInventory[(int)packet.DestSlot], packet.SrcSlot, true, true);

                RemoveItemBySlotForClan(client.Player.ClanId, packet.DestSlot, 0);
                unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, packet.DestSlot);
            }

            AddItemBySlot(client, InventoryType.ClanInventory, entityId, packet.DestSlot, true, true);

            if (!wasSwap)
                unitOfWork.CharacterInventories.DeleteInvItemByItemId(depositedItem.Id);

            depositedItem.OwnerSlotId = packet.DestSlot;
            RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, packet.DestSlot, ref client.Player.Inventory.ClanInventory, true);
        }

        public void ClanLockbox_DepositItemInTab(Client client, ClanLockbox_DepositItemInTabPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;

            var unlocked = UnlockedClanSlots(client);

            // Where in the lockbox this deposit may land. The second field of this packet is the
            // tab the window is showing, not a slot - it was read as one, so the tab check here
            // was asking whether slot 1 to 5 was unlocked and every tab id passed it. Placement
            // then went to the first free slot of all five hundred, so a clan that filled its
            // free tab went on depositing into the four it had never bought, and an item dropped
            // on tab 3 landed in tab 1 whenever tab 1 had room.
            var firstSlot = 0u;
            var lastSlot = unlocked;

            if (!packet.NoTabNamed)
            {
                var tabId = (uint)packet.DestTab;

                if (!ClanLockboxTab.Exists(tabId))
                    return;

                (firstSlot, lastSlot) = ClanLockboxTab.SlotRange(tabId);

                if (lastSlot > unlocked)
                {
                    Logger.WriteLog(LogType.Security,
                        $"{client.AccountEntry.FamilyName} tried to deposit into clan lockbox tab {tabId} with {unlocked} slots unlocked.");
                    return;
                }
            }

            var entityId = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(entityId);

            if (IsProtected(tempItem))
                return;
            RemoveItemBySlot(client, InventoryType.Personal, (uint)packet.SrcSlot);

            // AddItemToClanInventory saves the stack sizes it changes, and deletes every row
            // of an item it merges away; updating tempItem here afterwards was redundant, and
            // after a full merge it was an update of a deleted row.
            Item item = AddItemToClanInventory(client, tempItem, firstSlot, lastSlot, unlocked);
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (item == null)
            {
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            // The personal row is found by item id: the character id it was written with is
            // not always this character's.
            unitOfWork.CharacterInventories.DeleteInvItemByItemId(tempItem.Id);

            if (EntityManager.Instance.GetItem(entityId) == null)
                return;

            RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, item.OwnerSlotId, ref client.Player.Inventory.ClanInventory, true);

            RecordClanLockboxLog(client, ClanLockboxLogEntry.ForItem(client.Player.ClanId, InventoryTransactionType.Deposit,
                client.Player.Id, client.Player.Name, client.Player.FamilyName, item.ItemTemplate.ItemTemplateId, item.StackSize));
        }

        public void ClanLockbox_MoveItem(Client client, ClanLockbox_MoveItemPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.SrcSlot == packet.DestSlot)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 500)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 500)
                return;

            if (!ClanSlotIsUnlocked(client, (uint)packet.DestSlot))
                return;

            var entityId = client.Player.Inventory.ClanInventory[(int)packet.SrcSlot];

            if (entityId == 0 || HasProtected(entityId, client.Player.Inventory.ClanInventory[(int)packet.DestSlot]))
                return;

            // If DestSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.ClanInventory[(int)packet.DestSlot] != 0)
            {
                // Todo swap items
                return;
            }
            RemoveItemBySlot(client, InventoryType.ClanInventory, packet.SrcSlot); // Put this above swap if check once swap is implemented

            EntityManager.Instance.GetItem(entityId).OwnerSlotId = packet.DestSlot;
            AddItemBySlot(client, InventoryType.ClanInventory, entityId, packet.DestSlot, true, false);

            RemoveItemBySlotForClan(client.Player.ClanId, packet.SrcSlot, client.Player.Id);
            RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, packet.DestSlot, ref client.Player.Inventory.ClanInventory, true);
        }

        public void ClanLockbox_WithdrawItem(Client client, ClanLockbox_WithdrawItemPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            // Only the leader and the rank below them can withdraw items from the clan lockbox.
            if (!CanTakeFromClanLockbox(client))
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 500)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 250)
                return;

            var entityId = client.Player.Inventory.ClanInventory[(int)packet.SrcSlot];

            if (entityId == 0 || HasProtected(entityId, client.Player.Inventory.PersonalInventory[(int)packet.DestSlot]))
                return;

            var tempItem = EntityManager.Instance.GetItem(entityId);
            bool wasSwap = client.Player.Inventory.PersonalInventory[(int)packet.DestSlot] != 0;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (packet.ManagePersonalSlot)
            {
                wasSwap = false;
                Item item = AddItemToInventory(client, tempItem);

                if (item == null)
                {
                    RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, 0, ref client.Player.Inventory.ClanInventory, false);
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                    return;
                }
            }
            else
            {
                if (wasSwap)
                {
                    RemoveItemBySlot(client, InventoryType.ClanInventory, packet.SrcSlot);
                    unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, packet.SrcSlot);
                    AddItemBySlot(client, InventoryType.ClanInventory, client.Player.Inventory.PersonalInventory[(int)packet.DestSlot], packet.SrcSlot, true, true);

                    var newEntityId = client.Player.Inventory.ClanInventory[(int)packet.SrcSlot];
                    RefreshClanLockbox(client.Player.ClanId, newEntityId, client.Player.Id, packet.SrcSlot, ref client.Player.Inventory.ClanInventory, true);

                    var swappedOut = EntityManager.Instance.GetItem(client.Player.Inventory.PersonalInventory[(int)packet.DestSlot]);

                    RemoveItemBySlot(client, InventoryType.Personal, packet.DestSlot);

                    if (swappedOut != null)
                        unitOfWork.CharacterInventories.DeleteInvItemByItemId(swappedOut.Id);
                }
                AddItemBySlot(client, InventoryType.Personal, entityId, packet.DestSlot, true, true);
            }

            if (!wasSwap)
            {
                RemoveItemBySlotForClan(client.Player.ClanId, packet.SrcSlot, 0);
                unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, packet.SrcSlot);

                RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, 0, ref client.Player.Inventory.ClanInventory, false);
            }
        }

        public void ClanLockbox_DestroyItem(Client client, ClanLockbox_DestroyItemPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.EntityId == 0)
                return;

            if (!CanTakeFromClanLockbox(client))
                return;

            // The slot comes from where the item actually sits in this clan's lockbox, not from
            // the item's own OwnerSlotId. Nothing checked that the entity was a lockbox item at
            // all, and OwnerSlotId for something in the player's own pack is a personal slot
            // number - every one of which is a valid clan slot index - so naming a stack of one
            // in pack slot K wiped clan slot K for every member while the pack item sat
            // untouched.
            var slotId = client.Player.Inventory.ClanInventory.IndexOf(packet.EntityId);

            if (slotId < 0)
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} (character {client.Player.Id}) tried to destroy entity {packet.EntityId}, which is not in clan {client.Player.ClanId}'s lockbox.");
                return;
            }

            var tempItem = EntityManager.Instance.GetItem(packet.EntityId);

            if (tempItem == null || IsProtected(tempItem))
                return;

            //TODO: Support deleting portions
            if ((tempItem.StackSize - packet.Quantity) > 0)
                return;

            RemoveItemBySlotForClan(client.Player.ClanId, (uint)slotId, 0);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // By item id, and the item row with it: deleting the lockbox row by slot alone left
            // the item itself behind as a row nothing referenced any more.
            DeleteItemRows(unitOfWork, tempItem);

            RefreshClanLockbox(client.Player.ClanId, packet.EntityId, client.Player.Id, 0, ref client.Player.Inventory.ClanInventory, false);

            RecordClanLockboxLog(client, ClanLockboxLogEntry.ForItem(client.Player.ClanId, InventoryTransactionType.Deletion,
                client.Player.Id, client.Player.Name, client.Player.FamilyName, tempItem.ItemTemplate.ItemTemplateId, packet.Quantity));
        }

        public void RequestTakeItemFromHomeInventory(Client client, RequestTakeItemFromHomeInventoryPacket packet)
        {
            // remove item
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 480)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 250)
                return;

            var entityId = client.Player.Inventory.HomeInventory[(int)packet.SrcSlot];

            if (entityId == 0 || HasProtected(entityId, client.Player.Inventory.PersonalInventory[(int)packet.DestSlot]))
                return;

            RemoveItemBySlot(client, InventoryType.HomeInventory, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.PersonalInventory[(int)packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.HomeInventory, client.Player.Inventory.PersonalInventory[(int)packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.Personal, entityId, packet.DestSlot, true);
        }

        /// <summary>
        /// The Pick Up Items tab's Receive button: takes one item out of the inbox and into the
        /// pack. The inbox is a flat list, not a slotted inventory, so the item is found by its
        /// entity id rather than by a source slot.
        /// </summary>
        public void RequestTakeItemFromInboxInventory(Client client, RequestTakeItemFromInboxInventoryPacket packet)
        {
            if (packet.DestSlot >= 250)
                return;

            if (!client.Player.Inventory.InboxItems.Contains(packet.ItemEntityId))
            {
                Logger.WriteLog(LogType.Error, $"Character {client.Player.Id} asked for inbox item {packet.ItemEntityId}, which is not in their inbox.");
                return;
            }

            var item = EntityManager.Instance.GetItem(packet.ItemEntityId);

            if (item == null || IsProtected(item))
                return;

            // The destination has to be free: the inbox has no slot to swap an item back into,
            // so a swap here would drop whatever was in the pack.
            if (client.Player.Inventory.PersonalInventory[(int)packet.DestSlot] != 0)
            {
                Logger.WriteLog(LogType.Debug, $"Character {client.Player.Id} asked to take inbox item {packet.ItemEntityId} into occupied slot {packet.DestSlot}.");
                return;
            }

            client.Player.Inventory.InboxItems.Remove(packet.ItemEntityId);
            client.CallMethod(SysEntity.ClientInventoryManagerId, new RemoveInboxItemPacket(packet.ItemEntityId));

            item.OwnerId = client.Player.Id;
            item.OwnerSlotId = packet.DestSlot;

            // The row exists already - it was written when the item entered the inbox - so this
            // moves it rather than inserting a second one.
            AddItemBySlot(client, InventoryType.Personal, packet.ItemEntityId, packet.DestSlot, true);
        }

        /// <summary>
        /// Puts an item into a character's inbox, whether or not they are logged in: the row is
        /// written either way, and a client that is online is told about it so the Pick Up Items
        /// tab updates without a relog. Returns false when the inbox is full, in which case
        /// nothing is changed and the caller has to keep the item where it is.
        /// </summary>
        public bool DeliverToInbox(ICharUnitOfWork unitOfWork, uint accountId, uint characterId, Item item)
        {
            var recipient = Server.Clients.Find(c => c?.Player != null && c.Player.Id == characterId
                                                     && c.State == ClientState.Ingame);
            if (!TryMoveToInbox(
                    unitOfWork, accountId, characterId, item.Id, out var slot))
                return false;

            item.OwnerId = characterId;
            item.OwnerSlotId = slot;
            PublishInboxDelivery(recipient, item);
            return true;
        }

        internal bool TryMoveToInbox(
            ICharUnitOfWork unitOfWork,
            uint accountId,
            uint characterId,
            uint itemId,
            out uint slot)
        {
            if (unitOfWork.CharacterMissionItems.GetOwner(itemId) != null)
            {
                slot = 0;
                return false;
            }
            var used = new HashSet<uint>();
            var stored = unitOfWork.CharacterInventories.GetItems(accountId)
                .Where(row => row.CharacterId == characterId
                              && row.InventoryType == (uint)InventoryType.InboxInventory)
                .ToList();

            if (stored.Count >= Inventory.MaxInboxItems)
            {
                slot = 0;
                return false;
            }

            foreach (var row in stored)
                used.Add(row.SlotId);

            slot = 0;

            while (slot < Inventory.MaxInboxItems && used.Contains(slot))
                slot++;

            if (slot >= Inventory.MaxInboxItems)
                return false;

            unitOfWork.CharacterInventories.MoveInvItem(accountId, characterId,
                (uint)InventoryType.InboxInventory, slot, itemId);
            return true;
        }

        internal void PublishInboxDelivery(Client recipient, Item item)
        {
            if (recipient == null ||
                recipient.Player.Inventory.InboxItems.Contains(item.EntityId))
                return;

            recipient.Player.Inventory.InboxItems.Add(item.EntityId);
            ItemManager.Instance.SendItemDataToClient(recipient, item, false);
            recipient.CallMethod(SysEntity.ClientInventoryManagerId,
                new AddInboxItemPacket(item.EntityId));
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

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            //deposit
            if (amount >= 500)
            {
                if (client.Player.Credits[CurencyType.Credits] >= amount)
                {
                    var deposit = client.Player.LockboxCredits + amount;

                    // The lockbox is credited only if the purse was actually debited. The check
                    // above already covers it, but the two halves are written separately here and
                    // a lockbox that gains what nobody lost is credits made out of nothing.
                    if (!_currencyManager.LossCredits(client, amount))
                        return;

                    client.CallMethod(client.Player.EntityId, new LockboxFundsPacket(deposit));

                    client.Player.LockboxCredits = deposit;
                    unitOfWork.CharacterLockboxes.UpdateCredits(client.AccountEntry.Id, deposit);
                }
                else
                    CommunicatorManager.Instance.SystemMessage(client, "Not enof credit's in inventory\nP.S. Go earn some credits :)");
            }
            // withdraw
            else if (amount <= -500)
            {
                if (client.Player.LockboxCredits >= -amount)
                {
                    var withdraw = client.Player.LockboxCredits + amount;

                    if (!_currencyManager.GainCredits(client, -amount))
                        return;

                    client.CallMethod(client.Player.EntityId, new LockboxFundsPacket(withdraw));

                    client.Player.LockboxCredits = withdraw;
                    unitOfWork.CharacterLockboxes.UpdateCredits(client.AccountEntry.Id, withdraw);
                }
                else
                    CommunicatorManager.Instance.SystemMessage(client, "Not enof credit's in Lockbox\nP.S. Dont be greedy :)");
            }
            else
                CommunicatorManager.Instance.SystemMessage(client, "Minimum transfer value is 500 credits");

        }

        /// <summary>
        /// Unlocks the next clan lockbox tab, paid for out of the clan's prestige.
        ///
        /// Not the buyer's: the confirmation the window puts up says "spend %(price)s of your
        /// clan's prestige to unlock tab %(tabId)s for your clan", and the funds it checks before
        /// sending are the prestige figure on the lockbox window, which is the clan's balance.
        ///
        /// Same shape as the personal lockbox tabs after 1.15 - the tab has to exist, has to be
        /// the next one up, and is paid for once - because the client applies the same sequential
        /// rule in ClanLockboxTabCanBePurchased and the same quote-then-check before sending.
        /// Held to the withdraw rank: spending the clan's prestige is a withdrawal in everything
        /// but name, and the window gates withdrawing while leaving this button open to anyone.
        /// </summary>
        public void PurchaseClanLockboxTab(Client client, PurchaseClanLockboxTabPacket packet)
        {
            var clanId = client.Player?.ClanId ?? 0;

            if (clanId == 0)
                return;

            if (!CanTakeFromClanLockbox(client))
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var clan = unitOfWork.Clans.GetClanById(clanId);

            if (clan == null)
                return;

            var owned = Math.Max(clan.PurashedTabs, ClanLockboxTab.FreeTab);
            var tabId = packet.TabId < 0 ? 0u : (uint)packet.TabId;

            if (!ClanLockboxTab.Exists(tabId) || tabId != owned + 1)
            {
                Logger.WriteLog(LogType.Security, $"{client.AccountEntry.FamilyName} asked to buy clan lockbox tab {packet.TabId} while clan {clanId} holds {owned}.");
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmClanlockboxPurchaseTabCannotBuy, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            var price = ClanLockboxTab.Price(tabId);

            if (clan.Prestige < price)
            {
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInsufficientFundsToPurchaseClanLockboxTab, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            var prestigeLeft = clan.Prestige - price;

            // The tab is recorded first and the prestige taken second only if that worked, so a
            // clan can never be charged for a tab it did not get.
            if (!unitOfWork.Clans.UpdatePurashedTabs(clanId, tabId))
            {
                Logger.WriteLog(LogType.Error, $"PurchaseClanLockboxTab: could not record tab {tabId} for clan {clanId}; nothing charged.");
                return;
            }

            unitOfWork.Clans.UpdatePrestige(clanId, prestigeLeft);

            clan.PurashedTabs = tabId;
            clan.Prestige = prestigeLeft;

            if (ClanManager.Instance.Clans.ContainsKey(clanId))
                ClanManager.Instance.Clans[clanId] = new Lazy<ClanEntry>(() => clan);

            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (uint)SysEntity.ClientInventoryManagerId, new UpdateClanLockboxTabCountPacket(tabId));

            RecordClanLockboxLog(client, ClanLockboxLogEntry.ForCredits(clanId, InventoryTransactionType.TabPurchase,
                client.Player.Id, client.Player.Name, client.Player.FamilyName, (byte)CurencyType.Prestige, price));
        }

        /// <summary>
        /// Whether this client may take something out of their clan's lockbox.
        ///
        /// Withdrawing an item, swapping a deposit onto an occupied slot, destroying something,
        /// buying a tab and drawing credits or prestige out are all one act as far as the clan is
        /// concerned - clan property leaves - so they are held to one rank. Only the withdraw and
        /// tab paths asked; the swap handed the occupied slot's item straight into the
        /// depositor's pack, destroy deleted whatever it was pointed at, and the credit transfer
        /// moved the whole bank, all at rank 0.
        /// </summary>
        private static bool CanTakeFromClanLockbox(Client client)
        {
            ClanMemberEntry member = ClanManager.Instance.GetClanMember(client.Player.ClanId, client.Player.Id);

            if (member != null && member.Rank >= ClanRank.MinRankToWithdrawFromLockbox)
                return true;

            Logger.WriteLog(LogType.Security,
                $"{client.Player.FamilyName} (character {client.Player.Id}) tried to take from clan {client.Player.ClanId}'s lockbox at rank {member?.Rank.ToString() ?? "no membership"}.");

            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmClanInsufficientPermissions, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

            return false;
        }

        /// <summary>How many of the five hundred clan lockbox slots the clan has actually bought.</summary>
        private static uint UnlockedClanSlots(Client client)
        {
            var clan = ClanManager.Instance.Clans.GetValueOrDefault(client.Player.ClanId)?.Value;

            return ClanLockboxTab.UnlockedSlots(clan?.PurashedTabs ?? ClanLockboxTab.FreeTab);
        }

        /// <summary>
        /// Whether the clan may put something in that lockbox slot. All 500 were addressable
        /// whatever the clan had unlocked, so the tabs bought nothing; the client hides the
        /// locked ones and never sends a slot inside them.
        /// </summary>
        private static bool ClanSlotIsUnlocked(Client client, uint slot)
        {
            var unlocked = UnlockedClanSlots(client);

            if (slot < unlocked)
                return true;

            Logger.WriteLog(LogType.Security, $"{client.AccountEntry.FamilyName} tried clan lockbox slot {slot} with {unlocked} slots unlocked.");
            return false;
        }

        /// <summary>
        /// Writes one line of the clan's lockbox history and hands it to whoever is online to see
        /// it. Best effort: the transaction it describes has already happened, so a log that
        /// cannot be written is logged here rather than undoing it.
        /// </summary>
        private void RecordClanLockboxLog(Client client, ClanLockboxLogEntry entry)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var stored = unitOfWork.ClanLockboxLogs.Add(entry);

            if (stored == null)
                return;

            ClanManager.Instance.CallMethodForOnlineMembers(entry.ClanId, (uint)SysEntity.ClientClanManagerId,
                ClanLockboxLogsPacket.Update(new List<ClanLockboxLogEntry> { stored }));
        }

        /// <summary>
        /// The clan's lockbox history and tab count, sent when a member enters the world. Nothing
        /// in the client asks for either - the window draws whatever it was last told - so this is
        /// the only chance to fill it.
        /// </summary>
        public void SendClanLockboxState(Client client)
        {
            var clanId = client.Player?.ClanId ?? 0;

            if (clanId == 0)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var clan = unitOfWork.Clans.GetClanById(clanId);

            client.CallMethod(SysEntity.ClientInventoryManagerId,
                new UpdateClanLockboxTabCountPacket(Math.Max(clan?.PurashedTabs ?? 0, ClanLockboxTab.FreeTab)));

            // The client keeps CLAN_LOCKBOX_LOGS_DISPLAY_LIMIT of them and throws the rest away
            // as they arrive, so sending more than that is work nobody sees.
            client.CallMethod(SysEntity.ClientClanManagerId,
                ClanLockboxLogsPacket.Load(unitOfWork.ClanLockboxLogs.Get(clanId, ClanLockboxLogDisplayLimit)));
        }

        /// <summary>CLAN_LOCKBOX_LOGS_DISPLAY_LIMIT.</summary>
        private const int ClanLockboxLogDisplayLimit = 100;

        public void ClanCreditTransfer(Client client, long amount, uint creditType)
        {
            // amount > 0 deposits into the lockbox, amount < 0 withdraws from it.
            // creditType 1 is credits, 2 is prestige.
            if (client.Player == null || client.Player.ClanId == 0)
                return;

            if (creditType != 1 && creditType != 2)
                return;

            // Membership, from the clan's own roster rather than from the id the manifestation
            // is carrying around.
            if (ClanManager.Instance.GetClanMember(client.Player.ClanId, client.Player.Id) == null)
                return;

            // A withdrawal is a withdrawal whether it is an item or the money. Items were held
            // to the withdraw rank and the bank was not, so any member could empty it.
            if (amount < 0 && !CanTakeFromClanLockbox(client))
                return;

            if (amount > -500 && amount < 500)
            {
                CommunicatorManager.Instance.SystemMessage(client, "Minimum transfer value is 500 credits");
                return;
            }

            // The character side is an int; anything past that cannot be a real request.
            if (amount < int.MinValue || amount > int.MaxValue)
                return;

            var currency = creditType == 1 ? CurencyType.Credits : CurencyType.Prestige;
            var characterUpdate = creditType == 1 ? CharacterUpdate.Credits : CharacterUpdate.Prestige;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var clanInfo = unitOfWork.Clans.GetClanById(client.Player.ClanId);

            if (clanInfo == null)
                return;

            long lockboxBalance = creditType == 1 ? clanInfo.Credits : clanInfo.Prestige;
            long lockboxAfter = lockboxBalance + amount;
            long playerAfter = client.Player.Credits[currency] - amount;

            if (playerAfter < 0)
            {
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInsufficientDepositFunds, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            if (lockboxAfter < 0)
            {
                CommunicatorManager.Instance.SystemMessage(client, "Not enough credit's");
                return;
            }

            if (lockboxAfter > uint.MaxValue || playerAfter > int.MaxValue)
                return;

            // The character is charged first and the lockbox credited second. This used to be
            // the other way round, and the character step threw before it ran: the long
            // amount was boxed and unboxed as an int, an InvalidCastException, so the clan
            // kept every deposit, the depositor kept the money, and the client was
            // disconnected. If the second step fails now the player is short, not the clan.
            if (!_characterManager.UpdateCharacter(
                    client, characterUpdate, (int)(-amount)))
                return;

            if (creditType == 1)
                unitOfWork.Clans.UpdateCredits(client.Player.ClanId, (uint)lockboxAfter);
            else
                unitOfWork.Clans.UpdatePrestige(client.Player.ClanId, (uint)lockboxAfter);

            var lockboxCredits = creditType == 1 ? (uint)lockboxAfter : clanInfo.Credits;
            var lockboxPrestige = creditType == 2 ? (uint)lockboxAfter : clanInfo.Prestige;

            foreach (var dynamicObj in EntityManager.Instance.DynamicObjects)
            {
                var dynamicObject = dynamicObj.Value;

                if (dynamicObject.EntityClassId == EntityClasses.UsableClanLockboxV01)
                    ClanManager.Instance.CallMethodForOnlineMembers(client.Player.ClanId, dynamicObject.EntityId, new UpdateClanLockboxCreditsPacket(lockboxCredits, lockboxPrestige));
            }

            // Logged by which way the money went, with the amount as a positive number: the
            // client prints the transaction type as its own word and the amount beside it, so a
            // withdrawal of -5,000 would read as "withdrew -5000 credits".
            RecordClanLockboxLog(client, ClanLockboxLogEntry.ForCredits(client.Player.ClanId,
                amount > 0 ? InventoryTransactionType.Deposit : InventoryTransactionType.Withdrawal,
                client.Player.Id, client.Player.Name, client.Player.FamilyName, (byte)currency, Math.Abs(amount)));
        }

        public void WeaponDrawerInventory_MoveItem(Client client, WeaponDrawerInventory_MoveItemPacket packet)
        {
            // Nothing checked either slot; the drawer has five.
            if (packet.SrcSlot >= 5 || packet.DestSlot >= 5 || packet.SrcSlot == packet.DestSlot)
                return;

            var srcEntityId = client.Player.Inventory.WeaponDrawer[(int)packet.SrcSlot];

            if (srcEntityId == 0)
                return;

            var destEntityId = client.Player.Inventory.WeaponDrawer[(int)packet.DestSlot];
            if (HasProtected(srcEntityId, destEntityId))
                return;
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

        #endregion

        #region Helper Functions

        public void UpdateItemSlot(Client client, ulong entityId)
        {
            Item tempItem = EntityManager.Instance.GetItem(entityId);
            ItemManager.Instance.SendItemDataToClient(client, tempItem, false);
        }

        public void AddItemBySlot(Client client, InventoryType inventoryType, ulong entityId, uint slotId, bool updateDB, bool actuallyAdd = false)
        {
            var tempItem = EntityManager.Instance.GetItem(entityId);

            if (tempItem == null)
                return;
            var targetSlots = Slots(client.Player, inventoryType);
            if (targetSlots != null && (slotId >= targetSlots.Count ||
                targetSlots[(int)slotId] != entityId && HasProtected(targetSlots[(int)slotId])))
                return;
            if (IsProtected(tempItem) && (updateDB || inventoryType != InventoryType.Personal ||
                tempItem.OwnerId != client.Player.Id || tempItem.OwnerSlotId != slotId))
                return;

            // set entityId in slot
            switch (inventoryType)
            {
                case InventoryType.Personal:
                    client.Player.Inventory.PersonalInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.HomeInventory:
                    client.Player.Inventory.HomeInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    client.Player.Inventory.EquippedInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    client.Player.Inventory.WeaponDrawer[(int)slotId] = tempItem.EntityId; // update slot

                    // EquippedInventory[13] is the weapon in hand, which is the drawer slot
                    // ActiveWeapon names - not whichever drawer slot was written last. This
                    // used to set it unconditionally, so equipping into a non-active slot,
                    // swapping drawer slots, or just loading the drawer in row order made
                    // CurrentWeapon() answer a weapon the player was not holding, and fire,
                    // reload and ammo all acted on that one.
                    if (slotId == client.Player.ActiveWeapon)
                        client.Player.Inventory.EquippedInventory[13] = tempItem.EntityId;
                    break;
                case InventoryType.ClanInventory:
                    client.Player.Inventory.ClanInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                default:
                    Console.WriteLine("Unsuported inventory type");
                    break;
            }
            // send inventoryAddItem
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(inventoryType, tempItem.EntityId, slotId));
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // OwnerId is the character id the inventory row is written with, and it follows
            // the destination: the home lockbox is the account's (0), the clan lockbox is the
            // clan's, and the three character inventories are this character's. It used to
            // be set only for Home, so an item taken out of the home lockbox kept OwnerId 0
            // and its Personal row was written with character_id 0 - which the loader only
            // reads for Home, so the item was gone at the next login - and an item pulled from
            // the clan lockbox kept whatever the depositor had left in it.
            switch (inventoryType)
            {
                case InventoryType.HomeInventory:
                case InventoryType.ClanInventory:
                    tempItem.OwnerId = 0;
                    break;
                case InventoryType.Personal:
                case InventoryType.EquipedInventory:
                case InventoryType.WeaponDrawerInventory:
                    tempItem.OwnerId = client.Player.Id;
                    break;
            }

            // update item in database
            if (updateDB)
            {
                if (inventoryType == InventoryType.ClanInventory)
                {
                    if (actuallyAdd)
                    {
                        unitOfWork.ClanInventories.AddInvItem(client.Player.ClanId, slotId, tempItem.Id);
                    }
                    else
                    {
                        unitOfWork.ClanInventories.MoveInvItem(client.Player.ClanId, slotId, tempItem.Id);
                    }
                }
                else
                {
                    if (actuallyAdd)
                    {
                        unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, tempItem.OwnerId, (uint)inventoryType, slotId, tempItem.Id);
                    }
                    else
                    {
                        unitOfWork.CharacterInventories.MoveInvItem(client.AccountEntry.Id, tempItem.OwnerId, (uint)inventoryType, slotId, tempItem.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a merged-away item from the database: its inventory row first, then the item
        /// row. The item row alone used to be deleted here, and the caller was expected to
        /// delete the inventory row afterwards - by slot, with a character id that is not
        /// written consistently, and after a call that threw. Nothing in the schema stops a
        /// character_inventory or clan_inventory row from pointing at an item that no longer
        /// exists, and a row like that made the next login (or, for a clan, the next server
        /// start) dereference null. An item has one row in one of the two tables; both
        /// deletes are no-ops when there is nothing to delete.
        /// </summary>
        private static void DeleteItemRows(ICharUnitOfWork unitOfWork, Item item)
        {
            if (MissionItemProtection.IsProtected(item, unitOfWork))
                throw new GameplayRejectionException("Assignment items require authored cleanup.");
            if (item.Id == 0)
                return;

            // One transaction: either all three rows go or none does, so a failure between
            // them cannot leave the item row gone and an inventory row pointing at it.
            using var transaction = unitOfWork.BeginTransaction();

            unitOfWork.CharacterInventories.DeleteInvItemByItemId(item.Id);
            unitOfWork.ClanInventories.DeleteInvItemByItemId(item.Id);
            unitOfWork.Items.DeleteItem(item.Id);

            transaction.Commit();
        }

        /// <summary>
        /// Puts an item in the slot the player asked for, falling back to the ordinary
        /// first-that-fits placement when that slot is not usable.
        ///
        /// The slot is a personal-inventory index the client worked out itself (inventory.py adds
        /// the category's start to the slot within it), so it is checked here rather than trusted:
        /// out of range, occupied, or in another category's block all fall back instead of being
        /// refused, because the player asked to take the item and where it lands is the lesser
        /// question.
        /// </summary>
        public Item AddItemToInventory(Client client, Item item, uint destSlot)
        {
            return AddItemToInventory(client, item, destSlot, false);
        }

        /// <summary>
        /// Places a newly granted item and records mission acquisition. Storage and equipment
        /// transfers must use <see cref="AddItemToInventory(Client, Item, uint)"/>.
        /// </summary>
        public Item GrantItemToInventory(Client client, Item item, uint destSlot)
        {
            return AddItemToInventory(client, item, destSlot, true);
        }

        private Item AddItemToInventory(
            Client client,
            Item item,
            uint destSlot,
            bool recordAcquisition)
        {
            if (item == null || IsProtected(item))
                return null;

            var inventory = client.Player.Inventory.PersonalInventory;
            var categoryOffset = ((int)item.ItemTemplate.InventoryCategory - 1) * 50;

            var usable = categoryOffset >= 0
                         && destSlot < inventory.Count
                         && destSlot >= categoryOffset
                         && destSlot < categoryOffset + 50
                         && inventory[(int)destSlot] == 0;

            if (!usable)
                return AddItemToInventory(client, item, recordAcquisition);

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);
            var acquired = item.StackSize;

            item.OwnerId = client.Player.Id;
            item.OwnerSlotId = destSlot;
            item.CurrentHitPoints = itemClassInfo.MaxHitPoints;

            ItemManager.Instance.SendItemDataToClient(client, item, false);
            AddItemBySlot(client, InventoryType.Personal, item.EntityId, destSlot, true, true);
            if (recordAcquisition)
                RecordItemProgress(
                    client,
                    item,
                    acquired,
                    MissionProgressEventKind.ItemAcquired);

            return item;
        }

        public Item AddItemToInventory(Client client, Item item)
        {
            return AddItemToInventory(client, item, false);
        }

        /// <summary>
        /// Places a newly granted item and records mission acquisition. Storage and equipment
        /// transfers must use <see cref="AddItemToInventory(Client, Item)"/>.
        /// </summary>
        public Item GrantItemToInventory(Client client, Item item)
        {
            return AddItemToInventory(client, item, true);
        }

        private Item AddItemToInventory(
            Client client,
            Item item,
            bool recordAcquisition)
        {
            if (item == null || IsProtected(item))
                return null;

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);
            var initialQuantity = item.StackSize;

            // get item category offset
            var itemCategoryOffset = (int)item.ItemTemplate.InventoryCategory - 1;

            if (itemCategoryOffset < 0 || itemCategoryOffset >= 5)
            {
                Logger.WriteLog(LogType.Error, $"AddItemToInventory: ItemTemplateId = {item.ItemTemplate.ItemTemplateId} inventory category = {item.ItemTemplate.InventoryCategory} is invalid");
                return null;
            }

            itemCategoryOffset *= 50;
            var stackSizeChanged = false;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            // see if we can merge the item into an already existing item
            for (var i = 0; i < 50; i++)
                if (client.Player.Inventory.PersonalInventory[itemCategoryOffset + i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(client.Player.Inventory.PersonalInventory[itemCategoryOffset + i]);

                    // same item template?
                    if (slotItem.ItemTemplate.ItemTemplateId != item.ItemTemplate.ItemTemplateId ||
                        MissionItemProtection.IsProtected(slotItem, unitOfWork))
                        continue;

                    // calculate how many items we can add to the stack
                    var stackAdd = itemClassInfo.StackSize - slotItem.StackSize;
                    if (stackAdd == 0)
                        continue;

                    // add item to existing stack
                    var stackMove = Math.Min(stackAdd, item.StackSize);
                    slotItem.StackSize += stackMove;
                    unitOfWork.Items.UpdateItemStackSize(slotItem);

                    // remove stack's from source item
                    item.StackSize -= stackMove;
                    stackSizeChanged = true;

                    // notify client of changed stack count
                    client.CallMethod(slotItem.EntityId, new SetStackCountPacket(slotItem.StackSize));

                    if (item.StackSize == 0)
                    {
                        // destroy the item
                        EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                        DeleteItemRows(unitOfWork, item);
                        if (recordAcquisition)
                            RecordItemProgress(
                                client,
                                slotItem,
                                initialQuantity,
                                MissionProgressEventKind.ItemAcquired);
                        // return the 'new' item instead
                        return slotItem;
                    }

                }

            // item have new stackSize?
            if (stackSizeChanged)
            {
                client.CallMethod(item.EntityId, new SetStackCountPacket(item.StackSize));

                // The rows of the stacks it was merged into were updated as it went; the
                // remainder's own row still says the whole amount. Left like that, a purchase
                // that half-merged and then took a free slot came back at its full size on the
                // next login - the merged part counted twice.
                if (item.Id != 0)
                    unitOfWork.Items.UpdateItemStackSize(item);
            }

            // find free slot
            for (var i = 0; i < 50; i++)
            {
                if (client.Player.Inventory.PersonalInventory[itemCategoryOffset + i] == 0)
                {
                    item.OwnerId = client.Player.Id;
                    item.OwnerSlotId = (uint)(itemCategoryOffset + i);
                    item.CurrentHitPoints = itemClassInfo.MaxHitPoints;
                    // send data to client
                    ItemManager.Instance.SendItemDataToClient(client, item, false);
                    // add item to empty slot
                    AddItemBySlot(client, InventoryType.Personal, item.EntityId, (uint)(itemCategoryOffset + i), true, true);
                    if (recordAcquisition)
                        RecordItemProgress(
                            client,
                            item,
                            initialQuantity,
                            MissionProgressEventKind.ItemAcquired);
                    return item;
                }
            }

            if (recordAcquisition)
                RecordItemProgress(
                    client,
                    item,
                    initialQuantity - item.StackSize,
                    MissionProgressEventKind.ItemAcquired);
            return null;
        }

        /// <summary>
        /// Puts an item in the clan lockbox: merged into a stack of its own kind if there is room
        /// in one, and otherwise in a free slot.
        ///
        /// Both used to walk all five hundred slots, which is what made the paid tabs
        /// unenforceable on this path however carefully the handler checked. The merge may reach
        /// anywhere the clan has bought - a stack is a stack wherever it sits - but a new slot is
        /// taken only from the window the caller names, which is the tab the player dropped the
        /// item on. A full tab returns null, as a full lockbox already did.
        /// </summary>
        public Item AddItemToClanInventory(Client client, Item item, uint firstSlot, uint lastSlot, uint unlockedSlots)
        {
            if (item == null || IsProtected(item))
                return null;

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var stackSizeChanged = false;
            // see if we can merge the item into an already existing item
            for (var i = 0; i < unlockedSlots; i++)
                if (client.Player.Inventory.ClanInventory[i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(client.Player.Inventory.ClanInventory[i]);

                    // same item template?
                    if (slotItem.ItemTemplate.ItemTemplateId != item.ItemTemplate.ItemTemplateId ||
                        MissionItemProtection.IsProtected(slotItem, unitOfWork))
                        continue;

                    // calculate how many items we can add to the stack
                    var stackAdd = itemClassInfo.StackSize - slotItem.StackSize;
                    if (stackAdd == 0)
                        continue;

                    // add item to existing stack
                    var stackMove = Math.Min(stackAdd, item.StackSize);
                    slotItem.StackSize += stackMove;
                    unitOfWork.Items.UpdateItemStackSize(slotItem);

                    // remove stack's from source item
                    item.StackSize -= stackMove;
                    stackSizeChanged = true;

                    // notify client of changed stack count
                    //client.CallMethod(slotItem.EntityId, new SetStackCountPacket(slotItem.Stacksize));
                    ClanManager.Instance.CallMethodForOnlineMembers(client.Player.ClanId, slotItem.EntityId, new SetStackCountPacket(slotItem.StackSize));

                    if (item.StackSize == 0)
                    {
                        // destroy the item
                        EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                        DeleteItemRows(unitOfWork, item);
                        // return the 'new' item instead
                        return slotItem;
                    }

                }

            // item have new stackSize?
            if (stackSizeChanged)
            {
                client.CallMethod(item.EntityId, new SetStackCountPacket(item.StackSize));

                // The rows of the stacks it was merged into were updated as it went; the
                // remainder's own row still says the whole amount. Left like that, a purchase
                // that half-merged and then took a free slot came back at its full size on the
                // next login - the merged part counted twice.
                if (item.Id != 0)
                    unitOfWork.Items.UpdateItemStackSize(item);
            }

            // find free slot
            for (var i = firstSlot; i < lastSlot; i++)
            {
                if (client.Player.Inventory.ClanInventory[(int)i] == 0)
                {
                    // AddItemBySlot sets OwnerId for the destination; SelectedSlot (a pod
                    // number) used to be stored here as if it were a character id.
                    item.OwnerSlotId = (uint)(i);
                    item.CurrentHitPoints = itemClassInfo.MaxHitPoints;
                    // send data to client
                    ItemManager.Instance.SendItemDataToClient(client, item, false);
                    // add item to empty slot
                    AddItemBySlot(client, InventoryType.ClanInventory, item.EntityId, (uint)(i), true, true);
                    return item;
                }
            }

            return null;
        }

        public Item CurrentWeapon(Client client)
        {
            return EntityManager.Instance.GetItem(client.Player.Inventory.EquippedInventory[13]);
        }

        public uint FreeSlotIndex(Manifestation player, InventoryType inventoryType, uint slotIndex)
        {
            var slots = Slots(player, inventoryType);
            if (slots != null && slotIndex < slots.Count && HasProtected(slots[(int)slotIndex]))
                throw new GameplayRejectionException("Assignment items require authored cleanup.");
            switch (inventoryType)
            {
                case InventoryType.Personal:
                    player.Inventory.PersonalInventory[(int)slotIndex] = 0; // update slot
                    break;
                case InventoryType.HomeInventory:
                    player.Inventory.HomeInventory[(int)slotIndex] = 0; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    player.Inventory.EquippedInventory[(int)slotIndex] = 0; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    player.Inventory.WeaponDrawer[(int)slotIndex] = 0;    // update slot

                    if (slotIndex == player.ActiveWeapon)
                        player.Inventory.EquippedInventory[13] = 0;       // nothing in hand
                    break;
                default:
                    Console.WriteLine("RemoveItemBySlot: Invalid inventoryType{0}/slotIndex{1}\n", inventoryType, slotIndex);
                    break;
            }

            return slotIndex;
        }

        public void InitForClient(Client client)
        {
            InitCharacterInventory(client);

            // Auctions that ran out while this character was away, or while the server was down,
            // are returned now - before the load below would otherwise show them as still listed.
            AuctionHouseManager.Instance.ExpireAuctionsFor(client);

            // init LockboxTabPermissions
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.Player.LockboxTabs));
        }

        /// <summary>
        /// Shows the client the inventory the server already holds for it, after a map change
        /// that made the client forget its entities. Nothing is loaded or registered: the
        /// items and their entity ids are the ones in hand. The dropship arrival used to call
        /// InitForClient here, which loaded the inventory from the database a second time and
        /// registered a second Item entity for every row without destroying the first.
        /// </summary>
        public void ResendToClient(Client client)
        {
            var inventory = client.Player.Inventory;

            ResendList(client, InventoryType.Personal, inventory.PersonalInventory, -1);
            ResendList(client, InventoryType.HomeInventory, inventory.HomeInventory, -1);
            // Slot 13 is the weapon in hand, a mirror of a drawer slot, and is not shown as an
            // equipped item; the login path does not send it either.
            ResendList(client, InventoryType.EquipedInventory, inventory.EquippedInventory, 13);
            ResendList(client, InventoryType.WeaponDrawerInventory, inventory.WeaponDrawer, -1);

            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.Player.LockboxTabs));
        }

        private static void ResendList(Client client, InventoryType inventoryType, List<ulong> slots, int skipSlot)
        {
            for (var slot = 0; slot < slots.Count; slot++)
            {
                if (slot == skipSlot || slots[slot] == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(slots[slot]);

                if (item == null)
                {
                    slots[slot] = 0;
                    continue;
                }

                ItemManager.Instance.SendItemDataToClient(client, item, false);
                client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(inventoryType, item.EntityId, (uint)slot));
            }

            // it seems  that InventoryCreatePacket dont need to be called, ToDo; investigate more
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.Personal, client.MapClient.Inventory.PersonalInventory, 250));
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.HomeInventory, client.MapClient.Inventory.HomeInventory, 480));
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.WeaponDrawerInventory, client.MapClient.Inventory.WeaponDrawer, 5));
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.EquipedInventory, client.MapClient.Inventory.EquippedInventory, 22));
        }

        internal void ResendForMap(Client client)
        {
            var inventory = client.Player.Inventory;
            var groups = new[]
            {
                (InventoryType.Personal, inventory.PersonalInventory),
                (InventoryType.HomeInventory, inventory.HomeInventory),
                (InventoryType.WeaponDrawerInventory, inventory.WeaponDrawer),
                (InventoryType.EquipedInventory, inventory.EquippedInventory)
            };
            var sent = new HashSet<ulong>();
            foreach (var (type, slots) in groups)
            {
                for (var slot = 0; slot < slots.Count; slot++)
                {
                    var entityId = slots[slot];
                    if (entityId == 0)
                        continue;
                    var item = EntityManager.Instance.GetItem(entityId) ??
                        throw new InvalidOperationException($"Missing inventory entity {entityId} during map transfer.");
                    if (sent.Add(entityId))
                        ItemManager.Instance.SendItemDataToClient(client, item, false);
                    client.CallMethod(SysEntity.ClientInventoryManagerId,
                        new InventoryAddItemPacket(type, entityId, (uint)slot));
                }
            }
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.Player.LockboxTabs));
        }

        public void SetupLocalClanInventory(Client client)
        {
            if (client.Player.ClanId == 0)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            List<ClanInventoryEntry> getClanInventoryData = unitOfWork.ClanInventories.GetItems(client.Player.ClanId);

            foreach (var item in getClanInventoryData)
            {
                var itemData = unitOfWork.Items.GetItem(item.ItemId);

                if (itemData == null)
                {
                    // A lockbox row whose item is gone. It used to be dereferenced here, which
                    // disconnected every member of the clan at MapLoaded; the row is garbage, so
                    // it is removed and the rest of the lockbox still loads.
                    Logger.WriteLog(LogType.Error, $"Clan {client.Player.ClanId} lockbox slot {item.SlotId} refers to item {item.ItemId}, which does not exist; row removed.");
                    unitOfWork.ClanInventories.DeleteInvItemByItemId(item.ItemId);
                    continue;
                }

                var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                if (itemTemplate == null)
                {
                    Logger.WriteLog(LogType.Error, $"Item {item.ItemId} has unknown template {itemData.ItemTemplateId}; skipped.");
                    continue;
                }

                Item tempItem = null;

                foreach (var entities in EntityManager.Instance.Items)
                {
                    Item existingItem = entities.Value;

                    if (existingItem.Id == item.ItemId)
                    {
                        tempItem = existingItem;
                    }
                }

                // check if item is weapon
                if (tempItem.ItemTemplate.WeaponInfo != null)
                    tempItem.CurrentAmmo = itemData.AmmoCount;

                // fill invenoty slot
                ItemManager.Instance.SendItemDataToClient(client, tempItem, false);

                AddItemBySlot(client, InventoryType.ClanInventory, tempItem.EntityId, tempItem.OwnerSlotId, false);
            }

            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.ClanInventory, client.Player.Inventory.ClanInventory, 500));
        }

        public void InitClanInventory(Client client)
        {
            // Emptied first, as InitCharacterInventory empties the character's lists. This runs on
            // every world entry - the login and each map change after it, on the same manifestation
            // - and appended another 500 slots each time. SetupLocalClanInventory only fills the
            // first 500, and the list goes out whole in InventoryCreate here and in the
            // InventoryReload every lockbox change sends the clan, so each map change added 500
            // entries to both, until they outgrew the 8192-byte send buffer and were dropped.
            client.Player.Inventory.ResetClanInventory();

            SetupLocalClanInventory(client);
        }

        public void InitCharacterInventory(Client client)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var getInventoryData = unitOfWork.CharacterInventories.GetItems(client.AccountEntry.Id);
            var accountCharacterIds = new HashSet<uint>((client.AccountEntry.Characters ?? new List<CharacterEntry>()).Select(c => c.Id));
            var missionItems = unitOfWork.CharacterMissionItems.GetOwned(client.Player.Id).ToDictionary(entry => entry.ItemId);
            foreach (var row in getInventoryData.Where(row => row.CharacterId == client.Player.Id || row.CharacterId == 0))
            {
                var owner = unitOfWork.CharacterMissionItems.GetOwner(row.ItemId);
                if (owner != null)
                    missionItems.TryAdd(owner.ItemId, owner);
            }
            foreach (var owner in missionItems.Values)
            {
                var rows = getInventoryData.Where(row => row.ItemId == owner.ItemId).ToArray();
                var saved = unitOfWork.Items.GetItem(owner.ItemId);
                if (owner.CharacterId != client.Player.Id || rows.Length != 1 ||
                    rows[0].CharacterId != owner.CharacterId || rows[0].InventoryType != (uint)InventoryType.Personal ||
                    rows[0].SlotId >= PersonalCategorySize * PersonalCategoryCount ||
                    getInventoryData.Count(row => row.CharacterId == owner.CharacterId &&
                        row.InventoryType == (uint)InventoryType.Personal && row.SlotId == rows[0].SlotId) != 1 ||
                    saved == null || saved.StackSize != owner.Quantity || owner.Quantity == 0 ||
                    string.IsNullOrWhiteSpace(owner.AssignmentId) || owner.Generation == 0 ||
                    string.IsNullOrWhiteSpace(owner.ItemKey))
                    throw new GameplayRejectionException("Assignment inventory is inconsistent; quarantined without repair or reissuance.");
            }

            // init for server inventory. Cleared first: this runs again on the manifestation
            // after a summon or .teleport, and used to append another block of slots each
            // time, so the lists grew by 757 entries per zone change.
            client.Player.Inventory.EquippedInventory.Clear();
            client.Player.Inventory.HomeInventory.Clear();
            client.Player.Inventory.PersonalInventory.Clear();
            client.Player.Inventory.WeaponDrawer.Clear();
            client.Player.Inventory.AuctionItems.Clear();
            client.Player.Inventory.InboxItems.Clear();

            for (uint i = 0; i < 22; i++)
                client.Player.Inventory.EquippedInventory.Add(0);

            for (uint i = 0; i < LockboxTab.TotalSlots; i++)
                client.Player.Inventory.HomeInventory.Add(0);

            for (uint i = 0; i < 250; i++)
                client.Player.Inventory.PersonalInventory.Add(0);

            for (uint i = 0; i < 5; i++)
                client.Player.Inventory.WeaponDrawer.Add(0);

            foreach (var item in getInventoryData)
            {
                var inventoryType = (InventoryType)item.InventoryType;
                var sharedHome = item.CharacterId == 0 && inventoryType == InventoryType.HomeInventory;
                if (item.CharacterId != client.Player.Id && !sharedHome)
                {
                    if (!accountCharacterIds.Contains(item.CharacterId))
                        Logger.WriteLog(LogType.Error,
                            $"Account {client.AccountEntry.Id} {inventoryType} slot {item.SlotId} item {item.ItemId} "
                            + $"has owner {item.CharacterId} outside the account's character snapshot; ignored without reassignment.");
                    continue;
                }

                var itemData = unitOfWork.Items.GetItem(item.ItemId);

                if (itemData == null)
                {
                    // An inventory row whose item is gone (a stack merged away before the
                    // row was deleted, in older builds). Dereferencing it here disconnected the
                    // character at every login; the row is removed and the rest still loads.
                    Logger.WriteLog(LogType.Error, $"Account {client.AccountEntry.Id} inventory {item.InventoryType} slot {item.SlotId} refers to item {item.ItemId}, which does not exist; row removed.");
                    unitOfWork.CharacterInventories.DeleteInvItemByItemId(item.ItemId);
                    continue;
                }

                var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                if (itemTemplate == null)
                {
                    Logger.WriteLog(LogType.Error, $"Item {item.ItemId} has unknown template {itemData.ItemTemplateId}; skipped.");
                    continue;
                }

                var newItem = new Item
                {
                    OwnerId = item.CharacterId,
                    OwnerSlotId = item.SlotId,
                    ItemTemplate = itemTemplate,
                    ItemTemplateId = itemTemplate.ItemTemplateId,
                    StackSize = itemData.StackSize,
                    CurrentHitPoints = itemData.CurrentHitPoints,
                    Color = itemData.Color,
                    Id = item.ItemId,
                    Crafter = itemData.CrafterName,
                    MissionOwnership = missionItems.TryGetValue(item.ItemId, out var provenance)
                        ? InventoryPlan.ToOwnership(provenance) : null
                };

                // check if item is weapon
                if (newItem.ItemTemplate.WeaponInfo != null)
                    newItem.CurrentAmmo = itemData.AmmoCount;

                // register item
                EntityManager.Instance.RegisterEntity(newItem.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(newItem.EntityId, newItem);

                // fill invenoty slot
                ItemManager.Instance.SendItemDataToClient(client, newItem, false);

                if (item.CharacterId == client.Player.Id)
                {
                    if ((InventoryType)item.InventoryType == InventoryType.Personal)
                        AddItemBySlot(client, InventoryType.Personal, newItem.EntityId, newItem.OwnerSlotId, false);

                    else if ((InventoryType)item.InventoryType == InventoryType.EquipedInventory)
                        AddItemBySlot(client, InventoryType.EquipedInventory, newItem.EntityId, newItem.OwnerSlotId, false);

                    else if ((InventoryType)item.InventoryType == InventoryType.WeaponDrawerInventory)
                    {
                        // AddItemBySlot sets EquippedInventory[13] itself when this is the
                        // active drawer slot.
                        AddItemBySlot(client, InventoryType.WeaponDrawerInventory, newItem.EntityId, newItem.OwnerSlotId, false);
                    }

                    else if ((InventoryType)item.InventoryType == InventoryType.InboxInventory)
                    {
                        // Waiting at an auction house. The client's Pick Up Items tab reads a
                        // list only AddInboxItem and CreateInventory fill, and CreateInventory
                        // iterates its argument expecting bare entity ids while
                        // InventoryCreatePacket writes (index, entityId) pairs - so the items go
                        // over one at a time.
                        client.Player.Inventory.InboxItems.Add(newItem.EntityId);
                        client.CallMethod(SysEntity.ClientInventoryManagerId, new AddInboxItemPacket(newItem.EntityId));
                    }

                    else if ((InventoryType)item.InventoryType == InventoryType.AuctionInventory)
                    {
                        // Listed at an auction house. SendItemDataToClient above already created
                        // the entity, which is all the client needs to render the row when it
                        // asks for auction status; it belongs in no inventory list it can move
                        // items in, so it only goes in the server's own auction list.
                        client.Player.Inventory.AuctionItems.Add(newItem.EntityId);
                    }
                }
                else if (item.CharacterId == 0)
                {
                    if ((InventoryType)item.InventoryType == InventoryType.HomeInventory)
                    {
                        client.Player.Inventory.HomeInventory[(int)item.SlotId] = newItem.EntityId;
                        // make the item appear on the client
                        AddItemBySlot(client, InventoryType.HomeInventory, client.Player.Inventory.HomeInventory[(int)item.SlotId], item.SlotId, false);
                    }
                }

            }

            // character_inventory rows arrive in whatever order the query returns them, and
            // both lists are shown in the order things happened, so put them back in slot order.
            static void SortBySlot(List<ulong> entityIds) => entityIds.Sort((left, right) =>
            {
                var leftItem = EntityManager.Instance.GetItem(left);
                var rightItem = EntityManager.Instance.GetItem(right);

                return (leftItem?.OwnerSlotId ?? 0).CompareTo(rightItem?.OwnerSlotId ?? 0);
            });

            SortBySlot(client.Player.Inventory.AuctionItems);
            SortBySlot(client.Player.Inventory.InboxItems);
        }

        /// <summary>How many items of the entity class the player carries in their personal inventory, all stacks together.</summary>
        public uint CountItemsByClass(Client client, EntityClasses entityClass)
        {
            var total = 0u;

            foreach (var entityId in client.Player.Inventory.PersonalInventory)
            {
                if (entityId == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(entityId);

                if (item?.ItemTemplate != null && item.ItemTemplate.Class == entityClass && !IsProtected(item))
                    total += item.StackSize;
            }

            return total;
        }

        /// <summary>
        /// Takes <paramref name="quantity"/> items of the entity class out of the personal
        /// inventory, smallest stacks first so partial stacks are used up before full ones are
        /// broken. Returns how many it could not take (0 when the player had enough); check with
        /// <see cref="CountItemsByClass"/> first when the whole amount has to be there.
        /// </summary>
        public uint RemoveItemsByClass(Client client, EntityClasses entityClass, uint quantity)
        {
            var stacks = new List<Item>();

            foreach (var entityId in client.Player.Inventory.PersonalInventory)
            {
                if (entityId == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(entityId);

                if (item?.ItemTemplate != null && item.ItemTemplate.Class == entityClass && item.StackSize > 0 && !IsProtected(item))
                    stacks.Add(item);
            }

            foreach (var stack in stacks.OrderBy(s => s.StackSize))
            {
                if (quantity == 0)
                    break;

                var take = Math.Min(quantity, stack.StackSize);
                ReduceStackCount(client, InventoryType.Personal, stack, take);
                quantity -= take;
            }

            return quantity;
        }

        public void ReduceStackCount(Client client, InventoryType inventoryType, Item tempItem, uint stackDecreaseCount)
        {
            if (client.Player == null || tempItem == null || stackDecreaseCount == 0 || IsProtected(tempItem))
                return;

            // Ownership is decided by where the entity actually sits: it has to be in this
            // client's list for the inventory named. It used to compare Item.OwnerId (a
            // character id, or 0 for the home lockbox) with AccountEntry.SelectedSlot (1..16),
            // which almost never matched, so destroying an item did nothing - and WeaponReload,
            // which consumes ammo through here, reloaded for free.
            List<ulong> slots;

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    slots = client.Player.Inventory.PersonalInventory;
                    break;
                case InventoryType.HomeInventory:
                    slots = client.Player.Inventory.HomeInventory;
                    break;
                default:
                    return;
            }

            var slotId = slots.IndexOf(tempItem.EntityId);

            if (slotId < 0)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.Name} tried to reduce item {tempItem.EntityId}, which is not in their {inventoryType}");
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var consumed = Math.Min(stackDecreaseCount, tempItem.StackSize);

            // uint - uint: a count larger than the stack wrapped to ~4 billion instead of
            // emptying it.
            if (stackDecreaseCount >= tempItem.StackSize)
            {
                EntityManager.Instance.DestroyPhysicalEntity(client, tempItem.EntityId, EntityType.Item);
                client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(inventoryType, tempItem.EntityId));
                FreeSlotIndex(client.Player, inventoryType, (uint)slotId);

                // By item id: the row's character id has been written as the character id, the
                // account's selected slot or 0 depending on which path stored it.
                unitOfWork.CharacterInventories.DeleteInvItemByItemId(tempItem.Id);
                // ToDo will we delete items from db, or we will let tham stay, so thay can be retrived
                //ItemsTable.DeleteItem(tempItem.ItemId);
            }
            else
            {
                tempItem.StackSize -= stackDecreaseCount;
                client.CallMethod(tempItem.EntityId, new SetStackCountPacket(tempItem.StackSize));
                unitOfWork.Items.UpdateItemStackSize(tempItem);
            }

            if (inventoryType == InventoryType.Personal)
                RecordItemProgress(
                    client,
                    tempItem,
                    consumed,
                    MissionProgressEventKind.ItemConsumed);
        }

        private void RecordItemProgress(
            Client client,
            Item item,
            uint quantity,
            MissionProgressEventKind kind)
        {
            if (quantity == 0 || item?.ItemTemplate == null)
                return;
            var itemClassId = (uint)item.ItemTemplate.Class;
            var progress = kind == MissionProgressEventKind.ItemAcquired
                ? MissionProgressEvent.ItemAcquired(itemClassId, quantity)
                : MissionProgressEvent.ItemConsumed(itemClassId, quantity);
            (_missionManager ?? MissionApplication.Instance).RecordProgress(
                client,
                progress);
        }

        private void RecordEquippedItemProgress(
            Client client,
            Item item)
        {
            if (item?.ItemTemplate == null)
                return;

            (_missionManager ?? MissionApplication.Instance).RecordProgress(
                client,
                MissionProgressEvent.ItemEquipped(
                    (uint)item.ItemTemplate.Class,
                    item.ItemTemplate.ItemTemplateId));
        }

        public void RemoveItemBySlot(Client client, InventoryType inventoryType, uint slotIndex)
        {
            var slots = Slots(client.Player, inventoryType);
            if (slots != null && slotIndex < slots.Count && HasProtected(slots[(int)slotIndex]))
                return;
            var entityId = 0ul;

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    entityId = client.Player.Inventory.PersonalInventory[(int)slotIndex];
                    client.Player.Inventory.PersonalInventory[(int)slotIndex] = 0;
                    break;
                case InventoryType.HomeInventory:
                    entityId = client.Player.Inventory.HomeInventory[(int)slotIndex];
                    client.Player.Inventory.HomeInventory[(int)slotIndex] = 0;
                    break;
                case InventoryType.EquipedInventory:
                    entityId = client.Player.Inventory.EquippedInventory[(int)slotIndex];
                    client.Player.Inventory.EquippedInventory[(int)slotIndex] = 0;
                    break;
                case InventoryType.WeaponDrawerInventory:
                    entityId = client.Player.Inventory.WeaponDrawer[(int)slotIndex];
                    client.Player.Inventory.WeaponDrawer[(int)slotIndex] = 0;

                    if (slotIndex == client.Player.ActiveWeapon)
                        client.Player.Inventory.EquippedInventory[13] = 0;
                    break;
                case InventoryType.ClanInventory:
                    entityId = client.Player.Inventory.ClanInventory[(int)slotIndex];
                    client.Player.Inventory.ClanInventory[(int)slotIndex] = 0;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"RemoveItemBySlot: Unsuported Inventory type {inventoryType}");
                    return;
            }

            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(inventoryType, entityId));
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

        public bool ValidateItemEquip(Client client, Item itemToEquip)
        {
            if (IsProtected(itemToEquip))
                return false;
            var canEquip = true;
            // min level criteria met?
            if (itemToEquip != null)
            {
                // check requirements
                foreach (var requirement in itemToEquip.ItemTemplate.ItemInfo.Requirements)
                {
                    switch (requirement.Key)
                    {
                        case RequirementsType.ReqXpLevel:
                            if (client.Player.Level < itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevel])
                            {
                                CommunicatorManager.Instance.SystemMessage(client, "Level too low, cannot equip item.");
                                canEquip = false;
                            }

                            break;
                        case RequirementsType.ReqBody:
                            if (client.Player.Attributes[Attributes.Body].Current < itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqBody])
                            {
                                CommunicatorManager.Instance.SystemMessage(client, "Body attribute too low, cannot equip item.");
                                canEquip = false;
                            }

                            break;
                        case RequirementsType.ReqMind:
                            if (client.Player.Attributes[Attributes.Mind].Current < itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqMind])
                            {
                                CommunicatorManager.Instance.SystemMessage(client, "Mind attribute too low, cannot equip item.");
                                canEquip = false;
                            }

                            break;
                        case RequirementsType.ReqSpirit:
                            if (client.Player.Attributes[Attributes.Spirit].Current < itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqSpirit])
                            {
                                CommunicatorManager.Instance.SystemMessage(client, "Spirit attribute too low, cannot equip item.");
                                canEquip = false;
                            }

                            break;

                        case RequirementsType.ReqXpLevelMax:
                            if (client.Player.Level > itemToEquip.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevelMax])
                            {
                                CommunicatorManager.Instance.SystemMessage(client, "Level too high, cannot equip item.");
                                canEquip = false;
                            }

                            break;

                        default:
                            Logger.WriteLog(LogType.Error, $"Unknown RequirementsType {requirement.Key}");
                            break;
                    }
                }

                // check race requirements
                if (itemToEquip.ItemTemplate.ItemInfo.RaceReq != 0 && itemToEquip.ItemTemplate.ItemInfo.RaceReq != (int)client.Player.Race)
                {
                    CommunicatorManager.Instance.SystemMessage(client, "Item is not for your race, cannot equip it.");
                    canEquip = false;
                }

                // check skill requrements if it's still true
                if (canEquip)
                    if (itemToEquip.ItemTemplate.EquipableInfo != null)
                    {
                        if (client.Player.Skills.ContainsKey((SkillId)itemToEquip.ItemTemplate.EquipableInfo.SkillId))
                        {
                            if (client.Player.Skills[(SkillId)itemToEquip.ItemTemplate.EquipableInfo.SkillId].SkillLevel >= itemToEquip.ItemTemplate.EquipableInfo.SkillLevel)
                                canEquip = true;
                            else
                            {
                                CommunicatorManager.Instance.SystemMessage(client, "Skill level to low, cannot equip item.");
                                canEquip = false;
                            }
                        }
                        else
                        {
                            CommunicatorManager.Instance.SystemMessage(client, $"{(SkillId)itemToEquip.ItemTemplate.EquipableInfo.SkillId} not learned, cannot equip item.");
                            canEquip = false;
                        }
                    }
            }

            return canEquip;
        }

        public void RefreshClanLockbox(uint clanId, ulong entityId, uint characterId, uint slotId, ref List<ulong> clanInventory, bool addBySlot)
        {
            if (addBySlot)
                ClanManager.Instance.CallMethodForOnlineMembers(clanId, (client) => AddItemBySlot(client, InventoryType.ClanInventory, entityId, slotId, false), characterId);

            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (client) => UpdateItemSlot(client, entityId), characterId);
            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (uint)SysEntity.ClientInventoryManagerId, new ClanInventoryReload(InventoryType.ClanInventory, clanInventory, 500));
        }

        public void RemoveItemBySlotForClan(uint clanId, uint slotId, uint skipThisCharacter)
        {
            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (client) => RemoveItemBySlot(client, InventoryType.ClanInventory, slotId), skipThisCharacter);
        }

        #endregion
    }
}
