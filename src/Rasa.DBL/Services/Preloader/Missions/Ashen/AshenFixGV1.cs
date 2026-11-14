using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job G: a delta on migration 20271110001300 (set 13, Indra Pass; AshenIndraPassNpcsV1,
    /// AshenGodOfStormsV1, AshenBondsOfBloodV1, live) and on set 2's and fix F's Atta pools; their data classes stay. It sorts
    /// after fix F (20271110001310), which deploys first.
    ///
    /// Item 1 (to-fix row 24, real client 2026-11-10, server-ashen13): the real client could not climb from (99, 294.49, 482) to
    /// (86, 299.74, 469) on set 13's way from the valley's south end to the Indra Pass waypoint (stops (89.4, 293.9, 468.0),
    /// (92.9, 292.6, 472.0), (92.4, 293.2, 472.0), (89.2, 293.9, 467.6), (88.5, 294.1, 466.9); the screenshot: a steep rock face).
    /// The leg runs slant up terrain of 52-59 degrees; our client-floor check measured only the rise along the line (0.3-0.6 m
    /// per metre, under its 1.1 m step) and took floors up to 60 degrees. The client climbs at most 55 degrees
    /// (tabula_rasa.exe 1.16.5.0, Client.Developer.MaxAngleToClimb default 55; AshenFixBTests.MaxClimbAngle). With that limit no
    /// way in to the Indra Pass waypoint's floor was found (a client-floor flood from the valley, White Oasis's side, and from
    /// the Ashoka Settlement waypoint, 31 m off every hostile pool's circle; only a way through the Bane Conscription Facility's
    /// grounds, past its pools, joins it: NOTES-upstream-bugs N23). So the waypoint's floor is no place for a new player's
    /// NPCs or objectives:
    ///  - Surveyor Malya Odesha 790480 and Researcher Endala Berish 790481 move into the valley, 40 m west of its south entry
    ///    where set 10's way from the White Oasis Post waypoint comes in, on the server's navmesh joined to that waypoint, 85 m or
    ///    more from set 10's Indra Pass Beam Manta pool 790369 (ESTIMATE; deviation from TaRapedia's "cave directly outside of
    ///    the Indra Caverns", /loc 312, 324, 293, 250 m south-east of them).
    ///  - God of Storms (1281)'s four resistors on the waypoint's floor (1, 2, 3, 7) move into the valley, 25 m or more from the
    ///    other six (ESTIMATE); their markers move with them, and 1282's Endala marker with her.
    ///  - Every set 13 walk is a client-floor walk from the new place (the slope limit and 31 m off every hostile pool's circle;
    ///    AshenIndraPassTests).
    ///
    /// Item 2 (coordinator, real client 2026-11-10 22:46, server-ashen14): a level 50 player with the Sonic rifle cannot win a
    /// pack of three Ashen Atta (8409 health each, about 2500 damage a 4 s burst; the three deal about 13000 damage in 20 s,
    /// ESTIMATE from the tester's log). The seven Atta pools of set 2 (790046-790051) and fix F (790378) put down one creature,
    /// not three (ESTIMATE); respawn (120 s), places and creatures stay. 1242 (10 Soldiers, 10 Workers), 1243 and 1287 stay
    /// possible on respawns.
    ///
    /// Plain SQL; each UPDATE only where the row holds set 13's (or set 2's / fix F's) exact stored values (markers within 0.001
    /// of them: mission_indicator keeps single floats); Down puts them back the same way.
    /// </summary>
    public static class AshenFixGV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;

        // ---- Item 1: the Indra Pass places ----

        /// <summary>The NPCs' places as set 13 wrote them and fix G's (the navmesh's heights, on the client's floor; probe 2026-11-11, ESTIMATE).</summary>
        public static readonly (double X, double Y, double Z) OdeshaBefore = AshenIndraPassNpcsV1.OdeshaAt, EndalaBefore = AshenIndraPassNpcsV1.EndalaAt;
        public static readonly (double X, double Y, double Z) OdeshaAt = (115, 304.69, 450), EndalaAt = (112, 304.54, 458);

        /// <summary>The resistors fix G moves (1-based numbers) and their new places (between the navmesh's and the client floor's heights; ESTIMATE).</summary>
        public static readonly (int Number, ScenePosition At)[] MovedResistors =
        {
            (1, new ScenePosition(158f, 312.2f, 541f)),
            (2, new ScenePosition(60f, 301f, 551f)),
            (3, new ScenePosition(201f, 317.56f, 525f)),
            (7, new ScenePosition(66f, 300.72f, 599f))
        };

        /// <summary>The ten resistors after fix G.</summary>
        public static ScenePosition[] ResistorsAt()
        {
            var at = AshenGodOfStormsV1.ResistorsAt.ToArray();
            foreach (var (number, now) in MovedResistors)
                at[number - 1] = now;
            return at;
        }

        /// <summary>1281's scene as set 13 stored it (AshenGodOfStormsV1.Up: the scene, its requirement and the CloseOther sequence) at the given resistors.</summary>
        public static MissionSceneDefinition StormsScene(ScenePosition[] resistorsAt)
        {
            var scene = AshenGodOfStormsV1.Scene(resistorsAt);
            scene.Requirement = AshenGodOfStormsV1.NotWhileTheOther(AshenBondsOfBloodV1.MissionId);
            scene.Sequences[AshenGodOfStormsV1.CloseOther] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(AshenBondsOfBloodV1.MissionId, AshenGodOfStormsV1.ClosedByOther, 1) }
            };
            return scene;
        }

        public static string StormsBefore() => Json(StormsScene(AshenGodOfStormsV1.ResistorsAt));
        public static string StormsNow() => Json(StormsScene(ResistorsAt()));

        /// <summary>1282's talk objective (Endala) and its marker number.</summary>
        public const uint EndalaObjective = 2, EndalaMarker = 2;

        // ---- Item 2: the Atta pools ----

        /// <summary>The Atta pools fix G thins: pool, creature (set 2's AttaPools and fix F's 790378).</summary>
        public static readonly (uint Pool, uint Creature)[] AttaPools =
            AshenMaunaBarrensNpcsV1.AttaPools.Select(pool => (pool.Pool, pool.Creature))
                .Append((AshenFixFV1.AttaPool, AshenFixFV1.AttaSoldier)).ToArray();

        public const uint AttaCountBefore = AshenMaunaBarrensNpcsV1.PoolCreatures;     // 3 (set 2, fix F)
        public const uint AttaCount = 1;                                               // ESTIMATE

        public const string Source = "repository:AshenFixGV1";

        public const string PlacesNote =
            "Fix job G (to-fix row 24): the real client could not climb (99, 482) -> (86, 469) on set 13's way to the Indra Pass " +
            "waypoint (52-59 degree terrain; the client climbs 55 at most, tabula_rasa.exe MaxAngleToClimb). No way in to the " +
            "waypoint's floor outside hostile reach (N23). Odesha (115, 304.69, 450) and Endala (112, 304.54, 458) stand 40 m west " +
            "of the valley's south entry, set 10's way from White Oasis Post; resistors 1, 2, 3, 7 in the valley. ESTIMATE: the places; " +
            "TaRapedia has them by the Indra Caverns cave.";

        public const string AttaNote =
            "Fix job G (coordinator, real client 2026-11-10 22:46): a level 50 player with the Sonic rifle could not win three " +
            "Ashen Atta (8409 health each; about 13000 damage in 20 s from the three, ESTIMATE from the log). The seven Atta pools " +
            "790046-790051 and 790378 put down one, not three; respawn 120 s, places and creatures kept. ESTIMATE: the count.";

        /// <summary>The evidence rows fix G writes: mission, evidence id (the next after set 13, sets 2 and fix F), note.</summary>
        public static readonly (uint Mission, uint Evidence, string Note)[] Evidences =
        {
            (AshenMissingScientistsV1.MissionId, 4, PlacesNote),
            (AshenGodOfStormsV1.MissionId, 4, PlacesNote),
            (AshenBondsOfBloodV1.MissionId, 4, PlacesNote),
            (AshenAngryAngryAttaV1.MissionId, 4, AttaNote),
            (AshenHarmfulMutationsV1.MissionId, 4, AttaNote),
            (AshenDesignerJewelryV1.MissionId, 6, AttaNote)
        };

        public static IEnumerable<string> UpSql()
        {
            yield return PoolSql(AshenIndraPassNpcsV1.Odesha, OdeshaAt, OdeshaBefore);
            yield return PoolSql(AshenIndraPassNpcsV1.Endala, EndalaAt, EndalaBefore);
            yield return SceneSql(StormsNow(), StormsBefore());
            foreach (var (number, now) in MovedResistors)
                yield return MarkerSql(AshenGodOfStormsV1.MissionId, (uint)number, (uint)number, Of(now), Of(AshenGodOfStormsV1.ResistorsAt[number - 1]));
            yield return MarkerSql(AshenBondsOfBloodV1.MissionId, EndalaObjective, EndalaMarker, EndalaAt, EndalaBefore);
            foreach (var (pool, creature) in AttaPools)
                yield return CountSql(pool, creature, AttaCount, AttaCountBefore);
        }

        public static IEnumerable<string> DownSql()
        {
            foreach (var (pool, creature) in AttaPools.Reverse())
                yield return CountSql(pool, creature, AttaCountBefore, AttaCount);
            yield return MarkerSql(AshenBondsOfBloodV1.MissionId, EndalaObjective, EndalaMarker, EndalaBefore, EndalaAt);
            foreach (var (number, now) in MovedResistors.Reverse())
                yield return MarkerSql(AshenGodOfStormsV1.MissionId, (uint)number, (uint)number, Of(AshenGodOfStormsV1.ResistorsAt[number - 1]), Of(now));
            yield return SceneSql(StormsBefore(), StormsNow());
            yield return PoolSql(AshenIndraPassNpcsV1.Endala, EndalaBefore, EndalaAt);
            yield return PoolSql(AshenIndraPassNpcsV1.Odesha, OdeshaBefore, OdeshaAt);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            foreach (var (mission, evidence, note) in Evidences)
                AshenMissionDataV1.Existing(migration, mission).Evidence(evidence, MissionEvidenceSourceKind.Reconstruction, Source, note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var (mission, evidence, _) in Evidences.Reverse())
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} " +
                              $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static (double X, double Y, double Z) Of(ScenePosition at) => (at.X, at.Y, at.Z);

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>The scene as the codec writes it (as AshenMissionDataV1.Enable); plain JSON with no quote or backslash to escape.</summary>
        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("1281's scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        /// <summary>An NPC's pool from one place to the other, only where it holds the one (and its own creature).</summary>
        private static string PoolSql(uint pool, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE spawnpool SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)} WHERE id = {pool} AND creature_1_Id = {pool} " +
            $"AND pos_x = {F(from.X)} AND pos_y = {F(from.Y)} AND pos_z = {F(from.Z)};";

        /// <summary>1281's scene from one JSON to the other, only where the row holds the one.</summary>
        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {AshenGodOfStormsV1.MissionId} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        /// <summary>A marker from one place to the other, only where it stands within 0.001 of the one (single floats).</summary>
        private static string MarkerSql(uint mission, uint objective, uint marker, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)} WHERE mission_id = {mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {objective} AND indicator_id = {MissionIndicator.UnnamedFrom + marker} " +
            $"AND ABS(pos_x - {F(from.X)}) < 0.001 AND ABS(pos_y - {F(from.Y)}) < 0.001 AND ABS(pos_z - {F(from.Z)}) < 0.001;";

        /// <summary>An Atta pool's count from one value to the other, only where it holds the one (and its creature).</summary>
        private static string CountSql(uint pool, uint creature, uint to, uint from) =>
            $"UPDATE spawnpool SET creature_1_min_count = {to}, creature_1_max_count = {to} WHERE id = {pool} AND creature_1_Id = {creature} " +
            $"AND creature_1_min_count = {from} AND creature_1_max_count = {from};";
    }
}
