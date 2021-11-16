namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.MapChannel.Client;
    using Packets.Clan.Client;
    using Packets.Inventory.Client;
    using Packets.Social.Client;

    public partial class ClientPacketHandler
    {
        public Client Client { get; }

        public ClientPacketHandler(Client client)
        {
            Client = client;
        }

        [PacketHandler(GameOpcode.AcceptPartyInvitesChanged)]
        private void AcceptPartyInvitesChanged(AcceptPartyInvitesChangedPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo AcceptPartyInvitesChanged");
        }

        [PacketHandler(GameOpcode.AllocateAttributePoints)]
        private void AllocateAttributePoints(AllocateAttributePointsPacket packet)
        {
            ManifestationManager.Instance.AllocateAttributePoints(Client, packet);
        }

        [PacketHandler(GameOpcode.AssignNPCMission)]
        private void AssignNPCMission(AssignNPCMissionPacket packet)
        {
            NpcManager.Instance.AssignNPCMission(Client, packet);
        }

        [PacketHandler(GameOpcode.AutoFireKeepAlive)]
        private void AutoFireKeepAlive(AutoFireKeepAlivePacket packet)
        {
            ManifestationManager.Instance.AutoFireKeepAlive(Client, packet.KeepAliveDelay);
        }

        [PacketHandler(GameOpcode.CancelLogoutRequest)]
        private void CancelLogoutRequest(CancelLogoutRequestPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo CancelLogoutRequest");  // gues nothing to do here
        }

        [PacketHandler(GameOpcode.ChangeShowHelmet)]
        private void ChangeShowHelmet(ChangeShowHelmetPacket packet)
        {
            ManifestationManager.Instance.ChangeShowHelmet(Client, packet);
        }

        [PacketHandler(GameOpcode.ChangeTitle)]
        private void ChangeTitle(ChangeTitlePacket packet)
        {
            ManifestationManager.Instance.ChangeTitle(Client, packet.TitleId);
        }

        [PacketHandler(GameOpcode.CharacterLogout)]
        private void CharacterLogout(CharacterLogoutPacket packet)
        {
            MapChannelManager.Instance.CharacterLogout(Client);
        }
        
        [PacketHandler(GameOpcode.ClearTargetId)]
        private void ClearTargetId(ClearTargetIdPacket packet)
        {
            ManifestationManager.Instance.SetTargetId(Client, 0);
        }

        [PacketHandler(GameOpcode.ClearTrackingTarget)]
        private void ClearTrackingTarget(ClearTrackingTargetPacket packet)
        {
            ManifestationManager.Instance.SetTrackingTarget(Client, 0);
        }

        [PacketHandler(GameOpcode.CompleteNPCMission)]
        private void CompleteNPCMission(CompleteNPCMissionPacket packet)
        {
            NpcManager.Instance.CompleteNPCMission(Client, packet);
        }

        [PacketHandler(GameOpcode.CreateClan)]
        private void CreateClan(CreateClanPacket packet)
        {
            ClanManager.Instance.CreateClan(Client, packet);
        }
        
        [PacketHandler(GameOpcode.GetCustomizationChoices)]
        private void GetCustomizationChoices(GetCustomizationChoicesPacket packet)
        {
            ManifestationManager.Instance.GetCustomizationChoices(Client, packet);
        }

        [PacketHandler(GameOpcode.GetPvPClanMembershipStatus)]
        private void GetPvPClanMembershipStatus(GetPvPClanMembershipStatusPacket packet)
        {
            ClanManager.Instance.GetPvPClanMembershipStatus(Client);    // packet have 0 argumenst, no need to pass it
        }
        
        [PacketHandler(GameOpcode.LevelSkills)]
        private void LevelSkills(LevelSkillsPacket packet)
        {
            ManifestationManager.Instance.LevelSkills(Client, packet);
        }
        
        [PacketHandler(GameOpcode.MapLoaded)]
        private void MapLoaded(MapLoadedPacket packet)
        {
            MapChannelManager.Instance.MapLoaded(Client);
        }
        
        [PacketHandler(GameOpcode.Ping)]
        private void Ping(PingPacket packet)
        {
            MapChannelManager.Instance.Ping(Client, packet.Ping);
        }

        [PacketHandler(GameOpcode.PrivilegedCommand)]
        private void PrivilegedCommand(PrivilegedCommandPacket packet)
        {
            // ToDo
        }
        
        [PacketHandler(GameOpcode.RadialChat)]
        private void RadialChat(RadialChatPacket packet)
        {
            CommunicatorManager.Instance.Recv_RadialChat(Client, packet.TextMsg);
        }

        [PacketHandler(GameOpcode.RequestActionInterrupt)]
        private void RequestActionInterrupt(RequestActionInterruptPacket packet)
        {
            ActorManager.Instance.RequestActionInterrupt(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestArmAbility)]
        private void RequestArmAbility(RequestArmAbilityPacket packet)
        {
            ManifestationManager.Instance.RequestArmAbility(Client, packet.AbilityDrawerSlot);
        }

        [PacketHandler(GameOpcode.RequestArmWeapon)]
        private void RequestArmWeapon(RequestArmWeaponPacket packet)
        {
            ManifestationManager.Instance.RequestArmWeapon(Client, packet.RequestedWeaponDrawerSlot);
        }

        [PacketHandler(GameOpcode.RequestAuctionBuyout)]
        private void RequestAuctionBuyout(RequestAuctionBuyoutPacket packet)
        {
            AuctionHouseManager.Instance.RequestAuctionBuyout(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestAuctionStatus)]
        private void RequestAuctionStatus(RequestAuctionStatusPacket packet)
        {
            AuctionHouseManager.Instance.RequestAuctionStatus(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCancelAuctioneer)]
        private void RequestCancelAuctioneer(RequestCancelAuctioneerPacket packet)
        {
            AuctionHouseManager.Instance.RequestCancelAuctioneer(Client);
        }

        [PacketHandler(GameOpcode.RequestCancelAuction)]
        private void RequestCancelAuction(RequestCancelAuctionPacket packet)
        {
            AuctionHouseManager.Instance.RequestCancelAuction(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCancelVendor)]
        private void RequestCancelVendor(RequestCancelVendorPacket packet)
        {
            NpcManager.Instance.RequestCancelVendor(Client, packet.EntityId);
        }

        [PacketHandler(GameOpcode.RequestCreateAuction)]
        private void RequestCreateAuction(RequestCreateAuctionPacket packet)
        {
            AuctionHouseManager.Instance.RequestCreateAuction(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCustomization)]
        private void RequestCustomization(RequestCustomizationPacket packet)
        {
            ManifestationManager.Instance.RequestCustomization(Client, packet);
        }
        
        [PacketHandler(GameOpcode.RequestGesture)]
        private void RequestGesture(RequestGesturePacket packet)
        {
            // ToDo
        }

        /*[PacketHandler(GameOpcode.RequestGestureWeapon)]
        private void RequestGestureWeapon(RequestGestureWeaponPacket packet)
        {
            // ToDo
        }*/
        
        [PacketHandler(GameOpcode.RequestLogout)]
        private void RequestLogout(RequestLogoutPacket packet)
        {
            MapChannelManager.Instance.RequestLogout(Client);
        }

        [PacketHandler(GameOpcode.RequestMoveItemToClanLockbox)]
        private void RequestMoveItemToClanLockbox(RequestMoveItemToClanLockboxPacket packet)
        {
            InventoryManager.Instance.RequestMoveItemToClanLockbox(Client, packet);
        }
        
        [PacketHandler(GameOpcode.RequestNPCConverse)]
        private void RequestNPCConverse(RequestNPCConversePacket packet)
        {
            NpcManager.Instance.RequestNpcConverse(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestNPCOpenAuctionHouse)]
        private void RequestNPCOpenAuctionHouse(RequestNPCOpenAuctionHousePacket packet)
        {
            NpcManager.Instance.RequestNPCOpenAuctionHouse(Client, packet.EntityId);
        }

        [PacketHandler(GameOpcode.RequestNPCVending)]
        private void RequestNPCVending(RequestNPCVendingPacket packet)
        {
            NpcManager.Instance.RequestNPCVending(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestPerformAbility)]
        private void RequestPerformAbility(RequestPerformAbilityPacket packet)
        {
            ManifestationManager.Instance.RequestPerformAbility(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestQueryAuctions)]
        private void RequestQueryAuctions(RequestQueryAuctionsPacket packet)
        {
            AuctionHouseManager.Instance.RequestQueryAuctions(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestSetAbilitySlot)]
        private void RequestSetAbilitySlot(RequestSetAbilitySlotPacket packet)
        {
            ManifestationManager.Instance.RequestSetAbilitySlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestSwapAbilitySlots)]
        private void RequestSwapAbilitySlots(RequestSwapAbilitySlotsPacket packet)
        {
            ManifestationManager.Instance.RequestSwapAbilitySlots(Client, packet);
        }
        
        [PacketHandler(GameOpcode.RequestToggleRun)]
        private void RequestToggleRun(RequestToggleRunPacket packet)
        {
            ManifestationManager.Instance.RequestToggleRun(Client);
        }

        [PacketHandler(GameOpcode.RequestTooltipForItemTemplateId)]
        private void RequestTooltipForItemTemplateId(RequestTooltipForItemTemplateIdPacket packet)
        {
            InventoryManager.Instance.RequestTooltipForItemTemplateId(Client, packet.ItemTemplateId);
        }

        [PacketHandler(GameOpcode.RequestTooltipForModuleId)]
        private void RequestTooltipForModuleId(RequestTooltipForModuleIdPacket packet)
        {
            InventoryManager.Instance.RequestTooltipForModuleId(Client, packet.ModuleId);
        }

        [PacketHandler(GameOpcode.RequestUseObject)]
        private void RequestUseObject(RequestUseObjectPacket packet)
        {
            DynamicObjectManager.Instance.RequestUseObjectPacket(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestVendorBuyback)]
        private void RequestVendorBuyback(RequestVendorBuybackPacket packet)
        {
            NpcManager.Instance.RequestVendorBuyback(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestVendorPurchase)]
        private void RequestVendorPurchase(RequestVendorPurchasePacket packet)
        {
            NpcManager.Instance.RequestVendorPurchase(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestVendorRepair)]
        private void RequestVendorRepair(RequestVendorRepairPacket packet)
        {
            NpcManager.Instance.RequestVendorRepair(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestVendorSale)]
        private void RequestVendorSale(RequestVendorSalePacket packet)
        {
            NpcManager.Instance.RequestVendorSale(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestVisualCombatMode)]
        private void RequestVisualCombatMode(RequestVisualCombatModePacket packet)
        {
            ActorManager.Instance.RequestVisualCombatMode(Client, packet.CombatMode);
        }

        [PacketHandler(GameOpcode.RequestWeaponAttack)]
        private void RequestWeaponAttack(RequestWeaponAttackPacket packet)
        {
            MissileManager.Instance.RequestWeaponAttack(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestWeaponDraw)]
        private void RequestWeaponDraw(RequestWeaponDrawPacket packet)
        {
            ManifestationManager.Instance.RequestWeaponDraw(Client);
        }

        [PacketHandler(GameOpcode.RequestWeaponReload)]
        private void RequestWeaponReload(RequestWeaponReloadPacket packet)
        {
            ManifestationManager.Instance.RequestWeaponReload(Client, false);
        }

        [PacketHandler(GameOpcode.RequestWeaponStow)]
        private void RequestWeaponStow(RequestWeaponStowPacket packet)
        {
            ManifestationManager.Instance.RequestWeaponStow(Client);
        }

        [PacketHandler(GameOpcode.SaveCharacterOptions)]
        private void SaveCharacterOptions(SaveCharacterOptionsPacket packet)
        {
            ManifestationManager.Instance.SaveCharacterOptions(Client, packet);
        }

        [PacketHandler(GameOpcode.SaveUserOptions)]
        private void SaveUserOptions(SaveUserOptionsPacket packet)
        {
            ManifestationManager.Instance.SaveUserOptions(Client, packet);
        }

        [PacketHandler(GameOpcode.SelectWaypoint)]
        private void SelectWaypoint(SelectWaypointPacket packet)
        {
            DynamicObjectManager.Instance.SelectWaypoint(Client, packet);
        }

        [PacketHandler(GameOpcode.SetAutoLootThreshold)]
        private void SetAutoLootThreshold(SetAutoLootThresholdPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo SetAutoLootThreshold");
        }

        [PacketHandler(GameOpcode.SetDesiredCrouchState)]
        private void SetDesiredCrouchState(SetDesiredCrouchStatePacket packet)
        {
            ActorManager.Instance.SetDesiredCrouchState(Client, packet.DesiredCrouchState);
        }

        [PacketHandler(GameOpcode.SetTargetId)]
        private void SetTargetId(SetTargetIdPacket packet)
        {
            ManifestationManager.Instance.SetTargetId(Client, packet.EntityId);
        }

        [PacketHandler(GameOpcode.SetTrackingTarget)]
        private void SetTrackingTarget(SetTrackingTargetPacket packet)
        {
            ManifestationManager.Instance.SetTrackingTarget(Client, packet.EntityId);
        }

        [PacketHandler(GameOpcode.StartAutoFire)]
        private void StartAutoFire(StartAutoFirePacket packet)
        {
            ManifestationManager.Instance.StartAutoFire(Client, packet.FromUi);
        }

        [PacketHandler(GameOpcode.StopAutoFire)]
        private void StopAutoFire(StopAutoFirePacket packet)
        {
            ManifestationManager.Instance.StopAutoFire(Client);
        }

        [PacketHandler(GameOpcode.TeleportAcknowledge)]
        private void TeleportAcknowledge(TeleportAcknowledgePacket packet)
        {
            DynamicObjectManager.Instance.TeleportAcknowledge(Client);
        }

        #region Clan

        [PacketHandler(GameOpcode.ClanChangeRankTitle)]
        private void ClanChangeRankTitle(ClanChangeRankTitlePacket packet)
        {
            ClanManager.Instance.ClanChangeRankTitle(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanCreditTransfer)]
        private void ClanCreditTransfer(ClanCreditTransferPacket packet)
        {
            InventoryManager.Instance.ClanCreditTransfer(Client, packet.Ammount, packet.CreditsType);
        }

        [PacketHandler(GameOpcode.ClanDemotePlayer)]
        private void CreateClan(ClanDemotePlayerPacket packet)
        {
            ClanManager.Instance.ClanDemotePlayer(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanInvitationResponse)]
        private void ClanInvitationResponse(ClanInvitationResponsePacket packet)
        {
            ClanManager.Instance.ClanInvitationResponse(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanPromotePlayer)]
        private void ClanPromotePlayer(ClanPromotePlayerPacket packet)
        {
            ClanManager.Instance.ClanPromotePlayer(Client, packet);
        }

        [PacketHandler(GameOpcode.DisbandClan)]
        private void DisbandClan(DisbandClanPacket packet)
        {
            ClanManager.Instance.DisbandClan(Client, packet);
        }

        [PacketHandler(GameOpcode.InviteToClanById)]
        private void InviteToClanById(InviteToClanByIdPacket packet)
        {
            ClanManager.Instance.InviteToClanById(Client, packet);
        }

        [PacketHandler(GameOpcode.InviteToClanByName)]
        private void InviteToClanByName(InviteToClanByNamePacket packet)
        {
            ClanManager.Instance.InviteToClanByName(Client, packet);
        }

        [PacketHandler(GameOpcode.KickPlayerFromClan)]
        private void KickPlayerFromClan(KickPlayerFromClanPacket packet)
        {
            ClanManager.Instance.KickPlayerFromClan(Client, packet);
        }

        [PacketHandler(GameOpcode.KickPlayerFromClanByName)]
        private void KickPlayerFromClanByName(KickPlayerFromClanByNamePacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"KickPlayerFromClanByNamePacket => ToDo");
        }

        [PacketHandler(GameOpcode.LeaveClan)]
        private void LeaveClan(LeaveClanPacket packet)
        {
            ClanManager.Instance.LeaveClan(Client, packet);
        }

        [PacketHandler(GameOpcode.MakePlayerClanLeader)]
        private void MakePlayerClanLeader(MakePlayerClanLeaderPacket packet)
        {
            ClanManager.Instance.MakePlayerClanLeader(Client, packet);
        }
        #endregion

        #region Inventory

        [PacketHandler(GameOpcode.ClanLockbox_DepositItemInSlot)]
        private void ClanLockbox_DepositItemInSlot(ClanLockbox_DepositItemInSlotPacket packet)
        {
            InventoryManager.Instance.ClanLockbox_DepositItemInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanLockbox_DepositItemInTab)]
        private void ClanLockbox_MoveItem(ClanLockbox_DepositItemInTabPacket packet)
        {
            InventoryManager.Instance.ClanLockbox_DepositItemInTab(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanLockbox_DestroyItem)]
        private void ClanLockbox_DestroyItem(ClanLockbox_DestroyItemPacket packet)
        {
            InventoryManager.Instance.ClanLockbox_DestroyItem(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanLockbox_MoveItem)]
        private void ClanLockbox_MoveItem(ClanLockbox_MoveItemPacket packet)
        {
            InventoryManager.Instance.ClanLockbox_MoveItem(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanLockbox_WithdrawItem)]
        private void ClanLockbox_WithdrawItem(ClanLockbox_WithdrawItemPacket packet)
        {
            InventoryManager.Instance.ClanLockbox_WithdrawItem(Client, packet);
        }

        [PacketHandler(GameOpcode.HomeInventory_DestroyItem)]
        private void HomeInventory_DestroyItem(HomeInventory_DestroyItemPacket packet)
        {
            InventoryManager.Instance.HomeInventory_DestroyItem(Client, packet);
        }

        [PacketHandler(GameOpcode.HomeInventory_MoveItem)]
        private void HomeInventory_MoveItem(HomeInventory_MoveItemPacket packet)
        {
            InventoryManager.Instance.HomeInventory_MoveItem(Client, packet);
        }

        // ToDo: OverflowTransfer(destType, entityId, quantity, slot)

        [PacketHandler(GameOpcode.PersonalInventory_DestroyItem)]
        private void PersonalInventory_DestroyItem(PersonalInventory_DestroyItemPacket packet)
        {
            InventoryManager.Instance.PersonalInventory_DestroyItem(Client, packet);
        }

        [PacketHandler(GameOpcode.PersonalInventory_MoveItem)]
        private void PersonalInventory_MoveItem(PersonalInventory_MoveItemPacket packet)
        {
            InventoryManager.Instance.PersonalInventory_MoveItem(Client, packet);
        }

        // ToDo: PurchaseClanLockboxTab(tabId)

        [PacketHandler(GameOpcode.PurchaseLockboxTab)]
        private void PurchaseLockboxTab(PurchaseLockboxTabPacket packet)
        {
            InventoryManager.Instance.PurchaseLockboxTab(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestEquipArmor)]
        private void RequestEquipArmor(RequestEquipArmorPacket packet)
        {
            InventoryManager.Instance.RequestEquipArmor(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestEquipWeapon)]
        private void RequestEquipWeapon(RequestEquipWeaponPacket packet)
        {
            InventoryManager.Instance.RequestEquipWeapon(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestLockboxTabPermissions)]
        private void RequestLockboxTabPermissions(RequestLockboxTabPermissionsPacket packet)
        {
            InventoryManager.Instance.RequestLockboxTabPermissions(Client);
        }

        [PacketHandler(GameOpcode.RequestMoveItemToHomeInventory)]
        private void RequestMoveItemToHomeInventory(RequestMoveItemToHomeInventoryPacket packet)
        {
            InventoryManager.Instance.RequestMoveItemToHomeInventory(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestTakeItemFromHomeInventory)]
        private void RequestTakeItemFromHomeInventory(RequestTakeItemFromHomeInventoryPacket packet)
        {
            InventoryManager.Instance.RequestTakeItemFromHomeInventory(Client, packet);
        }

        // ToDo: RequestTakeItemFromInboxInventory(auctioneerId, entityId, destSlot)

        [PacketHandler(GameOpcode.TransferCreditToLockbox)]
        private void TransferCreditToLockbox(TransferCreditToLockboxPacket packet)
        {
            InventoryManager.Instance.TransferCreditToLockbox(Client, packet.Ammount);
        }

        [PacketHandler(GameOpcode.WeaponDrawerInventory_MoveItem)]
        private void WeaponDrawerInventory_MoveItem(WeaponDrawerInventory_MoveItemPacket packet)
        {
            InventoryManager.Instance.WeaponDrawerInventory_MoveItem(Client, packet);
        }

        #endregion

        #region Social

        [PacketHandler(GameOpcode.AddFriendByName)]
        private void AddFriendByName(AddFriendByNamePacket packet)
        {
            SocialManager.Instance.AddFriendByName(Client, packet);
        }

        [PacketHandler(GameOpcode.AddIgnore)]
        private void AddIgnore(AddIgnorePacket packet)
        {
            SocialManager.Instance.AddIgnore(Client, packet);
        }

        [PacketHandler(GameOpcode.AddIgnoreByName)]
        private void AddIgnoreByName(AddIgnoreByNamePacket packet)
        {
            SocialManager.Instance.AddIgnoreByName(Client, packet);
        }
        
        [PacketHandler(GameOpcode.RemoveFriend)]
        private void RemoveFriend(RemoveFriendPacket packet)
        {
            SocialManager.Instance.RemoveFriend(Client, packet);
        }

        [PacketHandler(GameOpcode.RemoveIgnore)]
        private void RemoveIgnore(RemoveIgnorePacket packet)
        {
            SocialManager.Instance.RemoveIgnore(Client, packet);
        }
        #endregion
    }
}
