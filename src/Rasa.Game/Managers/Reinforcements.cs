using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    /// <summary>
    /// How fresh troops come into the open world, as they did on the original servers: "When the game
    /// needs to replenish the bad guys, it swoops in with a dropship or beams them down with a Star
    /// Trek-style teleporter. In either case, there is a very distinctive sound that precedes them, as
    /// well as a bunch of glowing red light." - and on the AFS side, "reinforcements are constantly
    /// being deployed by each faction's dropships" at the control points.
    ///
    /// It is the arrival points' own mechanism (BaneArrivals, spawnpool_arrival) for the pools that
    /// have no row of their own: <see cref="ArrivalFor"/> makes an arrival point of no table's for the
    /// pool's next spawn, and SpawnPoolManager sends it as it sends a row's.
    ///  - A pool of Bane troops on one of the open world's maps (<see cref="ReinforcementConfig.ShipMaps"/>)
    ///    restocks by Bane dropship (UsableCrSpawnerBaneDropshipV01, 9268) when there is
    ///    open sky over some of its ground: the ship comes down there, DynamicObjectManager.DropshipsWorker
    ///    flies it through the spawner states every spawner ship has (USE_CS_STATE_BEGIN, SPAWN, END),
    ///    its troops are set down under it and walk out to their posts (BehaviorManager.WalkIn), and it
    ///    leaves (<see cref="SpawnPoolArrivalEntry.KindDropship"/>).
    ///  - A Bane ship never comes down within an outpost's walls - the yard of a point the AFS hold, or of
    ///    one the Bane hold whose garrison is restocking: before one of its gates, or outside its walls
    ///    round it (<see cref="LandingOutsideWalls"/>), and the troops walk in.
    ///  - The soldiers of an AFS control point garrison come back the same way, by AFS dropship
    ///    (UsableCrSpawnerHumDropshipV01, 9269; <see cref="SpawnPoolArrivalEntry.KindAfsDropship"/>).
    ///  - Troops that come back without a ship - indoors, underground, under a roof, nowhere outside
    ///    the walls just now - beam in where they stand (<see cref="SpawnPoolArrivalEntry.KindBeamIn"/>):
    ///    CREATURE_BIRTH (action 155) at their own class, the action the client data gives most Bane
    ///    classes for it and the AFS's allies the Thrax's one, as CreatureSummons does for a summon. A
    ///    class without one (the NoBeaminBirth Thrax, every boss, the human D11 soldiers) just stands
    ///    there.
    ///  - A pool whose own anim_type asks for a ship (1 the Bane's, 2 the AFS's), or that has arrival
    ///    rows, has them as before, and none of this.
    ///  - The world's first stocking after the server starts is a plain one, as BaneArrivals has it:
    ///    the world starts stocked rather than with every camp's dropship coming down at once. A pool
    ///    that is to come by ship on its next spawn whatever (<see cref="SpawnPool.ShipNextArrival"/>:
    ///    a control point's new garrison) does.
    ///
    /// The landing spot: the pool's own ground (its radius, at least <see cref="MinLandingRadius"/>)
    /// on the navmesh, above ground, with <see cref="ReinforcementConfig.SkyClearance"/> metres of
    /// open sky over it and room for the ship's body round it (<see cref="IsOpenSky"/>), by the map's
    /// cover mesh (its collision triangles), and no other ship's hull within reach of its own. A map
    /// without a cover mesh is taken to be open.
    /// </summary>
    public static class Reinforcements
    {
        /// <summary>CREATURE_BIRTH: the client's CreatureBirthAction, at the creature's own class.</summary>
        public const ActionId BirthAction = ActionId.CreatureBirth;

        /// <summary>gameeffectdata CREATURE_BIRTH.</summary>
        public const int BirthTypeId = 10;

        /// <summary>How far round a pool's centre a ship may come down, at the least, when the pool is a point or small.</summary>
        public const float MinLandingRadius = 12f;

        /// <summary>How many spots a landing tries before the troops beam in instead.</summary>
        public const int LandingAttempts = 12;

        /// <summary>
        /// How far round the spot the ship's body needs room, and from what height (it hovers above its
        /// beam). The retail hulls reach some 12 m fore and aft and 8 m to the sides.
        /// </summary>
        public const float HullRadius = 15f;
        public const float HullHeight = 5f;

        public static ReinforcementConfig Config { get; set; } = new ReinforcementConfig();

        /// <summary>The open world: the client's battlefield contexts (gamecontext type 4) that are not wargames or tests.</summary>
        public static readonly IReadOnlyCollection<uint> DefaultShipMaps = new HashSet<uint>
        {
            1148, // Concordia Divide
            1220, // Concordia Wilderness
            1244, // Concordia Palisades
            1304, // Valverde Pools
            1454, // Valverde Marshes
            1497, // Valverde Plateau
            1734, // Ligo Ashen Desert
            1759, // Torden Mires
            1761, // Torden Incline
            1764, // Torden Plains
            1911, // Ligo Thunderhead
            1993, // Ligo Burning Steps (Crucible)
            2028, // Torden Abyss
            2047, // Valverde Descent
            2051  // Howling Maw
        };

        /// <summary>
        /// The Bane army's own classes, by the start of the client's class names: what the Bane ship or
        /// beam in. Their beasts (Fithik, Amoeboids, Howlers, Xanx, Mox, Miasma), the machina,
        /// emplacements and walkers are not; nor the native wildlife.
        /// </summary>
        private static readonly string[] BaneTroopPrefixes =
        {
            "Bane_Thrax_", "NPC_Bane_Thrax_", "NPC_Bane_Miniboss_Overseer", "Bane_Caretaker", "Bane_Hunter",
            "Bane_Shield_Drone", "Bane_Linker", "Bane_Lightbender", "Bane_Kael", "Bane_Sentinel",
            "Bane_Nitroglazer", "Bane_Pyroglazer", "Bane_Seeker", "Bane_Ichaiden"
        };

        /// <summary>The AFS's soldiers and walkers: what an AFS ship brings.</summary>
        private static readonly string[] AfsTroopPrefixes = { "Redshirt_Human_Soldier_", "Vehicle_AFS_Mech" };

        /// <summary>
        /// The AFS's side as the client names its classes, soldiers and allies alike (Redshirt_Human_,
        /// Redshirt_Forean_, Redshirt_Brann_, ...). Those whose class has a CREATURE_BIRTH have the Bane
        /// troops' one (animation family 622): the Forean warriors, gunners, archers and elders, the
        /// Brann, the human swapsets, the Heapsaw.
        /// </summary>
        private static readonly string[] AllyPrefixes = { "Redshirt_", "Vehicle_AFS_Mech" };

        #region What is what

        public static bool IsShipMap(MapChannel mapChannel)
        {
            var id = mapChannel?.MapInfo?.MapContextId ?? 0;

            return Config.ShipMaps != null && Config.ShipMaps.Count > 0
                ? Config.ShipMaps.Contains(id)
                : DefaultShipMaps.Contains(id);
        }

        private static string ClassNameOf(Creature creature)
        {
            return creature != null && EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(creature.EntityClass, out var info)
                ? info?.ClassName ?? ""
                : "";
        }

        private static bool Starts(string name, string[] prefixes) =>
            prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        /// <summary>One of the Bane army: a hostile creature of a Bane troop class.</summary>
        public static bool IsBaneTroop(Creature creature) =>
            creature != null && creature.TargetCategory == TargetCategory.Hostile && !Emplacements.Is(creature)
            && Starts(ClassNameOf(creature), BaneTroopPrefixes);

        /// <summary>One of the AFS's soldiers or walkers that can walk: a friendly creature of a soldier class.</summary>
        public static bool IsAfsTroop(Creature creature) =>
            creature != null && creature.TargetCategory == TargetCategory.Friendly && !Emplacements.Is(creature)
            && creature.WalkSpeed > 0.01f && Starts(ClassNameOf(creature), AfsTroopPrefixes);

        /// <summary>One of the AFS's side, soldier or ally: a friendly creature of an allied class.</summary>
        public static bool IsAlly(Creature creature) =>
            creature != null && creature.TargetCategory == TargetCategory.Friendly && !Emplacements.Is(creature)
            && Starts(ClassNameOf(creature), AllyPrefixes);

        /// <summary>A boss by its flags: no drop or raid is made of one.</summary>
        public static bool IsBoss(Creature template) =>
            CreatureManager.CreatureFlagsOf(template).Any(flag => flag == (int)CreatureFlag.Powerlevel6Boss || flag == (int)CreatureFlag.Powerlevel7Superboss);

        #endregion

        #region The arrival of a pool with no row of its own

        /// <summary>
        /// Where a pool's next spawn of <paramref name="creatures"/> arrives when it has no arrival point
        /// of its own (BaneArrivals.Pick found none): a spot a Bane or AFS ship comes down on, or the
        /// pool's own ground for them to beam in on - an arrival point of no table's, which
        /// SpawnPoolManager sends as it does a row's. Null when nothing of this is for it: they come as
        /// they always did.
        /// </summary>
        public static SpawnPoolArrivalEntry ArrivalFor(MapChannel mapChannel, SpawnPool pool, IReadOnlyList<Creature> creatures)
        {
            if (mapChannel == null || pool == null || creatures == null || creatures.Count == 0)
                return null;

            // A mission's own spawn is the mission's to stage; a pool its data animates (anim_type 1
            // Bane, 2 AFS) or gives arrival rows has its own way in; the world's first stocking is plain.
            if (pool.ScenarioKey != null || pool.SpawnPolicy == MissionSpawnGroupPolicy.ScenarioControlled
                || pool.FollowOwnerCharacterId != 0 || pool.FollowTargetEntityId != 0
                || pool.AnimType != 0 || pool.Arrivals.Count > 0 || !pool.HasSpawned && !pool.ShipNextArrival)
                return null;

            var shipMap = IsShipMap(mapChannel);
            byte? kind = null;

            if (Config.BaneDropships && shipMap && creatures.All(c => IsBaneTroop(c) && c.WalkSpeed > 0.01f))
                kind = SpawnPoolArrivalEntry.KindDropship;
            else if (Config.AfsDropships && shipMap && pool.IsGarrison && creatures.All(IsAfsTroop))
                kind = SpawnPoolArrivalEntry.KindAfsDropship;

            if (kind != null)
            {
                var spot = ShipSpot(mapChannel, pool, kind == SpawnPoolArrivalEntry.KindDropship);

                if (spot != null)
                    return ArrivalAt(pool, spot.Value, pool.Rotation, kind.Value);
            }

            // No ship, or nowhere to bring one down: troops beam in on their own ground.
            return BeamsIn(creatures) ? ArrivalAt(pool, pool.Position, pool.Rotation, SpawnPoolArrivalEntry.KindBeamIn) : null;
        }

        /// <summary>
        /// Where a pool's ship comes down: on its own ground with open sky over it - and a Bane ship for
        /// ground within an outpost's walls (OutpostWalls), or for a control point's garrison within
        /// walls, before its gates or outside them, never in the yard. Null with nowhere just now.
        /// </summary>
        private static Vector3? ShipSpot(MapChannel mapChannel, SpawnPool pool, bool bane)
        {
            Vector3? spot = null;

            if (bane)
            {
                if (OutpostWalls.Within(mapChannel, pool.Position))
                    return LandingOutsideWalls(mapChannel, pool.Position);

                if ((pool.IsGarrison || pool.Mode == SpawnPoolManager.ModeControlPoint) && Enclosed(mapChannel, pool.Position))
                    return RaidLanding(mapChannel, pool.Position, 30f, 60f, Random.Shared.NextDouble() * Math.PI * 2);
            }

            spot ??= LandingSpot(mapChannel, pool.Position, Math.Max(MinLandingRadius, pool.Radius));

            // A camp beside an outpost: somewhere round its ground may be over the wall.
            if (bane && spot != null && OutpostWalls.Within(mapChannel, spot.Value))
                spot = LandingOutsideWalls(mapChannel, spot.Value);

            return spot;
        }

        /// <summary>An arrival point of no table's: where a ship of ours comes down, or troops beam in.</summary>
        public static SpawnPoolArrivalEntry ArrivalAt(SpawnPool pool, Vector3 spot, double rotation, byte kind = SpawnPoolArrivalEntry.KindDropship)
        {
            return new SpawnPoolArrivalEntry
            {
                Id = 0,
                PoolId = pool?.DbId ?? 0,
                Kind = kind,
                PosX = spot.X,
                PosY = spot.Y,
                PosZ = spot.Z,
                Rotation = rotation,
                EntityId = 0,
                Comment = "reinforcements"
            };
        }

        /// <summary>Whose ship lands at an arrival point: the AFS's at an AFS one, the Bane's at any other.</summary>
        public static TargetCategory SideOf(SpawnPoolArrivalEntry arrival) =>
            arrival?.Kind == SpawnPoolArrivalEntry.KindAfsDropship ? TargetCategory.Friendly : TargetCategory.Hostile;

        /// <summary>Whether an arrival is a ship's, of either side.</summary>
        public static bool IsShip(SpawnPoolArrivalEntry arrival) =>
            arrival?.Kind == SpawnPoolArrivalEntry.KindDropship || arrival?.Kind == SpawnPoolArrivalEntry.KindAfsDropship;

        #endregion

        #region Where a ship comes down

        /// <summary>Whether a ship's hull at this spot would keep clear of every other ship on the map, of either side, until that one leaves.</summary>
        public static bool HasShipClearance(MapChannel mapChannel, Vector3 spot)
        {
            var separation = HullRadius * 2 + 4;

            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values)
            {
                if (ship.RuntimeMapChannel != null ? ship.RuntimeMapChannel != mapChannel : ship.MapContextId != (mapChannel.MapInfo?.MapContextId ?? 0))
                    continue;

                var dx = spot.X - ship.Position.X;
                var dz = spot.Z - ship.Position.Z;

                if (dx * dx + dz * dz < separation * separation)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether a ship can come down on this spot: it is not under the terrain (a cave, a tunnel), no
        /// other ship's hull is in the way (<see cref="HasShipClearance"/>), and nothing of the map's
        /// collision is over it for <see cref="ReinforcementConfig.SkyClearance"/> metres, nor round it
        /// where the hull hangs - a wall through a wing blocks it although the beam itself is clear.
        /// </summary>
        public static bool IsOpenSky(MapChannel mapChannel, Vector3 spot)
        {
            if (mapChannel == null || NavMeshManager.IsUnderground(mapChannel, spot))
                return false;

            if (!HasShipClearance(mapChannel, spot))
                return false;

            var cover = mapChannel.Cover;

            if (cover == null)
                return true;

            var clearance = Math.Max(HullHeight + 2f, Config.SkyClearance);
            var foot = spot + new Vector3(0, 1.5f, 0);
            var top = spot + new Vector3(0, clearance, 0);

            if (cover.GeometryBlocks(foot, top))
                return false;

            for (var i = 0; i < 16; i++)
            {
                var angle = i * Math.PI / 8;
                var side = new Vector3((float)Math.Cos(angle) * HullRadius, 0, (float)Math.Sin(angle) * HullRadius);

                if (cover.GeometryBlocks(spot + new Vector3(0, HullHeight, 0) + side, top + side))
                    return false;

                // Across the hull at the heights it hangs at: a wall beside the beam (the eight across
                // the circle are each a diameter, so half of them suffice).
                if (i >= 8)
                    continue;

                foreach (var height in HullCrossings)
                {
                    var centre = spot + Vector3.UnitY * height;

                    if (cover.GeometryBlocks(centre - side, centre + side))
                        return false;
                }
            }

            return true;
        }

        /// <summary>The heights over the spot at which the hull's width is looked across for collision (metres).</summary>
        private static readonly float[] HullCrossings = { HullHeight, 8f, 12f, 16f, 24f };

        /// <summary>
        /// Somewhere a ship can come down within <paramref name="radius"/> of <paramref name="centre"/>:
        /// on the navmesh, above ground, open sky over it. Null when nowhere tried is.
        /// </summary>
        public static Vector3? LandingSpot(MapChannel mapChannel, Vector3 centre, float radius)
        {
            if (mapChannel == null)
                return null;

            var anchor = NavMeshManager.NearestWalkable(mapChannel, centre, Math.Max(32f, radius)) ?? NavMeshManager.SnapToGround(mapChannel, centre);

            for (var attempt = 0; attempt < LandingAttempts; attempt++)
            {
                var candidate = attempt == 0
                    ? anchor
                    : NavMeshManager.RandomPointAround(mapChannel, anchor, radius) ?? anchor + SpawnPoolManager.InDisc(radius);

                candidate = NavMeshManager.SnapToGround(mapChannel, candidate);

                if (IsOpenSky(mapChannel, candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>Whether there is a way on foot from one spot to the other, ending within a few metres of it. True on a map with no navmesh.</summary>
        public static bool Reaches(MapChannel mapChannel, Vector3 from, Vector3 to)
        {
            if (mapChannel?.NavMesh == null)
                return true;

            // Bounded: a spot in another walkable region than the point (a wall top, a rock) is given up
            // on quickly, not searched across the whole map.
            var path = NavMeshManager.FindPathBounded(mapChannel, from, to, out var complete);

            return path != null && path.Count > 0 && complete && Vector3.Distance(path[^1], to) <= 6f;
        }

        /// <summary>
        /// Somewhere a ship can come down between <paramref name="minDistance"/> and <paramref name="maxDistance"/>
        /// from <paramref name="centre"/>, round it: open sky (<see cref="IsOpenSky"/>), on the navmesh, and -
        /// with <paramref name="mustReach"/> - with a way on foot from there to the centre. Tries
        /// <paramref name="attempts"/> bearings, starting at <paramref name="firstBearing"/> (radians), so that
        /// spots asked for one after another come down round the centre rather than in one place.
        /// </summary>
        public static Vector3? LandingRing(MapChannel mapChannel, Vector3 centre, float minDistance, float maxDistance,
            bool mustReach, double firstBearing = 0, int attempts = 16, Func<Vector3, bool> accept = null)
        {
            if (mapChannel == null)
                return null;

            var ground = NavMeshManager.SnapToGround(mapChannel, centre);

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var bearing = firstBearing + attempt * (Math.PI * 2 / Math.Max(1, attempts)) * 5 % (Math.PI * 2);
                var distance = minDistance + (float)Random.Shared.NextDouble() * Math.Max(0f, maxDistance - minDistance);
                var probe = ground + new Vector3((float)Math.Cos(bearing) * distance, 0, (float)Math.Sin(bearing) * distance);

                // The ground there: the walkable point in the column, nearest the centre's height.
                var candidate = NavMeshManager.NearestInColumn(mapChannel, probe, 60f) ?? NavMeshManager.SnapToGround(mapChannel, probe);

                if (Vector3.Distance(new Vector3(candidate.X, 0, candidate.Z), new Vector3(ground.X, 0, ground.Z)) < minDistance * 0.75f)
                    continue;

                if (!IsOpenSky(mapChannel, candidate))
                    continue;

                if (accept != null && !accept(candidate))
                    continue;

                if (mustReach && !Reaches(mapChannel, candidate, ground))
                    continue;

                return candidate;
            }

            return null;
        }

        /// <summary>How far round a spot walls are looked for (<see cref="Enclosed"/>).</summary>
        public const float WallSearch = 90f;

        /// <summary>
        /// Whether a spot stands within walls - a fort's, a compound's: of <paramref name="rays"/> level
        /// rays from 2 m above it, out to <paramref name="reach"/>, at least <paramref name="share"/> meet the
        /// map's collision. A point in the open is not; one in a walled base is. False with no cover mesh.
        /// </summary>
        public static bool Enclosed(MapChannel mapChannel, Vector3 spot, float share = 0.6f, int rays = 16, float reach = WallSearch)
        {
            var cover = mapChannel?.Cover;

            if (cover == null || rays <= 0)
                return false;

            var eye = spot + new Vector3(0, 2f, 0);
            var blocked = 0;

            for (var i = 0; i < rays; i++)
            {
                var angle = i * Math.PI * 2 / rays;

                if (cover.GeometryBlocks(eye, eye + new Vector3((float)Math.Cos(angle) * reach, 0, (float)Math.Sin(angle) * reach)))
                    blocked++;
            }

            return blocked >= share * rays;
        }

        /// <summary>
        /// Whether a spot is in a yard - walled in close round on most sides: of 16 level rays from 2 m
        /// above it, out to <see cref="GateYard"/>, more than half meet the map's collision. Looked at
        /// this near rather than as far as <see cref="WallSearch"/>, a clearing in the forest round the
        /// Landing Zone's fence, or before Imperial Valley's cliffs, is the open it is.
        /// </summary>
        public static bool InYard(MapChannel mapChannel, Vector3 spot) => Enclosed(mapChannel, spot, 0.55f, 16, GateYard);

        /// <summary>
        /// Whether a raid's ship may come down on a spot: anywhere round a point in the open; round one
        /// within walls (<see cref="Enclosed"/>: a fort, an outpost's compound) only outside them - a spot
        /// with a wall between it and the point that is in no yard itself (<see cref="InYard"/>). Never in
        /// the courtyard the raid has come to take.
        /// </summary>
        public static bool OutsideWalls(MapChannel mapChannel, Vector3 point, Vector3 spot)
        {
            var cover = mapChannel?.Cover;

            if (cover == null || !Enclosed(mapChannel, point))
                return true;

            return cover.GeometryBlocks(spot + new Vector3(0, 2f, 0), point + new Vector3(0, 2f, 0))
                   && !InYard(mapChannel, spot);
        }

        /// <summary>The furthest from its point a raid's ship comes down (metres): raiders set down 150 to 220 m off came running to the point one by one.</summary>
        public const float RaidLandingFurthest = 120f;

        /// <summary>
        /// Where a raid's ship comes down round a point: a ring <paramref name="min"/> to <paramref name="max"/>
        /// out with a way on foot to it, then wider - and, round a point within walls, only outside them
        /// (<see cref="OutsideWalls"/>), and never within any outpost's walls as its gates tell them
        /// (OutpostWalls). Null when there is no such spot within <see cref="RaidLandingFurthest"/>: the ship
        /// is not sent there (a raid's waits for room nearer by: BaneRaids).
        /// </summary>
        public static Vector3? RaidLanding(MapChannel mapChannel, Vector3 point, float min, float max, double bearing)
        {
            var cover = mapChannel?.Cover;
            Func<Vector3, bool> outside = null;

            // As OutsideWalls, with the point's walls looked at once and the one ray to it before the sixteen round the spot.
            if (cover != null && Enclosed(mapChannel, point))
            {
                var eye = point + new Vector3(0, 2f, 0);

                outside = spot => cover.GeometryBlocks(spot + new Vector3(0, 2f, 0), eye) && !InYard(mapChannel, spot);
            }

            bool Accept(Vector3 spot) => (outside == null || outside(spot)) && !OutpostWalls.Within(mapChannel, spot);

            // Always with a way on foot to the point: a spot on a wall top or a rock left its troops standing there.
            return LandingRing(mapChannel, point, min, max, mustReach: true, firstBearing: bearing, accept: Accept)
                   ?? LandingRing(mapChannel, point, Math.Max(max, 75f), RaidLandingFurthest, mustReach: true, firstBearing: bearing, attempts: 24, accept: Accept);
        }

        /// <summary>
        /// Where a Bane ship meant for a place within an outpost's walls (OutpostWalls) comes down instead:
        /// before one of the outpost's gates, the one nearest the place first (<see cref="GateLanding"/>), or
        /// outside its walls round it (<see cref="RaidLanding"/>); its troops walk in from there. The place
        /// itself when it is within no walls. Null when there is nowhere outside just now: the caller beams
        /// them in or waits, never sets the ship in the yard.
        /// </summary>
        public static Vector3? LandingOutsideWalls(MapChannel mapChannel, Vector3 spot)
        {
            var holder = OutpostWalls.Holder(mapChannel, spot);

            if (holder == null)
                return spot;

            var centre = holder.Object?.Position ?? holder.Position;

            foreach (var gate in OutpostWalls.GatesOf(holder).OrderBy(g => Vector3.DistanceSquared(g.Middle, spot)))
            {
                var before = GateLanding(mapChannel, centre, gate.Middle, gate.Through);

                if (before != null)
                    return before;
            }

            return RaidLanding(mapChannel, centre, 30f, 60f, Math.Atan2(spot.Z - centre.Z, spot.X - centre.X));
        }

        /// <summary>How far out from a gate a ship comes down, the first of these that has room for it (metres).</summary>
        public static readonly float[] GateLandingDistances = { 26f, 32f, 20f, 38f, 44f, 52f, 60f };

        /// <summary>How far to either side of the line straight out from a gate a ship may come down when the line itself has no room (metres).</summary>
        public const float GateLandingAside = 7f;

        /// <summary>
        /// How far to either side of that line a ship may come down, nearest first: on it, a little beside
        /// it where the line has no room, and a ship's width and more beside it - where the second and third
        /// ships of a wave come down while the first still stands before the gate (<see cref="HasShipClearance"/>
        /// keeps them 34 m apart).
        /// </summary>
        public static readonly float[] GateLandingAsides = { 0f, GateLandingAside, -GateLandingAside, 22f, -22f, 36f, -36f, 50f, -50f };

        /// <summary>How far out on the line (and a little beside it) a ship is looked for before the spots wide of it are (metres).</summary>
        public const float GateLandingNear = 44f;

        /// <summary>
        /// The spots before a gate in the order they are tried, (aside, distance out): on the line and a little
        /// beside it out to <see cref="GateLandingNear"/>; then wide of it, as far; then the same further out.
        /// So the second ship of a wave comes down beside the first, not out behind it.
        /// </summary>
        public static readonly (float Aside, float Distance)[] GateLandingSpots = new[] { true, false }
            .SelectMany(near => new[] { false, true }.SelectMany(wide => GateLandingAsides
                .Where(a => Math.Abs(a) > GateLandingAside == wide)
                .SelectMany(a => GateLandingDistances.Where(d => d <= GateLandingNear == near).Select(d => (a, d)))))
            .ToArray();

        /// <summary>
        /// How far round a spot before a gate walls are looked for, to tell a yard from the open: a yard's
        /// walls stand this near on most sides. At <see cref="WallSearch"/> the forest and the fort beside
        /// the Landing Zone's gate, and the cliffs of Imperial Valley, made nearly every spot before those
        /// gates "within walls"; at this the spots before them have 0 to 8 of 16 rays stopped and the yards
        /// behind the inner gates 9 to 16.
        /// </summary>
        public const float GateYard = 30f;

        /// <summary>
        /// The spot <paramref name="distance"/> metres out from a gate and <paramref name="aside"/> to the side
        /// of it: straight through the gate (<paramref name="through"/>, either way along it), on the side away
        /// from the point the gate guards - the gate stands between the spot and the point.
        /// </summary>
        public static Vector3 BeforeGate(Vector3 point, Vector3 gate, Vector3 through, float distance, float aside = 0f)
        {
            var level = new Vector3(through.X, 0f, through.Z);

            level = level.LengthSquared() > 1e-6f ? Vector3.Normalize(level) : Vector3.UnitZ;

            if (Vector3.Dot(level, new Vector3(gate.X - point.X, 0f, gate.Z - point.Z)) < 0f)
                level = -level;

            return gate + level * distance + new Vector3(-level.Z, 0f, level.X) * aside;
        }

        /// <summary>
        /// Where a ship comes down before a gate of an outpost: in front of the entrance, as the raids of the
        /// original came - straight out from the gate (<see cref="BeforeGate"/>) at the first of
        /// <see cref="GateLandingDistances"/>, or to either side of the line (<see cref="GateLandingAsides"/>),
        /// where a ship can come down (<see cref="IsOpenSky"/>), in the open rather than in a yard
        /// (<see cref="GateYard"/>), with a way on foot from there to the point, and within no outpost's
        /// walls. Null when that gate has no such spot.
        /// </summary>
        public static Vector3? GateLanding(MapChannel mapChannel, Vector3 point, Vector3 gate, Vector3 through)
        {
            if (mapChannel == null)
                return null;

            var ground = NavMeshManager.SnapToGround(mapChannel, point);

            foreach (var (aside, distance) in GateLandingSpots)
            {
                var probe = BeforeGate(point, gate, through, distance, aside);
                var candidate = NavMeshManager.NearestInColumn(mapChannel, probe, 60f) ?? NavMeshManager.SnapToGround(mapChannel, probe);

                if (!IsOpenSky(mapChannel, candidate) || InYard(mapChannel, candidate))
                    continue;

                if (!Reaches(mapChannel, candidate, ground))
                    continue;

                // Not in another yard of the outpost (an inner gate's far side) nor of one beside it.
                if (OutpostWalls.Within(mapChannel, candidate))
                    continue;

                return candidate;
            }

            return null;
        }

        #endregion

        #region A drop of our own

        /// <summary>
        /// Pool ids of no table's, for the pools a drop or a raid makes for itself: far above any row's, so
        /// nothing that finds a pool by its id finds one of these.
        /// </summary>
        public const uint FirstTransientPoolId = 4_000_000_000;

        private static long _nextTransientPoolId = FirstTransientPoolId;

        /// <summary>Whether a pool is one a drop or a raid made for itself, and no table's.</summary>
        public static bool IsTransient(SpawnPool pool) => pool != null && pool.DbId >= FirstTransientPoolId;

        /// <summary>
        /// A pool of no table's for one drop or one raid: its creatures' home is its ground
        /// (<paramref name="position"/>, <paramref name="radius"/>), it never restocks on its own (scripted)
        /// and is on no map's list of pools.
        /// </summary>
        public static SpawnPool TransientPool(MapChannel mapChannel, Vector3 position, double rotation, float radius,
            IEnumerable<SpawnPoolSlot> slots)
        {
            return new SpawnPool
            {
                DbId = (uint)Math.Min(uint.MaxValue, System.Threading.Interlocked.Increment(ref _nextTransientPoolId)),
                Position = position,
                Rotation = rotation,
                Radius = radius,
                SpawnSlot = slots?.ToList() ?? new List<SpawnPoolSlot>(),
                Mode = SpawnPoolManager.ModeScripted,
                AnimType = 0,
                MapContextId = mapChannel.MapInfo.MapContextId,
                RuntimeMapChannel = mapChannel,
                HasSpawned = true
            };
        }

        /// <summary>How many of the pools nearest a place speak for its troops (<see cref="TroopsNear"/>).</summary>
        public const int TroopPools = 8;

        /// <summary>
        /// The Bane troops of the ground round a place on its map: the creature rows the <see cref="TroopPools"/>
        /// nearest Bane troop pools within <paramref name="maxDistance"/> spawn, the commonest first (by how
        /// many of each those pools may hold), up to <paramref name="kinds"/> of them - what a drop or a raid
        /// there is made of, at the levels of that ground. The rank and file only: no boss, nothing that
        /// cannot walk.
        /// </summary>
        public static List<uint> TroopsNear(MapChannel mapChannel, Vector3 position, float maxDistance, int kinds = 4)
        {
            var weights = new Dictionary<uint, (int Weight, int First)>();

            if (mapChannel?.SpawnPools == null)
                return new List<uint>();

            var creatures = CreatureManager.Instance.LoadedCreatures;
            var order = 0;

            bool IsTroop(SpawnPoolSlot slot) =>
                slot != null && slot.CountMax > 0 && creatures.TryGetValue(slot.CreatureId, out var template)
                && IsBaneTroop(template) && template.WalkSpeed > 0.01f && !IsBoss(template);

            foreach (var pool in mapChannel.SpawnPools
                         .Where(p => p != null && p.SpawnSlot != null && p.ScenarioKey == null && !IsTransient(p) && p.SpawnSlot.Any(IsTroop))
                         .Select(p => (Pool: p, Distance: Vector3.Distance(p.Position, position)))
                         .Where(p => p.Distance <= maxDistance)
                         .OrderBy(p => p.Distance)
                         .Take(TroopPools)
                         .Select(p => p.Pool))
            {
                foreach (var slot in pool.SpawnSlot.Where(IsTroop))
                    weights[slot.CreatureId] = weights.TryGetValue(slot.CreatureId, out var had)
                        ? (had.Weight + slot.CountMax, had.First)
                        : (slot.CountMax, order++);
            }

            return weights.OrderByDescending(w => w.Value.Weight).ThenBy(w => w.Value.First)
                .Take(kinds).Select(w => w.Key).ToList();
        }

        /// <summary>
        /// Sends a ship of <paramref name="side"/> down at <paramref name="spot"/> with <paramref name="creatures"/>
        /// (templates, one entry each) of <paramref name="pool"/>: they are set down under it and walk to the
        /// pool's ground, which is their home from then on. Null when nothing was sent.
        /// </summary>
        public static Dropship Drop(MapChannel mapChannel, SpawnPool pool, List<Creature> creatures, Vector3 spot, double rotation, TargetCategory side)
        {
            if (mapChannel == null || pool == null || creatures == null || creatures.Count == 0)
                return null;

            var kind = side == TargetCategory.Friendly ? SpawnPoolArrivalEntry.KindAfsDropship : SpawnPoolArrivalEntry.KindDropship;

            return SpawnPoolManager.Instance.EnqueueDropship(mapChannel, pool, creatures, ArrivalAt(pool, spot, rotation, kind));
        }

        #endregion

        #region Beaming in

        /// <summary>
        /// Whether troops that come without a ship beam in: the setting is on and they are the Bane's troops
        /// or of the AFS's side - its allies too. Each plays only what its class has (<see cref="BeamIn"/>).
        /// </summary>
        public static bool BeamsIn(IReadOnlyList<Creature> creatures) =>
            Config.BeamIn && creatures != null && creatures.Count > 0 && creatures.All(c => IsBaneTroop(c) || IsAlly(c));

        /// <summary>
        /// A creature just set down plays its class's arrival, if its class has one: CREATURE_BIRTH (the
        /// effect, then the action's windup and recovery), seen by everyone who has it. Whether it had one.
        /// </summary>
        public static bool BeamIn(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null)
                return false;

            var classId = (uint)creature.EntityClass;

            if (!AbilityManager.Instance.TryGetLevel(BirthAction, classId, out var birth))
                return false;

            GameEffectManager.Instance.Attach(mapChannel, creature, new GameEffect
            {
                TypeId = BirthTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = (int)creature.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ExpiresTick = Environment.TickCount64 + Math.Max(1000, birth.RecoveryMs)
            });

            CellManager.Instance.CellCallMethod(mapChannel, creature, new PerformWindupPacket(PerformType.TwoArgs, BirthAction, classId));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new AbilityRecoveryPacket(BirthAction, classId, AbilityRecoveryPacket.HitDataKind.None));

            return true;
        }

        #endregion
    }
}
