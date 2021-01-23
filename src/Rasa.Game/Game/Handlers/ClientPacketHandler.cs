namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.Game.Client;
    using Packets.Game.Server;

    public partial class ClientPacketHandler
    {
        private readonly ICharacterManager _characterManager;
        private readonly IMapChannelManager _mapChannelManager;
        private readonly IManifestationManager _manifestationManager;

        public Client Client { get; private set; }

        public ClientPacketHandler(
            ICharacterManager characterManager, 
            IMapChannelManager mapChannelManager,
            IManifestationManager manifestationManager)
        {
            _characterManager = characterManager;
            _mapChannelManager = mapChannelManager;
            _manifestationManager = manifestationManager;
        }

        public void RegisterClient(Client client)
        {
            Client = client;
        }

        [PacketHandler(GameOpcode.AcceptPartyInvitesChanged)]
        private void AcceptPartyInvitesChanged(AcceptPartyInvitesChangedPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo AcceptPartyInvitesChanged");
        }

        [PacketHandler(GameOpcode.MapLoaded)]
        private void MapLoaded(MapLoadedPacket packet)
        {
            _mapChannelManager.MapLoaded(Client);
        }

        [PacketHandler(GameOpcode.Ping)]
        private void Ping(PingPacket packet)
        {
            Client.CallMethod(SysEntity.ClientMethodId, new AckPingPacket(packet.Ping));
        }

        [PacketHandler(GameOpcode.RequestVisualCombatMode)]
        private void RequestVisualCombatMode(RequestVisualCombatModePacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo RequestVisualCombatMode");
        }

        [PacketHandler(GameOpcode.SetAutoLootThreshold)]
        private void SetAutoLootThreshold(SetAutoLootThresholdPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo SetAutoLootThreshold");
        }

        [PacketHandler(GameOpcode.RequestLogout)]
        private void RequestLogout(RequestLogoutPacket packet)
        {
            Client.CallMethod(SysEntity.ClientMethodId, new LogoutTimeRemainingPacket());
        }

        [PacketHandler(GameOpcode.CharacterLogout)]
        private void CharacterLogout(CharacterLogoutPacket packet)
        {
            _mapChannelManager.CharacterLogout(Client);
            _characterManager.StartCharacterSelection(Client);
        }
    }
}
