using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Good Day to Die (native mission 1283), Ligo Ashen Desert: Lt. Colonel Trent Ivan at Shadow's Edge Post sends the player
    /// east to Captain Akio Yamamoto in the Boneyard.
    ///
    /// Native: texts 10354-10357, 10358 "none" (missionconversation): log "Travel out to the Boneyard in the eastern Ashen
    /// Desert and report to Captain Yamamoto.", offer "Captain Yamamoto is stationed out in The Boneyard ... The Boneyard is
    /// east of here, just over Stonewall Ridge and past Ashoka Settlement. ...", turn-in "Well, I can use all the help I can
    /// get. ..."; objective 1 "Report to Captain Yamamoto" 10359/10360, no counter (missionobjective); objectiveconversation
    /// (1283,1) package 1414 (Yamamoto) text 10361 "Whoo boy! Looks like we got ourselves some more fresh meat.", variant 1.
    /// Names 9261 Lt. Colonel Trent Ivan, 9303 Captain Akio Yamamoto (TaRapedia spells him "Aiko"; the client wins, D15).
    ///
    /// Sourced: TaRapedia Good Day to Die: giver Lt. Colonel Trent Ivan (Shadow's Edge Post), reward giver Captain Yamamoto
    /// (Boneyard Outpost); no requirement; 45,000 XP, 4,100 credits; one of Class VIII Concussion Grenade x3, Basic Med Pack
    /// x6, Frag Grenade x3, EMP Bomb. TaRapedia Captain Aiko Yamamoto: Boneyard Outpost "in eastern tent", 686, 290, -495,
    /// level 49. Items by their client names: 26269 Class VIII Concussion Grenade (template 111019), 22546 Basic Med Pack
    /// (45063), 22964 Fragmentation Grenade (45447), 26543 EMP Bomb (111233).
    ///
    /// Reconstructed (ESTIMATE): level 40 (triage D5); the four stacks one to choose; a marker at Yamamoto.
    /// </summary>
    public static class AshenGoodDayToDieV1
    {
        public const uint MissionId = 1283;
        public const uint Level = 40;
        public const uint Ivan = AshenLeadInNpcsV1.Ivan;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint YamamotoPackage = AshenLeadInNpcsV1.YamamotoPackage;
        public const uint Report = 1;
        public const double MarkerRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4100;
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 3;   // Class VIII Concussion Grenade (class 26269)
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack (class 22546)
        public const uint FragGrenade = 45447, FragGrenades = 3;                // Class VIII Fragmentation Grenade (class 22964)
        public const uint EmpBomb = 111233, EmpBombs = 1;                       // Class VIII EMP Bomb (class 26543)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Good Day to Die", 10354, Ivan, Yamamoto, Level);
            mission.Reward(Experience, Credits,
                new[] { (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks), (FragGrenade, FragGrenades), (EmpBomb, EmpBombs) },
                new (uint, uint)[0]);

            mission.Objective(Report, 10359, 10360, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, YamamotoPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var yamamoto = AshenLeadInNpcsV1.At(Yamamoto);
            mission.Indicator(Report, 1, yamamoto.X, yamamoto.Y, yamamoto.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1283",
                "Native1 'Report to Captain Yamamoto' 10359/10360, no counter; objectiveconversation 1283,1 package1414 text10361. " +
                "Texts 10354-10357, 10358 'none'. Names 9261 Ivan, 9303 Captain Akio Yamamoto. Items 26269/111019, 22546/45063, " +
                "22964/45447, 26543/111233 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Good_Day_to_Die",
                "Giver Lt. Colonel Trent Ivan (Shadow's Edge Post), reward giver Captain Yamamoto (Boneyard Outpost); no requirement; " +
                "45000XP, 4100 credits; Class VIII Concussion Grenade x3, Basic Med Pack x6, Frag Grenade x3, EMP Bomb. Yamamoto's " +
                "page: Boneyard Outpost, eastern tent, 686,290,-495, level49.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenGoodDayToDieV1",
                "Level40 estimated. Ivan 790001 new at Shadow's Edge Post; Yamamoto 790003 new by TaRapedia's loc (package1414). " +
                "Stacks 111019x3, 45063x6, 45447x3, 111233x1, one to choose. A marker at Yamamoto. NPC facing rotation0 estimated.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
