using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
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

        public bool HasActiveAction(Client client)
        {
            if (client.MapClient.Player.Actor.CurrentAction == 0)
                return false;

            CommunicatorManager.Instance.SystemMessage(client, "Player is busy.");
            return true;
        }

        public void DoWork(MapChannel mapChannel, long delta)
        {
            if (mapChannel.PerformActions.Count > 0)
            {
                Logger.WriteLog(LogType.Debug, $"PerformActions count = { mapChannel.PerformActions.Count}");
                // iterate backwards through list
                for (var i = mapChannel.PerformActions.Count - 1; i >= 0; i--)
                {
                    var action = mapChannel.PerformActions[i];

                    // skip if client is busy
                    if (HasActiveAction(action.Client))
                        continue;

                    action.PassedTime += delta;

                    if (action.WaitTime <= action.PassedTime)
                    {
                        // perform action
                        PerformAction(action);
                        // remove action
                        mapChannel.PerformActions.Remove(action);
                    }
                }
            }
        }

        public void PerformAction(ActionData action)
        {
            switch (action.ActionId)
            {
                case ActionId.WeaponAttack:
                    PlayerManager.Instance.StartAutoFire(action.Client, 0D);
                    action.Client.CellSendPacket(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(action.ActionId, action.ActionArgId, new List<int> { 1 }));
                    break;
                case ActionId.WeaponStow:
                    action.Client.CellSendPacket(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(action.ActionId, action.ActionArgId));
                    action.Client.MapClient.Player.WeaponReady = false;
                    break;
                case ActionId.WeaponDraw:
                    action.Client.CellSendPacket(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(action.ActionId, action.ActionArgId));
                    action.Client.MapClient.Player.WeaponReady = true;
                    break;
                case ActionId.WeaponReload:
                    PlayerManager.Instance.WeaponReload(action);
                    break;
                case ActionId.AaRecruitLightning:
                    action.Client.CellSendPacket(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformWindupPacket(action.ActionId, action.ActionArgId));
                    break;
                case ActionId.AaRecruitSprint:
                    GameEffectManager.Instance.AttachSprint(action.Client, action.Client.MapClient.Player.Actor, action.ActionArgId, 500);
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"PerformAction: unsuported {action.ActionId}");
                    break;
            };
        }
    }
}
