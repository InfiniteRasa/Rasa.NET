using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Self Preservation (879): Ten Ton Hammer (guide to Maligo Base, lower level): the Security CPUs sit in rooms behind
    /// "several force fields blocking the hallways, each marked by a different colored panel. The switches will open and
    /// close the colored doorways and force fields." One sealed Bane force field across the one opening of each CPU's room
    /// in every copy of Maligo Base (1830), and the TakeDownField action on each switch's hidden objective (2 green, 3
    /// yellow, 4 red: PlateauSelfPreservationV1.Switches) that takes its field down for good in the player's copy.
    ///
    /// Places (ESTIMATE; the v3 client map has no gate there, TTH's coordinates are of the older map): the openings from
    /// the client map's service pieces (tools/stuckprobe geo): the north-east room's south end at z 312.5 (jamb x
    /// 37.7..46.3), the north room's west end at x -21.8 (z 323.7..332.3), the north-west room's south end at z 313.5,
    /// at the foot of the ramp (x -50.3..-41.7, floor -138.9). Class 20000003, the Bane outpost wall minor entrance's
    /// field (13.7 m by 5.3 m, local x -10.85..2.85): wider than the 8.6 m corridors, its ends in the walls.
    ///
    /// Data only, plain SQL (PlateauFieldSqlV1). Down takes out only these rows.
    /// </summary>
    public static class PlateauSelfPreservationFieldsV1
    {
        public const uint FieldClass = 20000003;    // UsableForceFieldBaneOutpostWallMinorV01
        public const uint Action = 1;

        /// <summary>The fields, in the order of PlateauSelfPreservationV1.Switches and Cpus.</summary>
        public static readonly (uint Id, double X, double Y, double Z, double Yaw, string Where)[] Fields =
        {
            (750221, 45.0, -130.9, 312.5, 0, "the north-east room (green)"),
            (750222, -21.8, -130.9, 324.5, 1.5708, "the north room (yellow)"),
            (750223, -43.0, -138.9, 313.5, 0, "the north-west room (red)"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var field in Fields)
                yield return PlateauFieldSqlV1.Field(field.Id, PlateauMaligoBaseV1.MapContextId, FieldClass, field.X, field.Y, field.Z, field.Yaw,
                    $"Maligo Base: 879 force field at {field.Where}");
            for (var index = 0; index < Fields.Length; index++)
                yield return PlateauFieldSqlV1.TakeDown(PlateauSelfPreservationV1.MissionId, PlateauSelfPreservationV1.Switches[index].Objective, 1,
                    Action, Fields[index].Id, $"{PlateauSelfPreservationV1.Switches[index].Colour}-field-down",
                    $"The {PlateauSelfPreservationV1.Switches[index].Colour} switch: the field at {Fields[index].Where} goes down");
        }

        public static IEnumerable<string> Down()
        {
            foreach (var flip in PlateauSelfPreservationV1.Switches)
                yield return PlateauFieldSqlV1.RemoveTakeDown(PlateauSelfPreservationV1.MissionId, flip.Objective, 1, Action);
            foreach (var field in Fields)
                yield return PlateauFieldSqlV1.RemoveField(field.Id, FieldClass);
        }
    }
}
