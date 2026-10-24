using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Plateau;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// These Old Bones (native mission 1976), Velon Hollow (squad instance 2136): Elder Hathen, at the hospital, comes
    /// with the player to clear the infected Foreans and Bane out of the three temples; Warrior Algona takes the hand-in.
    ///
    /// Native: texts 20808-20812 (missionconversation; offer "They're holding everyone inside the temples"); objectives
    /// 1 "Secure the Mausoleum of the Beholden" 20841/20842, 2 "Secure the Shrine of the Penitent" 20843/20844, 3
    /// "Secure the Santuary of Renewal" 20845/20846 (each "Eliminate all infected Foreans and Bane found ..."), 4
    /// "Protect Elder Hathen" 20847/20848 ("must be protected as a VIP"); no counters, no objective conversations
    /// (missionobjective). Names 10209 Elder Hathen, 10513 Warrior Algona, 9859 Infected Forean.
    ///
    /// Sourced: TaRapedia These Old Bones (Velon Hollow mission list): giver Elder Hathen, reward from Warrior Algona,
    /// follow-up Holy Vengeance; 45,000 XP, 4,800 credits, Prodigy/Titan Gloves.
    ///
    /// Reconstructed (ESTIMATE, the user's decisions of 2026-10-22 and 2026-10-24):
    ///  - Requires Village Of The Damned (1981), as the other Velon Hollow missions. Level 32, as theirs.
    ///  - 4: on accept a copy of Hathen (<see cref="HathenEscort"/>, the server's escort health and armour) stands at the
    ///    hospital and follows the player; his death fails 4. After the third temple he walks back to the hospital and
    ///    is gone there (PlateauTheseOldBonesScene).
    ///  - 1-3: walking within <see cref="TempleRadius"/> of a temple's centre (its map_region) completes its hidden
    ///    objective (5-7), which brings 4 Infected Foreans and 1 outpost Thrax Soldier (level 31,
    ///    PlateauVelonHollowBaneV1) round the centre against the player; all five dead completes the temple. The
    ///    temple's boss (Velon Elder Bosses) is not one of them.
    ///  - Warrior Algona: a new public NPC by Hathen at the hospital, in Ranger Kaely's NPC class and outfit (7034) at level 32, with a
    ///    server-only package (the client has none for him).
    ///  - Rewards: the four V04 Uncommon gloves of level 30-34 (the TaRapedia names are not in the client tables).
    ///  - The escort walks the navmesh: on the shipped Velon Hollow mesh the hospital is an island (an upstream navmesh bug
    ///    report), so Hathen cannot leave it until the mesh is fixed. No indicators: instance.
    ///
    /// Ids: creatures 650107 (the escort copy) and 650108 (Algona), Algona's pool 650108, free before.
    /// </summary>
    public static class PlateauTheseOldBonesV1
    {
        public const uint MissionId = PlateauTheseOldBonesScene.MissionId;
        public const uint Level = 32;
        public const uint MapContextId = PlateauVelonHollowBaneV1.MapContextId;
        public const uint Hathen = PlateauOrderOfVelonV1.Hathen;
        public const uint Mausoleum = 1, Shrine = 2, Sanctuary = 3, Protect = PlateauTheseOldBonesScene.Protect;
        public const uint Outcome = PlateauTheseOldBonesScene.Outcome;

        public const uint HathenEscort = 650107, HathenClass = 7035, HathenName = 10209;
        public const uint Algona = 650108, AlgonaPool = 650108, AlgonaName = 10513, AlgonaClass = 7034;   // Ranger Kaely's NPC class (New Velon)
        /// <summary>Algona's package: the client has none for him, so a server-only id (as the Infinite Rasa welcome officer's).</summary>
        public const uint AlgonaPackage = 650108;
        public const uint AlgonaOutfitOf = 510189;      // Ranger Kaely - New Velon Village
        public const uint EscortSpeed = 7, EscortWalk = 2;

        public const uint Experience = 45000;
        public const uint Credits = 4800;

        public const double TempleRadius = 30, TempleHeight = 20;

        /// <summary>Hathen's copy and Algona, at the hospital (on the navmesh).</summary>
        public static readonly ScenePosition HathenAt = new(167.4f, 75.3f, -513.6f);
        public static readonly (double X, double Y, double Z) AlgonaAt = (160.0, 77.9, -515.6);

        /// <summary>The temples' centres (map_region 20176, 20178, 20179) and their five places, 7 m round (on the navmesh).</summary>
        public static readonly (uint Objective, uint Hidden, uint Name, uint Body, (double X, double Y, double Z) At, ScenePosition[] Group)[] Temples =
        {
            (Mausoleum, 5, 20841, 20842, (-115.2, 60.6, -488.8), new ScenePosition[]
            {
                new(-108.2f, 61.3f, -488.8f), new(-113f, 60.7f, -482.1f), new(-120.9f, 60.6f, -484.7f),
                new(-120.9f, 60.7f, -492.9f), new(-113f, 60.5f, -495.5f)
            }),
            (Shrine, 6, 20843, 20844, (-119.3, 82.1, -279.5), new ScenePosition[]
            {
                new(-112.3f, 82.2f, -279.5f), new(-117.1f, 82.9f, -272.8f), new(-125f, 82.1f, -275.4f),
                new(-125f, 82.2f, -283.6f), new(-117.1f, 83.2f, -286.2f)
            }),
            (Sanctuary, 7, 20845, 20846, (631.6, 49.9, -343.2), new ScenePosition[]
            {
                new(638.6f, 50f, -343.2f), new(633.8f, 50.1f, -336.5f), new(625.9f, 50f, -339.1f),
                new(625.9f, 50.1f, -347.3f), new(633.8f, 50.2f, -349.9f)
            })
        };

        public static IEnumerable<string> Creatures()
        {
            yield return Friend(HathenEscort, "Elder Hathen - Velon Hollow (These Old Bones escort)", HathenClass, HathenName,
                EscortNpcStats.Health, EscortSpeed, EscortWalk);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {HathenEscort}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Hathen};";
            yield return Stat(HathenEscort, EscortNpcStats.Health, EscortNpcStats.Armor);

            yield return Friend(Algona, "Warrior Algona - Velon Hollow", AlgonaClass, AlgonaName, 1000, 0, 0);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Algona}, slot_id, Class_id, color FROM creature_appearance WHERE id = {AlgonaOutfitOf};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Algona}, {AlgonaPackage}, 'Warrior Algona');";
            yield return Pool(AlgonaPool, AlgonaAt.X, AlgonaAt.Y, AlgonaAt.Z, MapContextId, Algona, 1, 1, 20, 0);
        }

        public static IEnumerable<string> RemoveCreatures()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {AlgonaPool};";
            yield return $"DELETE FROM npc_package WHERE id = {Algona};";
            yield return $"DELETE FROM creature_stat WHERE id = {HathenEscort};";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({HathenEscort}, {Algona});";
            yield return $"DELETE FROM creature WHERE id IN ({HathenEscort}, {Algona});";
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in Creatures())
                migration.Sql(sql);

            var mission = new PlateauMissionDataV1(migration, MissionId, "These Old Bones", 20808, Hathen, Algona, Level);
            mission.Reward(Experience, Credits, new[]
            {
                PlateauGraveyardShiftV1.BioGloves, PlateauGraveyardShiftV1.GravitonGloves,
                PlateauGraveyardShiftV1.MechGloves, PlateauGraveyardShiftV1.StealthGloves
            });
            mission.Prerequisite(PlateauVillageOfTheDamnedV1.MissionId);

            // 1-3 are the scene's (all five of a temple dead); 5-7, hidden, are walking into a temple.
            foreach (var temple in Temples)
                mission.Objective(temple.Objective, temple.Name, temple.Body, temple.Objective - 1, MissionObjectiveState.Incomplete);
            mission.Objective(Protect, 20847, 20848, 3, MissionObjectiveState.Incomplete);
            foreach (var temple in Temples)
                mission.Objective(temple.Hidden, temple.Name, temple.Body, temple.Hidden - 1, MissionObjectiveState.Incomplete);

            foreach (var temple in Temples)
            {
                mission.Scenario(temple.Objective, $"Temple {temple.Objective} reached", 0);
                mission.Area(temple.Objective, MapContextId, temple.At.X, temple.At.Y, temple.At.Z, TempleRadius, TempleHeight);
                mission.Transition(temple.Hidden);
                mission.EnterArea(temple.Hidden, temple.Objective);
                mission.Action(temple.Hidden, 1, MissionActionKind.StartScenario, scenario: temple.Objective);
            }
            mission.Scenario(Outcome, "The three temples secured", 1);

            // 4: the scene's signal with the third temple; its death fails it (PlateauTheseOldBonesScene).
            mission.Transition(Protect);
            mission.Progress(Protect, MissionProgressEventKind.ScenarioEvent, PlateauTheseOldBonesScene.Secured, null, counter: Outcome);
            mission.Action(Protect, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1976",
                "Native1 Mausoleum 20841/20842, 2 Shrine 20843/20844, 3 Sanctuary 20845/20846, 4 protect Hathen 20847/20848; no " +
                "counters, no objective conversations. Texts 20808-20812. Names 10209 Elder Hathen, 10513 Warrior Algona, 9859 " +
                "Infected Forean.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/These_Old_Bones",
                "Giver Elder Hathen (Velon Hollow), reward from Warrior Algona; follow-up Holy Vengeance; 45000XP, 4800 credits, " +
                "Prodigy/Titan Gloves.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauTheseOldBonesV1",
                "Level32, requires 1981. Hathen's copy650107 follows the player from the hospital; his death fails 4; after the " +
                "third temple he walks back and is gone. Each temple: within 30m of its centre, 4 Infected Foreans650024 and 1 " +
                "Thrax Soldier650070 at 7m; all five dead completes it. Algona650108 at the hospital. Gloves 46011, 24950, " +
                "34076, 40203. Places are estimates; the escort needs a joined navmesh.", 0.2);

            mission.Enable(Scene());
        }

        public static void Down(MigrationBuilder migration)
        {
            PlateauMissionDataV1.Remove(migration, MissionId);
            foreach (var sql in RemoveCreatures())
                migration.Sql(sql);
        }

        public static MissionSceneDefinition Scene()
        {
            var hostile = new ActorGameplayPolicy
            {
                RewardScenarioKills = true, TrackParticipation = true,
                // The run's own creature: no ordinary loot table can bind it, so none (as Foreas Base Past's).
                Loot = new AuthoredLootProfile(Array.Empty<LootDrop>())
            };
            var scene = new MissionSceneDefinition
            {
                Script = "plateau.these-old-bones",
                MapContextId = MapContextId,   // accepted at Hathen in the squad's Velon Hollow
                HiddenObjectiveIds = new() { 5, 6, 7 },
                Actors = new()
                {
                    [PlateauTheseOldBonesScene.Hathen] = new SceneActorDefinition(PlateauTheseOldBonesScene.Hathen,
                        SceneActorKind.Creature, HathenEscort, HathenAt, Orientation: 0, InitiallyInteractable: false)
                },
                Routes = new()
                {
                    [PlateauTheseOldBonesScene.Home] = new SceneRoute(PlateauTheseOldBonesScene.Home,
                        new[] { new SceneWaypoint(HathenAt) }, Speed: 2.5f)
                },
                DefeatSequences = new() { [PlateauTheseOldBonesScene.Hathen] = PlateauTheseOldBonesScene.HathenDown },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [PlateauTheseOldBonesScene.HathenDown] = new SceneSequenceDefinition()
                }
            };
            foreach (var temple in Temples)
            {
                scene.Sequences[temple.Objective] = new SceneSequenceDefinition();
                for (var index = 0; index < temple.Group.Length; index++)
                {
                    var role = PlateauTheseOldBonesScene.Role(temple.Objective, index);
                    var template = index < 4 ? PlateauInfectedForeansV1.Infected : PlateauVelonHollowBaneV1.Soldier;
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, template, temple.Group[index],
                        Orientation: index * 1.2566370614, GameplayPolicy: hostile);
                    var sequence = PlateauTheseOldBonesScene.DefeatSequence(temple.Objective, index);
                    scene.DefeatSequences[role] = sequence;
                    scene.Sequences[sequence] = new SceneSequenceDefinition();
                }
            }
            return scene;
        }

        private static string Friend(uint id, string comment, uint classId, uint nameId, uint hitPoints, uint run, uint walk) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"VALUES ({id}, '{comment}', {classId}, 1, {Level}, {hitPoints}, {nameId}, {run}, {walk}, 0, 0, 0, 0, 0, 0, 0, 0);";
    }
}
