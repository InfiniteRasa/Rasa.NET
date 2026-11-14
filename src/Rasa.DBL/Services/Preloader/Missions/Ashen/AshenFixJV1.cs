using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job J: a delta on set 15 (20271110001500, AshenIndraFacilityNpcsV1 / AshenThresholdV1, live) and set
    /// 16 (20271110001600, AshenDiggingForTheTruthV1 / AshenDesistanceV1, live), after set 16. Indra Caverns (2055). The data
    /// classes of sets 15 and 16 stay as they wrote them.
    ///
    /// To-fix row 36 (real client 2026-11-12 07:24, server-ashen20): the Watch Post's east pool 790566 (the Thrax Ravager 531009,
    /// one, radius 0) at (44, 16.35, -335) stood 17.5 m from the only way into the facility, the 3.3 m tunnel along z -328 from
    /// x 52 to fix I's door 23160 at (64, 16, -328); its Ravager stood in the tunnel's mouth (51.3, -328) and hit players from
    /// x 52 to 60. The pool moves to the Watch Post's north-west terrace, (-50, 14.56, -296) on the client floor (14.555; the
    /// navmesh 15.27, joined to the Watch Post), ESTIMATE:
    ///  - 106.9 m from the tunnel line and 115 m from the door: past the leash (BehaviorManager.MaxChaseDistance, 60 m from
    ///    home across the ground) and past the roam (a radius-0 pool's creature strolls within WanderZoneFallbackRadius, 10 m of
    ///    home) plus the 34 m place rule;
    ///  - 65 m and more from every walk leg of 1360 and 1395 (30 m plus the 10 m roam);
    ///  - 60.6 m from the Watch Post's west pool 790565 (the 60 m rule between hostile pools), 125 m from Kuutk's 790564;
    ///  - 34 m and more (roam included) from every set 15 and 16 place.
    /// The Watch Post's floor nearer (-5, -351) holds no point that keeps these: south of z -300 the west pool, the legs and the
    /// tunnel leave none with the roam counted, and north of it a pipe run and a rock rise cut the navmesh (to-fix row 36,
    /// AshenFixJTests). 1360's "Kill the Watch Post's two" stays (SpawnPoolCleared on both pools, by id); its marker 223 moves to
    /// the middle of the two pools, (-43, 14.14, -325.5), radius 45 kept (both pools' roam inside).
    ///
    /// To-fix row 38 (real client 2026-11-12 06:4x): 1447's drill 5 at (-312, 85.86, 227) stood in the lava river (the client's
    /// terra_arieki_lava_river_* meshes, LavaFlows; LavaDamage.InLava true at its floor and at 122 of 128 points within 8 m of
    /// it): 500-1000 Fire every 3 s (LAVA_DAMAGE_*); drills 4 (-311, 85.86, 245) and 6 (-322, 85.86, 211) stood in the same river,
    /// and set 16's walks to them crossed it. The three move onto the mining area's client floor west of the river, 10 m and
    /// more from every lava, 6 m and more apart from the other drills (centres), inside the mining marker (radius 40 round drill 4's old
    /// place), their markers 221 (objectives 9-11) with them, ESTIMATE:
    ///  - drill 4 to (-337, 90.43, 264) between drills 2 and 3;
    ///  - drill 5 to (-339, 90.43, 276) north of drill 3;
    ///  - drill 6 to (-341, 90.56, 282) north of drill 5;
    /// a row with drills 2 and 3, each drill's body (prop_bane_articulateddrill_v01's collision, 17 m along x and 5 m across)
    /// 0.5 m and more from every other's (row 43 follow-up: fix J's first places overlapped drills 2 and each other; the
    /// platform holds no three places 2.5 m apart, the 1 m grid probe of 2026-11-12), 17.1, 13.8 and 11.3 m from the lava.
    /// Objective 1's marker 134 over the mining operation (radius 40) stood at set 16's MiningAt (-311, 85.86, 245), in the river
    /// too: it moves to the mining platform among the six drills on the platform, (-334, 90.43, 255) (client floor 90.43; between drills 1 and 2), radius kept.
    ///
    /// Set 16's re-audit finding 1 (1452): Defeat Overlord Kretas (6) opened only when Locate Bane Central Administration (5)
    /// completed, and 5 is each member's own walk into the area: a member who shot Kretas from outside its 16 m got no kill.
    /// Now 3 (the generator) opens 6 together with 5 (two more actions of 3: reveal 6, activate 6); 5 stays the area objective
    /// and still opens 6 (a no-op once 6 is open or done: MissionActionPlanner; a member mid-way keeps that way in). The
    /// hidden 9 (the hand, open from the accept) and 7's order (opened by 6) are unchanged.
    ///
    /// Plain SQL; each UPDATE only where the row holds the set's exact stored value (the scene JSON; a pool or marker within
    /// 0.001: mission_indicator keeps single floats); the two actions as guarded INSERTs; Down puts it all back and deletes fix
    /// J's actions and evidence rows.
    /// </summary>
    public static class AshenFixJV1
    {
        public const uint Threshold = AshenThresholdV1.MissionId, Digging = AshenDiggingForTheTruthV1.MissionId, Desistance = AshenDesistanceV1.MissionId;

        /// <summary>Row 36: the Watch Post's east pool (the Ravager), set 15's place and fix J's.</summary>
        public const uint Pool = AshenIndraFacilityNpcsV1.WatchPostEast;
        public static readonly (double X, double Y, double Z) PoolBefore = AshenIndraFacilityNpcsV1.WatchPostEastAt, PoolAt = (-50, 14.56, -296);

        /// <summary>1360's marker 223 over the Watch Post (objective 3): set 15's place and fix J's, the middle of the two pools.</summary>
        public const uint WatchPostObjective = AshenThresholdV1.WatchPost, WatchPostMarker = AshenThresholdV1.WatchPostIndicator;
        public static readonly (double X, double Y, double Z) MarkerBefore = AshenThresholdV1.WatchPostMarkerAt, MarkerAt = (-43, 14.14, -325.5);

        /// <summary>Row 38: 1447's drills 4-6 (objectives 9-11, markers 221): fix J's places (set 16's in the lava river).</summary>
        public static readonly (int Number, ScenePosition At)[] Drills =
        {
            (4, new(-337f, 90.43f, 264f)), (5, new(-339f, 90.43f, 276f)), (6, new(-341f, 90.56f, 282f))
        };
        public const uint DrillMarker = AshenDiggingForTheTruthV1.DrillIndicator;


        /// <summary>1447 objective 1's marker 134 (r40): set 16's place (MiningAt, in the lava river) and fix J's (the mining platform among the six drills).</summary>
        public const uint MiningObjective = AshenDiggingForTheTruthV1.DestroyDrills, MiningMarker = AshenDiggingForTheTruthV1.DrillsIndicator;
        public static readonly (double X, double Y, double Z) MiningMarkerBefore = AshenDiggingForTheTruthV1.MiningAt, MiningMarkerAt = (-334, 90.43, 255);

        /// <summary>A drill's place after fix J (set 16's for drills 1-3).</summary>
        public static ScenePosition DrillAt(int number)
        {
            foreach (var (moved, at) in Drills)
                if (moved == number)
                    return at;
            return AshenDiggingForTheTruthV1.DrillsAt[number - 1];
        }

        public static ScenePosition DrillBefore(int number) => AshenDiggingForTheTruthV1.DrillsAt[number - 1];
        public static uint DrillObjective(int number) => AshenDiggingForTheTruthV1.DrillObjective(number);

        /// <summary>1452: 3's two new actions (after set 16's 1-4): reveal and activate 6.</summary>
        public const uint OpenerObjective = AshenDesistanceV1.Generator, KretasObjective = AshenDesistanceV1.DefeatKretas;
        public const uint RevealAction = 5, ActivateAction = 6;

        public const uint ThresholdEvidence = 5, DiggingEvidence = 4, DesistanceEvidence = 4;
        public const string Source = "repository:AshenFixJV1";

        public const string ThresholdNote =
            "Fix job J (to-fix row 36): the Watch Post's east pool 790566 (Ravager 531009) from (44, 16.35, -335), 17.5 m from the " +
            "facility's entrance tunnel (its Ravager stood in the tunnel's mouth), to the Watch Post's north-west terrace (-50, 14.56, " +
            "-296): 106.9 m from the tunnel (leash 60 m), 65 m+ from the walks, 60.6 m from the west pool; marker 223 to the two " +
            "pools' middle (-43, 14.14, -325.5), r45. ESTIMATE.";
        public const string DiggingNote =
            "Fix job J (to-fix row 38): drills 4-6 out of the lava river (LavaFlows) to the mining area's floor west of it: 4 " +
            "(-311, 85.86, 245) -> (-337, 90.43, 264), 5 (-312, 85.86, 227) -> (-339, 90.43, 276), 6 (-322, 85.86, 211) " +
            "-> (-341, 90.56, 282), 11 m+ from lava, each body 0.5 m+ from the others; markers 221 (objectives 9-11) with them; marker 134 " +
            "(objective 1, r40) from (-311, 85.86, 245) to the platform among the drills (-334, 90.43, 255). ESTIMATE.";
        public const string DesistanceNote =
            "Fix job J (set 16 re-audit 1): 3 opens 6 (reveal, activate) with 5, so a member who shoots Kretas from outside Central " +
            "Administration's 16 m gets his kill; 5 stays the area objective (its own opening of 6 a no-op then). Row 43: the charge from " +
            "(-406, 176.31, -303), inside the generator's body, to its east face (-392.3, 175.81, -301); marker 214 to (-395, 175.81, -301). ESTIMATE.";

        /// <summary>1447's scene with fix J's drills 4-6.</summary>
        public static MissionSceneDefinition DiggingScene()
        {
            var scene = AshenDiggingForTheTruthV1.Scene();
            foreach (var (number, at) in Drills)
            {
                var role = AshenDiggingForTheTruthV1.DrillRole(number);
                scene.Actors[role] = scene.Actors[role] with { Position = at };
            }
            return scene;
        }

        /// <summary>
        /// To-fix row 43 (real client 2026-11-12): 1452's Explosive Charge (-406, 176.31, -303) stood inside the Outpost Generator's
        /// body (arch_bane_outpost_generator_v01, 17 x 27 m, its collision from x -409 to -393 at z -301): no client reached it, a
        /// use from 14 m did nothing. It moves to the generator's east face, on the floor the client uses (175.81), touching the
        /// generator's east rim (x -393), ESTIMATE. Marker 214 (objective 3, r6) moves between it and the generator.
        /// </summary>
        public static readonly ScenePosition ChargeBefore = AshenDesistanceV1.ChargeAt, ChargeAt = new(-392.3f, 175.81f, -301f);
        public const uint GeneratorObjective = AshenDesistanceV1.Generator, GeneratorMarker = AshenDesistanceV1.GeneratorIndicator;
        public static readonly (double X, double Y, double Z) GeneratorMarkerBefore =
            (AshenDesistanceV1.GeneratorAt.X, AshenDesistanceV1.GeneratorAt.Y, AshenDesistanceV1.GeneratorAt.Z), GeneratorMarkerAt = (-395, 175.81, -301);

        /// <summary>1452's scene with fix J's charge.</summary>
        public static MissionSceneDefinition DesistanceScene()
        {
            var scene = AshenDesistanceV1.Scene();
            scene.Actors[AshenDesistanceV1.ChargeRole] = scene.Actors[AshenDesistanceV1.ChargeRole] with { Position = ChargeAt };
            return scene;
        }

        public static string DesistanceSceneBefore() => Json(AshenDesistanceV1.Scene());
        public static string DesistanceSceneNow() => Json(DesistanceScene());

        public static string DiggingSceneBefore() => Json(AshenDiggingForTheTruthV1.Scene());
        public static string DiggingSceneNow() => Json(DiggingScene());

        public static IEnumerable<string> UpSql()
        {
            yield return PoolSql(PoolAt, PoolBefore);
            yield return MarkerSql(Threshold, WatchPostObjective, WatchPostMarker, MarkerAt, MarkerBefore);
            yield return SceneSql(Digging, DiggingSceneNow(), DiggingSceneBefore());
            foreach (var (number, at) in Drills)
                yield return MarkerSql(Digging, DrillObjective(number), DrillMarker, (at.X, at.Y, at.Z), (DrillBefore(number).X, DrillBefore(number).Y, DrillBefore(number).Z));
            yield return MarkerSql(Digging, MiningObjective, MiningMarker, MiningMarkerAt, MiningMarkerBefore);
            yield return SceneSql(Desistance, DesistanceSceneNow(), DesistanceSceneBefore());
            yield return MarkerSql(Desistance, GeneratorObjective, GeneratorMarker, GeneratorMarkerAt, GeneratorMarkerBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return $"DELETE FROM mission_action WHERE mission_id = {Desistance} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                         $"AND objective_id = {OpenerObjective} AND transition_id = 1 AND action_id IN ({RevealAction}, {ActivateAction}) " +
                         $"AND target_objective_id = {KretasObjective};";
            yield return MarkerSql(Desistance, GeneratorObjective, GeneratorMarker, GeneratorMarkerBefore, GeneratorMarkerAt);
            yield return SceneSql(Desistance, DesistanceSceneBefore(), DesistanceSceneNow());
            yield return MarkerSql(Digging, MiningObjective, MiningMarker, MiningMarkerBefore, MiningMarkerAt);
            foreach (var (number, at) in Drills)
                yield return MarkerSql(Digging, DrillObjective(number), DrillMarker, (DrillBefore(number).X, DrillBefore(number).Y, DrillBefore(number).Z), (at.X, at.Y, at.Z));
            yield return SceneSql(Digging, DiggingSceneBefore(), DiggingSceneNow());
            yield return MarkerSql(Threshold, WatchPostObjective, WatchPostMarker, MarkerBefore, MarkerAt);
            yield return PoolSql(PoolBefore, PoolAt);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            var desistance = AshenMissionDataV1.Existing(migration, Desistance);
            desistance.Action(OpenerObjective, RevealAction, MissionActionKind.RevealObjective, targetObjective: KretasObjective);
            desistance.Action(OpenerObjective, ActivateAction, MissionActionKind.ActivateObjective, targetObjective: KretasObjective,
                state: MissionObjectiveState.Incomplete);
            AshenMissionDataV1.Existing(migration, Threshold).Evidence(ThresholdEvidence, MissionEvidenceSourceKind.Reconstruction, Source, ThresholdNote, 0.3);
            AshenMissionDataV1.Existing(migration, Digging).Evidence(DiggingEvidence, MissionEvidenceSourceKind.Reconstruction, Source, DiggingNote, 0.3);
            desistance.Evidence(DesistanceEvidence, MissionEvidenceSourceKind.Reconstruction, Source, DesistanceNote, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id IN ({Threshold}, {Digging}, {Desistance}) " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>The scene as the codec writes it (as AshenMissionDataV1.Enable); plain JSON with no quote or backslash to escape.</summary>
        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("The scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        private static string SceneSql(uint mission, string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        private static string PoolSql((double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE spawnpool SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)} WHERE id = {Pool} AND map_context_id = {AshenIndraFacilityNpcsV1.Caverns} " +
            $"AND ABS(pos_x - {F(from.X)}) < 0.001 AND ABS(pos_y - {F(from.Y)}) < 0.001 AND ABS(pos_z - {F(from.Z)}) < 0.001;";

        private static string MarkerSql(uint mission, uint objective, uint indicator, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)} WHERE mission_id = {mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {objective} AND indicator_id = {indicator} " +
            $"AND ABS(pos_x - {F(from.X)}) < 0.001 AND ABS(pos_y - {F(from.Y)}) < 0.001 AND ABS(pos_z - {F(from.Z)}) < 0.001;";
    }
}
