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
    /// Clean Slate (native mission 1983), Plateau: Corporal Wainwright in New Velon Village sends the player
    /// to escort an amnesiac soldier to Special Agent Perdu at Fort Defiance; at the fort the soldier goes
    /// berserk and must be killed; Perdu explains the Bane mind-control implants. It opens Mind Bender (905).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 20892-20895 and 20896 (missionconversation); objectives
    /// 1 "Escort the soldier to Agent Perdu." 20897/20898, 4 "Kill the soldier!" 20904/20905, 5 "Special Agent
    /// Perdu wants to talk to you." 20921/20922, no counters (missionobjective); Perdu's "That soldier was a
    /// walking timebomb." on package 639 (20923) (objectiveconversation 1983,5, flag 1). Mission 714 is the
    /// older version of the same mission (the soldier gives it, a 4 h timer); it stays off.
    ///
    /// Sourced: TaRapedia Clean Slate: giver Corporal Wainwright, reward from Agent Perdu, level "29 (possibly
    /// less)", no requirement, follow-up Mind Bender; 40,000 XP, 4,650 credits, "Equipment"; "When the
    /// soldier returns to the Fort Defiance entrance, he turns hostile and you are directed to put him down."
    /// TR MMO DB: level 30. Ten Ton Hammer: "escort him to the second shield and realize he's not what he
    /// seems. Killing him will continue the series with Agent Perdu."
    ///
    /// Reconstructed (ESTIMATE): level 30. The soldier is the run's own (a scene creature), spawned by
    /// Wainwright when the mission is taken; he walks the navmesh's path from the village to the gate
    /// (406 m, 2.5 m/s) and fights on the way; if he dies the escort fails. The berserk copy gives kill experience but no loot. At the gate's end point his
    /// hostile copy takes his place and attacks the player (PlateauCleanSlateScene). "Equipment" read as the
    /// four Uncommon V04 helmets of the 30-34 band, one of each armor type, no module.
    /// </summary>
    public static class PlateauCleanSlateV1
    {
        public const uint MissionId = PlateauCleanSlateScene.MissionId;
        public const uint Level = 30;
        public const uint Wainwright = PlateauPerduNpcsV1.Wainwright;
        public const uint Perdu = PlateauPerduNpcsV1.Perdu, PerduPackage = PlateauPerduNpcsV1.PerduPackage;

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint BioHelmet = 46019, GravitonHelmet = 25079, MechHelmet = 34205, StealthHelmet = 40333;
        public const float Speed = 2.5f;

        /// <summary>The navmesh's path from beside Wainwright to Perdu at the gate (NavMeshQuery.FindPath).</summary>
        public static readonly ScenePosition[] Route =
        {
            new(198, 352.3f, 524.4f),
            new(173.2f, 351.1f, 534.8f),
            new(143.6f, 348.7f, 538.4f),
            new(123.2f, 355.3f, 548),
            new(100.8f, 364.5f, 552),
            new(69.2f, 378.1f, 583.6f),
            new(52.8f, 389.7f, 620.4f),
            new(16.8f, 380.1f, 646.4f),
            new(-46, 383.1f, 681.2f),
            new(-77.2f, 399.1f, 710),
            new(-88.4f, 399.9f, 713.2f),
            new(-124, 399.1f, 712)
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Clean Slate", 20892, Wainwright, Perdu, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, GravitonHelmet, MechHelmet, StealthHelmet });

            mission.Objective(PlateauCleanSlateScene.Escort, 20897, 20898, 0, MissionObjectiveState.Incomplete);
            mission.Objective(PlateauCleanSlateScene.Kill, 20904, 20905, 1, MissionObjectiveState.Inactive);
            mission.Objective(PlateauCleanSlateScene.Report, 20921, 20922, 2, MissionObjectiveState.Inactive);

            // 1 and 4 are the scene's (PlateauCleanSlateScene); 5: Perdu.
            mission.Transition(PlateauCleanSlateScene.Report);
            mission.Conversation(PlateauCleanSlateScene.Report, PerduPackage);
            mission.Action(PlateauCleanSlateScene.Report, 1, MissionActionKind.GrantReward, reward: 1);

            var gate = Route[^1];
            mission.Indicator(PlateauCleanSlateScene.Escort, 1, gate.X, gate.Y, gate.Z, 15);   // Perdu at the gate
            mission.Indicator(PlateauCleanSlateScene.Kill, 2, gate.X, gate.Y, gate.Z, 15);
            var perdu = PlateauPerduNpcsV1.Moves[0].At;
            mission.Indicator(PlateauCleanSlateScene.Report, 3, perdu.X, perdu.Y, perdu.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1983",
                "Native1 'Escort the soldier to Agent Perdu.' 20897/20898, 4 'Kill the soldier!' 20904/20905, 5 Perdu 20921/20922, " +
                "no counters; objectiveconversation 1983,5,639 text20923. Texts 20892-20896. Wainwright name6765. 714 is the older version.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Clean_Slate",
                "Giver Corporal Wainwright, reward from Agent Perdu; level '29 (possibly less)'; no requirement; follow-up Mind " +
                "Bender; 40000XP, 4650 credits, 'Equipment'; the soldier turns hostile at the Fort Defiance entrance. TR MMO DB " +
                "level30; Ten Ton Hammer: escort him to the second shield.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauCleanSlateV1",
                "Level30. Personal scene soldier650035 from beside Wainwright along the navmesh path to the gate (406m, 2.5m/s), " +
                "fights on the way, his death fails 1; at the end his hostile copy650036 replaces him and attacks; its death " +
                "completes 4. Equipment read as Uncommon V04 helmets 30-34: 46019, 25079, 34205, 40333. Perdu moved to the gate " +
                "(PlateauPerduNpcsV1).", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "plateau.clean-slate",
                Actors = new()
                {
                    ["soldier"] = new SceneActorDefinition("soldier", SceneActorKind.Creature, PlateauPerduNpcsV1.Soldier,
                        Route[0], Orientation: 0, InitiallyInteractable: false),
                    ["berserk"] = new SceneActorDefinition("berserk", SceneActorKind.Creature, PlateauPerduNpcsV1.Berserk,
                        gate, Orientation: 0,
                        GameplayPolicy: new ActorGameplayPolicy
                        {
                            RewardScenarioKills = true, TrackParticipation = true,
                            // The run's own creature on a public map: no ordinary loot table can bind it, so none.
                            Loot = new AuthoredLootProfile(System.Array.Empty<LootDrop>())
                        })
                },
                Routes = new()
                {
                    ["to-perdu"] = new SceneRoute("to-perdu", System.Array.ConvertAll(Route, point => new SceneWaypoint(point)),
                        Speed: Speed)
                },
                DefeatSequences = new() { ["berserk"] = PlateauCleanSlateScene.BerserkDefeated },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [PlateauCleanSlateScene.BerserkDefeated] = new SceneSequenceDefinition()
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
