using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Plateau;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Stainless Steel Bloodhound (native mission 867), Plateau: Field Sgt. Garde activates a captured,
    /// reprogrammed Bane Sentinel that backtracks a Thrax patrol route; the player follows it to the hidden
    /// entrance of Valverde Chasm, and Commander Russ calls in by radio.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4534-4537 and 6470 (missionconversation; the turn-in
    /// 4537 is "INCOMING RADIO TRANSMISSION"); objectives 3 "Tell Garde you're ready." 4539/6471 and 2
    /// "Escort the Sentinel." 4540/6472, no counters (missionobjective); Garde's line on package 387 for
    /// objective 3 (4542: "I'm activating the Sentinel now") (objectiveconversation 867,3, flag 1).
    ///
    /// Sourced: TaRapedia The Stainless Steel Bloodhound: giver Field Sgt. Garde, reward "Radio Mission" at
    /// the Valverde Chasm cave entrance, requirement Lookout Down Below, follow-up Behind Enemy Lines;
    /// 40,000 XP, 4650 credits; one of AccuMax Virulent Injector Gun (green, 32), ChiTech Pulse Chaingun
    /// (green, 33), Teleract Pulse Pistol (green, 30), Vitalius Laser Cannon (green, 29); the bot floats
    /// beside the stream, crosses it and finds a cave entrance at -459.6, 273.1, 135.7. TR MMO DB: level 30.
    /// Patch notes: the escort can be killed (it "only drops junk loot if killed").
    ///
    /// Reconstructed (ESTIMATE): level 30. The rewards are the server's consecutive Uncommon templates
    /// 120366-120369, one of each of those four weapons at those levels (Weapon_Avatar_InjectionGun_Virulent
    /// _UNC_32_to_36, MachineGun_EMP_UNC_33_to_37, Pistol_EMP_UNC_30_to_34, RocketLauncher_Laser_UNC_29_to_33,
    /// whose client names are Virulent Injector Gun, Pulse Chaingun, Pulse Pistol, Laser Cannon). The sentinel
    /// (PlateauChasmNpcsV1) is the run's own; it appears at Garde's word on the navmesh beside the teleporter
    /// pad (the pad has none) and walks the navmesh's path to the cave (400 m, 2 m/s); its death fails the
    /// escort; at the cave it is gone. No sentinel loot.
    /// </summary>
    public static class PlateauBloodhoundV1
    {
        public const uint MissionId = PlateauBloodhoundScene.MissionId;
        public const uint Level = 30;
        public const uint Garde = 510183, GardePackage = 387;

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint InjectorGun = 120366, PulseChaingun = 120367, PulsePistol = 120368, LaserCannon = 120369;
        public const float Speed = 2f;

        /// <summary>The navmesh's path from beside the teleporter pad to the cave (NavMeshQuery.FindPath).</summary>
        public static readonly ScenePosition[] Route =
        {
            new(-106, 269.3f, 186.4f),
            new(-108, 269.3f, 187.6f),
            new(-110.4f, 270.1f, 187.6f),
            new(-160, 264.7f, 171.2f),
            new(-178.8f, 264.7f, 163.6f),
            new(-194.4f, 267.7f, 151.6f),
            new(-196.8f, 267.3f, 151.6f),
            new(-267.2f, 266.5f, 162),
            new(-270.4f, 268.1f, 162.4f),
            new(-276.8f, 268.7f, 160.8f),
            new(-278.8f, 267.9f, 157.2f),
            new(-284.8f, 267.9f, 157.2f),
            new(-292.8f, 266.9f, 161.2f),
            new(-303.2f, 266.9f, 168.8f),
            new(-324.4f, 266.7f, 184),
            new(-331.2f, 266.5f, 188.8f),
            new(-345.6f, 266.7f, 194.8f),
            new(-353.6f, 266.5f, 196.4f),
            new(-355.6f, 266.5f, 196.4f),
            new(-378.4f, 266.9f, 194.8f),
            new(-382.4f, 267.1f, 193.2f),
            new(-418.4f, 272.9f, 176.8f),
            new(-428.4f, 272.9f, 170.4f),
            new(-430.8f, 272.9f, 168.4f),
            new(-450.4f, 272.5f, 151.2f),
            new(-455.2f, 272.5f, 146.4f),
            new(-459.6f, 273.4f, 135.7f)
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Stainless Steel Bloodhound", 4534, Garde, null, Level);
            mission.Reward(Experience, Credits, new[] { InjectorGun, PulseChaingun, PulsePistol, LaserCannon });
            mission.RadioCompletion();
            mission.Prerequisite(PlateauLookoutDownBelowV1.MissionId);

            mission.Objective(PlateauBloodhoundScene.Ready, 4539, 6471, 0, MissionObjectiveState.Incomplete);
            mission.Objective(PlateauBloodhoundScene.Escort, 4540, 6472, 1, MissionObjectiveState.Inactive);

            // 3: Garde activates the sentinel; the scene's sequence 2 sends it on its way.
            mission.Scenario(PlateauBloodhoundScene.Arrived, "The sentinel reaches the hidden cave", 1);
            mission.Scenario(PlateauBloodhoundScene.Activate, "Garde activates the sentinel", 0);
            mission.Transition(PlateauBloodhoundScene.Ready);
            mission.Conversation(PlateauBloodhoundScene.Ready, GardePackage);
            mission.Action(PlateauBloodhoundScene.Ready, 1, MissionActionKind.RevealObjective, targetObjective: PlateauBloodhoundScene.Escort);
            mission.Action(PlateauBloodhoundScene.Ready, 2, MissionActionKind.ActivateObjective, targetObjective: PlateauBloodhoundScene.Escort,
                state: MissionObjectiveState.Incomplete);
            mission.Action(PlateauBloodhoundScene.Ready, 3, MissionActionKind.StartScenario, scenario: PlateauBloodhoundScene.Activate);

            // 2: the sentinel at the cave; then the radio.
            mission.Transition(PlateauBloodhoundScene.Escort);
            mission.Progress(PlateauBloodhoundScene.Escort, MissionProgressEventKind.ScenarioEvent, 1, null,
                counter: PlateauBloodhoundScene.Arrived);
            mission.Action(PlateauBloodhoundScene.Escort, 1, MissionActionKind.GrantReward, reward: 1);

            var start = Route[0];
            var cave = Route[^1];
            mission.Indicator(PlateauBloodhoundScene.Ready, 1, -92.9, 269.12, 179.7);              // Garde
            mission.Indicator(PlateauBloodhoundScene.Escort, 2, cave.X, cave.Y, cave.Z, 15);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/867",
                "Native3 'Tell Garde you're ready.' 4539/6471, 2 'Escort the Sentinel.' 4540/6472, no counters; " +
                "objectiveconversation 867,3,387 text4542. Texts 4534-4537 (4537 radio), 6470. Sentinel name8537.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Stainless_Steel_Bloodhound",
                "Giver Field Sgt. Garde, reward by radio at the cave entrance; requirement Lookout Down Below; follow-up Behind " +
                "Enemy Lines; 40000XP, 4650 credits; AccuMax Virulent Injector Gun 32, ChiTech Pulse Chaingun 33, Teleract Pulse " +
                "Pistol 30, Vitalius Laser Cannon 29 (green); cave entrance -459.6, 273.1, 135.7. TR MMO DB level30; patch " +
                "notes: the escort can be killed.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBloodhoundV1",
                $"Level30. Rewards: consecutive Uncommon templates 120366-120369. Personal sentinel650042 from ({start.X}, " +
                $"{start.Y}, {start.Z}) along the navmesh path to the cave (400m, 2m/s) after Garde's talk; its death fails 2; " +
                "at the cave it goes and 2 completes; radio turn-in.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Script = "plateau.bloodhound",
                Actors = new()
                {
                    ["sentinel"] = new SceneActorDefinition("sentinel", SceneActorKind.Creature, PlateauChasmNpcsV1.Sentinel,
                        start, Orientation: 0, InitiallyInteractable: false)
                },
                Routes = new()
                {
                    ["to-cave"] = new SceneRoute("to-cave", System.Array.ConvertAll(Route, point => new SceneWaypoint(point)),
                        Speed: Speed)
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [PlateauBloodhoundScene.Activate] = new SceneSequenceDefinition()
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
