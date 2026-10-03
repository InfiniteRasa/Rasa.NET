namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Waypoints and hospitals moved onto the ids the client knows them by.
    ///
    /// The client names a waypoint from waypointlanguage by its id, in "You just gained local
    /// waypoint ..." and in every row of the waypoint window, and a hospital the same way in
    /// "You just gained ...". Its table ends at 533. Ten waypoints were placed under ids after
    /// that (534-624) and read "ERROR- 23- Missing translation for waypointlanguage ID 534".
    /// What each one is comes from the client's own map markers (generated.client.uimapmarker),
    /// which have a marker at each of these places:
    ///
    ///  - 575 and 622, Tampeii Settlement (ABYSS_WAYPOINT_TAMPEII): one pad, entered twice. The
    ///    client's id is 375, "Tampeii Cavern", between the settlement's hospital (374) and
    ///    Archeron Outpost (376).
    ///  - 589, Plains Post on Torden Incline (INCLINE_PLAINS_WAY): the client's 150, "Plains
    ///    Post". Row 150 was on Torden Plains at Irendas Penal Colony (PLAINS_IRENDAS_WAY), which
    ///    is the client's 75, "Irendas Colony". The hospitals had the same mix-up: 145 "Plains
    ///    Post Medical Tent" was Irendas' (the client's 153, "Irendas Colony Hospital"), and the
    ///    one at Plains Post was 590.
    ///  - 582, Stalker Woods (MARSHES_WAYPOINT_STALKER_WOODS), was on the Bane Supply Depot's
    ///    map (1451); the marker is on Valverde Marshes (1454). The one Marshes waypoint name the
    ///    client has left over is 137, "Waypoint: High Point Retreat", and this is the high one
    ///    (y 241 against 217-219 for the rest): taken as that, by elimination.
    ///  - 583, Exodus Point (MARSHES_WAYPOINT_EXODUSPOINT): a second marker on the Paludos
    ///    waypoint, 3.5 m from row 135. Not a pad of its own; 135 is the waypoint.
    ///  - 607, Velon Hollow ("Portable Waypoint: Velon Hollow"): the client has fourteen ids
    ///    named "Portable Waypoint" and nothing says which is this one. 418, the first after the
    ///    hollow's hospital (414).
    ///  - 541, Ashoka Lower, and 576, Larai Outpost, are local teleporters, not waypoints (the
    ///    markers are LOCAL_TELEPORTER: ASHEN_DESERT_TP_LOCAL_ASHOKA_BOTTOM,
    ///    THUNDERHEAD_TELEPORTER_LARAI): type 1, as the others are, which is never gained. 613,
    ///    Ashoka Upper, is the other end of 541 and was a hospital on Omega Labs (2203): type 1
    ///    on Ashen Desert (1734).
    ///  - 534, Staging Point, and 624, Viands Village: the client has a marker for each and a
    ///    name for neither, under any id. Kept, under two names it has no place for: 107 "AFS
    ///    Preparation Camp" and 415 "Rendezvous Point".
    ///
    /// A row takes its new id by replacing the placeholder that held the name (a row with no
    /// map); the map markers that stood for it follow, and Stalker Woods gets the marker its
    /// wrong map kept it from. The character database follows with <see cref="CharUp"/>.
    ///
    /// The world statements are safe to repeat, and exact in reverse: Down puts back each
    /// teleporter row as it was. The character ones are not safe to repeat (150 is two places in
    /// turn); the script for a MySQL server guards them by the migration history.
    /// </summary>
    public static class ClientWaypointIds
    {
        private const string Placeholder = "insert into teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) values";

        public static readonly string[] WorldUp =
        {
            // Tampeii Settlement: 575 (the pad) becomes 375; 622 is the same place again.
            "delete from teleporter where id = 375 and map_context_id = 0;",
            "delete from teleporter where id = 622;",
            "update teleporter set id = 375, description = 'Waypoint: Tampeii Settlement' where id = 575;",
            "update map_marker set object_id = 375 where object_kind = 1 and object_id = 622;",

            // Irendas Colony's waypoint was 150; Plains Post, on Incline, is 150.
            "delete from teleporter where id = 75 and map_context_id = 0;",
            "update teleporter set id = 75, description = 'Waypoint: Irendas Colony' where id = 150 and map_context_id = 1764;",
            "update teleporter set id = 150, description = 'Waypoint: Plains Post' where id = 589;",
            "update map_marker set object_id = 75, comment = 'Waypoint: Irendas Colony' where object_kind = 1 and object_id = 150 and map_context_id = 1764;",
            "update map_marker set object_id = 150, comment = 'Waypoint: Plains Post' where object_kind = 1 and object_id = 589;",

            // And their hospitals: Irendas' was 145; Plains Post's is 145.
            "delete from teleporter where id = 153 and map_context_id = 0;",
            "update teleporter set id = 153, description = 'Hospital: Irendas Colony' where id = 145 and map_context_id = 1764;",
            "update teleporter set id = 145, description = 'Hospital: Plains Post Medical Tent' where id = 590;",
            "update map_marker set object_id = 153, comment = 'Hospital: Irendas Colony' where object_kind = 1 and object_id = 145 and map_context_id = 1764;",
            "update map_marker set object_id = 145, comment = 'Hospital: Plains Post Medical Tent' where object_kind = 1 and object_id = 590;",

            // Stalker Woods: 137, on Valverde Marshes, with its marker.
            "delete from teleporter where id = 137 and map_context_id = 0;",
            "update teleporter set id = 137, map_context_id = 1454, description = 'Waypoint: Stalker Woods (High Point Retreat)' where id = 582;",
            "delete from map_marker where marker_entity_id = 134084584059679 and map_context_id = 1454;",
            "insert into map_marker (marker_entity_id, map_context_id, marker_type, object_kind, object_id, match_distance, comment) values (134084584059679, 1454, 2, 1, 137, 0, 'Waypoint: Stalker Woods');",

            // Exodus Point is the Paludos waypoint: its marker goes to 135.
            "delete from teleporter where id = 583;",
            "update map_marker set object_id = 135, match_distance = 3.55, comment = 'Waypoint Paludos' where object_kind = 1 and object_id = 583;",

            // Velon Hollow's portable waypoint.
            "delete from teleporter where id = 418 and map_context_id = 0;",
            "update teleporter set id = 418, description = 'Portable Waypoint: Velon Hollow' where id = 607;",
            "update map_marker set object_id = 418, comment = 'Portable Waypoint: Velon Hollow' where object_kind = 1 and object_id = 607;",

            // Local teleporters, not waypoints (and not a hospital).
            "update teleporter set type = 1, description = 'Local Teleporter: Ashoka Lower' where id = 541;",
            "update teleporter set type = 1, map_context_id = 1734 where id = 613;",
            "update teleporter set type = 1, description = 'Local Teleporter: Larai Outpost' where id = 576;",

            // Staging Point and Viands Village, under names the client has spare.
            "delete from teleporter where id = 107 and map_context_id = 0;",
            "update teleporter set id = 107, description = 'Waypoint: Staging Point (AFS Preparation Camp)' where id = 534;",
            "update map_marker set object_id = 107 where object_kind = 1 and object_id = 534;",
            "delete from teleporter where id = 415 and map_context_id = 0;",
            "update teleporter set id = 415, description = 'Waypoint: Viands Village (Rendezvous Point)' where id = 624;",
            "update map_marker set object_id = 415, comment = 'Waypoint: Viands Village' where object_kind = 1 and object_id = 624;"
        };

        public static readonly string[] WorldDown =
        {
            "update teleporter set id = 624, description = 'I thnk Viands Village' where id = 415 and map_context_id = 1244;",
            Placeholder + " (415, 0, 0, 'Rendezvous Point', 0, 0, 0, 0, 0);",
            "update map_marker set object_id = 624, comment = 'I thnk Viands Village' where object_kind = 1 and object_id = 415;",
            "update teleporter set id = 534, description = 'Waypoint: Staging Point' where id = 107 and map_context_id = 1244;",
            Placeholder + " (107, 0, 0, 'AFS Preparation Camp', 0, 0, 0, 0, 0);",
            "update map_marker set object_id = 534 where object_kind = 1 and object_id = 107;",

            "update teleporter set type = 2, description = 'Waypoint: THUNDERHEAD_WAYPOINT_LARAI' where id = 576;",
            "update teleporter set type = 5, map_context_id = 2203 where id = 613;",
            "update teleporter set type = 2, description = 'Waypoint: Ashoka Lower Teleporter' where id = 541;",

            "update teleporter set id = 607, description = 'Waypoint: PLATEAU_VELONHOLLOW_WAYPOINT' where id = 418 and map_context_id = 2136;",
            Placeholder + " (418, 0, 0, 'Portable Waypoint', 0, 0, 0, 0, 0);",
            "update map_marker set object_id = 607, comment = 'Waypoint: PLATEAU_VELONHOLLOW_WAYPOINT' where object_kind = 1 and object_id = 418;",

            Placeholder + " (583, 25651, 2, 'Waypoint: MARSHES_WAYPOINT_EXODUSPOINT', -307.79513, 218.80331, 96.7045, 0, 1454);",
            "update map_marker set object_id = 583, match_distance = 0, comment = 'Waypoint: MARSHES_WAYPOINT_EXODUSPOINT' where marker_entity_id = 134084584059680 and map_context_id = 1454;",

            "delete from map_marker where marker_entity_id = 134084584059679 and map_context_id = 1454;",
            "update teleporter set id = 582, map_context_id = 1451, description = 'Waypoint: MARSHES_WAYPOINT_STALKER_WOODS' where id = 137 and map_context_id = 1454;",
            Placeholder + " (137, 0, 0, 'Waypoint: High Point Retreat', 0, 0, 0, 0, 0);",

            "update teleporter set id = 590, description = 'Hospital: INCLINE_HOSPITAL_PLAINS_POST' where id = 145 and map_context_id = 1761;",
            "update teleporter set id = 145, description = 'Hospital: Plains Post Medical Tent' where id = 153 and map_context_id = 1764;",
            Placeholder + " (153, 0, 0, 'Irendas Colony Hospital', 0, 0, 0, 0, 0);",
            "update map_marker set object_id = 590, comment = 'Hospital: INCLINE_HOSPITAL_PLAINS_POST' where object_kind = 1 and object_id = 145 and map_context_id = 1761;",
            "update map_marker set object_id = 145, comment = 'Hospital: Plains Post Medical Tent' where object_kind = 1 and object_id = 153;",

            "update teleporter set id = 589, description = 'Waypoint: INCLINE_PLAINS_WAY' where id = 150 and map_context_id = 1761;",
            "update teleporter set id = 150, description = 'Waypoint: Plains Post' where id = 75 and map_context_id = 1764;",
            Placeholder + " (75, 0, 0, 'Irendas Colony', 0, 0, 0, 0, 0);",
            "update map_marker set object_id = 589, comment = 'Waypoint: INCLINE_PLAINS_WAY' where object_kind = 1 and object_id = 150 and map_context_id = 1761;",
            "update map_marker set object_id = 150, comment = 'Waypoint: Plains Post' where object_kind = 1 and object_id = 75;",

            "update teleporter set id = 575, description = 'Waypoint: Tampeii Settlement waypoint' where id = 375 and map_context_id = 2028;",
            Placeholder + " (622, 0, 2, 'Waypoint: Tampeii Settlement', 408.89062, 542.28125, -114.91406, 0, 2028);",
            Placeholder + " (375, 0, 0, 'Tampeii Cavern', 0, 0, 0, 0, 0);",
            "update map_marker set object_id = 622 where object_kind = 1 and object_id = 375;"
        };

        /// <summary>
        /// The character database: what each character has gained (character_teleporter), under
        /// the new ids. In this order - 150 is first what Irendas was and then what Plains Post
        /// is. A character who had both of two rows that are now one keeps one. The local
        /// teleporters and the hospital that was none are nobody's to have gained.
        /// </summary>
        public static readonly string[] CharUp = Concat(
            Move(575, 375), Move(622, 375),
            Move(150, 75), Move(589, 150),
            Move(145, 153), Move(590, 145),
            Move(582, 137), Move(583, 135), Move(607, 418), Move(534, 107), Move(624, 415),
            new[] { "delete from character_teleporter where waypointId in (541, 576, 613);" });

        /// <summary>
        /// Back to the old ids. What Up merged or deleted stays so: a character who had gained
        /// both Tampeii rows has 575 back and not 622, Exodus Point (583) stays Paludos (135),
        /// and 541, 576 and 613 are not given back.
        /// </summary>
        public static readonly string[] CharDown = Concat(
            Move(415, 624), Move(107, 534), Move(418, 607), Move(137, 582),
            Move(145, 590), Move(153, 145),
            Move(150, 589), Move(75, 150),
            Move(375, 575));

        /// <summary>
        /// Every character's row for one id becomes the other; a character who has both keeps the
        /// one. The inner select is DISTINCT so that MySQL keeps it a table of its own: merged
        /// into the delete it would be reading the table it deletes from (error 1093).
        /// </summary>
        private static string[] Move(uint from, uint to)
        {
            return new[]
            {
                $"delete from character_teleporter where waypointId = {from} and character_id in (select character_id from (select distinct character_id from character_teleporter where waypointId = {to}) as held);",
                $"update character_teleporter set waypointId = {to} where waypointId = {from};"
            };
        }

        private static string[] Concat(params string[][] parts)
        {
            var all = new System.Collections.Generic.List<string>();

            foreach (var part in parts)
                all.AddRange(part);

            return all.ToArray();
        }
    }
}
