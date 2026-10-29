using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Lookout Down Below (native mission 970), Plateau: Outpost Cmdr. Russ at Wedge Rock Outpost sends the
    /// player down to Field Sgt. Garde by the Valverde Chasm teleporter platform beneath Trinity Bridge.
    /// It opens The Stainless Steel Bloodhound (867).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5305-5308 and 6393 (missionconversation); objective 1
    /// "Rendezvous with Sgt. Garde." 5610/6394, no counter (missionobjective); Garde's "About time you got
    /// here, soldier." on package 387 (5611) (objectiveconversation 970,1, flag 1). Garde is creature
    /// 510183, already in the NPC seed at (-92.9, 269.12, 179.7) (TaRapedia /loc -103, 269, 183).
    ///
    /// Sourced: TaRapedia Lookout Down Below: giver Outpost Cmdr. Russ, reward from Field Sgt. Garde, no
    /// requirement, follow-up The Stainless Steel Bloodhound; 20000 XP, 3100 credits, Class VI Advanced
    /// Med Pack (2), Class VI Armor Charger (2), Class VI Incendiary Grenade (4), Class VI EMP Grenade (4).
    /// TR MMO DB quest list: level 30.
    ///
    /// Reconstructed: the four items are fixed rewards (TaRapedia lists no choice): templates 45059, 118897,
    /// 111037, 111027 (classes 22542, 28488, 26287, 26277, client names as above).
    /// </summary>
    public static class PlateauLookoutDownBelowV1
    {
        public const uint MissionId = 970;
        public const uint Level = 30;
        public const uint Russ = PlateauPerduNpcsV1.Russ;
        public const uint Garde = PlateauBloodhoundV1.Garde, GardePackage = PlateauBloodhoundV1.GardePackage;

        public const uint Experience = 20000;
        public const uint Credits = 3100;
        public static readonly (uint Template, uint Quantity)[] Items =
        {
            (45059, 2),     // Class VI Advanced Med Pack
            (118897, 2),    // Class VI Armor Charger
            (111037, 4),    // Class VI Incendiary Grenade
            (111027, 4),    // Class VI EMP Grenade
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Lookout Down Below", 5305, Russ, Garde, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], Items);

            mission.Objective(1, 5610, 6394, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, GardePackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -92.9, 269.12, 179.7);         // Garde

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/970",
                "Native1 'Rendezvous with Sgt. Garde.' 5610/6394, no counter; objectiveconversation 970,1,387 text5611. " +
                "Texts 5305-5308, 6393. Garde name4665, creature510183.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Lookout_Down_Below",
                "Giver Outpost Cmdr. Russ, reward from Field Sgt. Garde; no requirement; follow-up The Stainless Steel " +
                "Bloodhound; 20000XP, 3100 credits; Class VI Advanced Med Pack 2, Armor Charger 2, Incendiary Grenade 4, EMP " +
                "Grenade 4. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauLookoutDownBelowV1",
                "Fixed items 45059x2, 118897x2, 111037x4, 111027x4.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
