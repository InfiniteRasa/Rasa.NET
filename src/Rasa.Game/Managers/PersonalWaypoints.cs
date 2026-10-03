using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Personal Waypoints: CONSUMABLE_PORTABLE_WAYPOINT (487, abilities.temporarywormhole),
    /// used from three items, a level each:
    ///  1. One-Way Personal Waypoint (118781): "A portable waypoint. Works only for the person
    ///     who deploys it. Allows one-way access to the local waypoint network. Duration is 5
    ///     minutes."
    ///  2. Two-Way Personal Waypoint (118782): "... Allows two-way access to the local waypoint
    ///     network. Duration is 5 minutes normally, but indefinite in Operations zones."
    ///  3. Two-Way Squad Waypoint (118783): "... Works for the entire squad of the person who
    ///     deploys it. Allows two-way access ..."
    /// The levels by the client's own names for them (shared/gameconstants.py):
    /// LEVEL_TWO_WAY_WORMHOLE_SELF 2 and LEVEL_TWO_WAY_WORMHOLE_PARTY 3. The action's one
    /// property is DURATION, 300 s; its windup is the level's 8 s, and it has no reuse.
    ///
    /// What is put down is UsableAbilityTemporaryWormhole (class 20272, "Portable Waypoint",
    /// vfx_ability_wormhole_orb.geo with ABILITY_TEMPORARY_WORMHOLE_EFFECT on it), a WORMHOLE
    /// usable - "an in-world proxy object that allows players to teleport to any waypoint
    /// teleporter on the map" (client/augmentations/wormhole.py). The action has no CLASS_ID
    /// property, so the class is given here. The class has nothing to show for the usable's
    /// states, so it stands in USE_WH_STATE_0 from the moment it is put down and is in service
    /// at once: the windup was the wait.
    ///
    /// One way: whoever may use it gets, within reach of it, the waypoint window with the
    /// waypoints they have gained on this map, and SelectWaypoint takes it for the departure
    /// station (<see cref="IsNearUsable"/>). Using it opens the window too. The window's name
    /// for where the player stands is waypointlanguage 420, "Portable Waypoint" - an id no
    /// waypoint in the world has, because the window greys out the row with that id.
    ///
    /// Two ways, for levels 2 and 3: a waypoint's window lists it (EnteredWaypoint's
    /// tempWormholes: its id, where it is and its owner's name, shown as "Temp Wormhole:name"
    /// under the map the player is on), and picking that row is the client's ReturnToWormhole
    /// with the id. The window keeps waypoints and wormholes in one list by id, so the id is
    /// the object's entity id: those begin at 1000, and the waypoints placed in the world end
    /// below that. Another Personal Waypoint's window lists it too.
    ///
    /// Whose: its owner's, and at level 3 their squad's, on the map it stands on. It is told
    /// to each client enabled or not as a Dropship Extraction Beacon's ship is
    /// (<see cref="DropshipBeacons"/>).
    ///
    /// How long: DURATION, or with no end for levels 2 and 3 on a map that is entered as a
    /// squad's instance - the Operations (<see cref="SquadInstancePolicies"/>). In either case
    /// it goes when its owner puts down another, leaves the map or leaves the game.
    ///
    /// Not here: the client lets a WORMHOLE usable be shown hostile and destroyed
    /// (Recv_TargetCategory, IsDestroyable). Nothing can harm one.
    /// </summary>
    public static class PersonalWaypoints
    {
        public const string Module = "abilities.temporarywormhole";

        /// <summary>UsableAbilityTemporaryWormhole.</summary>
        public const EntityClasses WaypointClass = (EntityClasses)20272;

        /// <summary>The client's LEVEL_TWO_WAY_WORMHOLE_SELF: from this level the waypoint can be returned to.</summary>
        public const uint TwoWayLevel = 2;

        /// <summary>The client's LEVEL_TWO_WAY_WORMHOLE_PARTY: from this level it is the squad's too.</summary>
        public const uint SquadLevel = 3;

        /// <summary>waypointlanguage 420, "Portable Waypoint": the window's name for where the player is. No waypoint has the id.</summary>
        public const uint WindowNameId = 420;

        /// <summary>How near it a player has to stand, as for a Dropship Extraction Beacon's ship; the client uses a usable from 6 m.</summary>
        public const float Reach = 5f;

        public const int DefaultDurationSeconds = 300;

        /// <summary>A Personal Waypoint a player may return to: what EnteredWaypoint lists.</summary>
        private sealed class Placed
        {
            public Manifestation Owner;
            public string OwnerName;
            public uint Level;
            public DynamicObject Object;

            /// <summary>long.MaxValue: no end.</summary>
            public long ExpiresAt;

            public readonly List<Client> WindowOpen = new List<Client>();

            /// <summary>The clients that have been told it is enabled.</summary>
            public readonly HashSet<Client> ToldUsable = new HashSet<Client>();
        }

        private static readonly ConditionalWeakTable<MapChannel, List<Placed>> Waypoints = new();

        /// <summary>The waypoints of the map's network a player has gained, for the window. Replaced in tests.</summary>
        internal static Func<Client, Dictionary<uint, MapWaypointInfoList>> Network =
            client => DynamicObjectManager.Instance.CreateListOfWaypoints(client, WaypointType.Waypoint);

        public static bool Is(ActionInfo action) => action?.Module == Module;

        /// <summary>
        /// Why a Personal Waypoint cannot be put down now, as the client's own check has it
        /// (temporarywormhole.py CheckAction): on a map with teams, by a player on none.
        /// </summary>
        public static PlayerMessage? Refusal(Manifestation player)
        {
            if (player?.MapChannel != null && Battlegrounds.Instance.IsBattleground(player.MapChannel.MapInfo.MapContextId)
                && Battlegrounds.Instance.TeamOf(player) == 0)
                return PlayerMessage.PmCannotPerformActionNow;

            return null;
        }

        /// <summary>Whether a Personal Waypoint of this level put down on this map has no end: a two-way one in an Operation.</summary>
        public static bool Stays(MapChannel mapChannel, uint level) =>
            level >= TwoWayLevel && mapChannel != null &&
            (mapChannel.IsSquadInstance || SquadInstancePolicies.IsSquadMap(mapChannel.MapInfo.MapContextId));

        /// <summary>Puts the player's Personal Waypoint down where they stand, in service. One of theirs already out on this map goes first.</summary>
        public static DynamicObject Deploy(MapChannel mapChannel, Manifestation owner, uint level, ActionLevelInfo info, long now = 0)
        {
            if (mapChannel == null || owner == null)
                return null;

            if (now == 0)
                now = Environment.TickCount64;

            var waypoints = Waypoints.GetValue(mapChannel, _ => new List<Placed>());
            List<Placed> theirs;

            lock (waypoints)
                theirs = waypoints.Where(placed => placed.Owner == owner).ToList();

            foreach (var placed in theirs)
                Remove(mapChannel, waypoints, placed);

            var obj = new DynamicObject
            {
                EntityClassId = WaypointClass,
                DynamicObjectType = DynamicObjectType.PersonalWaypoint,
                Position = NavMeshManager.SnapToGround(mapChannel, owner.Position),
                Rotation = owner.Rotation,
                MapContextId = owner.MapContextId,
                StateId = UseObjectState.WhState0,

                // In service on the server, so a use of it is looked at; the clients are told one
                // by one, whoever it is for yes and nobody else anything (SyncUsable).
                IsEnabled = true
            };

            var seconds = Math.Max(1, info?.Get(AbilityProperty.Duration, DefaultDurationSeconds) ?? DefaultDurationSeconds);
            var waypoint = new Placed
            {
                Owner = owner,
                OwnerName = $"{owner.Name} {owner.FamilyName}".Trim(),
                Level = level,
                Object = obj,
                ExpiresAt = Stays(mapChannel, level) ? long.MaxValue : now + seconds * 1000L
            };

            // Listed before the clients around are shown it, so that each is shown it as it is for them (ShowTo).
            lock (waypoints)
                waypoints.Add(waypoint);

            CellManager.Instance.AddToWorld(mapChannel, obj);

            return obj;
        }

        /// <summary>Whether the player may use it: its owner, or at level 3 their squad, alive and in the world on its map.</summary>
        public static bool MayUse(Client client, DynamicObject obj)
        {
            var placed = Find(obj);

            return placed != null && MayUse(client, placed);
        }

        private static bool MayUse(Client client, Placed placed)
        {
            var player = client?.Player;

            return player != null && client.State == ClientState.Ingame && client.PendingTransfer == null &&
                   player.State != CharacterState.Dead && player.State != CharacterState.Dying &&
                   placed.Object.RuntimeMapChannel != null && player.MapChannel == placed.Object.RuntimeMapChannel &&
                   IsFor(player, placed);
        }

        private static bool IsFor(Manifestation player, Placed placed) =>
            player == placed.Owner || placed.Level >= SquadLevel && Detection.SameSquad(placed.Owner, player);

        /// <summary>Whether it goes to this client enabled: theirs, or their squad's. For the UsableInfo of a client meeting it, and remembered.</summary>
        public static bool ShowTo(Client client, DynamicObject obj)
        {
            var placed = Find(obj);

            if (placed == null || client?.Player == null || !IsFor(client.Player, placed))
                return false;

            lock (placed.ToldUsable)
                placed.ToldUsable.Add(client);

            return true;
        }

        /// <summary>
        /// A Personal Waypoint the player may use within reach, on this map: a departure station
        /// for SelectWaypoint and ReturnToWormhole. <paramref name="except"/> is the one they
        /// are going to, which is no way to itself.
        /// </summary>
        public static bool IsNearUsable(Client client, MapChannel mapChannel, ulong except = 0)
        {
            if (client?.Player == null || mapChannel == null || !Waypoints.TryGetValue(mapChannel, out var waypoints))
                return false;

            lock (waypoints)
                return waypoints.Any(placed => placed.Object.EntityId != except && MayUse(client, placed) && InReach(client, placed));
        }

        /// <summary>
        /// The Personal Waypoints on this map the player may return to, for a waypoint window:
        /// the two-way ones that are theirs or their squad's. Null when there are none, which is
        /// what EnteredWaypoint sends then. <paramref name="except"/> is the one whose window it is.
        /// </summary>
        public static List<TempWormhole> ReturnPoints(Client client, MapChannel mapChannel, ulong except = 0)
        {
            if (client?.Player == null || mapChannel == null || !Waypoints.TryGetValue(mapChannel, out var waypoints))
                return null;

            List<TempWormhole> points;

            lock (waypoints)
                points = waypoints
                    .Where(placed => placed.Object.EntityId != except && placed.Level >= TwoWayLevel && MayUse(client, placed))
                    .Select(placed => new TempWormhole(placed.Object.EntityId, placed.Object.Position, placed.OwnerName))
                    .ToList();

            return points.Count == 0 ? null : points;
        }

        /// <summary>Where a Personal Waypoint the player may return to stands: the one ReturnToWormhole names.</summary>
        public static bool TryGetReturnPoint(Client client, MapChannel mapChannel, ulong id, out Vector3 position, out double rotation)
        {
            position = default;
            rotation = 0;

            if (id == 0 || client?.Player == null || mapChannel == null || !Waypoints.TryGetValue(mapChannel, out var waypoints))
                return false;

            Placed found;

            lock (waypoints)
                found = waypoints.FirstOrDefault(placed => placed.Object.EntityId == id);

            if (found == null || found.Level < TwoWayLevel || !MayUse(client, found))
                return false;

            position = found.Object.Position;
            rotation = found.Object.Rotation;

            return true;
        }

        /// <summary>It is used: the request is closed, and the waypoint window opened for someone who may use it.</summary>
        public static void Use(Client client, DynamicObject obj, RequestUseObjectPacket packet)
        {
            var placed = Find(obj);

            if (placed == null || !MayUse(client, placed) || !InReach(client, placed))
            {
                ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, PlayerMessage.PmUseObjectNotUsable);
                return;
            }

            // Nothing is performed on it: the use is answered, and the window is what it does.
            ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, null);

            lock (placed.WindowOpen)
                if (!placed.WindowOpen.Contains(client))
                    placed.WindowOpen.Add(client);

            OpenWindow(client, placed);
        }

        /// <summary>
        /// The Personal Waypoints on a map: taken away when their time is up or their owner has
        /// gone, the clients around told whose they are, and the waypoint window opened for
        /// those who come within reach and closed for those who leave it.
        /// <paramref name="now"/> is for tests; 0 is the clock.
        /// </summary>
        public static void Worker(MapChannel mapChannel, long now = 0)
        {
            if (mapChannel == null || !Waypoints.TryGetValue(mapChannel, out var waypoints))
                return;

            if (now == 0)
                now = Environment.TickCount64;

            List<Placed> all;

            lock (waypoints)
                all = waypoints.ToList();

            foreach (var placed in all)
            {
                // Its time is up, or its owner has left the map or the game.
                if (now >= placed.ExpiresAt || placed.Owner.MapChannel != mapChannel ||
                    !mapChannel.ClientList.Any(client => client?.Player == placed.Owner))
                {
                    Remove(mapChannel, waypoints, placed);
                    continue;
                }

                SyncUsable(mapChannel, placed);
                Proximity(mapChannel, placed);
            }
        }

        private static void Remove(MapChannel mapChannel, List<Placed> waypoints, Placed placed)
        {
            lock (waypoints)
                waypoints.Remove(placed);

            List<Client> open;

            lock (placed.WindowOpen)
            {
                open = placed.WindowOpen.ToList();
                placed.WindowOpen.Clear();
            }

            foreach (var client in open)
                if (client.State != ClientState.Disconnected)
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());

            placed.Object.IsEnabled = false;
            CellManager.Instance.RemoveFromWorld(mapChannel, placed.Object);
        }

        /// <summary>
        /// Tells each client around it whether it is theirs to use, when that has changed: the
        /// owner when it is put down, someone who joins the owner's squad while it is out, and
        /// someone who leaves it. Clients no longer around are forgotten; meeting it again sends
        /// them its UsableInfo (ShowTo).
        /// </summary>
        private static void SyncUsable(MapChannel mapChannel, Placed placed)
        {
            if (!CellManager.TryGetCellCoordinates(placed.Object.Position, out var x, out var z))
                return;

            var around = CellManager.Instance.GetClientsInCells(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, x, z)).ToList();

            lock (placed.ToldUsable)
                placed.ToldUsable.RemoveWhere(client => !around.Contains(client));

            foreach (var client in around)
            {
                var usable = client.Player != null && IsFor(client.Player, placed);
                bool told;

                lock (placed.ToldUsable)
                    told = placed.ToldUsable.Contains(client);

                if (usable == told)
                    continue;

                lock (placed.ToldUsable)
                {
                    if (usable)
                        placed.ToldUsable.Add(client);
                    else
                        placed.ToldUsable.Remove(client);
                }

                client.CallMethod(placed.Object.EntityId, new SetUsablePacket(usable));
            }
        }

        private static void Proximity(MapChannel mapChannel, Placed placed)
        {
            // Those who left, or may no longer use it.
            List<Client> closing;

            lock (placed.WindowOpen)
            {
                closing = placed.WindowOpen.Where(client => !MayUse(client, placed) || !InReach(client, placed)).ToList();
                placed.WindowOpen.RemoveAll(closing.Contains);
            }

            foreach (var client in closing)
                if (client.State != ClientState.Disconnected)
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());

            if (!CellManager.TryGetCellCoordinates(placed.Object.Position, out var x, out var z))
                return;

            foreach (var client in CellManager.Instance.GetClientsInCells(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, x, z)))
            {
                if (!MayUse(client, placed) || !InReach(client, placed))
                    continue;

                lock (placed.WindowOpen)
                {
                    if (placed.WindowOpen.Contains(client))
                        continue;

                    placed.WindowOpen.Add(client);
                }

                OpenWindow(client, placed);
            }
        }

        private static void OpenWindow(Client client, Placed placed)
        {
            var mapChannel = client.Player.MapChannel;

            client.CallMethod(SysEntity.ClientMethodId,
                new EnteredWaypointPacket(mapChannel.MapInfo.MapContextId, placed.Object.MapContextId, Network(client),
                    WaypointType.Waypoint, WindowNameId, ReturnPoints(client, mapChannel, placed.Object.EntityId)));
        }

        private static bool InReach(Client client, Placed placed) =>
            Vector3.Distance(client.Player.Position, placed.Object.Position) <= Reach;

        private static Placed Find(DynamicObject obj)
        {
            var mapChannel = obj?.RuntimeMapChannel;

            if (mapChannel == null || !Waypoints.TryGetValue(mapChannel, out var waypoints))
                return null;

            lock (waypoints)
                return waypoints.FirstOrDefault(placed => placed.Object == obj);
        }
    }
}
