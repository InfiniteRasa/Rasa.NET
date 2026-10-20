using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Models;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;

    /// <summary>
    /// The abilities that put their user back where they marked - Tactical Evasion's Tactical
    /// Retreat (P5) and Self Destruct - shown as the client's teleports of their own types
    /// (teleportertype.teleporterFX): TACTICAL_EVASION (7) vanishes in
    /// vfx_ability_reinforcements_death_v01.pkg and arrives in ..._birth_v01.pkg, SELF_DESTRUCT (5)
    /// arrives in vfx_ability_selfdestruct_return_v01.pkg. Until now the player was moved with a
    /// MoveObject to their own client and nothing else: no effect, and everyone else saw them
    /// where they had been until their next step.
    ///
    /// As local travel does it (DynamicObjectManager.StartLocalTravel and DepartLocalTravel):
    /// PreTeleport(type) to everyone who can see them where they stand, which plays the type's
    /// pre effect (actor.py Recv_PreTeleport) and tells the client the type its Teleport's
    /// departure effect goes by - without it Recv_PostTeleport plays the default teleporter's;
    /// then the move, to the player's own client and to everyone around; then Teleport(home, yaw,
    /// type, TeleportDelayMs), which plays the arrival effect on the body once it is there
    /// (Recv_Teleport, _TelportMovementCompleted, _PlayTeleportArrivalFX). The Teleport goes only
    /// to those who were sent the PreTeleport and can still see the player, and to the player:
    /// one who only meets them at the far end has them created there, and a Teleport with no
    /// PreTeleport before it would play the default departure effect on them. No BeginTeleport
    /// is sent, so the client queues no TeleportAcknowledge and none is waited for; the player
    /// keeps their state.
    ///
    /// The retreat waits <see cref="RetreatHoldMs"/> between the two, so its vanishing effect
    /// plays where they are: ours, as the waypoints' hold is (WaypointHoldMs).
    /// </summary>
    public partial class AbilityManager
    {
        /// <summary>How long Tactical Retreat's vanishing effect plays before the Ranger goes.</summary>
        internal const int RetreatHoldMs = 1000;

        private sealed class PendingReturn
        {
            public MapChannel MapChannel;
            public Client Client;
            public Manifestation Player;
            public Vector3 Home;
            public TeleportType Type;
            public List<Client> Watchers;
            public long DepartAt;
        }

        private static readonly List<PendingReturn> PendingReturns = new List<PendingReturn>();
        private static readonly object PendingReturnsLock = new object();

        /// <summary>
        /// The player is sent back to <paramref name="home"/> as a teleport of
        /// <paramref name="type"/>: the pre effect now, and the move and the arrival now or, with
        /// a hold, from <see cref="ReturnWorker"/> once it is up.
        /// </summary>
        internal static void SendBack(MapChannel mapChannel, Client client, Vector3 home, TeleportType type, int holdMs = 0)
        {
            var player = client?.Player;

            if (mapChannel == null || player == null)
                return;

            var watchers = CellManager.Instance.GetClientsInCells(mapChannel, player.Cells);

            foreach (var watcher in watchers)
                watcher.CallMethod(player.EntityId, new PreTeleportPacket(type));

            if (holdMs <= 0)
            {
                Arrive(mapChannel, client, home, type, watchers);
                return;
            }

            lock (PendingReturnsLock)
                PendingReturns.Add(new PendingReturn
                {
                    MapChannel = mapChannel,
                    Client = client,
                    Player = player,
                    Home = home,
                    Type = type,
                    Watchers = watchers,
                    DepartAt = Environment.TickCount64 + holdMs
                });
        }

        /// <summary>
        /// The returns on this map whose hold is up. A player who died, left the map or the game
        /// in the meantime is not moved, and those who saw the effect start are told it failed
        /// (Recv_TeleportFailed stops it).
        /// </summary>
        internal void ReturnWorker(MapChannel mapChannel)
        {
            List<PendingReturn> due;
            var now = Environment.TickCount64;

            lock (PendingReturnsLock)
            {
                due = PendingReturns.Where(r => r.MapChannel == mapChannel && now >= r.DepartAt).ToList();

                foreach (var pending in due)
                    PendingReturns.Remove(pending);
            }

            foreach (var pending in due)
            {
                var client = pending.Client;
                var player = pending.Player;

                if (ReferenceEquals(client.Player, player) && player.MapChannel == mapChannel && client.State == ClientState.Ingame &&
                    player.State != CharacterState.Dead && player.State != CharacterState.Dying)
                {
                    Arrive(mapChannel, client, pending.Home, pending.Type, pending.Watchers);
                    continue;
                }

                foreach (var watcher in pending.Watchers.Where(w => w.Player?.MapChannel == mapChannel))
                    watcher.CallMethod(player.EntityId, new TeleportFailedPacket());
            }
        }

        /// <summary>Whether a return is waiting on this player's hold (tests, and a second retreat).</summary>
        internal static bool IsReturning(Manifestation player)
        {
            lock (PendingReturnsLock)
                return PendingReturns.Any(r => r.Player == player);
        }

        private static void Arrive(MapChannel mapChannel, Client client, Vector3 home, TeleportType type, List<Client> sawItStart)
        {
            var player = client.Player;
            var movement = new Movement(home, client.Movement.ViewDirection);

            player.PlaceAt(home);
            client.Movement = movement;
            CellManager.Instance.UpdateVisibility(client);

            // The body first, to them and to everyone around, so the arrival plays where it lands.
            client.MoveObject(player.EntityId, movement);
            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), true);

            var teleport = new TeleportPacket(home, player.Rotation, type, DynamicObjectManager.TeleportDelayMs);

            client.CallMethod(player.EntityId, teleport);

            foreach (var watcher in CellManager.Instance.GetClientsInCells(mapChannel, player.Cells, client))
                if (sawItStart.Contains(watcher))
                    watcher.CallMethod(player.EntityId, teleport);
        }
    }
}
