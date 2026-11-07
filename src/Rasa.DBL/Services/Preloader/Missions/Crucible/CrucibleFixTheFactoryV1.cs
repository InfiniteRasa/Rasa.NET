using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Fix the Factory (native mission 1522), the Chaukas Robotics Facility (squad instance 2146): Cipher Specialist Brian
    /// has the player restart the factory's machine and then turn the ore processor back on, to get the factory making
    /// friendly Warden Bots.
    ///
    /// Native: texts 13586-13590 (missionconversation); objectives (missionobjective) 1 "Restart the Machine" 13939/13940, 2
    /// "Turn on the Ore Processor" 13941/13942 ("The Ore Processing machine turned off, get to it quick..."); no counters, no
    /// objective conversation. Names 190 "Factory Reset Switch" and 191 "Ore Machine Override Switch" (usablenameoverride);
    /// no table ties them to a class. Regions (client statics): Robotics Assembly (88, 6, -120), Ore Processing (-153.5, 7,
    /// -80).
    ///
    /// Sourced: TaRapedia Fix the Factory: giver and reward giver Brian; requires Control the Brain; follow-up Factory
    /// Fortification; 85,000 XP, 8,000 credits; Modification: Armor Absorption, Regen Armor Bonus, Regen Health Bonus, Regen
    /// Power Bonus; "get the factory here making us some friendly Warden Bots" (cache, 0.5). TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The class of both switches: 26578 UsableTwoStateWardenbotGenericSwitchV01 (usabledata argument 1, states 55 and
    ///    56). A candidate for the ore switch: 25897 UsableTwoStateWardenbotOreMachineV01 (its target_flag 0: not usable as
    ///    the client has it).
    ///  - Taken at Brian in the squad's Chaukas, the scene bound to it. Two personal switches: the Factory Reset Switch at the
    ///    Robotics Assembly room's south entrance (88, 17.36, -176) (the room's inside is a navmesh island apart from the
    ///    arrival's, upstream N14; the entrance strip is joined; the north strip lies inside Claviger pool 531133's circle),
    ///    the Ore Machine Override Switch on the north side of the Ore Processing room's ring (-146, 16.68, -55). Both
    ///    ReachClearance and more outside every Claviger pool's spawn reach. Each used once (55 to 56), a scenario event; 1
    ///    opens 2, and a use of the ore switch before that is refused (2 not open).
    ///  - The items: "Modification" read as the grade [4] modules (as Rehabilitation at Gunpoint 1402): Armor Module: Armor
    ///    Absorption [4] 122951, Armor Module: Regen Bonus [4] 122986, Armor Module: Health Regen Bonus [4] 123001, Armor
    ///    Module: Power Regen Bonus [4] 123006 (client itemtemplatelanguage). One to choose.
    ///  - Markers on each switch.
    /// </summary>
    public static class CrucibleFixTheFactoryV1
    {
        public const uint MissionId = 1522;
        public const uint Level = 35;
        public const uint Brian = CrucibleChaukasNpcsV1.Brian;
        public const uint MapContextId = CrucibleChaukasNpcsV1.Chaukas;
        public const uint RestartMachine = 1, OreProcessor = 2;
        public const double SwitchRadius = 3;

        /// <summary>Scenarios: the factory switch thrown, the ore switch thrown.</summary>
        public const uint FactoryThrown = 1, OreThrown = 2;
        public const uint FactorySequence = 11, OreSequence = 12;
        public const string FactoryRole = "factory-reset-switch", OreRole = "ore-machine-override-switch";

        public const uint SwitchClass = 26578;          // UsableTwoStateWardenbotGenericSwitchV01
        public const uint FactoryName = 190, OreName = 191;
        public const uint UseArgument = 1;              // usabledata lookup[26578]: [0] None
        public const uint ReadyState = 55, ThrownState = 56;
        public static readonly ScenePosition FactorySwitchAt = new(88f, 17.36f, -176f);
        public static readonly ScenePosition OreSwitchAt = new(-146f, 16.68f, -55f);

        public const uint Experience = 85000;
        public const uint Credits = 8000;
        public const uint ArmorAbsorption = 122951;     // Armor Module: Armor Absorption [4]
        public const uint RegenBonus = 122986;          // Armor Module: Regen Bonus [4]
        public const uint HealthRegenBonus = 123001;    // Armor Module: Health Regen Bonus [4]
        public const uint PowerRegenBonus = 123006;     // Armor Module: Power Regen Bonus [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Fix the Factory", 13586, Brian, Brian, Level);
            mission.Reward(Experience, Credits, new[] { ArmorAbsorption, RegenBonus, HealthRegenBonus, PowerRegenBonus });
            mission.Prerequisite(CrucibleControlTheBrainV1.MissionId);
            mission.Scenario(FactoryThrown, "The Factory Reset Switch thrown", 1);
            mission.Scenario(OreThrown, "The Ore Machine Override Switch thrown", 1);

            mission.Objective(RestartMachine, 13939, 13940, 0, MissionObjectiveState.Incomplete);
            mission.Objective(OreProcessor, 13941, 13942, 1, MissionObjectiveState.Inactive);

            mission.Transition(RestartMachine);
            mission.Progress(RestartMachine, MissionProgressEventKind.ScenarioEvent, 1, null, counter: FactoryThrown);
            CrucibleCalendarGirlV1.Open(mission, RestartMachine, 1, 1, OreProcessor);

            mission.Transition(OreProcessor);
            mission.Progress(OreProcessor, MissionProgressEventKind.ScenarioEvent, 1, null, counter: OreThrown);
            mission.Action(OreProcessor, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(RestartMachine, 1, FactorySwitchAt.X, FactorySwitchAt.Y, FactorySwitchAt.Z, SwitchRadius, MapContextId);
            mission.Indicator(OreProcessor, 2, OreSwitchAt.X, OreSwitchAt.Y, OreSwitchAt.Z, SwitchRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1522",
                "Native1 'Restart the Machine' 13939/13940, 2 'Turn on the Ore Processor' 13941/13942; no counters, no " +
                "objectiveconversation. Texts 13586-13590. Usable class26578 names 190 'Factory Reset Switch', 191 'Ore Machine " +
                "Override Switch'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Fix_the_Factory",
                "Brian gives and rewards; requires Control the Brain; follow-up Factory Fortification; 85000XP, 8000 credits; " +
                "Modification: Armor Absorption, Regen Armor Bonus, Regen Health Bonus, Regen Power Bonus. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleFixTheFactoryV1",
                "Bound to the squad's Chaukas 2146. Two personal switches (26578, arg1, 55->56): name190 at 88,17.36,-176 (the " +
                "Robotics Assembly room's south entrance) for 1, name191 at -146,16.68,-55 (the Ore Processing ring) for 2; 1 opens " +
                "2. Modules [4] 122951, 122986, 123001, 123006, one to choose. Markers on the switches.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own two switches in the squad's Chaukas, each thrown once.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Brian in the squad's Chaukas
                MapContextId = MapContextId
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var (role, at, name, objective, sequence, scenario, yaw) in new[]
            {
                (FactoryRole, FactorySwitchAt, FactoryName, RestartMachine, FactorySequence, FactoryThrown, 0.0),
                (OreRole, OreSwitchAt, OreName, OreProcessor, OreSequence, OreThrown, 0.0)
            })
            {
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SwitchClass, at, Orientation: yaw,
                    InitialObjectState: ReadyState, NameOverrideId: name,
                    UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-thrown", role, ThrownState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, scenario, 1) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
