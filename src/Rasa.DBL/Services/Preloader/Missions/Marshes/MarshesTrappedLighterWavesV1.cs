using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (1052) with first and second waves one player can hold (tester, real client 2026-11-05, server-marshes6, a
    /// level 30 player killing one Thrax per 10-15 s with abilities: Samuel, with his 12000 hit points, died 98 s after wave
    /// 1, still in wave 2; wave 3 never came).
    ///
    /// Why: wave 1 (three Thrax at 5 s) and wave 2 (four at 75 s) were seven Thrax in the first 75 s; at one kill per 12 s
    /// the second wave came with wave 1 still up, and with the spare (to-fix 29) every Retread down sends them all on Samuel.
    ///
    /// Now (ESTIMATE, no source gives the waves): wave 1 two soldiers (its grenadier wave-1-3 left out), wave 2 three Thrax
    /// (its second grenadier wave-2-4 left out) and 100 s after the talk, not 75 s; wave 3 (three at 150 s,
    /// MarshesTrappedSmallerWaveV1), the hold (240 s), the spare and Samuel's hit points stay. The binding is written whole
    /// both ways: up the new scene, down the scene as MarshesTrappedSmallerWaveV1 wrote it.
    /// </summary>
    public static class MarshesTrappedLighterWavesV1
    {
        public static readonly string[] LeftOut = { MarshesDefenceScene.WaveRole(1, 3), MarshesDefenceScene.WaveRole(2, 4) };
        public const uint SecondWaveAfter = 100000;
        public static readonly uint SecondWaveBefore = MarshesTrappedV1.Waves[1].After;

        /// <summary>The waves now: MarshesTrappedSmallerWaveV1's with wave 1's grenadier and wave 2's second grenadier left out,
        /// wave 2 later.</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (MarshesTrappedSmallerWaveV1.Waves[0].After, MarshesTrappedSmallerWaveV1.Waves[0].Members.Take(2).ToArray()),
            (SecondWaveAfter, MarshesTrappedSmallerWaveV1.Waves[1].Members.Take(3).ToArray()),
            MarshesTrappedSmallerWaveV1.Waves[2]
        };

        /// <summary>The binding as the live server has it: MarshesTrappedSmallerWaveV1's.</summary>
        public static string Before() => MarshesBindings.Json(MarshesTrappedSmallerWaveV1.Scene());

        /// <summary>The scene now: the two members, their defeat sequences and those sequences gone; wave 2 later.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MarshesBindings.Read(Before());
            foreach (var role in LeftOut)
            {
                scene.Actors.Remove(role);
                scene.Sequences.Remove(scene.DefeatSequences[role]);
                scene.DefeatSequences.Remove(role);
            }
            scene.Names[MarshesDefenceScene.After(2)] = SecondWaveAfter;
            return scene;
        }

        public static void Up(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesTrappedV1.MissionId, MarshesBindings.Json(Scene()));

        public static void Down(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesTrappedV1.MissionId, Before());
    }
}
