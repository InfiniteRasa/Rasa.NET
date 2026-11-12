using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// A Taste of Their Own Medicine's hack (546) one player can hold (tester, real client 2026-11-05, server-marshes6, a level
    /// 30 player killing one Thrax per 10-15 s with abilities: Hax, with MarshesHaxHoldsV1's 12000 hit points, died 51 s and
    /// 42 s after wave 1 in two runs; at the north spot two of wave 1's four Thrax reached him).
    ///
    /// Why: each wave member had one fixed target in the data.sequence scene, every other one Hax (the rest the medic), so
    /// two Thrax per wave shot Hax from the moment they came, whatever the player did; 12 Thrax in 120 s at one kill per 12 s
    /// leaves 2-4 on Hax all the hack.
    ///
    /// Now (ESTIMATE, no source gives the waves): every wave member attacks the player, the mission's owner
    /// (AttackActorIntent with no target role, SceneWorldAdapter), so the player draws the waves and Hax is shot only when a
    /// Thrax turns to him; and each wave three Thrax, not four (the fourth member of each, wave-K-4, left out with its
    /// intents): 9 Thrax in the 120 s hack, about one per 13 s. Hax's hit points stay MarshesHaxHoldsV1's. The binding is
    /// written whole both ways: up the new scene, down the scene as MarshesTasteOfTheirOwnMedicineV1 wrote it.
    /// </summary>
    public static class MarshesHackWavesOnPlayerV1
    {
        public static readonly string[] LeftOut = Enumerable.Range(0, MarshesTasteOfTheirOwnMedicineV1.Waves.Length)
            .Select(wave => MarshesTasteOfTheirOwnMedicineV1.WaveRole(wave, 3)).ToArray();

        /// <summary>The binding as the live server has it: MarshesTasteOfTheirOwnMedicineV1's.</summary>
        public static string Before() => MarshesBindings.Written(MarshesTasteOfTheirOwnMedicineV1.Up, MarshesTasteOfTheirOwnMedicineV1.MissionId);

        /// <summary>The scene now: the wave members attack the player, and each wave's fourth member is gone.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MarshesBindings.Read(Before());
            foreach (var role in LeftOut)
                scene.Actors.Remove(role);
            foreach (var sequence in scene.Sequences.Values)
            {
                sequence.World.RemoveAll(intent => LeftOut.Contains(intent.Role));
                for (var index = 0; index < sequence.World.Count; index++)
                    if (sequence.World[index] is AttackActorIntent attack && attack.Role.StartsWith("wave-", System.StringComparison.Ordinal))
                        sequence.World[index] = attack with { TargetRole = null };
            }
            return scene;
        }

        public static void Up(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesTasteOfTheirOwnMedicineV1.MissionId, MarshesBindings.Json(Scene()));

        public static void Down(MigrationBuilder migration) =>
            MarshesBindings.Write(migration, MarshesTasteOfTheirOwnMedicineV1.MissionId, Before());
    }
}
