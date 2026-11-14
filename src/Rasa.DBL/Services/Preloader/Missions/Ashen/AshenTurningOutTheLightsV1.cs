using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Turning Out the Lights (native mission 1354), Ligo Ashen Desert: Lieutenant Gabriel Infanger at Boneyard Outpost gives the
    /// player explosives to blow up the three power generators of the Bane Conscription Base, which draw the energy of the
    /// resistors at Indra Pass.
    ///
    /// Native: texts 11133-11137 (missionconversation): log "Lieutenant Infanger wants you to infiltrate the Bane Conscription
    /// Base and destroy the three power generators being used to collect electrical energy from Indra Pass and power the base.
    /// ...", offer "There are three power generators outside of the Bane Conscription Facility. ... Take these explosives, get
    /// in there and blow those generators. Be careful though; the last place you want to be when those generators explode is
    /// nearby.", turn-in "Thanks, soldier. ...", reminder "There are three generators just west of the control center of the
    /// Bane base. ..."; objectives (missionobjective) 1-3 "Destroy Power Generator 1" .. "3" 11333/11334, 11335/11336,
    /// 11337/11338, no counters. No objective conversation, no marker names. Item class 26441 Consc_Power_Bomb
    /// "Electromagnetic Charged Explosive" (template 111184, stack 6). Usable name 245 POWER_GENERATOR "Power Generator"
    /// (usablenameoverride). Name 9304 Infanger.
    ///
    /// Sourced: TaRapedia Turning Out the Lights: giver and reward giver Lieutenant Gabriel Infanger (Boneyard Outpost); requires
    /// Fallen Soldiers and Xanxless Job; 100,000 XP, 6,450 credits; one of Voltrox Bio Armor Legs (icon 3), Dynamo Stealth
    /// Armor Helmet (2), Olympia Hazmat Armor Vest (2), CryoGen Motor Assist Armor Legs (9). TaRapedia Overseer Naxveik:
    /// "Northwest of the Bane Conscription Garrison CP, at /loc 300, 20. Inside the compound with three generators." TaRapedia
    /// updates 2007-08-21 lists the explosives of 1352 as not given at the accept; here 1354's are given too.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: the zone band, after 1262 and 1263).
    ///  - Three explosives (a mission item, AssignmentIssued, 3) at the accept, taken back at an abandon. Three personal Bane
    ///    generators (20594 UsableInertDestBaneGeneratorSmallV01, named 245 "Power Generator"; the client places none and has
    ///    no Ashen generator class: the Bane outpost generator 3857 is 17 by 27 m and the plateau holds three of it only side
    ///    by side) in the compound on the plateau west of the Garrison ("just west of the control center"), round Overseer
    ///    Naxveik (AshenConscriptionBaseNpcsV1), each on flat navmesh ground and a client floor; beside each (6 m west: the
    ///    model reaches 1.61 m from its point, so the device stands 4.4 m outside it whichever way it is turned) a personal
    ///    Detonation Device (7334, use argument 1, 119 to 121) that takes one explosive and blows its generator (110 to 2, a
    ///    destruction no hit reaches, as AshenDeathFromAboveV1's launchers: class 20594's target flag is 1, so the client lets
    ///    a player aim at it, but it is not interactable and the server drops a hit on a disabled object), completing that
    ///    generator's objective once. The explosion's damage (the offer's warning) is not built (triage set 5).
    ///  - A marker r8 on each generator.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 41, the nearest icon where
    ///    the band has none: Bio V03 Legs 19802 (40-44), Stealth V02 Helmet 39081 (40-44), Hazmat V02 Vest 20951 (39-43),
    ///    Motor Assist V07 Legs 31394 (38-42; no V08 or V09 Uncommon legs); one to choose.
    /// </summary>
    public static class AshenTurningOutTheLightsV1
    {
        public const uint MissionId = 1354;
        public const uint Level = 41;
        public const uint Infanger = AshenBoneyardNpcsV1.Infanger;
        public const uint Generators = 3;
        public const uint Blown = 1;                    // the scenario of the generators: one scene result per generator
        public const double MarkerRadius = 8;

        public const uint ExplosiveTemplate = 111184, ExplosiveClass = 26441;
        public const string ExplosiveKey = "electromagnetic-charged-explosive";
        public const uint GeneratorClass = 20594;       // UsableInertDestBaneGeneratorSmallV01
        public const uint GeneratorName = 245;          // usablenameoverride POWER_GENERATOR "Power Generator"
        public const uint IntactState = AshenDeathFromAboveV1.IntactState, DestroyedState = AshenDeathFromAboveV1.DestroyedState;
        public const uint GeneratorHitPoints = AshenDeathFromAboveV1.LauncherHitPoints;
        public const uint DeviceClass = AshenDeathFromAboveV1.DeviceClass;
        public const uint DeviceReadyState = AshenDeathFromAboveV1.DeviceReadyState, DevicePlantedState = AshenDeathFromAboveV1.DevicePlantedState;
        public const uint UseArgument = AshenDeathFromAboveV1.UseArgument;

        /// <summary>Power Generators 1-3 on the compound's plateau, north to south.</summary>
        public static readonly ScenePosition[] GeneratorsAt =
        {
            new(318f, 296.2f, -14f), new(326f, 296.7f, -34f), new(314f, 296.3f, -52f)
        };

        /// <summary>Each generator's Detonation Device, 6 m west of it (probe 2026-11-09: navmesh ground within 0.5 m of the client floor).</summary>
        public static readonly ScenePosition[] DevicesAt =
        {
            new(312f, 296.38f, -14f), new(320f, 296.45f, -34f), new(308f, 296.3f, -52f)
        };

        public const uint Experience = 100000;
        public const uint Credits = 6450;
        public const uint BioLegs = 19802;              // Armor_T3_Bio_V03_UNC_Legs_40_to_44
        public const uint StealthHelmet = 39081;        // Armor_T3_Stealth_V02_UNC_Helmet_40_to_44
        public const uint HazmatVest = 20951;           // Armor_T2_Hazmat_V02_UNC_Vest_39_to_43
        public const uint MotorAssistLegs = 31394;      // Armor_T1_MotorAssist_V07_UNC_Legs_38_to_42

        /// <summary>The objective of Power Generator <paramref name="number"/> (1-3): the client's objective of the same number.</summary>
        public static uint Destroy(int number) => (uint)number;

        public static string GeneratorRole(int number) => $"power-generator-{number}";
        public static string DeviceRole(int number) => $"power-generator-{number}-detonation-device";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Turning Out the Lights", 11133, Infanger, Infanger, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, StealthHelmet, HazmatVest, MotorAssistLegs });
            mission.Prerequisite(AshenFallenSoldiersV1.MissionId, 1);
            mission.Prerequisite(AshenXanxlessJobV1.MissionId, 2);
            mission.Scenario(Blown, "A power generator blown up", Generators);

            for (var number = 1; number <= Generators; number++)
            {
                var objective = Destroy(number);
                mission.Objective(objective, 11331 + 2 * (uint)number, 11332 + 2 * (uint)number, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: Blown);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = GeneratorsAt[number - 1];
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1354",
                "Native1-3 'Destroy Power Generator 1..3' 11333/11334 .. 11337/11338, no counters; no objectiveconversation. " +
                "Texts 11133-11137 (three generators west of the control center; take these explosives). Item class26441 " +
                "Electromagnetic Charged Explosive (template111184). Usable name 245 'Power Generator'. Name 9304 Infanger.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Turning_Out_the_Lights",
                "Giver/reward giver Lieutenant Gabriel Infanger (Boneyard Outpost); requires Fallen Soldiers, Xanxless Job; " +
                "100000XP, 6450 credits; Voltrox Bio Armor Legs 3, Dynamo Stealth Armor Helmet 2, Olympia Hazmat Armor Vest 2, " +
                "CryoGen Motor Assist Armor Legs 9. Overseer Naxveik: the compound with three generators, /loc 300, 20.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenTurningOutTheLightsV1",
                "Level41 estimated. Three explosives at the accept; three personal generators 20594 named 245 on the compound's " +
                "plateau round Naxveik, a Detonation Device 7334 (arg1, 119->121) beside each takes one and blows it (110->2). " +
                "No explosion damage. Markers r8. Items 19802, 39081, 20951, 31394 (lowest Uncommon, nearest icon, band of " +
                "level 41), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The three explosives issued at the accept and the player's own three generators, each with its Detonation Device.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ExplosiveKey, ExplosiveTemplate, MissionItemScope.AssignmentIssued, Generators,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-electromagnetic-charged-explosives", MissionId, ExplosiveKey, ExplosiveTemplate, Generators) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= Generators; number++)
            {
                var generator = GeneratorRole(number);
                var device = DeviceRole(number);
                var sequence = (uint)number;
                var objective = Destroy(number);
                scene.Actors[generator] = new SceneActorDefinition(generator, SceneActorKind.Object, GeneratorClass, GeneratorsAt[number - 1],
                    Orientation: number * 1.0471975512, InitiallyInteractable: false, InitialObjectState: IntactState, NameOverrideId: GeneratorName,
                    Destruction: new SceneObjectDestruction(MissionId, objective, sequence, GeneratorHitPoints, DestroyedState));
                scene.Actors[device] = new SceneActorDefinition(device, SceneActorKind.Object, DeviceClass, DevicesAt[number - 1],
                    InitialObjectState: DeviceReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{generator}", generator));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{device}", device));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{device}", device, DevicePlantedState),
                        new SetInteractionIntent($"close-{device}", device, false),
                        new TransitionObjectStateIntent($"blow-{generator}", generator, DestroyedState)
                    },
                    Character = { new ConsumeMissionItemIntent($"{device}-explosive", MissionId, ExplosiveKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, Blown, sequence) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
