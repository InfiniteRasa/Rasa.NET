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
    /// Holy Vengeance (native mission 1977), Velon Hollow (squad instance 2136): Warrior Algona and two warriors go with
    /// the player to clear the Bane outpost and take back the Law of Asperion, which goes to Luminary Sampei at New Velon.
    ///
    /// Native: texts 20813-20817 (missionconversation; offer "Me and a few selected warriors will go with you");
    /// objectives 1 "Secure outer Bane Outpost" 20883/20884, 2 "Clear inner Bane Outpost" 20885/20886, 3 "Recover the
    /// Law of Asperion" 20887/20888 ("believed to be stored inside the Bane Outpost"), 4 "(Hidden) Alonga Escort
    /// Scripting" 21162/21163; no counters, no objective conversations (missionobjective). Item
    /// Hollow_Mission_Law_of_Asperion (template 122242).
    ///
    /// Sourced: TaRapedia Holy Vengeance (Velon Hollow mission list): giver Warrior Algona, requires These Old Bones,
    /// reward from Luminary Sampei; 45,000 XP, 4,800 credits, Purifier Legs.
    ///
    /// Reconstructed (ESTIMATE, the user's decisions of 2026-10-22 and 2026-10-24):
    ///  - Level 32, as the other Velon Hollow missions.
    ///  - 1: the outer outpost's pools (531225, 531226) cleared in the squad's copy; 2: the inner ones (531224, the
    ///    control room's 650075 and the tunnel's 650076). Pools already dead when the mission is taken count.
    ///  - 3 opens with 2: the book in a Bane crate (class 11116, as Weapons Trafficking's) in the control room; the
    ///    crate gives it while 3 is open. Sampei takes it at the turn-in.
    ///  - 4, hidden: from the accept, a copy of Algona and two Forean warriors follow the player; their deaths fail
    ///    nothing. With the book they walk back to the hospital and are gone there (PlateauHolyVengeanceScene). 4 is
    ///    done with 3.
    ///  - Rewards: the four V06 Rare legs of level 30-34, the Purifier set of Better Half's helmets and Mountain
    ///    Spring Toxins' vests.
    ///  - The followers walk the navmesh: on the shipped Velon Hollow mesh the hospital and the control room are
    ///    islands (an upstream navmesh bug report). No indicators: instance.
    ///
    /// Ids: creatures 650109 (Algona's escort copy) and 650110 (a Forean warrior), free before.
    /// </summary>
    public static class PlateauHolyVengeanceV1
    {
        public const uint MissionId = PlateauHolyVengeanceScene.MissionId;
        public const uint Level = 32;
        public const uint MapContextId = PlateauVelonHollowBaneV1.MapContextId;
        public const uint Algona = PlateauTheseOldBonesV1.Algona, Sampei = PlateauVillageOfTheDamnedV1.Sampei;
        public const uint Outer = 1, Inner = 2, Recover = PlateauHolyVengeanceScene.Recover, Escort = 4;
        public const uint Book = PlateauHolyVengeanceScene.Book;

        public const uint AlgonaEscort = 650109, Warrior = 650110;
        public const uint WarriorClass = 6043, WarriorOutfitOf = 400305;   // Proving Grounds front Forean Warrior, spear
        public const uint SpearAction = 5;              // that warrior's action

        public const uint BookTemplate = PlateauHolyVengeanceScene.BookTemplateId, BookClass = 29315;
        public const uint CrateClass = PlateauWeaponsTraffickingV1.CrateClass;

        public static readonly uint[] OuterPools = { 531225, 531226 };
        public static readonly uint[] InnerPools = { 531224, PlateauVelonHollowBaneV1.ControlRoomPool, PlateauVelonHollowBaneV1.TunnelPool };

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint GravitonLegs = 122257, BioLegs = 122259, MechLegs = 122261, StealthLegs = 122263;

        /// <summary>The crate, in the control room (on the navmesh, 14 m from Mountain Spring Toxins' engine).</summary>
        public static readonly ScenePosition CrateAt = new(452, 33.2f, 112);

        /// <summary>
        /// Algona's copy and the two warriors at the hospital (on the navmesh). The copy stands in the public Algona's place
        /// (pool 650108's row, 162.4, 77.1, -516.4, facing 0; the local navmesh is at 77.10 there).
        /// </summary>
        public static readonly ScenePosition[] FollowersAt =
        {
            new((float)PlateauTheseOldBonesV1.AlgonaAt.X, (float)PlateauTheseOldBonesV1.AlgonaAt.Y, (float)PlateauTheseOldBonesV1.AlgonaAt.Z),
            new(169.4f, 77.9f, -522.4f), new(167.4f, 79f, -527f)
        };

        /// <summary>
        /// Where each follower's walk back from the crate ends and he is gone: corners of the local navmesh path from the
        /// crate towards the hospital, about 100 m along it (ESTIMATE 100 m, the user's rule for 1976, taken for 1977 by the
        /// main session 2026-10-25; path 98, 97, 91 m of about 2440 m).
        /// </summary>
        public static readonly ScenePosition[] WayBackAt =
        {
            new(384.8f, 34.3f, 75.6f), new(386f, 34.3f, 75.6f), new(391.6f, 34.3f, 74.8f)
        };

        public static IEnumerable<string> Creatures()
        {
            yield return Friend(AlgonaEscort, "Warrior Algona - Velon Hollow (Holy Vengeance escort)", PlateauTheseOldBonesV1.AlgonaClass,
                PlateauTheseOldBonesV1.AlgonaName);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {AlgonaEscort}, slot_id, Class_id, color FROM creature_appearance WHERE id = {PlateauTheseOldBonesV1.AlgonaOutfitOf};";
            yield return Stat(AlgonaEscort, EscortNpcStats.Health, EscortNpcStats.Armor);

            yield return Friend(Warrior, "Forean Warrior - Velon Hollow (Holy Vengeance escort)", WarriorClass, 0);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Warrior}, slot_id, Class_id, color FROM creature_appearance WHERE id = {WarriorOutfitOf};";
            yield return Stat(Warrior, EscortNpcStats.Health, EscortNpcStats.Armor);
        }

        public static IEnumerable<string> RemoveCreatures()
        {
            yield return $"DELETE FROM creature_stat WHERE id IN ({AlgonaEscort}, {Warrior});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({AlgonaEscort}, {Warrior});";
            yield return $"DELETE FROM creature WHERE id IN ({AlgonaEscort}, {Warrior});";
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in Creatures())
                migration.Sql(sql);

            var mission = new PlateauMissionDataV1(migration, MissionId, "Holy Vengeance", 20813, Algona, Sampei, Level);
            mission.Reward(Experience, Credits, new[] { GravitonLegs, BioLegs, MechLegs, StealthLegs });
            mission.Prerequisite(PlateauTheseOldBonesV1.MissionId);

            mission.Objective(Outer, 20883, 20884, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Inner, 20885, 20886, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Recover, 20887, 20888, 2, MissionObjectiveState.Inactive);
            mission.Objective(Escort, 21162, 21163, 3, MissionObjectiveState.Incomplete);
            mission.Scenario(Book, "The Law of Asperion taken from the crate", 1);

            // 1, 2: every pool of the part cleared at once in the squad's copy (SpawnPoolCleared, an all-pools set).
            mission.Transition(Outer);
            for (var index = 0; index < OuterPools.Length; index++)
                mission.Progress(Outer, MissionProgressEventKind.SpawnPoolCleared, OuterPools[index], null, trigger: (uint)index + 1);
            mission.Transition(Inner);
            for (var index = 0; index < InnerPools.Length; index++)
                mission.Progress(Inner, MissionProgressEventKind.SpawnPoolCleared, InnerPools[index], null, trigger: (uint)index + 1);
            PlateauSilentButDeadlyV1.Open(mission, Inner, 1, Recover);

            mission.Transition(Recover);
            mission.Progress(Recover, MissionProgressEventKind.ScenarioEvent, PlateauHolyVengeanceScene.Taken, null, counter: Book);

            // 4, hidden: done with 3; Sampei takes the book at the turn-in.
            mission.Transition(Escort);
            mission.ObjectiveState(Escort, Recover, MissionObjectiveState.Completed);
            mission.Action(Escort, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1977",
                "Native1 outer outpost 20883/20884, 2 inner outpost 20885/20886, 3 Law of Asperion 20887/20888, 4 hidden escort " +
                "21162/21163; no counters, no objective conversations. Texts 20813-20817. Item Hollow_Mission_Law_of_Asperion " +
                "template122242.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Holy_Vengeance",
                "Giver Warrior Algona (Velon Hollow), requires These Old Bones, reward from Luminary Sampei; 45000XP, 4800 " +
                "credits, Purifier Legs.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauHolyVengeanceV1",
                "Level32. 1 pools 531225/531226 cleared, 2 pools 531224/650075/650076 cleared (squad copy); 3 a Bane crate11116 " +
                "in the control room (452,33.2,112) gives 122242 while 3 is open; 4 hidden, Algona650109 and 2 warriors650110 " +
                "follow, walk back with the book. Legs 122257/122259/122261/122263. Places are estimates; the followers need a " +
                "joined navmesh.", 0.2);

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
            var scene = new MissionSceneDefinition
            {
                Script = "plateau.holy-vengeance",
                MapContextId = MapContextId,   // accepted at Algona in the squad's Velon Hollow
                HiddenObjectiveIds = new() { Escort },
                ExistingFactObjectiveIds = new() { Outer, Inner },
                Items = new()
                {
                    new MissionItemBinding(PlateauHolyVengeanceScene.BookKey, BookTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                },
                Actors = new()
                {
                    [PlateauHolyVengeanceScene.Crate] = new SceneActorDefinition(PlateauHolyVengeanceScene.Crate, SceneActorKind.Object,
                        CrateClass, CrateAt, Orientation: 0, InitialObjectState: PlateauHolyVengeanceScene.ReadyState,
                        UseAction: new SceneObjectAction(MissionId, Recover, PlateauHolyVengeanceScene.BookTaken, PlateauHolyVengeanceScene.UseArgument))
                },
                Routes = new(),
                DefeatSequences = new(),
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [PlateauHolyVengeanceScene.BookTaken] = new SceneSequenceDefinition()
                }
            };
            for (var index = 0; index < PlateauHolyVengeanceScene.Followers.Length; index++)
            {
                var role = PlateauHolyVengeanceScene.Followers[index];
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, index == 0 ? AlgonaEscort : Warrior,
                    FollowersAt[index], Orientation: 0, InitiallyInteractable: false);
                scene.Routes[PlateauHolyVengeanceScene.Home(index)] = new SceneRoute(PlateauHolyVengeanceScene.Home(index),
                    new[] { new SceneWaypoint(WayBackAt[index]) }, Speed: 2.5f);
                var down = PlateauHolyVengeanceScene.DownFrom + 1 + (uint)index;
                scene.DefeatSequences[role] = down;
                scene.Sequences[down] = new SceneSequenceDefinition();
            }
            return scene;
        }

        private static string Friend(uint id, string comment, uint classId, uint nameId) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"VALUES ({id}, '{comment}', {classId}, 1, {Level}, {EscortNpcStats.Health}, {nameId}, " +
            $"{PlateauTheseOldBonesV1.EscortSpeed}, {PlateauTheseOldBonesV1.EscortWalk}, {SpearAction}, 0, 0, 0, 0, 0, 0, 0);";
    }
}
