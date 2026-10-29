using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Retreads to Retreads (native mission 1751): Amee Corman at the Retread Camp, Valverde Pools, cannot tell the
    /// player what she promised about the Eloh, and sends them to Retread Vincent in Retread City, Valverde Marshes.
    ///
    /// Native: texts 17125-17129 (missionconversation): offer "Retread Vincent who lives in Retread City over in
    /// Valverde Marshes should know if anybody does", turn-in "Well if Amee Corman sent you here she must have a lot of
    /// trust in you"; objective 1 "Speak to Retread Vincent" 17165/17166 (missionobjective), no counter; its talk
    /// (objectiveconversation) on package 951, text 17167 "What does a soldier like you want with me?".
    ///
    /// Sourced: TaRapedia Retreads to Retreads: giver Amee Corman (Retread Camp), reward from Retread Vincent, requires
    /// Weapons Trafficking, follow-up It Lies in Ruins; 30,000 XP, 3500 credits, 2 Class VII EMP Bombs, 2 Class VII
    /// Power Chargers, 4 Class VII Concussion Grenades, 4 Class VII Fragmentation Grenades (client classes 26542, 28498,
    /// 26268, 22963). TR MMO DB: level 33, series Weapons Trafficking. World DB: Amee Corman 510195 (package 329) at the
    /// Retread Camp; Retread Vincent 510168 (package 951) in Retread City, Valverde Marshes (map 1454).
    ///
    /// Revision plateau_1_16_5 (PlateauMissionDataV1), as Weapons Trafficking before it: a required chain stays on one
    /// revision (MissionContentValidator, "cross-revision-reference").
    ///
    /// Reconstructed (ESTIMATE): the reward items as the lowest template of each client class (the EMP Bomb has two),
    /// all given, as PlateauSouthOfTheBorderV1's consumables; Vincent's talk completes 1, turn-in at Vincent. A
    /// marker at Vincent, though he is on another map (as 1541's).
    /// </summary>
    public static class PlateauRetreadsToRetreadsV1
    {
        public const uint MissionId = 1751;
        public const uint Level = 33;
        public const uint Amee = PlateauNewOrdersFromTheGeneralV1.Amee;
        public const uint Vincent = 510168, VincentPackage = 951;   // creature and pool, "Retread Vincent", Valverde Marshes
        public const uint WeaponsTrafficking = PlateauWeaponsTraffickingV1.MissionId;
        public const uint Speak = 1;

        /// <summary>Vincent's spawn pool 510168 in Retread City (MissionNpcSpawnpoolPreloader, map 1454), the marker.</summary>
        public static readonly (double X, double Y, double Z) VincentAt = (-276.8, 217.48, -761.0);

        public const uint Experience = 30000;
        public const uint Credits = 3500;
        public const uint EmpBomb = 111232, EmpBombs = 2;                       // Class VII EMP Bomb
        public const uint PowerCharger = 118907, PowerChargers = 2;             // Class VII Power Charger
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4;   // Class VII Concussion Grenade
        public const uint FragGrenade = 45446, FragGrenades = 4;                // Class VII Fragmentation Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Retreads to Retreads", 17125, Amee, Vincent, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (EmpBomb, EmpBombs), (PowerCharger, PowerChargers), (ConcussionGrenade, ConcussionGrenades), (FragGrenade, FragGrenades)
            });
            mission.Prerequisite(WeaponsTrafficking);

            mission.Objective(Speak, 17165, 17166, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Speak);
            mission.Conversation(Speak, VincentPackage);
            mission.Action(Speak, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Speak, 1, VincentAt.X, VincentAt.Y, VincentAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1751",
                "Native1 'Speak to Retread Vincent' 17165/17166, no counter; objectiveconversation 1751,1 on package 951 text17167. " +
                "Texts 17125-17129.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Retreads_to_Retreads",
                "Giver Amee Corman (Retread Camp), reward from Retread Vincent; requires Weapons Trafficking; follow-up It Lies in " +
                "Ruins; 30000XP, 3500 credits, 2 Class VII EMP Bomb, 2 Class VII Power Charger, 4 Class VII Concussion Grenade, 4 " +
                "Class VII Fragmentation Grenade. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauRetreadsToRetreadsV1",
                "Level33. Vincent's talk (510168, Valverde Marshes) completes 1; turn-in at Vincent. Items 111232x2, 118907x2, " +
                "111018x4, 45446x4 (lowest template of each class). A marker at Vincent (another map: the server draws it on every map for now).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
