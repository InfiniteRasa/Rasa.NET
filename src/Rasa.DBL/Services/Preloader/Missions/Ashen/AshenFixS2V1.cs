using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job S2 (real client 2026-11-13, after fix S), a delta on 20271110002150 (fix S): set 19's and set 20's HQ "hall"
    /// places stand on the terrain heightmap alone (135.94 from x -40 to 40, z -373 to -355), which the real client has no floor on
    /// (fix S: it walked down the spiral tunnel ramp under it). The hall's objects move to Central Operations at the ramp's foot,
    /// the floor the client reaches (a mesh-only flood of the client floor from the tunnel's end deck, 2026-11-13; the round room
    /// at 103.6 round a central platform, x -14..24, z -340..-310):
    ///  - 1498's teleporter (scene object, marker 287) to (19.8, 103.62, -325.6) and Slocombe (scene creature, marker 3) to
    ///    (17, 103.62, -326.2), on the room's east ring; both used from (18.3, 103.65, -328.2).
    ///  - Pad 921 (to the biolab's service room 922) to (-7, 103.7, -325) and pad 925 (to the east wing's 926) to (-7, 103.62,
    ///    -318) on the west ring; pad 927 (to the Enslaver Fortress' 928) to (21.5, 103.62, -319) on the east ring. Class 8044
    ///    (not the Atta 25302): its frame stands on its east side, so the walks step on over its shallow north rim (921) or south
    ///    rim (925, 927). Their links are by id and stay. 7-8 m apart, the walks keep 2.5 m (2D) off the other pads' 2 m triggers.
    /// The server navmesh has the terrain plate, not the ramp: Central Operations is a navmesh region of its own, not joined to
    /// the deck or the charge. No scene of 1498, 1551, 1499 or 1501 moves an NPC (Slocombe stands where he comes through), so no
    /// route crosses between them. Hostile pools: the nearest (531084) 82 m away. ESTIMATE: retail's places are not known.
    /// Each UPDATE guarded on the exact values before; Down puts them back.
    /// </summary>
    public static class AshenFixS2V1
    {
        public const uint Blast = AshenBlastTheBiolabV1.MissionId;
        public const uint Facility = AshenConscriptionEntryNpcsV1.Facility;
        public const uint TeleporterObjective = AshenBlastTheBiolabV1.Teleporter, TeleporterMarker = AshenBlastTheBiolabV1.TeleporterIndicator;
        public const uint SlocombeObjective = AshenBlastTheBiolabV1.GiveToSlocombe, SlocombeMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 3;

        public static readonly ScenePosition TeleporterBefore = AshenBlastTheBiolabV1.TeleporterAt, TeleporterAt = new(19.8f, 103.62f, -325.6f);
        public static readonly ScenePosition SlocombeBefore = AshenBlastTheBiolabV1.SlocombeAt, SlocombeAt = new(17f, 103.62f, -326.2f);

        /// <summary>Where the walk to Slocombe and the teleporter ends (2.4 m from him, 3.0 m from it).</summary>
        public static readonly ScenePosition Stand = new(18.3f, 103.65f, -328.2f);

        /// <summary>Where the walk down the spiral ends, in Central Operations' north door.</summary>
        public static readonly ScenePosition RampFoot = new(7f, 103.81f, -340f);

        /// <summary>The hall pads: id, set 19's / set 20's place, fix S2's place.</summary>
        public static readonly (uint Id, (double X, double Y, double Z) Before, (double X, double Y, double Z) At)[] Pads =
        {
            (AshenConscriptionEntryNpcsV1.HallPad, AshenConscriptionEntryNpcsV1.HallPadAt, (-7, 103.7, -325)),
            (AshenConscriptionQueenNpcsV1.HallWingPad, AshenConscriptionQueenNpcsV1.HallWingPadAt, (-7, 103.62, -318)),
            (AshenConscriptionQueenNpcsV1.HallFortressPad, AshenConscriptionQueenNpcsV1.HallFortressPadAt, (21.5, 103.62, -319))
        };

        public static (double X, double Y, double Z) PadAt(uint id) => Pads.Single(pad => pad.Id == id).At;

        /// <summary>A pad's place after fix S2: its new one for a hall pad, else as given.</summary>
        public static (double X, double Y, double Z) Moved(uint id, (double X, double Y, double Z) at) => Pads.Any(pad => pad.Id == id) ? PadAt(id) : at;

        public const uint Evidence = 6;
        public const string Source = "repository:AshenFixS2V1";
        public const string Note =
            "Fix job S2 (real client 2026-11-13): the HQ hall (135.94) is terrain the client has no floor on; 1498's teleporter " +
            "(19.8, 103.62, -325.6) and Slocombe (17, 103.62, -326.2) with markers 287 and 3, and the hall pads 921 (-7, 103.7, -325), " +
            "925 (-7, 103.62, -318) and 927 (21.5, 103.62, -319), moved to Central Operations at the spiral ramp's foot. ESTIMATE.";

        /// <summary>1498's scene as fix M left it, the teleporter and Slocombe moved.</summary>
        public static MissionSceneDefinition BlastScene()
        {
            var scene = AshenFixMV1.BlastScene();
            scene.Actors[AshenBlastTheBiolabV1.TeleporterRole] = scene.Actors[AshenBlastTheBiolabV1.TeleporterRole] with { Position = TeleporterAt };
            scene.Actors[AshenBlastTheBiolabV1.SlocombeRole] = scene.Actors[AshenBlastTheBiolabV1.SlocombeRole] with { Position = SlocombeAt };
            return scene;
        }

        public static string BlastBefore() => AshenFixMV1.BlastNow();
        public static string BlastNow() => Json(BlastScene());

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(BlastNow(), BlastBefore());
            yield return MarkerSql(TeleporterObjective, TeleporterMarker, AshenBlastTheBiolabV1.ObjectMarkerRadius, P(TeleporterAt), P(TeleporterBefore));
            yield return MarkerSql(SlocombeObjective, SlocombeMarker, AshenBlastTheBiolabV1.NpcMarkerRadius, P(SlocombeAt), P(SlocombeBefore));
            foreach (var (id, before, at) in Pads)
                yield return PadSql(id, at, before);
        }

        public static IEnumerable<string> DownSql()
        {
            foreach (var (id, before, at) in Pads.Reverse())
                yield return PadSql(id, before, at);
            yield return MarkerSql(SlocombeObjective, SlocombeMarker, AshenBlastTheBiolabV1.NpcMarkerRadius, P(SlocombeBefore), P(SlocombeAt));
            yield return MarkerSql(TeleporterObjective, TeleporterMarker, AshenBlastTheBiolabV1.ObjectMarkerRadius, P(TeleporterBefore), P(TeleporterAt));
            yield return SceneSql(BlastBefore(), BlastNow());
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Blast).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Blast} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static (double X, double Y, double Z) P(ScenePosition at) => (at.X, at.Y, at.Z);

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("The scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        private static string Near((double X, double Y, double Z) at) =>
            $"ABS(pos_x - {F(at.X)}) < 0.001 AND ABS(pos_y - {F(at.Y)}) < 0.001 AND ABS(pos_z - {F(at.Z)}) < 0.001";

        private static string Set((double X, double Y, double Z) to) => $"pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)}";

        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {Blast} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        private static string MarkerSql(uint objective, uint indicator, double radius, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET {Set(to)} WHERE mission_id = {Blast} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND objective_id = {objective} AND indicator_id = {indicator} AND ABS(radius - {F(radius)}) < 0.001 AND {Near(from)};";

        private static string PadSql(uint id, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE teleporter SET {Set(to)} WHERE id = {id} AND map_context_id = {Facility} AND type = {AshenConscriptionEntryNpcsV1.LocalTeleporter} " +
            $"AND class_id = {AshenConscriptionEntryNpcsV1.PadClass} AND {Near(from)};";
    }
}
