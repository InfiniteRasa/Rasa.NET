using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The human gate force fields set into their archways' nooks (Gate_field_nooks).
    ///
    /// An outpost fence gate (ArchHumOutpostFenceGateV01, arch_hum_outpost_fence_entrance_v03.geo;
    /// V02, _v04.geo) runs 16 m along its local x from its origin, a 2 m post at each end. Each
    /// post's inner face has a slot cut into it for the field: 0.5 m deep (local x 1.5 and 14.5),
    /// 0.31 m wide (local z -1.613 to -1.300) and from 1.3 m to 9.25 m up, the same in both
    /// versions. The field (UsableForceFieldHumOutpostGateV01, arch_hum_outpost_fence_entrance_v01_forcefield.geo)
    /// is a sheet 12.96 m wide, 0.33 m thick and 1.12 m to 9.36 m up about its own origin: with its
    /// origin at the gate's local (8, 0, <see cref="NookZ"/>), facing the gate's way, its ends sit
    /// in the two slots.
    ///
    /// The seed (Wilderness posts, Twin Pillars) put the four fields on the gates' local z = 0 line,
    /// the fence's outer face, 1.46 m in front of the slots, so each end of the sheet ran half a
    /// metre into the solid post. Positions and facings below are the gates' own, from the
    /// 1.16.5.0 client's adv_foreas_concordia_wilderness.map.
    ///
    /// Torcastra's gate (a Bane outpost wall's major entrance) has its slot on the wall's centre
    /// line, where its field already stands. Devil's Den's laser door stands across a chunnel's
    /// mouth, not in an archway.
    ///
    /// Guarded: a row moves only from where the seed put it, as the gate field it was, and Down
    /// moves it back only from where Up put it.
    /// </summary>
    public static class GateFieldNooks
    {
        public const uint Map = 1220;
        public const uint FieldClass = WildernessPostNpcs.HumanGateField;

        /// <summary>The slots' centre on the gate's local z, in metres (arch_hum_outpost_fence_entrance_v03/_v04.geo).</summary>
        public const double NookZ = -1.4567;

        /// <summary>The middle of the gateway along the gate's local x.</summary>
        public const double NookX = 8.0;

        /// <summary>
        /// A field row and its gate: the gate's origin and facing (yaw) from the client's map, where
        /// the seed had the field, and where it goes.
        /// </summary>
        public sealed record Gate(uint Row, string Name, (double X, double Y, double Z, double Yaw) Arch,
            (double X, double Y, double Z, double Rotation) From, (double X, double Y, double Z, double Rotation) To);

        public static readonly Gate[] Gates =
        {
            new(WildernessPostNpcs.GateFieldId, "Landing Zone gate (ArchHumOutpostFenceGateV02)",
                (206.0, 163.0, -35.0, -3.1416), (198.0, 163.0, -35.0, 3.1416), (198.0, 163.0, -33.5433, 3.1416)),
            new(TwinPillarsNpcs.DestructibleId(0), "Twin Pillars east gate (ArchHumOutpostFenceGateV01)",
                (31.0, 203.0, -498.0, -1.5708), (31.0, 203.0, -490.0, -1.5708), (32.4567, 203.0, -490.0, -1.5708)),
            new(TwinPillarsNpcs.DestructibleId(1), "Twin Pillars north gate (ArchHumOutpostFenceGateV01)",
                (-12.4637, 188.0, -398.594, 2.9199), (-20.26, 188.0, -400.33, 2.920), (-20.5882, 188.0, -398.9321, 2.9199)),
            new(TwinPillarsNpcs.DestructibleId(2), "Twin Pillars west gate (ArchHumOutpostFenceGateV01)",
                (-16.0, 220.0, -532.0, -3.1416), (-24.0, 220.0, -532.0, -3.1416), (-24.0, 220.0, -530.5433, -3.1416))
        };

        public static IEnumerable<string> Up()
        {
            foreach (var gate in Gates)
                yield return Move(gate, gate.From, gate.To);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var gate in Gates)
                yield return Move(gate, gate.To, gate.From);
        }

        private static string Move(Gate gate, (double X, double Y, double Z, double Rotation) from, (double X, double Y, double Z, double Rotation) to) =>
            $"UPDATE {WorldDestructibleEntry.TableName} SET pos_x={N(to.X)}, pos_y={N(to.Y)}, pos_z={N(to.Z)}, rotation={N(to.Rotation)} " +
            $"WHERE id={gate.Row} AND map_context_id={Map} AND class_id={FieldClass} " +
            $"AND ABS(pos_x-({N(from.X)}))<0.001 AND ABS(pos_y-({N(from.Y)}))<0.001 AND ABS(pos_z-({N(from.Z)}))<0.001 " +
            $"AND ABS(rotation-({N(from.Rotation)}))<0.0001;";

        private static string N(double value) => value.ToString("0.0###", CultureInfo.InvariantCulture);
    }
}
