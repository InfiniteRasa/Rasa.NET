using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Digging for the Truth (native mission 1447), Indra Caverns (squad instance 2055): Sergeant Jonas Drof, in the Brann
    /// facility's Control Room, has the player put the power supply into the Power Supply Box behind him: the power back, the
    /// teleporters take the player to the facility's rear exit; through the rear caves, the Bane's point of entry and their
    /// tunnel to the Bane mining operation, where six Articulated Drills are destroyed.
    ///
    /// Native: texts 12708-12712 (missionconversation; 12710 the offer: "keep the power supply", 12712 empty); objectives
    /// (missionobjective) 1 "Destroy Mining Equipment" 12713/12714 counter 12715 "Articulated Drill", 2 "Find the Rear Cave
    /// Entrance" 12724/12725, 3 "Explore the Tunnel" 12878/12879, 5 "Restore Power and Use the Teleporters" 12882/12883 ("the
    /// console just behind Sergeant Drof", "unlock the door to the rear exit", the teleporters to the back of the facility), 6-11
    /// "Articulated Drill (1 of 6)" .. "(6 of 6)" 13708/13709 .. 13718/13719; no objective conversation. Item: class 25631 Power
    /// Supply (97519). Usable name 144 Power Supply Box. Indicator names 134 Bane Mining Drills, 218 Power Supply Box, 219 Rear
    /// Cave Entrance, 220 Tunnel Exit, 221 Articulated Drill.
    ///
    /// Sourced: TaRapedia Digging for the Truth (0.5): Drof gives; Headquarters (Radio) rewards; requires Vindication; follow-up
    /// Meet Sergeant Drof; 110,000 XP, 8,800 credits; one of Teleract Torqueshell Rifle, Vitalius Sonic Disperser, Vextronics
    /// Electric Rifle, Virulent Injector Gun.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; taken at set 15's Drof in the Control Room; turned in by radio. The Power Supply at the accept (each member his
    ///    own, AssignmentIssued: 1395 takes its own at its turn-in).
    ///  - 5 counts hidden 12 and 13 (the scene's aggregation). 12: the player's own Power Supply Box (UsableTwoStateBaneConsole
    ///    SwitchV01 7468, ready 55, set 56, argument 1, name 144) behind Drof, locked to the power supply: its use completes 12 (a
    ///    scene result; every member on 1447 here is credited), 12 takes each member's power supply and turns his own power on:
    ///    his rear exit door (UsableDoorBrannCorridor04mV01 10749 in the rear exit's jamb, closed 31, open 91, not usable) opens
    ///    and his two teleporters (UsableTwoStateBrannTeleporter 24805, off 55, on 56) in the Control Room come on. 13: the area
    ///    just outside the rear exit entered (a member's own walk); a teleporter's use takes him to just short of it (a move on
    ///    this map; an area is entered by walking into it), and through his open door he can walk there too.
    ///  - 2 the area of the Bane's point of entry in the rear caves, then 3 the area of their tunnel's exit in the mining area
    ///    (each opened by the one before).
    ///  - 6-11 the player's own Articulated Drills (UsableInertDestBaneArticulatedDrillV01 6318, intact 110, destroyed 2,
    ///    targetable, 500 hit points) in the Bane mining operation; each destroyed completes its objective; 1 counts them
    ///    (counter 0); they can be shot from the accept on. The reward with the last.
    ///  - Markers: 218 the box, 219 the cave entrance, 220 the tunnel exit, 221 each drill, 134 the mining operation (1), the box's
    ///    for 5.
    /// </summary>
    public static class AshenDiggingForTheTruthV1
    {
        public const uint MissionId = 1447;
        public const uint Level = 43;
        public const uint Caverns = AshenIndraFacilityNpcsV1.Caverns;
        public const uint Drof = AshenIndraFacilityNpcsV1.Drof;

        public const uint DestroyDrills = 1, RearCaveEntrance = 2, ExploreTunnel = 3, RestorePower = 5, Drill1 = 6, Drills = 6;
        public const uint PowerOn = 12, OutsideTheRearExit = 13, Opener = 14;
        public const uint RearArea = 1, EntranceArea = 2, TunnelExitArea = 3;

        public const uint PowerSupplyTemplate = 97519, PowerSupplyClass = 25631;
        public const string PowerSupplyKey = "drofs-power-supply";

        public const uint SwitchClass = 7468;           // UsableTwoStateBaneConsoleSwitchV01
        public const uint SwitchReady = 55, SwitchSet = 56;
        public const uint BoxName = 144;
        public const uint TeleporterClass = 24805;      // UsableTwoStateBrannTeleporter
        public const uint TeleporterOff = 55, TeleporterOn = 56;
        public const uint DoorClass = 10749;            // UsableDoorBrannCorridor04mV01
        public const uint DoorClosed = 31, DoorOpen = 91;
        public const uint DrillClass = 6318;            // UsableInertDestBaneArticulatedDrillV01
        public const uint IntactState = 110, DestroyedState = 2;
        public const uint DrillHitPoints = 500;
        public const uint UseArgument = 1;              // usabledata lookup[7468]: (1, 0, 0, 0, 0.0); [24805]: None

        public const string BoxRole = "indra-power-supply-box", DoorRole = "indra-rear-exit-door";
        public static string TeleporterRole(int number) => $"indra-teleporter-{number}";
        public static string DrillRole(int number) => $"gotha-drill-{number}";
        public static uint DrillObjective(int number) => Drill1 + (uint)number - 1;

        /// <summary>Scenarios: the box used (12), the power on (12 done), the drills (one step each).</summary>
        public const uint BoxUsed = 1, PowerComesOn = 2, DrillDown = 3;
        public const uint BoxSequence = 11, TeleportSequence = 12, DrillSequenceFrom = 21;

        public const uint DrillsIndicator = 134, BoxIndicator = 218, EntranceIndicator = 219, TunnelExitIndicator = 220, DrillIndicator = 221;
        public const double ObjectMarkerRadius = 6, AreaMarkerRadius = 10, MiningMarkerRadius = 40;   // ESTIMATE

        /// <summary>The Power Supply Box, on the console just behind Drof in the Control Room.</summary>
        public static readonly ScenePosition BoxAt = new(-9f, 0.5f, -221f);
        /// <summary>The two teleporters, in the Control Room.</summary>
        public static readonly ScenePosition[] TeleportersAt = { new(-12f, 0.5f, -215f), new(-1f, 0.5f, -227f) };
        /// <summary>The rear exit's door: the jamb ArchBrannCorridorJamb014mV01 (140, 16, -228), yaw 0, 2 m along its normal.</summary>
        public static readonly ScenePosition DoorAt = new(140f, 16.5f, -226f);
        public const double DoorOrientation = 0;
        /// <summary>
        /// Where a teleporter takes the player: just outside the rear exit, short of the rear area (RearAt): an area is entered by
        /// walking into it, not by arriving in it (MissionAreaService), so the first steps toward the caves enter it.
        /// </summary>
        public static readonly ScenePosition RearArrival = new(140f, 16.02f, -205f);
        public const double RearArrivalYaw = 0;
        public static readonly (double X, double Y, double Z) RearAt = (131, 15.93, -191);
        public const double RearRadius = 6;   // ESTIMATE
        public static readonly (double X, double Y, double Z) EntranceAt = (4, 46.18, -94);
        public const double EntranceRadius = 10;   // ESTIMATE
        public static readonly (double X, double Y, double Z) TunnelExitAt = (-325, 64.12, 24);
        public const double TunnelExitRadius = 10;   // ESTIMATE
        public static readonly (double X, double Y, double Z) MiningAt = (-311, 85.86, 245);

        /// <summary>The six Articulated Drills in the Bane mining operation.</summary>
        public static readonly ScenePosition[] DrillsAt =
        {
            new(-327f, 90.31f, 239f), new(-326f, 90.43f, 259f), new(-338f, 90.43f, 270f),
            new(-311f, 85.86f, 245f), new(-312f, 85.86f, 227f), new(-322f, 85.86f, 211f)
        };

        public const uint Experience = 110000;
        public const uint Credits = 8800;
        // ESTIMATE: the templates of TaRapedia's four names chosen by the level band (TaRapedia gives no levels).
        public const uint TorqueshellRifle = 12111;     // Weapon_Avatar_Torqueshell_Rifle_Physical_UNC_43_to_47
        public const uint SonicDisperser = 12071;       // Weapon_Avatar_PropellantGun_Sonic_UNC_42_to_46
        public const uint ElectricRifle = 11953;        // Weapon_Avatar_Rifle_Electric_UNC_41_to_45
        public const uint VirulentInjector = 12155;     // Weapon_Avatar_InjectionGun_Virulent_UNC_42_to_46

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Digging for the Truth", 12708, Drof, null, Level);
            mission.Reward(Experience, Credits, new[] { TorqueshellRifle, SonicDisperser, ElectricRifle, VirulentInjector });
            mission.Prerequisite(AshenVindicationV1.MissionId);
            mission.RadioCompletion();
            mission.Scenario(BoxUsed, "The Power Supply Box used with the power supply", 1);
            mission.Scenario(PowerComesOn, "The power on (12 done)", 0);
            mission.Scenario(DrillDown, "An Articulated Drill destroyed", Drills);

            mission.Objective(DestroyDrills, 12713, 12714, 0, MissionObjectiveState.Incomplete, counterTextId: 12715);
            mission.Objective(RearCaveEntrance, 12724, 12725, 2, MissionObjectiveState.Inactive);
            mission.Objective(ExploreTunnel, 12878, 12879, 3, MissionObjectiveState.Inactive);
            mission.Objective(RestorePower, 12882, 12883, 1, MissionObjectiveState.Incomplete);
            for (var number = 1; number <= Drills; number++)
            {
                var objective = DrillObjective(number);
                mission.Objective(objective, 13706 + 2 * (uint)number, 13707 + 2 * (uint)number, 3 + (uint)number, MissionObjectiveState.Incomplete);
            }
            mission.Objective(PowerOn, 12882, 12883, 10, MissionObjectiveState.Incomplete, required: false);             // hidden
            mission.Objective(OutsideTheRearExit, 12882, 12883, 11, MissionObjectiveState.Incomplete, required: false);  // hidden
            mission.Objective(Opener, 12882, 12883, 12, MissionObjectiveState.Incomplete, required: false);              // hidden

            mission.Area(RearArea, Caverns, RearAt.X, RearAt.Y, RearAt.Z, RearRadius);
            mission.Area(EntranceArea, Caverns, EntranceAt.X, EntranceAt.Y, EntranceAt.Z, EntranceRadius);
            mission.Area(TunnelExitArea, Caverns, TunnelExitAt.X, TunnelExitAt.Y, TunnelExitAt.Z, TunnelExitRadius);

            // 12, hidden: the box used (its event): each member whose 12 completes spends his own power supply, his power comes on;
            // a mate away at the use turns his own on later with his own power supply.
            mission.Transition(PowerOn);
            mission.Progress(PowerOn, MissionProgressEventKind.ScenarioEvent, 1, null, counter: BoxUsed);
            mission.Action(PowerOn, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("the-box-takes-the-power-supply", MissionId, PowerSupplyKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(PowerOn, 2, MissionActionKind.StartScenario, scenario: PowerComesOn);

            // 13, hidden: just outside the rear exit (a teleporter's move or the walk through the open door).
            mission.Transition(OutsideTheRearExit);
            mission.EnterArea(OutsideTheRearExit, RearArea);

            // 14, hidden: 5 (12 and 13 done) opens 2.
            mission.Transition(Opener);
            mission.ObjectiveState(Opener, RestorePower, MissionObjectiveState.Completed);
            mission.Action(Opener, 1, MissionActionKind.RevealObjective, targetObjective: RearCaveEntrance);
            mission.Action(Opener, 2, MissionActionKind.ActivateObjective, targetObjective: RearCaveEntrance, state: MissionObjectiveState.Incomplete);

            mission.Transition(RearCaveEntrance);
            mission.EnterArea(RearCaveEntrance, EntranceArea);
            mission.Action(RearCaveEntrance, 1, MissionActionKind.RevealObjective, targetObjective: ExploreTunnel);
            mission.Action(RearCaveEntrance, 2, MissionActionKind.ActivateObjective, targetObjective: ExploreTunnel, state: MissionObjectiveState.Incomplete);

            mission.Transition(ExploreTunnel);
            mission.EnterArea(ExploreTunnel, TunnelExitArea);
            mission.Action(ExploreTunnel, 1, MissionActionKind.GrantReward, reward: 1);

            for (var number = 1; number <= Drills; number++)
            {
                var objective = DrillObjective(number);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: DrillDown);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = DrillsAt[number - 1];
                mission.NamedIndicator(objective, DrillIndicator, at.X, at.Y, at.Z, ObjectMarkerRadius, Caverns);
            }

            mission.NamedIndicator(DestroyDrills, DrillsIndicator, MiningAt.X, MiningAt.Y, MiningAt.Z, MiningMarkerRadius, Caverns);
            mission.NamedIndicator(RestorePower, BoxIndicator, BoxAt.X, BoxAt.Y, BoxAt.Z, ObjectMarkerRadius, Caverns);
            mission.NamedIndicator(RearCaveEntrance, EntranceIndicator, EntranceAt.X, EntranceAt.Y, EntranceAt.Z, EntranceRadius, Caverns);
            mission.NamedIndicator(ExploreTunnel, TunnelExitIndicator, TunnelExitAt.X, TunnelExitAt.Y, TunnelExitAt.Z, TunnelExitRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1447",
                "Native1 'Destroy Mining Equipment' 12713/12714 c12715, 2 'Find the Rear Cave Entrance' 12724/12725, 3 'Explore the " +
                "Tunnel' 12878/12879, 5 'Restore Power and Use the Teleporters' 12882/12883, 6-11 'Articulated Drill (1 of 6)..(6 of " +
                "6)' 13708/13709..13718/13719; no objectiveconversation. Texts 12708-12712. Item Power Supply 25631 (97519). Usable " +
                "name 144. Indicators 134, 218-221.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Digging_for_the_Truth",
                "Sergeant Jonas Drof gives, Headquarters (Radio) rewards; requires Vindication; follow-up Meet Sergeant Drof; 110000XP, " +
                "8800 credits; Teleract Torqueshell Rifle, Vitalius Sonic Disperser, Vextronics Electric Rifle, Virulent Injector Gun.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenDiggingForTheTruthV1",
                "Level43; bound to the squad's copy; radio completion. Power supply at the accept. 5 counts hidden 12 (the personal " +
                "box 7468 (name144) behind Drof, locked to 97519: 12 takes each member's power supply, opens his rear exit door 10749 " +
                "and turns on his two Control Room teleporters 24805) and 13 (the area outside the rear exit; a teleporter takes him " +
                "there). 2 and 3 areas (the Bane's point of entry, the tunnel's exit). 6-11 six personal drills 6318 (500 HP) in the " +
                "mining operation; 1 counts them. Items 12111, 12071, 11953, 12155.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The power supply, the player's own box, door, teleporters and drills, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Caverns,    // taken at Drof in the squad's copy
                MapContextId = Caverns,
                HiddenObjectiveIds = new() { PowerOn, OutsideTheRearExit, Opener },
                ObjectiveAggregations = new()
                {
                    [RestorePower] = new MissionObjectiveAggregation(new[] { PowerOn, OutsideTheRearExit }, 2),
                    [DestroyDrills] = new MissionObjectiveAggregation(Enumerable.Range(1, (int)Drills).Select(DrillObjective).ToArray(), Drills, 0)
                },
                Items = new()
                {
                    new MissionItemBinding(PowerSupplyKey, PowerSupplyTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("drof-gives-the-power-supply", MissionId, PowerSupplyKey, PowerSupplyTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            scene.Actors[BoxRole] = new SceneActorDefinition(BoxRole, SceneActorKind.Object, SwitchClass, BoxAt,
                InitialObjectState: SwitchReady, NameOverrideId: BoxName,
                UseAction: new SceneObjectAction(MissionId, PowerOn, BoxSequence, UseArgument), KeyItemTemplateId: PowerSupplyTemplate);
            scene.Actors[DoorRole] = new SceneActorDefinition(DoorRole, SceneActorKind.Object, DoorClass, DoorAt, Orientation: DoorOrientation,
                InitiallyInteractable: false, InitialObjectState: DoorClosed);
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{BoxRole}", BoxRole));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{DoorRole}", DoorRole));
            var powerOn = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("box-set", BoxRole, SwitchSet),
                    new SetInteractionIntent("box-done", BoxRole, false),
                    new TransitionObjectStateIntent("rear-door-opens", DoorRole, DoorOpen)
                }
            };
            for (var number = 1; number <= TeleportersAt.Length; number++)
            {
                var role = TeleporterRole(number);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, TeleporterClass, TeleportersAt[number - 1],
                    InitiallyInteractable: false, InitialObjectState: TeleporterOff,
                    UseAction: new SceneObjectAction(MissionId, RestorePower, TeleportSequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                powerOn.World.Add(new TransitionObjectStateIntent($"{role}-on", role, TeleporterOn));
                powerOn.World.Add(new SetInteractionIntent($"{role}-usable", role, true));
            }
            // The box used with the power supply: the event of 12 (12 takes each member's power supply and turns his power on).
            scene.Sequences[BoxSequence] = new SceneSequenceDefinition
            {
                // Only the event: the box is set in PowerComesOn, after 12 took the supply (the set 15 door's order).
                Signals = { new SceneMissionSignal(MissionId, BoxUsed, 1) }
            };
            scene.Sequences[PowerComesOn] = powerOn;
            scene.Sequences[TeleportSequence] = new SceneSequenceDefinition
            {
                World = { new TransferIntent("teleporter-to-the-rear-exit", Caverns, RearArrival, RearArrivalYaw) }
            };

            for (var number = 1; number <= Drills; number++)
            {
                var role = DrillRole(number);
                var sequence = DrillSequenceFrom + (uint)number - 1;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, DrillClass, DrillsAt[number - 1],
                    InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, DrillObjective(number), sequence, DrillHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new TransitionObjectStateIntent($"{role}-destroyed", role, DestroyedState) },
                    Signals = { new SceneMissionSignal(MissionId, DrillDown, (uint)number) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
