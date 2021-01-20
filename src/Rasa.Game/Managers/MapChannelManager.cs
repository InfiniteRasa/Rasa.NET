namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Structures;
    using Timer;

    public class MapChannelManager : IMapChannelManager
    {
        private readonly Timer Timer = new Timer();

        public void MapLoaded(Client client)
        {
            var player = client.Player;
            var map = GetMap(player.MapContextId);

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(10001, (EntityClass)player.Gender == 0 ? (EntityClass)692 : (EntityClass)691));
            client.CallMethod(10001, new WorldLocationDescriptorPacket(player.GetPositionVector(), (float)player.Rotation));
            client.CallMethod(SysEntity.ClientMethodId, new SetCurrentContextIdPacket(map.MapInfo.ContextId));
            client.CallMethod(SysEntity.ClientMethodId, new SetControlledActorIdPacket(10001));
            client.CallMethod(player.EntityId, new IsRunningPacket(player.IsRunning));

            client.State = ClientState.Ingame;
        }

        public void PassClientToMap(Client client)
        {
            var player = client.MapClient.Player;
            var map = GetMap(player.MapContextId);

            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
            client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket
               (
                   map.MapInfo.ContextId,
                   map.InstanceId,
                   map.MapInfo.Version,
                    player.GetPositionVector(),
                   (float)player.Rotation
               ));

            client.State = ClientState.Loading;
        }

        private static Map GetMap(uint contextId)
        {
            // TODO support additional maps
            var map = new Map
            {
                MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0)
            };
            return map;
        }
    }
}
