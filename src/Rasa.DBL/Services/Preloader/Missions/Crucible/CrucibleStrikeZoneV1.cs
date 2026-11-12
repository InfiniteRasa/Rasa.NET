using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Strike Zone (native mission 1214), Ligo Crucible: Major Keplinger at Prometheus Outpost gives the player three GPS
    /// Beacons to place inside Khulago Base, to paint targets for an AFS air strike.
    ///
    /// Native: texts 9569-9573 (missionconversation): log "Place three GPS Beacons at the predesignated spots to call in AFS
    /// airstrikes. When you've placed all three, return to Major Keplinger at Prometheus Outpost.", offer "Okay, High Command's
    /// calling in an air strike. ... Take these GPS Beacons and place them in strategic locations inside the fort. Frohman's
    /// already done some recon to determine the best spots. I'm uploading the locations to your datapad. Once you place a
    /// beacon, get the hell away from it! Those bomb drops are going to come in hard and fast. ...", turn-in "It's like the
    /// friggin' Fourth of July out there! Great job. ...", reminder "High Command's waiting on those GPS signals. Get those
    /// beacons placed!"; objectives 1-3 "Place Beacon #1" .. "#3" 9665/9666, 9667/9668, 9669/9670 ("Place the first ..
    /// third GPS Beacon to call in an airstrike."), no counters (missionobjective). No objective conversation. Item class 25258
    /// MisCrucibleItemGPSbeacons "GPS Beacon" (template 50714). The beacon spot class 7847 UsableItemAccHumGpsBeacon "GPS
    /// Beacon" (an item acceptor, augmentation 43, use argument 3, a planted beacon's model in state 121; the one Mapping The
    /// Pools 1025 plants, PoolsMappingThePoolsV1).
    ///
    /// Sourced: TaRapedia Strike Zone: giver and reward giver Major Keplinger (Prometheus Outpost); requires Pump Up The Jam;
    /// 170,000 XP, 8,000 credits; one of ChiTech Stealth Armor Vest (icon 6), Luminar Graviton Armor Helmet (6), Teleract Hazmat
    /// Armor Helmet (4), Wellcare Motor Assist Armor Boots (4). TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The three beacons a mission item (AssignmentIssued, 3) given at the accept; three personal spots of 7847 inside
    ///    Khulago Base (no source gives Frohman's spots), any order, each takes one beacon, shows the planted beacon, closes and
    ///    completes its objective; all open from the accept.
    ///  - The spots: (403, 174.48, -168), (363, 174.48, -208), (383, 174.22, -268), on the open ground in the middle of the
    ///    base, on the navmesh and a client floor, 15 m or more from a wall, seen from 14 or more of 16 places 2 m round each,
    ///    no navmesh lava within 40 m, 26 m or more outside every hostile pool's circle, 650 m and more from every map link,
    ///    walked to from Confirmed Kills' north trench marker with no lava (probe 2026-11-07). A marker (r8) on each.
    ///  - The air strike after a beacon ("bomb drops"): not built. No mission action or scene intent calls an air strike:
    ///    the fire support strike (AbilityManager.FireSupport, effect 393) is a player ability's, TorcastraPrison's carpet bomb
    ///    is that prison's own code, and a falling bomb (RandomExplosionBaneFallingBomb, PlacedUsables) goes off at random on
    ///    its own clock for every player. A planted beacon completes its objective and nothing falls (deviation).
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 35, the nearest icon
    ///    number where the band has none (Stealth V04 Vest 40611, Graviton V04 Helmet 25099, Hazmat V03 Helmet 21380 (34-38),
    ///    Motor Assist V04 Boots 28614 (33-37)), one to choose.
    /// </summary>
    public static class CrucibleStrikeZoneV1
    {
        public const uint MissionId = 1214;
        public const uint Level = 35;
        public const uint Keplinger = CrucibleKardashAndPrometheusNpcsV1.Keplinger;
        public const uint Beacons = 3;
        public const uint PlantedScenario = 1;          // one scene result per beacon

        public const uint BeaconTemplate = 50714, BeaconClass = 25258;
        public const string BeaconKey = "gps-beacon";
        public const uint SpotClass = 7847;             // UsableItemAccHumGpsBeacon "GPS Beacon"
        public const uint SpotReadyState = 119, SpotPlantedState = 121;
        public const uint UseArgument = 3;
        public const double MarkerRadius = 8;

        public const uint Experience = 170000;
        public const uint Credits = 8000;
        public const uint StealthVest = 40611;          // Armor_T3_Stealth_V04_UNC_Vest_35_to_39
        public const uint GravitonHelmet = 25099;       // Armor_T3_Graviton_V04_UNC_Helmet_35_to_39
        public const uint HazmatHelmet = 21380;         // Armor_T2_Hazmat_V03_UNC_Helmet_34_to_38
        public const uint MotorAssistBoots = 28614;     // Armor_T1_MotorAssist_V04_UNC_Boots_33_to_37

        /// <summary>Beacons #1-#3 inside Khulago Base, on the navmesh.</summary>
        public static readonly ScenePosition[] Spots =
        {
            new(403f, 174.48f, -168f),
            new(363f, 174.48f, -208f),
            new(383f, 174.22f, -268f),
        };

        /// <summary>Objective of beacon #<paramref name="number"/> (1-3): the client's objective <paramref name="number"/>.</summary>
        public static uint Place(int number) => (uint)number;

        /// <summary>Name and body texts of beacon #<paramref name="number"/>: 9665/9666, 9667/9668, 9669/9670.</summary>
        public static (uint Name, uint Body) TextsOf(int number) => (9663 + 2 * (uint)number, 9664 + 2 * (uint)number);

        public static string SpotRole(int number) => $"gps-beacon-spot-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Strike Zone", 9569, Keplinger, Keplinger, Level);
            mission.Reward(Experience, Credits, new[] { StealthVest, GravitonHelmet, HazmatHelmet, MotorAssistBoots });
            mission.Prerequisite(CruciblePumpUpTheJamV1.MissionId);
            mission.Scenario(PlantedScenario, "A GPS Beacon placed", Beacons);

            for (var number = 1; number <= Beacons; number++)
            {
                var (name, body) = TextsOf(number);
                mission.Objective(Place(number), name, body, (uint)number - 1, MissionObjectiveState.Incomplete);
                mission.Transition(Place(number));
                mission.Progress(Place(number), MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: PlantedScenario);
                mission.Action(Place(number), 1, MissionActionKind.GrantReward, reward: 1);
                var spot = Spots[number - 1];
                mission.Indicator(Place(number), (uint)number, spot.X, spot.Y, spot.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1214",
                "Native1-3 'Place Beacon #1..#3' 9665/9666, 9667/9668, 9669/9670, no counters; no objectiveconversation. Texts " +
                "9569-9573 (place them inside the fort; bomb drops after a beacon). Item class25258 GPS Beacon (template50714); " +
                "spot class7847 UsableItemAccHumGpsBeacon (augmentation43, use3, planted in state121).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Strike_Zone",
                "Giver/reward giver Major Keplinger (Prometheus Outpost); requires Pump Up The Jam; 170000XP, 8000 credits; one of " +
                "ChiTech Stealth Armor Vest 6, Luminar Graviton Armor Helmet 6, Teleract Hazmat Armor Helmet 4, Wellcare Motor " +
                "Assist Armor Boots 4; 3 beacons. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleStrikeZoneV1",
                "Three beacons at the accept (a mission item); three personal spots 7847 inside Khulago Base (no source gives " +
                "them), any order, each takes one; markers r8. The air strike after a beacon is not built (no mission strike). " +
                "Items 40611, 25099, 21380, 28614 (lowest Uncommon of each kind and nearest icon, band of level 35), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The three beacons issued at the accept and the player's own three spots inside Khulago Base.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(BeaconKey, BeaconTemplate, MissionItemScope.AssignmentIssued, Beacons,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-gps-beacons", MissionId, BeaconKey, BeaconTemplate, Beacons) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= Beacons; number++)
            {
                var role = SpotRole(number);
                var sequence = (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, Spots[number - 1],
                    InitialObjectState: SpotReadyState, UseAction: new SceneObjectAction(MissionId, Place(number), sequence, UseArgument));
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

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
