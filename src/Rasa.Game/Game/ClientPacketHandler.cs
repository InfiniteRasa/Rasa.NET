using System;

namespace Rasa.Game
{
    using Data;
    using Managers;
    using Packets;
    using Packets.Game.Client;
    using Packets.MapChannel.Client;    

    public class ClientPacketHandler
    {
        public Client Client { get; }

        public ClientPacketHandler(Client client)
        {
            Client = client;
        }
       
        #region CharacterSelection

        [PacketHandler(GameOpcode.RequestCharacterName)]
        private void RequestCharacterName(RequestCharacterNamePacket packet)
        {
            CharacterManager.Instance.RequestCharacterName(Client, packet.Gender);
        }
        
        [PacketHandler(GameOpcode.RequestFamilyName)]
        private void RequestFamilyName(RequestFamilyNamePacket packet)
        {
            CharacterManager.Instance.RequestFamilyName(Client);
        }

        [PacketHandler(GameOpcode.RequestCreateCharacterInSlot)]
        private void RequestCreateCharacterInSlot(RequestCreateCharacterInSlotPacket packet)
        {
            CharacterManager.Instance.RequestCreateCharacterInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestDeleteCharacterInSlot)]
        private void RequestDeleteCharacterInSlot(RequestDeleteCharacterInSlotPacket packet)
        {
            CharacterManager.Instance.RequestDeleteCharacterInSlot(Client, packet.SlotNum);
        }

        [PacketHandler(GameOpcode.RequestSwitchToCharacterInSlot)]
        private void RequestSwitchToCharacterInSlot(RequestSwitchToCharacterInSlotPacket packet)
        {
            CharacterManager.Instance.RequestSwitchToCharacterInSlot(Client, packet.SlotNum);
        }
        #endregion

        #region MapChannel

        [PacketHandler(GameOpcode.AcceptPartyInvitesChanged)]
        private void AcceptPartyInvitesChanged(AcceptPartyInvitesChangedPacket packet)
        {
            Console.WriteLine("ToDo 'AcceptPartyInvitesChanged'");
        }

        [PacketHandler(GameOpcode.AutoFireKeepAlive)]
        private void AutoFireKeepAlive(AutoFireKeepAlivePacket packet)
        {
            PlayerManager.AutoFireKeepAlive(Client, packet.KeepAliveDelay);
        }

        [PacketHandler(GameOpcode.CharacterLogout)]
        private void CharacterLogout(CharacterLogoutPacket packet)
        {
            MapChannelManager.Instance.CharacterLogout(Client);
        }

        [PacketHandler(GameOpcode.LevelSkills)]
        private void LevelSkills(LevelSkillsPacket packet)
        {
            PlayerManager.LevelSkills(Client, packet);
        }

        [PacketHandler(GameOpcode.MapLoaded)]
        private void MapLoaded(MapLoadedPacket packet)
        {
            MapChannelManager.Instance.MapLoaded(Client);
        }

        [PacketHandler(GameOpcode.Ping)]
        private void Ping(PingPacket packet)
        {
            MapChannelManager.Instance.Ping(Client);
        }

        [PacketHandler(GameOpcode.RadialChat)]
        private void RadialChat(RadialChatPacket packet)
        {
            MapChannelManager.Instance.RadialChat(Client, packet.TextMsg);
        }

        [PacketHandler(GameOpcode.RequestArmAbility)]
        private void RequestArmAbility(RequestArmAbilityPacket packet)
        {
            PlayerManager.RequestArmAbility(Client, packet.AbilityDrawerSlot);
        }

        [PacketHandler(GameOpcode.RequestArmWeapon)]
        private void RequestArmWeapon(RequestArmWeaponPacket packet)
        {
            PlayerManager.RequestArmWeapon(Client, packet.RequestedWeaponDrawerSlot);
        }

        [PacketHandler(GameOpcode.RequestEquipWeapon)]
        private void RequestEquipWeapon(RequestEquipWeaponPacket packet)
        {
            InventoryManager.RequestEquipWeapon(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestLogout)]
        private void RequestLogout(RequestLogoutPacket packet)
        {
            MapChannelManager.Instance.RequestLogout(Client);
        }

        [PacketHandler(GameOpcode.RequestSwapAbilitySlots)]
        private void RequestSwapAbilitySlots(RequestSwapAbilitySlotsPacket packet)
        {
            PlayerManager.RequestSwapAbilitySlots(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestTooltipForItemTemplateId)]
        private void RequestTooltipForItemTemplateId(RequestTooltipForItemTemplateIdPacket packet)
        {
            InventoryManager.RequestTooltipForItemTemplateId(Client, packet.ItemTemplateId);
        }

        [PacketHandler(GameOpcode.RequestSetAbilitySlot)]
        private void RequestSetAbilitySlot(RequestSetAbilitySlotPacket packet)
        {
            PlayerManager.RequestSetAbilitySlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestVisualCombatMode)]
        private void RequestVisualCombatMode(RequestVisualCombatModePacket packet)
        {
            PlayerManager.RequestVisualCombatMode(Client, packet.CombatMode);
        }

        [PacketHandler(GameOpcode.SetAutoLootThreshold)]
        private void SetAutoLootThreshold(SetAutoLootThresholdPacket packet)
        {
            Console.WriteLine("ToDo 'SetAutoLootThreshold'");
        }

        [PacketHandler(GameOpcode.StartAutoFire)]
        private void StartAutoFire(StartAutoFirePacket packet)
        {
            PlayerManager.StartAutoFire(Client, packet.RetryDelayMs);
        }

        [PacketHandler(GameOpcode.WeaponDrawerInventory_MoveItem)]
        private void WeaponDrawerInventory_MoveItem(WeaponDrawerInventory_MoveItemPacket packet)
        {
            InventoryManager.WeaponDrawerInventory_MoveItem(Client, packet);
        }
        #endregion
    }
}
