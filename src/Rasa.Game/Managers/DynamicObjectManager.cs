using System.Collections.Generic;

namespace Rasa.Managers
{
    using Database.Tables.Character;
    using Packets.MapChannel.Server;
    using Packets;
    using Timer;
    using Structures;
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;
    using System;

    public class DynamicObjectManager
    {
        private static DynamicObjectManager _instance;
        private static readonly object InstanceLock = new object();
        public static DynamicObjectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new DynamicObjectManager();
                    }
                }

                return _instance;
            }
        }

        private DynamicObjectManager()
        {
        }

        public void ForceState(Client client, uint entityId, int state)
        {
            client.SendPacket(entityId, new ForceStatePacket(state));
        }

        public void RequestUseObjectPacket(Client client, RequestUseObjectPacket packet)
        {
            client.SendPacket((uint)packet.EntityId, new UsePacket(client.MapClient.Player.Actor.EntityId, 1, 0));
        }
    }
}