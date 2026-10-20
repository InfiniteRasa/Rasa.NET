using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Models;
    using Structures;

    /// <summary>
    /// /stuck: UNSTICK (280/1), client/actions/unstick.py. The client sends RequestUnstick and at
    /// once plays the action's windup - a second of stuck_loop (animation family 1245), the wave -
    /// and then waits for the server to resolve the request. Its data: [1000 ms windup, 1245,
    /// 1000 ms recovery, no animation, 0 range, 60 000 ms reuse, 0, start the reuse on perform].
    ///
    /// The player was moved the moment the request came in, which cut the wave off before it had
    /// begun, and the request was never resolved. Now it is an action like any other: everyone
    /// else is shown the windup, the player is moved when it has run (ActorActionManager), and the
    /// request is resolved with its recovery - or refused, or called off by an interrupt or a
    /// stun, with the client told either way. The reuse is held to on the server too, from the
    /// request: a second's windup and then the minute, never longer than the client's own, which
    /// starts at the end of the windup.
    ///
    /// Where they go is NavMeshManager.UnstickDestination: the nearest walkable point, unless that
    /// is mesh under the terrain with ground over it - inside a hill, where a knockback could leave
    /// a player, and from where the nearest walkable point was more of the same (moved 0.4 m, still
    /// inside the hill) - and then the ground over them.
    ///
    /// The position in the request is the client's, and is not used: a client is free to claim it
    /// is stuck anywhere, and acting on that would make this a teleport. A dead player is left
    /// alone: they have a respawn for that, and moving a corpse would take the body away from
    /// anyone about to revive it.
    /// </summary>
    public static class Unstick
    {
        public const ActionId Action = ActionId.Unstick;
        public const uint ArgId = 1;

        /// <summary>The windup the client plays: the server moves the player when it has run.</summary>
        public const int WindupMs = 1000;

        /// <summary>The reuse, after the windup.</summary>
        public const int ReuseMs = 60000;

        public static void Request(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (player == null)
                return;

            if (player.State == CharacterState.Dead
                || player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0)
            {
                ActorManager.RefuseRequest(client, Action, ArgId, PlayerMessage.PmCannotDoThatWhileDead);
                return;
            }

            var now = Environment.TickCount64;

            if (mapChannel == null || player.ActionReuseUntil.TryGetValue(Action, out var readyAt) && readyAt > now
                || mapChannel.PerformRecovery.Exists(a => a.Actor == player && a.ActionId == Action))
            {
                ActorManager.RefuseRequest(client, Action, ArgId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            player.ActionReuseUntil[Action] = now + WindupMs + ReuseMs;

            // Their own client is already playing it.
            SendToOthers(mapChannel, player, new PerformWindupPacket(PerformType.TwoArgs, Action, ArgId));

            mapChannel.PerformRecovery.Add(new ActionData(player, Action, ArgId, WindupMs) { StunInterrupts = true });
        }

        /// <summary>The windup has run (ActorActionManager.PerformRecovery): moved, and the request resolved.</summary>
        public static void Land(MapChannel mapChannel, ActionData action)
        {
            if (action.Actor is not Manifestation player)
                return;

            var client = GameEffectManager.ClientOf(mapChannel, player);

            if (action.IsInrerrupted)
            {
                Forgive(player);
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, Action, ArgId));
                ActorManager.ResolveInterruptedRequest(client, Action, ArgId);
                return;
            }

            if (player.State == CharacterState.Dead
                || player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0)
            {
                Refuse(mapChannel, client, player, PlayerMessage.PmCannotDoThatWhileDead);
                return;
            }

            var from = player.Position;
            var destination = NavMeshManager.UnstickDestination(mapChannel, from);

            if (destination == null)
            {
                // No navmesh for this map, or nothing walkable within reach. Moving them somewhere
                // arbitrary would be worse than saying so.
                Refuse(mapChannel, client, player, PlayerMessage.PmCannotPerformActionNow);
                Logger.WriteLog(LogType.Debug, $"Character {player.Id} used /stuck at {from} on map {mapChannel.MapInfo?.MapContextId}, where nothing walkable was found.");
                return;
            }

            // The request resolved, for them and for everyone who saw the windup.
            CellManager.Instance.CellCallMethod(mapChannel, player, new PerformRecoveryPacket(PerformType.TwoArgs, Action, ArgId));

            player.PlaceAt(destination.Value);

            if (client != null)
            {
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId,
                    new Movement(destination.Value, client.Movement?.ViewDirection ?? new Vector2(0f, 0f))), false);

                // The message the live game showed for this command.
                client.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmStuckBugReportSent, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            }

            Logger.WriteLog(LogType.Debug, $"Character {player.Id} unstuck from {from} to {destination.Value} ({Vector3.Distance(from, destination.Value):F1} m).");
        }

        private static void Refuse(MapChannel mapChannel, Client client, Manifestation player, PlayerMessage message)
        {
            Forgive(player);
            SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, Action, ArgId));
            ActorManager.RefuseRequest(client, Action, ArgId, message);
        }

        /// <summary>Nothing came of it: they may try again at once.</summary>
        private static void Forgive(Manifestation player) => player.ActionReuseUntil.Remove(Action);

        private static void SendToOthers(MapChannel mapChannel, Actor actor, PythonPacket packet)
        {
            foreach (var cell in CellManager.CellsIn(mapChannel, actor.Cells))
                foreach (var client in cell.ClientList)
                    if (client.Player != actor)
                        client.CallMethod(actor.EntityId, packet);
        }
    }
}
