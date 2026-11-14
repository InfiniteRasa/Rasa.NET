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
    /// Ashen fix job V2 (to-fix row 64, second try, real client 2026-11-14), a delta on 20271110002180 (fix V), Bane Conscription
    /// Facility (1988): 1501's Life Support System 26423 (UsableThreeStateAttaQueenControlDeviceV01) hangs from the ceiling. Its use
    /// point HPS_Damage1 is 19.8 m above the floor in 10000001 and 8.8 m in 10000002 (8.7 m 3D from a player under it), and the
    /// client's use check is the 3D distance to DAMAGE1 against 6 m (client useobject.pyo _InUseRadiusOf): it can never be used
    /// from the floor. As Plateau 1979 (to-fix #32, VERIFIED in the real client), a Bane console takes the use:
    ///  - The scene's device role (<see cref="AshenSaveTheQueenV1.DeviceRole"/>) is now a console, UsableTwoStateBaneConsoleSwitchV01
    ///    7468 (ready 55, set 56, argument 1), on the floor 2.8 m in front of the device toward the queen, named 153 "Life Support
    ///    System" (as 1451's four Life Support Systems, 7468 with name 153, real-client PASS 2026-11-12; 1499's Cell Door Control,
    ///    7468 later enabled, PASS 2026-11-14). It keeps the role's use (objective 4's event), its "usable once 3 is done" and "done
    ///    once 4 is done" steps; when 4 is done it goes to 56 (set), not usable. The role is kept so that a run that made it and
    ///    switched it on before this fix (its journal: the role's Ensure and SetInteraction) gets the usable console on its next
    ///    rebuild: a scene's rebuild replays the run's journaled effects (SceneApplication.Reconcile, DesiredEffects), not the
    ///    sequences, so a new actor or step does not reach a run that ran those sequences already (fix V's 10000002 step did not).
    ///  - The device (26423) is a new role, <see cref="VisualRole"/>, the visual only: never usable, spawned in 10000002 (the
    ///    lower pose), made again when 4 is done and taken to 10000003 (its chain's end). The second Ensure lets a run that ran
    ///    sequence 0 before this fix (no device of the new role) get the device at the end; in a later run it is a no-op.
    ///  - Objective 4's marker (unnamed 4, radius kept) moves to the console.
    /// One scene row (guarded on fix V's exact JSON), one marker row (guarded on its place and radius), evidence row 5. Down puts
    /// them back. ESTIMATE: the console's place and orientation (no source places it); 7468's DAMAGE1 is not measured (the model
    /// stands at most <see cref="ConsoleUseHeight"/> m tall, so the use point is within the client's 6 m from the stand).
    /// </summary>
    public static class AshenFixV2V1
    {
        public const uint Queen = AshenSaveTheQueenV1.MissionId;
        public const uint Objective = AshenSaveTheQueenV1.EndTheSuffering;
        public const uint Marker = Rasa.Structures.MissionIndicator.UnnamedFrom + 4;
        public const double MarkerRadius = AshenSaveTheQueenV1.ObjectMarkerRadius;

        public const uint ConsoleClass = 7468;          // UsableTwoStateBaneConsoleSwitchV01
        public const uint ConsoleReady = 55, ConsoleSet = 56;
        public const float ConsoleUseHeight = 2.5f;     // ESTIMATE, upper bound checked on the client's collision mesh in the tests
        public static readonly ScenePosition ConsoleAt = new(8.5f, 78.11f, 160.5f);
        public const double ConsoleOrientation = 0;     // ESTIMATE, as 1499's Cell Door Control
        /// <summary>Where the player stands to use the console (the walk's end, 2 m south of it).</summary>
        public static readonly ScenePosition ConsoleStand = new(8.5f, 78.11f, 158.5f);

        public const string VisualRole = "bcf-life-support-device";
        public const string VisualCreateKey = "create-bcf-life-support-device", VisualAtRestKey = "life-support-device-at-rest",
            VisualEndKey = "life-support-device-end";

        public const uint Evidence = 5;
        public const string Source = "repository:AshenFixV2V1";
        public const string Note =
            "Fix job V2 (to-fix row 64, real client 2026-11-14): the hanging Life Support System 26423's use point HPS_Damage1 is " +
            "19.8 m (10000001) or 8.8 m (10000002) above the floor, past the client's 6 m use check. As 1979 (to-fix #32), a Bane " +
            "console 7468 (name 153) on the floor at (8.5, 78.11, 160.5) takes the device role's use (objective 4); the device stays " +
            "as a visual (role bcf-life-support-device, 10000002, 10000003 after 4); marker 4 on the console. ESTIMATE.";

        /// <summary>1501's scene after fix V (live 2026-11-14).</summary>
        public static MissionSceneDefinition QueenSceneBefore() => AshenFixVV1.QueenScene();

        /// <summary>1501's scene with the console on the device role and the device as a visual role.</summary>
        public static MissionSceneDefinition QueenScene()
        {
            var scene = QueenSceneBefore();
            var role = AshenSaveTheQueenV1.DeviceRole;
            var device = scene.Actors[role];
            scene.Actors[role] = device with
            {
                TemplateId = ConsoleClass, Position = ConsoleAt, Orientation = ConsoleOrientation, InitialObjectState = ConsoleReady
            };
            scene.Actors[VisualRole] = new SceneActorDefinition(VisualRole, SceneActorKind.Object, AshenSaveTheQueenV1.DeviceClass,
                AshenSaveTheQueenV1.DeviceAt, InitiallyInteractable: false, InitialObjectState: AshenFixVV1.DeviceActive,
                NameOverrideId: AshenSaveTheQueenV1.DeviceName);
            scene.Sequences[0].World.Add(new EnsureActorIntent(VisualCreateKey, VisualRole));

            var ready = scene.Sequences[AshenSaveTheQueenV1.DeviceReady].World;
            ready.Remove(ready.OfType<TransitionObjectStateIntent>().Single(intent => intent.OperationKey == AshenFixVV1.ActiveKey));

            var atRest = scene.Sequences[AshenSaveTheQueenV1.QueenAtRest].World;
            var off = atRest.OfType<TransitionObjectStateIntent>().Single(intent => intent.Role == role);
            atRest[atRest.IndexOf(off)] = new TransitionObjectStateIntent(off.OperationKey, role, ConsoleSet);
            atRest.Add(new EnsureActorIntent(VisualAtRestKey, VisualRole));
            atRest.Add(new TransitionObjectStateIntent(VisualEndKey, VisualRole, AshenFixVV1.DeviceEnd));
            return scene;
        }

        public static string QueenBefore() => Json(QueenSceneBefore());
        public static string QueenNow() => Json(QueenScene());

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(QueenNow(), QueenBefore());
            yield return MarkerSql(ConsoleAt, AshenSaveTheQueenV1.DeviceAt);
        }

        public static IEnumerable<string> DownSql()
        {
            yield return SceneSql(QueenBefore(), QueenNow());
            yield return MarkerSql(AshenSaveTheQueenV1.DeviceAt, ConsoleAt);
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

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

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

        private static string MarkerSql(ScenePosition to, ScenePosition from) =>
            $"UPDATE mission_indicator SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)} WHERE mission_id = {Queen} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {Objective} AND indicator_id = {Marker} " +
            $"AND ABS(radius - {F(MarkerRadius)}) < 0.001 AND ABS(pos_x - {F(from.X)}) < 0.001 AND ABS(pos_y - {F(from.Y)}) < 0.001 " +
            $"AND ABS(pos_z - {F(from.Z)}) < 0.001;";
    }
}
