namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Three places the client's map marks, whose teleporter rows were there under the right id
    /// and name and with no usable position, so that nothing stood for the marker.
    ///
    /// The map screen's status markers are tied to teleporter rows by position (MapMarkerPreloader:
    /// the nearest row of the marker's kind on its map, within 25 m). A row whose position is
    /// wrong or missing is never the nearest, and these three were passed over:
    ///
    ///  - 307, "Waypoint: Cuthah Scout Post (Control Point)", on Howling Maw (2051). Its z was
    ///    -42909375: -429.09375 with the decimal point lost. That is an exact 256th, as the row's
    ///    x is (-1134.9648 is -290551 / 256) and as a position taken from the wire is, and it
    ///    puts the pad 6.7 m from the marker (-1139.02, 245.78, -423.81). The pad was in no cell
    ///    a player can reach, the point (control_point 39) had its hospital and no waypoint, and
    ///    the marker had no row.
    ///  - 306, "Hospital: Drill Resonator (Control Point)", on Valverde Descent (2047): a row with
    ///    the name and no map. The marker (HOSPITAL) is at 36.27, 240.31, 161.86, where the
    ///    hospital's NPC already stands (spawnpool 500225). The point (control_point 38) had its
    ///    waypoint (305) and no hospital. The hospital window has it as graveyard 182
    ///    (HospitalGraveyards).
    ///  - 110, "Level 01: Control Room", on Purgas Station (1806): a row with the name and no map.
    ///    The marker is a HOSPITAL at 152.25, 73.58, 128.50, where its NPC already stands
    ///    (spawnpool 500164); the map's other hospital marker, "Level 01: Entrance Hall", is row
    ///    360. The waypoint a metre away (78, "Level 1: Control Room") is another row and stays.
    ///    The hospital window has no name for this one, and gives it a generic one.
    ///
    /// A hospital's position is its marker's: the hospitals that were matched are a median of
    /// 0.36 m from theirs. Its rotation - which way a player faces who goes back to life there -
    /// is in nothing of the client's, and is 0.
    ///
    /// Each row is linked to its marker, and the two that are a control point's to the point: the
    /// waypoint closes and the hospital is lost while the Bane hold it (ControlPoints).
    ///
    /// Row 86, "Waypoint: Outpost Aurora" on Thunderhead (1911), has the same fault in its z
    /// (-14538672 for -145.38672) and is left as it is: put right, it would be a second pad,
    /// one that is gained, 0.2 m from the local teleporter that is there (549), where the
    /// client's map marks a local teleporter and no waypoint. It is still a waypoint row of
    /// Thunderhead's that nobody can reach, which Thunderhead Cartographer asks for
    /// (WaypointTitles).
    ///
    /// The statements are safe to repeat, and exact in reverse: Down leaves each row as it was.
    /// No character has gained any of the three - two had no place and one was out of reach -
    /// so the character database has nothing to follow with.
    /// </summary>
    public static class MarkedTeleporters
    {
        private const string Marker = "insert into map_marker (marker_entity_id, map_context_id, marker_type, object_kind, object_id, match_distance, comment) values";
        private const string Link = "insert into control_point_link (control_point_id, kind, object_id) values";

        public static readonly string[] WorldUp =
        {
            // Cuthah Scout Post's waypoint: where it is, its marker, and its control point.
            "update teleporter set pos_z = -429.09375 where id = 307 and map_context_id = 2051;",
            "delete from map_marker where marker_entity_id = 134419591475383 and map_context_id = 2051;",
            Marker + " (134419591475383, 2051, 2, 1, 307, 6.73, 'Waypoint: Cuthah Scout Post (Control Point)');",
            "delete from control_point_link where control_point_id = 39 and kind = 4 and object_id = 307;",
            Link + " (39, 4, 307);",

            // Drill Resonator's hospital: a place, its marker, and its control point.
            "update teleporter set type = 5, pos_x = 36.271484, pos_y = 240.30869, pos_z = 161.86011, rotation = 0, map_context_id = 2047 where id = 306 and map_context_id = 0;",
            "delete from map_marker where marker_entity_id = 134419591468356 and map_context_id = 2047;",
            Marker + " (134419591468356, 2047, 3, 1, 306, 0, 'Hospital: Drill Resonator (Control Point)');",
            "delete from control_point_link where control_point_id = 38 and kind = 3 and object_id = 306;",
            Link + " (38, 3, 306);",

            // Purgas Station's control room: a hospital, and its marker.
            "update teleporter set type = 5, description = 'Hospital: Level 01: Control Room', pos_x = 152.2477, pos_y = 73.57834, pos_z = 128.49892, rotation = 0, map_context_id = 1806 where id = 110 and map_context_id = 0;",
            "delete from map_marker where marker_entity_id = 134419591470252 and map_context_id = 1806;",
            Marker + " (134419591470252, 1806, 3, 1, 110, 0, 'Hospital: Level 01: Control Room');"
        };

        public static readonly string[] WorldDown =
        {
            "delete from map_marker where marker_entity_id = 134419591470252 and map_context_id = 1806;",
            "update teleporter set type = 0, description = 'Level 01: Control Room', pos_x = 0, pos_y = 0, pos_z = 0, rotation = 0, map_context_id = 0 where id = 110 and map_context_id = 1806;",

            "delete from control_point_link where control_point_id = 38 and kind = 3 and object_id = 306;",
            "delete from map_marker where marker_entity_id = 134419591468356 and map_context_id = 2047;",
            "update teleporter set type = 0, pos_x = 0, pos_y = 0, pos_z = 0, rotation = 0, map_context_id = 0 where id = 306 and map_context_id = 2047;",

            "delete from control_point_link where control_point_id = 39 and kind = 4 and object_id = 307;",
            "delete from map_marker where marker_entity_id = 134419591475383 and map_context_id = 2051;",
            "update teleporter set pos_z = -42909375 where id = 307 and map_context_id = 2051;"
        };
    }
}
