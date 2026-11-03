using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The hospitals and waypoints Add_control_points left off their control points
    /// (Control_point_teleporter_links). ControlPointSeed linked a point's teleporters by how near
    /// they stood to its marker, and three stood further off than it looked:
    ///  - "Waypoint: Wilderness LZ" (61), 72 m from the Landing Zone's marker, inside the base's
    ///    fence beside its hospital (104), which was linked. With the Bane holding the point the
    ///    hospital closed and the waypoint went on taking players in.
    ///  - "Hospital: Iapyx (Control Point)" (238), 132 m from Iapyx's marker, inside the
    ///    point's own region (158.7 m) and nearer than its waypoint (237, 110 m), which was linked.
    ///  - "Waypoint: Eastern Rim (Control Point)" (325), the waypoint the client names for the
    ///    point, 193 m from its marker; Eastern Rim had its hospitals (326, 577) and no waypoint.
    ///    No other point is nearer it: Outpost Aurora, at 249 m, has its own (327).
    ///
    /// A link goes in only where its teleporter row is there, of its kind and on its point's map,
    /// and no point has it yet - a game master may have given it one. Down takes out these rows
    /// alone.
    /// </summary>
    public static class ControlPointTeleporterLinks
    {
        public sealed record Link(uint ControlPointId, byte Kind, uint TeleporterId, byte TeleporterType, uint MapContextId);

        public const uint LandingZone = 4;
        public const uint WildernessLzWaypoint = 61;

        private const byte HospitalType = 5;
        private const byte WaypointType = 2;

        public static readonly Link[] Links =
        {
            new(LandingZone, ControlPointLinkEntry.KindWaypoint, WildernessLzWaypoint, WaypointType, 1220),   // Waypoint: Wilderness LZ
            new(19, ControlPointLinkEntry.KindHospital, 238, HospitalType, 1759),                              // Hospital: Iapyx (Control Point)
            new(27, ControlPointLinkEntry.KindWaypoint, 325, WaypointType, 1911)                               // Waypoint: Eastern Rim (Control Point)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var link in Links)
                yield return $"INSERT INTO {ControlPointLinkEntry.TableName} (control_point_id, kind, object_id) " +
                    $"SELECT p.id, {link.Kind}, t.id FROM {ControlPointEntry.TableName} p, teleporter t " +
                    $"WHERE p.id = {link.ControlPointId} AND p.map_context_id = {link.MapContextId} " +
                    $"AND t.id = {link.TeleporterId} AND t.type = {link.TeleporterType} AND t.map_context_id = {link.MapContextId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {ControlPointLinkEntry.TableName} x WHERE x.object_id = {link.TeleporterId} " +
                    $"AND x.kind IN ({ControlPointLinkEntry.KindHospital}, {ControlPointLinkEntry.KindWaypoint}));";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var link in Links)
                yield return $"DELETE FROM {ControlPointLinkEntry.TableName} " +
                    $"WHERE control_point_id = {link.ControlPointId} AND kind = {link.Kind} AND object_id = {link.TeleporterId};";
        }
    }
}
