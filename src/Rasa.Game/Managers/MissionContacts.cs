using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The NPC a finished mission is handed in to, on the map and the radar from anywhere on its
    /// map.
    ///
    /// The client draws it itself, from the NPC's own entity. An NPC whose conversation status is
    /// MissionComplete or Reward (Recv_NPCConversationStatus, the missions as its data) gets a
    /// mission marker on the map where it stands, named for it and its missions
    /// (mapwindow.py HandleUpdateOverheadIndicator, kept on it by Update), and on the radar a
    /// pip, or an arrow at the rim pointing to it when it is out of the radar's range
    /// (radarwindow.py _UpdateWidgets) - each shown while one of those missions is tracked.
    /// Nothing else in the client can put it there: an objective's indicators stop being drawn
    /// when the objective is completed, and a finished mission has no objective left to carry
    /// one.
    ///
    /// But only an entity the client holds is drawn, and a client is given the creatures of the
    /// five by five cells around its player: 64 m at the most. Further off than that there was
    /// nothing to draw, so the marker appeared once the player had already found their way
    /// back.
    ///
    /// So the receiver of a mission a player can hand in is given to that player's client
    /// wherever it stands on their map, and kept there:
    ///  - <see cref="Sync"/> gives the client each one it does not hold and takes away the ones
    ///    it was given that are no longer one - the mission handed in or abandoned, the NPC dead
    ///    or no longer to be spoken to. Run when a mission's progress has changed the NPCs'
    ///    statuses (MissionApplication.RefreshNpcConversationStatuses), and every few seconds
    ///    (<see cref="Worker"/>) for what no mission event says: the player arriving on a map,
    ///    the NPC respawning or dying.
    ///  - a receiver the player walks away from stays on their client rather than going with
    ///    its cells (<see cref="Keep(Client, List{Creature})"/>), so the marker does not blink
    ///    out at 64 m and come back.
    ///  - one taken out of the world is taken off every client that was given it
    ///    (<see cref="Removed"/>): no cell would tell them.
    /// Walking back into its cells gives it again as any creature is; the client takes a
    /// creature it already holds as an update.
    ///
    /// What a client is given from afar is not kept up to date as a creature in its cells is:
    /// it is not told of the NPC moving or changing until it is back in range or the next
    /// <see cref="Sync"/> takes it away. A mission's receiver stands where its pool put it.
    /// </summary>
    public static class MissionContacts
    {
        /// <summary>How often each client's contacts are checked without a mission event asking.</summary>
        public const long SyncIntervalMs = 3000;

        /// <summary>
        /// Whether this creature is where one of the player's missions is handed in now: an NPC
        /// that can be spoken to, alive, and the receiver of a mission MissionApplication says
        /// can be handed in (<paramref name="receivers"/>, TurnInReceivers).
        /// </summary>
        private static bool IsContact(Creature creature, HashSet<uint> receivers) =>
            creature?.Npc != null && creature.IsInteractable && creature.State != CharacterState.Dead
            && receivers.Contains(creature.DbId);

        private static bool InWorld(Client client) =>
            client?.Player?.MapChannel != null && client.State == ClientState.Ingame && CellManager.Instance.IsInWorld(client);

        /// <summary>Whether the creature's cell is one of the cells around the player: the client is given it anyway.</summary>
        private static bool InView(Manifestation player, Creature creature)
        {
            if (player.Cells == null || creature.Cells == null)
                return false;

            var cell = creature.Cells[2, 2];

            foreach (var seen in player.Cells)
                if (seen == cell)
                    return true;

            return false;
        }

        /// <summary>
        /// Brings what the client has been given from afar into line with the player's missions:
        /// takes away what is no longer a contact, forgets what has come into view or left the
        /// world, and gives each contact on the map the client does not hold.
        /// </summary>
        public static void Sync(Client client)
        {
            if (!InWorld(client))
                return;

            var player = client.Player;
            var map = player.MapChannel;
            var receivers = MissionApplication.Instance?.TurnInReceivers(player) ?? new HashSet<uint>();

            foreach (var entityId in client.FarContacts.ToList())
            {
                // Gone from the world: Removed told the client. In view: its cells have it now.
                if (!MapInstanceScope.TryGetCreature(map, entityId, out var held) || InView(player, held))
                {
                    client.FarContacts.Remove(entityId);
                    continue;
                }

                if (IsContact(held, receivers))
                    continue;

                client.FarContacts.Remove(entityId);
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
            }

            if (receivers.Count == 0)
                return;

            foreach (var creature in map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList())
            {
                if (!IsContact(creature, receivers) || InView(player, creature) || client.FarContacts.Contains(creature.EntityId))
                    continue;

                // The creature as any client is given it, its conversation status with it
                // (CreateCreatureOnClient): MissionComplete and the missions, which is the marker.
                client.FarContacts.Add(creature.EntityId);
                CreatureManager.Instance.CreateCreatureOnClient(client, creature);
            }
        }

        /// <summary>The map's turn: every client on it whose check has come due.</summary>
        public static void Worker(MapChannel mapChannel, long now)
        {
            foreach (var client in mapChannel.ClientList.ToArray())
            {
                if (client?.Player?.MapChannel != mapChannel || now < client.NextContactSync)
                    continue;

                client.NextContactSync = now + SyncIntervalMs;
                Sync(client);
            }
        }

        /// <summary>
        /// The creatures of the cells a player has just left, less the ones their client keeps:
        /// a mission's receiver stays, held from afar from here on.
        /// </summary>
        public static List<Creature> Keep(Client client, List<Creature> leaving)
        {
            if (!InWorld(client) || !leaving.Any(creature => creature?.Npc != null))
                return leaving;

            var receivers = MissionApplication.Instance?.TurnInReceivers(client.Player);

            if (receivers == null || receivers.Count == 0)
                return leaving;

            var going = new List<Creature>(leaving.Count);

            foreach (var creature in leaving)
            {
                if (IsContact(creature, receivers))
                    client.FarContacts.Add(creature.EntityId);
                else
                    going.Add(creature);
            }

            return going;
        }

        /// <summary>Whether the client keeps a creature that has walked out of its player's cells: it does if it is a contact.</summary>
        public static bool Keep(Client client, Creature creature)
        {
            if (creature?.Npc == null || !InWorld(client))
                return false;

            var receivers = MissionApplication.Instance?.TurnInReceivers(client.Player);

            if (receivers == null || !IsContact(creature, receivers))
                return false;

            client.FarContacts.Add(creature.EntityId);
            return true;
        }

        /// <summary>
        /// The creature is leaving the world (CellManager.RemoveCreatureFromWorld), and the
        /// clients in its cells have been told: so are the ones that were given it from afar.
        /// </summary>
        public static void Removed(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature?.Npc == null)
                return;

            foreach (var client in mapChannel.ClientList.ToArray())
                if (client != null && client.FarContacts.Remove(creature.EntityId))
                    client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));
        }

        /// <summary>The client is off the map: it has dropped every entity of it, and is checked afresh on the next.</summary>
        public static void Forget(Client client)
        {
            if (client == null)
                return;

            client.FarContacts.Clear();
            client.NextContactSync = 0;
        }
    }
}
