using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Cryptic Messages (native mission 557), Plateau: Comm Officer Devinchy at Wedge Rock Outpost needs a
    /// Bane Decryption Device; Comm Officer McKinley knows who carries one - Thrax Preceptor Vorvaak, near
    /// Trinity Bridge. General Bailey closes the mission by radio.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 1580-1583 and 6349 (missionconversation; the turn-in
    /// 1583 is General Bailey's); objectives 1 "Speak to Comm Officer McKinley." 3415/6350, 2 "Acquire a
    /// Decryption Device." 3419/6351, 3 "Return to Devinchy." 3420/6352, no counters (missionobjective);
    /// McKinley's line on package 395 for objective 1 (3418), Devinchy's on 389 for objective 3 (3421)
    /// (objectiveconversation, flag 1). The device is class 8095 "Bane Decryption Device" (template 764).
    /// Devinchy is creature 510178, McKinley 510179, Vorvaak 520054 (pool at Trinity Bridge).
    ///
    /// Sourced (TaRapedia Cryptic Messages, stub): giver Devinchy, reward giver "Headquarters", "Radio";
    /// level "32 (possibly less)"; 45,000 XP, 4,800 credits, "Equipment". Vorvaak "wanders the Eloh Forum".
    ///
    /// Reconstructed (ESTIMATE): 1 open, 2 and 3 open in turn; the device a personal drop from Vorvaak at
    /// every kill while objective 2 is open; Devinchy takes it at objective 3; then the mission is turned
    /// in by radio (no receiver NPC). "Equipment" read as the four Uncommon V04 legs of the 30-34 band.
    /// </summary>
    public static class PlateauCrypticMessagesV1
    {
        public const uint MissionId = 557;
        public const uint Level = 32;
        public const uint Devinchy = 510178, DevinchyPackage = 389;
        public const uint McKinley = 510179, McKinleyPackage = 395;
        public const uint Vorvaak = 520054;
        public const uint DeviceTemplate = 764, DeviceClass = 8095;
        public const string DeviceKey = "bane-decryption-device";
        public const int DeviceChance = 100;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioLegs = 46027;              // Armor_T3_Bio_V04_UNC_Legs_30_to_34
        public const uint GravitonLegs = 25208;         // Armor_T3_Graviton_V04_UNC_Legs_30_to_34
        public const uint MechLegs = 34334;             // Armor_T3_Mech_V04_UNC_Legs_30_to_34
        public const uint StealthLegs = 40462;          // Armor_T3_Stealth_V04_UNC_Legs_30_to_34

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Cryptic Messages", 1580, Devinchy, null, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, GravitonLegs, MechLegs, StealthLegs });
            mission.RadioCompletion();

            mission.Objective(1, 3415, 6350, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 3419, 6351, 1, MissionObjectiveState.Inactive);
            mission.Objective(3, 3420, 6352, 2, MissionObjectiveState.Inactive);

            // 1: McKinley names Vorvaak.
            mission.Transition(1);
            mission.Conversation(1, McKinleyPackage);
            Open(mission, 1, 1, 2);

            // 2: Vorvaak's device.
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, DeviceClass, 1);
            Open(mission, 2, 1, 3);

            // 3: Devinchy decodes with it.
            mission.Transition(3);
            mission.Conversation(3, DevinchyPackage);
            mission.Action(3, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("devinchy-takes-device", MissionId, DeviceKey, 1, MissionItemScope.CharacterOwned));
            mission.Action(3, 2, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, 477.4, 426, 428.4);        // McKinley
            mission.Indicator(2, 2, -120, 386, 211, 60);        // Vorvaak at Trinity Bridge
            mission.Indicator(3, 3, 482, 426, 430);             // Devinchy

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/557",
                "Native1 McKinley 3415/6350, 2 device 3419/6351, 3 Devinchy 3420/6352, no counters; objectiveconversation 557,1,395 " +
                "text3418 and 557,3,389 text3421. Texts 1580-1583 (turn-in by General Bailey), 6349. Device class8095/template764.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Cryptic_Messages",
                "Stub. Giver Comm Officer Devinchy, Wedge Rock Outpost; reward giver Headquarters, Radio; level '32 (possibly less)'; " +
                "45000XP, 4800 credits, 'Equipment'; Vorvaak wanders the Eloh Forum.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauCrypticMessagesV1",
                "1 opens 2, 2 opens 3. Device a 100% personal drop from Vorvaak520054 while 2 is open; Devinchy takes it at 3; radio " +
                "turn-in (channel Radio, no receiver). Equipment read as Uncommon V04 legs 30-34: 46027, 25208, 34334, 40462.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 2 },
                Items = new()
                {
                    new MissionItemBinding(DeviceKey, DeviceTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { Vorvaak }, 2, PlateauMissionDataV1.MapContextId, DeviceChance, 1))
                }
            });
        }

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(PlateauMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
