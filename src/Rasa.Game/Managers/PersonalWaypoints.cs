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
    /// Where: not on a battleground's map and not in an instance (<see cref="Refusal"/>). The
    /// items' text has them "indefinite in Operations zones"; here an Operation takes none.
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
    /// Whose: its owner's, and at level 3 their squad's, on the map it stands on.
    ///
    /// Attacked: it has <see cref="MaxHealth"/> hit points and is gone at none. Its owner's
    /// enemies across a wargame may shoot and strike it, and so may their creatures; a HOSTILE
    /// creature picks it for a target as it would its owner, and fights it with what it has
    /// (BehaviorManager, Threat, MissileManager, ConstantFire). A hit lands as it is: no crit,
    /// cover or resistance, as on a force field. Abilities do not reach it; they are aimed at
    /// actors.
    ///
    /// To each client it is one of three things (<see cref="Standing"/>), told when the client
    /// meets it and again when that changes - a squad joined or left, a wargame begun or over:
    ///  - theirs to use: enabled (UsableInfo, SetUsable), which the client shows as an OBJECT;
    ///  - an enemy's: not enabled, HOSTILE and with hit points that can be damaged
    ///    (DamageInfo) - the client's "wormholes are mouse-targetable if you can harm them";
    ///  - neither: not enabled and FRIENDLY. The category has to be said: the client starts a
    ///    wormhole with one of False, which it reads as HOSTILE (0).
    /// Everyone is told the hit points, which the overhead bar of a destroyable usable shows.
    ///
    /// How long: DURATION. It goes sooner when it is destroyed, or when its owner puts down
    /// another, leaves the map or leaves the game. However it goes, vfx_ability_wormhole_death
    /// plays where it stood - the client's ABILITY_TEMPORARY_WORMHOLE_DEATH, which nothing in
    /// its own code plays.
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

        /// <summary>Its hit points. The client has none for the class; a force field's.</summary>
        public const int MaxHealth = 5000;

        /// <summary>vfx_ability_wormhole_death (FxPackages).</summary>
        public const uint DeathPackage = 28072;

        /// <summary>How long the death is left playing where it stood.</summary>
        public const int DeathMs = 3000;

        /// <summary>What a Personal Waypoint is to a player, and so what their client has been told of it.</summary>
        private enum Standing
        {
            /// <summary>Nothing told yet: the client's own start - not enabled, HOSTILE, no hit points.</summary>
            Unmet,

            /// <summary>Theirs, or their squad's, to use.</summary>
            Theirs,

            /// <summary>An enemy's: to attack.</summary>
            Enemy,

            /// <summary>Somebody else's: neither.</summary>
            Other
        }

        /// <summary>A Personal Waypoint out on a map.</summary>
        private sealed class Placed
        {
            public Manifestation Owner;
            public string OwnerName;
            public uint Level;
            public DynamicObject Object;
            public long ExpiresAt;
            public int Health = MaxHealth;

            public readonly List<Client> WindowOpen = new List<Client>();

            /// <summary>What each client around it has been told it is to them.</summary>
            public readonly Dictionary<Client, Standing> Told = new Dictionary<Client, Standing>();
        }

        /// <summary>The death of one that has gone, playing where it stood.</summary>
        private sealed class Death
        {
            public MapEmitter Emitter;
            public long RemoveAt;
        }

        /// <summary>The Personal Waypoints of a map channel. Locked for its lists and for a waypoint's hit points.</summary>
        private sealed class OnMap
        {
            public readonly List<Placed> Waypoints = new List<Placed>();
            public readonly List<Death> Deaths = new List<Death>();
        }

        private static readonly ConditionalWeakTable<MapChannel, OnMap> Maps = new();

        /// <summary>The waypoints of the map's network a player has gained, for the window. Replaced in tests.</summary>
        internal static Func<Client, Dictionary<uint, MapWaypointInfoList>> Network =
            client => DynamicObjectManager.Instance.CreateListOfWaypoints(client, WaypointType.Waypoint);

        public static bool Is(ActionInfo action) => action?.Module == Module;

        #region Where

        /// <summary>
        /// Why a Personal Waypoint cannot be put down where the player is: on a battleground's
        /// map, or in an instance. The client's own check (temporarywormhole.py CheckAction)
        /// refuses only a player with no team on a map that has teams.
        /// </summary>
        public static PlayerMessage? Refusal(Manifestation player)
        {
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return null;

            if (Battlegrounds.Instance.IsBattleground(mapChannel.MapInfo.MapContextId) || IsInstance(mapChannel))
                return PlayerMessage.PmCannotPerformActionNow;

            return null;
        }

        /// <summary>
        /// An instance: a squad's or a player's own copy of a map, and any map that is entered
        /// as a squad's instance - the Operations, which are instances whether or not the
        /// server is entering them as squad instances (<see cref="SquadInstancePolicies"/>). A
        /// further public copy of an open map (MapChannel.IsSharedInstance) is not one.
        /// </summary>
        public static bool IsInstance(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return false;

            var map = mapChannel.MapInfo.MapContextId;

            return mapChannel.IsSquadInstance || mapChannel.IsPrivateInstance ||
                   SquadInstancePolicies.IsSquadMap(map) || SquadInstancePolicies.DefaultMaps.Contains(map);
        }

        #endregion

        #region Put down

        /// <summary>Puts the player's Personal Waypoint down where they stand, in service. One of theirs already out on this map goes first.</summary>
        public static DynamicObject Deploy(MapChannel mapChannel, Manifestation owner, uint level, ActionLevelInfo info, long now = 0)
        {
            if (mapChannel == null || owner == null)
                return null;

            if (now == 0)
                now = Environment.TickCount64;

            var onMap = Maps.GetValue(mapChannel, _ => new OnMap());
            List<Placed> theirs;

            lock (onMap)
                theirs = onMap.Waypoints.Where(placed => placed.Owner == owner).ToList();

            foreach (var placed in theirs)
                Remove(mapChannel, onMap, placed, now);

            var obj = new DynamicObject
            {
                EntityClassId = WaypointClass,
                DynamicObjectType = DynamicObjectType.PersonalWaypoint,
                Position = NavMeshManager.SnapToGround(mapChannel, owner.Position),
                Rotation = owner.Rotation,
                MapContextId = owner.MapContextId,
                StateId = UseObjectState.WhState0,

                // A player's, as its owner is.
                TargetCategory = TargetCategory.Friendly,

                // In service on the server, so a use of it is looked at; each client is told
                // what it is to them (Introduce, Sync).
                IsEnabled = true
            };

            var seconds = Math.Max(1, info?.Get(AbilityProperty.Duration, DefaultDurationSeconds) ?? DefaultDurationSeconds);
            var waypoint = new Placed
            {
                Owner = owner,
                OwnerName = $"{owner.Name} {owner.FamilyName}".Trim(),
                Level = level,
                Object = obj,
                ExpiresAt = now + seconds * 1000L
            };

            // Listed before the clients around are shown it, so that each is shown it as it is for them (ShowTo).
            lock (onMap)
                onMap.Waypoints.Add(waypoint);

            CellManager.Instance.AddToWorld(mapChannel, obj);

            return obj;
        }

        #endregion

        #region Whose

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
                   placed.Health > 0 &&
                   placed.Object.RuntimeMapChannel != null && player.MapChannel == placed.Object.RuntimeMapChannel &&
                   IsFor(player, placed);
        }

        private static bool IsFor(Manifestation player, Placed placed) =>
            player == placed.Owner || placed.Level >= SquadLevel && Detection.SameSquad(placed.Owner, player);

        private static Standing StandingOf(Manifestation player, Placed placed)
        {
            if (IsFor(player, placed))
                return Standing.Theirs;

            return Pvp.AreEnemies(player, placed.Owner) ? Standing.Enemy : Standing.Other;
        }

        /// <summary>
        /// The category a client is told: FRIENDLY for a bystander. For whoever may use it or
        /// attack it, HOSTILE - the client's wormhole takes the mouse only while its category is
        /// that, and shows as an OBJECT anyway while it is enabled.
        /// </summary>
        private static TargetCategory CategoryOf(Standing standing) =>
            standing == Standing.Other ? TargetCategory.Friendly : TargetCategory.Hostile;

        /// <summary>
        /// Whether it goes to this client enabled: theirs, or their squad's. For the UsableInfo
        /// of a client meeting it; the rest of what they are told follows (<see cref="Introduce"/>).
        /// </summary>
        public static bool ShowTo(Client client, DynamicObject obj)
        {
            var placed = Find(obj);

            return placed != null && client?.Player != null && IsFor(client.Player, placed);
        }

        /// <summary>
        /// What a client meeting it is told besides its creation: its hit points, whether they
        /// are the client's to take, and its category when that is not the one the client
        /// starts it with. Called from DynamicObjectManager.CreateDynamicObjectOnClient.
        /// </summary>
        internal static void Introduce(Client client, DynamicObject obj)
        {
            var placed = Find(obj);

            if (placed == null || client?.Player == null)
                return;

            var standing = StandingOf(client.Player, placed);

            lock (placed.Told)
                placed.Told[client] = standing;

            client.CallMethod(obj.EntityId, new UsableDamageInfoPacket(standing == Standing.Enemy, false, MaxHealth, placed.Health));

            if (CategoryOf(standing) != CategoryOf(Standing.Unmet))
                client.CallMethod(obj.EntityId, new TargetCategoryPacket(CategoryOf(standing)));
        }

        /// <summary>
        /// Tells each client around it what it is to them, when that has changed: someone who
        /// joins or leaves the owner's squad while it is out, and someone whose wargame with
        /// the owner begins or ends. Clients no longer around are forgotten; meeting it again
        /// introduces it to them (ShowTo, Introduce).
        /// </summary>
        private static void Sync(MapChannel mapChannel, Placed placed)
        {
            if (!CellManager.TryGetCellCoordinates(placed.Object.Position, out var x, out var z))
                return;

            var around = CellManager.Instance.GetClientsInCells(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, x, z)).ToList();

            lock (placed.Told)
                foreach (var gone in placed.Told.Keys.Where(client => !around.Contains(client)).ToList())
                    placed.Told.Remove(gone);

            foreach (var client in around)
            {
                if (client.Player == null)
                    continue;

                var standing = StandingOf(client.Player, placed);
                Standing told;

                lock (placed.Told)
                {
                    placed.Told.TryGetValue(client, out told);
                    placed.Told[client] = standing;
                }

                if (standing == told)
                    continue;

                if ((standing == Standing.Theirs) != (told == Standing.Theirs))
                    client.CallMethod(placed.Object.EntityId, new SetUsablePacket(standing == Standing.Theirs));

                if ((standing == Standing.Enemy) != (told == Standing.Enemy))
                    client.CallMethod(placed.Object.EntityId, new UsableDamageInfoPacket(standing == Standing.Enemy, false, MaxHealth, placed.Health));

                if (CategoryOf(standing) != CategoryOf(told))
                    client.CallMethod(placed.Object.EntityId, new TargetCategoryPacket(CategoryOf(standing)));
            }
        }

        #endregion

        #region The ways out and back

        /// <summary>
        /// A Personal Waypoint the player may use within reach, on this map: a departure station
        /// for SelectWaypoint and ReturnToWormhole. <paramref name="except"/> is the one they
        /// are going to, which is no way to itself.
        /// </summary>
        public static bool IsNearUsable(Client client, MapChannel mapChannel, ulong except = 0)
        {
            if (client?.Player == null || mapChannel == null || !Maps.TryGetValue(mapChannel, out var onMap))
                return false;

            lock (onMap)
                return onMap.Waypoints.Any(placed => placed.Object.EntityId != except && MayUse(client, placed) && InReach(client, placed));
        }

        /// <summary>
        /// The Personal Waypoints on this map the player may return to, for a waypoint window:
        /// the two-way ones that are theirs or their squad's. Null when there are none, which is
        /// what EnteredWaypoint sends then. <paramref name="except"/> is the one whose window it is.
        /// </summary>
        public static List<TempWormhole> ReturnPoints(Client client, MapChannel mapChannel, ulong except = 0)
        {
            if (client?.Player == null || mapChannel == null || !Maps.TryGetValue(mapChannel, out var onMap))
                return null;

            List<TempWormhole> points;

            lock (onMap)
                points = onMap.Waypoints
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

            if (id == 0 || client?.Player == null || mapChannel == null || !Maps.TryGetValue(mapChannel, out var onMap))
                return false;

            Placed found;

            lock (onMap)
                found = onMap.Waypoints.FirstOrDefault(placed => placed.Object.EntityId == id);

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

        #endregion

        #region Attacked

        /// <summary>
        /// Whether this actor may harm it: a player who is an enemy of its owner's across a
        /// wargame, a creature of such a player's, and a HOSTILE creature, to which a player's
        /// waypoint is what the player is.
        /// </summary>
        private static bool MayHarm(Actor attacker, Placed placed)
        {
            var mapChannel = placed.Object.RuntimeMapChannel;

            if (attacker == null || mapChannel == null || placed.Health <= 0 || !MapInstanceScope.Contains(mapChannel, attacker))
                return false;

            return attacker switch
            {
                Manifestation player => Pvp.AreEnemies(player, placed.Owner),
                Creature creature when creature.MasterEntityId != 0 => Pvp.AreEnemies(Pvp.Controller(creature), placed.Owner),
                Creature creature => TargetCategories.Seeks(creature.TargetCategory, placed.Object.TargetCategory),
                _ => false
            };
        }

        /// <summary>Whether the entity is a Personal Waypoint this actor may attack: a shot at it is let go (MissileManager.MissileLaunch).</summary>
        public static bool MayBeAttackedBy(Actor attacker, ulong entityId)
        {
            var placed = Find(entityId);

            return placed != null && MayHarm(attacker, placed);
        }

        /// <summary>Whether the entity is a Personal Waypoint this creature may pick a fight with and keep at (BehaviorManager.MayFight, Threat.CanFight).</summary>
        public static bool MayBeFoughtBy(Creature creature, ulong entityId) => MayBeAttackedBy(creature, entityId);

        /// <summary>Where a Personal Waypoint stands: what a creature fighting it walks up to. False for anything else.</summary>
        public static bool TryGetPosition(ulong entityId, out Vector3 position)
        {
            var placed = Find(entityId);

            position = placed?.Object.Position ?? default;

            return placed != null;
        }

        /// <summary>
        /// A missile at an object landing (MissileManager.MissileTrigger). False when the object
        /// is no Personal Waypoint. Its damage goes on the waypoint's hit points as it is, and
        /// what it took is what the hit shows.
        /// </summary>
        internal static bool TakeHit(Missile missile)
        {
            var placed = Find(missile.TargetEntityId);

            if (placed == null)
                return false;

            missile.DamageA = MayHarm(missile.Source, placed) ? Damage(placed, missile.DamageA, missile.Source) : 0;

            return true;
        }

        /// <summary>
        /// A hit that is no missile - a constant-fire weapon's pulse (ConstantFire). What the
        /// waypoint took, or null when the entity is not a Personal Waypoint the attacker may harm.
        /// </summary>
        public static int? TakeDamage(Actor attacker, ulong entityId, int amount)
        {
            var placed = Find(entityId);

            if (placed == null || !MayHarm(attacker, placed))
                return null;

            return Damage(placed, amount, attacker);
        }

        /// <summary>Its hit points now; 0 for anything that is not a Personal Waypoint out on a map.</summary>
        public static int HealthOf(DynamicObject obj) => Find(obj)?.Health ?? 0;

        private static int Damage(Placed placed, int amount, Actor attacker)
        {
            var mapChannel = placed.Object.RuntimeMapChannel;

            if (mapChannel == null || amount <= 0 || !Maps.TryGetValue(mapChannel, out var onMap))
                return 0;

            int taken;
            int left;

            lock (onMap)
            {
                taken = Math.Min(amount, placed.Health);
                placed.Health -= taken;
                left = placed.Health;
            }

            if (taken <= 0)
                return 0;

            // An attack on an enemy's is an attack on an enemy: the attacker's PvP Safety comes off (Pvp.Attack).
            if (Pvp.Controller(attacker) is Manifestation player)
                Pvp.EndSafety(mapChannel, player);

            CellManager.Instance.CellCallMethod(placed.Object, new UpdateHitPointsPacket(left));

            if (left <= 0)
                Remove(mapChannel, onMap, placed, Environment.TickCount64);

            return taken;
        }

        #endregion

        #region How long

        /// <summary>
        /// The Personal Waypoints on a map: taken away when their time is up or their owner has
        /// gone, the clients around told what they are to them, and the waypoint window opened
        /// for those who come within reach and closed for those who leave it.
        /// <paramref name="now"/> is for tests; 0 is the clock.
        /// </summary>
        public static void Worker(MapChannel mapChannel, long now = 0)
        {
            if (mapChannel == null || !Maps.TryGetValue(mapChannel, out var onMap))
                return;

            if (now == 0)
                now = Environment.TickCount64;

            List<Placed> all;
            List<Death> played;

            lock (onMap)
            {
                all = onMap.Waypoints.ToList();
                played = onMap.Deaths.Where(death => now >= death.RemoveAt).ToList();
                onMap.Deaths.RemoveAll(played.Contains);
            }

            foreach (var death in played)
                EmitterManager.Instance.RemoveTemporary(mapChannel, death.Emitter);

            foreach (var placed in all)
            {
                // Its time is up, or its owner has left the map or the game.
                if (now >= placed.ExpiresAt || placed.Owner.MapChannel != mapChannel ||
                    !mapChannel.ClientList.Any(client => client?.Player == placed.Owner))
                {
                    Remove(mapChannel, onMap, placed, now);
                    continue;
                }

                Sync(mapChannel, placed);
                Proximity(mapChannel, placed);
            }
        }

        private static void Remove(MapChannel mapChannel, OnMap onMap, Placed placed, long now)
        {
            lock (onMap)
                if (!onMap.Waypoints.Remove(placed))
                    return;

            List<Client> open;

            lock (placed.WindowOpen)
            {
                open = placed.WindowOpen.ToList();
                placed.WindowOpen.Clear();
            }

            foreach (var client in open)
                if (client.State != ClientState.Disconnected)
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());

            var obj = placed.Object;

            obj.IsEnabled = false;
            CellManager.Instance.RemoveFromWorld(mapChannel, obj);

            var death = new Death
            {
                Emitter = EmitterManager.Instance.PlayTemporary(mapChannel, obj.MapContextId, obj.Position, obj.Rotation, DeathPackage,
                    $"personal waypoint of {placed.OwnerName} gone"),
                RemoveAt = now + DeathMs
            };

            lock (onMap)
                onMap.Deaths.Add(death);
        }

        #endregion

        private static Placed Find(DynamicObject obj)
        {
            var mapChannel = obj?.RuntimeMapChannel;

            if (mapChannel == null || obj.DynamicObjectType != DynamicObjectType.PersonalWaypoint || !Maps.TryGetValue(mapChannel, out var onMap))
                return null;

            lock (onMap)
                return onMap.Waypoints.FirstOrDefault(placed => placed.Object == obj);
        }

        private static Placed Find(ulong entityId) =>
            entityId != 0 && EntityManager.Instance.TryGetObject(entityId, out var obj) ? Find(obj) : null;
    }
}
