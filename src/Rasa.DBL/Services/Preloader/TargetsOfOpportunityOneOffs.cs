using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;

namespace Rasa.Services.Preloader
{
    using Data;
    using Structures.World;

    public static partial class TargetsOfOpportunitySeed
    {
        /// <summary>
        /// Four titles that are one of a kind, each on an objective of a Targets of Opportunity
        /// mission that is seeded already: the rows of Oneoff_titles, as SQL. <see cref="Zones"/>
        /// is what Add_targets_of_opportunity inserted and stays as it was; what is added to its
        /// missions since is here.
        ///
        ///  - Wilderness Spelunker (372), "Visited every cave on Wilderness.": objective 48 of
        ///    1449, "Visit All Wilderness Caves!", its body "Spelunker: Visit every cave on
        ///    Wilderness." The Wilderness missions count the six caves already (objectives 49 to
        ///    54); the title goes into the scene binding, and nothing else changes.
        ///  - Mires Explorer (735), "Entered all Operations available on Mires.": objective 40 of
        ///    1585, the sum of 56, 57 and 58 - "Entered the Fluxite Mines", "Entered Casso
        ///    Weapons Facility", "Entered Tahrendra Base". Each is an area that is the whole of
        ///    its instance (MissionAreaShape.Map), entered by arriving there. The
        ///    objective's own name is "Complete 3 Mires Operations!" and its counter "Operations
        ///    Completed"; what it counts are the three that say "Entered", and the title of the
        ///    Mires titles (728 to 735, one an objective of this mission) that says so too.
        ///  - Undertaker (724), "Killed 100 Infected Foreans on Howling Maw.": objective 74 of
        ///    1752, a kill counter like the mission's others, by class - the infected Forean
        ///    gunners (25607, 25618, 25882), which carry no flag that is theirs alone. None of
        ///    them stands in the world yet; it counts from the day one does.
        ///  - Palisades Stalker Killer (510), "Killed 5 Stalkers in Palisades": its objective,
        ///    64, is in the earlier Palisades mission (1630) and not in the one that runs (1809),
        ///    so the client has no text to show for it there. It is counted all the same, as a
        ///    hidden objective of 1809: nothing in the log, and the title when the fifth falls.
        ///    No Stalker stands in the Palisades yet either.
        ///
        /// World: the objectives, their transitions, triggers and actions, the three areas, a row
        /// of evidence for each mission added to, and the four scene bindings written again
        /// with what is added. Every insert is of rows this alone inserts, and Down deletes them
        /// and puts the bindings back.
        ///
        /// Character: a mission held with fewer objectives than its definition is cleared on
        /// login ("objective state is incomplete or invalid"), so every character holding 1585,
        /// 1752 or 1809 is given the new objectives' rows, uncounted (<see cref="CharUp"/>);
        /// their other counts stay. And one who has visited every cave already is given the
        /// Spelunker, which nothing would otherwise give them: the objective is complete and
        /// does not complete again. Down takes the objective rows out; titles stay.
        /// </summary>
        public static class OneOffs
        {
            /// <summary>A kill objective added to one of <see cref="Zones"/>' missions.</summary>
            public sealed class AddedKill
            {
                public uint MissionId { get; }
                public Kill Kill { get; }

                /// <summary>False where the client has no texts for the objective under this mission: counted, and not shown.</summary>
                public bool Shown { get; }

                internal AddedKill(uint missionId, bool shown, Kill kill)
                {
                    MissionId = missionId;
                    Shown = shown;
                    Kill = kill;
                }
            }

            /// <summary>An operation of the Mires: the objective that says it was entered, and the area that is its map.</summary>
            public sealed class Operation
            {
                public uint ObjectiveId { get; }
                public uint NameTextId { get; }
                public uint BodyTextId { get; }
                public string Name { get; }
                public uint MapContextId { get; }

