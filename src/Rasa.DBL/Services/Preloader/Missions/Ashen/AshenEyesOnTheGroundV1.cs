using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Eyes on the Ground (native mission 1245), Ligo Ashen Desert: Captain Sera Karter at Shadow's Edge Post gives the player
    /// five Atta sensor beacons to plant in the Mauna Barrens, at the places on the map.
    ///
    /// Native: texts 9939-9943 (missionconversation): log "Travel to the Mauna Barrens and plant the 5 Mauna Sensor Beacons in
    /// the places indicated on your map. Report back to Captain Sera Karter once you have completed this task.", offer
    /// "Lieutenant Kuang has briefed me on the situation. ... the horde of Atta grows larger by the second ...", turn-in "Thank you
    /// again for your hard work. ...", reminder "The assigned locations for the Mauna Sensor Beacons should be on your map. The
    /// sensor beacons are augmented with some classified technology that should make them undetectable to the Atta."; objectives
    /// 1-5 "Sensor Beacon 1" .. "5" 9944/9945, 9946/9947, 9948/9949, 9950/9951, 9952/9953, no counters (missionobjective). No
    /// objective conversation. Item class 25411 Mis_Ligo_Ashen_Atta_Beacon "Atta Sensor Beacon" (template 97460); the planted
    /// object's name 71 LIGO_ASHEN_ATTA_BEACON "Atta Sensor Beacon" (usablenameoverride); the markers' names 155-159 "Atta
    /// Beacon Point 1" .. "5" (missionobjectiveindicatorlanguage). Name 9281 Karter.
    ///
    /// Sourced: TaRapedia Eyes on the Ground: giver and reward giver Captain Sera Karter (Shadow's Edge Post); requires Harmful
    /// Mutations; 90,000 XP, 6,150 credits; one of Teleract Mech Armor Boots (icon 5), Hellstrom Graviton Armor Gloves (1),
    /// Pulsar Hazmat Armor Boots (4), Pathogex Bio Armor Gloves (5); "The assigned locations should be outlined on your map."
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: after Harmful Mutations).
    ///  - The five beacons a mission item (AssignmentIssued, 5) given at the accept; five personal spots in the Mauna Barrens
    ///    (no source gives them), any order, each takes one beacon, shows the planted beacon, closes and completes its objective;
    ///    all open from the accept. The spot is the client's GPS beacon acceptor 7847 UsableItemAccHumGpsBeacon (augmentation 43,
    ///    use argument 3, planted in state 121; as the Crucible's Strike Zone and the Pools' Mapping The Pools), named "Atta
    ///    Sensor Beacon" by override 71: the client has no Atta beacon class of its own.
    ///  - The spots: round the Barrens about 110 m from the region's centre, one in each fifth of the circle, on the open
    ///    ground: on the navmesh and a client floor, 8 m or more from a wall, seen from 16 of 16 places 2 m round each (each on
    ///    a client floor no more than 0.8 m under or 0.6 m over the spot's), 24 m or
    ///    more outside every hostile pool's circle (the new Atta pools' too), walked to from the Shadow's Edge Post waypoint
    ///    (probe of 2026-11-09). A marker (r8) on each, keyed by the client's names 155-159 (QUEST-RULES).
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 41, the nearest icon
    ///    number where the band has none: Mech V04 Boots 33987, Graviton V02 Gloves 23698, Hazmat V03 Boots 21100 (39-43),
    ///    Bio V04 Gloves 46013; one to choose.
    /// </summary>
    public static class AshenEyesOnTheGroundV1
    {
        public const uint MissionId = 1245;
        public const uint Level = 41;
        public const uint Karter = AshenMaunaBarrensNpcsV1.Karter;
        public const uint Beacons = 5;
        public const uint PlantedScenario = 1;          // one scene result per beacon

        public const uint BeaconTemplate = 97460, BeaconClass = 25411;
        public const string BeaconKey = "atta-sensor-beacon";
        public const uint SpotClass = 7847;             // UsableItemAccHumGpsBeacon
        public const uint SpotName = 71;                // usablenameoverride LIGO_ASHEN_ATTA_BEACON "Atta Sensor Beacon"
        public const uint SpotReadyState = 119, SpotPlantedState = 121;
        public const uint UseArgument = 3;
        public const uint FirstPointName = 155;         // missionobjectiveindicatorlanguage "Atta Beacon Point 1" (to 159 "5")
        public const double MarkerRadius = 8;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint MechBoots = 33987;            // Armor_T3_Mech_V04_UNC_Boots_40_to_44
        public const uint GravitonGloves = 23698;       // Armor_T3_Graviton_V02_UNC_Gloves_40_to_44
        public const uint HazmatBoots = 21100;          // Armor_T2_Hazmat_V03_UNC_Boots_39_to_43
        public const uint BioGloves = 46013;            // Armor_T3_Bio_V04_UNC_Gloves_40_to_44

        /// <summary>Beacons 1-5 in the Mauna Barrens, on the navmesh.</summary>
        public static readonly ScenePosition[] Spots =
        {
            new(-431f, 304.07f, -203f),
            new(-554f, 304.23f, -230f),
            new(-596f, 303.04f, -83f),
            new(-476f, 328.91f, -17f),
            new(-338f, 293.53f, -80f),
        };

        /// <summary>Objective of beacon <paramref name="number"/> (1-5): the client's objective <paramref name="number"/>.</summary>
        public static uint Place(int number) => (uint)number;

        /// <summary>Name and body texts of beacon <paramref name="number"/>: 9944/9945 .. 9952/9953.</summary>
        public static (uint Name, uint Body) TextsOf(int number) => (9942 + 2 * (uint)number, 9943 + 2 * (uint)number);

        /// <summary>The client's marker name of beacon <paramref name="number"/>: 155-159.</summary>
        public static uint PointName(int number) => FirstPointName + (uint)number - 1;

        public static string SpotRole(int number) => $"atta-beacon-spot-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Eyes on the Ground", 9939, Karter, Karter, Level);
            mission.Reward(Experience, Credits, new[] { MechBoots, GravitonGloves, HazmatBoots, BioGloves });
            mission.Prerequisite(AshenHarmfulMutationsV1.MissionId);
            mission.Scenario(PlantedScenario, "An Atta Sensor Beacon planted", Beacons);

            for (var number = 1; number <= Beacons; number++)
            {
                var (name, body) = TextsOf(number);
                mission.Objective(Place(number), name, body, (uint)number - 1, MissionObjectiveState.Incomplete);
                mission.Transition(Place(number));
                mission.Progress(Place(number), MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: PlantedScenario);
                mission.Action(Place(number), 1, MissionActionKind.GrantReward, reward: 1);
                var spot = Spots[number - 1];
                mission.NamedIndicator(Place(number), PointName(number), spot.X, spot.Y, spot.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1245",
                "Native1-5 'Sensor Beacon 1..5' 9944/9945 .. 9952/9953, no counters; no objectiveconversation. Texts 9939-9943 (5 " +
                "Mauna Sensor Beacons at the places on the map). Item class25411 Atta Sensor Beacon (template97460); usable name " +
                "71 'Atta Sensor Beacon'; indicator names 155-159 'Atta Beacon Point 1..5'. Name 9281 Karter.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Eyes_on_the_Ground",
                "Giver/reward giver Captain Sera Karter (Shadow's Edge Post); requires Harmful Mutations; 90000XP, 6150 credits; " +
                "Teleract Mech Armor Boots 5, Hellstrom Graviton Armor Gloves 1, Pulsar Hazmat Armor Boots 4, Pathogex Bio Armor " +
                "Gloves 5; the places outlined on the map.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenEyesOnTheGroundV1",
                "Level41 estimated. Karter 790042 new by the Shadow's Edge dropship pad. Five beacons at the accept (a mission " +
                "item); five personal spots 7847 named 71 in the Mauna Barrens (no source gives them), any order, each takes one; " +
                "markers r8 keyed 155-159. Items 33987, 23698, 21100, 46013, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The five beacons issued at the accept and the player's own five spots in the Mauna Barrens.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(BeaconKey, BeaconTemplate, MissionItemScope.AssignmentIssued, Beacons,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-atta-beacons", MissionId, BeaconKey, BeaconTemplate, Beacons) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= Beacons; number++)
            {
                var role = SpotRole(number);
                var sequence = (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, Spots[number - 1],
                    InitialObjectState: SpotReadyState, UseAction: new SceneObjectAction(MissionId, Place(number), sequence, UseArgument),
                    NameOverrideId: SpotName);
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{role}", role, SpotPlantedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-placed", MissionId, BeaconKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, PlantedScenario, (uint)number) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
