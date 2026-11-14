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
    /// Ligo Ashen Desert fix job E: deltas on migration 20271110000900 (set 9, AshenOnTheSameFrequencyV1, live) and
    /// 20271110000200 (set 3, AshenSheitanHighwayNpcsV1, live); their data classes stay.
    ///
    /// Item 1 (to-fix row 12, real client 2026-11-10): On The Same Frequency's (1277) consoles stood 2.7-4 m from their towers'
    /// upper teleporters, so the walk from the lift's exit to a console (2696 at tower 2) crossed the pad, whose 2 m trigger
    /// (3D) sent the player down again (the "transfer pending" refusal right after). Now each console stands 5-6 m off its
    /// upper pad on the side the lift opens to (real client: tower 1 east, 2 east, 3 north-west, 4 west), so the straight walk
    /// on the client floor from the exit side (ExitAt, 2.8 m off the pad) to it keeps 2.5 m or more from the pad's centre; on
    /// the top floor's client floor, seen from every ring place on that floor, the navmesh within 1.5 m (probe 2026-11-10,
    /// client files and navmesh; the places ESTIMATE).
    ///
    /// Item 2 (row 13): a console was used from the ground floor 13.6 m under it (the server's use check is a 3D distance
    /// within DynamicObjectManager.MaxUseDistance, 20 m). Each console's use now keeps the user within MaxUseHeight of its floor
    /// (SceneObjectAction.MaxUseHeight, an opt-in field: every other scene has none). The floors are 13.4 m apart (the
    /// towers' teleporters); 3 m ESTIMATE.
    ///
    /// Item 3 (row 14): the ambushes (three Thrax Soldiers at towers 2-4) came at the tower's foot and never reached a player on
    /// the top floor (no stairs, the lift only). Now they come on the top floor by the lift's exit, 3.2-4 m off the pad (out of
    /// its 2 m trigger), on the navmesh and the client floor, each walked to the exit side and the console on the client
    /// floor (probe 2026-11-10; ESTIMATE).
    ///
    /// Item 4 (the encounter tuning, user 2026-11-10 "if the player cant win, try tune the encounter a bit"): a solo player died
    /// twice at the Sheitan Highway's Ravager pools 790085 (-180, -305) and 790086 (-140, -305), 23.7 and 26.6 m from the camp's
    /// upstream pool 531034 (-161.8, 304.8, -320.2; radius 30: Thrax Soldiers, a Linker, a Technician, a Caretaker, World DB), in
    /// its strolls (the pool's radius, BehaviorManager.WanderZoneOf) and its members' aggro (Creature.AggroRange 18 m). Now
    /// each pool stands on the camp's edge, 53-55 m from 531034's point (its radius and the aggro reach, with 5 m to spare),
    /// 25 m or more from the mortars, the Soldier pools and the Predator beats, 35 m or more from Exing, 5 m or more from a
    /// navmesh edge, walked on the client floor from the Shadow's Edge Post waypoint both ways; two Ravagers each, not three
    /// (ESTIMATE: three Ravagers at once with their grenades are a hard pull for one player at level 40-41). The markers of
    /// 1247 (Highway to Hell), 1266 and 1269 (the launchers) on them move with them. The Ravager (531009) stays as it is.
    ///
    /// Item 5 (to-fix rows 17 and 18, user 2026-11-10: tune the cannot-win cases; set 10, migration 20271110001000, live): three
    /// of set 10's bosses stood in or by other hostile pools. Sally (1287, pool 790361) at TaRapedia's (-439, -258) was 20.6 m
    /// from the Atta Soldier pool 790047 and came with the Atta when a player pulled them (row 18); Molly (1289, pool 790366)
    /// was reached only past fix A's Fithik pool 540067 (the walk from the Ashoka Settlement waypoint 22.3 m from it; row 17);
    /// Trudy (1291, pool 790372) stood 15.8 m from the Beam Manta pool 790370. Now each boss is 38 m and more from every other
    /// hostile pool (less its radius), and the last 100 m of the walk from its waypoint (set 10's routes, AshenDesignerHidesTests
    /// .Places) keep 30 m and more from every hostile pool not of its mission: Sally 40 m south of TaRapedia's spot, Molly 47 m
    /// north-east of TaRapedia's notch (275, -659), Trudy 54 m west of set 10's ridge end (probe 2026-11-10, ESTIMATE). Their
    /// markers move with them. The eastern Boneyard's Beam Manta pool 790368 (1291) moves 78 m south-east onto the rise
    /// (770, -400): every walk to set 10's place from the Ashoka Settlement waypoint passed 2.2 m from the Nitroglazer pool
    /// 790244 (ESTIMATE, still the Boneyard's east side). Wendy (1293) and the other normal pools keep their places; the Coheri
    /// Pools walks from the White Oasis Post waypoint go by the west ridge (-300, 620), off the Fithik pool 540077 (test routes).
    ///
    /// Plain SQL; each UPDATE only where the row holds its exact stored values (a second run changes nothing); Down puts set 9's
    /// scene, set 3's pools and markers and set 10's boss pools and markers back only where the rows hold fix E's values.
    /// </summary>
    public static class AshenFixEV1
    {
        public const uint MissionId = AshenOnTheSameFrequencyV1.MissionId;

        /// <summary>The most metres a console's user stands over or under its floor (ESTIMATE; the floors are 13.4 m apart).</summary>
        public const float MaxUseHeight = 3;

        /// <summary>A local teleporter takes a player within 2 m of it (3D); the walk to a console keeps WalkClearance of its centre.</summary>
        public const float PadTrigger = 2, WalkClearance = 2.5f;

        /// <summary>
        /// The towers as set 9's (objective, ground and upper teleporter, marker at the ground pad, facing) with fix E's consoles
        /// and ambush spots on the top floor (probe 2026-11-10; ESTIMATE).
        /// </summary>
        public static readonly (uint Objective, uint Ground, uint Upper, ScenePosition PadAt, ScenePosition ConsoleAt, double Facing,
            ScenePosition[] AmbushAt)[] Towers =
        {
            (1, 539, 823, new(-62.80f, 353.43f, -400.71f), new(-60.03f, 366.93f, -405.23f), 3.1416, new ScenePosition[0]),
            (2, 538, 824, new(-3.80f, 339.87f, -533.49f), new(-0.97f, 353.37f, -529.11f), 3.1416,
                new ScenePosition[] { new(-2.22f, 353.45f, -530.61f), new(-0.72f, 353.45f, -531.61f), new(-0.72f, 353.45f, -530.61f) }),
            (3, 537, 825, new(59.41f, 350.58f, -474.90f), new(54.67f, 364.08f, -472.73f), 3.1416,
                new ScenePosition[] { new(56.92f, 364.11f, -472.48f), new(55.92f, 364.23f, -473.98f), new(57.42f, 364.09f, -477.98f) }),
            (4, 540, 826, new(146.75f, 337.25f, -602.82f), new(141.56f, 350.75f, -603.53f), 3.1416,
                new ScenePosition[] { new(143.31f, 350.85f, -601.78f), new(143.81f, 350.85f, -604.28f), new(142.81f, 350.85f, -603.28f) })
        };

        /// <summary>
        /// The lift's exit side on each top floor (real client, to-fix row 14: tower 1 east, 2 east, 3 north-west, 4 west): the
        /// first place 2.8 m off the upper pad in that direction on the navmesh and the client floor, walked to from the pad
        /// (tower 2 at 75 degrees from north: due east is walled at chest height). The walk to the console starts here.
        /// </summary>
        public static readonly ScenePosition[] ExitAt =
        {
            new(-59.98f, 366.89f, -400.73f), new(-1.01f, 353.32f, -532.88f), new(57.44f, 364.05f, -473.00f), new(144.01f, 350.73f, -602.78f)
        };

        /// <summary>The Ravager (Thrax Grenadier - Ashen Desert, World DB) and its two pools: set 3's place and count, fix E's.</summary>
        public const uint Ravager = AshenSheitanHighwayNpcsV1.Ravager;
        public const uint RavagersBefore = AshenSheitanHighwayNpcsV1.PoolCreatures, Ravagers = 2;

        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] RavagerPools =
        {
            (790085, (-180, 306.57, -305), (-213, 307.87, -302)),
            (790086, (-140, 306.34, -305), (-125, 314.27, -359))
        };

        /// <summary>The upstream camp pool the Ravagers pulled with (World DB): point and radius.</summary>
        public static readonly (double X, double Y, double Z) CampPool = (-161.8000030517578, 304.79998779296875, -320.20001220703125);
        public const double CampRadius = 30;

        /// <summary>The markers on the Ravager pools (each at its pool's point): mission, objective, indicator, pool.</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator, uint Pool)[] Markers =
        {
            (AshenHighwayToHellV1.MissionId, AshenHighwayToHellV1.Ravagers, 1000000001, 790085),
            (AshenHighwayToHellV1.MissionId, AshenHighwayToHellV1.Ravagers, 1000000002, 790086),
            (AshenWeaponSalvageV1.MissionId, AshenWeaponSalvageV1.Launchers, 1000000004, 790085),
            (AshenWeaponSalvageV1.MissionId, AshenWeaponSalvageV1.Launchers, 1000000005, 790086),
            (AshenWeaponAcquisitionsV1.MissionId, AshenWeaponAcquisitionsV1.Launchers, 1000000004, 790085),
            (AshenWeaponAcquisitionsV1.MissionId, AshenWeaponAcquisitionsV1.Launchers, 1000000005, 790086)
        };

        /// <summary>
        /// Set 10's pools that move (item 5; the three bosses and the eastern Boneyard's Beam Manta pool): pool, creature, mission, the objective and indicator of the marker on the pool, set
        /// 10's place (the stored doubles) and fix E's (the navmesh ground; probe 2026-11-10, ESTIMATE).
        /// </summary>
        public static readonly (uint Pool, uint Creature, uint Mission, uint Objective, uint Indicator, (double X, double Y, double Z) Before,
            (double X, double Y, double Z) Now)[] Set10Moves =
        {
            (AshenDesignerHidesNpcsV1.SallyPool, AshenDesignerHidesNpcsV1.Sally, AshenDesignerJewelryV1.MissionId, 2, 1000000005,
                AshenDesignerHidesNpcsV1.SallyAt, (-439, 298.47, -298)),
            (AshenDesignerHidesNpcsV1.MollyPool, AshenDesignerHidesNpcsV1.Molly, AshenWouldntWearItV1.MissionId, 2, 1000000003,
                AshenDesignerHidesNpcsV1.MollyAt, (315, 283.02, -622)),
            (AshenDesignerHidesNpcsV1.TrudyPool, AshenDesignerHidesNpcsV1.Trudy, AshenNewFadV1.MissionId, 2, 1000000004,
                AshenDesignerHidesNpcsV1.TrudyAt, (-250, 302.74, 632)),
            (AshenDesignerHidesNpcsV1.MantaPools[0].Pool, AshenDesignerHidesNpcsV1.Manta, AshenNewFadV1.MissionId, 1, 1000000001,
                AshenDesignerHidesNpcsV1.MantaPools[0].At, (770, 327.21, -400))
        };

        /// <summary>The least a set 10 boss keeps from every other hostile pool, and the last stretch of a walk from every hostile pool not of its mission.</summary>
        public const double BossClearance = 35, WalkClearancePools = 30, ApproachReach = 100;

        /// <summary>The evidence row fix E writes on each mission it changes (sets 3, 6, 8, 9 and 10 wrote 1-3).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:AshenFixEV1";

        public const string ConsoleNote =
            "Fix job E (to-fix rows 12-14): the consoles 5-6 m off the upper pads on the lift's exit side (towers 1, 2 east, 3 north-" +
            "west, 4 west), the walk from the exit keeps 2.5 m off the pad (its 2 m trigger sent the player down); a console's use " +
            "only within 3 m of its floor (it was used from the ground 13.6 m under it); the ambushes on the top floor by the exit " +
            "(they never reached the top from the foot). ESTIMATE: the places and the 3 m.";

        public const string BossNote =
            "Fix job E (to-fix rows 17, 18): the boss stood in or by another hostile pool (Sally 20.6 m from Atta pool 790047, " +
            "Molly reached past the Fithik pool 540067, Trudy 15.8 m from Manta pool 790370; the Boneyard Manta pool 790368's walk " +
            "2.2 m from Nitroglazer pool 790244). Now each boss 38 m and more from every other hostile pool, the last 100 m of each " +
            "walk 30 m and more from those not of the mission; the markers on them. ESTIMATE: the places.";

        public const string PoolNote =
            "Fix job E (encounter tuning, user 2026-11-10): the Sheitan Highway's Ravager pools 790085, 790086 were 24-27 m from the " +
            "camp's pool 531034 (radius 30) and pulled with its Linker and Caretaker; a solo player died twice. Now on the camp's " +
            "edge (-213, 307.87, -302) and (-125, 314.27, -359), 53-55 m from 531034, two Ravagers each; the markers on them. " +
            "ESTIMATE: the places and the count.";

        /// <summary>The missions whose evidence fix E writes, with their notes.</summary>
        public static readonly (uint Mission, string Note)[] Evidences =
        {
            (MissionId, ConsoleNote), (AshenHighwayToHellV1.MissionId, PoolNote),
            (AshenWeaponSalvageV1.MissionId, PoolNote), (AshenWeaponAcquisitionsV1.MissionId, PoolNote),
            (AshenDesignerJewelryV1.MissionId, BossNote), (AshenWouldntWearItV1.MissionId, BossNote), (AshenNewFadV1.MissionId, BossNote)
        };

        /// <summary>1277's scene as set 9 wrote it into mission_scene_binding.bindings, and fix E's.</summary>
        public static string SceneBefore() => Json(AshenOnTheSameFrequencyV1.Scene());
        public static string SceneNow() => Json(AshenOnTheSameFrequencyV1.Scene(Towers, MaxUseHeight));

        public static IEnumerable<string> UpSql()
        {
            yield return SceneSql(SceneNow(), SceneBefore());
            foreach (var (pool, before, now) in RavagerPools)
                yield return PoolSql(pool, now, Ravagers, before, RavagersBefore);
            foreach (var marker in Markers)
            {
                var (_, before, now) = RavagerPools.Single(entry => entry.Pool == marker.Pool);
                yield return MarkerSql(marker, now, before);
            }
            foreach (var boss in Set10Moves)
            {
                yield return BossSql(boss.Pool, boss.Creature, boss.Now, boss.Before);
                yield return MarkerSql((boss.Mission, boss.Objective, boss.Indicator, boss.Pool), boss.Now, boss.Before);
            }
        }

        public static IEnumerable<string> DownSql()
        {
            foreach (var boss in Set10Moves.Reverse())
            {
                yield return MarkerSql((boss.Mission, boss.Objective, boss.Indicator, boss.Pool), boss.Before, boss.Now);
                yield return BossSql(boss.Pool, boss.Creature, boss.Before, boss.Now);
            }
            foreach (var marker in Markers.Reverse())
            {
                var (_, before, now) = RavagerPools.Single(entry => entry.Pool == marker.Pool);
                yield return MarkerSql(marker, before, now);
            }
            foreach (var (pool, before, now) in RavagerPools.Reverse())
                yield return PoolSql(pool, before, RavagersBefore, now, Ravagers);
            yield return SceneSql(SceneBefore(), SceneNow());
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            foreach (var (mission, note) in Evidences)
                AshenMissionDataV1.Existing(migration, mission).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var (mission, _) in Evidences.Reverse())
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} " +
                              $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        /// <summary>The scene as the codec writes it (as AshenMissionDataV1.Enable); plain JSON with no quote or backslash to escape.</summary>
        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("1277's scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        /// <summary>1277's scene from one JSON to the other, only where the row holds the one.</summary>
        private static string SceneSql(string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {MissionId} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        /// <summary>A Ravager pool from one place and count to the other, only where it is the one (the doubles as stored).</summary>
        private static string PoolSql(uint pool, (double X, double Y, double Z) to, uint toCount, (double X, double Y, double Z) from, uint fromCount) =>
            $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, " +
            $"creature_1_min_count = {toCount}, creature_1_max_count = {toCount} " +
            $"WHERE id = {pool} AND creature_1_Id = {Ravager} AND pos_x = {N(from.X)} AND pos_y = {N(from.Y)} AND pos_z = {N(from.Z)} " +
            $"AND creature_1_min_count = {fromCount} AND creature_1_max_count = {fromCount};";

        /// <summary>A set 10 boss pool from one place to the other, only where it is the one (the doubles as stored).</summary>
        private static string BossSql(uint pool, uint creature, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE id = {pool} AND creature_1_Id = {creature} AND pos_x = {N(from.X)} AND pos_y = {N(from.Y)} AND pos_z = {N(from.Z)};";

        /// <summary>A marker on a moved pool from one place to the other, only where it is the one.</summary>
        private static string MarkerSql((uint Mission, uint Objective, uint Indicator, uint Pool) marker, (double X, double Y, double Z) to,
            (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE mission_id = {marker.Mission} AND content_revision = '{AshenMissionDataV1.Revision}' AND objective_id = {marker.Objective} " +
            $"AND indicator_id = {marker.Indicator} AND pos_x = {N(from.X)} AND pos_y = {N(from.Y)} AND pos_z = {N(from.Z)};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