                /// <summary>The area's id, the mission's own: the objective's.</summary>
                public uint AreaId => ObjectiveId;

                internal Operation(uint objectiveId, uint nameTextId, uint bodyTextId, string name, uint mapContextId)
                {
                    ObjectiveId = objectiveId;
                    NameTextId = nameTextId;
                    BodyTextId = bodyTextId;
                    Name = name;
                    MapContextId = mapContextId;
                }
            }

            public const uint WildernessMissionId = 1449;
            public const uint VisitAllCaves = 48;
            public const uint Spelunker = 372;

            public const uint MiresMissionId = 1585;
            public const uint MiresOperations = 40;
            public const uint MiresOperationsNameTextId = 14746;
            public const uint MiresOperationsBodyTextId = 14747;
            public const uint MiresOperationsCounterTextId = 14748;
            public const uint MiresExplorer = 735;

            public static readonly Operation[] Operations =
            {
                new Operation(56, 14801, 14802, "Fluxite Mines", 2115),
                new Operation(57, 14803, 14804, "Casso Weapons Facility", 2107),
                new Operation(58, 14805, 14806, "Tahrendra Base", 2125)
            };

            public static readonly AddedKill[] Kills =
            {
                new AddedKill(1752, true, Classes(74, 17299, 17300, 17301, 100, 724, "Undertaker", 25607, 25618, 25882)),
                new AddedKill(1809, false, Flags(64, 15574, 15575, 15576, 5, 510, "Palisades Stalker Killer", Flag.Stalker))
            };

            /// <summary>The missions of <see cref="Zones"/> that gain objectives, and which.</summary>
            public static IReadOnlyDictionary<uint, uint[]> AddedObjectives => Kills
                .Select(added => (Mission: added.MissionId, Objective: added.Kill.ObjectiveId))
                .Append((Mission: MiresMissionId, Objective: MiresOperations))
                .Concat(Operations.Select(operation => (Mission: MiresMissionId, Objective: operation.ObjectiveId)))
                .GroupBy(pair => pair.Mission, pair => pair.Objective)
                .ToDictionary(group => group.Key, group => group.OrderBy(id => id).ToArray());

            private static Zone ZoneOf(uint missionId) => Zones.Single(zone => zone.MissionId == missionId);

            /// <summary>The scene binding of one of the three missions with what is added to it: <see cref="TargetsOfOpportunitySeed.Scene"/> and more.</summary>
            public static MissionSceneDefinition Scene(uint missionId)
            {
                var zone = ZoneOf(missionId);
                var scene = TargetsOfOpportunitySeed.Scene(zone);
                var hidden = new List<uint>();

                foreach (var added in Kills.Where(added => added.MissionId == missionId))
                {
                    scene.Titles[added.Kill.ObjectiveId] = added.Kill.TitleId;
                    scene.Credit[added.Kill.ObjectiveId] = new MissionCreditPolicy(MissionCreditMode.NearbyParty, SquadRadius);
                    scene.ObjectiveRequirements[added.Kill.ObjectiveId] = new MapRequirement(zone.Maps);

                    if (!added.Shown)
                        hidden.Add(added.Kill.ObjectiveId);
                }

                if (missionId == MiresMissionId)
                {
                    scene.Titles[MiresOperations] = MiresExplorer;
                    scene.ObjectiveAggregations = new Dictionary<uint, MissionObjectiveAggregation>
                    {
                        [MiresOperations] = new MissionObjectiveAggregation(
                            Operations.Select(operation => operation.ObjectiveId).ToArray(), (uint)Operations.Length, 0)
                    };
                    hidden.AddRange(Operations.Select(operation => operation.ObjectiveId));
                }

                if (hidden.Count > 0)
                    scene.HiddenObjectiveIds = hidden;

                return scene;
            }

