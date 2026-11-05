using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Head East, Young Man (920): the waves come in from far enough out that one player meets them before they reach the
    /// station (tester, real client 2026-11-05, server-marshes7, a level 30 player with ability kills: wave 1's three Thrax
    /// were 10-18 m from the station when first seen, a rifleman died 11 s after wave 1, the other 78 s later, Shaw 30 s
    /// after him).
    ///
    /// Why: the waves stood 36-43 m from Shaw (MarshesGuardStationHoldsV1, MarshesHeadEastV2); a Marshes Thrax runs at 9
    /// (creature 531045, 531046 run_speed), so it was at the station 4-5 s after it came, before the player had turned to it.
    /// The station's men had their hit points in play (Shaw's stand-in 12000, the soldiers 8000:
    /// MarshesEastAndNorthTests.HeadEastHoldsTheStationAndMeekoComes reads them off the spawned actors; the server takes
    /// them from creature_stat, CreatureManager.EnsureScenarioAttributes). The rifleman who died first was the west one
    /// (ward-2, 86, 539, defeat sequence 102), on the side of the tester's walk-in from (40, 220, 535): that place is 4.6 m
    /// outside the circle of upstream Mox pool 540043 (22.9, 525.5, radius 15), and a Mox of it followed the player to the
    /// station. The station itself, its men and its waves stand 49 m and more outside that circle.
    ///
    /// Now (ESTIMATE, navmesh probe and client files 2026-11-05; no source gives where the attack comes from past Ten Ton
    /// Hammer's "Watch for the dropships"): the waves 54-62 m out on the open ground east of the station (terrain 216.86,
    /// on the navmesh, each with a whole navmesh path to Shaw through the station's north or south gap): wave 1 north-east
    /// (132.5, 570.4), (135.5, 572.4), (130.5, 573.4); wave 2's soldiers south-east (137.9, 495.1), (139.9, 498.1), its
    /// grenadiers south (121.4, 481.3), (124.4, 483.3), its Stalker east (148.7, 556.2). Their sizes, times, the hold,
    /// the spare and the hit points stay. The binding is written whole both ways.
    /// </summary>
    public static class MarshesGuardStationWavesOutV1
    {
        public const uint MoxPool = 540043;

        /// <summary>The waves' places now, member by member (their creatures and times as MarshesGuardStationHoldsV1's).</summary>
        public static readonly ScenePosition[][] WavesAt =
        {
            new ScenePosition[] { new(132.5f, 217.25f, 570.4f), new(135.5f, 217.21f, 572.4f), new(130.5f, 217.23f, 573.4f) },
            new ScenePosition[]
            {
                new(137.9f, 216.95f, 495.1f), new(139.9f, 217.05f, 498.1f), new(121.4f, 217.08f, 481.3f), new(124.4f, 217.03f, 483.3f),
                new(148.7f, 217.0f, 556.2f)
            }
        };

        /// <summary>The binding as the live server has it: MarshesGuardStationHoldsV1's.</summary>
        public static string Before() => MarshesBindings.Json(MarshesGuardStationHoldsV1.Scene());

        /// <summary>The scene now: the waves' members at their new places, nothing else changed.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MarshesBindings.Read(Before());
            for (var wave = 1; wave <= WavesAt.Length; wave++)
                for (var member = 1; member <= WavesAt[wave - 1].Length; member++)
                {
                    var role = MarshesDefenceScene.WaveRole(wave, member);
                    if (!scene.Actors.TryGetValue(role, out var actor))
                        throw new KeyNotFoundException($"920 has no actor {role}.");
                    scene.Actors[role] = actor with { Position = WavesAt[wave - 1][member - 1] };
                }
            return scene;
        }

        public static void Up(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesHeadEastV2.MissionId, MarshesBindings.Json(Scene()));

        public static void Down(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesHeadEastV2.MissionId, Before());
    }
}
