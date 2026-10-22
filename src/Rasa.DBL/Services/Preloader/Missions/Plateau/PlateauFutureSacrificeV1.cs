using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Future Sacrifice (native mission 1171), Temporal Chamber (squad instance 2029): the Eloh hologram
    /// Veryin sets the first trial, in a possible future where the Bane hold Foreas, and sends the player
    /// through the teleporter on its right to Colonel Hudson, who takes the mission there.
    ///
    /// Native: texts 8729-8733 (missionconversation); objective 1 "Speak with Colonel Hudson." 8751/8752, no
    /// counter (missionobjective); objectiveconversation 1171,1,1246 text 8753 (Hudson,
    /// PlateauTemporalChamberNpcsV1).
    ///
    /// Sourced: TaRapedia Future Sacrifice: giver the Eloh Hologram, receiver Colonel Hudson, both in the
    /// Temporal Chamber; requirement Array of Hope; 35,000 XP, 5,550 credits, one of a Class VII Res Trauma
    /// Kit, 3 Class VII Concussion Grenades, 6 Class VII Advanced Med Packs, 3 Class VII Fragmentation
    /// Grenades; "Step into the teleporter to my right and seek out Colonel Hudson." TR MMO DB (its second
    /// Array of Hope page, "speak with Colonel Hudson"): level 30, 140,000 XP, 5,550 credits.
    ///
    /// Reconstructed (ESTIMATE): level 30; TaRapedia's 35,000 XP, not TR MMO DB's 140,000 (that is The Final
    /// Assault's); the items as the class VII consumables of the server's other rewards (Res Sickness Kit,
    /// Sonic and Physical grenades, Consumable_Medpack_07C). No indicator: Hudson is in an instance.
    /// </summary>
    public static class PlateauFutureSacrificeV1
    {
        public const uint MissionId = 1171;
        public const uint Level = 30;
        public const uint Veryin = PlateauTemporalChamberNpcsV1.Veryin;
        public const uint Hudson = PlateauTemporalChamberNpcsV1.Hudson, HudsonPackage = PlateauTemporalChamberNpcsV1.HudsonPackage;

        public const uint Experience = 35000;
        public const uint Credits = 5550;
        public const uint ResKit = 47599;               // Class VII Res Trauma Kit (Consumable_Res_Sickness_Kit_07_35)
        public const uint ConcussionGrenade = 111018;   // Class VII Concussion Grenade (Consumable_Grenade_Sonic_07_35)
        public const uint AdvancedMedPack = 45062;      // Class VII Advanced Med Pack (Consumable_Medpack_07C_35)
        public const uint FragGrenade = 45446;          // Class VII Fragmentation Grenade (Consumable_Grenade_Physical_07_35)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Future Sacrifice", 8729, Veryin, Hudson, Level);
            mission.Reward(Experience, Credits,
                new (uint, uint)[] { (ResKit, 1), (ConcussionGrenade, 3), (AdvancedMedPack, 6), (FragGrenade, 3) },
                new (uint, uint)[0]);
            mission.Prerequisite(PlateauArrayOfHopeV1.MissionId);

            mission.Objective(1, 8751, 8752, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, HudsonPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1171",
                "Native1 'Speak with Colonel Hudson.' 8751/8752, no counter; objectiveconversation 1171,1,1246 text8753. Texts 8729-8733.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Future_Sacrifice",
                "Giver the Eloh Hologram, receiver Colonel Hudson (Temporal Chamber); requirement Array of Hope; 35000XP, 5550 " +
                "credits, one of a Class VII Res Trauma Kit/3 Concussion Grenades/6 Advanced Med Packs/3 Fragmentation Grenades. " +
                "TR MMO DB level30, 140000XP, 5550 credits.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauFutureSacrificeV1",
                "Level30, TaRapedia's 35000XP. Items 47599x1, 111018x3, 45062x6, 45446x3. Hudson through the chamber's teleporter " +
                "(PlateauTemporalChamberNpcsV1). No indicator (instance).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
