using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job D: two deltas, on migration 20271110000700 (set 7, AshenRoboticSalvageV1, live; its data class
    /// stays) and on upstream's teleporter row 208.
    ///
    /// Item 1 (auditor review of set 7, item 2): Robotic Salvage Recovery's (1264) second reward item: TaRapedia names "Dynamo Motor Assist Armor Legs 2" at level 43
    /// ("green, 43"; NOTES-ashen-research-group1.md); set 7 took the level 38-42 Uncommon 29093 (Armor_T1_MotorAssist_V04_UNC_
    /// Legs_38_to_42). Now the lowest template of the 43-47 band, 29113 (Armor_T1_MotorAssist_V04_UNC_Legs_43_to_47; Motor
    /// Assist Legs have no Uncommon below V04). The other three items stay. An evidence row (4) says so.
    ///
    /// Item 2 (to-fix row 8, real client 2026-11-09): the Shadow's Edge Post hospital, World DB teleporter 208 (upstream
    /// TeleporterPreloader, PR #158) at (-94.13672, 269.3, -828.72266), stands 27 m under the hospital's floor: at its x/z the
    /// client has floors at 296.20 (terrain), 296.50 (the hospital's floor), 300.95, 302.27 and 307.30 (upper parts) and none
    /// below; the server navmesh has 296.68 only. A player sent there on a hospital return fell (game.log "fell below the floor
    /// of map 1734 at <-94.14, -55.6, -828.72>", six times 22:31-23:35) and the safety floor put them back at the death spot.
    /// Now y 296.5, on the hospital's floor (x, z and rotation as upstream): on the navmesh (296.68), joined to the Shadow's
    /// Edge Post waypoint (195) and walked to and from it on the client's floor (probe 2026-11-09). The other hospitals and
    /// waypoints of map 1734 (195-198, 207, 209, 317-319, 322) stand on a client floor and the navmesh (upstream-bugs D19).
    ///
    /// Plain SQL; each UPDATE only where the row holds its exact stored values (a second run changes nothing); Down puts set
    /// 7's 29093 and upstream's 269.3 back only where the rows hold fix D's values.
    /// </summary>
    public static class AshenFixDV1
    {
        public const uint MissionId = AshenRoboticSalvageV1.MissionId;
        public const uint Reward = 1, Item = 2, Quantity = 1;
        public const uint Before = AshenRoboticSalvageV1.MotorAssistLegs;   // 29093, Armor_T1_MotorAssist_V04_UNC_Legs_38_to_42
        public const uint Now = 29113;                                      // Armor_T1_MotorAssist_V04_UNC_Legs_43_to_47

        /// <summary>1264's evidence row (set 7 wrote 1-3).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:AshenFixDV1";

        /// <summary>The Shadow's Edge Post hospital (teleporter 208): upstream's stored row and fix D's height.</summary>
        public const uint Hospital = 208, HospitalMap = 1734, HospitalType = 5;
        public static readonly (double X, double Y, double Z) HospitalBefore = (-94.13672, 269.3, -828.72266);
        public const double HospitalRotation = 3.7;
        public const double HospitalY = 296.5;

        public const string Note =
            "Fix job D (set 7 review): TaRapedia's Dynamo Motor Assist Armor Legs 2 is level 43 ('green, 43'); set 7's 29093 is the " +
            "38-42 band. Now 29113 (Motor Assist V04 Uncommon Legs 43-47, the lowest template of the band). The other three items stay.";

        public static IEnumerable<string> UpSql()
        {
            yield return ItemSql(Now, Before);
            yield return HospitalSql(HospitalY, HospitalBefore.Y);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return HospitalSql(HospitalBefore.Y, HospitalY);
            yield return ItemSql(Before, Now);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {MissionId} " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        /// <summary>The hospital's height from one value to the other, only where the row is upstream's (the stored doubles).</summary>
        private static string HospitalSql(double to, double from) =>
            $"UPDATE teleporter SET pos_y = {N(to)} WHERE id = {Hospital} AND map_context_id = {HospitalMap} AND type = {HospitalType} " +
            $"AND pos_x = {N(HospitalBefore.X)} AND pos_y = {N(from)} AND pos_z = {N(HospitalBefore.Z)} AND rotation = {N(HospitalRotation)};";

        private static string N(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>1264's reward item 2 from one template to the other, only where it is the one.</summary>
        private static string ItemSql(uint to, uint from) =>
            $"UPDATE mission_reward_item SET item_template_id = {to} " +
            $"WHERE mission_id = {MissionId} AND content_revision = '{AshenMissionDataV1.Revision}' AND reward_id = {Reward} " +
            $"AND item_id = {Item} AND item_template_id = {from} AND quantity = {Quantity};";
    }
}
