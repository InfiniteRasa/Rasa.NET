namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.Game.Client;

    public partial class ClientPacketHandler
    {
        private readonly ICharacterManager _characterManager;

        public Client Client { get; }

        public ClientPacketHandler(Client client, ICharacterManager characterManager)
        {
            Client = client;
            _characterManager = characterManager;
        }

        [PacketHandler(GameOpcode.AcceptPartyInvitesChanged)]
        private void AcceptPartyInvitesChanged(AcceptPartyInvitesChangedPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo AcceptPartyInvitesChanged");
        }

        [PacketHandler(GameOpcode.MapLoaded)]
        private void MapLoaded(MapLoadedPacket packet)
        {
            MapManager.Instance.MapLoaded(Client);
        }

        [PacketHandler(GameOpcode.Ping)]
        private void Ping(PingPacket packet)
        {
            MapManager.Instance.Ping(Client, packet.Ping);
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
    }
}
