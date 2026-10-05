using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;

namespace Rasa.Services.Preloader
{
    using Data;
    using Missions.Wilderness;
    using Structures.World;

    /// <summary>
    /// Wilderness Targets of Opportunity, mission 1449, was written twice: by
    /// TargetsOfOpportunitySeed (revision targets_1: four kill objectives and the "complete all"
    /// one, offered by radio) and by the Wilderness missions (WildernessAliaOpeningV1, revision
    /// wilderness_1_16_5: all twenty-six objectives - waypoints, Logos, the six officers, the
    /// caves, the stories - given by Lt Col Cimoch). A mission runs under one revision, and Game
    /// does not start with two enabled.
    ///
    /// The Wilderness missions' 1449 is the mission. What the other had that it has not goes onto
    /// its kill objectives 3, 4, 5 and 7, as the other fourteen battlefields have them:
    ///  - a kill counts by species - any creature whose class carries the creature flag - where
    ///    the mission named one creature row each (Bane Xanx 87, Bane Shield Drone 85, Thrax
    ///    Soldier 3, Bane Miasma 88);
    ///  - on the battlefield and the instances off it (TargetsOfOpportunitySeed.Wilderness.Maps);
    ///  - shared with the squad within TargetsOfOpportunitySeed.SquadRadius;
    ///  - and each gives its title: Wilderness Bug Zapper 365, Bot Stomper 366, Thrax Hunter 363,
    ///    Exterminator 369.
    /// The counts are the same on both: 40, 30, 200, 40.
    ///
    /// World: the targets_1 rows of 1449 are deleted - a database that ran
    /// Add_targets_of_opportunity before this has them, a new one never does - the four kill
    /// triggers change kind and subject, and the scene binding is the mission's own
    /// (<see cref="MissionScene"/>) with the titles, the credit and the maps added
    /// (<see cref="Scene"/>). Every statement is safe to repeat. Down puts the triggers and the
    /// binding back as the Wilderness missions wrote them; it does not bring the targets_1
    /// mission back, which could not run beside this one.
    ///
    /// Character: a character holding 1449 under targets_1 would have it set aside on every
    /// login ("pinned revision differs") and could never take the mission again, the row being
    /// in the way. Those assignments and offers are deleted (<see cref="CharUp"/>): the kill
    /// counts start again when the mission is taken from Cimoch. Titles already earned are the
    /// character's and stay. There is nothing for Down to put back.
    /// </summary>
    public static class WildernessTargetsKillRules
    {
        public const uint MissionId = 1449;

        /// <summary>The revision that runs: the Wilderness missions'.</summary>
        public const string Revision = WildernessMissionDataV1.Revision;

        /// <summary>The revision that goes: TargetsOfOpportunitySeed's.</summary>
        public const string SupersededRevision = TargetsOfOpportunitySeed.Revision;

        public static TargetsOfOpportunitySeed.Zone Zone => TargetsOfOpportunitySeed.Wilderness;

        /// <summary>The creature row each kill objective counted, as the Wilderness missions wrote it (WildernessAliaOpeningV1.Targets).</summary>
        public static readonly IReadOnlyDictionary<uint, uint> MissionCreatures = new Dictionary<uint, uint>
        {
            [3] = 87, [4] = 85, [5] = 3, [7] = 88
        };

        private static readonly uint[] Summaries = { 1, 3, 4, 5, 6, 7, 8, 40, 48, 55 };

        /// <summary>
        /// The story groups of objective 55, as WildernessAliaOpeningV1 has them. The binding is
        /// one document, so adding to it means writing all of it; a test holds this and
        /// <see cref="MissionScene"/> to what that migration inserts.
        /// </summary>
        private static readonly IReadOnlyList<uint>[] StoryGroups =
        {
            new uint[] { 421 }, new uint[] { 422 }, new uint[] { 425 }, new uint[] { 427 },
            new uint[] { 428 }, new uint[] { 429 }, new uint[] { 430 }, new uint[] { 431 },
            new uint[] { 432 }, new uint[] { 433 }, new uint[] { 434 }, new uint[] { 436 },
            new uint[] { 441 }, new uint[] { 442 }, new uint[] { 444 }, new uint[] { 451 },
            new uint[] { 479 }, new uint[] { 506 }, new uint[] { 508 }, new uint[] { 549 },
            new uint[] { 570 }, new uint[] { 574 }, new uint[] { 623, 791 }, new uint[] { 665 },
            new uint[] { 666 }, new uint[] { 679 }, new uint[] { 682 }, new uint[] { 695 },
            new uint[] { 696 }, new uint[] { 697 }, new uint[] { 698 }, new uint[] { 700, 820 },
            new uint[] { 701 }, new uint[] { 758 }, new uint[] { 769 }, new uint[] { 771 },
            new uint[] { 776 }, new uint[] { 787 }, new uint[] { 795 }, new uint[] { 860 },
            new uint[] { 1069 }, new uint[] { 1390 }, new uint[] { 1392, 1393 }, new uint[] { 1407 },
            new uint[] { 859 }, new uint[] { 489 }, new uint[] { 450 }, new uint[] { 593 },
            new uint[] { 674 }, new uint[] { 691 }, new uint[] { 692 }, new uint[] { 693 },
            new uint[] { 1054 }, new uint[] { 1055 }, new uint[] { 1059 }, new uint[] { 1056 },
            new uint[] { 711 }, new uint[] { 1065 }, new uint[] { 575 }, new uint[] { 1075 },
            new uint[] { 321 }, new uint[] { 323 }, new uint[] { 446 }
        };

