using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// What the Edmund Range match (the game server's Battlegrounds) stands on, for map 2374:
    /// the rows of Add_edmund_range_match, inserted as SQL.
    ///
    ///  - The Simulated Bane the client has names for (creaturenamelanguage 10727-10733:
    ///    "Simulated Thrax Rifleman", "Simulated Thrax Grunt", "Simulated Thrax Technician",
    ///    "Simulated Caretaker"), 595001-595004: each a copy of the Howling Maw's row of its kind
    ///    (531029-531032), with its stats and appearance, under the Simulated name.
    ///  - Two spawn pools of them at each of the three control points, 595101-595106, three
    ///    to a pool, mode 1: they stand while the point is nobody's (Battlegrounds).
    ///  - The control points Whiskey, Charlie and Echo, 101-103, where the client's markers put
    ///    them, as the ownable control point object (class 10000071, the one kind with a state
    ///    for a team's), with their links: the pools as the Bane's
    ///    garrison, Whiskey's and Echo's hospitals (teleporters 533 and 526) and the three
    ///    waypoints (528, 531, 532). The client's own ids for the three (controlpointdata 12,
    ///    10, 11) are the game server's to know.
    ///  - Four map links inside the map, 9003-9006: the Red and Blue teleporters in the staging
    ///    area (kinds 2 and 3), each to its team's base where the base's waypoint row puts the
    ///    arrival (teleporters 492 and 491), and the way back from each base to the staging
    ///    area (kind 4) by the "Return to Staging Area" sign the map has in it.
    ///
    /// The control points and the base arrivals are the client's positions. The teleporters in
    /// the staging area and where a returning player arrives are set by hand from the map's
    /// static objects - the red and blue arrows, the signs - and its navmesh, and want a
    /// walk-through; so do the pools.
    /// </summary>
    public static class EdmundRangeSeed
    {
        public const uint MapContextId = 2374;

        public const uint FirstCreatureId = 595001;
        public const uint LastCreatureId = 595004;

        public const uint FirstPoolId = 595101;
        public const uint LastPoolId = 595106;

        public const uint WhiskeyId = 101;
        public const uint CharlieId = 102;
        public const uint EchoId = 103;

        public const uint RedEntranceLinkId = 9003;
        public const uint BlueEntranceLinkId = 9004;
        public const uint RedReturnLinkId = 9005;
        public const uint BlueReturnLinkId = 9006;

        /// <summary>The second set of team teleporters, on the staging area's upper floor (Add_edmund_range_upper_floor).</summary>
        public const uint RedUpperLinkId = 9007;
        public const uint BlueUpperLinkId = 9008;

        /// <summary>Where the winning team is put when a match ends (Add_edmund_range_upper_floor).</summary>
        public const uint WinnersLinkId = 9009;

        /// <summary>A map link's kind: the Red team's teleporter in a battleground's staging area.</summary>
        public const byte KindTeamRed = 2;

        /// <summary>A map link's kind: the Blue team's teleporter in a battleground's staging area.</summary>
        public const byte KindTeamBlue = 3;

        /// <summary>A map link's kind: the way from a team's base back to the staging area, off the team.</summary>
        public const byte KindTeamLeave = 4;

        /// <summary>A map link's kind: where the winning team of a match is put when it ends - its arrival. Never walked into.</summary>
        public const byte KindTeamWinners = 5;

        /// <summary>New creature id, the row it is a copy of, its comment, its name id.</summary>
        public static readonly (uint Id, uint CopyOf, string Comment, uint NameId)[] Creatures =
        {
            (595001, 531029, "Simulated Thrax Rifleman", 10731),
            (595002, 531030, "Simulated Thrax Grunt", 10730),
            (595003, 531031, "Simulated Thrax Technician", 10732),
            (595004, 531032, "Simulated Caretaker", 10727)
        };

        private static readonly string[] PoolColumns =
        {
            "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
            "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
            "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
            "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
            "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
            "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
            "creature_6_Id", "creature_6_min_count", "creature_6_max_count",
            "radius"
        };

        private static object[] Pool(uint id, double x, double y, double z, uint first, uint second) => new object[]
        {
            id, 1, 0, 600, x, y, z, 0.0, MapContextId,
            first, 2, 2, second, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4.0
        };

        public static readonly object[][] Pools =
        {
            Pool(595101, -282.0, 421.2, -87.0, 595001, 595003),
            Pool(595102, -294.0, 421.2, -103.0, 595002, 595004),
            Pool(595103, -54.0, 430.2, 52.0, 595001, 595003),
            Pool(595104, -66.0, 430.2, 28.0, 595002, 595004),
            Pool(595105, 176.0, 420.8, 187.0, 595001, 595003),
            Pool(595106, 164.0, 420.8, 163.0, 595002, 595004)
        };

        private static readonly string[] PointColumns =
        {
            "id", "map_context_id", "name", "class_id", "pos_x", "pos_y", "pos_z", "rotation", "marker_entity_id", "default_owner"
        };

        public static readonly object[][] Points =
        {
            new object[] { WhiskeyId, MapContextId, "Whiskey", 10000071u, -287.959, 423.258, -95.099, 0.0, 134419591466262UL, (byte)0 },
            new object[] { CharlieId, MapContextId, "Charlie", 10000071u, -60.0, 431.754, 40.0, 0.0, 134419591466263UL, (byte)0 },
            new object[] { EchoId, MapContextId, "Echo", 10000071u, 170.0, 423.163, 175.0, 0.0, 134419591466264UL, (byte)0 }
        };

        private static readonly string[] LinkColumns = { "control_point_id", "kind", "object_id" };

        public static readonly object[][] Links =
        {
            new object[] { WhiskeyId, ControlPointLinkEntry.KindBanePool, 595101u },
            new object[] { WhiskeyId, ControlPointLinkEntry.KindBanePool, 595102u },
            new object[] { WhiskeyId, ControlPointLinkEntry.KindHospital, 533u },
            new object[] { WhiskeyId, ControlPointLinkEntry.KindWaypoint, 528u },
            new object[] { CharlieId, ControlPointLinkEntry.KindBanePool, 595103u },
            new object[] { CharlieId, ControlPointLinkEntry.KindBanePool, 595104u },
            new object[] { CharlieId, ControlPointLinkEntry.KindWaypoint, 531u },
            new object[] { EchoId, ControlPointLinkEntry.KindBanePool, 595105u },
            new object[] { EchoId, ControlPointLinkEntry.KindBanePool, 595106u },
            new object[] { EchoId, ControlPointLinkEntry.KindHospital, 526u },
            new object[] { EchoId, ControlPointLinkEntry.KindWaypoint, 532u }
        };

        private static readonly string[] MapLinkColumns =
        {
            "id", "map_context_id", "pos_x", "pos_y", "pos_z", "radius", "dest_map_context_id", "dest_pos_x", "dest_pos_y", "dest_pos_z",
            "dest_rotation", "kind", "enabled", "comment"
        };

        // A link reaches 12 m up and down (MapLinkManager.VerticalTolerance). The ways back are
        // set 6 m under the floor of the bases, 420.6, so that they take in somebody standing on
        // it and not somebody on the roof, 8 m above.
        public static readonly object[][] MapLinks =
        {
            new object[] { RedEntranceLinkId, MapContextId, -36.0, 359.7, -343.0, 3.0, MapContextId, 282.05, 420.5, 34.28, 1.5708, KindTeamRed, (byte)1, "edmundrange2 staging -> red team" },
            new object[] { BlueEntranceLinkId, MapContextId, -88.0, 359.7, -343.0, 3.0, MapContextId, -401.96, 420.5, 45.68, -1.5708, KindTeamBlue, (byte)1, "edmundrange2 staging -> blue team" },
            new object[] { RedReturnLinkId, MapContextId, 274.0, 414.6, 39.75, 1.5, MapContextId, -38.0, 359.8, -395.0, 3.1416, KindTeamLeave, (byte)1, "edmundrange2 red base -> staging" },
            new object[] { BlueReturnLinkId, MapContextId, -394.47, 414.6, 40.59, 1.5, MapContextId, -86.0, 359.8, -395.0, 3.1416, KindTeamLeave, (byte)1, "edmundrange2 blue base -> staging" }
        };

        /// <summary>
        /// The staging area's upper floor, 16 m over its ground floor (376): a gallery round the
        /// four walls with two shops' counters, weapon and armour cases, and tables, which no
        /// stair or ramp reaches from below - the navmesh has no way up. The range put the team
        /// that won a match there. Its north-east and north-west corners have the red and the
        /// blue arrows again, at that floor's height, as the ground floor's do by its teleporters:
        /// the second set of teleporters, and the way off the floor for whoever stands on it.
        /// Both lead where the first set does.
        ///
        /// The third row says where the winners arrive: in front of the north counters, facing
        /// them. It is never walked into - disabled, of no size - and is a link so that a game
        /// master can move the arrival as the others are moved (.link 9009 arrival).
        /// Set by hand from the map's static objects and the navmesh; wants a walk-through.
        /// </summary>
        public static readonly object[][] UpperFloorMapLinks =
        {
            new object[] { RedUpperLinkId, MapContextId, -33.0, 376.0, -338.0, 3.0, MapContextId, 282.05, 420.5, 34.28, 1.5708, KindTeamRed, (byte)1, "edmundrange2 staging upper floor -> red team" },
            new object[] { BlueUpperLinkId, MapContextId, -91.0, 376.0, -338.0, 3.0, MapContextId, -401.96, 420.5, 45.68, -1.5708, KindTeamBlue, (byte)1, "edmundrange2 staging upper floor -> blue team" },
            new object[] { WinnersLinkId, MapContextId, -62.0, 376.2, -345.0, 0.0, MapContextId, -62.0, 376.2, -345.0, 3.1416, KindTeamWinners, (byte)0, "edmundrange2 the winners' arrival on the staging upper floor" }
        };

        /// <summary>The inserts of Add_edmund_range_upper_floor.</summary>
        public static IEnumerable<string> UpperFloorInsertStatements
        {
            get { yield return Insert(MapLinkEntry.TableName, MapLinkColumns, UpperFloorMapLinks); }
        }

        /// <summary>The deletes that take them out again.</summary>
        public static IEnumerable<string> UpperFloorDeleteStatements
        {
            get { yield return $"delete from {MapLinkEntry.TableName} where id between {RedUpperLinkId} and {WinnersLinkId};"; }
        }

        private const string ActionColumns = "action1, action2, action3, action4, action5, action6, action7, action8";

        /// <summary>Every insert, in order; each is plain SQL either provider takes.</summary>
        public static IEnumerable<string> InsertStatements
        {
            get
            {
                foreach (var (id, copyOf, comment, nameId) in Creatures)
                {
                    yield return $"insert into {CreatureEntry.TableName} (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, {ActionColumns}) "
                        + $"select {id}, '{comment}', class_id, faction, level, max_hp, {nameId}, run_speed, walk_speed, {ActionColumns} from {CreatureEntry.TableName} where id = {copyOf};";
                    yield return $"insert into {CreatureStatEntry.TableName} (id, body, mind, spirit, health, armor) "
                        + $"select {id}, body, mind, spirit, health, armor from {CreatureStatEntry.TableName} where id = {copyOf};";
                    yield return $"insert into {CreatureAppearanceEntry.TableName} (id, slot_id, Class_id, color) "
                        + $"select {id}, slot_id, Class_id, color from {CreatureAppearanceEntry.TableName} where id = {copyOf};";
                }

                yield return Insert(SpawnPoolEntry.TableName, PoolColumns, Pools);
                yield return Insert(ControlPointEntry.TableName, PointColumns, Points);
                yield return Insert(ControlPointLinkEntry.TableName, LinkColumns, Links);
                yield return Insert(MapLinkEntry.TableName, MapLinkColumns, MapLinks);
            }
        }

        /// <summary>Every delete that takes the rows out again, in order.</summary>
        public static IEnumerable<string> DeleteStatements
        {
            get
            {
                yield return $"delete from {MapLinkEntry.TableName} where id between {RedEntranceLinkId} and {BlueReturnLinkId};";
                yield return $"delete from {ControlPointLinkEntry.TableName} where control_point_id between {WhiskeyId} and {EchoId};";
                yield return $"delete from {ControlPointEntry.TableName} where id between {WhiskeyId} and {EchoId};";
                yield return $"delete from {SpawnPoolEntry.TableName} where id between {FirstPoolId} and {LastPoolId};";

                foreach (var table in new[] { CreatureAppearanceEntry.TableName, CreatureStatEntry.TableName, CreatureEntry.TableName })
                    yield return $"delete from {table} where id between {FirstCreatureId} and {LastCreatureId};";
            }
        }

        private static string Insert(string table, string[] columns, object[][] rows)
        {
            return $"insert into {table} ({string.Join(", ", columns)}) values "
                + string.Join(", ", rows.Select(row => "(" + string.Join(", ", row.Select(Literal)) + ")"))
                + ";";
        }

        private static string Literal(object value)
        {
            return value switch
            {
                string text => "'" + text.Replace("'", "''") + "'",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                _ => System.Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}
