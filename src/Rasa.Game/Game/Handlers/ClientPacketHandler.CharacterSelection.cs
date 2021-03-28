namespace Rasa.Game.Handlers
{
    using Data;
    using Packets;
    using Packets.Game.Client;

    public partial class ClientPacketHandler
    {
        [PacketHandler(GameOpcode.RequestCharacterName)]
        private void RequestCharacterName(RequestCharacterNamePacket packet)
        {
            _characterManager.RequestCharacterName(Client, packet.Gender);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        [PacketHandler(GameOpcode.RequestFamilyName)]
        private void RequestFamilyName(RequestFamilyNamePacket packet)
        {
            _characterManager.RequestFamilyName(Client);
        }
#pragma warning restore IDE0060 // Remove unused parameter

        [PacketHandler(GameOpcode.RequestCreateCharacterInSlot)]
        private void RequestCreateCharacterInSlot(RequestCreateCharacterInSlotPacket packet)
        {
            _characterManager.RequestCreateCharacterInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestDeleteCharacterInSlot)]
        private void RequestDeleteCharacterInSlot(RequestDeleteCharacterInSlotPacket packet)
        {
            _characterManager.RequestDeleteCharacterInSlot(Client, packet);
        }

        [PacketHandler(GameOpcode.RequestSwitchToCharacterInSlot)]
        private void RequestSwitchToCharacterInSlot(RequestSwitchToCharacterInSlotPacket packet)
        {
            _characterManager.RequestSwitchToCharacterInSlot(Client, packet);
        }

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static
        [PacketHandler(GameOpcode.StoreUserClientInformation)]
        private void RequestStoreUserClientInformation(StoreUserClientInformationPacket packet)

        {
        }
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