            /// <summary>The scene binding of 1449, as WildernessTargetsKillRules leaves it, with the caves' title.</summary>
            public static MissionSceneDefinition WildernessScene()
            {
                var scene = WildernessTargetsKillRules.Scene();

                scene.Titles[VisitAllCaves] = Spelunker;
                return scene;
            }

            private static readonly string[] AreaColumns =
            {
                "mission_id", "content_revision", "area_id", "requirement", "map_context_id", "shape",
                "pos_x", "pos_y", "pos_z", "radius", "extent_x", "extent_y", "extent_z", "comment"
            };

            private static readonly string[] AreaTriggerColumns =
            {
                "mission_id", "content_revision", "objective_id", "transition_id", "trigger_id", "requirement", "kind", "sequence",
                "area_id", "comment"
            };

            private const uint EvidenceId = 4;

            /// <summary>The ordinal of a mission's first added objective: after "complete all" (1) and its kills (2 on).</summary>
            private static uint FirstOrdinal(uint missionId) => (uint)ZoneOf(missionId).Kills.Count + 2;

            public static object[][] Objectives
            {
                get
                {
                    var rows = new List<object[]>();

                    foreach (var mission in AddedObjectives.Keys.OrderBy(id => id))
                    {
                        var ordinal = FirstOrdinal(mission);

                        if (mission == MiresMissionId)
                        {
                            rows.Add(new object[]
                            {
                                mission, Revision, MiresOperations, Required, MiresOperationsNameTextId, MiresOperationsBodyTextId,
                                MiresOperationsCounterTextId, null, null, ordinal++, Incomplete, 0, "Mires Explorer"
                            });

                            foreach (var operation in Operations)
                                rows.Add(new object[]
                                {
                                    mission, Revision, operation.ObjectiveId, Required, operation.NameTextId, operation.BodyTextId,
                                    null, null, null, ordinal++, Incomplete, 0, $"Entered {operation.Name}"
                                });
                        }

                        foreach (var added in Kills.Where(added => added.MissionId == mission))
                            rows.Add(new object[]
                            {
                                mission, Revision, added.Kill.ObjectiveId, Required, added.Kill.NameTextId, added.Kill.BodyTextId,
                                added.Kill.CounterTextId, null, null, ordinal++, Incomplete, 0, added.Kill.Title
                            });
                    }

                    return rows.ToArray();
                }
            }

            public static object[][] Transitions => Kills.Select(added => new object[]
                {
                    added.MissionId, Revision, added.Kill.ObjectiveId, 1u, Required, 1u, Incomplete, Completed, $"{added.Kill.Count} killed"
                })
                .Concat(Operations.Select(operation => new object[]
                {
                    MiresMissionId, Revision, operation.ObjectiveId, 1u, Required, 1u, Incomplete, Completed, $"Entered {operation.Name}"
                })).ToArray();

            /// <summary>The kill triggers, one a subject, as <see cref="TargetsOfOpportunitySeed.Triggers"/> has a battlefield's.</summary>
            public static object[][] KillTriggers => Kills.SelectMany(added => added.Kill.Subjects.Select((subject, index) => new object[]
            {
                added.MissionId, Revision, added.Kill.ObjectiveId, 1u, (uint)index + 1, Required, (byte)MissionTriggerKind.ProgressEvent, (uint)index + 1,
                (byte)added.Kill.EventKind, subject, 0u, 0u, added.Kill.Count,
                added.Kill.EventKind == MissionProgressEventKind.CreatureFlagKilled ? $"Kill of creature flag {subject}" : $"Kill of creature class {subject}"
            })).ToArray();

            public static object[][] AreaTriggers => Operations.Select(operation => new object[]
            {
                MiresMissionId, Revision, operation.ObjectiveId, 1u, 1u, Required, (byte)MissionTriggerKind.AreaEntered, 1u,
                operation.AreaId, $"On map {operation.MapContextId}"
            }).ToArray();

