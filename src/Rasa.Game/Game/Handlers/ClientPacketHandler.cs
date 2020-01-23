namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.Game.Client;

    public partial class ClientPacketHandler
    {
        public Client Client { get; }

        public ClientPacketHandler(Client client)
        {
            Client = client;
        }

        [PacketHandler(GameOpcode.MapLoaded)]
        private void MapLoaded(MapLoadedPacket packet)
        {
            MapManager.Instance.MapLoaded(Client);
        }
    }
}
