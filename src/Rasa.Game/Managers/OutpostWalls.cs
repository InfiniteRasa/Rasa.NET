using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Navigation;
    using Structures;

    /// <summary>
    /// Whether a place is within the walls of a control point's outpost, as the outpost's own gates tell
    /// it: within them is where every way out on foot to the open ground round the outpost passes through
    /// one of the point's gates - the openings the maps build in the outposts' fences and walls, each
    /// closed by a force field (ForceFields). Asked of every Bane ship's landing (Reinforcements), so that
    /// none comes down in the yard a raid has come to take, nor in the yard of a point the Bane hold.
    ///
    /// The gates are the fields that stand in them (<see cref="GatesOf"/>); while no field has been set in
    /// an outpost's gates it has none, and nothing is within its walls here - the cover mesh's rule
    /// (Reinforcements.Enclosed) still keeps a raid's ships out of a fort.
    ///
    /// The open ground is a ring of places on the navmesh <see cref="AnchorBeyond"/> past the point's
    /// furthest gate (<see cref="AnchorLeast"/> at the least) that a way on foot joins to the point: found
    /// once for each point and navmesh. A place is looked at against the <see cref="AnchorsTried"/> of them
    /// nearest its own bearing from the point: one that a way from the place reaches without passing a
    /// gate says it is outside. A place none of them can be reached from at all (a wall top, a roof) is
    /// taken as within: nothing should come down there.
    ///
    /// An outpost whose walls leave a side open has no "within" but its fenced pens: its yard is the
    /// open's, and the landing's other rules keep ships off the point itself. A point with no gates, a map
    /// with no navmesh: nothing is within anything here.
    /// </summary>
    public static class OutpostWalls
    {
        /// <summary>A place further than this from a point across the ground is never within its walls (metres).</summary>
        public const float Reach = 260f;

        /// <summary>How far past the furthest of a point's gates its open ground is looked for, and how far at the least (metres).</summary>
        public const float AnchorBeyond = 90f;
        public const float AnchorLeast = 150f;

        /// <summary>How many bearings round the point the open ground is looked for on.</summary>
        public const int AnchorBearings = 24;

        /// <summary>How many places of the open ground a place is tried against, the nearest its own bearing first.</summary>
        public const int AnchorsTried = 3;

        /// <summary>How near the end of a way a place of the open ground has to be for the way to reach it (metres).</summary>
        public const float Arrive = 6f;

        /// <summary>How far from a point's object a force field on its map is taken for one of its outpost's gates, while nothing names them (metres).</summary>
        public const float GateReach = 120f;

        /// <summary>
        /// One gate of an outpost: the force field's class, where it stands and its yaw - the sheet that
        /// closes the opening - with the middle of the opening and the level direction straight through it.
        /// </summary>
        public sealed class Gate
        {
            public ForceFields.FieldClass Class { get; init; }
            public Vector3 Position { get; init; }
            public float Yaw { get; init; }

            /// <summary>
            /// The middle of the field's sheet - a field's own origin is not always its middle: the Bane's
            /// minor entrance hangs to one side of it.
            /// </summary>
            public Vector3 Middle
            {
                get
                {
                    var middle = new Vector3((Class.Min.X + Class.Max.X) * 0.5f, 0f, (Class.Min.Z + Class.Max.Z) * 0.5f);

                    return Position + Vector3.Transform(middle, Quaternion.CreateFromYawPitchRoll(Yaw, 0f, 0f));
                }
            }

            /// <summary>The level direction through the sheet (its local Z); which way is out is the asker's to say (Reinforcements.BeforeGate).</summary>
            public Vector3 Through => Vector3.Transform(Vector3.UnitZ, Quaternion.CreateFromYawPitchRoll(Yaw, 0f, 0f));

            public static Gate Of(ForceFields.Field field) => new Gate { Class = field.Class, Position = field.Position, Yaw = field.Yaw };
        }

        /// <summary>
        /// A point's gate shields on its map's own channel, standing or broken. Ours, while nothing names a
        /// point's own fields: every force field of the faction kind on the point's map channel within
        /// <see cref="GateReach"/> of its object. Replaceable - by what names a point's own fields (the
        /// control point defences' ControlPointFields.Of), and for tests.
        /// </summary>
        public static Func<ControlPoints.Point, IReadOnlyList<ForceFields.Field>> FieldsOf { get; set; } = FieldsNear;

        /// <summary>A point's gates: the openings its shields close (<see cref="FieldsOf"/>).</summary>
        public static IReadOnlyList<Gate> GatesOf(ControlPoints.Point point) =>
            (FieldsOf(point) ?? NoFields).Where(field => field?.Object != null && field.Class != null).Select(Gate.Of).ToList();

        private static readonly IReadOnlyList<ForceFields.Field> NoFields = Array.Empty<ForceFields.Field>();

        private static IReadOnlyList<ForceFields.Field> FieldsNear(ControlPoints.Point point)
        {
            var mapChannel = point?.Map;

            if (mapChannel == null)
                return NoFields;

            var centre = point.Object?.Position ?? point.Position;

            return ForceFields.OnMap(mapChannel)
                .Where(field => field?.Object != null && field.Class.Kind == ForceFields.Kind.Faction && Flat(field.Position - centre) <= GateReach)
                .ToList();
        }

        private static readonly ConditionalWeakTable<NavMeshQuery, Dictionary<(uint Point, int Gates), Vector3[]>> Anchors = new();

        /// <summary>The point within whose walls the place is, of the map's points; null when it is within none.</summary>
        public static ControlPoints.Point Holder(MapChannel mapChannel, Vector3 spot)
        {
            if (mapChannel?.MapInfo == null || mapChannel.NavMesh == null || mapChannel.IsCopy)
                return null;

            foreach (var point in ControlPoints.Instance.OnMap(mapChannel.MapInfo.MapContextId))
                if (!point.IsBattleground && point.Map == mapChannel && Holds(mapChannel, point, spot))
                    return point;

            return null;
        }

        /// <summary>Whether the place is within the walls of any of the map's points.</summary>
        public static bool Within(MapChannel mapChannel, Vector3 spot) => Holder(mapChannel, spot) != null;

        /// <summary>Whether the place is within the walls of this point's outpost.</summary>
        public static bool Holds(MapChannel mapChannel, ControlPoints.Point point, Vector3 spot)
        {
            if (mapChannel?.NavMesh == null || point == null)
                return false;

            var centre = point.Object?.Position ?? point.Position;

            // Cheap first: most places asked about are nowhere near the point.
            if (Flat(spot - centre) > Reach)
                return false;

            var gates = GatesOf(point);

            if (gates == null || gates.Count == 0)
                return false;

            var anchors = AnchorsOf(mapChannel, point, centre, gates);

            // No open ground joined to the point at all: only what is nearer than its gates is within.
            if (anchors.Length == 0)
                return Flat(spot - centre) <= Furthest(gates, centre) + 10f;

            var bearing = MathF.Atan2(spot.Z - centre.Z, spot.X - centre.X);

            foreach (var anchor in anchors.OrderBy(a => Turn(bearing, MathF.Atan2(a.Z - centre.Z, a.X - centre.X))).Take(AnchorsTried))
            {
                var way = Way(mapChannel.NavMesh, spot, anchor);

                if (way != null && !PathCrosses(way, gates))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether a way on foot passes through one of these gates: a step of it crosses a gate's sheet at
        /// the sheet's middle height (a way follows the ground, the sheet may hang over a step or a ramp),
        /// when the step is near the sheet's height at all.
        /// </summary>
        public static bool PathCrosses(IReadOnlyList<Vector3> path, IReadOnlyList<Gate> gates)
        {
            if (path == null || gates == null)
                return false;

            for (var i = 1; i < path.Count; i++)
                foreach (var gate in gates)
                {
                    var low = gate.Position.Y + gate.Class.Min.Y - 5f;
                    var high = gate.Position.Y + gate.Class.Max.Y;

                    if (MathF.Max(path[i - 1].Y, path[i].Y) < low || MathF.Min(path[i - 1].Y, path[i].Y) > high)
                        continue;

                    var middle = gate.Position.Y + (gate.Class.Min.Y + gate.Class.Max.Y) * 0.5f;

                    if (ForceFields.Crosses(gate.Class, gate.Position, gate.Yaw, new Vector3(path[i - 1].X, middle, path[i - 1].Z), new Vector3(path[i].X, middle, path[i].Z)))
                        return true;
                }

            return false;
        }

        /// <summary>The open ground round a point (see the class): found once for each point, set of gates and navmesh.</summary>
        internal static Vector3[] AnchorsOf(MapChannel mapChannel, ControlPoints.Point point, Vector3 centre, IReadOnlyList<Gate> gates)
        {
            var byPoint = Anchors.GetValue(mapChannel.NavMesh, _ => new Dictionary<(uint, int), Vector3[]>());
            var key = (point.Id, gates.Count);

            lock (byPoint)
            {
                if (byPoint.TryGetValue(key, out var known))
                    return known;
            }

            var radius = MathF.Max(AnchorLeast, Furthest(gates, centre) + AnchorBeyond);
            var ground = NavMeshManager.SnapToGround(mapChannel, centre);
            var found = new List<Vector3>();

            for (var i = 0; i < AnchorBearings; i++)
            {
                var angle = i * MathF.PI * 2f / AnchorBearings;
                var probe = ground + new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
                var place = NavMeshManager.NearestInColumn(mapChannel, probe, 80f);

                if (place == null || Flat(place.Value - centre) < radius * 0.8f)
                    continue;

                if (Way(mapChannel.NavMesh, place.Value, ground) != null)
                    found.Add(place.Value);
            }

            var anchors = found.ToArray();

            lock (byPoint)
                byPoint[key] = anchors;

            return anchors;
        }

        /// <summary>A way on foot from one place to the other, the first place first; null when none reaches the other.</summary>
        private static List<Vector3> Way(NavMeshQuery navMesh, Vector3 from, Vector3 to)
        {
            var way = navMesh.FindPathBounded(from, to, out var complete);

            if (way == null || way.Count == 0 || !complete || Vector3.Distance(way[^1], to) > Arrive)
                return null;

            way.Insert(0, from);

            return way;
        }

        private static float Furthest(IReadOnlyList<Gate> gates, Vector3 centre) =>
            gates.Count == 0 ? 0f : gates.Max(gate => Flat(gate.Middle - centre));

        private static float Flat(Vector3 v) => MathF.Sqrt(v.X * v.X + v.Z * v.Z);

        private static float Turn(float a, float b)
        {
            var d = MathF.Abs(a - b) % (MathF.PI * 2f);

            return d > MathF.PI ? MathF.PI * 2f - d : d;
        }
    }
}
