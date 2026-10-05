using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Protocol;
    using Structures;

    /// <summary>
    /// A player's squad, and on a battleground their team, on the map and the radar from anywhere
    /// on their map.
    ///
    /// The client draws them itself, from each member's own entity. The map window puts a marker
    /// where a squad member's or a team member's entity stands (mapwindow.py Update: for every
    /// party and team widget, GetEntity, and the widget is hidden when there is none), and the
    /// radar a pip, or an arrow at its rim pointing to them when they are beyond its range
    /// (radarwindow.py _UpdateWidgets: the rotate widget is for kRADARTYPE_PARTY and
    /// kRADARTYPE_TEAM). The squad window's rows are live while the entity is there too.
    ///
    /// But a client is given the players of the five by five cells around its own, 51 to 77 m
    /// out, and loses them past that. A squad spread over a base had no markers for one another,
    /// which is when they are wanted.
    ///
    /// So a player's allies - the members of their squad and, in a battleground's match, of their
    /// team - who are on their map channel are on their client wherever they stand:
    ///  - <see cref="Sync"/> gives a client each ally it does not hold and takes away the ones it
    ///    was given that are allies no longer; run for every client of a map once a second
    ///    (<see cref="Worker"/>), which is how a squad formed, left or joined from across the map
    ///    is caught up with.
    ///  - an ally walked away from stays, both ways, rather than going with the cells
    ///    (<see cref="KeptBy"/>, <see cref="KeepingFor"/>), and one walked back to is not made
    ///    again (<see cref="NotHeldBy"/>, <see cref="NotHolding"/>): the entity is the same one
    ///    throughout, so nothing blinks at the edge.
    ///  - what a player's cells are told of them, the clients that hold them from afar are told
    ///    as well (<see cref="Relay(Actor, PythonPacket)"/>, <see cref="Relay(Client, MoveObjectMessage)"/>):
    ///    their moves, and their health, effects and states, so the marker moves and the squad
    ///    window's bars are true.
    ///  - a player who leaves the map is taken off everyone who was given them (<see cref="Forget"/>).
    ///
    /// A cloaked player is hidden from everyone outside their squad (Detection), so a team mate
    /// who is not also a squad mate is not given while cloaked and is taken away when they cloak
    /// (<see cref="Hidden"/>).
    /// </summary>
    public static class FarAllies
    {
        /// <summary>How often each client's allies are looked at.</summary>
        public const long SyncIntervalMs = 1000;

        /// <summary>Every client of the map whose turn it is; from CellManager.DoWork.</summary>
        public static void Worker(MapChannel mapChannel, long now)
        {
            if (mapChannel?.ClientList == null)
                return;

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                if (client?.Player == null || client.Player.MapChannel != mapChannel || now < client.NextAllySync)
                    continue;

                client.NextAllySync = now + SyncIntervalMs;
                Sync(client);
            }
        }

        /// <summary>
        /// Brings what the client holds from afar in line with who its player's allies are now:
        /// gives the ones out of its cells it does not hold, takes away the ones it was given
        /// that are allies no longer, and lets go of the ones its cells have since taken over.
        /// </summary>
        public static void Sync(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return;

            var here = IsPresent(client, mapChannel);
            var teams = TeamsOn(mapChannel);

            foreach (var held in client.FarAllies.ToList())
            {
                if (!here || !IsPresent(held, mapChannel) || !AreAllies(client, held, teams) || Detection.IsHiddenFrom(held.Player, client))
                    Take(client, held);
                else if (InView(client, held))
                    Unlink(client, held);       // the cells have them now
            }

            if (!here)
                return;

            foreach (var other in mapChannel.ClientList)
            {
                if (other == client || client.FarAllies.Contains(other) || !IsPresent(other, mapChannel))
                    continue;

                if (!AreAllies(client, other, teams) || InView(client, other) || Detection.IsHiddenFrom(other.Player, client))
                    continue;

                Give(client, other);
            }
        }

        /// <summary>
        /// The players leaving the client's cells whose entities it is to lose: the others stay
        /// on it, held from afar from now on.
        /// </summary>
        internal static List<Client> KeptBy(Client client, List<Client> leaving)
        {
            return Keep(leaving, other => (client, other));
        }

        /// <summary>
        /// The players leaving the client's cells who are to lose the client's entity: the others
        /// keep it, held from afar from now on.
        /// </summary>
        internal static List<Client> KeepingFor(Client client, List<Client> leaving)
        {
            return Keep(leaving, other => (other, client));
        }

        private static List<Client> Keep(List<Client> leaving, System.Func<Client, (Client Holder, Client Held)> pair)
        {
            if (leaving == null || leaving.Count == 0)
                return leaving;

            var discard = new List<Client>(leaving.Count);
            Dictionary<Client, uint> teams = null;
            var looked = false;

            foreach (var other in leaving)
            {
                var (holder, held) = pair(other);
                var mapChannel = holder?.Player?.MapChannel;

                if (!looked)
                {
                    teams = TeamsOn(mapChannel);
                    looked = true;
                }

                if (IsPresent(holder, mapChannel) && IsPresent(held, mapChannel) && AreAllies(holder, held, teams) &&
                    !Detection.IsHiddenFrom(held.Player, holder))
                    Link(holder, held);
                else
                    discard.Add(other);
            }

            return discard;
        }

        /// <summary>The players entering the client's cells it has yet to be given: one held from afar is there already.</summary>
        internal static List<Client> NotHeldBy(Client client, List<Client> entering)
        {
            if (entering == null || client.FarAllies.Count == 0)
                return entering;

            var introduce = new List<Client>(entering.Count);

            foreach (var other in entering)
                if (!Unlink(client, other))
                    introduce.Add(other);

            return introduce;
        }

        /// <summary>The players entering the client's cells who have yet to be given the client: one holding it from afar has it already.</summary>
        internal static List<Client> NotHolding(Client client, List<Client> entering)
        {
            if (entering == null || client.Player == null || client.Player.FarWatchers.Count == 0)
                return entering;

            var introduce = new List<Client>(entering.Count);

            foreach (var other in entering)
                if (!Unlink(other, client))
                    introduce.Add(other);

            return introduce;
        }

        /// <summary>A method call on an actor that went to their cells, to the clients that hold them from afar.</summary>
        internal static void Relay(Actor origin, PythonPacket packet)
        {
            if (!(origin is Manifestation player) || player.FarWatchers.Count == 0)
                return;

            foreach (var watcher in player.FarWatchers)
                if (watcher.State == ClientState.Ingame)
                    watcher.CallMethod(player.EntityId, packet);
        }

        /// <summary>A player's move that went to their cells, to the clients that hold them from afar.</summary>
        internal static void Relay(Client client, MoveObjectMessage move)
        {
            var player = client?.Player;

            if (player == null || player.FarWatchers.Count == 0)
                return;

            foreach (var watcher in player.FarWatchers)
                if (watcher.State == ClientState.Ingame)
                    watcher.SendMessage(move, false, 1);
        }

        /// <summary>A player has cloaked: the clients that hold them from afar and are not of their squad lose them, as their cells' do.</summary>
        internal static void Hidden(Manifestation player)
        {
            if (player == null || player.FarWatchers.Count == 0)
                return;

            foreach (var watcher in player.FarWatchers.ToList())
            {
                if (!Detection.IsHiddenFrom(player, watcher))
                    continue;

                var held = watcher.FarAllies.FirstOrDefault(c => c.Player == player);

                if (held != null)
                    Take(watcher, held);
            }
        }

        /// <summary>
        /// The client is leaving its map: everyone who was given its player loses them, and it
        /// loses everyone it was given.
        /// </summary>
        internal static void Forget(Client client)
        {
            var player = client?.Player;

            if (player == null)
                return;

            foreach (var watcher in player.FarWatchers.ToList())
                Take(watcher, client);

            foreach (var held in client.FarAllies.ToList())
            {
                if (client.State == ClientState.Disconnected)
                    Unlink(client, held);
                else
                    Take(client, held);
            }
        }

        /// <summary>Whether the client holds the other's player from beyond its cells.</summary>
        public static bool Holds(Client client, Client other)
        {
            return client != null && other != null && client.FarAllies.Contains(other);
        }

        #region Rules

        /// <summary>Squad mates, or on the same team of a battleground's match.</summary>
        private static bool AreAllies(Client one, Client other, Dictionary<Client, uint> teams)
        {
            if (one?.Player == null || other?.Player == null || one == other)
                return false;

            if (Detection.SameSquad(one.Player, other.Player))
                return true;

            return teams != null && teams.TryGetValue(one, out var team) && team != 0 &&
                teams.TryGetValue(other, out var theirs) && theirs == team;
        }

        /// <summary>Each player's team in the match of this map channel; null when it has none.</summary>
        private static Dictionary<Client, uint> TeamsOn(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null || !Battlegrounds.Instance.IsBattleground(mapChannel.MapInfo.MapContextId))
                return null;

            var match = Battlegrounds.Instance.MatchOf(mapChannel);

            if (match == null || match.Members.Count == 0)
                return null;

            var teams = new Dictionary<Client, uint>();

            foreach (var member in match.Members)
                if (member.Client != null)
                    teams[member.Client] = member.Team;

            return teams;
        }

        /// <summary>In the world on this map channel, with a client that can be sent to.</summary>
        private static bool IsPresent(Client client, MapChannel mapChannel)
        {
            return client?.Player != null && mapChannel != null && client.State == ClientState.Ingame &&
                !client.Player.Disconected && client.Player.MapChannel == mapChannel && CellManager.Instance.IsInWorld(client);
        }

        /// <summary>Whether the other's player stands in one of the client's cells, where the cells give and take them.</summary>
        private static bool InView(Client client, Client other)
        {
            var mine = client.Player.Cells;
            var theirs = other.Player.Cells;

            if (mine == null || theirs == null)
                return false;

            var centre = theirs[2, 2];

            foreach (var cell in mine)
                if (cell == centre)
                    return true;

            return false;
        }

        #endregion

        #region Giving and taking

        private static void Give(Client holder, Client held)
        {
            Link(holder, held);
            ManifestationManager.Instance.CellIntroducePlayersToClient(holder, new List<Client> { held });
        }

        private static void Take(Client holder, Client held)
        {
            if (!Unlink(holder, held))
                return;

            if (holder.State != ClientState.Disconnected && held.Player != null)
                ManifestationManager.Instance.CellDiscardPlayersToClient(holder, new List<Client> { held });
        }

        private static void Link(Client holder, Client held)
        {
            if (!holder.FarAllies.Add(held))
                return;

            if (!held.Player.FarWatchers.Contains(holder))
                held.Player.FarWatchers.Add(holder);
        }

        private static bool Unlink(Client holder, Client held)
        {
            if (holder == null || held == null || !holder.FarAllies.Remove(held))
                return false;

            held.Player?.FarWatchers.Remove(holder);

            return true;
        }

        #endregion
    }
}
