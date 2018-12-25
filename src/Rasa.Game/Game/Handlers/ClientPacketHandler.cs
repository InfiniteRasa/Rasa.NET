namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.MapChannel.Client;

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

        [PacketHandler(GameOpcode.ChangeTitle)]
        private void ChangeTitle(ChangeTitlePacket packet)
        {
            ManifestationManager.Instance.ChangeTitle(Client, packet.TitleId);
        }

        [PacketHandler(GameOpcode.ChangeShowHelmet)]
        private void ChangeShowHelmet(ChangeShowHelmetPacket packet)
        {
            ManifestationManager.Instance.ChangeShowHelmet(Client, packet);
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

        [PacketHandler(GameOpcode.GetCustomizationChoices)]
        private void GetCustomizationChoices(GetCustomizationChoicesPacket packet)
        {
            ManifestationManager.Instance.GetCustomizationChoices(Client, packet);
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

        [PacketHandler(GameOpcode.Ping)]
        private void Ping(PingPacket packet)
        {
            MapChannelManager.Instance.Ping(Client, packet.Ping);
        }

        [PacketHandler(GameOpcode.PurchaseLockboxTab)]
        private void PurchaseLockboxTab(PurchaseLockboxTabPacket packet)
        {
            InventoryManager.Instance.PurchaseLockboxTab(Client, packet);
        }

        [PacketHandler(GameOpcode.RadialChat)]
        private void RadialChat(RadialChatPacket packet)
        {
            CommunicatorManager.Instance.Recv_RadialChat(Client, packet.TextMsg);
        }

        [PacketHandler(GameOpcode.RequestActionInterrupt)]
        private void RequestActionInterrupt(RequestActionInterruptPacket packet)
        {
            ManifestationManager.Instance.RequestActionInterrupt(Client, packet);
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

        [PacketHandler(GameOpcode.RequestCancelAuction)]
        private void RequestCancelAuction(RequestCancelAuctionPacket packet)
        {
            AuctionHouseManager.Instance.RequestCancelAuction(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCancelAuctioneer)]
        private void RequestCancelAuctioneer(RequestCancelAuctioneerPacket packet)
        {
            AuctionHouseManager.Instance.RequestCancelAuctioneer(Client);
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

        [PacketHandler(GameOpcode.RequestLogout)]
        private void RequestLogout(RequestLogoutPacket packet)
        {
            MapChannelManager.Instance.RequestLogout(Client);
        }

        [PacketHandler(GameOpcode.RequestMoveItemToHomeInventory)]
        private void RequestMoveItemToHomeInventory(RequestMoveItemToHomeInventoryPacket packet)
        {
            InventoryManager.Instance.RequestMoveItemToHomeInventory(Client, packet);
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

        [PacketHandler(GameOpcode.RequestTakeItemFromHomeInventory)]
        private void RequestTakeItemFromHomeInventory(RequestTakeItemFromHomeInventoryPacket packet)
        {
            InventoryManager.Instance.RequestTakeItemFromHomeInventory(Client, packet);
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
            ManifestationManager.Instance.RequestVisualCombatMode(Client, packet.CombatMode);
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
            ManifestationManager.Instance.RequestWeaponReload(Client);
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

        [PacketHandler(GameOpcode.SetAutoLootThreshold)]
        private void SetAutoLootThreshold(SetAutoLootThresholdPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo SetAutoLootThreshold");
        }

        [PacketHandler(GameOpcode.SetDesiredCrouchState)]
        private void SetDesiredCrouchState(SetDesiredCrouchStatePacket packet)
        {
            ManifestationManager.Instance.SetDesiredCrouchState(Client, packet.DesiredStateId);
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
            //ManifestationManager.Instance.StartAutoFire(Client, packet.FromUi);
            Logger.WriteLog(LogType.Debug, $"StartAutoFirePacket {packet.FromUi}");
        }

        [PacketHandler(GameOpcode.StopAutoFire)]
        private void StopAutoFire(StopAutoFirePacket packet)
        {
            ManifestationManager.Instance.StopAutoFire(Client);
        }

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
    }
}
