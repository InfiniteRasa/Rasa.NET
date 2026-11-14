using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Indra Caverns (native mission 1559), Ligo Ashen Desert to Indra Caverns: Surveyor Malya Odesha sends the player into the
    /// caverns behind her, where Major Darkua Diren waits just inside.
    ///
    /// Native: texts 14171-14175 (missionconversation; 6 empty); objectives (missionobjective) 1 "Locate Indra Caverns"
    /// 14207/14208, 2 "Find Major Diren" 14209/14210 ("he should be waiting for you just inside"), no counters; no objective
    /// conversation. Indicator names 261 (1) and 260 (2) (missionobjectiveindicatorlanguage). Names 9355 Odesha, 9727 Diren. The
    /// door: map_link 58, Ashen Desert (321.87, 318.96, 319.60) r6 to Indra Caverns (-40.69, 8.93, -421.65).
    ///
    /// Sourced: TaRapedia Indra Caverns (0.5): giver Surveyor Malya Odesha, reward from Major Darkua Diren "right inside the
    /// instance"; follow-up Threshold; 60,000 XP, 6,750 credits; one of Class IX Concussion Grenade, Basic Med Pack, Frag
    /// Grenade, EMP Bomb.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (the instance's band 42-44, D5); after Missing Scientists 1280 (Odesha's chain: she stands at the caverns'
    ///    door; TaRapedia names no requirement).
    ///  - 1 arriving in Indra Caverns (the whole map, a squad's copy too); 2 (opened by 1) walking within 10 m of Diren (the
    ///    client has no talk for 2; as Crucible 1569's "find"); the turn-in at Diren.
    ///  - The items: the lowest templates of the four kinds (client entityclass names): Concussion Grenade 111020 x3, Basic Med
    ///    Pack 45066 x2, Fragmentation Grenade 45448 x3, EMP Bomb 111234 x1 (the stack sizes as 1273's; no source); one to choose.
    ///  - Markers: 261 on the Ashen Desert side 18 m before the door (Ashen Desert), 260 on Diren (Indra Caverns, through
    ///    the scene's IndicatorMaps).
    /// </summary>
    public static class AshenIndraCavernsV1
    {
        public const uint MissionId = 1559;
        public const uint Level = 42;
        public const uint Odesha = AshenIndraPassNpcsV1.Odesha;
        public const uint Diren = AshenIndraFacilityNpcsV1.Diren;
        public const uint Caverns = AshenIndraFacilityNpcsV1.Caverns;
        public const uint Locate = 1, FindDiren = 2;
        public const uint CavernsArea = 1, DirenArea = 2;
        public const double DirenReach = 10;

        public const uint DoorIndicator = 261, DirenIndicator = 260;
        /// <summary>Before the Ashen Desert door (map_link 58's circle r6 at (321.87, 318.96, 319.60)): 18 m off its middle, on the client floor (ESTIMATE).</summary>
        public static readonly (double X, double Y, double Z) DoorMarkerAt = (305, 319.9, 326);
        public const double DoorMarkerRadius = 6, DirenMarkerRadius = 8;

        public const uint Experience = 60000;
        public const uint Credits = 6750;
        public const uint ConcussionGrenade = 111020;   // class 26270, Class IX Concussion Grenade (ESTIMATE the lowest)
        public const uint BasicMedPack = 45066;         // class 22549
        public const uint FragGrenade = 45448;          // class 22965
        public const uint EmpBomb = 111234;             // class 26544

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Indra Caverns", 14171, Odesha, Diren, Level);
            mission.Reward(Experience, Credits,
                new (uint, uint)[] { (ConcussionGrenade, 3), (BasicMedPack, 2), (FragGrenade, 3), (EmpBomb, 1) }, new (uint, uint)[0]);
            mission.Prerequisite(AshenMissingScientistsV1.MissionId);

            mission.MapArea(CavernsArea, Caverns);
            var diren = AshenIndraFacilityNpcsV1.DirenAt;
            mission.Area(DirenArea, Caverns, diren.X, diren.Y, diren.Z, DirenReach);

            mission.Objective(Locate, 14207, 14208, 0, MissionObjectiveState.Incomplete);
            mission.Objective(FindDiren, 14209, 14210, 1, MissionObjectiveState.Inactive);

            mission.Transition(Locate);
            mission.EnterArea(Locate, CavernsArea);
            mission.Action(Locate, 1, MissionActionKind.RevealObjective, targetObjective: FindDiren);
            mission.Action(Locate, 2, MissionActionKind.ActivateObjective, targetObjective: FindDiren, state: MissionObjectiveState.Incomplete);

            mission.Transition(FindDiren);
            mission.EnterArea(FindDiren, DirenArea);
            mission.Action(FindDiren, 1, MissionActionKind.GrantReward, reward: 1);

            mission.NamedIndicator(Locate, DoorIndicator, DoorMarkerAt.X, DoorMarkerAt.Y, DoorMarkerAt.Z, DoorMarkerRadius);
            mission.NamedIndicator(FindDiren, DirenIndicator, diren.X, diren.Y, diren.Z, DirenMarkerRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1559",
                "Native1 'Locate Indra Caverns' 14207/14208, 2 'Find Major Diren' 14209/14210, no counters; no objectiveconversation. " +
                "Texts 14171-14175. Indicators 261, 260. Names 9355 Odesha, 9727 Diren. map_link 58 AD (321.87,318.96,319.60) r6 -> IC " +
                "(-40.69,8.93,-421.65).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Indra_Caverns_(mission)",
                "Giver Surveyor Malya Odesha, reward from Major Darkua Diren right inside the instance; follow-up Threshold; 60000XP, " +
                "6750 credits; Class IX Concussion Grenade, Basic Med Pack, Frag Grenade, EMP Bomb.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenIndraCavernsV1",
                "Level42, after 1280 (estimated). 1 arriving on map 2055 (MapArea), 2 within 10m of Diren 790560 (-53,-395). Markers 261 " +
                "before the AD door, 260 on Diren (IC). Items 111020x3, 45066x2, 45448x3, 111234x1, one to choose.", 0.3);

            // The scene's map is Ashen Desert (the giver's), so the door's marker is sent there; Diren's goes to Indra Caverns
            // through the scene's IndicatorMaps (AshenMissionDataV1.Enable), as Crucible 1577.
            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = AshenMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
