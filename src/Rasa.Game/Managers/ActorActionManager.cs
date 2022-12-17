using System;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
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

        public bool HasActiveAction(Actor actor)
        {
            if (actor.CurrentAction == 0)
                return false;
            
            return true;
        }

        public void DoWork(MapChannel mapChannel, long delta)
        {
            if (mapChannel.PerformRecovery.Count > 0)
            {
                if (mapChannel.PerformRecovery.Count > 1)
                    Logger.WriteLog(LogType.Debug, $"PerformRecovery count = { mapChannel.PerformRecovery.Count}");

                // iterate backwards through list
                for (var i = mapChannel.PerformRecovery.Count - 1; i >= 0; i--)
                {
                    var action = mapChannel.PerformRecovery[i];

                    // skip if client is busy
                    if (HasActiveAction(action.Actor))
                        continue;

                    // if action is interrupted recover immediately
                    if (action.IsInrerrupted)
                        action.PassedTime = action.WaitTime;

                    action.PassedTime += delta;

                    if (action.WaitTime <= action.PassedTime)
                    {
                        // perform action
                        PerformRecovery(mapChannel, action);
                        // remove action
                        mapChannel.PerformRecovery.Remove(action);
                    }
                }
            }
        }

        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            switch (action.ActionId)
            {
                case ActionId.AaRecruitLightning:
                    MissileManager.Instance.MissileLaunch(mapChannel, action, new Random().Next(233, 311 + 1));
                    break;
                case ActionId.AaRecruitSprint:
                    GameEffectManager.Instance.AttachSprint(mapChannel, action.Actor, action.ActionArgId, 500);
                    break;
                case ActionId.UseObject:
                    CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                    switch (action.ActionArgId)
                    {
                        case 1:
                            DynamicObjectManager.Instance.FootlockerRecovery(mapChannel, action);
                            break;
                        case 6:
                            DynamicObjectManager.Instance.LogosRecovery(mapChannel, action);
                            break;
                        case 7:
                            DynamicObjectManager.Instance.CaptureControlPointRecovery(mapChannel, action);
                            break;
                        default:
                            Logger.WriteLog(LogType.Debug, $"PerformRecovery.UseObject: unsuported actionArgId {action.ActionArgId}");
                            break;
                    }
                    break;
                case ActionId.WeaponAttack:
                    Logger.WriteLog(LogType.Debug, $"PerformRecovery {action.ActionArgId} {action.ActionId} {action.Args}");
                    /*
                    PlayerManager.Instance.StartAutoFire(action.Client, 0D);
                    action.Client.CellCallMethod(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(action.ActionId, action.ActionArgId, new List<int> { 1 }));
                    */
                    break;
                case ActionId.WeaponDraw:
                    CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                    action.Actor.WeaponReady = true;
                    break;
                case ActionId.WeaponReload:
                    ManifestationManager.Instance.WeaponReload(action);
                    break;
                case ActionId.WeaponStow:
                    CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                    action.Actor.WeaponReady = false;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"PerformAction: unsuported {action.ActionId}");
                    break;
            };
        }
    }
}
