using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Final Assault (1167): TaRapedia, "Destroy the 3 Security Control Stations that maintain the force field to the
    /// underground facility. Once the force field is down, place the detonator on the Bane Power Core." A sealed Bane
    /// force field across the facility's service entrance in every copy of the Temporal Chamber (2029), and the
    /// TakeDownField action on the hidden tally's transition (the three stations down) that takes it down for good in the
    /// player's copy.
    ///
    /// Place (ESTIMATE): the client map's ArchBaneIndustrialServiceEntranceV01 (10126) at (620, 300, -440), yaw 0, its
    /// mesh x 608.8..631.2, z -452.1..-428.6 (client mesh02); the navmesh walks in through its south mouth at about
    /// x 614..626, z -443, floor 301.2 (tools/stuckprobe geo). No field class is made for this entrance; the Bane outpost
    /// wall minor entrance's (20000003, 13.7 m by 5.3 m, local x -10.85..2.85) is the nearest in size: origin x 623.5, so
    /// the sheet spans x 612.65..626.35.
    ///
    /// Data only, plain SQL (PlateauFieldSqlV1). Down takes out only these rows.
    /// </summary>
    public static class PlateauFinalAssaultFieldV1
    {
        public const uint FieldClass = 20000003;    // UsableForceFieldBaneOutpostWallMinorV01
        public const uint Field = 750211;
        public const double X = 623.5, Y = 301.2, Z = -442.5, Yaw = 0;
        public const uint Action = 3;               // after 1 and 2, which open objective 2

        public static IEnumerable<string> Up()
        {
            yield return PlateauFieldSqlV1.Field(Field, PlateauTemporalChamberNpcsV1.MapContextId, FieldClass, X, Y, Z, Yaw,
                "Temporal Chamber: 1167 force field at the underground facility's service entrance");
            yield return PlateauFieldSqlV1.TakeDown(PlateauFinalAssaultV1.MissionId, PlateauFinalAssaultV1.Tally, 1, Action, Field,
                "facility-field-down", "The three stations down: the facility's force field goes down");
        }

        public static IEnumerable<string> Down()
        {
            yield return PlateauFieldSqlV1.RemoveTakeDown(PlateauFinalAssaultV1.MissionId, PlateauFinalAssaultV1.Tally, 1, Action);
            yield return PlateauFieldSqlV1.RemoveField(Field, FieldClass);
        }
    }
}
