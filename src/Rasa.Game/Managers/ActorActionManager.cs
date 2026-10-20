using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
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

        /// <summary>
        /// Forgets every queued action of an actor that is leaving the world. An action fires
        /// after its wait time on the world loop, and looks its actor's client up when it does;
        /// a player who disconnected in the meantime is no longer in the client list, and a
        /// reload that found nobody used to take the whole server down with it. Every map is
        /// swept rather than the actor's own, since which map the actor thinks it is on is not
        /// always the one its actions were queued on.
        /// </summary>
        public void RemoveActor(Actor actor)
        {
            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
            {
                // A use they were partway through never gets its recovery, so its lock is let go
                // here - otherwise the object would show them using it to everyone still in range.
                foreach (var action in mapChannel.PerformRecovery)
                    if (action.Actor == actor)
                        DynamicObjectManager.Instance.ReleaseUseLock(action, true);

                mapChannel.PerformRecovery.RemoveAll(action => action.Actor == actor);
            }
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
                        // Off the list first, then performed.
                        //
                        // A recovery that threw used to leave its action where it was: the list
                        // is walked again on the very next tick, the same action is still first
                        // in line and still due, so it throws again - and again - while the
                        // exception escaping here abandons the rest of this map's worker and
                        // every map iterated after it. One action that cannot be performed cost
                        // the world its simulation from then until a restart.
                        //
                        // Nothing ever re-queues an action, so taking it off before performing it
                        // loses nothing, and a recovery that fails now costs only itself.
                        mapChannel.PerformRecovery.Remove(action);

                        // A stun called it off, and the clients have been told (CallOffForStun).
                        if (action.CalledOff)
                            continue;

                        try
                        {
                            PerformRecovery(mapChannel, action);
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLog(LogType.Error,
                                $"Recovery {action.ActionId}/{action.ActionArgId} for entity {action.Actor?.EntityId} on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A stun has landed on the player (GameEffectManager.Attach): an ability or a tool they
        /// are winding up is called off, as a creature's is (CreatureWindups.Interrupt). Their
        /// client does not do it itself: its StunEffect blocks a new windup, not the one under
        /// way, which would otherwise run to its end and land with the player stunned.
        ///
        /// Everyone who sees the player, their own client too, is sent ActionInterrupt, which
        /// cancels the action and floats "Interrupted" over them; the player's request is closed
        /// with a silent UserActionFailed, as an interrupt they asked for is. Nothing has been
        /// paid: an ability's costs and cooldown are taken when it lands, and a tool's shot was
        /// spent on the request, as a weapon's is. A Rushing Blow's charge stops where it has got
        /// to (ChargeWorker). The action is marked rather than taken off the queue, which may be
        /// being walked as the stun lands; the worker drops it unperformed on its next pass.
        ///
        /// Knockbacks and holds are stuns on the server, so they call a windup off too. Not
        /// called off: the four GM abilities the client marks noInterrupt (NoStunInterrupt).
        /// </summary>
        internal static void CallOffForStun(MapChannel mapChannel, Manifestation player)
        {
            if (mapChannel == null || player == null)
                return;

            Client client = null;

            foreach (var action in mapChannel.PerformRecovery)
            {
                if (action.Actor != player || !action.StunInterrupts || action.IsInrerrupted || action.Completed || action.CalledOff)
                    continue;

                action.IsInrerrupted = true;
                action.CalledOff = true;

                CellManager.Instance.CellCallMethod(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));

                client ??= mapChannel.ClientList.Find(c => c?.Player == player);
                ActorManager.ResolveInterruptedRequest(client, action.ActionId, action.ActionArgId);
            }
        }

        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            switch (action.ActionId)
            {
                case ActionId.Gesture:
                case ActionId.GestureWeapon:
                    GestureManager.Instance.PerformRecovery(mapChannel, action);
                    break;
                case ActionId.Unstick:
                    Unstick.Land(mapChannel, action);
                    break;
                case ActionId.UseObject:
                    // A claim of a control point that was interrupted has no recovery to show:
                    // those who were shown its windup are told it was interrupted instead
                    // (DynamicObjectManager.CaptureControlPointRecovery).
                    if (!DynamicObjectManager.Instance.IsInterruptedClaim(action))
                        CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

                    // The use is over, finished or not: the object is nobody's before whatever it
                    // does next (a control point changing hands) goes out.
                    DynamicObjectManager.Instance.ReleaseUseLock(action, false);

                    // A usable of the client's .map (MapUsables), whatever its arg: no object of
                    // the server's has its id.
                    if (MapUsables.IsUseOf(action))
                    {
                        MapUsables.UseRecovery(mapChannel, action);
                        break;
                    }

                    // A usable world object the server put down (PlacedUsables).
                    if (PlacedUsables.IsUseOf(action))
                    {
                        PlacedUsables.UseRecovery(mapChannel, action);
                        break;
                    }

                    // A lootable container the server put down (WorldContainers).
                    if (WorldContainers.IsUseOf(action))
                    {
                        WorldContainers.UseRecovery(mapChannel, action);
                        break;
                    }

                    // A logos shrine teaches its logos whatever arg its class is used with: two of
                    // them, classes 21214 (FEW) and 21414 (VICTORY), carry the footlocker's 1, and
                    // the arg alone sent theirs to the footlocker's recovery, which teaches nothing.
                    if (IsLogosShrine(mapChannel, action.SourceId))
                    {
                        DynamicObjectManager.Instance.LogosRecovery(mapChannel, action);
                        break;
                    }

                    switch (action.ActionArgId)
                    {
                        // A Hortimonculus plant is used with arg 1, as a footlocker is; the plant
                        // is named by the action's SourceId.
                        case AbilityManager.HortimonculusUseArgId when AbilityManager.IsHortimonculus(action.SourceId):
                            AbilityManager.Instance.UseHortimonculusRecovery(mapChannel, action);
                            break;
                        case DynamicObjectManager.FootlockerUseArgId:
                        case DynamicObjectManager.SurveyUseArgId:
                            DynamicObjectManager.Instance.FootlockerRecovery(mapChannel, action);
                            break;
                        case KraftwerksManager.UseObjectArgId:
                            KraftwerksManager.Instance.UseRecovery(mapChannel, action);
                            break;
                        case DynamicObjectManager.LogosUseArgId:
                            DynamicObjectManager.Instance.LogosRecovery(mapChannel, action);
                            break;
                        case DynamicObjectManager.ControlPointUseArgId:
                            DynamicObjectManager.Instance.CaptureControlPointRecovery(mapChannel, action);
                            break;
                        default:
                            Logger.WriteLog(LogType.Debug, $"PerformRecovery.UseObject: unsuported actionArgId {action.ActionArgId}");
                            break;
                    }
                    break;
                // Every tool action (ToolActionManager.ToolActions). A list of its own here left
                // out the Snowball Launcher's TOOL_NERFWEAPON (527), whose recovery then went to
                // AbilityManager, where no skill grants it: "cannot perform that action now".
                case var tool when ToolActionManager.ToolActions.Contains(tool):
                    ToolActionManager.Instance.PerformRecovery(mapChannel, action);
                    break;
                case ActionId.WeaponAttack:
                    Logger.WriteLog(LogType.Debug, $"PerformRecovery {action.ActionArgId} {action.ActionId} {action.Args}");
                    /*
                    PlayerManager.Instance.StartAutoFire(action.Client, 0D);
                    action.Client.CellCallMethod(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(action.ActionId, action.ActionArgId, new List<int> { 1 }));
                    */
                    break;
                case ActionId.CriticalDeathFinisher:
                    CritDeathManager.Instance.PerformRecovery(mapChannel, action);
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
                    // Anything in the action tables is an ability, resolved from its data. The
                    // lightning and sprint cases that used to sit here ran on hand-typed numbers.
                    if (AbilityManager.Instance.TryGetAction(action.ActionId, out _))
                        AbilityManager.Instance.PerformRecovery(mapChannel, action);
                    else
                        Logger.WriteLog(LogType.Error, $"PerformAction: unsuported {action.ActionId}");
                    break;
            };
        }

        private static bool IsLogosShrine(MapChannel mapChannel, ulong entityId)
        {
            foreach (var obj in mapChannel.DynamicObjects)
                if (obj.EntityId == entityId)
                    return obj is Logos;

            return false;
        }
    }
}
