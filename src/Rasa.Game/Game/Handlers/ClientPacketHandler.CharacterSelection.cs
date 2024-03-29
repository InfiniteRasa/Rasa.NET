﻿namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.Game.Client;

    public partial class ClientPacketHandler
    {
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

        [PacketHandler(GameOpcode.RequestCloneCharacterToSlot)]
        private void RequestCloneCharacterToSlot(RequestCloneCharacterToSlotPacket packet)
        {
            CharacterManager.Instance.RequestCloneCharacterToSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestCreateCharacterInSlot)]
        private void RequestCreateCharacterInSlot(RequestCreateCharacterInSlotPacket packet)
        {
            CharacterManager.Instance.RequestCreateCharacterInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestDeleteCharacterInSlot)]
        private void RequestDeleteCharacterInSlot(RequestDeleteCharacterInSlotPacket packet)
        {
            CharacterManager.Instance.RequestDeleteCharacterInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestSwitchToCharacterInSlot)]
        private void RequestSwitchToCharacterInSlot(RequestSwitchToCharacterInSlotPacket packet)
        {
            CharacterManager.Instance.RequestSwitchToCharacterInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.StoreUserClientInformation)]
        private void RequestStoreUserClientInformation(StoreUserClientInformationPacket packet)
        {
			// ToDo
        }
    }
}
