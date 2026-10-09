using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The NPC a mission has the player go and speak to - the one a finished mission is handed in
    /// to, and the one an objective is talked through with - on the map and the radar from
    /// anywhere on its map.
    ///
    /// The client draws it itself, from the NPC's own entity. An NPC whose conversation status is
    /// MissionComplete, Reward, ObjectivComplete or ObjectivChoice (Recv_NPCConversationStatus,
    /// the missions as its data) gets a mission marker on the map where it stands, named for it
    /// and its missions (mapwindow.py HandleUpdateOverheadIndicator, kept on it by Update), and
    /// on the radar a pip, or an arrow at the rim pointing to it when it is out of the radar's
    /// range (radarwindow.py _UpdateWidgets) - each shown while one of those missions is tracked.
    /// Nothing else in the client can put it there: an objective's indicators stop being drawn
    /// when the objective is completed, and a finished mission has no objective left to carry
    /// one; and a talk-to objective has an indicator only where the content gives it one.
    ///
    /// But only an entity the client holds is drawn, and a client is given the creatures of the
    /// five by five cells around its player: 64 m at the most. Further off than that there was
    /// nothing to draw, so the marker appeared once the player had already found their way
    /// back.
    ///
    /// So the receiver of a mission a player can hand in, and the NPC an objective of theirs is
    /// to be talked through with now, are given to that player's client wherever they stand on
    /// their map, and kept there:
    ///  - <see cref="Sync"/> gives the client each one it does not hold and takes away the ones
    ///    it was given that are no longer one - the mission handed in or abandoned, the objective
    ///    done, the NPC dead or no longer to be spoken to. Run when a mission's progress has changed the NPCs'
    ///    statuses (MissionApplication.RefreshNpcConversationStatuses), and every few seconds
    ///    (<see cref="Worker"/>) for what no mission event says: the player arriving on a map,
    ///    the NPC respawning or dying.
    ///  - one the player walks away from stays on their client rather than going with
    ///    its cells (<see cref="Keep(Client, List{Creature})"/>), so the marker does not blink
    ///    out at 64 m and come back.
    ///  - one taken out of the world is taken off every client that was given it
    ///    (<see cref="Removed"/>): no cell would tell them.
    /// Walking back into its cells takes it off the client and gives it again as the cells
    /// give any creature (<see cref="Entering"/>): made anew, not updated.
    ///
    /// Of what a creature's cells are told, a client holding it from afar is told its moves
    /// (<see cref="Relay"/>), so the marker is where the NPC is; nothing else, until it is back
    /// in range or the next <see cref="Sync"/> takes it away.
    /// </summary>
    public static class MissionContacts
    {
        /// <summary>How often each client's contacts are checked without a mission event asking.</summary>
        public const long SyncIntervalMs = 3000;

        /// <summary>
        /// Who a player's missions send them to speak to now: the creature rows their finished
        /// missions are handed in to (MissionApplication.TurnInReceivers) and the NPC packages
        /// their objectives are talked through with (MissionApplication.ObjectiveContacts) - or,
        /// for a topic one creature alone speaks, that creature row.
        /// </summary>
        private readonly struct Wanted
        {
            private readonly HashSet<uint> _receivers;
            private readonly HashSet<uint> _packages;
            private readonly HashSet<uint> _speakers;

            public Wanted(Manifestation player)
            {
                var missions = MissionApplication.Instance;

                _speakers = new HashSet<uint>();
                _receivers = missions?.TurnInReceivers(player);
                _packages = missions?.ObjectiveContacts(player, _speakers);
            }

            public bool Any => _receivers?.Count > 0 || _packages?.Count > 0 || _speakers?.Count > 0;

            public bool Has(Creature creature) =>
                _receivers != null && _receivers.Contains(creature.DbId) ||
                _speakers != null && _speakers.Contains(creature.DbId) ||
                _packages != null && _packages.Contains(creature.Npc.NpcPackageId);
        }

        /// <summary>
        /// Whether this creature is where one of the player's missions sends them now: an NPC
        /// that can be spoken to, alive, and one of <paramref name="wanted"/>.
        /// </summary>
        private static bool IsContact(Creature creature, Wanted wanted) =>
            creature?.Npc != null && creature.IsInteractable && creature.State != CharacterState.Dead
            && wanted.Has(creature);

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
            var wanted = new Wanted(player);

            foreach (var entityId in client.FarContacts.ToList())
            {
                // Gone from the world: Removed told the client. In view: its cells have it now.
                if (!MapInstanceScope.TryGetCreature(map, entityId, out var held) || InView(player, held))
                {
                    client.FarContacts.Remove(entityId);
                    continue;
                }

                if (IsContact(held, wanted))
                    continue;

                client.FarContacts.Remove(entityId);
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
            }

            if (!wanted.Any)
                return;

            foreach (var creature in map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList())
            {
                if (!IsContact(creature, wanted) || InView(player, creature) || client.FarContacts.Contains(creature.EntityId))
                    continue;

                // The creature as any client is given it, its conversation status with it
                // (CreateCreatureOnClient): MissionComplete or ObjectivComplete and the missions,
                // which is the marker. Held from afar once it has been given: CreateCreatureOnClient
                // takes one already held off the client first (Entering).
                CreatureManager.Instance.CreateCreatureOnClient(client, creature);
                client.FarContacts.Add(creature.EntityId);
            }
        }

        /// <summary>
        /// The creature is about to be given to the client (CreatureManager.CreateCreatureOnClient):
        /// if the client holds it from afar, it is destroyed there first and is its cells' from
        /// here on, so that what follows makes it anew.
        ///
        /// A CreatePhysicalEntity for an entity the client holds is not a creation.
        /// clientmethod.py hands it to Recv_UpdatePhysicalEntity: the body is taken out of the
        /// world, the entity data applied again, and the body put back. The conversation status
        /// sent after it is the one the NPC already had, and overheadwindow.py's
        /// HandleUpdateOverheadIndicator returns at once on a status it is already showing - so
        /// the icon attached to the body when the NPC was given from afar is never attached
        /// again, whatever taking the body out of the world did to it. mapwindow.py makes its
        /// marker again on every status, which is how a hand-in NPC came to be marked on the map
        /// and to stand there with nothing over its head. Destroyed first, the NPC arrives as any
        /// NPC walking into view does, and gets its icon as a mission giver gets theirs.
        /// </summary>
        public static void Entering(Client client, Creature creature)
        {
            if (client == null || creature == null || !client.FarContacts.Remove(creature.EntityId))
                return;

            client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));
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
        /// an NPC a mission sends them to stays, held from afar from here on.
        /// </summary>
        public static List<Creature> Keep(Client client, List<Creature> leaving)
        {
            if (!InWorld(client) || !leaving.Any(creature => creature?.Npc != null))
                return leaving;

            var wanted = new Wanted(client.Player);

            if (!wanted.Any)
                return leaving;

            var going = new List<Creature>(leaving.Count);

            foreach (var creature in leaving)
            {
                if (IsContact(creature, wanted))
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

            if (!IsContact(creature, new Wanted(client.Player)))
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

        /// <summary>
        /// A creature's move that went to its cells (CellManager.CellMoveObject), to the clients
        /// that hold it from afar: the marker is drawn where the client has the NPC standing.
        /// </summary>
        public static void Relay(MapChannel mapChannel, Creature creature, Movement movement)
        {
            if (mapChannel == null || creature?.Npc == null)
                return;

            foreach (var client in mapChannel.ClientList)
                if (client?.Player != null && client.FarContacts.Contains(creature.EntityId) && !InView(client.Player, creature))
                    client.MoveObject(creature.EntityId, movement);
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