            public static object[][] Actions => Kills.Select(added => new object[]
            {
                added.MissionId, Revision, added.Kill.ObjectiveId, 1u, 1u, Required, (byte)MissionActionKind.CompleteObjective, 1u,
                added.Kill.ObjectiveId, Completed, $"Complete {added.Kill.Title}"
            }).ToArray();

            /// <summary>The whole of each operation's map: a position is a column to fill, and is not read.</summary>
            public static object[][] Areas => Operations.Select(operation => new object[]
            {
                MiresMissionId, Revision, operation.AreaId, Required, operation.MapContextId, (byte)MissionAreaShape.Map,
                0.0, 0.0, 0.0, null, null, null, null, $"All of {operation.Name}"
            }).ToArray();

            public static object[][] Evidence => new[]
            {
                new object[]
                {
                    MiresMissionId, Revision, EvidenceId, (byte)MissionEvidenceOwnerKind.Mission, MiresMissionId, (byte)MissionEvidenceSourceKind.Reconstruction,
                    null, "generated/client/missionobjective.pyo, titledata.pyo", 0.6,
                    "objective 40 summed from 56-58, which the client names Entered: each the whole of its instance map, entered on arrival; title 735 Mires Explorer"
                },
                new object[]
                {
                    1752u, Revision, EvidenceId, (byte)MissionEvidenceOwnerKind.Mission, 1752u, (byte)MissionEvidenceSourceKind.Reconstruction,
                    null, "generated/client/missionobjective.pyo, titledata.pyo", 0.7,
                    "objective 74 counted by the infected Forean classes; none stands in the world yet; title 724 Undertaker"
                },
                new object[]
                {
                    1809u, Revision, EvidenceId, (byte)MissionEvidenceOwnerKind.Mission, 1809u, (byte)MissionEvidenceSourceKind.Reconstruction,
                    null, "generated/client/missionobjective.pyo (mission 1630), titledata.pyo", 0.4,
                    "objective 64 of the earlier Palisades mission 1630, absent from 1809 in the client: counted hidden; title 510 Palisades Stalker Killer"
                }
            };

            public static IEnumerable<string> WorldUp
            {
                get
                {
                    yield return Insert(MissionObjectiveDefinitionEntry.TableName, ObjectiveColumns, Objectives);
                    yield return Insert(MissionObjectiveTransitionEntry.TableName, TransitionColumns, Transitions);
                    yield return Insert(MissionAreaEntry.TableName, AreaColumns, Areas);
                    yield return Insert(MissionTriggerEntry.TableName, TriggerColumns, KillTriggers);
                    yield return Insert(MissionTriggerEntry.TableName, AreaTriggerColumns, AreaTriggers);
                    yield return Insert(MissionActionEntry.TableName, ActionColumns, Actions);
                    yield return Insert(MissionEvidenceEntry.TableName, EvidenceColumns, Evidence);

                    foreach (var mission in AddedObjectives.Keys.OrderBy(id => id))
                        yield return Binding(mission, Revision, Scene(mission));

                    yield return Binding(WildernessMissionId, WildernessTargetsKillRules.Revision, WildernessScene());
                }
            }

            public static IEnumerable<string> WorldDown
            {
                get
                {
                    yield return Binding(WildernessMissionId, WildernessTargetsKillRules.Revision, WildernessTargetsKillRules.Scene());

                    foreach (var (mission, objectives) in AddedObjectives.OrderBy(pair => pair.Key))
                    {
                        var ids = string.Join(", ", objectives);

                        yield return Binding(mission, Revision, TargetsOfOpportunitySeed.Scene(ZoneOf(mission)));
                        yield return $"delete from {MissionEvidenceEntry.TableName} where mission_id = {mission} and content_revision = '{Revision}' and evidence_id = {EvidenceId};";

                        foreach (var table in new[]
                        {
                            MissionActionEntry.TableName, MissionTriggerEntry.TableName, MissionObjectiveTransitionEntry.TableName,
                            MissionObjectiveDefinitionEntry.TableName
                        })
                            yield return $"delete from {table} where mission_id = {mission} and content_revision = '{Revision}' and objective_id in ({ids});";
                    }

                    yield return $"delete from {MissionAreaEntry.TableName} where mission_id = {MiresMissionId} and content_revision = '{Revision}' "
                        + $"and area_id in ({string.Join(", ", Operations.Select(operation => operation.AreaId))});";
                }
            }

