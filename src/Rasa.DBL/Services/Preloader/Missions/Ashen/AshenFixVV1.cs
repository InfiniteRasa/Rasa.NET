using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job V (to-fix row 64, real client 2026-11-14), a delta on 20271110002170 (fix T), Bane Conscription Facility
    /// (1988): 1501's Life Support System (26423 UsableThreeStateAttaQueenControlDeviceV01) could not be used. Set 20 spawned it in
    /// 10000003 and turned it "off" to 10000001 (AshenSaveTheQueenV1.DeviceOn / DeviceOff, an ESTIMATE). The client's usabledata
    /// for 26423 is a one-way chain (client tables, 2026-11-14): 10000001 idle (anim 52, SFX 46264) -> 10000002 (transition anim
    /// 4 + SFX 46262, idle anim 5 + SFX 46265) -> 10000003 the end (transition anim 10000008 + SFX 46263, static anim 10000006).
    /// In 10000003 the client has no use point (HPS_Damage1 hangs on a bone of the end model; GetUsableMaxRange 0), so its use
    /// check (client/actions/useobject.pyo CheckAction / _InUseRadiusOf) said "The target is out of range." at 1 m.
    ///  - The device spawns in 10000001 (idle), goes to 10000002 when 3 is done (DeviceReady, next to its SetInteraction), and to
    ///    10000003 (the end) when 4 is done (QueenAtRest). Everything else in 1501's scene is kept.
    /// One row: 1501's mission_scene_binding, guarded on its exact JSON (set 20's scene with its indicator maps); Down restores it.
    /// ESTIMATE: retail's states for this device are not known; the chain order is the client's.
    /// </summary>
    public static class AshenFixVV1
    {
        public const uint Queen = AshenSaveTheQueenV1.MissionId;

        /// <summary>The client's usabledata chain of 26423: idle, active, end.</summary>
        public const uint DeviceIdle = 10000001, DeviceActive = 10000002, DeviceEnd = 10000003;
        public const uint DeviceSpawnBefore = AshenSaveTheQueenV1.DeviceOn, DeviceAtRestBefore = AshenSaveTheQueenV1.DeviceOff;
        public const string ActiveKey = "life-support-active";

        public const uint Evidence = 4;
        public const string Source = "repository:AshenFixVV1";
        public const string Note =
            "Fix job V (to-fix row 64, real client 2026-11-14): 1501's Life Support System 26423 spawned in 10000003, the end of " +
            "its client usabledata chain (10000001 idle -> 10000002 -> 10000003 end), where the client has no use point ('out of " +
            "range' at 1 m). Now it spawns in 10000001, goes to 10000002 when 3 is done and to 10000003 when 4 is done. ESTIMATE.";

        /// <summary>1501's scene as set 20 enabled it (AshenSaveTheQueenV1.Up: markers 6 and 7 on Ashen Desert and Thunderhead).</summary>
        public static MissionSceneDefinition QueenSceneBefore()
        {
            var scene = AshenSaveTheQueenV1.Scene();
            scene.IndicatorMaps = new Dictionary<uint, Dictionary<uint, uint>>
            {
                [AshenSaveTheQueenV1.ToYamamoto] = new() { [Rasa.Structures.MissionIndicator.UnnamedFrom + 6] = AshenMissionDataV1.MapContextId },
                [AshenSaveTheQueenV1.ToJawan] = new() { [Rasa.Structures.MissionIndicator.UnnamedFrom + 7] = AshenSaveTheQueenV1.Thunderhead }
            };
            return scene;
        }

        /// <summary>1501's scene with the device's states on the client's chain.</summary>
        public static MissionSceneDefinition QueenScene()
        {
            var scene = QueenSceneBefore();
            var role = AshenSaveTheQueenV1.DeviceRole;
            scene.Actors[role] = scene.Actors[role] with { InitialObjectState = DeviceIdle };
            scene.Sequences[AshenSaveTheQueenV1.DeviceReady].World.Add(new TransitionObjectStateIntent(ActiveKey, role, DeviceActive));
            var atRest = scene.Sequences[AshenSaveTheQueenV1.QueenAtRest].World;
            var off = atRest.OfType<TransitionObjectStateIntent>().Single(intent => intent.Role == role);
            atRest[atRest.IndexOf(off)] = new TransitionObjectStateIntent(off.OperationKey, role, DeviceEnd);
            return scene;
        }

        public static string QueenBefore() => Json(QueenSceneBefore());
        public static string QueenNow() => Json(QueenScene());

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(QueenNow(), QueenBefore());
        }

        public static IEnumerable<string> DownSql()
        {
            yield return SceneSql(QueenBefore(), QueenNow());
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, Queen).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Queen} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("The scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {Queen} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";
    }
}
