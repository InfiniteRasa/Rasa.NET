using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// You Can't Drink Ash (native mission 1284), Ligo Ashen Desert: Lt. Colonel Dania Caldwell at Shadow's Edge Post sends
    /// the player north to Captain Daris Linier at White Oasis Post in Maji Field.
    ///
    /// Native: texts 10362-10365, 10366 "none" (missionconversation): log "Travel to White Oasis Outpost up in Maji Field, in
    /// the north end of Ashen Desert. Report to Captain Daris Linier for further orders.", offer "Captain Linier is commanding
    /// the expedition at White Oasis Outpost ... White Oasis Outpost is on the western end of Maji Field. ...", turn-in
    /// "Right, so let's get to work securing those Water Harvesters, shall we?"; objective 1 "Report to Captain Linier"
    /// 10367/10368, no counter (missionobjective); objectiveconversation (1284,1) package 1476 (Daris Linier) text 10369 "I'm
    /// glad Caldwell has finally been sending more reinformcements.", variant 1. Names 9262 Lt. Colonel Dania Caldwell, 9358
    /// Captain Daris Linier.
    ///
    /// Sourced: TaRapedia You Can't Drink Ash: giver Lt. Colonel Dania Caldwell (Shadow's Edge Post), reward giver Captain
    /// Daris Linier (White Oasis Post); no requirement; 47,500 XP, 4,200 credits; one of Class VIII EMP Grenade x3, Res Trauma
    /// Kit, Concussion Grenade x3, Basic Med Pack x6. Items by their client names: 26279 Class VIII EMP Grenade (template
    /// 111029), 23924 Res Trauma Kit (47600), 26269 Concussion Grenade (111019), 22546 Basic Med Pack (45063).
    ///
    /// Reconstructed (ESTIMATE): level 40 (triage D5); Daris Linier is a service NPC of control point 17 (White Oasis Post,
    /// AFS by default): while the Bane hold the post he is not there, and the objective waits (triage "Control points"); the
    /// four stacks one to choose; a marker at Daris Linier.
    /// </summary>
    public static class AshenCantDrinkAshV1
    {
        public const uint MissionId = 1284;
        public const uint Level = 40;
        public const uint Caldwell = AshenLeadInNpcsV1.Caldwell;
        public const uint DarisLinier = AshenLeadInNpcsV1.DarisLinier;
        public const uint DarisLinierPackage = AshenLeadInNpcsV1.DarisLinierPackage;
        public const uint Report = 1;
        public const double MarkerRadius = 8;

        public const uint Experience = 47500;
        public const uint Credits = 4200;
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                 // Class VIII EMP Grenade (class 26279)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;              // Class VIII Res Trauma Kit (class 23924)
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 3;   // Class VIII Concussion Grenade (class 26269)
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack (class 22546)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "You Can't Drink Ash", 10362, Caldwell, DarisLinier, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);

            mission.Objective(Report, 10367, 10368, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, DarisLinierPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var linier = AshenLeadInNpcsV1.At(DarisLinier);
            mission.Indicator(Report, 1, linier.X, linier.Y, linier.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1284",
                "Native1 'Report to Captain Linier' 10367/10368, no counter; objectiveconversation 1284,1 package1476 text10369. " +
                "Texts 10362-10365, 10366 'none'. Names 9262 Caldwell, 9358 Daris Linier. Items 26279/111029, 23924/47600, " +
                "26269/111019, 22546/45063 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/You_Can%27t_Drink_Ash",
                "Giver Lt. Colonel Dania Caldwell (Shadow's Edge Post), reward giver Captain Daris Linier (White Oasis Post); no " +
                "requirement; 47500XP, 4200 credits; Class VIII EMP Grenade x3, Res Trauma Kit, Concussion Grenade x3, Basic Med Pack x6.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenCantDrinkAshV1",
                "Level40 estimated. Caldwell 790002 new at Shadow's Edge Post; Daris Linier 510007 moved inside White Oasis Post " +
                "(control point 17 service NPC: gone while the Bane hold it). Stacks 111029x3, 47600x1, 111019x3, 45063x6, one to " +
                "choose. A marker at Daris Linier. NPC facing rotation0 estimated.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
