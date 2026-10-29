using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Trinity Bridge Logos: Past (native mission 1044), Plateau: Vogren's hologram sends the player for the
    /// Logos "Past" in a vault under Martyr's Canyon. The first of the six Trinity Bridge Logos; the other
    /// five (1047, 1048, 1049, 1051, 1057) lie in instanced maps, out of scope.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5870-5873 and 6379 (missionconversation); objective 1
    /// "Acquire "Past."" 5874/6380, no counter (missionobjective); no objective conversation. The Logos is
    /// gamecontext logos 57 "Past" (class 21330), on this map at (-120, 329, -703) - the server's logos shrine
    /// 57, at the client's ArchElohLogosDispenserBase (-120, 326.3, -703).
    ///
    /// Sourced: TaRapedia Trinity Bridge Logos: Past: giver and receiver the Holographic Projection Device,
    /// Vogren's Tomb; level "31 (unconfirmed)"; requirement A Voice From The Past; 50,000 XP, 4,950 credits;
    /// reward the Logos Past. TaRapedia: the vault is a cave reached from Maligo Creek.
    ///
    /// Reconstructed (ESTIMATE): level 31; no item. A Logos the player has already counts. The cave's
    /// tunnels reach Trinity Bridge on the navmesh up to a 3 m lip at the vault's ramp (z -754); the
    /// player's way over it is not checked (playtest).
    /// </summary>
    public static class PlateauTrinityLogosPastV1
    {
        public const uint MissionId = 1044;
        public const uint Level = 31;
        public const uint Hologram = PlateauVogrenNpcsV1.Hologram;
        public const uint Past = 57;                    // logos "Past", class 21330

        public const uint Experience = 50000;
        public const uint Credits = 4950;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Trinity Bridge Logos: Past", 5870, Hologram, Hologram, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Prerequisite(PlateauVoiceFromThePastV1.MissionId);

            mission.Objective(1, 5874, 6380, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.LogosAcquired, Past, null);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -120, 326.3, -703);         // the Logos in the vault

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1044",
                "Native1 'Acquire \"Past.\"' 5874/6380, no counter; texts 5870-5873, 6379. Logos57 'Past' class21330 at " +
                "-120,329,-703 (gamecontext); ArchElohLogosDispenserBase at -120,326.3,-703 (client map).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos:_Past",
                "Giver/receiver the Holographic Projection Device, Vogren's Tomb; level '31 (unconfirmed)'; requirement A Voice " +
                "From The Past; 50000XP, 4950 credits; reward the Logos Past. The vault: a cave reached from Maligo Creek.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauTrinityLogosPastV1",
                "Level31, no item. Completes on Logos57; a Logos already owned counts. The hologram is a stand-in creature " +
                "(PlateauVogrenNpcsV1).", 0.3);

            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { 1 } });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
