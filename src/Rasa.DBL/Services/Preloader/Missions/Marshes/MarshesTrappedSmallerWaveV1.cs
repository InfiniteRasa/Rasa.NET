using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (1052) with a third wave one player can hold (review list, to-fix 29 recheck, tester, real client 2026-11-04:
    /// with one player's weapons wave 3 killed the three Retreads and then Samuel 40 s after its spawn; with GM kills every
    /// 2 s all lived).
    ///
    /// Why: wave 3 (MarshesTrappedV1.Waves, 150 s after the talk) is five Thrax, three soldiers and two grenadiers, from two
    /// places (west 170, -690 and south 196, -700), more than waves 1 (three) and 2 (four).
    ///
    /// Now (ESTIMATE, no source gives the waves): wave 3 three Thrax, as wave 1: the two west soldiers and the south
    /// grenadier; its south soldier (wave-3-4, 198, -702) and second grenadier (wave-3-5, 194, -702) are left out. The spare
    /// (to-fix 29), Samuel's 12000 hit points and the other waves stay. The binding is written whole both ways: up the new
    /// scene, down the scene as MarshesTrappedV3 wrote it.
    /// </summary>
    public static class MarshesTrappedSmallerWaveV1
    {
        public static readonly string[] LeftOut = { "wave-3-4", "wave-3-5" };

        /// <summary>The waves now: MarshesTrappedV1's with wave 3's last two members left out.</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            MarshesTrappedV1.Waves[0],
            MarshesTrappedV1.Waves[1],
            (MarshesTrappedV1.Waves[2].After, MarshesTrappedV1.Waves[2].Members.Take(3).ToArray())
        };

        /// <summary>The binding as the live server has it: MarshesTrappedV3's.</summary>
        public static string Before() => MarshesBindings.Written(MarshesTrappedV3.Up, MarshesTrappedV1.MissionId);

        /// <summary>The scene now: wave 3's last two members, their defeat sequences and those sequences gone.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MarshesBindings.Read(Before());
            foreach (var role in LeftOut)
            {
                scene.Actors.Remove(role);
                scene.Sequences.Remove(scene.DefeatSequences[role]);
                scene.DefeatSequences.Remove(role);
            }
            return scene;
        }

        public static void Up(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesTrappedV1.MissionId, MarshesBindings.Json(Scene()));

        public static void Down(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesTrappedV1.MissionId, Before());
    }
}
