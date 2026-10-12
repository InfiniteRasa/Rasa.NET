using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Dropship Extraction Beacon (item 130285): ACCOUNTREWARD_PORTAL (511, abilities.portal)
    /// level 1. "A portable dropship waypoint. Works for the entire squad of the person who
    /// deploys it. Allows one-way access to the local dropship network. Duration: 5 minutes.
    /// Cooldown: 1 hour." The level's properties: CLASS_ID 29954 (UsableAbilityTemporaryDropship_
    /// Reward, "Dropship Waypoint", vfx_reward_personal_dropship.geo, a WORMHOLE usable),
    /// WAYPOINT_TYPE_ID 2 (the client's MAPWAYPOINT: the dropship network), DURATION 300 s and
    /// DELAY_TIME_MS 8000; the reuse is the action level's hour. The client's portal.py only
    /// preloads CLASS_ID's assets in the windup; the ship is the server's to put down.
    ///
    /// The ship's states are the WORMHOLE usable's (USE_WH_STATE_0/1/2 = 205/214/215):
    /// 214 is the particle proxy mesh - nothing to see - 214 to 215 plays
    /// vfx_reward_personal_dropship_birth, and 205 is the hovering loop
    /// (vfx_reward_personal_dropship_loop); 205 to 214 takes it away again. So it is put down
    /// in 214 and flown in at once, and after DELAY_TIME_MS it settles into 205 and is in
    /// service. At the end of DURATION it goes back to 214 and is taken away.
    ///
    /// While it is in service, the deployer and their squad within 5 m of it - the reach of a
    /// dropship pad - get the dropship travel window, the same list a pad gives (the pads they
    /// have gained on this planet), and leaving that reach closes it. Using the ship opens it
    /// too. It is not itself a destination: one way out, as the item says. The window's
    /// "current" line reads "Portable Waypoint" (waypointlanguage 418); the client has no name
    /// for a beacon. SelectWaypoint takes a beacon in reach as the departure station
    /// (<see cref="IsNearUsable"/>) and the flight is the pads' own.
    ///
    /// In service for the squad alone. Every usable's IsUsable() on the client is the enabled
    /// flag of UsableInfo and SetUsable, which go to each client on its own; for a WORMHOLE it
    /// decides whether the ship shows as an OBJECT (enabled) or under its target category (not).
    /// So the deployer and their squad are told the ship is enabled once it has settled, and
    /// everyone else is left with the disabled one it was created with; someone who joins or
    /// leaves the squad while it is out is told again (<see cref="SyncUsable"/>), as is a client
    /// meeting the ship later (<see cref="ShowTo"/>). The WORMHOLE's own Recv_IsUsable
    /// (IsUsable, 599) only stores the value in a field nothing reads, so it is not sent.
    /// </summary>
    public static class DropshipBeacons
    {
        public const string PortalModule = "abilities.portal";

        /// <summary>UsableAbilityTemporaryDropship_Reward, the level's CLASS_ID.</summary>
        public const EntityClasses ShipClass = (EntityClasses)29954;

        /// <summary>The client's MAPWAYPOINT, the level's WAYPOINT_TYPE_ID: the dropship network.</summary>
        public const int DropshipNetwork = 2;

        /// <summary>waypointlanguage 418, "Portable Waypoint": the window's name for where the player is.</summary>
        public const uint WindowNameId = 418;

        /// <summary>How near the ship a squad member has to stand: a dropship pad's reach.</summary>
        public const float Reach = 5f;

        public const int DefaultDurationSeconds = 300;
        public const int DefaultArrivalMs = 8000;

        /// <summary>How long the ship is kept after it has been sent away, for the 205 to 214 transition to play.</summary>
        public const int DepartureMs = 3000;

        private sealed class Beacon
        {
            public Manifestation Owner;
            public DynamicObject Ship;
            public long InServiceAt;
            public long ExpiresAt;
            public long RemoveAt;
            public bool InService;
            public bool Leaving;
            public readonly List<Client> WindowOpen = new List<Client>();

            /// <summary>The clients that have been told the ship is enabled.</summary>
            public readonly HashSet<Client> ToldUsable = new HashSet<Client>();
        }

        private static readonly ConditionalWeakTable<MapChannel, List<Beacon>> Beacons = new();

        /// <summary>Whether this action level is a dropship beacon: the portal module, a ship class and the dropship network.</summary>
        public static bool Is(ActionInfo action, ActionLevelInfo info) =>
            action?.Module == PortalModule && info != null &&
            info.Get(AbilityProperty.ClassId) > 0 && info.Get(AbilityProperty.WaypointTypeId) == DropshipNetwork;

        /// <summary>Puts the player's ship down where they stand and flies it in. A ship of theirs already out is sent away first.</summary>
        public static DynamicObject Deploy(MapChannel mapChannel, Manifestation owner, ActionLevelInfo info, long now = 0)
        {
            if (mapChannel == null || owner == null)
                return null;

            if (now == 0)
                now = Environment.TickCount64;

            SendAway(owner, now);

            var ship = new DynamicObject
            {
                EntityClassId = (EntityClasses)info.Get(AbilityProperty.ClassId, (int)ShipClass),
                DynamicObjectType = DynamicObjectType.DropshipBeacon,
                Position = NavMeshManager.SnapToGround(mapChannel, owner.Position),
                Rotation = owner.Rotation,
                MapContextId = owner.MapContextId,
                StateId = UseObjectState.WhState1,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, ship);

            // In: the birth plays, and the ship holds in 215 until it settles.
            CellManager.Instance.CellCallMethod(ship, new UsePacket(owner.EntityId, UseObjectState.WhState2, 0));
            ship.StateId = UseObjectState.WhState2;

            var arrival = Math.Max(0, info.Get(AbilityProperty.DelayTimeMs, DefaultArrivalMs));
            var seconds = Math.Max(1, info.Get(AbilityProperty.Duration, DefaultDurationSeconds));
            var beacon = new Beacon
            {
                Owner = owner,
                Ship = ship,
                InServiceAt = now + arrival,
                ExpiresAt = now + arrival + seconds * 1000L
            };

            var beacons = Beacons.GetValue(mapChannel, _ => new List<Beacon>());

            lock (beacons)
                beacons.Add(beacon);

            return ship;
        }

        /// <summary>Whether this player may use the ship: the one who put it down or their squad, alive and in the world, while it is in service.</summary>
        public static bool MayUse(Client client, DynamicObject ship)
        {
            var beacon = Find(ship);

            return beacon != null && MayUse(client, beacon);
        }

        private static bool MayUse(Client client, Beacon beacon)
        {
            var player = client?.Player;

            return beacon.InService && !beacon.Leaving && player != null && client.State == ClientState.Ingame &&
                   client.PendingTransfer == null && player.State != CharacterState.Dead && player.State != CharacterState.Dying &&
                   player.MapChannel == beacon.Ship.RuntimeMapChannel &&
                   (player == beacon.Owner || Detection.SameSquad(beacon.Owner, player));
        }

        /// <summary>
        /// Whether the ship goes to this client enabled: in service, and theirs or their squad's.
        /// Called for the UsableInfo of a client meeting the ship, and remembered.
        /// </summary>
        public static bool ShowTo(Client client, DynamicObject ship)
        {
            var beacon = Find(ship);

            if (beacon == null || !ShownUsable(client, beacon))
                return false;

            lock (beacon.ToldUsable)
                beacon.ToldUsable.Add(client);

            return true;
        }

        private static bool ShownUsable(Client client, Beacon beacon)
        {
            var player = client?.Player;

            return beacon.InService && !beacon.Leaving && player != null &&
                   (player == beacon.Owner || Detection.SameSquad(beacon.Owner, player));
        }

        /// <summary>A ship in service the player may use within reach, on this map: a departure station for SelectWaypoint.</summary>
        public static bool IsNearUsable(Client client, MapChannel mapChannel)
        {
            if (client?.Player == null || mapChannel == null || !Beacons.TryGetValue(mapChannel, out var beacons))
                return false;

            lock (beacons)
                return beacons.Any(beacon => MayUse(client, beacon) && InReach(client, beacon));
        }

        /// <summary>The ship used: the request is closed, and the travel window opened for someone who may use it.</summary>
        public static void Use(Client client, DynamicObject ship, RequestUseObjectPacket packet)
        {
            var beacon = Find(ship);

            if (beacon == null || !MayUse(client, beacon) || !InReach(client, beacon))
            {
                ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, PlayerMessage.PmUseObjectNotUsable);
                return;
            }

            // Nothing is performed on the ship: the use is answered, and the window is what it does.
            ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, null);

            lock (beacon.WindowOpen)
                if (!beacon.WindowOpen.Contains(client))
                    beacon.WindowOpen.Add(client);

            OpenWindow(client, beacon);
        }

        /// <summary>
        /// The ships on a map: settled and put in service, sent away when their time is up and
        /// taken away after, and the travel window opened for those who come within reach and
        /// closed for those who leave it. <paramref name="now"/> is for tests; 0 is the clock.
        /// </summary>
        public static void Worker(MapChannel mapChannel, long now = 0)
        {
            if (mapChannel == null || !Beacons.TryGetValue(mapChannel, out var beacons))
                return;

            if (now == 0)
                now = Environment.TickCount64;

            List<Beacon> all;

            lock (beacons)
                all = beacons.ToList();

            foreach (var beacon in all)
            {
                if (!beacon.Leaving && !beacon.InService && now >= beacon.InServiceAt)
                {
                    beacon.InService = true;
                    beacon.Ship.StateId = UseObjectState.WhState0;
                    CellManager.Instance.CellCallMethod(beacon.Ship, new UsePacket(beacon.Owner.EntityId, UseObjectState.WhState0, 0));

                    // In service on the server, so a use of it is looked at; the clients are told
                    // one by one, the squad yes and nobody else anything (SyncUsable).
                    beacon.Ship.IsEnabled = true;
                }

                if (!beacon.Leaving && now >= beacon.ExpiresAt)
                    Leave(beacon, now);

                if (beacon.Leaving && now >= beacon.RemoveAt)
                {
                    lock (beacons)
                        beacons.Remove(beacon);

                    CellManager.Instance.RemoveFromWorld(mapChannel, beacon.Ship);
                    continue;
                }

                SyncUsable(mapChannel, beacon);
                Proximity(mapChannel, beacon);
            }
        }

        /// <summary>
        /// Tells each client around the ship whether it is in service for them, when that has
        /// changed: the squad when it settles, someone who joins the squad while it is out, and
        /// someone who leaves it. Clients no longer around are forgotten; meeting the ship again
        /// sends them its UsableInfo (ShowTo).
        /// </summary>
        private static void SyncUsable(MapChannel mapChannel, Beacon beacon)
        {
            if (beacon.Leaving || !CellManager.TryGetCellCoordinates(beacon.Ship.Position, out var x, out var z))
                return;

            var around = CellManager.Instance.GetClientsInCells(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, x, z)).ToList();

            lock (beacon.ToldUsable)
                beacon.ToldUsable.RemoveWhere(client => !around.Contains(client));

            foreach (var client in around)
            {
                var usable = ShownUsable(client, beacon);
                bool told;

                lock (beacon.ToldUsable)
                    told = beacon.ToldUsable.Contains(client);

                if (usable == told)
                    continue;

                lock (beacon.ToldUsable)
                {
                    if (usable)
                        beacon.ToldUsable.Add(client);
                    else
                        beacon.ToldUsable.Remove(client);
                }

                client.CallMethod(beacon.Ship.EntityId, new SetUsablePacket(usable));
            }
        }

        /// <summary>Sends away the player's ship, if they have one out, on any map.</summary>
        private static void SendAway(Manifestation owner, long now)
        {
            var mapChannel = owner.MapChannel;

            if (mapChannel == null || !Beacons.TryGetValue(mapChannel, out var beacons))
                return;

            List<Beacon> theirs;

            lock (beacons)
                theirs = beacons.Where(beacon => beacon.Owner == owner && !beacon.Leaving).ToList();

            foreach (var beacon in theirs)
                Leave(beacon, now);
        }

        private static void Leave(Beacon beacon, long now)
        {
            beacon.Leaving = true;
            beacon.RemoveAt = now + DepartureMs;

            CloseAll(beacon);

            // Out of service for everyone: those told otherwise are told again.
            List<Client> told;

            lock (beacon.ToldUsable)
            {
                told = beacon.ToldUsable.ToList();
                beacon.ToldUsable.Clear();
            }

            beacon.Ship.IsEnabled = false;

            foreach (var client in told)
                if (client.State != ClientState.Disconnected)
                    client.CallMethod(beacon.Ship.EntityId, new SetUsablePacket(false));

            // 205 to 214: away. A ship that never settled goes straight from 215, which has no
            // transition to 214 - it is simply taken away when its time comes.
            if (beacon.Ship.StateId == UseObjectState.WhState0)
                CellManager.Instance.CellCallMethod(beacon.Ship, new UsePacket(beacon.Owner.EntityId, UseObjectState.WhState1, 0));

            beacon.Ship.StateId = UseObjectState.WhState1;
        }

        private static void Proximity(MapChannel mapChannel, Beacon beacon)
        {
            // Those who left, or may no longer use it.
            List<Client> closing;

            lock (beacon.WindowOpen)
            {
                closing = beacon.WindowOpen.Where(client => !MayUse(client, beacon) || !InReach(client, beacon)).ToList();
                beacon.WindowOpen.RemoveAll(closing.Contains);
            }

            foreach (var client in closing)
                if (client.State != ClientState.Disconnected)
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());

            if (!beacon.InService || beacon.Leaving ||
                !CellManager.TryGetCellCoordinates(beacon.Ship.Position, out var x, out var z))
                return;

            foreach (var client in CellManager.Instance.GetClientsInCells(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, x, z)))
            {
                if (!MayUse(client, beacon) || !InReach(client, beacon))
                    continue;

                lock (beacon.WindowOpen)
                {
                    if (beacon.WindowOpen.Contains(client))
                        continue;

                    beacon.WindowOpen.Add(client);
                }

                OpenWindow(client, beacon);
            }
        }

        private static void CloseAll(Beacon beacon)
        {
            List<Client> open;

            lock (beacon.WindowOpen)
            {
                open = beacon.WindowOpen.ToList();
                beacon.WindowOpen.Clear();
            }

            foreach (var client in open)
                if (client.State != ClientState.Disconnected)
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
        }

        private static void OpenWindow(Client client, Beacon beacon)
        {
            var mapChannel = client.Player.MapChannel;
            var dropships = DynamicObjectManager.Instance.CreateListOfDropships(client);

            // The map the player is on by its context id, which the window's rows are keyed by:
            // it marks that row "(current)" and draws its pads. The instance number matched no row.
            client.CallMethod(SysEntity.ClientMethodId,
                new EnteredWaypointPacket(mapChannel.MapInfo.MapContextId, beacon.Ship.MapContextId, dropships, WaypointType.Dropship, WindowNameId));
        }

        private static bool InReach(Client client, Beacon beacon) =>
            System.Numerics.Vector3.Distance(client.Player.Position, beacon.Ship.Position) <= Reach;

        private static Beacon Find(DynamicObject ship)
        {
            var mapChannel = ship?.RuntimeMapChannel;

            if (mapChannel == null || !Beacons.TryGetValue(mapChannel, out var beacons))
                return null;

            lock (beacons)
                return beacons.FirstOrDefault(beacon => beacon.Ship == ship);
        }
    }
}
