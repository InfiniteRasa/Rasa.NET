using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job S (real client 2026-11-13 19:4x and 20:02), a delta on 20271110002140 (fix R, live): 1497's objective 2
    /// ("Hack Computer Terminal") could not be done. Set 19 put the terminal in the HQ "hall" at (-4, 135.94, -372); that floor
    /// is the map's terrain heightmap only (no placed mesh at 135.3-136.6 from x -40 to 40, z -373 to -355), which the server
    /// navmesh and our collision reader (MapGeometry, no terrain holes) take as floor. The real client has no floor there: on
    /// set 19's corners it walked down the spiral tunnel ramp under it (class 4153 arch_bane_industrial_tunnel_ramp_01_32m_v01
    /// at (6.5, 104, -353.3)): (13, -369) 134.9, (13.4, -366.6) 132.9, (12.2, -361.8) 130.4, (2.2, -361.5) 126.7, (-1.2, -361.9)
    /// 125.6, (-2.0, -371.6) 121.9, the ramp mesh's floor there (134.6, 132.6, 130.4, 126.7, 125.5, 122.0), 14 m under the
    /// terminal, out of its use range, with no way up. ESTIMATE: the client's terrain has a hole over the ramp that the reader
    /// does not apply (as N26). The terminal and marker 251 (radius kept) move onto the HQ tunnel's end deck, the floor the
    /// client reached (mesh 137.6, navmesh joined to the charge's region, seen from its whole 2 m ring), 2.55 m east of the
    /// walk's end (9.5, 137.61, -380), no drop over EdgeDrop within 2 m of either. Orientation kept.
    /// Each UPDATE guarded on the exact values before; Down puts them back.
    /// </summary>
    public static class AshenFixSV1
    {
        public const uint Raid = AshenRaidTheConscriptionFacilityV1.MissionId;
        public const uint TerminalObjective = AshenRaidTheConscriptionFacilityV1.HackTheTerminal, TerminalMarker = AshenRaidTheConscriptionFacilityV1.TerminalIndicator;

        /// <summary>The terminal: set 19's place on the terrain over the ramp, fix S's on the tunnel's end deck.</summary>
        public static readonly ScenePosition TerminalBefore = AshenRaidTheConscriptionFacilityV1.TerminalAt, TerminalAt = new(12f, 137.6f, -379.5f);

        /// <summary>Where the walk to the terminal ends (2.55 m west of it, on the deck).</summary>
        public static readonly ScenePosition TerminalStand = new(9.5f, 137.61f, -380f);

        public const double MarkerRadius = AshenRaidTheConscriptionFacilityV1.ObjectMarkerRadius;

        public const uint Evidence = 5;
        public const string Source = "repository:AshenFixSV1";
        public const string Note =
            "Fix job S (real client 2026-11-13): 1497's terminal 10519 moved from the HQ hall (-4, 135.94, -372), terrain the client " +
            "has no floor on (it walked down the tunnel ramp under it to (-2.0, 121.9, -371.6)), onto the HQ tunnel's end deck " +
            "(12, 137.6, -379.5); marker 251 with it. No source places the terminal. ESTIMATE.";

        /// <summary>1497's scene as fix M left it, the terminal moved.</summary>
        public static MissionSceneDefinition RaidScene()
        {
            var scene = AshenFixMV1.RaidScene();
            var role = AshenRaidTheConscriptionFacilityV1.TerminalRole;
            scene.Actors[role] = scene.Actors[role] with { Position = TerminalAt };
            return scene;
        }

        public static string RaidBefore() => AshenFixMV1.RaidNow();
        public static string RaidNow() => Json(RaidScene());

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(RaidNow(), RaidBefore());
            yield return MarkerSql(TerminalAt, TerminalBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return MarkerSql(TerminalBefore, TerminalAt);
            yield return SceneSql(RaidBefore(), RaidNow());
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Raid).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Raid} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("The scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        private static string Near(ScenePosition at) =>
            $"ABS(pos_x - {F(at.X)}) < 0.001 AND ABS(pos_y - {F(at.Y)}) < 0.001 AND ABS(pos_z - {F(at.Z)}) < 0.001";

        private static string Set(ScenePosition to) => $"pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)}";

        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {Raid} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        private static string MarkerSql(ScenePosition to, ScenePosition from) =>
            $"UPDATE mission_indicator SET {Set(to)} WHERE mission_id = {Raid} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND objective_id = {TerminalObjective} AND indicator_id = {TerminalMarker} AND ABS(radius - {F(MarkerRadius)}) < 0.001 AND {Near(from)};";
    }
}
