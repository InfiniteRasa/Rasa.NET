using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Defiant Ones (native mission 887), Plateau: Transportation Officer Mamede welcomes the player
    /// to Fort Defiance and sends them to Field Lt. Peterson at High Command. It opens Fort Defiance's
    /// missions: A Bridge Too Far (713) and Crystal Blue Persuasion (954) require it.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4642-4645 and 6469 (missionconversation); objective
    /// 1 "Report to Field Lt. Peterson." 6968/6969, no counter (missionobjective); Peterson's "New
    /// arrival, eh?" on package 385 (6970) (objectiveconversation, flag 1). Mamede is
    /// PlateauFortDefianceNpcsV1's creature 650025, Peterson 510182.
    ///
    /// Sourced (TaRapedia The Defiant Ones, stub): giver Mamede, reward from Peterson, level "30 (possibly
    /// less)", no requirement, follow-ups A Bridge Too Far, Blue Flu, Crystal Blue Persuasion; 22,500 XP,
    /// 3,200 credits, "Consumables"; "This quest must be completed to open up all of the Fort Defiance
    /// missions." Ten Ton Hammer: "You'll get 3200 credits and a bit of experience".
    ///
    /// Reconstructed (ESTIMATE): level 30; "Consumables" read as level 30 (Class VI) ones, given with
    /// the mission, no choice: two Standard Med Packs and a Res Trauma Kit. Blue Flu (1014) is not on the
    /// server.
    /// </summary>
    public static class PlateauDefiantOnesV1
    {
        public const uint MissionId = 887;
        public const uint Level = 30;
        public const uint Mamede = PlateauFortDefianceNpcsV1.Mamede;
        public const uint Peterson = PlateauFortDefianceNpcsV1.Peterson;
        public const uint PetersonPackage = PlateauFortDefianceNpcsV1.PetersonPackage;

        public const uint Experience = 22500;
        public const uint Credits = 3200;
        public const uint MedPack = 45058, MedPacks = 2;    // Class VI Standard Med Pack (Consumable_Medpack_06B_30)
        public const uint ResKit = 47598;                   // Class VI Res Trauma Kit (Consumable_Res_Sickness_Kit_06_30)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Defiant Ones", 4642, Mamede, Peterson, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new (uint, uint)[] { (MedPack, MedPacks), (ResKit, 1) });

            mission.Objective(1, 6968, 6969, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, PetersonPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -66, 401.12, 931);         // Peterson at High Command

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/887",
                "Native1 'Report to Field Lt. Peterson.' 6968/6969, no counter; objectiveconversation 887,1,385 text6970. " +
                "Texts 4642-4645, 6469. Mamede name8554 (creaturenamelanguage).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Defiant_Ones",
                "Stub. Giver Transportation Officer Mamede, reward from Field Lt. Peterson, Fort Defiance; level '30 (possibly " +
                "less)'; no requirement; 22500XP, 3200 credits, 'Consumables'; opens the Fort Defiance missions.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauDefiantOnesV1",
                "Level30. Consumables read as Class VI (level 30), fixed: 2 Standard Med Packs 45058, 1 Res Trauma Kit 47598. " +
                "Mamede added, Peterson moved to High Command (PlateauFortDefianceNpcsV1).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