        /// <summary>The scene binding of 1449 as the Wilderness missions install it.</summary>
        public static MissionSceneDefinition MissionScene() => new MissionSceneDefinition
        {
            ObjectiveAggregations = new()
            {
                [6] = new MissionObjectiveAggregation(new uint[] { 20, 21, 22, 23, 24, 25 }, 6, 0),
                [40] = new MissionObjectiveAggregation(new uint[] { 41, 46, 47 }, 3, 0),
                [48] = new MissionObjectiveAggregation(new uint[] { 49, 50, 51, 52, 53, 54 }, 6, 0),
                [58] = new MissionObjectiveAggregation(Summaries, 10)
            },
            ObjectiveHistoryAggregations = new()
            {
                [55] = new MissionHistoryAggregation(StoryGroups, counterId: 0)
            },
            HiddenObjectiveIds = new() { 20, 21, 22, 23, 24, 25, 41, 46, 47, 49, 50, 51, 52, 53, 54, 58 },
            ExistingFactObjectiveIds = new() { 1, 8 }
        };

        /// <summary>That binding with the kills' titles, maps and squad credit, as TargetsOfOpportunitySeed.Scene gives a battlefield's.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MissionScene();
            var rules = TargetsOfOpportunitySeed.Scene(Zone);

            scene.Titles = rules.Titles;
            scene.Credit = rules.Credit;
            scene.ObjectiveRequirements = rules.ObjectiveRequirements;

            return scene;
        }

        private static readonly string[] SupersededTables =
        {
            MissionEvidenceEntry.TableName, "mission_scene_binding", "mission_channel_policy",
            MissionActionEntry.TableName, MissionTriggerEntry.TableName, MissionObjectiveTransitionEntry.TableName,
            MissionObjectiveDefinitionEntry.TableName, MissionContentDefinitionEntry.TableName
        };

        public static IEnumerable<string> WorldUp
        {
            get
            {
                foreach (var table in SupersededTables)
                    yield return $"delete from {table} where mission_id = {MissionId} and content_revision = '{SupersededRevision}';";

                foreach (var kill in Zone.Kills)
                    yield return Trigger(kill.ObjectiveId, kill.EventKind, kill.Subjects.Single(),
                        $"Kill of creature flag {kill.Subjects.Single()}");

                yield return Binding(Scene());
            }
        }

        public static IEnumerable<string> WorldDown
        {
            get
            {
                foreach (var kill in Zone.Kills)
                    yield return Trigger(kill.ObjectiveId, MissionProgressEventKind.CreatureKilled, MissionCreatures[kill.ObjectiveId],
                        $"Objective {kill.ObjectiveId} trigger 1");

                yield return Binding(MissionScene());
            }
        }

        /// <summary>The assignments and offers of 1449 under the revision that went, children first.</summary>
        public static readonly string[] CharUp =
        {
            Children("character_mission_objective_counter"),
            Children("character_mission_objective_item_counter"),
            Children("character_mission_objective"),
            Children("character_mission_deadline"),
            Children("character_mission_scenario_step"),
            $"delete from character_mission_offer where mission_id = {MissionId} and content_revision = '{SupersededRevision}';",
            $"delete from character_mission where mission_id = {MissionId} and content_revision = '{SupersededRevision}';"
        };

        private static string Children(string table) =>
            $"delete from {table} where mission_id = {MissionId} and character_id in "
            + $"(select character_id from character_mission where mission_id = {MissionId} and content_revision = '{SupersededRevision}');";

        /// <summary>The one progress trigger of a kill objective of the mission: transition 1, trigger 1.</summary>
        private static string Trigger(uint objectiveId, MissionProgressEventKind kind, uint subject, string comment) =>
            $"update {MissionTriggerEntry.TableName} set event_kind = {(byte)kind}, subject_id = {subject}, comment = '{comment}' "
            + $"where mission_id = {MissionId} and content_revision = '{Revision}' and objective_id = {objectiveId} "
            + "and transition_id = 1 and trigger_id = 1;";

        /// <summary>The document holds numbers and plain names only: nothing a SQL string of either provider needs escaped.</summary>
        private static string Binding(MissionSceneDefinition scene) =>
            $"update mission_scene_binding set bindings = '{JsonSerializer.Serialize(scene, MissionContentCodec.Options)}' "
            + $"where mission_id = {MissionId} and content_revision = '{Revision}';";
    }
}
