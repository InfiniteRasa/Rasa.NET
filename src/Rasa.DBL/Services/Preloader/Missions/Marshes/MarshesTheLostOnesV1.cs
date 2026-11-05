using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Lost Ones (native mission 1076), Valverde Marshes and the Village Ruins (squad instance 1743): Lieutenant Morrison at
    /// Falcon Hold sends the player into P'reo Das to plant a signal spike, so that the comm links with the Forean villages come
    /// back; Headquarters hears the uplink by radio.
    ///
    /// Native: texts 6069-6072 and 7276 (missionconversation): title "The Lost Ones", log "Re-establish communication lines in
    /// Forean villages near P'reo Das.", offer "We've lost communication with the nearby Forean villages, P'reo Das and beyond
    /// ... place a signal spike", turn-in "Uplink established. Commencing scan of the area in 3.... 2.... 1."; objectives
    /// (missionobjective) 2 "Travel to P'reo Das to reestablish a communications link." 8349/8350 (the first), 1 "Plant a signal
    /// spike in P'reo Das to reestablish the communication link." 6081/7277; no counters, no objective conversation. Item AFS
    /// Signal Spike (class 22968 MisMarshesVillageRuinsBeacon, template 45451). Usable name 273 "AFS Communication Spike"
    /// (usablenameoverride MIS_MARSHES_COMMUNICATIONSPIKE).
    ///
    /// Sourced: TaRapedia The Lost Ones: giver Lieutenant Morrison (Falcon Hold), receiver Headquarters (radio; Morrison until
    /// 2008-01-02); 30,000 XP, 3,500 credits; one of 3 Class VII Advanced Med Pack, 3 Armor Charger, 6 Cryogenic Grenade, 6
    /// Incendiary Grenade; follow-ups Needle in a Bane Camp and The Choices We Make. TR MMO DB level 33. Ten Ton Hammer (Falcon
    /// Hold guide): the travel point at (-107, 61, -355) and the spike at (181, 55, -118).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's conflict 39):
    ///  - Bound to the squad's Village Ruins. One spike at the accept (taken at an abandon). 2: an area of 15 m at Ten Ton
    ///    Hammer's travel point inside P'reo Das (read as the instance's /loc, 32 m from the arrival); it opens 1 and sets down
    ///    a personal spike spot (class 7847, as Now Can You Hear Me?'s, the client's name 273) at Ten Ton Hammer's spike place
    ///    (on the navmesh at 181, 56.27, -118, east of P'reo Das); its use takes the spike and completes 1.
    ///  - The stacks the client's counts (3 and 6 as TaRapedia), one to choose. Markers at the Marshes door, the travel point and
    ///    the spot.
    /// </summary>
    public static class MarshesTheLostOnesV1
    {
        public const uint MissionId = 1076;
        public const uint Level = 33;
        public const uint Morrison = 510162;
        public const uint Ruins = MarshesPreoDasNpcsV1.MapContextId;
        public const uint Plant = 1, Travel = 2;
        public const uint TravelArea = 1;

        /// <summary>Scenarios: the spot set down (its sequence), the spike planted (the result).</summary>
        public const uint SpotReady = 1, Planted = 2;
        public const uint PlantedSequence = 21;

        public const uint SpikeTemplate = 45451, SpikeClass = 22968;
        public const string SpikeKey = "afs-signal-spike";
        public const uint SpotClass = MarshesNowCanYouHearMeV1.SpotClass;
        public const uint ReadyState = MarshesNowCanYouHearMeV1.ReadyState, PlacedState = MarshesNowCanYouHearMeV1.PlacedState;
        public const uint UseArgument = MarshesNowCanYouHearMeV1.UseArgument;
        public const uint SpotName = MarshesNowCanYouHearMeV1.SpotName;
        public const string SpotRole = "signal-spike-spot";

        /// <summary>The Marshes door to the Village Ruins (map_link 44).</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (755.08, 215.56, 727.67);
        public static readonly (double X, double Y, double Z) TravelAt = (-107, 60.17, -355);
        public const double TravelRadius = 15;
        public static readonly ScenePosition SpotAt = new(181f, 56.27f, -118f);
        public const double MarkerRadius = 8;

        public const uint Experience = 30000;
        public const uint Credits = 3500;
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 3;        // Class VII Advanced Med Pack
        public const uint ArmorCharger = 118898, ArmorChargers = 3;             // Class VII Armor Charger
        public const uint CryoGrenade = 111048, CryoGrenades = 6;               // Class VII Cryogenic Grenade
        public const uint IncendiaryGrenade = 111038, IncendiaryGrenades = 6;   // Class VII Incendiary Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Lost Ones", 6069, Morrison, null, Level);
            mission.RadioCompletion();
            mission.Reward(Experience, Credits,
                new[] { (AdvancedMedPack, AdvancedMedPacks), (ArmorCharger, ArmorChargers), (CryoGrenade, CryoGrenades),
                    (IncendiaryGrenade, IncendiaryGrenades) },
                new (uint, uint)[0]);
            mission.Scenario(SpotReady, "In P'reo Das: the spike's spot", 0);
            mission.Scenario(Planted, "The signal spike planted", 1);
            mission.Area(TravelArea, Ruins, TravelAt.X, TravelAt.Y, TravelAt.Z, TravelRadius);

            mission.Objective(Travel, 8349, 8350, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Plant, 6081, 7277, 1, MissionObjectiveState.Inactive);

            mission.Transition(Travel);
            mission.EnterArea(Travel, TravelArea);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Travel, 1, Plant);
            mission.Action(Travel, 3, MissionActionKind.StartScenario, scenario: SpotReady);

            mission.Transition(Plant);
            mission.Progress(Plant, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Planted);
            mission.Action(Plant, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Travel, 1, DoorAt.X, DoorAt.Y, DoorAt.Z, 10);
            mission.Indicator(Travel, 2, TravelAt.X, TravelAt.Y, TravelAt.Z, TravelRadius, Ruins);
            mission.Indicator(Plant, 3, SpotAt.X, SpotAt.Y, SpotAt.Z, MarkerRadius, Ruins);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1076",
                "Native2 'Travel to P'reo Das to reestablish a communications link.' 8349/8350 (first), 1 'Plant a signal spike in " +
                "P'reo Das' 6081/7277; no counters, no objectiveconversation. Texts 6069-6072, 7276. Item class22968 " +
                "MisMarshesVillageRuinsBeacon (template 45451). Usable name273 'AFS Communication Spike'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Lost_Ones",
                "Giver Lieutenant Morrison (Falcon Hold), receiver Headquarters (radio; Morrison until 2008-01-02); 30000XP, 3500 " +
                "credits; one of 3 Class VII Advanced Med Pack, 3 Armor Charger, 6 Cryogenic Grenade, 6 Incendiary Grenade; " +
                "follow-ups 893, 1078. TR MMO DB level33. Ten Ton Hammer: travel (-107,61,-355), spike (181,55,-118).", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesTheLostOnesV1",
                "Bound to the squad's Village Ruins (1743). One spike (45451) at accept; 2 an area r15 at (-107,60.17,-355); it " +
                "sets down a personal spot 7847 (arg3, name273) at (181,56.27,-118), which takes the spike (1). Radio turn-in. " +
                "Places read as instance /locs.", 0.3);

            mission.Enable(Scene());
        }

        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Ruins,      // taken at Morrison on the Marshes, played in the squad's Village Ruins
                MapContextId = Ruins,
                Items = new()
                {
                    new MissionItemBinding(SpikeKey, SpikeTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-spike", MissionId, SpikeKey, SpikeTemplate, 1) }
            };
            scene.Actors[SpotRole] = new SceneActorDefinition(SpotRole, SceneActorKind.Object, SpotClass, SpotAt,
                InitialObjectState: ReadyState, NameOverrideId: SpotName,
                UseAction: new SceneObjectAction(MissionId, Plant, PlantedSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[SpotReady] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-spot", SpotRole) } };
            scene.Sequences[PlantedSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("plant-spike", SpotRole, PlacedState),
                    new SetInteractionIntent("spot-done", SpotRole, false)
                },
                Character = { new ConsumeMissionItemIntent("spike-planted", MissionId, SpikeKey, 1, MissionItemScope.AssignmentIssued) },
                Signals = { new SceneMissionSignal(MissionId, Planted, 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
