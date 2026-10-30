using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Silent But Deadly (1187), Security Station Bravo's door and Taskmaster Yvik. Objective 6 "Blow Generator to Open
    /// Door to the Security Station" (client), and the Ten Ton Hammer guide: "The mission will update as you've blown open
    /// the door to the security station." The station was open from the start, so a player could go in before the
    /// generator and find nothing there (a player's report, 2026-10-30).
    ///
    /// The door (ESTIMATE: no door is in the client's map, and no field class is made for this entrance): a sealed Bane
    /// field in each of the station's two service entrances (ArchBaneIndustrialServiceEntranceV01, 10126), in every copy
    /// of Ustor Yard (1502), and the two TakeDownField actions on 6's transition that take them down for good in the
    /// player's copy (a squad's copy made again has them down again as soon as one of them is in:
    /// PlacedDestructibles.CatchUp). As The Final Assault's field in the same entrance class
    /// (PlateauFinalAssaultFieldV1: the outpost wall minor entrance's field, 20000003, 13.7 m by 5.3 m, at the entrance's
    /// local (3.5, 1.2, -2.5) and its yaw), each turned by its entrance's yaw (the client map's quaternion):
    ///  - the east entrance (232, 164, -124), yaw -pi/2: its mouth opens east onto the low ground of the generator (the
    ///    navmesh's corridor runs x 222-240 at z -127..-122, floor 165-166); the field at (234.5, 165.2, -120.5) spans
    ///    z -131.35..-117.65.
    ///  - the north entrance (160, 172, -148), yaw pi: its corridor runs south to the ramp down to the consoles (the
    ///    navmesh's at x 158-162, floor 173); the field at (156.5, 173.2, -145.5) spans x 153.65..167.35.
    ///
    /// Taskmaster Yvik (the guide: the south-east room's named Bane; client name 9561, creaturenamelanguage): leads
    /// the station's security detail (PlateauSilentButDeadlyV1.DetailAt). As the Officer of Monkey Wrench
    /// (PlateauUstorYardV1.Officer): a Thrax (ESTIMATE: the guide does not say his kind) at level 31 with three times
    /// the security's hit points and the level 33 Thrax's attacks.
    ///
    /// Ids 750066 (creature) and 750204-750205 (fields), free before. Data only, plain SQL; Down takes out only these rows.
    /// </summary>
    public static class PlateauSilentButDeadlyStationV1
    {
        public const uint Yvik = 750066, YvikName = 9561;

        public const uint FieldClass = PlateauSilentButDeadlyFieldsV1.FieldClass;
        /// <summary>Actions on 6's transition 1, after 1 and 2 (they open objective 1).</summary>
        public const uint FirstAction = 3;

        public static readonly (uint Id, double X, double Y, double Z, double Yaw, string Where)[] Fields =
        {
            (750204, 234.5, 165.2, -120.5, -1.5708, "the station's east service entrance"),
            (750205, 156.5, 173.2, -145.5, 3.1416, "the station's north service entrance"),
        };

        public static IEnumerable<string> Up()
        {
            yield return Creature(Yvik, "Taskmaster Yvik - Ustor Yard, Security Station Bravo", PlateauUstorYardV1.ThraxClass,
                PlateauUstorYardV1.SecurityLevel, PlateauUstorYardV1.OfficerHitPoints, YvikName,
                PlateauUstorYardV1.ThraxKick33, PlateauUstorYardV1.ThraxWeapon33);
            yield return Stat(Yvik, PlateauUstorYardV1.OfficerHitPoints, PlateauUstorYardV1.SecurityArmor);

            for (var index = 0; index < Fields.Length; index++)
            {
                var field = Fields[index];
                yield return PlateauFieldSqlV1.Field(field.Id, PlateauUstorYardV1.MapContextId, FieldClass, field.X, field.Y, field.Z,
                    field.Yaw, $"Ustor Yard: 1187 station door, {field.Where}");
                yield return PlateauFieldSqlV1.TakeDown(PlateauSilentButDeadlyV1.MissionId, PlateauSilentButDeadlyV1.Generator, 1,
                    FirstAction + (uint)index, field.Id, $"station-door-{index + 1}-down", $"1187 generator blown: {field.Where}");
            }
        }

        public static IEnumerable<string> Down()
        {
            for (var index = 0; index < Fields.Length; index++)
            {
                yield return PlateauFieldSqlV1.RemoveTakeDown(PlateauSilentButDeadlyV1.MissionId, PlateauSilentButDeadlyV1.Generator, 1,
                    FirstAction + (uint)index);
                yield return PlateauFieldSqlV1.RemoveField(Fields[index].Id, FieldClass);
            }
            yield return $"DELETE FROM creature_stat WHERE id = {Yvik};";
            yield return $"DELETE FROM creature WHERE id = {Yvik};";
        }

        /// <summary>The written mission's detail without Yvik (the migration's Down): his actor as the yard's security.</summary>
        public static string DetailWithoutYvikSql() =>
            $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, '\"templateId\":{Yvik},', '\"templateId\":{PlateauUstorYardV1.Security},') " +
            $"WHERE mission_id = {PlateauSilentButDeadlyV1.MissionId} AND content_revision = '{PlateauMissionDataV1.Revision}';";
    }
}
