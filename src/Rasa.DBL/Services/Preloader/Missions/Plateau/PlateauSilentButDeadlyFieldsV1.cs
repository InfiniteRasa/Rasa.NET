using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Silent But Deadly (1187), objective 4 "Interface With Bane Computer and Bring Down Ustor Force Fields": the
    /// yard's force fields, sealed (no shot takes them down), in every copy of Ustor Yard (1502), and the two
    /// TakeDownField actions on 4's transition that take them down for good in the player's copy (Rapp's turn-in:
    /// "the force fields are all down").
    ///
    /// Places: the client's Ustor map builds four Bane outpost wall minor entrances (ArchBaneOutpostWallMinorEntranceV01,
    /// 10765) with their openings empty; the field class UsableForceFieldBaneOutpostWallMinorV01 (20000003, ForceFields
    /// "baneminor") is that gate's mesh with "_forcefield" in the name, so each field stands at its gate's origin and yaw
    /// (yaw = 2 atan2(qy, qw) of the map's rotation). Two of the four (ESTIMATE which): the one on the west wall
    /// (-41.86, 176, -8.37) stands across the way to the Maligo exit, which One Way Trip (877) walks without 1187, so it
    /// is left open; the one south-west of the hospital (750202, <see cref="Retired"/>) stood across the way from Rapp to
    /// the generator, which 6 needs before 4 takes the fields down (a bot was held there, to-fix #6), so it is left open
    /// too (our choice, the recommended option). Bane side: they stop players and let the Bane through.
    ///
    /// Data only, plain SQL (PlateauFieldSqlV1): rows of world_destructible (sealed, so after
    /// 20270921000000_World_destructible_sealed) and mission_action (kind 14, after 20270921000100_Take_down_field_action).
    /// Down takes out only these rows, and those of the retired field and its action (action 4) where a database still
    /// has them from the first version of this migration.
    /// </summary>
    public static class PlateauSilentButDeadlyFieldsV1
    {
        public const uint FieldClass = 20000003;    // UsableForceFieldBaneOutpostWallMinorV01
        public const uint FirstAction = 2;          // after action 1, the reward

        public static readonly (uint Id, double X, double Y, double Z, double Yaw, string Where)[] Fields =
        {
            (750201, 216.557, 176.904, 66.443, 1.5708, "the minor entrance west of Chemical Storage"),
            (750203, 116.531, 176.904, 57.796, 2.3562, "the minor entrance north-east of the centre"),
        };

        /// <summary>The field of the first version, south-west of the hospital, no longer placed (to-fix #6).</summary>
        public const uint Retired = 750202;

        /// <summary>The TakeDownField actions the first version had: one for each of its three fields.</summary>
        private const int FirstVersionActions = 3;

        public static IEnumerable<string> Up()
        {
            foreach (var field in Fields)
                yield return PlateauFieldSqlV1.Field(field.Id, PlateauUstorYardV1.MapContextId, FieldClass, field.X, field.Y, field.Z, field.Yaw,
                    $"Ustor Yard: 1187 force field, {field.Where}");
            for (var index = 0; index < Fields.Length; index++)
                yield return PlateauFieldSqlV1.TakeDown(PlateauSilentButDeadlyV1.MissionId, PlateauSilentButDeadlyV1.Computer, 1,
                    FirstAction + (uint)index, Fields[index].Id, $"ustor-field-{index + 1}-down",
                    $"Take down the Ustor force field at {Fields[index].Where}");
        }

        public static IEnumerable<string> Down()
        {
            for (var index = 0; index < FirstVersionActions; index++)
                yield return PlateauFieldSqlV1.RemoveTakeDown(PlateauSilentButDeadlyV1.MissionId, PlateauSilentButDeadlyV1.Computer, 1,
                    FirstAction + (uint)index);
            foreach (var field in Fields)
                yield return PlateauFieldSqlV1.RemoveField(field.Id, FieldClass);
            yield return PlateauFieldSqlV1.RemoveField(Retired, FieldClass);
        }
    }
}
