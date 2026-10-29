using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Total Slaughter (native mission 1138), Retread Caves (squad instance 1694), Valverde Pools: H.P. Miles wants the
    /// camp's old water pump turned on; the noise draws the miasmas, and the player kills them all.
    ///
    /// Native: texts 8127-8131 (missionconversation): log "Turn on the water pump and when the Miasmas come to stop you
    /// eliminate them.", offer "I believe if we can destroy all the miasmas left that we can find in the caves they'll be
    /// done.", turn-in "It's good to have running water again"; objectives 1 "Turn on the Water Pump" 8185/8186, 2
    /// "Slaughter the Miasmas" 8187/8188 (missionobjective); no objective conversation.
    ///
    /// Sourced: TaRapedia Total Slaughter: giver H.P. Miles, reward giver Ellen Lenee (Retread Caves); requires Miasma
    /// Testing; 50,000 XP, 4,950 credits; one of Teleract Electric Injector Gun lvl 32, Vitalius Electric Chaingun lvl
    /// 33, Teleract Sonic Cannon lvl 34, Teleract Sonic Shotgun lvl 32; Miles: "there's some old water pumps down there
    /// ... Go turn on the computer down there, it will get the water pumping, and that should attract the miasmas to your
    /// location. When they come, kill them. Kill all of them."; "The miasma will approach in waves. The last wave most
    /// likely will have a long pause, and then begin with an enormous wave of miasma." TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: a personal scripted scene as the client's steps):
    ///  - A personal human computer (class 9478 UsableTwoStateHumComputerV01V06) on the camp's lowest floor by the river,
    ///    south-west of Miles, turns on the pump: 1 completes, 2 opens, and three waves of personal hostile Actinium
    ///    Miasmas (PoolsRetreadCavesNpcsV1) come at the player from round it: three, then four when the first are dead,
    ///    then six 20 s after the second are dead. Server-only hidden objectives 3-5 count the waves; 2 completes with
    ///    the last and pays.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels ("Chaingun" the Machine Gun, "Sonic
    ///    Cannon" the client's name of the Sonic Rocket Launcher), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsTotalSlaughterV1
    {
        public const uint MissionId = 1138;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Miles = PoolsRetreadCavesNpcsV1.Miles, Ellen = PoolsRetreadCavesNpcsV1.Ellen;
        public const uint Pump = 1, Slaughter = 2, FirstWave = 3, SecondWave = 4, LastWave = 5;
        public const uint PumpOn = 6, FirstDown = 7, SecondDown = 8, LastDown = 9;
        public const uint PumpSequence = 1, SecondLands = 2, LastPause = 3, LastLands = 4;
        public const uint LastPauseMs = 20000;
        public const string LastTimer = "last-wave";

        public const uint ComputerClass = 9478;         // UsableTwoStateHumComputerV01V06
        public const uint ComputerReadyState = 55, ComputerOnState = 56, UseArgument = 1;
        public const string ComputerRole = "water-pump-computer";
        public static readonly ScenePosition ComputerAt = new(-32f, 47.38f, -224f);
        public const double ObjectRadius = 8, WaveRadius = 30;

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint ElectricInjectorGun = 15582;  // Weapon_Avatar_InjectionGun_Electric_UNC_32_to_36
        public const uint ElectricChaingun = 15105;     // Weapon_Avatar_MachineGun_Electric_UNC_33_to_37
        public const uint SonicCannon = 49168;          // Weapon_Avatar_RocketLauncher_Sonic_UNC_34_to_38
        public const uint SonicShotgun = 14933;         // Weapon_Avatar_Shotgun_v1_Sonic_UNC_32_to_36

        /// <summary>The waves: the hidden objective that counts it, its scenario, where its miasmas start.</summary>
        public static readonly (uint Objective, uint Counter, ScenePosition[] At)[] Waves =
        {
            (FirstWave, FirstDown, new ScenePosition[] { new(-24f, 45.87f, -224f), new(-40f, 48.45f, -232f), new(-24f, 46.01f, -208f) }),
            (SecondWave, SecondDown, new ScenePosition[]
            {
                new(-16f, 45.85f, -216f), new(-40f, 48.45f, -216f), new(-31.2f, 47.56f, -232.6f), new(-8f, 47.66f, -216f)
            }),
            (LastWave, LastDown, new ScenePosition[]
            {
                new(-16.4f, 46.05f, -224f), new(-48f, 48.45f, -224f), new(-24f, 45.95f, -216f),
                new(-40f, 48.45f, -208f), new(-16f, 45.86f, -208f), new(-32f, 47.26f, -208f)
            }),
        };

        public static string MiasmaRole(int wave, int index) => $"wave-{wave + 1}-miasma-{index + 1}";
        public static uint MiasmaDown(int wave, int index) => 31 + 10 * (uint)wave + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Total Slaughter", 8127, Miles, Ellen, Level);
            mission.Reward(Experience, Credits, new[] { ElectricInjectorGun, ElectricChaingun, SonicCannon, SonicShotgun });
            mission.Prerequisite(PoolsMiasmaTestingV1.MissionId);

            mission.Objective(Pump, 8185, 8186, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Slaughter, 8187, 8188, 1, MissionObjectiveState.Inactive);
            for (var wave = 0; wave < Waves.Length; wave++)
            {
                mission.Objective(Waves[wave].Objective, 8187, 8188, 2 + (uint)wave, MissionObjectiveState.Incomplete);
                mission.Scenario(Waves[wave].Counter, $"Total Slaughter wave {wave + 1} killed", (uint)Waves[wave].At.Length);
            }
            mission.Scenario(PumpOn, "The water pump turned on", 1);
            mission.Scenario(SecondLands, "The second wave comes", 0);
            mission.Scenario(LastPause, "The pause before the last wave", 0);

            // 1: the pump on; 2 opens (the first wave comes with the pump).
            mission.Transition(Pump);
            mission.Progress(Pump, MissionProgressEventKind.ScenarioEvent, 1, null, counter: PumpOn);
            mission.Action(Pump, 1, MissionActionKind.RevealObjective, targetObjective: Slaughter);
            mission.Action(Pump, 2, MissionActionKind.ActivateObjective, targetObjective: Slaughter, state: MissionObjectiveState.Incomplete);

            // 3-5: each wave's deaths; the next wave follows.
            for (var wave = 0; wave < Waves.Length; wave++)
            {
                var (objective, counter, at) = Waves[wave];
                var count = (uint)at.Length;
                mission.Transition(objective);
                for (var id = 1U; id <= count; id++)
                    mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, id, count, counter: counter, trigger: id);
            }
            mission.Action(FirstWave, 1, MissionActionKind.StartScenario, scenario: SecondLands);
            mission.Action(SecondWave, 1, MissionActionKind.StartScenario, scenario: LastPause);

            // 2: with the last wave dead.
            mission.Transition(Slaughter);
            mission.ObjectiveState(Slaughter, LastWave, MissionObjectiveState.Completed);
            mission.Action(Slaughter, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                DefeatSequences = new(),
                HiddenObjectiveIds = new() { FirstWave, SecondWave, LastWave }
            };
            scene.Actors[ComputerRole] = new SceneActorDefinition(ComputerRole, SceneActorKind.Object, ComputerClass, ComputerAt,
                Orientation: 1.5708, InitialObjectState: ComputerReadyState,
                UseAction: new SceneObjectAction(MissionId, Pump, PumpSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-computer", ComputerRole) } };
            var pumpOn = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("pump-on", ComputerRole, ComputerOnState),
                    new SetInteractionIntent("computer-done", ComputerRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, PumpOn, 1) }
            };
            var lands = new[] { pumpOn, new SceneSequenceDefinition(), new SceneSequenceDefinition() };
            for (var wave = 0; wave < Waves.Length; wave++)
            {
                var (_, counter, at) = Waves[wave];
                for (var index = 0; index < at.Length; index++)
                {
                    var role = MiasmaRole(wave, index);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PoolsRetreadCavesNpcsV1.PumpMiasma,
                        at[index], GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
                    lands[wave].World.Add(new EnsureActorIntent($"{role}-comes", role));
                    lands[wave].World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                    scene.DefeatSequences[role] = MiasmaDown(wave, index);
                    scene.Sequences[MiasmaDown(wave, index)] = new SceneSequenceDefinition
                    {
                        Signals = { new SceneMissionSignal(MissionId, counter, (uint)index + 1) }
                    };
                }
            }
            scene.Sequences[PumpSequence] = lands[0];
            scene.Sequences[SecondLands] = lands[1];
            scene.Sequences[LastPause] = new SceneSequenceDefinition { Timers = { new SequenceTimer(LastTimer, LastPauseMs, LastLands) } };
            scene.Sequences[LastLands] = lands[2];

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1138",
                "Native1 'Turn on the Water Pump' 8185/8186, 2 'Slaughter the Miasmas' 8187/8188; no objectiveconversation. Texts " +
                "8127-8131. Name 7913 Actinium Miasma.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Total_Slaughter",
                "Giver H.P. Miles, reward Ellen Lenee (Retread Caves); requires Miasma Testing; 50000XP, 4950 credits; one of Teleract " +
                "Electric Injector Gun32, Vitalius Electric Chaingun33, Teleract Sonic Cannon34, Teleract Sonic Shotgun32; turn on " +
                "the computer down there; waves, a long pause before an enormous last wave. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsTotalSlaughterV1",
                "Personal scene (user): a computer 9478 on the camp's lowest floor turns the pump on; waves of 3, 4 and (20s later) " +
                "6 personal Actinium Miasmas 760228; hidden 3-5 count them, 2 with the last. Items 15582, 15105, 49168, 14933, one " +
                "to choose. Places are estimates. Indicators in the instance too.", 0.3);

            mission.Indicator(Pump, 1, ComputerAt.X, ComputerAt.Y, ComputerAt.Z, ObjectRadius);
            mission.Indicator(Slaughter, 2, ComputerAt.X, ComputerAt.Y, ComputerAt.Z, WaveRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
