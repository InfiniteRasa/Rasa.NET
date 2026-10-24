using System;
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
    /// Out of Thin Air (native mission 1190), Ustor Yard (squad instance 1502): a prisoner thrown out of Maligo's
    /// wormhole prototype has landed on a rock in the yard; the player takes a Bane patrol's environmental stim pack
    /// for her and escorts her out by the west exit.
    ///
    /// Native: texts 9142-9146 (missionconversation): offer "I've been watching the Bane patrols and they appear to have
    /// some sort of stim pack that keeps them from feeling the effects of all this stuff around us ... Can you get me
    /// one of those stim packs so I can get off this rock?", turn-in (High Command) "head to Maligo ASAP"; objectives
    /// (missionobjective), no counters, empty descriptions: 2 "Ambush Thrax Patrols in the Area and Obtain an
    /// Environmental Protection Stim Pack" 9234/9235, 3 "Return to Maligo Prisoner and Give Her the Environmental
    /// Protection Stim Pack" 9238/9239 (objectiveconversation on 1307, 9240 "All good now!"), 1 "Escort Maligo
    /// Prisoner Out of Ustor Yard." 9236/9237.
    ///
    /// Sourced: TaRapedia Out of Thin Air: giver Maligo Base Prisoner (Ustor Yard), reward from "Headquarters", no
    /// requirement; 45,000 XP, 6,400 credits, one of Pulsar Bio Armor Vest, Olympia Graviton Armor Legs, Audiodyne
    /// Reflective Armor Vest, Luminar Motor Assist Armor Gloves. Walkthrough: "When you accept the mission a small Bane
    /// patrol will spawn to the south east of the waste processing pool. Kill and loot this patrol to find the
    /// environmental stim pack. The prisoner wants to be escorted out of the west instance exit. This won't be
    /// accessible until you have completed Monkey Wrench". TR MMO DB: level 32.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-24; see PlateauMaligoPrisonerV1 for the NPCs):
    ///  - Requires Monkey Wrench (1188), for the west exit (the exit itself is open to everybody: PlateauMaligoBaseV1).
    ///  - 2: three personal Thrax south-east of the middle dirty pond (client WaterForeasPond32mDirty at 204, 173, -36);
    ///    one drops the stim pack, a personal 100% drop while 2 is open. The item: MisPlateauUstorItemBaneRaditionBlocker
    ///    (class 24895, template 50352), the one Ustor mission item no other mission uses.
    ///  - 3: her talk takes the stim pack and sets her off (PlateauOutOfThinAirScene).
    ///  - 1: upstream's escort: she, the public NPC (as Escape Velocity's Sgt. Pierre: busy for others meanwhile), walks
    ///    the navmesh's path to the west exit's inner side, stops to fight; her death, a blocked route or the owner's
    ///    loss fails 1; at the end she is gone. Turned in by radio.
    ///  - Rewards: the nearest Uncommon template of each kind (the manufacturer names are not in the client tables).
    ///    No indicators: instance.
    /// </summary>
    public static class PlateauOutOfThinAirV1
    {
        public const uint MissionId = PlateauOutOfThinAirScene.MissionId;
        public const uint Level = 32;
        public const uint Prisoner = PlateauMaligoPrisonerV1.Prisoner, PrisonerPackage = PlateauMaligoPrisonerV1.PrisonerPackage;
        public const uint Escort = PlateauOutOfThinAirScene.Escort, StimPack = 2, GiveIt = 3;
        public const uint EscortStarts = PlateauOutOfThinAirScene.EscortStarts, OutOfTheYard = PlateauOutOfThinAirScene.OutOfTheYard,
            Out = PlateauOutOfThinAirScene.Out;

        public const uint Stim = 50352, StimClass = 24895;
        public const string StimKey = "environmental-stim-pack";

        public const uint Experience = 45000;
        public const uint Credits = 6400;
        public const uint BioVest = 120374;             // Armor_T3_Bio_V04_UNC_Vest_30_to_34
        public const uint GravitonLegs = 120431;        // Armor_T3_Graviton_V04_UNC_Legs_30_to_34
        public const uint ReflectiveVest = 36148;       // Armor_T2_Reflective_V02_UNC_Vest_29_to_33
        public const uint MechGloves = 120412;          // Armor_T3_Mech_V04_UNC_Gloves_30_to_34
        public const float EscortSpeed = 2.5f;

        /// <summary>The patrol, south-east of the middle pond (on the navmesh).</summary>
        public static readonly ScenePosition[] PatrolAt =
            { new(218.9f, 177.32f, -49.3f), new(214.4f, 177.4f, -54.8f), new(224.2f, 175.53f, -46.4f) };

        /// <summary>The navmesh's path from her rock to the west exit's inner side (NavMeshQuery.FindPath, 290 m).</summary>
        public static readonly ScenePosition[] Route =
        {
            new(19, 178.15f, 121),
            new(18, 177.8f, 118),
            new(8.4f, 178, 96),
            new(-11.2f, 172.4f, 14.4f),
            new(-16, 175.2f, 11.6f),
            new(-39.2f, 175.8f, -7.2f),
            new(-44, 176, -8),
            new(-62.4f, 172.2f, -7.6f),
            new(-134.8f, 168.6f, 2.8f),
            new(-151.6f, 168.8f, -0.4f),
            new(-163.2f, 164.8f, -10),
            new(-172, 160.98f, 2)
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Out of Thin Air", 9142, Prisoner, null, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, GravitonLegs, ReflectiveVest, MechGloves });
            mission.RadioCompletion();
            mission.Prerequisite(PlateauMonkeyWrenchV1.MissionId);

            mission.Objective(StimPack, 9234, 9235, 0, MissionObjectiveState.Incomplete);
            mission.Objective(GiveIt, 9238, 9239, 1, MissionObjectiveState.Inactive);
            mission.Objective(Escort, 9236, 9237, 2, MissionObjectiveState.Inactive);
            mission.Scenario(EscortStarts, "The prisoner walks to the west exit", 0);
            mission.Scenario(OutOfTheYard, "The prisoner is out by the west exit", 1);

            mission.Transition(StimPack);
            mission.Progress(StimPack, MissionProgressEventKind.ItemAcquired, StimClass, 1);
            PlateauSilentButDeadlyV1.Open(mission, StimPack, 1, GiveIt);

            mission.Transition(GiveIt);
            mission.Conversation(GiveIt, PrisonerPackage);
            mission.Action(GiveIt, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("stim-to-prisoner", MissionId, StimKey, 1, MissionItemScope.CharacterOwned));
            PlateauSilentButDeadlyV1.Open(mission, GiveIt, 2, Escort);
            mission.Action(GiveIt, 4, MissionActionKind.StartScenario, scenario: EscortStarts);

            // 1: the scene's event at the exit completes it, or the scene fails it (PlateauOutOfThinAirScene); then the radio.
            mission.Transition(Escort);
            mission.Progress(Escort, MissionProgressEventKind.ScenarioEvent, Out, null, counter: OutOfTheYard);
            mission.Action(Escort, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1190",
                "Native2 stim pack 9234/9235, 3 give it 9238/9239, 1 escort 9236/9237; no counters. objectiveconversation 1190,3 " +
                "on 1307 text9240. Texts 9142-9146. Name 9168 Maligo Base Prisoner. Item class24895/template50352 (ESTIMATE).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Out_of_Thin_Air",
                "Giver Maligo Base Prisoner (Ustor Yard), reward from Headquarters; no requirement; 45000XP, 6400 credits; Pulsar " +
                "Bio vest/Olympia Graviton legs/Audiodyne Reflective vest/Luminar Motor Assist gloves. Walkthrough: a patrol SE of " +
                "the waste processing pool on accept; the west exit, after Monkey Wrench. TR MMO DB level32.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauOutOfThinAirV1",
                "Level32. Requires 1188. Three personal Thrax650106 SE of the pond (204,173,-36), a personal 100% drop of 50352 " +
                "while 2 is open. Her talk takes it; the public prisoner650105 walks the navmesh path to the west exit (290m), her " +
                "death fails 1. Radio turn-in. Items 120374, 120431, 36148, 120412. Places are estimates.", 0.3);

            mission.Enable(Scene());
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        public static MissionSceneDefinition Scene()
        {
            var hostile = new ActorGameplayPolicy
            {
                RewardScenarioKills = true, TrackParticipation = true,
                // The run's own creature: no ordinary loot table can bind it, so none (the stim pack is the mission's drop).
                Loot = new AuthoredLootProfile(Array.Empty<LootDrop>())
            };
            var scene = new MissionSceneDefinition
            {
                Script = "plateau.out-of-thin-air",
                MapContextId = PlateauMaligoPrisonerV1.MapContextId,
                ExistingFactObjectiveIds = new() { StimPack },
                Items = new()
                {
                    // A corpse drop is character-owned, so kept on abandon or failure (as One Way Trip's keypass).
                    new MissionItemBinding(StimKey, Stim, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { PlateauMaligoPrisonerV1.Patrol }, StimPack,
                            PlateauMaligoPrisonerV1.MapContextId, 100, 1))
                },
                Actors = new()
                {
                    [PlateauOutOfThinAirScene.Prisoner] = new SceneActorDefinition(PlateauOutOfThinAirScene.Prisoner,
                        SceneActorKind.PublicSpawn, PlateauMaligoPrisonerV1.PrisonerPool)
                },
                Routes = new()
                {
                    [PlateauOutOfThinAirScene.Route] = new SceneRoute(PlateauOutOfThinAirScene.Route,
                        Array.ConvertAll(Route, point => new SceneWaypoint(point)), Speed: EscortSpeed)
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [EscortStarts] = new SceneSequenceDefinition()
                },
                PublicEncounter = new PublicEncounterBinding(MissionId, PlateauMaligoPrisonerV1.PrisonerPool,
                    PlateauOutOfThinAirScene.Prisoner, "plateau.out-of-thin-air", OwnerLossPolicy: "Fail", ManualCombat: true)
            };
            for (var index = 0; index < PlateauOutOfThinAirScene.PatrolSize; index++)
            {
                var role = PlateauOutOfThinAirScene.PatrolRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PlateauMaligoPrisonerV1.Patrol,
                    PatrolAt[index], Orientation: Math.PI, GameplayPolicy: hostile);
            }
            return scene;
        }
    }
}