            /// <summary>
            /// The new objectives' rows for every character holding one of the three missions, as
            /// taking the mission would have written them: Incomplete, and counter 0 at 0 where
            /// the objective has one. Each skips a character that has the row, so it may be run
            /// twice - by a join to the row that is not there, the table inserted into being one
            /// MySQL does not let a subquery of its own insert read. Then the Spelunker for a
            /// character whose caves are all visited.
            /// </summary>
            public static IEnumerable<string> CharUp
            {
                get
                {
                    var counted = Kills.Select(added => (added.MissionId, added.Kill.ObjectiveId)).Append((MiresMissionId, MiresOperations)).ToHashSet();

                    foreach (var (mission, objectives) in AddedObjectives.OrderBy(pair => pair.Key))
                        foreach (var objective in objectives)
                        {
                            yield return "insert into character_mission_objective (character_id, mission_id, objective_id, objective_state) "
                                + $"select m.character_id, m.mission_id, {objective}, {Incomplete} from character_mission m "
                                + "left join character_mission_objective o on o.character_id = m.character_id "
                                + $"and o.mission_id = m.mission_id and o.objective_id = {objective} "
                                + $"where m.mission_id = {mission} and m.content_revision = '{Revision}' and o.character_id is null;";

                            if (counted.Contains((mission, objective)))
                                yield return "insert into character_mission_objective_counter (character_id, mission_id, objective_id, counter_id, counter_value) "
                                    + $"select m.character_id, m.mission_id, {objective}, 0, 0 from character_mission m "
                                    + "left join character_mission_objective_counter c on c.character_id = m.character_id "
                                    + $"and c.mission_id = m.mission_id and c.objective_id = {objective} and c.counter_id = 0 "
                                    + $"where m.mission_id = {mission} and m.content_revision = '{Revision}' and c.character_id is null;";
                        }

                    yield return "insert into character_title (character_id, title_id) "
                        + $"select o.character_id, {Spelunker} from character_mission_objective o "
                        + $"left join character_title t on t.character_id = o.character_id and t.title_id = {Spelunker} "
                        + $"where o.mission_id = {WildernessMissionId} and o.objective_id = {VisitAllCaves} and o.objective_state = {Completed} "
                        + "and t.character_id is null;";
                }
            }

            /// <summary>The objective rows out again, counters first, for a world that has gone back.</summary>
            public static IEnumerable<string> CharDown
            {
                get
                {
                    foreach (var (mission, objectives) in AddedObjectives.OrderBy(pair => pair.Key))
                        foreach (var table in new[] { "character_mission_objective_counter", "character_mission_objective" })
                            yield return $"delete from {table} where mission_id = {mission} and objective_id in ({string.Join(", ", objectives)}) "
                                + $"and character_id in (select character_id from character_mission where mission_id = {mission} and content_revision = '{Revision}');";
                }
            }

            /// <summary>The document holds numbers and plain names only; one that held a quote or a backslash would not go into a SQL string as it is.</summary>
            private static string Binding(uint missionId, string revision, MissionSceneDefinition scene)
            {
                var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);

                if (json.Contains('\'') || json.Contains('\\'))
                    throw new InvalidOperationException($"The scene binding of mission {missionId} cannot be written as a SQL string.");

                return $"update {SceneBindingTable} set bindings = '{json}' where mission_id = {missionId} and content_revision = '{revision}';";
            }
        }
    }
}
