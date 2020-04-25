using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets;
    using Packets.Game.Server;
    using Structures;
    using Timer;

    public class MapManager
    {

        private static MapManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly Timer Timer = new Timer();

        public static MapManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapManager();
                    }
                }

                return _instance;
            }
        }

        private MapManager()
        {
        }

        internal void MapLoaded(Client client)
        {
            var player = client.MapClient.Player;

            // default enetiyId, until we implement EntityManager
            var entityId = 10001u;

            var entityData = new List<PythonPacket>
            {
                new WorldLocationDescriptorPacket(new Vector3((float)player.CoordX, (float)player.CoordY, (float)player.CoordZ), (float)player.Rotation),
                new AppearanceDataPacket(CharacterAppearanceTable.GetAppearances(player.Id))
            };

            var map = new Map
            {
                MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0)
            };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(entityId, (EntityClass)player.Gender == 0 ? (EntityClass)692 : (EntityClass)691, entityData));
            client.CallMethod(SysEntity.ClientMethodId, new SetCurrentContextIdPacket(map.MapInfo.ContextId));
            client.CallMethod(SysEntity.ClientMethodId, new SetControlledActorIdPacket(entityId));

            client.State = ClientState.Ingame;
        }

        internal void PassClientToMap(Client client)
        {
            var map = new Map
            {
                MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0)
            };

            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
            client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket
               (
                   map.MapInfo.ContextId,
                   map.InstanceId,
                   map.MapInfo.Version,
                   new Vector3((float)client.MapClient.Player.CoordX, (float)client.MapClient.Player.CoordY, (float)client.MapClient.Player.CoordZ),
                   (float)client.MapClient.Player.Rotation
               ));

            client.State = ClientState.Loading;
        }

        internal void Ping(Client client, double ping)
        {
            client.CallMethod(SysEntity.ClientMethodId, new AckPingPacket(ping));
        }
    }
}
