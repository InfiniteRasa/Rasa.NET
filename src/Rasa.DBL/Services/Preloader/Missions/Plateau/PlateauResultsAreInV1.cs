using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Results Are In (native mission 936), Plateau: Science Officer Clark finds the surveillance data
    /// locked for Penumbra and sends the player to Special Agent Perdu outside the Fort Defiance gates.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5037-5040 and 6433 (missionconversation); objective 1
    /// "Meet with Special Agent Perdu." 5772/6434, no counter (missionobjective); Perdu's "If it isn't my
    /// favorite agent-in-training!" on package 639 (5773) (objectiveconversation, flag 1).
    ///
    /// Sourced (TaRapedia The Results Are In): giver Science Officer Clark, reward from Agent Perdu,
    /// requirement Photo Ops, follow-up Mind Slayer; 22,500 XP, 3,200 credits, "Consumables".
    ///
    /// Reconstructed (ESTIMATE): level 32 (no source); "Consumables" read as The Defiant Ones' (887), the same
    /// XP and credits: two Class VI Standard Med Packs and a Res Trauma Kit, no choice.
    /// </summary>
    public static class PlateauResultsAreInV1
    {
        public const uint MissionId = 936;
        public const uint Level = 32;
        public const uint Clark = PlateauPerduNpcsV1.Clark;
        public const uint Perdu = PlateauPerduNpcsV1.Perdu, PerduPackage = PlateauPerduNpcsV1.PerduPackage;

        public const uint Experience = 22500;
        public const uint Credits = 3200;
        public const uint MedPack = PlateauDefiantOnesV1.MedPack, MedPacks = PlateauDefiantOnesV1.MedPacks;
        public const uint ResKit = PlateauDefiantOnesV1.ResKit;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Results Are In", 5037, Clark, Perdu, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new (uint, uint)[] { (MedPack, MedPacks), (ResKit, 1) });
            mission.Prerequisite(PlateauPhotoOpsV1.MissionId);

            mission.Objective(1, 5772, 6434, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, PerduPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var perdu = PlateauPerduNpcsV1.Moves[0].At;
            mission.Indicator(1, 1, perdu.X, perdu.Y, perdu.Z);   // Perdu at the gate

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/936",
                "Native1 'Meet with Special Agent Perdu.' 5772/6434, no counter; objectiveconversation 936,1,639 text5773. " +
                "Texts 5037-5040, 6433.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Results_Are_In",
                "Giver Science Officer Clark, reward from Agent Perdu; requirement Photo Ops; follow-up Mind Slayer; 22500XP, " +
                "3200 credits, 'Consumables'; Perdu outside the Fort Defiance gate.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauResultsAreInV1",
                "Level32 (no source). Consumables as The Defiant Ones': 2 Standard Med Packs 45058, 1 Res Trauma Kit 47598. " +
                "Perdu moved to the gate (PlateauPerduNpcsV1).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
