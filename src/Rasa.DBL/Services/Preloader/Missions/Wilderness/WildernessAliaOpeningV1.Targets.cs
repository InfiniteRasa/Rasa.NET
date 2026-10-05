using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static partial class WildernessAliaOpeningV1
    {
        private static readonly uint[] Waypoints = { 49, 50, 51, 57, 61, 73, 156 };
        private static readonly uint[] Logos = { 1, 2, 6, 9, 10, 23, 24, 28, 38, 49, 53, 56 };
        private static readonly uint[] Summaries = { 1, 3, 4, 5, 6, 7, 8, 40, 48, 55 };
        // R reconstruction from the frozen w1-story-roster.json, not a recovered original story-only checklist.
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

        private static void Targets(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 1449, "Wilderness Targets of Opportunity",
                12764, 118, 118, 4);
            var objectives = new (uint Id, uint Name, uint Body, uint? Counter)[]
            {
                (1, 12769, 12770, 12789),
                (3, 12773, 12774, 12805),
                (4, 12775, 12776, 12808),
                (5, 12771, 12772, 12802),
                (6, 12777, 12778, 12809),
                (7, 12779, 12780, 12824),
                (8, 12781, 12782, null),
                (20, 12810, 12811, null),
                (21, 12812, 12813, null),
                (22, 12814, 12815, null),
                (23, 12816, 12817, null),
                (24, 12818, 12819, null),
                (25, 12820, 12821, null),
                (40, 12852, 12853, 12854),
                (41, 12855, 12856, null),
                (46, 12859, 12860, null),
                (47, 12861, 12862, null),
                (48, 12863, 12864, 12865),
                (49, 12866, 12867, null),
                (50, 12868, 12869, null),
                (51, 12870, 12871, null),
                (52, 12872, 12873, null),
                (53, 12874, 12875, null),
                (54, 12876, 12877, null),
                (55, 13213, 13214, 13685),
                (58, 17853, 17854, null)
            };
            uint ordinal = 0;
            foreach (var objective in objectives)
                mission.Objective(objective.Id, objective.Name, objective.Body, ordinal++,
                    objective.Id is 41 or 46 or 47 ? MissionObjectiveState.Inactive : MissionObjectiveState.Incomplete,
                    required: Summaries.Contains(objective.Id) || objective.Id == 58,
                    counterTextId: objective.Counter);

            mission.Transition(1);
            for (var index = 0; index < Waypoints.Length; index++)
                mission.Progress(1, MissionProgressEventKind.WaypointAcquired, Waypoints[index],
                    counter: 0, target: (uint)Waypoints.Length, trigger: (uint)index + 1);
            mission.Transition(8);
            for (var index = 0; index < Logos.Length; index++)
                mission.Progress(8, MissionProgressEventKind.LogosAcquired, Logos[index], trigger: (uint)index + 1);

            foreach (var (objective, creature, target) in new (uint, uint, uint)[]
            {
                (3, 87, 40), (4, 85, 30), (5, 3, 200), (7, 88, 40)
            })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, creature, counter: 0, target: target);
            }
            foreach (var (objective, creature) in new (uint, uint)[]
            {
                (20, 82), (21, 83), (22, 84), (23, 79), (24, 80), (25, 75)
            })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, creature);
            }

            Cave(mission, 49, 49, 816, 281.72, 615);
            Cave(mission, 50, 50, -797.7596, 263.3995, 761.2568);
            Cave(mission, 51, 51, -869, 172.1378, -592);
            Cave(mission, 52, 52, 372.8438, 213.9441, 517.8008);
            Cave(mission, 53, 53, -486.5, 213.8642, -402.8);
            Cave(mission, 54, 541, -490.543, 220.3178, -607.2);
            Cave(mission, 54, 542, -743, 173.3677, -571, transition: 2);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1449",
                "All26 native objective identities retained; real instance goals40/55/58 remain unfinished.");
            mission.Evidence(2, MissionEvidenceSourceKind.Server, "repository:MissionDefinitionCatalog/1449",
                "Preserved seven waypoint and twelve Logos identities; Earth408 excluded.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction,
                "https://tabularasa.fandom.com/wiki/Wilderness_Targets_of_Opportunity",
                "Caves49-54 mapped to sourced six members with bounded nav-sampled entrance volumes; two unnamed-cave entrances count once.");
            mission.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Final payout not enabled until genuine instance requirements and native clone/title rewards are implemented.");
            mission.Evidence(5, MissionEvidenceSourceKind.Reconstruction,
                "https://tabularasa.fandom.com/wiki/Wilderness_Targets_of_Opportunity",
                "R selection: frozen63 groups,44 outdoor and19 instance-dependent; OR1392/1393,623/791,700/820. Sources provide no tagged story-only subset; this is not the recovered original checklist.");
            mission.Evidence(6, MissionEvidenceSourceKind.Reconstruction,
                "native:missionconversation+missiontextlanguage@167d2b88b48e040c5e244e6f932518c52638f4f9",
                "Roster candidates are native-ID-mapped from Wilderness, Donn, Crater Lake and Pravus lists; unavailable instance groups remain required. Counter0 keeps native empty label13685 and separate numeric rendering.");
            mission.Enable(new MissionSceneDefinition
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
            });
        }

        private static void Cave(WildernessMissionDataV1 mission, uint objective, uint area,
            double x, double y, double z, uint transition = 1)
        {
            mission.Area(area, x, y, z, radius: 7, height: 3);
            mission.Transition(objective, transition);
            mission.EnterArea(objective, area, transition);
        }
    }
}
