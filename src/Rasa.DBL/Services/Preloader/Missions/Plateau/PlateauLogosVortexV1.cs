using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Logos: Vortex (native mission 1659), Plateau: Receptive Liaison Ridout at Fort Defiance gives the player
    /// the place of the last Logos terminal, "Vortex", in a river cave south of Fort Defiance.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 15972-15976 (missionconversation); objective 18 "Acquire
    /// Logos Information: Vortex" 15977/15978, no counter (missionobjective); no objective conversation. The
    /// Logos is the server's shrine 48 "Vortex" (class 12700) at (-120, 410.75, -324); the client map has the
    /// chamber door signs Eloh, Empower, Only, The, Strong and "True power is now yours" at (-120, 408-413, -340).
    ///
    /// Sourced: TaRapedia Logos: Vortex: giver and receiver Receptive Liaison Ridout, Fort Defiance; level 30;
    /// requirement none; 100,000 XP, 4,950 credits; reward the Logos Vortex; the door needs Eloh, Empower, Only,
    /// The, Strong. TR MMO DB quest list: level 30.
    ///
    /// Reconstructed: no item. A Vortex Logos the player already has counts.
    /// </summary>
    public static class PlateauLogosVortexV1
    {
        public const uint MissionId = 1659;
        public const uint Level = 30;
        public const uint Acquire = 18;
        public const uint Vortex = 48;                  // logos "Vortex", class 12700
        public const uint Ridout = PlateauBaileyNpcsV1.Ridout;

        public const uint Experience = 100000;
        public const uint Credits = 4950;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Logos: Vortex", 15972, Ridout, Ridout, Level);
            mission.Reward(Experience, Credits, new uint[0]);

            mission.Objective(Acquire, 15977, 15978, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Acquire);
            mission.Progress(Acquire, MissionProgressEventKind.LogosAcquired, Vortex, null);
            mission.Action(Acquire, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Acquire, 1, -120, 410.75, -324);      // the Logos in the river cave

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1659",
                "Native18 'Acquire Logos Information: Vortex' 15977/15978, no counter; texts 15972-15976. Logos48 'Vortex' " +
                "class12700 at -120,410.75,-324; door signs Eloh/Empower/Only/The/Strong at -120,408,-340 (client map). " +
                "Ridout name9939.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Logos:_Vortex",
                "Giver/receiver Receptive Liaison Ridout, Fort Defiance; level 30; requirement none; 100000XP, 4950 credits; " +
                "reward the Logos Vortex; door needs Eloh, Empower, Only, The, Strong. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauLogosVortexV1",
                "No item. A Vortex Logos already owned counts. Ridout new (PlateauBaileyNpcsV1).", 0.3);

            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { Acquire } });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
