using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Stick Your Neck Out (native mission 1329), Valverde Pools: Amee Corman at the Retread Camp sends the player into
    /// the Retread Caves (squad instance 1694) to Kaethi Rose.
    ///
    /// Native: texts 10861-10865 (missionconversation): log "Amee Corman wants you to travel to the Retread Caves to
    /// speak with Kaethi Rose.", offer "Go check in with Kaethi Rose inside, she'll be able to tell you more.", turn-in
    /// "Let's get you to work."; objective 1 "Speak with Kaethi Rose" 10866/10867 (missionobjective); objectiveconversation
    /// 1329,1 package 336 text 10868 "AFS huh? Maybe we can clear this place out yet."
    ///
    /// Sourced: TaRapedia Stick Your Neck Out: giver Amee Corman (Retread Camp), reward giver Kaethi Rose (Retread Caves);
    /// requires Retread Planning; 30,000 XP, 3,500 credits; 2 Class VII Advanced Med Pack, 2 Class VII Adrenaline
    /// Booster, 4 Class VII EMP Grenade, 4 Class VII Concussion Grenade. Ten Ton Hammer Retread Caves guide: "turn it in
    /// with Kaethi Rose at the Retread Camp"; not needed for the caves' missions. TR MMO DB: level 33. Client names
    /// (physicalentityclassnamelanguage): 45062 "Class VII Advanced Med Pack", 118814 "Class VII Adrenaline Booster",
    /// 111028 "Class VII EMP Grenade", 111018 "Class VII Concussion Grenade".
    ///
    /// Reconstructed (ESTIMATE): talk only, with Kaethi (PoolsRetreadCavesNpcsV1) in the caves' camp. The items all come
    /// with the turn-in, as Retread Planning Part II's. Markers in the instance too.
    /// </summary>
    public static class PoolsStickYourNeckOutV1
    {
        public const uint MissionId = 1329;
        public const uint Level = 33;
        public const uint Amee = PoolsRetreadCampNpcsV1.Amee;
        public const uint Kaethi = PoolsRetreadCavesNpcsV1.Kaethi;
        public const uint Speak = 1;
        public const double NpcRadius = 8;

        public const uint Experience = 30000;
        public const uint Credits = 3500;
        public const uint MedPack = 45062, MedPacks = 2;                 // Class VII Advanced Med Pack
        public const uint AdrenalineBooster = 118814, AdrenalineBoosters = 2; // Class VII Adrenaline Booster
        public const uint EmpGrenade = 111028, EmpGrenades = 4;          // Class VII EMP Grenade
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4; // Class VII Concussion Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Stick Your Neck Out", 10861, Amee, Kaethi, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (MedPack, MedPacks), (AdrenalineBooster, AdrenalineBoosters), (EmpGrenade, EmpGrenades), (ConcussionGrenade, ConcussionGrenades)
            });
            mission.Prerequisite(PoolsRetreadPlanningV1.MissionId);

            mission.Objective(Speak, 10866, 10867, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Speak);
            mission.Conversation(Speak, PoolsRetreadCavesNpcsV1.KaethiPackage);
            mission.Action(Speak, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1329",
                "Native1 'Speak with Kaethi Rose' 10866/10867; objectiveconversation 1329,1 package336 text10868. Texts " +
                "10861-10865. Items by name 45062, 118814, 111028, 111018.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Stick_Your_Neck_Out",
                "Giver Amee Corman (Retread Camp), reward Kaethi Rose (Retread Caves); requires Retread Planning; 30000XP, 3500 " +
                "credits, 2 Class VII Advanced Med Pack, 2 Class VII Adrenaline Booster, 4 Class VII EMP Grenade, 4 Class VII " +
                "Concussion Grenade. Ten Ton Hammer: Kaethi at the camp in the caves. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsStickYourNeckOutV1",
                "Talk only, Kaethi 760222 in the caves. Items 45062x2, 118814x2, 111028x4, 111018x4, all given. Indicators in the instance too.", 0.3);

            var kaethi = PoolsRetreadCavesNpcsV1.KaethiAt;
            mission.Indicator(Speak, 1, kaethi.X, kaethi.Y, kaethi.Z, NpcRadius);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
