using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Big Secret (native mission 1581), Avernus Outpost (squad instance 2110) to Ashen Desert: Captain Petrovich sends the
    /// player through the Atta Teleporter on the Food Storage's west side to the Eloh Sanctuary, where the Eloh Zuriel tells him
    /// the Bane took the Atta Queen to the Conscription Facility; the turn-in is Captain Yamamoto at the Boneyard Outpost.
    ///
    /// Native: texts 14557-14561 (missionconversation): title "The Big Secret", log "Captain Petrovich wants you to go through the
    /// teleporter and talk to someone, but he isn't saying who. Once you're done in there you'll need to head to Ashen Desert and
    /// speak with Captain Yamamoto, who can be found in the Boneyard Outpost.", offer "Frogga's men got the other teleporter
    /// working ... He...well, he asked for you. Specifically ...", turn-in "So, you talked to Zuriel, eh? Well, this is the place
    /// you're looking for.", 14561 "none". Objectives (missionobjective) 1 "Enter the Final Chamber" 17157/17158, 2 "Speak with
    /// Zuriel" 17159/17160; no counters, no marker names. Objective conversation 1581,2 package 2075 (Zuriel) variant 1, 17162
    /// ("... The Bane have taken her to someplace called the Conscription Facility ..."). Name 9676 Zuriel.
    ///
    /// Sourced: TaRapedia The Big Secret: giver Captain Petrovich (Avernus Outpost), reward giver Captain Aiko Yamamoto (Ashen
    /// Desert); requires God Save the Queen; 40,000 XP, 5,850 credits; Class VII Fragmentation Grenade x3, Class VII EMP Bomb,
    /// Class VII Cryogenic Grenade x3, Class VII Standard Med Pack x3; "Travel to the west side of the Food Storage chamber and
    /// enter the Atta teleporter there. You will find yourself at the entrance of an Eloh Sanctuary. Inside is the Eloh Zuriel ...
    /// Return to Captain Aiko Yamamoto in Ashen Desert" (0.5). The client's log names the turn-in (Yamamoto, "speak with"): an NPC
    /// turn-in, not a radio one (QUEST-RULES).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 44 (the band's top, the chain's last). Requires 1537 (TaRapedia). Given at Petrovich's second row (790643) at the
    ///    Food Storage; taken in at Yamamoto (AshenLeadInNpcsV1 790003, Ashen Desert, the Boneyard Outpost).
    ///  - The way: the Food Storage's west pad (AshenAvernusHiveNpcsV1 919) to the Eloh Sanctuary's entrance corridor (920). 1 the
    ///    player in the sanctuary's round room (AreaEntered, ChamberRadius round its middle, 23 m on from the pad) opens 2; 2 the
    ///    talk with Zuriel (17162) gives the reward. The TaRapedia note "one Operation for the Ashen Desert Targets of
    ///    Opportunity" (1580, upstream, out of scope) is not built.
    ///  - The items by their client names, each a stack, one to choose: Class VII Fragmentation Grenade (45446) x3, Class VII EMP
    ///    Bomb (111232, the lower of its two templates) x1, Class VII Cryogenic Grenade (111048) x3, Class VII Standard Med Pack
    ///    (45061) x3.
    ///  - Markers (unnamed): 1 the round room, 2 Zuriel; on Avernus Outpost. Yamamoto has none (the client draws a completeable
    ///    mission's receiver itself, as 1533).
    /// </summary>
    public static class AshenTheBigSecretV1
    {
        public const uint MissionId = 1581;
        public const uint Level = 44;
        public const uint Outpost = AshenAvernusHiveNpcsV1.Outpost;
        public const uint Petrovich = AshenAvernusOutpostNpcsV1.PetrovichTop, Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint ZurielPackage = AshenAvernusHiveNpcsV1.ZurielPackage, Variant = 1;
        public const uint FinalChamber = 1, SpeakWithZuriel = 2;
        public const uint ChamberArea = 1;
        public const double ChamberRadius = 10, NpcMarkerRadius = 8;

        /// <summary>The middle of the Eloh Sanctuary's round room (its obelisk and Logos signs at (628, 275-287, -660), client map).</summary>
        public static readonly (double X, double Y, double Z) ChamberAt = (628, 276, -662);

        public const uint Experience = 40000;
        public const uint Credits = 5850;
        public const uint FragGrenade = 45446, FragGrenades = 3;                // Class VII Fragmentation Grenade (class 22963)
        public const uint EmpBomb = 111232, EmpBombs = 1;                       // Class VII EMP Bomb (class 26542)
        public const uint CryogenicGrenade = 111048, CryogenicGrenades = 3;     // Class VII Cryogenic Grenade (class 26298)
        public const uint StandardMedPack = 45061, StandardMedPacks = 3;        // Class VII Standard Med Pack (class 22544)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "The Big Secret", 14557, Petrovich, Yamamoto, Level);
            mission.Prerequisite(AshenGodSaveTheQueenV1.MissionId);
            mission.Reward(Experience, Credits,
                new[] { (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryogenicGrenade, CryogenicGrenades), (StandardMedPack, StandardMedPacks) },
                new (uint, uint)[0]);

            mission.Objective(FinalChamber, 17157, 17158, 0, MissionObjectiveState.Incomplete);
            mission.Objective(SpeakWithZuriel, 17159, 17160, 1, MissionObjectiveState.Inactive);

            mission.Area(ChamberArea, Outpost, ChamberAt.X, ChamberAt.Y, ChamberAt.Z, ChamberRadius);
            mission.Transition(FinalChamber);
            mission.EnterArea(FinalChamber, ChamberArea);
            mission.Action(FinalChamber, 1, MissionActionKind.RevealObjective, targetObjective: SpeakWithZuriel);
            mission.Action(FinalChamber, 2, MissionActionKind.ActivateObjective, targetObjective: SpeakWithZuriel, state: MissionObjectiveState.Incomplete);

            mission.Transition(SpeakWithZuriel);
            mission.Conversation(SpeakWithZuriel, ZurielPackage, Variant);
            mission.Action(SpeakWithZuriel, 1, MissionActionKind.GrantReward, reward: 1);

            var zuriel = AshenAvernusHiveNpcsV1.ZurielAt;
            mission.Indicator(FinalChamber, 1, ChamberAt.X, ChamberAt.Y, ChamberAt.Z, ChamberRadius, Outpost);
            mission.Indicator(SpeakWithZuriel, 2, zuriel.X, zuriel.Y, zuriel.Z, NpcMarkerRadius, Outpost);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1581",
                "Native1 'Enter the Final Chamber' 17157/17158, 2 'Speak with Zuriel' 17159/17160; no counters, no indicator names. " +
                "objectiveconversation 1581,2 pkg2075 17162. Texts 14557-14561 (log: speak with Captain Yamamoto in the Boneyard " +
                "Outpost). Name 9676 Zuriel.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Big_Secret",
                "Giver Captain Petrovich (Avernus Outpost), reward giver Captain Aiko Yamamoto (Ashen Desert); requires God Save the " +
                "Queen; 40000XP, 5850 credits; Class VII Fragmentation Grenade x3, Class VII EMP Bomb, Class VII Cryogenic Grenade x3, " +
                "Class VII Standard Med Pack x3; the Food Storage's west Atta teleporter, the Eloh Sanctuary's entrance, Zuriel.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenTheBigSecretV1",
                "Level44; requires 1537; Petrovich's second row 790643 gives, Yamamoto 790003 (Ashen Desert) rewards. Pads 919 " +
                "(Food Storage west) and 920 (the Eloh Sanctuary's entrance corridor); 1 the 10 m area in the round room " +
                "(628,276,-662); 2 Zuriel's talk (pkg2075) and the reward. Items 45446x3, 111232x1, 111048x3, 45061x3, one to choose.", 0.3);

            // No actors: the scene only puts the markers on Avernus Outpost.
            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = Outpost };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
