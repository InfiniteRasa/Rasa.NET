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

        /// <summary>
        /// Until this existed the opcode had no handler, and an unhandled opcode fails the packet
        /// terminator check and closes the connection: Decline on a shared mission disconnected the
        /// recipient.
        /// </summary>
        [PacketHandler(GameOpcode.DeclineSharedMission)]
        private void DeclineSharedMission(DeclineSharedMissionPacket packet) =>
            MissionApplication.Instance.Sharing.TryDecline(Client, packet.SourcePlayerEntityId, packet.MissionId);

        [PacketHandler(GameOpcode.AutoFireKeepAlive)]
        private void AutoFireKeepAlive(AutoFireKeepAlivePacket packet)
        {
            ManifestationManager.Instance.AutoFireKeepAlive(Client, packet.KeepAliveDelay);
        }

        /// <summary>
        /// A client effect calling the server (BaseGameEffect.SendMethodCall). Nothing in the
        /// client's Python ever does, and no effect here takes calls, so it is logged and
        /// dropped - handled so that one arriving is read to its end rather than closing the
        /// connection over a payload nobody could parse.
        /// </summary>
        [PacketHandler(GameOpcode.CallGameEffectMethod)]
        private void CallGameEffectMethod(CallGameEffectMethodPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"{Client.Player?.Name} called {packet.MethodName} on effect {packet.EffectId}; no server effect takes calls.");
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

        /// <summary>
        /// ExamineHack (67), and the ExamineResults (68) it would be answered with: a developer's
        /// readout of an entity. Deliberately a placeholder that sends nothing.
        ///
        /// The request is client/physicalentity.py OnExamine:
        /// <c>SendCallUserMethod('ExamineHack', (self.entityId,))</c>, for any entity. Nothing in the
        /// retail client calls OnExamine - no slash command, key binding, menu or UI element, and the
        /// string "examine" appears nowhere in tabula_rasa.exe - so, like the developer commands in
        /// client_nca_internal, which the retail client does not ship, it was reached from tools the
        /// players never had. The only reference is the method's own definition.
        ///
        /// The answer is Recv_ExamineResults(resultDict) on the examined entity. The dict it reads:
        ///  - every entity: 'repr', 'classId', 'classCollisionRole', 'collisionRole',
        ///    'position' (x, y, z) and 'quaternion' (x, y, z, w);
        ///  - an actor, when 'isActor' is present: 'isPlayer', 'level', 'xp', 'abilities',
        ///    'targetId', 'factions' and 'attributes'.
        /// It formats them under "===[SERVER]===", adds the client's own position and rotation under
        /// "===[CLIENT]===" and the server-to-client position delta and distance - a check for
        /// position drift. Then it fetches the OK button's text and returns: in the shipped build
        /// (trpython.zip, client/physicalentity.pyo) the call that would have shown the report is
        /// gone, so a reply would be built and thrown away.
        ///
        /// Neither server answered it: the C++ server lists the method ids only. The handler exists
        /// so that a client that does send it is not disconnected - an opcode with no handler fails
        /// the packet terminator check and closes the connection. The same information for a GM is
        /// what .npcinfo, .where and .los give in chat.
        /// </summary>
        [PacketHandler(GameOpcode.ExamineHack)]
        private void ExamineHack(ExamineHackPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"{Client.Player?.Name} sent ExamineHack for entity {packet.EntityId}; ExamineResults is not sent (developer-only, and the retail client does not display it).");
        }

        /// <summary>
        /// GetServerSkeleton (281), and the ServerSkeleton (361) it would be answered with: an
        /// entity's server-side physics skeleton, for a developer to draw over the client's own
        /// collision. Deliberately a placeholder that sends nothing.
        ///
        /// The request is client/physicalentity.py GetServerSkeleton, marked "DEVELOPMENT ONLY":
        /// <c>SendCallUserMethod('GetServerSkeleton', (self.entityId,))</c>. Nothing calls it -
        /// only physicalentity.pyo in trpython.zip has the name, and tabula_rasa.exe does not - so,
        /// like ExamineHack and the server collision overlay, it was reached from developer tools
        /// the retail client does not ship. The answer could not be used anyway: the exe's entity
        /// body has no SetServerSkeleton, so a skeleton with data raises AttributeError (see
        /// ServerSkeletonPacket). This server has no skeletons to send either.
        ///
        /// The handler exists so that a client that does send it is not disconnected - an opcode
        /// with no handler fails the packet terminator check and closes the connection.
        /// </summary>
        [PacketHandler(GameOpcode.GetServerSkeleton)]
        private void GetServerSkeleton(GetServerSkeletonPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"{Client.Player?.Name} sent GetServerSkeleton for entity {packet.EntityId}; ServerSkeleton is not sent (developer-only, and the retail client cannot load it).");
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

        [PacketHandler(GameOpcode.RequestUseTransferCredit)]
        private void RequestUseTransferCredit(RequestUseTransferCreditPacket packet)
        {
            ManifestationManager.Instance.RequestUseTransferCredit(Client, packet);
        }

        [PacketHandler(GameOpcode.FinishedCameraScript)]
        private void FinishedCameraScript(FinishedCameraScriptPacket packet)
        {
            CameraScripts.Finished(Client, packet.ScriptId);
        }

        [PacketHandler(GameOpcode.RequestAddLogosStoneToTabula)]
        private void RequestAddLogosStoneToTabula(RequestAddLogosStoneToTabulaPacket packet)
        {
            ManifestationManager.Instance.RequestAddLogosStoneToTabula(Client, packet);
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

        /// <summary>
        /// RequestDisassembleItem (676) and RequestModifyItem (677): the Reverse Engineering and
        /// Modification pages of the old crafting window. Deliberately placeholders that do
        /// nothing and send nothing.
        ///
        /// client/ui/craftingwindow.py is the Crafting v1 window - Fabrication, Modification and
        /// Disassembly ("Reverse Engineering") tabs. Its disassembly page takes an item from the
        /// personal inventory that has at least one loot module with a row in the module crafting
        /// table, lists each module's ComponentItemClassId with its ReverseEngineerChance, and on
        /// the button sends <c>RequestDisassembleItem((kraftwerksId, itemId))</c>, a 5 s job
        /// (shared/crafting.py g_disassemblyTime; failure is PM_CRAFTING_DISASSEMBLY_FAILED,
        /// 10000115). Its modification page applies a modification recipe to one of the item's
        /// modules, sending <c>RequestModifyItem((kraftwerksId, recipeId, itemId,
        /// selectedModuleClassId, attemptCriticalSuccess))</c>.
        ///
        /// In the 1.16.5 client neither can be sent:
        ///  - the window never opens. A station posts UI_CRAFTINGSTATION_ACTIVATE on Recv_Use, which
        ///    only craftingwindownew.py - the Crafting v2 window (fabrication, salvage, extraction,
        ///    integration, upgrade) - handles. The old window opens on UI_KRAFTWERKS_ACTIVATED, and
        ///    nothing posts that: only uieventmanager.pyo and craftingwindow.pyo in trpython.zip
        ///    name it. Tabula_Rasa_UI_EXPORT.xml still defines both, CraftingWindow (script
        ///    craftingwindow) and CraftingWindow_NEW;
        ///  - the data behind both pages is empty. generated.shared.recipe's
        ///    spGenShared_ModuleClassCrafting and spGenShared_RecipeModuleEnhancement have no rows
        ///    (the recipe templates, their inputs and the module classes do), so no item passes the
        ///    disassembly slot's check and there is no modification recipe;
        ///  - here, items carry no modules: ItemInfo sends empty classModuleIds and lootModuleIds.
        ///
        /// The C++ server only listed the ids. The v2 pages' requests are answered by
        /// KraftwerksManager.
        ///
        /// The handlers exist so that a client that does send one is not disconnected - an opcode
        /// with no handler fails the packet terminator check and closes the connection.
        /// </summary>
        [PacketHandler(GameOpcode.RequestDisassembleItem)]
        private void RequestDisassembleItem(RequestDisassembleItemPacket packet)
        {
            Logger.WriteLog(LogType.Debug,
                $"{Client.Player?.Name} sent RequestDisassembleItem (station {packet.KraftwerksId}, item {packet.ItemId}); the old crafting window's reverse engineering is not supported, nothing done.");
        }

        /// <inheritdoc cref="RequestDisassembleItem"/>
        [PacketHandler(GameOpcode.RequestModifyItem)]
        private void RequestModifyItem(RequestModifyItemPacket packet)
        {
            Logger.WriteLog(LogType.Debug,
                $"{Client.Player?.Name} sent RequestModifyItem (station {packet.KraftwerksId}, recipe {packet.RecipeId}, item {packet.ItemId}, module {packet.SelectedModuleClassId}, critical {packet.AttemptCriticalSuccess}); the old crafting window's modification is not supported, nothing done.");
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

        [PacketHandler(GameOpcode.RequestCritDeathFinish)]
        private void RequestCritDeathFinish(RequestCritDeathFinishPacket packet)
        {
            CritDeathManager.Instance.RequestCritDeathFinish(Client, packet);
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
            ManifestationManager.Instance.SetDesiredCrouchState(Client, packet.DesiredCrouchState);
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

        [PacketHandler(GameOpcode.ClanWarfareSearch)]
        private void ClanWarfareSearch(ClanWarfareSearchPacket packet)
        {
            ClanManager.Instance.ClanWarfareSearch(Client, packet);
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
            ClanFeuds.Instance.ChallengeClanToFeud(Client, packet.ClanName, packet.Invite);
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
            ClanFeuds.Instance.FeudChallengeResponse(Client, packet.ClanName, packet.AcceptChalange);
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
            LosReport.Answer(Client, packet.TargetId);
        }

        [PacketHandler(GameOpcode.RevokeClanFeud)]
        private void RevokeClanFeud(RevokeClanFeudPacket packet)
        {
            ClanFeuds.Instance.RevokeClanFeud(Client, packet.ClanName);
        }

        [PacketHandler(GameOpcode.Shout)]
        private void Shout(ShoutPacket packet)
        {
            CommunicatorManager.Instance.Shout(Client, packet.TextMsg);
        }

        [PacketHandler(GameOpcode.SurrenderClanFeud)]
        private void SurrenderClanFeud(SurrenderClanFeudPacket packet)
        {
            ClanFeuds.Instance.SurrenderClanFeud(Client, packet.ClanName);
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

        /// <summary>
        /// OverflowTransfer (308): taking an item out of the overflow inventory. Deliberately a
        /// placeholder that moves nothing and sends nothing.
        ///
        /// The overflow inventory is inventory type 7 (InventoryType.OverflowInventory), a list
        /// without slots. The server would fill it with AddOverflowItem, RemoveOverflowItem and
        /// ResetOverflowInventory, and client/inventory.py keeps the ids in g_overflowItems - a
        /// holding area for items the server could not fit in the pack.
        /// _TransferOverflowItem(entityId, quantity, slot, destType) takes one out, sending
        /// <c>OverflowTransfer((destType, entityId, quantity, slot))</c>, and refuses any destination
        /// but PERSONALINVENTORY (">>> Can only transfer overflow items to personal storage!").
        ///
        /// In the 1.16.5 client none of it can be used:
        ///  - nothing calls _TransferOverflowItem, in the decompiled source or the shipped
        ///    inventory.pyo in trpython.zip;
        ///  - no window lists overflow items and nothing calls HaveOverflow() (the only "Overflow"
        ///    widget in Tabula_Rasa_UI_EXPORT.xml is the status updater's "more" button);
        ///  - _SendServerRequest, which picks the request for a drag between two inventories, has
        ///    no case for OVERFLOWINVENTORY as the source, so a drag out of it fails on the client.
        /// A server that filled the list would leave items the player could neither see nor
        /// retrieve. The feature was abandoned before this build.
        ///
        /// This server has no overflow inventory. A full pack is handled where the item comes
        /// from: a harvest is refused with "Your inventory is full", crafting keeps what did not fit
        /// on the job, and a purchase or buyback charges only for what fitted.
        ///
        /// The handler exists so that a client that does send it is not disconnected - an opcode
        /// with no handler fails the packet terminator check and closes the connection.
        /// </summary>
        [PacketHandler(GameOpcode.OverflowTransfer)]
        private void OverflowTransfer(OverflowTransferPacket packet)
        {
            Logger.WriteLog(LogType.Debug,
                $"{Client.Player?.Name} sent OverflowTransfer (destType {packet.DestType}, entity {packet.EntityId}, quantity {packet.Quantity}, slot {packet.Slot}); there is no overflow inventory, nothing moved.");
        }

        /// <summary>
        /// RequestReturnItemToInventory (349) and RequestPlaceObject (344): taking a placed
        /// decoration back into the inventory, and placing one. Player housing - apartment
        /// decorating - which was cut. Deliberately placeholders that move nothing and send
        /// nothing.
        ///
        /// client/augmentations/decoration.py is what is left of it:
        ///  - OnDesignateCurrentItem picks an inventory item to place, spawns a half-transparent
        ///    proxy of it on every free decoration socket its plug fits (sockets the map loader
        ///    records from any entity that has them, gamemap.py), posts
        ///    PM_DECORATION_SELECT_LOCATION or PM_DECORATION_NO_LOCATIONS_AVAILABLE, and switches to
        ///    the homedecoration input state;
        ///  - OnPlaceItemAtSelectedLocation, on a proxy, sends
        ///    <c>RequestPlaceObject((itemId, destEntityId or None, socketId))</c>. The proxies are
        ///    put on their sockets with the client's own Recv_WorldPlacementDescriptor, and the
        ///    placed item would have come back the same way (WorldPlacementDescriptorPacket);
        ///  - OnReturnItemToInventory, on a placed decoration, sends
        ///    <c>RequestReturnItemToInventory((entityId,))</c>;
        ///  - OnCancelPlacement posts PM_DECORATION_CANCELLED. Around it: apartment point and
        ///    directional lights (APARTMENT_LIGHT_GROUP), the DECORATION and DECORATIONPROXY target
        ///    categories (targeting.py refuses to target a decoration) and CUSTOMIZE_HUE_DECORATION
        ///    in the customization window.
        ///
        /// In the 1.16.5 client none of it can be reached:
        ///  - OnDesignateCurrentItem, OnPlaceItemAtSelectedLocation and OnReturnItemToInventory are
        ///    defined and never called - in the decompiled source, or the 970 shipped .pyo files in
        ///    trpython.zip, where only decoration.pyo has the names; tabula_rasa.exe and
        ///    Tabula_Rasa_UI_EXPORT.xml have none of them;
        ///  - client/inputstate/homedecoration.py only stores a cancel callback: it has no key or
        ///    mouse handlers, so there is no way to pick a location in it;
        ///  - no entity class in the client's data carries the DECORATION augmentation (36), so no
        ///    entity is a Decoration to have the methods.
        ///
        /// This server has no apartments or decorations, and the C++ server only listed the ids.
        ///
        /// The handlers exist so that a client that does send one is not disconnected - an opcode
        /// with no handler fails the packet terminator check and closes the connection.
        /// </summary>
        [PacketHandler(GameOpcode.RequestReturnItemToInventory)]
        private void RequestReturnItemToInventory(RequestReturnItemToInventoryPacket packet)
        {
            Logger.WriteLog(LogType.Debug,
                $"{Client.Player?.Name} sent RequestReturnItemToInventory (entity {packet.EntityId}); there are no decorations, nothing moved.");
        }

        /// <inheritdoc cref="RequestReturnItemToInventory"/>
        [PacketHandler(GameOpcode.RequestPlaceObject)]
        private void RequestPlaceObject(RequestPlaceObjectPacket packet)
        {
            Logger.WriteLog(LogType.Debug,
                $"{Client.Player?.Name} sent RequestPlaceObject (item {packet.ItemId}, destination {packet.DestEntityId}, socket {packet.SocketId}); there are no decorations, nothing placed.");
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

        [PacketHandler(GameOpcode.RequestBind)]
        private void RequestBind(RequestBindPacket packet)
        {
            ItemManager.Instance.RequestBind(Client, packet);
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

        [PacketHandler(GameOpcode.RequestJoinVoiceChannel)]
        private void RequestJoinVoiceChannel(RequestJoinVoiceChannelPacket packet)
        {
            PartyManager.Instance.RequestJoinVoiceChannel(Client);
        }

        [PacketHandler(GameOpcode.RequestLeaveVoiceChannel)]
        private void RequestLeaveVoiceChannel(RequestLeaveVoiceChannelPacket packet)
        {
            PartyManager.Instance.RequestLeaveVoiceChannel(Client);
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
