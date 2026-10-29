using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Bad Doctor (native mission 886), Turpis Refinery (squad instance 1429), Valverde Pools: Commander Lindloff wants
    /// the refinery's supervisor, the "Poison Master", dead.
    ///
    /// Native: texts 4637-4640, 7328 (missionconversation): log "Find and kill The Poison Master and return his head to
    /// Commander Lindloff.", offer "The leader of this place is someone we've dubbed the "Poison Master". He's a ruthless
    /// Thrax soldier ... Kill him, and bring me some proof of it.", turn-in "You've actually done it."; objective 1 "Kill
    /// the Master Poisoner" 4668/7329 (missionobjective); no objective conversation. Name 10092 "The Toxicologist".
    ///
    /// Sourced: TaRapedia The Bad Doctor: giver and receiver Commander Lindloff (Turpis Refinery); no requirement; level
    /// 33; 50,000 XP, 4,950 credits; one of Prodigy Bio Armor Helmet lvl 30, Astra Hazmat Armor Boots lvl 34, Phoenix Motor
    /// Assist Armor Boots lvl 33, Titan Reflective Armor Boots lvl 34; "He is a technician-type mob called "The
    /// Toxicologist." He spawns at x138 y-38 when you go near it (but not before". Ten Ton Hammer Turpis Refinery guide:
    /// "killing the level 36 Thrax engineer named The Toxicologist at 133, -25". TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: a personal Toxicologist that spawns when the player
    /// enters his area):
    ///  - A server-only hidden objective 2 waits for the player in a cylinder of 25 m radius, 16 m above and below the
    ///    place (extent_y is a half-height about pos_y, MissionAreaService) (the navmesh's floor 5 m
    ///    from Ten Ton Hammer's point, 18 m from TaRapedia's); it brings the personal Toxicologist
    ///    (PoolsTurpisRefineryNpcsV1) there, who attacks. His death completes 1. No head item (the client has none).
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsTheBadDoctorV1
    {
        public const uint MissionId = 886;
        public const uint Level = 33;
        public const uint MapContextId = PoolsTurpisRefineryNpcsV1.MapContextId;
        public const uint Lindloff = PoolsTurpisRefineryNpcsV1.Lindloff;
        public const uint Kill = 1, Approach = 2, Lair = 1;
        public const uint ToxicologistKilled = 5, ToxicologistComes = 10, ToxicologistDown = 1;
        public const double LairRadius = 25;
        public const string ToxicologistRole = "toxicologist";
        public static readonly ScenePosition ToxicologistAt = new(127.8f, 106.11f, -23.5f);

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint BioHelmet = 18988;            // Armor_T3_Bio_V02_UNC_Helmet_30_to_34
        public const uint HazmatBoots = 20335;          // Armor_T2_Hazmat_V02_UNC_Boots_34_to_38
        public const uint MotorAssistBoots = 28614;     // Armor_T1_MotorAssist_V04_UNC_Boots_33_to_37
        public const uint ReflectiveBoots = 35571;      // Armor_T2_Reflective_V02_UNC_Boots_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "The Bad Doctor", 4637, Lindloff, Lindloff, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, HazmatBoots, MotorAssistBoots, ReflectiveBoots });

            mission.Objective(Kill, 4668, 7329, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Approach, 4668, 7329, 1, MissionObjectiveState.Incomplete);
            mission.Scenario(ToxicologistKilled, "The Toxicologist killed", 1);
            mission.Scenario(ToxicologistComes, "The Toxicologist comes", 0);
            mission.Area(Lair, MapContextId, ToxicologistAt.X, ToxicologistAt.Y, ToxicologistAt.Z, LairRadius, 16);

            // 2: near his place; he comes.
            mission.Transition(Approach);
            mission.EnterArea(Approach, Lair);
            mission.Action(Approach, 1, MissionActionKind.StartScenario, scenario: ToxicologistComes);

            // 1: his death.
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ToxicologistKilled);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { Approach },
                DefeatSequences = new() { [ToxicologistRole] = ToxicologistDown }
            };
            scene.Actors[ToxicologistRole] = new SceneActorDefinition(ToxicologistRole, SceneActorKind.Creature,
                PoolsTurpisRefineryNpcsV1.Toxicologist, ToxicologistAt, Orientation: 3.1416, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[ToxicologistComes] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("toxicologist-comes", ToxicologistRole),
                    QuestSceneIntents.Attack("toxicologist-attacks", ToxicologistRole)
                }
            };
            scene.Sequences[ToxicologistDown] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, ToxicologistKilled, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/886",
                "Native1 'Kill the Master Poisoner' 4668/7329; no objectiveconversation. Texts 4637-4640, 7328. Name 10092 The " +
                "Toxicologist.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Bad_Doctor",
                "Giver/receiver Commander Lindloff (Turpis Refinery); no requirement; level33; 50000XP, 4950 credits; one of Prodigy " +
                "Bio Armor Helmet30, Astra Hazmat Armor Boots34, Phoenix Motor Assist Armor Boots33, Titan Reflective Armor Boots34; " +
                "The Toxicologist spawns at 138,-38 when you go near. Ten Ton Hammer: a level36 Thrax engineer at 133,-25.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsTheBadDoctorV1",
                "A hidden 2 waits for the player within 25m of the place (user); then the personal Toxicologist 760245 comes and " +
                "attacks; his death completes 1. Items 18988, 20335, 28614, 35571, one to choose. Indicators in the instance too.", 0.4);

            mission.Indicator(Kill, 1, ToxicologistAt.X, ToxicologistAt.Y, ToxicologistAt.Z, LairRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
