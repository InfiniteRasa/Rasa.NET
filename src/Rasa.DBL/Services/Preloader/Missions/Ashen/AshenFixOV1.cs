using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job O (real client 2026-11-13 05:09, 1535 objective 1 never completes), a delta on 20271110001700 (set 17, live)
    /// and 20271110001930 (fix M, live), Avernus Outpost (2110). Petrovich's Run's first objective is the graveyard's four Atta
    /// pools cleared at once (SpawnPoolCleared, set 17). Two of the four upstream pools stand their creatures on navmesh islands
    /// inside the client's rock (as upstream N29; the probe, 2026-11-13):
    ///  - 531049 (821.3, 289, -240.5) r 20: its point on a 22 m2 region joined to nothing; every one of 1500 seeded draws on it
    ///    (x 822.5-831.1, z -242.8 to -236.4). The client: no model, "You cannot see your target from here" from 8.9-15 m, no
    ///    sight line from the main floor 2-8 m away, a free run of 1-2 m round the tester's stack point (831.2, 288.8, -238.8). A
    ///    chase's path stops at the island's edge nearest the player, so the members stand on one point (ESTIMATE).
    ///  - 531060 (974.5, 274.1, -230.4) r 13.7: an 8.8 m2 island (x 968-972, z -228 to -224), not one of 30 sampled draws seen
    ///    from 345 main-floor spots within 25 m, no path from Petrovich (ESTIMATE: as 531049 in the client).
    /// Upstream rows stay as they are (upstream bug, NOTES-upstream-bugs.md). Fix O adds two pools of ours with the same creature
    /// rows and counts on the main floor beside them and points objective 1's triggers 1 and 3 and its markers 1 and 3 at them:
    ///  - 790700 (814, 289.12, -248) r 4 for 531049: the creature rows 531015 (2-3), 531016 (3-4), 531017 (0-2), respawn 1200
    ///    (as 531049); its draws on the floor 1535's walk to the tunnel crosses (x 809.3-818.8, z -253.4 to -244.4), each seen
    ///    from that walk and walked to from it; north of the knee-high rock at (810.9, -254.9) that a probe at (812, -252) r 6
    ///    walked into from the north; 62.7 m from 531063's point, 60 m and more from Petrovich and his squad.
    ///  - 790701 (972, 274.08, -212) r 6 for 531060: 531016 (2-4), respawn 1200 (as 531060); 14.5 m from the island's middle;
    ///    19.5 m from 531050's draws (both the objective's own); 62 m from the squad.
    /// The markers: the pool's circle and PoolMarkerPadding (4 m), as set 17's. All ESTIMATE (a navmesh and client-floor probe).
    /// </summary>
    public static class AshenFixOV1
    {
        public const uint Outpost = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint Run = AshenPetrovichsRunV1.MissionId, ClearTheCave = AshenPetrovichsRunV1.ClearTheCave;

        /// <summary>The upstream pools left on their islands, and the pools of ours that stand in for them.</summary>
        public const uint WestIsland = 531049, EastIsland = 531060;
        public const uint WestPool = 790700, EastPool = 790701, NextFreeId = 790702;

        /// <summary>Set 17's trigger and marker of each island pool (AshenPetrovichsRunV1: one each, in CavePools' order).</summary>
        public const uint WestTrigger = 1, EastTrigger = 3, WestMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 1,
            EastMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 3;

        public const uint Respawn = 1200;
        public const double WestRadius = 4, EastRadius = 6;
        public static readonly (double X, double Y, double Z) WestAt = (814, 289.12, -248), EastAt = (972, 274.08, -212);

        /// <summary>The graveyard's pools objective 1 counts now (set 17's CavePools, the islands' two replaced): pool, centre, radius.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At, double Radius)[] CavePools =
        {
            (WestPool, WestAt, WestRadius), AshenPetrovichsRunV1.CavePools[1], (EastPool, EastAt, EastRadius), AshenPetrovichsRunV1.CavePools[3]
        };

        public const uint Evidence = 5;
        public const string Source = "repository:AshenFixOV1";
        public const string RunNote =
            "Fix job O (real client 2026-11-13): upstream pools 531049 and 531060 stand their Atta on navmesh islands inside the " +
            "client's rock (unseen, unreachable); objective 1 counts our pools 790700 (814, 289.12, -248) r 4 and 790701 (972, " +
            "274.08, -212) r 6 instead, the same creature rows and counts; markers 1 and 3 with them. ESTIMATE.";

        public static IEnumerable<string> UpSql()
        {
            yield return CrucibleSql.Guard(Pool(WestPool, WestAt, WestRadius, (531015, 2, 3), (531016, 3, 4), (531017, 0, 2)));
            yield return CrucibleSql.Guard(Pool(EastPool, EastAt, EastRadius, (531016, 2, 4), (0, 0, 0), (0, 0, 0)));
            yield return TriggerSql(WestTrigger, WestPool, WestIsland);
            yield return TriggerSql(EastTrigger, EastPool, EastIsland);
            var west = AshenPetrovichsRunV1.CavePools[0];
            var east = AshenPetrovichsRunV1.CavePools[2];
            yield return MarkerSql(WestMarker, WestAt, WestRadius + AshenPetrovichsRunV1.PoolMarkerPadding, west.At, west.Radius + AshenPetrovichsRunV1.PoolMarkerPadding);
            yield return MarkerSql(EastMarker, EastAt, EastRadius + AshenPetrovichsRunV1.PoolMarkerPadding, east.At, east.Radius + AshenPetrovichsRunV1.PoolMarkerPadding);
        }

        public static IEnumerable<string> DownSql()
        {
            var west = AshenPetrovichsRunV1.CavePools[0];
            var east = AshenPetrovichsRunV1.CavePools[2];
            yield return MarkerSql(EastMarker, east.At, east.Radius + AshenPetrovichsRunV1.PoolMarkerPadding, EastAt, EastRadius + AshenPetrovichsRunV1.PoolMarkerPadding);
            yield return MarkerSql(WestMarker, west.At, west.Radius + AshenPetrovichsRunV1.PoolMarkerPadding, WestAt, WestRadius + AshenPetrovichsRunV1.PoolMarkerPadding);
            yield return TriggerSql(EastTrigger, EastIsland, EastPool);
            yield return TriggerSql(WestTrigger, WestIsland, WestPool);
            yield return $"DELETE FROM spawnpool WHERE id IN ({WestPool}, {EastPool}) AND map_context_id = {Outpost};";
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Run).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, RunNote, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Run} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Pool(uint id, (double X, double Y, double Z) at, double radius, (uint Id, uint Min, uint Max) first, (uint Id, uint Min, uint Max) second,
            (uint Id, uint Min, uint Max) third) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"VALUES ({id}, 0, 0, {Respawn}, {F(at.X)}, {F(at.Y)}, {F(at.Z)}, 0, {Outpost}, " +
            $"{first.Id}, {first.Min}, {first.Max}, {second.Id}, {second.Min}, {second.Max}, {third.Id}, {third.Min}, {third.Max}, " +
            $"0, 0, 0, 0, 0, 0, 0, 0, 0, {F(radius)});";

        private static string TriggerSql(uint trigger, uint to, uint from) =>
            $"UPDATE mission_trigger SET subject_id = {to} WHERE mission_id = {Run} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND objective_id = {ClearTheCave} AND transition_id = 1 AND trigger_id = {trigger} " +
            $"AND event_kind = {(int)MissionProgressEventKind.SpawnPoolCleared} AND subject_id = {from};";

        private static string MarkerSql(uint indicator, (double X, double Y, double Z) to, double toRadius, (double X, double Y, double Z) from, double fromRadius) =>
            $"UPDATE mission_indicator SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)}, radius = {F(toRadius)} " +
            $"WHERE mission_id = {Run} AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {ClearTheCave} " +
            $"AND indicator_id = {indicator} AND ABS(pos_x - {F(from.X)}) < 0.001 AND ABS(pos_y - {F(from.Y)}) < 0.001 " +
            $"AND ABS(pos_z - {F(from.Z)}) < 0.001 AND ABS(radius - {F(fromRadius)}) < 0.001;";
    }
}
