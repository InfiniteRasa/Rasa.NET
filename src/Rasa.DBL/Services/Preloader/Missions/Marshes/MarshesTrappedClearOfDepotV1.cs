using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (1052) fought clear of the Bane depot's Thrax pool (tester, real client 2026-11-05, server-marshes7, a level
    /// 30 player with ability kills, one per 12-25 s: failed twice, Samuel dead 05:52:57 and 05:59:59; the first Retread
    /// died 31 s after wave 2).
    ///
    /// Why: the depot's wild Thrax (upstream pool 531291: 2-3 soldiers, 2 grenadiers and a Technician, radius 18 round
    /// (225.1, -652.7), respawn 1200 = 120 s) joined the defence each try, and came back while it ran: entity 2580 was
    /// killed three times and 2584 twice in 2 min (the pool's Technician revives the dead within 20 m, every 30 s:
    /// creature_action 68077; what one survivor lost tops up 120 s after the first loss: SpawnPoolManager.TopUp). The crew
    /// (MarshesTrappedV3) stood 24-29 m outside that circle, wave 2 came in 19 m outside it ((225, -690), its soldiers and
    /// grenadier), so the player meeting wave 2 stood in the pool's 18 m aggro reach. No scene intent can hold that pool on
    /// the public map: QuietSpawnPoolsIntent works on the owner's private map only (SceneWorldAdapter), HideSpawnPoolsIntent
    /// only hides it from the owner's client; a longer respawn would change the public pool for every player and still
    /// leave it in the fight.
    ///
    /// Now (ESTIMATE, navmesh probe and client files 2026-11-05; no source gives the crew's place past TaRapedia's "Bane
    /// Refueling Station"): the crew 25 m west, on the open ground by the depot's lights (terrain 218.36, navmesh within 0.2
    /// m, seen from 16 of 16 places round): Samuel's stand-in at (164, 218.55, -667), the Retreads at (158, -663), (166,
    /// -676), (159, -672); the waves from the free ground west, north-west and south-west of it, 37-41 m out, each with a
    /// whole navmesh path in: wave 1 (2 soldiers) (126, -667), (124, -665); wave 2 (2 soldiers, a grenadier) (154, -630),
    /// (156, -632), (152, -632); wave 3 (2 soldiers, a grenadier) (131, -686), (133, -688), (129, -688). Every ward and
    /// wave place stands at least FightClearance (35 m) outside every hostile automatic pool's circle on the map
    /// (MarshesSafeGroundTests); the depot pool is 44 m off now. The approach area (40 m) and the two markers move with
    /// the stand-in. The waves' sizes and times, the hold, the spare and the hit points stay (MarshesTrappedLighterWavesV1).
    /// The binding is written whole both ways; the area and the markers set back on the way down.
    /// </summary>
    public static class MarshesTrappedClearOfDepotV1
    {
        public const uint DepotPool = 531291;

        public static readonly ScenePosition SamuelAt = new(164f, 218.55f, -667f);

        public static readonly ScenePosition[] RetreadsAt =
        {
            new(158f, 218.48f, -663f), new(166f, 218.58f, -676f), new(159f, 218.51f, -672f)
        };

        /// <summary>The waves' places now, member by member (their creatures and times as MarshesTrappedLighterWavesV1's).</summary>
        public static readonly ScenePosition[][] WavesAt =
        {
            new ScenePosition[] { new(126f, 221.44f, -667f), new(124f, 221.41f, -665f) },
            new ScenePosition[] { new(154f, 219.47f, -630f), new(156f, 219.05f, -632f), new(152f, 219.11f, -632f) },
            new ScenePosition[] { new(131f, 220.34f, -686f), new(133f, 220.33f, -688f), new(129f, 220.29f, -688f) }
        };

        /// <summary>The binding as the live server has it: MarshesTrappedLighterWavesV1's.</summary>
        public static string Before() => MarshesBindings.Json(MarshesTrappedLighterWavesV1.Scene());

        /// <summary>The scene now: the wards and the waves' members at their new places, nothing else changed.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MarshesBindings.Read(Before());
            Move(scene, MarshesDefenceScene.Leader, SamuelAt);
            for (var member = 0; member < RetreadsAt.Length; member++)
                Move(scene, MarshesDefenceScene.Ward(member + 2), RetreadsAt[member]);
            for (var wave = 1; wave <= WavesAt.Length; wave++)
                for (var member = 1; member <= WavesAt[wave - 1].Length; member++)
                    Move(scene, MarshesDefenceScene.WaveRole(wave, member), WavesAt[wave - 1][member - 1]);
            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            MarshesBindings.Write(migration, MarshesTrappedV1.MissionId, MarshesBindings.Json(Scene()));
            Place(migration, SamuelAt);
        }

        public static void Down(MigrationBuilder migration)
        {
            MarshesBindings.Write(migration, MarshesTrappedV1.MissionId, Before());
            Place(migration, MarshesTrappedV3.SamuelAt);
        }

        private static void Move(MissionSceneDefinition scene, string role, ScenePosition to)
        {
            if (!scene.Actors.TryGetValue(role, out var actor))
                throw new KeyNotFoundException($"1052 has no actor {role}.");
            scene.Actors[role] = actor with { Position = to };
        }

        /// <summary>The approach area (1) and the markers of 1 and 2 on <paramref name="at"/>, as MarshesTrappedV1.Write puts them.</summary>
        private static void Place(MigrationBuilder migration, ScenePosition at)
        {
            string Xyz() => string.Join(", ", new[] { ("pos_x", at.X), ("pos_y", at.Y), ("pos_z", at.Z) }
                .Select(entry => $"{entry.Item1} = {MarshesSql.Literal(migration, (double)entry.Item2)}"));
            var where = $"WHERE mission_id = {MarshesTrappedV1.MissionId} AND content_revision = {MarshesSql.Literal(migration, MarshesMissionDataV1.Revision)}";
            migration.Sql($"UPDATE mission_area SET {Xyz()} {where} AND area_id = {MarshesTrappedV2.ApproachArea.ToString(CultureInfo.InvariantCulture)};");
            migration.Sql($"UPDATE mission_indicator SET {Xyz()} {where} AND objective_id IN ({MarshesTrappedV1.Locate}, {MarshesTrappedV1.Defend});");
        }
    }
}
