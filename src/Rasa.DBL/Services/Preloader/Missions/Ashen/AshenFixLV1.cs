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
    /// Ligo Ashen Desert fix job L: a delta on migrations 20271110001700 (set 17, Avernus Outpost; AshenOverlySensitiveV1) and
    /// 20271110001900 (set 19, the Bane Conscription Facility; AshenBlastTheBiolabV1), both live. It sorts after set 19 and fix
    /// K (20271110001910).
    ///
    /// To-fix row 46 (real client 2026-11-12, server-ashen22, normal play): 1473's Universal Interior Teleporter (the scene's
    /// class 7895 at (28, 528.12, -106), locked to the card) sets the player down in the prison (-8, 520.01, -46), and the
    /// prison is a closed room in the client: the tester's probe found a walkable box of about x -19.4 .. -4.1, z -49.7 ..
    /// -37.5 at y 520, walls in 12 directions; the walk out to the east stops at once at (-8.1, 520.0, -45.6), at the foot
    /// of the stair up to the prison's east corridor; /stuck moves 2 m. There is no way out: 1473 cannot be turned in at Karter
    /// without /suicide. Set 17 took the prison as open: the client collision the server reads (arch_hum_fort_prison_v02.geo,
    /// class 3577 ArchHumBasePrison at (-10.71, 520, -45.24), one collision bone, its BVWS mesh) has that stair (x -7.45 ..
    /// -3.7, 35 degrees), a corridor at 522.35 to x 6.3 and a 30-degree ramp down to the outside at x 10.6, and nothing in
    /// the World DB or the .map stands in it. The real client stops at the stair's foot (ESTIMATE: a block the client has
    /// that the collision data the server reads does not; TaRapedia: the stash is "behind some pretty heavy security force
    /// fields").
    ///
    /// The fix (ESTIMATE, a deviation: retail's way out is not known): a one-way local teleporter pair, as Timora's 946 to 947
    /// (TimoraMinesContent) and the Few logos chamber's 988 to 989: the prison pad 923, class 7895 UsableTwoStateHumTeleporter
    /// (the client's human teleporter, the inbound teleporter's own class; shown running in TsState0,
    /// DynamicObjectManager.TeleporterRunningState: its collision a 3 m disc 0.6 m high at most, 3.6 m tall drawn, under the
    /// prison's 7.8 m ceiling) in the room's open middle, linked to 924, an arrival only (class 0: not drawn, no link: it
    /// takes nobody, DynamicObjectManager.LocalTeleporterWorker) beside the inbound teleporter on the landing strip's road. A
    /// player who steps onto 923 (within 2 m) is held 3 s and set down a metre over 924. Public, as every local teleporter:
    /// anyone in the room uses it at any time (with 1473 open, done, abandoned, or never taken), in the base map and in
    /// every squad copy (MapChannel templates clone the teleporters). Plain SQL, valid on SQLite and MySQL; each INSERT
    /// guarded (only where the row is not there; the link only where both pads are); Down takes out exactly these rows.
    /// 1473 gets evidence row 4.
    ///
    /// To-fix row 47 (coordinator, 2026-11-12): 1498's objective 1 (five Bio Material each, 50 % drops from the Caretaker
    /// 531011 and the Linker 531014) stalls in a squad's copy of the facility: its marked pool 531064 holds two or three of them
    /// (Linker 1-2, Caretaker 1), and a cleared pool there comes back only 45 min after its last death
    /// (SquadInstanceState, SquadInstances.RespawnMinutes). 1498's scene names 531064 and the parts pool 531066 as public-spawn
    /// actors (SceneActorKind.PublicSpawn, as 1452's Kretas pool, AshenDesistanceV1): a pool a scene names returns on its own
    /// respawn time (spawnpool.respown_time 1200: 120 s; SpawnPoolManager.ReturnsOnOwnTime), not the instance's. No intent
    /// names the two roles, so no creature of them is ever bound (a pool of several kinds). The scene row is updated only where
    /// it is set 19's exactly; Down puts set 19's back the same way. The parts pool's centre goes up 1.37 m onto the navmesh's
    /// floor (PartsPoolY; upstream row, guarded on its exact place, Down restores it). 1498 gets evidence row 4. ESTIMATE:
    /// retail's respawn.
    /// </summary>
    public static class AshenFixLV1
    {
        public const uint MapContextId = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint Mission = AshenOverlySensitiveV1.MissionId;

        /// <summary>The pads: the prison's (drawn as the client's human teleporter) and the arrival outside (class 0, not drawn).</summary>
        public const uint PrisonPad = 923, OutsidePad = 924, LocalTeleporter = 1;
        public const uint PrisonPadClass = AshenOverlySensitiveV1.TeleporterClass, OutsidePadClass = 0;
        public const string PrisonDescription = "Local Teleporter: Avernus Outpost prison, the way out (fix L)";
        public const string OutsideDescription = "Local Teleporter: Avernus Outpost, out of the prison (fix L)";

        /// <summary>
        /// Where the pads stand, on the client's floor (probe 2026-11-12, ESTIMATE): the prison pad in the room's open middle, 6.3 m
        /// west of the inbound arrival (-8, 520.01, -46) and 9.7 m from the stasis tube, 3 m and more from every crate, barrel and
        /// workstation (the pad's disc 1.5 m and a player's 0.5 m), inside the tester's walkable box; the arrival 4.2 m south-east of
        /// the inbound teleporter (28, 528.12, -106), on set 17's walk to it from the strip road (42, 528, -120), flat, nothing
        /// over it, 17.7 m from the tower's lift pad 952.
        /// </summary>
        public static readonly (double X, double Y, double Z) PrisonPadAt = (-14, 520.01, -44), OutsidePadAt = (31, 528.12, -109);

        /// <summary>
        /// The prison as the real client has it: the room's floor in the prison's collision (arch_hum_fort_prison_v02.geo at class
        /// 3577's place: x -19.73 .. -3.72, z -53.24 .. -37.2, y 520.01; its walls round it, the east stair's opening in the wall
        /// at x -3.72), with no walk out of it in the real client (the tester's probe, 2026-11-12, normal play). The server's
        /// collision has the room open to the east (the summary), so a client-floor walk check cannot see it: the tests take this
        /// box as a wall (AshenAvernusOutpostTests.ClientClosedRooms).
        /// </summary>
        public static readonly (double MinX, double MaxX, double MinZ, double MaxZ, double Y) PrisonBox = (-19.73, -3.72, -53.24, -37.2, 520);

        /// <summary>Where the tester's player could stand in it (the probe's box, 2026-11-12; crates and the stasis tube south of it).</summary>
        public static readonly (double MinX, double MaxX, double MinZ, double MaxZ) PrisonWalkable = (-19.4, -4.1, -49.7, -37.5);

        public const uint EvidenceId = 4;
        public const string Source = "repository:AshenFixLV1";
        public const string Note =
            "Fix job L (to-fix row 46): the prison is closed in the real client (walls in 12 directions, the east stair stops the " +
            "player; the server's collision has it open). One-way local teleporter 923 (class 7895, in the prison at (-14,520.01,-44)) " +
            "to 924 (class 0, arrival only, (31,528.12,-109) by the inbound teleporter). Public. ESTIMATE: retail's way out unknown; the places.";

        /// <summary>Row 47: 1498 and its two drop pools, named in its scene as public-spawn actors.</summary>
        public const uint Blast = AshenBlastTheBiolabV1.MissionId;
        public const string BioPoolRole = "biolab-bio-pool", PartsPoolRole = "biolab-parts-pool";
        public const uint BlastEvidenceId = 4;
        public const string BlastNote =
            "Fix job L (to-fix row 47): 1498's drop pools 531064 (bio: Linker 1-2, Caretaker 1) and 531066 (parts) named in its " +
            "scene as public-spawn actors, so in a squad's copy each comes back on its own 120 s (respown_time 1200), not 45 min " +
            "after its last death (SquadInstances.RespawnMinutes): five Bio Material each at 50 % stalled. ESTIMATE: retail's respawn.";

        /// <summary>1498's scene with fix L's two public-spawn actors.</summary>
        public static MissionSceneDefinition BlastScene()
        {
            var scene = AshenBlastTheBiolabV1.Scene();
            // Never name biolab-bio-pool or biolab-parts-pool in an intent: binding a public-spawn actor on a private map takes the
            // pool's one creature (SceneWorldAdapter.Ensure, SingleOrDefault), which throws on these pools of several creatures.
            // Named only, they make the pools return on their own time (SpawnPoolManager.ReturnsOnOwnTime).
            scene.Actors[BioPoolRole] = new SceneActorDefinition(BioPoolRole, SceneActorKind.PublicSpawn, AshenBlastTheBiolabV1.BioPool);
            scene.Actors[PartsPoolRole] = new SceneActorDefinition(PartsPoolRole, SceneActorKind.PublicSpawn, AshenBlastTheBiolabV1.PartsPool);
            return scene;
        }

        /// <summary>
        /// Audit R2: named in the scene, the parts pool's centre is a mission placement (MissionGroundTests): upstream's
        /// (-233.6, 103.9, -205.3) lies 1.37 m under the navmesh's floor there (105.27, straight above). Its height is set on the
        /// floor; x and z stay (its creatures were set down on the ground round it before too). Down restores upstream's value.
        /// </summary>
        public const double PartsPoolYBefore = 103.9000015258789, PartsPoolY = 105.27;     // upstream's stored value, exactly (as set 10's IdaBefore)

        public static string BlastSceneBefore() => Json(AshenBlastTheBiolabV1.Scene());
        public static string BlastSceneNow() => Json(BlastScene());

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(Blast, BlastSceneNow(), BlastSceneBefore());
            yield return PartsPoolSql(PartsPoolY, PartsPoolYBefore);
            yield return Pad(PrisonPad, PrisonPadClass, PrisonDescription, PrisonPadAt);
            yield return Pad(OutsidePad, OutsidePadClass, OutsideDescription, OutsidePadAt);
            yield return "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
                         $"SELECT t.id, {OutsidePad} FROM teleporter t WHERE t.id = {PrisonPad} AND EXISTS (SELECT 1 FROM teleporter u WHERE u.id = {OutsidePad}) " +
                         $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {PrisonPad});";
        }

        public static IEnumerable<string> DownSql()
        {
            yield return PartsPoolSql(PartsPoolYBefore, PartsPoolY);
            yield return SceneSql(Blast, BlastSceneBefore(), BlastSceneNow());
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {PrisonPad} AND to_teleporter_id = {OutsidePad};";
            yield return PadDown(OutsidePad, OutsidePadClass, OutsidePadAt);
            yield return PadDown(PrisonPad, PrisonPadClass, PrisonPadAt);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            // Guarded on its key (CrucibleSql.Insert), as fix H's and fix I's.
            AshenMissionDataV1.Existing(migration, Mission).Evidence(EvidenceId, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
            AshenMissionDataV1.Existing(migration, Blast).Evidence(BlastEvidenceId, MissionEvidenceSourceKind.Reconstruction, Source, BlastNote, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Blast} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {BlastEvidenceId} AND source_uri = '{Source}';");
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Mission} AND content_revision = '{AshenMissionDataV1.Revision}' " +
                          $"AND evidence_id = {EvidenceId} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string Pad(uint id, uint classId, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"SELECT {id}, {classId}, {LocalTeleporter}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, 0, m.map_context_id " +
            $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {id});";

        /// <summary>A pad's delete, only where it is fix L's own row (its class and place, as FewShaftTeleporter's Down).</summary>
        private static string PadDown(uint id, uint classId, (double X, double Y, double Z) at) =>
            $"DELETE FROM teleporter WHERE id = {id} AND class_id = {classId} AND type = {LocalTeleporter} AND map_context_id = {MapContextId} " +
            $"AND ABS(pos_x - {F(at.X)}) < 0.01 AND ABS(pos_y - {F(at.Y)}) < 0.01 AND ABS(pos_z - {F(at.Z)}) < 0.01;";

        private static string PartsPoolSql(double to, double from) =>
            $"UPDATE spawnpool SET pos_y = {F(to)} WHERE id = {AshenBlastTheBiolabV1.PartsPool} AND map_context_id = {AshenConscriptionEntryNpcsV1.Facility} " +
            $"AND ABS(pos_x - {F(AshenBlastTheBiolabV1.PartsPoolAt.X)}) < 0.001 AND ABS(pos_y - {F(from)}) < 0.001 AND ABS(pos_z - {F(AshenBlastTheBiolabV1.PartsPoolAt.Z)}) < 0.001;";

        private static string SceneSql(uint mission, string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        /// <summary>The scene as the codec writes it (as AshenMissionDataV1.Enable); plain JSON with no quote or backslash to escape.</summary>
        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("The scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
