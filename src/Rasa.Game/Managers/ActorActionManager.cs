using System.Collections.Generic;

namespace Rasa.Managers
{
    using Database.Tables.Character;
    using Game;
    using Packets.MapChannel.Server;
    using Packets;
    using Timer;
    using Structures;

    public class ActorActionManager
    {
        private static ActorActionManager _instance;
        private static readonly object InstanceLock = new object();
        public readonly Timer Timer = new Timer();
        public List<ActionsData> QueuedActions { get; set; } = new List<ActionsData>();
        public static ActorActionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ActorActionManager();
                    }
                }

                return _instance;
            }
        }

        private ActorActionManager()
        {
        }

        public void RequestWeaponReload(Client client, Actor actor, Item weapon, int foundAmmo)
        {
            weapon.CurrentAmmo = foundAmmo;
            // update ammo in database
            ItemsTable.UpdateItemCurrentAmmo(weapon.ItemId, foundAmmo);
            // send action complete packet
            QueuedActions.Add(new ActionsData
            {
                Client = client,
                EntityId = actor.EntityId,
                Packet = new PerformRecoveryPacket(134, 1, new List<int>{foundAmmo}),
                WaitTime = weapon.ItemTemplate.WeaponInfo.ReloadTime
            });
        }

        public void DoWork(long delta)
        {
            foreach (var action in QueuedActions)
            {
                action.PassedTime += delta;
                if (action.WaitTime <= action.PassedTime)
                {
                    action.Client.CellSendPacket(action.Client, action.EntityId, action.Packet);
                    QueuedActions.Remove(action);
                    break;
                }
            }
        }

        public class ActionsData
        {
            public Client Client { get; set; }
            public uint EntityId { get; set; }
            public PythonPacket Packet { get; set; }
            public long WaitTime { get; set; }
            public long PassedTime { get; set; }
        }
    }
}
