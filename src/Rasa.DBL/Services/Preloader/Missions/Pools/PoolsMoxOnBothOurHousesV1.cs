using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// A Mox On Both Our Houses (native mission 1133), Retread Caves (squad instance 1694), Valverde Pools: Dr. Donius
    /// gives the player a laced carton of cigarettes for the old incense burner in the Mox cave, to draw out the Alpha Mox.
    ///
    /// Native: texts 8023-8027 (missionconversation): log "Place the laced carton of cigarettes into the empty container,
    /// then wait for the Alpha Mox to come to you. When he does, kill him, then return to Ellen Lenee.", offer "take this
    /// carton of cigarettes to the cave where you found those Mox and killed them earlier for Ellen. There's an old Forean
    /// incense burner sitting in there", turn-in "That should calm things down a bit."; objectives 1 "Place the Tobacco"
    /// 8028/8029, 2 "Kill the Alpha" 8030/8031 counter 8032 "Alpha Mox Killed" (missionobjective); no objective
    /// conversation. Item: 49704 MisRetreadCavesCartonOfCigarettes.
    ///
    /// Sourced: TaRapedia A Mox On Both Our Houses: giver Dr. Donius, reward giver Ellen Lenee (Retread Caves); requires
    /// Mox Shouldn't Smoke; 45,000 XP, 4,800 credits; one of Phoenix Radial Repair Tool lvl 30, AccuMax Sonic Disperser
    /// lvl 32, AccuMax Rifle lvl 31, Eclipse Sonic Torqueshell Rifle lvl 33. Ten Ton Hammer Retread Caves guide: the
    /// Alpha Mox level 36 with a really high health regeneration rate, "not hard to dodge as long as you keep moving, he
    /// is just really hard to kill". TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The carton (a new one, the laced carton) comes at the accept. A personal Forean brazier (class 7636
    ///    UsableTwoStateForeanElohBrazierV01, the client's only usable Forean burner) in the Western Grotto's main room
    ///    takes it; 2 opens and the Alpha Mox (PoolsRetreadCavesNpcsV1, personal, hostile) comes at the player from the
    ///    room's far side. Its death completes 2 and pays.
    ///  - The Alpha has no regeneration (the server has no such mechanic); more hit points instead.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels ("Radial" the Area Repair Tool), one
    ///    to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsMoxOnBothOurHousesV1
    {
        public const uint MissionId = 1133;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Donius = PoolsRetreadCavesNpcsV1.Donius, Ellen = PoolsRetreadCavesNpcsV1.Ellen;
        public const uint Place = 1, Kill = 2;
        public const uint Placed = 5, AlphaKilled = 6, AlphaComes = 3;
        public const uint BurnerSequence = 1, AlphaDown = 2;

        public const uint BurnerClass = 7636;           // UsableTwoStateForeanElohBrazierV01
        public const uint BurnerReadyState = 55, BurnerLitState = 56, UseArgument = 1;
        public const uint CartonTemplate = PoolsMoxShouldntSmokeV1.CartonTemplate;
        public const string CartonKey = "laced-cigarettes", BurnerRole = "incense-burner", AlphaRole = "alpha-mox";
        public static readonly ScenePosition BurnerAt = new(144f, 11.08f, -48f);
        public static readonly ScenePosition AlphaAt = new(200f, 9.1f, -48f);
        public const double ObjectRadius = 8, AlphaRadius = 15;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint RadialRepairTool = 120434;    // Tool_Avatar_Field_Repair_Area_UNC_30_to_34
        public const uint SonicDisperser = 48788;       // Weapon_Avatar_PropellantGun_Sonic_UNC_32_to_36
        public const uint Rifle = 14610;                // Weapon_Avatar_Rifle_Physical_UNC_31_to_35
        public const uint SonicTorqueshellRifle = 49652; // Weapon_Avatar_Torqueshell_Rifle_Sonic_UNC_33_to_37

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "A Mox On Both Our Houses", 8023, Donius, Ellen, Level);
            mission.Reward(Experience, Credits, new[] { RadialRepairTool, SonicDisperser, Rifle, SonicTorqueshellRifle });
            mission.Prerequisite(PoolsMoxShouldntSmokeV1.MissionId);

            mission.Objective(Place, 8028, 8029, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Kill, 8030, 8031, 1, MissionObjectiveState.Inactive, counterTextId: 8032);
            mission.Scenario(Placed, "The laced carton in the incense burner", 1);
            mission.Scenario(AlphaKilled, "The Alpha Mox killed", 1);
            mission.Scenario(AlphaComes, "The Alpha Mox comes", 0);

            // 1: the carton in the burner; 2 opens and the Alpha comes.
            mission.Transition(Place);
            mission.Progress(Place, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Placed);
            mission.Action(Place, 1, MissionActionKind.RevealObjective, targetObjective: Kill);
            mission.Action(Place, 2, MissionActionKind.ActivateObjective, targetObjective: Kill, state: MissionObjectiveState.Incomplete);
            mission.Action(Place, 3, MissionActionKind.StartScenario, scenario: AlphaComes);

            // 2: the Alpha's death.
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: AlphaKilled);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                DefeatSequences = new() { [AlphaRole] = AlphaDown },
                Items = new()
                {
                    new MissionItemBinding(CartonKey, CartonTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-laced-carton", MissionId, CartonKey, CartonTemplate, 1) }
            };
            scene.Actors[BurnerRole] = new SceneActorDefinition(BurnerRole, SceneActorKind.Object, BurnerClass, BurnerAt,
                InitialObjectState: BurnerReadyState, UseAction: new SceneObjectAction(MissionId, Place, BurnerSequence, UseArgument));
            scene.Actors[AlphaRole] = new SceneActorDefinition(AlphaRole, SceneActorKind.Creature, PoolsRetreadCavesNpcsV1.AlphaMox,
                AlphaAt, Orientation: 3.1416, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-burner", BurnerRole) } };
            scene.Sequences[BurnerSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("burner-lit", BurnerRole, BurnerLitState),
                    new SetInteractionIntent("burner-done", BurnerRole, false)
                },
                Character = { new ConsumeMissionItemIntent("carton-in-burner", MissionId, CartonKey, 1, MissionItemScope.AssignmentIssued) },
                Signals = { new SceneMissionSignal(MissionId, Placed, 1) }
            };
            scene.Sequences[AlphaComes] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("alpha-comes", AlphaRole),
                    QuestSceneIntents.Attack("alpha-attacks", AlphaRole)
                }
            };
            scene.Sequences[AlphaDown] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, AlphaKilled, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1133",
                "Native1 'Place the Tobacco' 8028/8029, 2 'Kill the Alpha' 8030/8031 counter8032 'Alpha Mox Killed'; no " +
                "objectiveconversation. Texts 8023-8027 (the old Forean incense burner in the Mox cave). Name 9028 Alpha Mox.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Mox_On_Both_Our_Houses",
                "Giver Dr. Donius, reward Ellen Lenee (Retread Caves); requires Mox Shouldn't Smoke; 45000XP, 4800 credits; one of " +
                "Phoenix Radial Repair Tool30, AccuMax Sonic Disperser32, AccuMax Rifle31, Eclipse Sonic Torqueshell Rifle33. Ten " +
                "Ton Hammer: the Alpha Mox level36, high health regeneration. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMoxOnBothOurHousesV1",
                "Laced carton at accept; a personal brazier 7636 in the Western Grotto takes it; the personal Alpha Mox 760225 " +
                "then attacks, its death pays. No regeneration. Items 120434, 48788, 14610, 49652, one to choose. Places are " +
                "estimates. Indicators in the instance too.", 0.3);

            mission.Indicator(Place, 1, BurnerAt.X, BurnerAt.Y, BurnerAt.Z, ObjectRadius);
            mission.Indicator(Kill, 2, AlphaAt.X, AlphaAt.Y, AlphaAt.Z, AlphaRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
