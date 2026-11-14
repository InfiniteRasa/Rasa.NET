using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job I: a delta on migration 20271110001500 (set 15, Indra Caverns; AshenThresholdV1, live) after
    /// fix H (20271110001510, live). It sorts after fix H and before set 16 (20271110001600). AshenThresholdV1 stays as set 15
    /// wrote it.
    ///
    /// To-fix row 34 (real client 2026-11-11 23:15-23:35, server-ashen18): no set 15 place in Indra Caverns (2055) was reached
    /// or seen. The cause is not the client collision we read: the survey walked the server's navmesh path to each place
    /// (the tester's place.py: cwalk.py, visual.walk), and the navmesh of 2055 is islands (upstream N29). Each stop is where
    /// that path ends (the navmesh's partial path from the pillbox: (54.1, -323.6) for the facility door, (55.3, 17.0, -339.2)
    /// for Kuutk, (54.1, -320.8) for Aksrah, (38.9, -340.8) for the Science Lab, (24.5, -302.4) for the crate, (-22.3,
    /// -299.6) for Drof, (12.1, 10.2, -302.4) for Naimi; the tester's stops within 1.7 m of them), and the wall the camera
    /// saw at each stop is in the collision we read (AshenFixITests.TheSurveysStopsAreTheNavmeshsIslandEnds). Set 15's
    /// client-floor walks take other ways, which the survey did not walk.
    ///
    /// Two places move all the same:
    ///  - Guard Rolim Bendal's body (class 24995) from (5, 16, -347), outside the pillbox (ArchHumOutpostTrenchBunkerV01 at
    ///    (14, 16, -343)) behind its south-west wall, to (9.5, 18.52, -336) on the pillbox's floor: TaRapedia has "the keycard
    ///    on the body in the pillbox in front of the facility door", the real client walked into the pillbox (on the navmesh's
    ///    path, joined to the Watch Post) and stood at (11.1, 18.5, -343.3), and the old place is on a navmesh island of its
    ///    own. The tester proposed (14, 18.5, -340); this one is 5.7 m from it, 3.5 m and more from the walls, 34.5 m outside
    ///    the Watch Post's east pool (ESTIMATE). Its marker 224 moves with it.
    ///  - The facility door: the client's door for the facility is the one its entrance is built for. The Security Check
    ///    Point's entrance (ArchBrannCorridorEntrance01V01, class 9775, at (64, 16, -328), yaw +90 degrees) is an open tunnel
    ///    3.3 m wide from x 52 to 62 along z -328 (its collision and render have no leaf across it); its mesh has a bone named
    ///    "door Proxy" at its origin, and the client has the door made for it: UsableDoorBrannCorridorEntranceDoorV01 (23160,
    ///    arch_brann_corridor_entrance_01_door_v01.geo, a door (augmentation 10) with USE_DOOR_STATE_CLOSED 31 / OPEN 91 and
    ///    usabledata None: use argument 1, as 10749), whose collision box stands at x 59.8-61.7 across the tunnel when it is
    ///    placed at the entrance's origin and turn. No .map places it (as TunnelDoors' Bane mouths), so the server puts it
    ///    there: our personal door moves from the jamb (82, 16.5, -328), class 10749 at -90 degrees, to (64, 16, -328), class
    ///    23160 at +90 degrees. The "closed dark door panel" the tester saw from (55.5, 16.0, -323.1) is the entrance shell's
    ///    face (ArchBrannCorridorEntranceShell01V01) north of the tunnel's mouth. ESTIMATE: retail's door class and place (the
    ///    mesh names, the proxy bone, the box's fit in the tunnel).
    ///
    /// The mission's logic (objectives, the key card lock, states, use argument, signals) stays as set 15's.
    ///
    /// Plain SQL; each UPDATE only where the row holds set 15's exact stored value (the scene JSON; the marker within 0.001:
    /// mission_indicator keeps single floats); Down puts them back the same way and deletes fix I's evidence row.
    /// </summary>
    public static class AshenFixIV1
    {
        public const uint Mission = AshenThresholdV1.MissionId, Objective = AshenThresholdV1.FindBendal, Marker = AshenThresholdV1.BendalIndicator;

        /// <summary>Bendal's body: set 15's place and fix I's (the pillbox's floor, client floor 18.52; ESTIMATE).</summary>
        public static readonly ScenePosition BendalBefore = AshenThresholdV1.BendalAt, BendalAt = new(9.5f, 18.52f, -336f);

        /// <summary>The facility door as set 15 stored it: the 4 m corridor door in the jamb (84, 16.5, -328).</summary>
        public const uint DoorClassBefore = AshenThresholdV1.DoorClass;
        public static readonly ScenePosition DoorBefore = AshenThresholdV1.DoorAt;
        public const double DoorOrientationBefore = AshenThresholdV1.DoorOrientation;

        /// <summary>
        /// Fix I's facility door: UsableDoorBrannCorridorEntranceDoorV01 at the Security Check Point entrance's origin and turn
        /// (the .map: ArchBrannCorridorEntrance01V01 at (64, 16, -328), yaw +pi/2), its leaf across the tunnel at x 59.8-61.7.
        /// </summary>
        public const uint DoorClass = 23160;
        public static readonly ScenePosition DoorAt = new(64f, 16f, -328f);
        public const double DoorOrientation = Math.PI / 2;

        /// <summary>The door's collision box in its own frame (arch_brann_corridor_entrance_01_door_v01.geo, "ABV Door"): x +-1.7, y -0.5..5.2, z -4.2..-2.3.</summary>
        public const float LeafHalfWidth = 1.7f, LeafNear = -4.2f, LeafFar = -2.3f;

        public const uint EvidenceId = 4;
        public const string Source = "repository:AshenFixIV1";

        public const string Note =
            "Fix job I (to-fix row 34): the survey's stops were the 2055 navmesh's island ends (upstream N29), not client walls we " +
            "miss. Bendal's body (24995) from (5, 16, -347), behind the pillbox's wall, to (9.5, 18.52, -336) in the pillbox " +
            "(TaRapedia: the body in the pillbox in front of the facility door); marker 224 with it. The facility door from the jamb " +
            "(82, 16.5, -328) class 10749 at -pi/2 to the Security Check Point entrance's door proxy (64, 16, -328) at +pi/2, as " +
            "UsableDoorBrannCorridorEntranceDoorV01 23160 (closed 31, open 91, argument 1), its leaf across the 3.3 m tunnel. " +
            "ESTIMATE: the places and the door class.";

        /// <summary>1360's scene with fix I's places and door.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = AshenThresholdV1.Scene();
            scene.Actors[AshenThresholdV1.CorpseRole] = scene.Actors[AshenThresholdV1.CorpseRole] with { Position = BendalAt };
            scene.Actors[AshenThresholdV1.DoorRole] = scene.Actors[AshenThresholdV1.DoorRole] with
            {
                TemplateId = DoorClass,
                Position = DoorAt,
                Orientation = DoorOrientation
            };
            return scene;
        }

        public static string SceneBefore() => Json(AshenThresholdV1.Scene());
        public static string SceneNow() => Json(Scene());

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(SceneNow(), SceneBefore());
            yield return MarkerSql(BendalAt, BendalBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return MarkerSql(BendalBefore, BendalAt);
            yield return SceneSql(SceneBefore(), SceneNow());
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Mission).Evidence(EvidenceId, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Mission} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {EvidenceId} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>The scene as the codec writes it (as AshenMissionDataV1.Enable); plain JSON with no quote or backslash to escape.</summary>
        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("1360's scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        /// <summary>1360's scene from one JSON to the other, only where the row holds the one.</summary>
        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {Mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        /// <summary>Marker 224 (1360.1, on Bendal) from one place to the other, only where it stands within 0.001 of the one (single floats).</summary>
        private static string MarkerSql(ScenePosition to, ScenePosition from) =>
            $"UPDATE mission_indicator SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)} WHERE mission_id = {Mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {Objective} AND indicator_id = {Marker} " +
            $"AND ABS(pos_x - {F(from.X)}) < 0.001 AND ABS(pos_y - {F(from.Y)}) < 0.001 AND ABS(pos_z - {F(from.Z)}) < 0.001;";
    }
}
