namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.MapChannel.Client;
    using Packets.Clan.Client;
    using Packets.Crafting.Client;
    using Packets.Communicator.Both;
    using Packets.Communicator.Client;
    using Packets.Inventory.Client;
    using Packets.LookingForGroup.Client;
    using Packets.LootDispenser.Client;
    using Packets.Manifestation.Client;
    using Packets.Minion.Client;
    using Packets.Party.Both;
    using Packets.Party.Client;
    using Packets.Petition.Client;
    using Packets.Summon.Client;
    using Packets.Social.Client;
    using Packets.Trade.Client;

    public partial class ClientPacketHandler
    {
        public Client Client { get; private set; }

        public ClientPacketHandler()
        {
        }

        public void RegisterClient(Client client)
        {
            Client = client;
        }

        [PacketHandler(GameOpcode.AbandonMission)]
        private void AbandonMission(AbandonMissionPacket packet)
        {
            NpcManager.Instance.AbandonMission(Client, packet);
        }
        
        [PacketHandler(GameOpcode.AllocateAttributePoints)]
        private void AllocateAttributePoints(AllocateAttributePointsPacket packet)
        {
            ManifestationManager.Instance.AllocateAttributePoints(Client, packet);
        }

        [PacketHandler(GameOpcode.SelectNewCharacterClass)]
        private void SelectNewCharacterClass(SelectNewCharacterClassPacket packet)
        {
            ManifestationManager.Instance.SelectNewCharacterClass(Client, packet);
        }

        [PacketHandler(GameOpcode.AssignNPCMission)]
        private void AssignNPCMission(AssignNPCMissionPacket packet)
        {
            NpcManager.Instance.AssignNPCMission(Client, packet);
        }

        [PacketHandler(GameOpcode.AssignRadioMission)]
        private void AssignRadioMission(AssignRadioMissionPacket packet)
        {
            MissionApplication.Instance.TryAcceptRadioMission(
                Client, packet.MissionId);
        }

        [PacketHandler(GameOpcode.ShareMission)]
        private void ShareMission(ShareMissionPacket packet) =>
            MissionApplication.Instance.Sharing.TryShare(Client, packet.MissionId);

        [PacketHandler(GameOpcode.AssignSharedMission)]
        private void AssignSharedMission(AssignSharedMissionPacket packet) =>
            MissionApplication.Instance.Sharing.TryAccept(Client, packet.SourcePlayerEntityId, packet.MissionId);

        [PacketHandler(GameOpcode.AutoFireKeepAlive)]
        private void AutoFireKeepAlive(AutoFireKeepAlivePacket packet)
        {
            ManifestationManager.Instance.AutoFireKeepAlive(Client, packet.KeepAliveDelay);
        }

        [PacketHandler(GameOpcode.CancelLogoutRequest)]
        private void CancelLogoutRequest(CancelLogoutRequestPacket packet)
        {
            MapChannelManager.Instance.CancelLogoutRequest(Client);
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

        [PacketHandler(GameOpcode.CompleteRadioMission)]
        private void CompleteRadioMission(CompleteRadioMissionPacket packet) =>
            MissionApplication.Instance.TryCompleteRadioMission(Client, packet.MissionId, packet.SelectionIdx, packet.Rating);

        [PacketHandler(GameOpcode.CompleteNPCObjective)]
        private void CompleteNPCObjective(CompleteNPCObjectivePacket packet)
        {
            NpcManager.Instance.CompleteNPCObjective(Client, packet);
        }

        [PacketHandler(GameOpcode.PerformNPCChoice)]
        private void PerformNPCChoice(PerformNPCChoicePacket packet)
        {
            NpcManager.Instance.PerformNPCChoice(Client, packet);
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

        [PacketHandler(GameOpcode.RewardNPCMission)]
        private void RewardNPCMission(RewardNPCMissionPacket packet)
        {
            NpcManager.Instance.RewardNPCMission(Client, packet);
        }
        
        [PacketHandler(GameOpcode.MapLoaded)]
        private void MapLoaded(MapLoadedPacket packet)
        {
            MapChannelManager.Instance.MapLoaded(Client);
        }
        
        // The minion commands. All nine are addressed to the player's own actor by
        // SendCallActorMethod, which the router does not care about - it dispatches on opcode -
        // and none of them names the minion, because the client does not know which entity that
        // is. MinionManager resolves it.
        //
        // The client hides every one of these unless the MinionCommands server flag is set, so a
        // server that has not turned the feature on never sees them.

        [PacketHandler(GameOpcode.MinionAssistMe)]
        private void MinionAssistMe(MinionAssistMePacket packet)
        {
            MinionManager.Instance.AssistMe(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionAssistTarget)]
        private void MinionAssistTarget(MinionAssistTargetPacket packet)
        {
            MinionManager.Instance.AssistTarget(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionCommand)]
        private void MinionCommand(MinionCommandPacket packet)
        {
            MinionManager.Instance.Command(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionFollowMe)]
        private void MinionFollowMe(MinionFollowMePacket packet)
        {
            MinionManager.Instance.FollowMe(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionFollowTarget)]
        private void MinionFollowTarget(MinionFollowTargetPacket packet)
        {
            MinionManager.Instance.FollowTarget(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionGo)]
        private void MinionGo(MinionGoPacket packet)
        {
            MinionManager.Instance.Go(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionStay)]
        private void MinionStay(MinionStayPacket packet)
        {
            MinionManager.Instance.Stay(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionTarget)]
        private void MinionTarget(MinionTargetPacket packet)
        {
            MinionManager.Instance.Target(Client, packet);
        }

        [PacketHandler(GameOpcode.MinionTargetMe)]
        private void MinionTargetMe(MinionTargetMePacket packet)
        {
            MinionManager.Instance.TargetMe(Client, packet);
        }

        [PacketHandler(GameOpcode.Ping)]
        private void Ping(PingPacket packet)
        {
            MapChannelManager.Instance.Ping(Client, packet.Ping);
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
        
        [PacketHandler(GameOpcode.RequestDetachGameEffect)]
        private void RequestDetachGameEffect(RequestDetachGameEffectPacket packet)
        {
            GestureManager.Instance.RequestDetachGameEffect(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestGesture)]
        private void RequestGesture(RequestGesturePacket packet)
        {
            GestureManager.Instance.RequestGesture(Client, packet);
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

        [PacketHandler(GameOpcode.RequestUseCloneCredit)]
        private void RequestUseCloneCredit(RequestUseCloneCreditPacket packet)
        {
            ManifestationManager.Instance.RequestUseCloneCredit(Client, packet);
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

        // Crafting, all made at a Kraftwerks station; see KraftwerksManager.

        [PacketHandler(GameOpcode.RequestCraftItemNew)]
        private void RequestCraftItemNew(RequestCraftItemNewPacket packet)
        {
            KraftwerksManager.Instance.RequestCraftItemNew(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCraftItem)]
        private void RequestCraftItem(RequestCraftItemPacket packet)
        {
            KraftwerksManager.Instance.RequestCraftItem(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestSalvageItem)]
        private void RequestSalvageItem(RequestSalvageItemPacket packet)
        {
            KraftwerksManager.Instance.RequestSalvageItem(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestExtractModule)]
        private void RequestExtractModule(RequestExtractModulePacket packet)
        {
            KraftwerksManager.Instance.RequestExtractModule(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestIntegrateItem)]
        private void RequestIntegrateItem(RequestIntegrateItemPacket packet)
        {
            KraftwerksManager.Instance.RequestIntegrateItem(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestUpgradeItem)]
        private void RequestUpgradeItem(RequestUpgradeItemPacket packet)
        {
            KraftwerksManager.Instance.RequestUpgradeItem(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestRetrieveFinishedCraftItem)]
        private void RequestRetrieveFinishedCraftItem(RequestRetrieveFinishedCraftItemPacket packet)
        {
            KraftwerksManager.Instance.RequestRetrieveFinishedCraftItem(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestRetrieveAllFinishedItems)]
        private void RequestRetrieveAllFinishedItems(RequestRetrieveAllFinishedItemsPacket packet)
        {
            KraftwerksManager.Instance.RequestRetrieveAllFinishedItems(Client, packet);
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

        [PacketHandler(GameOpcode.RequestRepair)]
        private void RequestRepair(RequestRepairPacket packet)
        {
            NpcManager.Instance.RequestRepair(Client, packet);
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

        [PacketHandler(GameOpcode.RequestToolAction)]
        private void RequestToolAction(RequestToolActionPacket packet)
        {
            ToolActionManager.Instance.RequestToolAction(Client, packet);
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
            LootDispenserManager.Instance.SetAutoLootThreshold(Client, packet);
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
            ClanManager.Instance.KickPlayerFromClanByName(Client, packet);
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

        #region Communicator

        [PacketHandler(GameOpcode.ChallengeClanToFeud)]
        private void ChallengeClanToFeud(ChallengeClanToFeudPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: ChallengeClanToFeudPacket");
        }

        [PacketHandler(GameOpcode.ChangeClanName)]
        private void ChangeClanName(ChangeClanNamePacket packet)
        {
            ClanManager.Instance.ChangeClanName(Client, packet);
        }

        [PacketHandler(GameOpcode.ChangeFirstName)]
        private void ChangeFirstName(ChangeFirstNamePacket packet)
        {
            CharacterManager.Instance.ChangeFirstName(Client, packet);
        }

        [PacketHandler(GameOpcode.ChangeLastName)]
        private void ChangeLastName(ChangeLastNamePacket packet)
        {
            CharacterManager.Instance.ChangeLastName(Client, packet);
        }

        [PacketHandler(GameOpcode.ChannelChat)]
        private void ChannelChat(ChannelChatPacket packet)
        {
            CommunicatorManager.Instance.ChannelChat(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanChat)]
        private void ClanChat(ClanChatPacket packet)
        {
            CommunicatorManager.Instance.ClanChat(Client, packet);
        }

        [PacketHandler(GameOpcode.ClanLeadersChat)]
        private void ClanLeadersChat(ClanLeadersChatPacket packet)
        {
            CommunicatorManager.Instance.ClanLeadersChat(Client, packet);
        }

        [PacketHandler(GameOpcode.Emote)]
        private void Emote(EmotePacket packet)
        {
            CommunicatorManager.Instance.Emote(Client, packet);
        }

        [PacketHandler(GameOpcode.FeudChallengeResponse)]
        private void FeudChallengeResponse(FeudChallengeResponsePacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: FeudChallengeResponsePacket");
        }

        [PacketHandler(GameOpcode.GotoMob)]
        private void GotoMob(GotoMobPacket packet)
        {
            CommunicatorManager.Instance.GotoMob(Client, packet);
        }

        [PacketHandler(GameOpcode.GuildChat)]
        private void GuildChat(GuildChatPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: GuildChatPacket");
        }

        [PacketHandler(GameOpcode.PartyChat)]
        private void PartyChat(PartyChatPacket packet)
        {
            CommunicatorManager.Instance.PartyChat(Client, packet);
        }

        [PacketHandler(GameOpcode.PrivilegedCommand)]
        private void PrivilegedCommand(PrivilegedCommandPacket packet)
        {
            ChatCommandsManager.Instance.PrivilegedCommand(Client, packet);
        }

        [PacketHandler(GameOpcode.RadialChat)]
        private void RadialChat(RadialChatPacket packet)
        {
            CommunicatorManager.Instance.RadialChat(Client, packet.TextMsg);
        }

        [PacketHandler(GameOpcode.Reply)]
        private void Reply(ReplyPacket packet)
        {
            CommunicatorManager.Instance.Reply(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestLOSReport)]
        private void RequestLOSReport(RequestLOSReportPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: RequestLOSReportPacket");
        }

        [PacketHandler(GameOpcode.RevokeClanFeud)]
        private void RevokeClanFeud(RevokeClanFeudPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: RevokeClanFeudPacket");
        }

        [PacketHandler(GameOpcode.Shout)]
        private void Shout(ShoutPacket packet)
        {
            CommunicatorManager.Instance.Shout(Client, packet.TextMsg);
        }

        [PacketHandler(GameOpcode.SurrenderClanFeud)]
        private void SurrenderClanFeud(SurrenderClanFeudPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: SurrenderClanFeudPacket");
        }

        [PacketHandler(GameOpcode.SurrenderWargame)]
        private void SurrenderWargame(SurrenderWargamePacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: SurrenderWargamePacket");
        }

        [PacketHandler(GameOpcode.ToggleAfk)]
        private void ToggleAfk(ToggleAfkPacket packet)
        {
            ManifestationManager.Instance.ToggleAfk(Client);
        }

        [PacketHandler(GameOpcode.Whisper)]
        private void Whisper(WhisperPacket packet)
        {
            CommunicatorManager.Instance.Whisper(Client, packet);
        }

        [PacketHandler(GameOpcode.Who)]
        private void Who(WhoPacket packet)
        {
            CommunicatorManager.Instance.Who(Client, packet);
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

        [PacketHandler(GameOpcode.PurchaseClanLockboxTab)]
        private void PurchaseClanLockboxTab(PurchaseClanLockboxTabPacket packet)
        {
            InventoryManager.Instance.PurchaseClanLockboxTab(Client, packet);
        }

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

        [PacketHandler(GameOpcode.RequestUnstick)]
        private void RequestUnstick(RequestUnstickPacket packet)
        {
            ManifestationManager.Instance.RequestUnstick(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestTakeItemFromInboxInventory)]
        private void RequestTakeItemFromInboxInventory(RequestTakeItemFromInboxInventoryPacket packet)
        {
            InventoryManager.Instance.RequestTakeItemFromInboxInventory(Client, packet);
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

        #endregion

        #region LootDispenser
        [PacketHandler(GameOpcode.RequestCorpseLooting)]
        private void RequestCorpseLooting(RequestCorpseLootingPacket packet)
        {
            LootDispenserManager.Instance.RequestCorpseLooting(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestLootAllFromCorpse)]
        private void RequestLootAllFromCorpse(RequestLootAllFromCorpsePacket packet)
        {
            LootDispenserManager.Instance.RequestLootAllFromCorpse(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestLootItemFromCorpse)]
        private void RequestLootItemFromCorpse(RequestLootItemFromCorpsePacket packet)
        {
            LootDispenserManager.Instance.RequestLootItemFromCorpse(Client, packet);
        }

        [PacketHandler(GameOpcode.CancelCorpseLooting)]
        private void CancelCorpseLooting(CancelCorpseLootingPacket packet)
        {
            LootDispenserManager.Instance.CancelCorpseLooting(Client, packet);
        }
        #endregion

        #region LookingForGroup

        [PacketHandler(GameOpcode.RemoveLookingForGroupAd)]
        private void RemoveLookingForGroupAd(RemoveLookingForGroupAdPacket packet)
        {
            LookingForGroupManager.Instance.RemoveLookingForGroupAd(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCreateLookingForGroupAd)]
        private void RequestCreateLookingForGroupAd(RequestCreateLookingForGroupAdPacket packet)
        {
            LookingForGroupManager.Instance.RequestCreateLookingForGroupAd(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestLookingForGroupSearch)]
        private void RequestLookingForGroupSearch(RequestLookingForGroupSearchPacket packet)
        {
            LookingForGroupManager.Instance.RequestLookingForGroupSearch(Client, packet);
        }

        #endregion

        #region Party

        [PacketHandler(GameOpcode.AcceptPartyInvitesChanged)]
        private void AcceptPartyInvitesChanged(AcceptPartyInvitesChangedPacket packet)
        {
            PartyManager.Instance.AcceptPartyInvitesChanged(Client, packet);
        }

        [PacketHandler(GameOpcode.CancelSquadInviteRequest)]
        private void CancelSquadInviteRequest(CancelSquadInviteRequestPacket packet)
        {
            PartyManager.Instance.CancelSquadInviteRequest(Client, packet);
        }

        [PacketHandler(GameOpcode.CancelSquadJoinRequest)]
        private void CancelSquadJoinRequest(CancelSquadJoinRequestPacket packet)
        {
            PartyManager.Instance.CancelSquadJoinRequest(Client, packet);
        }

        [PacketHandler(GameOpcode.ChangePartyLootMethod)]
        private void ChangePartyLootMethod(ChangePartyLootMethodPacket packet)
        {
            PartyManager.Instance.ChangePartyLootMethod(Client, packet);
        }

        [PacketHandler(GameOpcode.ChangePartyLootThreshold)]
        private void ChangePartyLootThreshold(ChangePartyLootThresholdPacket packet)
        {
            PartyManager.Instance.ChangePartyLootThreshold(Client, packet);
        }

        [PacketHandler(GameOpcode.DisbandParty)]
        private void DisbandParty(DisbandPartyPacket packet)
        {
            PartyManager.Instance.DisbandParty(Client);
        }

        [PacketHandler(GameOpcode.InviteUserToPartyByName)]
        private void InviteUserToPartyByName(InviteUserToPartyByNamePacket packet)
        {
            PartyManager.Instance.InviteUserToPartyByName(Client, packet);
        }

        [PacketHandler(GameOpcode.InviteSquad)]
        private void InviteSquad(InviteSquadPacket packet)
        {
            PartyManager.Instance.InviteSquad(Client, packet);
        }

        [PacketHandler(GameOpcode.LeaveParty)]
        private void LeaveParty(LeavePartyPacket packet)
        {
            PartyManager.Instance.LeaveParty(Client);
        }

        [PacketHandler(GameOpcode.KickUserFromParty)]
        private void KickUserFromParty(KickUserFromPartyPacket packet)
        {
            PartyManager.Instance.KickUserFromParty(Client, packet);
        }

        [PacketHandler(GameOpcode.KickUserFromPartyById)]
        private void KickUserFromPartyById(KickUserFromPartyByIdPacket packet)
        {
            PartyManager.Instance.KickUserFromPartyById(Client, packet);
        }

        [PacketHandler(GameOpcode.MakeUserPartyLeader)]
        private void MakeUserPartyLeader(MakeUserPartyLeaderPacket packet)
        {
            PartyManager.Instance.MakeUserPartyLeader(Client, packet);
        }

        [PacketHandler(GameOpcode.MakeUserPartyLeaderById)]
        private void MakeUserPartyLeaderById(MakeUserPartyLeaderByIdPacket packet)
        {
            PartyManager.Instance.MakeUserPartyLeaderById(Client, packet);
        }

        #region Summon

        [PacketHandler(GameOpcode.InviteFriendToJoin)]
        private void InviteFriendToJoin(InviteFriendToJoinPacket packet)
        {
            SummonManager.Instance.InviteFriendToJoin(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestInvitationToJoin)]
        private void RequestInvitationToJoin(RequestInvitationToJoinPacket packet)
        {
            SummonManager.Instance.RequestInvitationToJoin(Client, packet);
        }

        [PacketHandler(GameOpcode.RespondToJoinFriend)]
        private void RespondToJoinFriend(RespondToJoinFriendPacket packet)
        {
            SummonManager.Instance.RespondToJoinFriend(Client, packet);
        }

        [PacketHandler(GameOpcode.RespondToAddAndJoinFriend)]
        private void RespondToAddAndJoinFriend(RespondToAddAndJoinFriendPacket packet)
        {
            SummonManager.Instance.RespondToAddAndJoinFriend(Client, packet);
        }

        [PacketHandler(GameOpcode.RespondToRequestToJoin)]
        private void RespondToRequestToJoin(RespondToRequestToJoinPacket packet)
        {
            SummonManager.Instance.RespondToRequestToJoin(Client, packet);
        }

        #endregion

        [PacketHandler(GameOpcode.PartyInvitationResponse)]
        private void PartyInvitationResponse(PartyInvitationResponsePacket packet)
        {
            PartyManager.Instance.PartyInvitationResponse(Client, packet);
        }

        [PacketHandler(GameOpcode.PartyJoinRequestResponse)]
        private void PartyJoinRequestResponse(PartyJoinRequestResponsePacket packet)
        {
            PartyManager.Instance.PartyJoinRequestResponse(Client, packet);
        }

        [PacketHandler(GameOpcode.SendJoinRequestToPartyByName)]
        private void SendJoinRequestToPartyByName(SendJoinRequestToPartyByNamePacket packet)
        {
            PartyManager.Instance.SendJoinRequestToPartyByName(Client, packet);
        }

        [PacketHandler(GameOpcode.SendJoinRequestToSquadLeader)]
        private void SendJoinRequestToSquadLeader(SendJoinRequestToSquadLeaderPacket packet)
        {
            PartyManager.Instance.SendJoinRequestToSquadLeader(Client, packet);
        }

        #endregion

        #region Trade

        [PacketHandler(GameOpcode.RequestAcceptTradeRequest)]
        private void RequestAcceptTradeRequest(RequestAcceptTradeRequestPacket packet)
        {
            TradeManager.Instance.RequestAcceptTradeRequest(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestAddItemToTrade)]
        private void RequestAddItemToTrade(RequestAddItemToTradePacket packet)
        {
            TradeManager.Instance.RequestAddItemToTrade(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCancelTrade)]
        private void RequestCancelTrade(RequestCancelTradePacket packet)
        {
            TradeManager.Instance.RequestCancelTrade(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestChangeEnergyUnitAmount)]
        private void RequestChangeEnergyUnitAmount(RequestChangeEnergyUnitAmountPacket packet)
        {
            TradeManager.Instance.RequestChangeEnergyUnitAmount(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestConfirmTrade)]
        private void RequestConfirmTrade(RequestConfirmTradePacket packet)
        {
            TradeManager.Instance.RequestConfirmTrade(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestRemoveItemFromTrade)]
        private void RequestRemoveItemFromTrade(RequestRemoveItemFromTradePacket packet)
        {
            TradeManager.Instance.RequestRemoveItemFromTrade(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestTrade)]
        private void RequestTrade(RequestTradePacket packet)
        {
            TradeManager.Instance.RequestTrade(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestUnconfirmTrade)]
        private void RequestUnconfirmTrade(RequestUnconfirmTradePacket packet)
        {
            TradeManager.Instance.RequestUnconfirmTrade(Client, packet);
        }

        #endregion

        #region Petition

        [PacketHandler(GameOpcode.CreateBugReport)]
        private void CreateBugReport(CreateBugReportPacket packet)
        {
            PetitionManager.Instance.CreateBugReport(Client, packet);
        }

        [PacketHandler(GameOpcode.CreateHelpRequest)]
        private void CreateHelpRequest(CreateHelpRequestPacket packet)
        {
            PetitionManager.Instance.CreateHelpRequest(Client, packet);
        }

        [PacketHandler(GameOpcode.CancelPetition)]
        private void CancelPetition(CancelPetitionPacket packet)
        {
            PetitionManager.Instance.CancelPetition(Client, packet);
        }

        [PacketHandler(GameOpcode.RetrievePetition)]
        private void RetrievePetition(RetrievePetitionPacket packet)
        {
            PetitionManager.Instance.RetrievePetition(Client, packet);
        }

        [PacketHandler(GameOpcode.AddToPetition)]
        private void AddToPetition(AddToPetitionPacket packet)
        {
            PetitionManager.Instance.AddToPetition(Client, packet);
        }

        [PacketHandler(GameOpcode.SearchKB)]
        private void SearchKB(SearchKBPacket packet)
        {
            PetitionManager.Instance.SearchKB(Client, packet);
        }

        [PacketHandler(GameOpcode.RetrieveKBArticle)]
        private void RetrieveKBArticle(RetrieveKBArticlePacket packet)
        {
            PetitionManager.Instance.RetrieveKBArticle(Client, packet);
        }

        [PacketHandler(GameOpcode.SearchPetitions)]
        private void SearchPetitions(SearchPetitionsPacket packet)
        {
            PetitionManager.Instance.SearchPetitions(Client, packet);
        }

        #endregion

        #region Social

        [PacketHandler(GameOpcode.AddFriend)]
        private void AddFriend(AddFriendPacket packet)
        {
            SocialManager.Instance.AddFriend(Client, packet);
        }

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

        [PacketHandler(GameOpcode.RemoveFriendByName)]
        private void RemoveFriendByName(RemoveFriendByNamePacket packet)
        {
            SocialManager.Instance.RemoveFriendByName(Client, packet);
        }

        [PacketHandler(GameOpcode.RemoveIgnore)]
        private void RemoveIgnore(RemoveIgnorePacket packet)
        {
            SocialManager.Instance.RemoveIgnore(Client, packet);
        }

        [PacketHandler(GameOpcode.RemoveIgnoreByName)]
        private void RemoveIgnoreByName(RemoveIgnoreByNamePacket packet)
        {
            SocialManager.Instance.RemoveIgnoreByName(Client, packet);
        }
        #endregion
    }
}
