using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Dustiest Place on Earth (native mission 1250), Ligo Ashen Desert: Sergeant Bernard Ames at the Ashen Lookout sends
    /// the new arrival down the west road to Colonel Mason Linier at Shadow's Edge Post.
    ///
    /// Native: texts 10000-10003, 10004 (missionconversation; 10004 is empty): log "Report to Colonel Mason Linier in Shadow's
    /// Edge Post, which is west of the entrance into Ashen Desert.", offer "If you're here reporting for duty, I suggest you go
    /// back and head down the west road towards Shadow's Edge Post. ...", turn-in "The Ashen Desert is a dangerous place. ...
    /// My officer staff will brief you on our current operations."; objective 1 "Report to Colonel Linier" 10005/10006, no
    /// counter (missionobjective); objectiveconversation (1250,1) package 1389 (Colonel Mason Linier) text 10007 "A Receptive,
    /// eh?", variant 1. Names 9279 Sergeant Bernard Ames, 9260 Colonel Mason Linier (creaturenamelanguage).
    ///
    /// Sourced: TaRapedia The Dustiest Place on Earth: giver Sergeant Bernard Ames (Ashen Lookout), reward giver Colonel Mason
    /// Linier (Shadow's Edge Post); no requirement; 42,500 XP, 4,000 credits; one of Class VIII Advanced Med Pack x2, EMP
    /// Grenade x3, Res Trauma Kit, Concussion Grenade x3. TR MMO DB: the same XP and credits, Ames "Ashen Lookout" (its level
    /// 30 is an MMODB default, triage X2). The items by their client names (physicalentityclassnamelanguage): 22548 Class VIII
    /// Advanced Med Pack (template 45065), 26279 Class VIII EMP Grenade (111029), 23924 Class VIII Res Trauma Kit (47600),
    /// 26269 Class VIII Concussion Grenade (111019).
    ///
    /// Reconstructed (ESTIMATE): level 40 (the client's Ashen Desert band 40-43, a lead-in: triage D5); the four stacks one
    /// to choose; a marker at Linier.
    /// </summary>
    public static class AshenDustiestPlaceV1
    {
        public const uint MissionId = 1250;
        public const uint Level = 40;
        public const uint Ames = AshenLeadInNpcsV1.Ames;
        public const uint MasonLinier = AshenLeadInNpcsV1.MasonLinier;
        public const uint MasonLinierPackage = AshenLeadInNpcsV1.MasonLinierPackage;
        public const uint Report = 1;
        public const double MarkerRadius = 8;

        public const uint Experience = 42500;
        public const uint Credits = 4000;
        public const uint AdvancedMedPack = 45065, AdvancedMedPacks = 2;       // Class VIII Advanced Med Pack (class 22548)
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                 // Class VIII EMP Grenade (class 26279)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;              // Class VIII Res Trauma Kit (class 23924)
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 3;   // Class VIII Concussion Grenade (class 26269)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "The Dustiest Place on Earth", 10000, Ames, MasonLinier, Level);
            mission.Reward(Experience, Credits,
                new[] { (AdvancedMedPack, AdvancedMedPacks), (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades) },
                new (uint, uint)[0]);

            mission.Objective(Report, 10005, 10006, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, MasonLinierPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var linier = AshenLeadInNpcsV1.At(MasonLinier);
            mission.Indicator(Report, 1, linier.X, linier.Y, linier.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1250",
                "Native1 'Report to Colonel Linier' 10005/10006, no counter; objectiveconversation 1250,1 package1389 text10007. " +
                "Texts 10000-10003, 10004 empty. Names 9279 Ames, 9260 Mason Linier. Items 22548/45065, 26279/111029, 23924/47600, " +
                "26269/111019 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Dustiest_Place_on_Earth",
                "Giver Sergeant Bernard Ames (Ashen Lookout), reward giver Colonel Mason Linier (Shadow's Edge Post); no requirement; " +
                "42500XP, 4000 credits; Class VIII Advanced Med Pack x2, EMP Grenade x3, Res Trauma Kit, Concussion Grenade x3. TR MMO " +
                "DB the same XP and credits (level30 an MMODB default).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenDustiestPlaceV1",
                "Level40 estimated (zone band 40-42, Ellatha). Ames 790000 new at the Ashen Lookout camp; Linier 510010 moved to the " +
                "Shadow's Edge HQ entrance. Stacks 45065x2, 111029x3, 47600x1, 111019x3, one to choose. A marker at Linier. NPC facing rotation0 estimated.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
