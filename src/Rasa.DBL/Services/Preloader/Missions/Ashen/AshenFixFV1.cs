using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Scenes;
using Rasa.Missions.Content;
using Rasa.Structures;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert fix job F: a delta on migration 20271110001000 (set 10, AshenDesignerHidesNpcsV1 and
    /// AshenDesignerJewelryV1, live); their data classes stay. It sorts after set 13 (20271110001300), which deploys first.
    ///
    /// Item 1 (to-fix row 18, real client 2026-11-10 18:51): Sally came back about 50 s after her death, at the Mauna Barrens
    /// camp where a player farms 1287's carapaces. Set 10's four boss pools had the World DB's boss respawn, 500 tenths (every
    /// upstream boss pool 520001-520073 has it). Now 3000 tenths, 300 s (ESTIMATE: longer than a round of the camp's Atta at
    /// set 2's 120 s, and the World DB's most common respawn, 882 pools).
    ///
    /// Item 2 (to-fix row 22, real client 2026-11-10 18:51-19:19: four deaths farming 1287's carapaces, each pull a group
    /// fight): set 2's Ashen Atta Worker (790044, a copy of the Thunderhead harvester 531099) has CR_ATTA_HARVESTER_WARCRY 477
    /// (creature_action 61058); action_property gives RADIUS_AROUND_SOURCE 80 and CALL_FOR_HELP_NUM_CREATURES 2, and
    /// CreatureBuffs.Warcry calls the two nearest hostile creatures in that reach that are not fighting yet (the cells about the
    /// Worker: 51-77 m), once a minute a Worker. The camp's four Atta pools stand 32-64 m apart, Sally 40.5 m from the Workers
    /// 790049, the Mutated pools 32-72 m off: no spread inside the camp gets a Worker out of it (it needs 100 m), and 1242 wants
    /// the Workers there. So 1287 gets a pool of its own: three Ashen Atta Soldiers (790043: no warcry), radius 0, set 2's
    /// 120 s, on the slope east of the camp, 79.6 m from the nearest hostile pool (790051, 790047), 106.2 m from the nearest
    /// Worker pool (790048), 357 m from the nearest NPC (probe 2026-11-10: a single navmesh floor joined to the Shadow's Edge
    /// Post waypoint within 17 m, the client's floor under it, open sky, 7.5 m from a navmesh edge, walked to on the client floor
    /// both ways and 30 m and more outside every other hostile pool's circle; the place ESTIMATE). Its Soldiers drop 1287's
    /// carapace as any set 2 Soldier (AshenDesignerJewelryV1.Droppers) and count for 1242. A marker of 1287's objective 1 on it.
    ///
    /// Item 3 (to-fix row 23, real client 2026-11-10: a blown Raw Fuel Pump kept its intact model; coordinator: take the
    /// recommended option): Gas Shortages' (1356, set 11, live) three pumps change from UsableInertDestBaneRawFuelPumpHouseV01
    /// 10190 to the V02 pump 21007 (UsableInertDestBaneRawFuelPumpHouseV02, prop_bane_fuel_pump_v02.geo), whose wreck showed in
    /// the real client (1053, 2026-10-27). Both have augmentation 41, target flag 1, no use argument (usabledata.lookup all
    /// None: the pumps are not used, their devices are), the same wreck (alternateMesh (class, 2, USE_STATE_NULL) = 26121) and
    /// the same explosion (specialFX (class, 186, 2) = 24019); 10190 alone loops an animation in its intact state
    /// (usabledata.animation (10190, 110, USE_STATE_NULL) = 53). ESTIMATE: that the animation kept the wreck from showing (not
    /// checked in the client). The scene's row is swapped only where it holds set 11's exact JSON.
    ///
    /// No data change for to-fix rows 20 and 21 (docs/ashen-playtest.md, fix F): set 10's and set 12's creature classes keep
    /// their IMMUNE_* flags (every client class of each kind has the same ones); White Oasis's panel 4 is outside the Xanx pool
    /// 540076's notice reach (15 + 18 m; a pulled Xanx follows 60 m and webs from 40 m).
    ///
    /// Plain SQL; each UPDATE only where the row holds its exact stored values (a second run changes nothing); Down puts set 10's
    /// respawns back only where the rows hold fix F's, and takes the new pool and marker out.
    /// </summary>
    public static class AshenFixFV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;

        /// <summary>Set 10's boss pools and their bosses; the respawn as set 10 wrote it and fix F's (tenths of a second).</summary>
        public static readonly (uint Pool, uint Boss, uint Mission)[] BossPools =
        {
            (AshenDesignerHidesNpcsV1.SallyPool, AshenDesignerHidesNpcsV1.Sally, AshenDesignerJewelryV1.MissionId),
            (AshenDesignerHidesNpcsV1.MollyPool, AshenDesignerHidesNpcsV1.Molly, AshenWouldntWearItV1.MissionId),
            (AshenDesignerHidesNpcsV1.TrudyPool, AshenDesignerHidesNpcsV1.Trudy, AshenNewFadV1.MissionId),
            (AshenDesignerHidesNpcsV1.WendyPool, AshenDesignerHidesNpcsV1.Wendy, AshenLatestRageV1.MissionId)
        };

        public const uint BossRespawnBefore = AshenDesignerHidesNpcsV1.BossRespawnTenths;   // 500, 50 s
        public const uint BossRespawnTenths = 3000;                                        // 300 s, ESTIMATE

        /// <summary>1287's own Ashen Atta Soldier pool (set 10's next free id): the creature, count, respawn, radius as set 2's pools.</summary>
        public const uint AttaPool = AshenDesignerHidesNpcsV1.NextFreeId;                  // 790378
        public const uint AttaSoldier = AshenMaunaBarrensNpcsV1.AttaSoldier;
        public const uint AttaCount = AshenMaunaBarrensNpcsV1.PoolCreatures, AttaRespawnTenths = AshenMaunaBarrensNpcsV1.RespawnTenths,
            AttaRadius = AshenMaunaBarrensNpcsV1.PoolRadius;
        public static readonly (double X, double Y, double Z) AttaAt = (-355, 328.97, -210);  // the navmesh ground (probe 2026-11-10), ESTIMATE

        /// <summary>Its marker: 1287's objective 1, the next unnamed indicator after set 10's five (1-4 the camp's pools, 5 Sally), set 10's radius.</summary>
        public const uint MarkerObjective = AshenDesignerJewelryV1.Shiny, MarkerNumber = 6;
        public const uint MarkerIndicator = MissionIndicator.UnnamedFrom + MarkerNumber;
        public const double MarkerRadius = AshenDesignerJewelryV1.MarkerRadius;

        public const uint NextFreeId = AttaPool + 1;

        /// <summary>1356's pump class as set 11 wrote it and fix F's.</summary>
        public const uint PumpClassBefore = AshenGasShortagesV1.PumpClass;     // 10190, the V01 pump
        public const uint PumpClass = 21007;                                    // UsableInertDestBaneRawFuelPumpHouseV02

        /// <summary>1356's scene as set 11 wrote it into mission_scene_binding.bindings, and fix F's.</summary>
        public static string SceneBefore() => Json(AshenGasShortagesV1.Scene());
        public static string SceneNow() => Json(AshenGasShortagesV1.Scene(PumpClass));

        public const string Source = "repository:AshenFixFV1";

        public const string RespawnNote =
            "Fix job F (to-fix row 18): the boss pool respawned in 500 tenths (50 s, the World DB's boss value); Sally came back " +
            "about 50 s after her death by the Atta camp a player farms. Now 3000 tenths (300 s). ESTIMATE: the time.";

        public const string AttaNote =
            "Fix job F (to-fix row 22): the camp's Ashen Atta Workers call for help (CR_ATTA_HARVESTER_WARCRY 477: 2 creatures " +
            "within 80 m), so every pull at the camp became a group fight. A pool of three Ashen Atta Soldiers (no warcry) for the " +
            "carapaces at (-355, 328.97, -210), 79.6 m from the nearest hostile pool, 106 m from the Workers; marker 1/6 on it. " +
            "Also the boss pool's respawn 500 -> 3000 tenths (row 18). ESTIMATE: the place and the time.";

        public const string PumpNote =
            "Fix job F (to-fix row 23): a blown Raw Fuel Pump 10190 kept its intact model in the real client (the explosion " +
            "showed). The pumps are the V02 pump 21007 now: same augmentation, wreck mesh 26121 and explosion 24019 in usabledata, " +
            "no looping intact animation; its wreck showed in 1053. ESTIMATE: the cause (not checked in the client).";

        /// <summary>The evidence rows fix F writes: mission, evidence id (the next after sets 10 and fix E), note.</summary>
        public static readonly (uint Mission, uint Evidence, string Note)[] Evidences =
        {
            (AshenDesignerJewelryV1.MissionId, 5, AttaNote),
            (AshenWouldntWearItV1.MissionId, 5, RespawnNote),
            (AshenNewFadV1.MissionId, 5, RespawnNote),
            (AshenLatestRageV1.MissionId, 4, RespawnNote),
            (AshenGasShortagesV1.MissionId, 4, PumpNote)
        };

        public static IEnumerable<string> UpSql()
        {
            foreach (var (pool, boss, _) in BossPools)
                yield return RespawnSql(pool, boss, BossRespawnTenths, BossRespawnBefore);
            // Guarded as set 10's rows (CrucibleSql.Guard): only where no row has the id, so a second Up adds nothing.
            yield return SceneSql(SceneNow(), SceneBefore());
            yield return Crucible.CrucibleSql.Guard(Pool(AttaPool, AttaAt.X, AttaAt.Y, AttaAt.Z, MapContextId, AttaSoldier, AttaCount, AttaCount, AttaRespawnTenths, AttaRadius));
        }

        public static IEnumerable<string> DownSql()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {AttaPool} AND creature_1_Id = {AttaSoldier};";
            yield return SceneSql(SceneBefore(), SceneNow());
            foreach (var (pool, boss, _) in BossPools.Reverse())
                yield return RespawnSql(pool, boss, BossRespawnBefore, BossRespawnTenths);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, AshenDesignerJewelryV1.MissionId)
                .Indicator(MarkerObjective, MarkerNumber, AttaAt.X, AttaAt.Y, AttaAt.Z, MarkerRadius);
            foreach (var (mission, evidence, note) in Evidences)
                AshenMissionDataV1.Existing(migration, mission).Evidence(evidence, MissionEvidenceSourceKind.Reconstruction, Source, note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var (mission, evidence, _) in Evidences.Reverse())
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} " +
                              $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {evidence};");
            migration.Sql($"DELETE FROM mission_indicator WHERE mission_id = {AshenDesignerJewelryV1.MissionId} " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {MarkerObjective} AND indicator_id = {MarkerIndicator};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        /// <summary>The scene as the codec writes it (as AshenMissionDataV1.Enable); plain JSON with no quote or backslash to escape.</summary>
        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("1356's scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        /// <summary>1356's scene from one JSON to the other, only where the row holds the one.</summary>
        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {AshenGasShortagesV1.MissionId} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        /// <summary>A boss pool's respawn from one value to the other, only where it holds the one (and its boss).</summary>
        private static string RespawnSql(uint pool, uint boss, uint to, uint from) =>
            $"UPDATE spawnpool SET respown_time = {to} WHERE id = {pool} AND creature_1_Id = {boss} AND respown_time = {from};";
    }
}
