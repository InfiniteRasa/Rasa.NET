using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Tread Lightly (native mission 888), Valverde Marshes: Retread Duvall on the north road sends Retread Otto with the
    /// player through the woods to Retread Vincent at the Retread Camp (Retread City), so that Vincent can send help.
    ///
    /// Native: texts 4646-4649, 7005 (missionconversation): log "Escort Retread Otto through the woods to the Retread Camp in
    /// the southeast. Report in to Retread Vincent once you arrive.", turn-in "the Bane have been hassling everyone off to
    /// the east of here lately!"; objectives 1 "Escort Retread Otto to the Retread Camp" 4654/7006, 2 4655/7007 "no desc"
    /// (missionobjective); no objective conversation. Name 8550 Retread Otto.
    ///
    /// Sourced: TaRapedia Tread Lightly / Tread Lighty: giver Retread Duvall (Stalker Woods), receiver Retread Vincent;
    /// 65,000 XP, 5,400 credits; one of 6 Class VII Fragmentation Grenade, 2 Class VII EMP Bomb, 6 Class VII Cryogenic
    /// Grenade, 8 Class VII Standard Med Pack (the current list); "hug the north side of Stalker's Woods and avoid the
    /// strider". TR MMO DB list: level 35. World DB: Vincent 510168 at (-276.8, 217.5, -761).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Otto the run's own creature (MarshesEscortsNpcsV1.Otto), beside Duvall at the accept, following the player; 15 m from
    ///    Vincent completes 1 and Otto goes in; his death fails 1. The navmesh path is 587 m (probe of 2026-11-02).
    ///  - Objective 2 (the client's "no desc", no text) left out: nothing names what it is.
    ///  - The stacks the lowest templates of the classes (as set 7's), one to choose. A marker at Vincent.
    /// </summary>
    public static class MarshesTreadLightlyV1
    {
        public const uint MissionId = 888;
        public const uint Level = 35;
        public const uint Duvall = MarshesStalkerWoodsNpcsV1.Duvall, Vincent = MarshesStalkerWoodsNpcsV1.Vincent;
        public const uint Escort = 1, Area = 1, Arrived = 2;
        public const double ArrivalRadius = 15;

        public static readonly ScenePosition OttoAt = new(-591f, 217.01f, -436f);
        public static readonly ScenePosition VincentAt = new(-276.8f, 217.48f, -761f);

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint FragGrenade = 45446, FragGrenades = 6;                // Class VII Fragmentation Grenade
        public const uint EmpBomb = 111232, EmpBombs = 2;                       // Class VII EMP Bomb
        public const uint CryoGrenade = 111048, CryoGrenades = 6;               // Class VII Cryogenic Grenade
        public const uint StandardMedPack = 45061, StandardMedPacks = 8;        // Consumable_Medpack_07B_35

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Tread Lightly", 4646, Duvall, Vincent, Level);
            mission.Reward(Experience, Credits,
                new[] { (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryoGrenade, CryoGrenades), (StandardMedPack, StandardMedPacks) },
                new (uint, uint)[0]);
            mission.Scenario(Arrived, "Otto at the Retread Camp", 0);

            mission.Objective(Escort, 4654, 7006, 0, MissionObjectiveState.Incomplete);
            mission.Area(Area, MarshesMissionDataV1.MapContextId, VincentAt.X, VincentAt.Y, VincentAt.Z, ArrivalRadius);
            mission.Transition(Escort);
            mission.EnterArea(Escort, Area);
            mission.Action(Escort, 1, MissionActionKind.StartScenario, scenario: Arrived);
            mission.Action(Escort, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Escort, 1, VincentAt.X, VincentAt.Y, VincentAt.Z, ArrivalRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/888",
                "Native1 'Escort Retread Otto to the Retread Camp' 4654/7006, 2 4655/7007 'no desc'; no objectiveconversation. " +
                "Texts 4646-4649 (log: through the woods to the Retread Camp in the southeast, report to Vincent). Name 8550.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Tread_Lighty",
                "Giver Retread Duvall (Stalker Woods), receiver Retread Vincent; 65000XP, 5400 credits; one of 6 Fragmentation " +
                "Grenade VII, 2 EMP Bomb VII, 6 Cryogenic Grenade VII, 8 Standard Med Pack VII; hug the north side of Stalker's " +
                "Woods. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesTreadLightlyV1",
                "Personal Otto 760333 (Vincent's look, L35, 4000 HP) follows the player from Duvall; 15 m from Vincent completes " +
                "1; his death fails it. Objective 2 (no text) left out. Stacks 45446x6, 111232x2, 111048x6, 45061x8.", 0.3);

            mission.Enable(MarshesEscortsNpcsV1.Scene(MissionId,
                new MarshesEscortsNpcsV1.Leg(Escort, 0, Arrived, null, MarshesEscortsNpcsV1.Otto, new[] { OttoAt })));
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
