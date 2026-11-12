using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Rows of one Ligo Crucible mission (revision crucible_1_16_5, category 1 "Battlefield (Crucible)",
    /// missioncategorylanguage), as MarshesMissionDataV1 writes the Marshes ones, every row Required, in plain SQL
    /// (<see cref="CrucibleSql"/>): no EF data operation, so no Up or Down depends on a migration's model.
    /// Fixed once a migration uses it: a later change to a mission goes in a new migration.
    /// </summary>
    public sealed class CrucibleMissionDataV1
    {
        public const string Revision = "crucible_1_16_5";
        public const uint MapContextId = 1993;          // adv_arieki_ligo_burningsteps
        public const uint CategoryId = 1;               // "Battlefield (Crucible)", missioncategorylanguage

        private static readonly string[] MissionTables =
        {
            "mission_channel_policy", "mission_repeat_policy", "mission_scene_binding", "mission_evidence",
            "mission_action", "mission_trigger", "mission_scenario_step", "mission_scenario",
            "mission_spawn", "mission_spawn_group", "mission_indicator", "mission_area",
            "mission_reward_item", "mission_reward_definition", "mission_objective_transition",
            "mission_objective_definition", "mission_prerequisite", "mission_content_definition"
        };

        private readonly MigrationBuilder _migration;
        private readonly uint _missionId;
        private readonly string _revision;
        /// <summary>The map of each marker written (objective, indicator id, map), for <see cref="Enable"/>.</summary>
        private readonly List<(uint Objective, uint Indicator, uint Map)> _markerMaps = new();

        private CrucibleMissionDataV1(MigrationBuilder migration, uint missionId, string revision = Revision)
        {
            _migration = migration;
            _missionId = missionId;
            _revision = revision;
        }

        /// <summary>Rows added to a mission an earlier migration put in: no new definition row.</summary>
        public static CrucibleMissionDataV1 Existing(MigrationBuilder migration, uint missionId) => new(migration, missionId);

        public CrucibleMissionDataV1(MigrationBuilder migration, uint missionId, string name,
            uint nameTextId, uint? giverId, uint? receiverId, uint level, bool shareable = false)
            : this(migration, missionId, Revision, name, nameTextId, giverId, receiverId, level, shareable)
        {
        }

        /// <summary>
        /// A Crucible mission (category <see cref="CategoryId"/>) kept in another zone's revision
        /// <paramref name="revision"/>, as Marshes It Lies in Ruins (1673) is kept in plateau_1_16_5.
        /// </summary>
        public CrucibleMissionDataV1(MigrationBuilder migration, uint missionId, string revision, string name,
            uint nameTextId, uint? giverId, uint? receiverId, uint level, bool shareable = false)
            : this(migration, missionId, revision)
        {
            Insert("mission_content_definition",
                ("client_name_text_id", nameTextId), ("giver_id", giverId), ("receiver_id", receiverId),
                ("level", level), ("group_type", (byte)1), ("category_id", CategoryId),
                ("shareable", shareable), ("radio_completeable", false), ("comment", name),
                ("abandonment_policy", (byte)MissionAbandonmentPolicy.Allowed));
        }

        /// <summary>
        /// The turn-in reward, reward 1. Insert it before the GrantReward action that names it. Fixed
        /// items, if any, follow the selectable ones and come with every choice.
        /// </summary>
        public void Reward(uint experience, uint credits, IReadOnlyList<uint> selectableTemplates,
            IReadOnlyList<uint> fixedTemplates = null) =>
            Reward(experience, credits, selectableTemplates.Select(template => (template, 1U)).ToArray(),
                (fixedTemplates ?? new uint[0]).Select(template => (template, 1U)).ToArray());

        /// <summary>As <see cref="Reward(uint, uint, IReadOnlyList{uint}, IReadOnlyList{uint})"/>, a stack of each item.</summary>
        public void Reward(uint experience, uint credits, IReadOnlyList<(uint Template, uint Quantity)> selectableItems,
            IReadOnlyList<(uint Template, uint Quantity)> fixedItems)
        {
            Insert("mission_reward_definition",
                ("reward_id", 1U), ("experience", experience), ("credits", credits), ("prestige", 0U),
                ("selection_count", (byte)(selectableItems.Count > 0 ? 1 : 0)), ("comment", "Crucible turn-in reward"));
            var items = selectableItems.Select(item => (Kind: MissionRewardItemKind.Selectable, item.Template, item.Quantity))
                .Concat(fixedItems.Select(item => (Kind: MissionRewardItemKind.Fixed, item.Template, item.Quantity))).ToArray();
            for (var i = 0; i < items.Length; i++)
                CrucibleSql.Insert(_migration, "mission_reward_item",
                    new[] { "mission_id", "content_revision", "reward_id", "item_id", "kind", "item_template_id", "quantity" },
                    new object[] { _missionId, _revision, 1U, (uint)i + 1, (byte)items[i].Kind, items[i].Template, items[i].Quantity });
        }

        /// <summary>
        /// Turned in by radio, not at an NPC: accepted at the giver (channel Npc), completed from the
        /// mission log (channel Radio, mission_channel_policy). Construct the mission with no receiver.
        /// </summary>
        public void RadioCompletion()
        {
            CrucibleSql.Update(_migration, "mission_content_definition", _missionId, _revision, "radio_completeable", true);
            CrucibleSql.Insert(_migration, "mission_channel_policy",
                new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
                new object[] { _missionId, _revision, (int)MissionChannel.Npc, (int)MissionChannel.Radio, "[]" });
        }

        /// <summary>
        /// Offered by radio, not by an NPC (channel Radio, mission_channel_policy), from
        /// <paramref name="sources"/>; turned in at the receiver (channel Npc). Construct the mission with no giver.
        /// </summary>
        public void RadioOffer(params MissionOfferSourceDefinition[] sources) =>
            CrucibleSql.Insert(_migration, "mission_channel_policy",
                new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
                new object[] { _missionId, _revision, (int)MissionChannel.Radio, (int)MissionChannel.Npc, JsonSerializer.Serialize(sources) });

        /// <summary>
        /// Offered by radio from <paramref name="sources"/> and turned in by radio (channel Radio both ways,
        /// mission_channel_policy), as Headquarters' radio missions. Construct the mission with no giver or receiver.
        /// </summary>
        public void RadioOfferAndCompletion(params MissionOfferSourceDefinition[] sources)
        {
            CrucibleSql.Update(_migration, "mission_content_definition", _missionId, _revision, "radio_completeable", true);
            CrucibleSql.Insert(_migration, "mission_channel_policy",
                new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
                new object[] { _missionId, _revision, (int)MissionChannel.Radio, (int)MissionChannel.Radio, JsonSerializer.Serialize(sources) });
        }

        /// <summary>
        /// <paramref name="objective"/> fails, and the mission with it, when <paramref name="seconds"/> pass after it
        /// becomes Incomplete (MissionDeadlineService), as WildernessDaghdasUrnV1's timed objectives: its transition
        /// <paramref name="transition"/> to Failed on a timer trigger. The way it completes is another transition.
        /// </summary>
        public void FailOnDeadline(uint objective, uint seconds, uint transition = 1)
        {
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", transition), ("sequence", transition),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)MissionObjectiveState.Failed),
                ("comment", $"Objective {objective} fails at its deadline"));
            Trigger(objective, transition, MissionTriggerKind.TimerElapsed, 1, ("duration_seconds", seconds));
        }

        /// <summary>Offered again as soon as it is turned in (mission_repeat_policy Immediate), as WildernessDaghdasUrnV1's replacement batch.</summary>
        public void RepeatImmediately() =>
            CrucibleSql.Insert(_migration, "mission_repeat_policy",
                new[] { "mission_id", "content_revision", "repeat_kind", "cooldown_seconds", "reset_second_utc" },
                new object[] { _missionId, _revision, (int)Rasa.Missions.Runtime.MissionRepeatKind.Immediate, null, null });

        public void Prerequisite(uint mission, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.MissionCompleted),
                ("required_mission_id", mission), ("required_mission_state", (byte)MissionState.Completed),
                ("required_level", null), ("player_flag_id", null), ("player_flag_value", null),
                ("comment", $"Requires completed mission {mission}"));

        /// <summary>
        /// Offered only while <paramref name="mission"/> is in the player's mission log and Active (TaRapedia's "requires ...
        /// (in mission log)"): MissionAccepted with the state Active, as the Bootcamp's retry row (MissionStateRequirement).
        /// </summary>
        public void ActivePrerequisite(uint mission, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.MissionAccepted),
                ("required_mission_id", mission), ("required_mission_state", (byte)MissionState.Active),
                ("required_level", null), ("player_flag_id", null), ("player_flag_value", null),
                ("comment", $"Requires mission {mission} active in the mission log"));

        /// <summary>Offered only to a player of level <paramref name="level"/> or above (TaRapedia's "requires level").</summary>
        public void LevelPrerequisite(uint level, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.PlayerLevelAtLeast),
                ("required_mission_id", null), ("required_mission_state", null),
                ("required_level", level), ("player_flag_id", null), ("player_flag_value", null),
                ("comment", $"Requires level {level}"));

        /// <summary>Offered only to a player whose mission flag <paramref name="flag"/> holds <paramref name="value"/>.</summary>
        public void FlagPrerequisite(uint flag, uint value, string comment, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.PlayerFlagValue),
                ("required_mission_id", null), ("required_mission_state", null),
                ("required_level", null), ("player_flag_id", flag), ("player_flag_value", value),
                ("comment", comment));

        public void Objective(uint id, uint nameTextId, uint bodyTextId, uint ordinal, MissionObjectiveState initial,
            uint? counterTextId = null, bool required = true, uint? counter1TextId = null) =>
            Insert("mission_objective_definition",
                ("objective_id", id), ("client_name_text_id", nameTextId), ("client_body_text_id", bodyTextId),
                ("client_counter_0_text_id", counterTextId), ("client_counter_1_text_id", counter1TextId),
                ("client_counter_2_text_id", null), ("ordinal", ordinal), ("initial_state", (byte)initial),
                ("is_required", required), ("comment", $"Native objective {_missionId}.{id}"));

        public void Transition(uint objective) => Transition(objective, 1, 1);

        /// <summary>One more way to complete <paramref name="objective"/>, as a dialogue choice's (WildernessMissionDataV1).</summary>
        public void Transition(uint objective, uint transition, uint sequence) =>
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", transition), ("sequence", sequence),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)MissionObjectiveState.Completed),
                ("comment", $"Objective {objective} transition {transition}"));

        /// <summary>
        /// A way for <paramref name="objective"/> to <paramref name="toState"/>: Failed, or Incomplete for a dialogue choice
        /// that only does its actions and leaves the objective open (a menu's page, PoolsSpyNetworkingV1).
        /// </summary>
        public void Transition(uint objective, uint transition, uint sequence, MissionObjectiveState toState) =>
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", transition), ("sequence", sequence),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)toState),
                ("comment", $"Objective {objective} transition {transition} to {toState}"));

        /// <summary>Completes <paramref name="objective"/> once objective <paramref name="related"/> reaches <paramref name="state"/>.</summary>
        public void ObjectiveState(uint objective, uint related, MissionObjectiveState state) =>
            Trigger(objective, MissionTriggerKind.ObjectiveState, ("related_objective_id", related), ("related_state", (byte)state));

        /// <summary>The trigger of <paramref name="objective"/>'s transition <paramref name="transition"/>: objective <paramref name="related"/> reaches <paramref name="state"/>.</summary>
        public void ObjectiveState(uint objective, uint transition, uint related, MissionObjectiveState state) =>
            Trigger(objective, transition, MissionTriggerKind.ObjectiveState, 1, ("related_objective_id", related), ("related_state", (byte)state));

        public void Conversation(uint objective, uint package, uint flag = 1) =>
            Trigger(objective, MissionTriggerKind.Conversation, ("npc_package_id", package), ("player_flag_id", flag));

        /// <summary>The conversation that completes <paramref name="objective"/> by <paramref name="transition"/>.</summary>
        public void Conversation(uint objective, uint package, uint flag, uint transition) =>
            Trigger(objective, transition, MissionTriggerKind.Conversation, 1, ("npc_package_id", package), ("player_flag_id", flag));

        /// <summary>
        /// A progress rule. Rules that name the same <paramref name="counter"/> add to one count:
        /// counter 0 for a kill count; for a scene result set the counter is the scenario id
        /// (MissionProgressRuleAuthoring). Each rule needs a trigger id of its own. No target for a
        /// one-off event (a mission completed).
        /// </summary>
        public void Progress(uint objective, MissionProgressEventKind kind, uint subject, uint? target,
            uint? counter = null, uint trigger = 1) =>
            Trigger(objective, MissionTriggerKind.ProgressEvent, trigger,
                ("event_kind", (byte)kind), ("subject_id", subject),
                ("counter_id", counter), ("initial_value", target.HasValue ? 0U : null), ("target_value", target));

        /// <summary>The progress rule of <paramref name="objective"/>'s transition <paramref name="transition"/>.</summary>
        public void Progress(uint objective, uint transition, MissionProgressEventKind kind, uint subject, uint? target,
            uint? counter = null, uint trigger = 1) =>
            Trigger(objective, transition, MissionTriggerKind.ProgressEvent, trigger,
                ("event_kind", (byte)kind), ("subject_id", subject),
                ("counter_id", counter), ("initial_value", target.HasValue ? 0U : null), ("target_value", target));

        public void Action(uint objective, uint id, MissionActionKind kind, uint? targetObjective = null,
            uint? reward = null, MissionObjectiveState? state = null, CharacterIntent itemIntent = null,
            uint? playerFlag = null, uint? playerFlagValue = null, string comment = null, uint? scenario = null,
            uint transition = 1) =>
            Insert("mission_action",
                ("objective_id", objective), ("transition_id", transition), ("action_id", id),
                ("kind", (byte)kind), ("sequence", id), ("target_objective_id", targetObjective),
                ("objective_state", state.HasValue ? (byte)state.Value : null), ("reward_id", reward),
                ("spawn_group_id", null), ("scenario_id", scenario), ("indicator_id", null),
                ("player_flag_id", playerFlag), ("player_flag_value", playerFlagValue), ("npc_package_id", null),
                ("item_intent", itemIntent == null ? null : JsonSerializer.Serialize(itemIntent, MissionContentCodec.Options)),
                ("comment", comment ?? $"Objective {objective} action {id}"));

        /// <summary>
        /// A scenario that starts with the mission. Each step passes on one scene result as
        /// scenario event <c>step</c>, as Cache of the Day (WildernessLandingZoneV1) has its own.
        /// </summary>
        public void Scenario(uint id, string name, uint steps)
        {
            Insert("mission_scenario", ("scenario_id", id), ("start_policy", (byte)MissionScenarioStartPolicy.Automatic),
                ("name", name), ("comment", name));
            for (var step = 1U; step <= steps; step++)
                Insert("mission_scenario_step",
                    ("scenario_id", id), ("step_id", step), ("kind", (byte)MissionScenarioStepKind.EmitScenarioEvent),
                    ("sequence", step), ("scenario_event_id", step), ("comment", "Crucible scene result"));
        }

        /// <summary>
        /// An indicator with no client name: its id is <paramref name="id"/> above
        /// <see cref="MissionIndicator.UnnamedFrom"/>, as WildernessUnnamedIndicatorsV1 keys the
        /// Wilderness ones. A lower id is a missionobjectiveindicatorlanguage name of another mission.
        /// <paramref name="map"/> is the map the marker is on. The row has no map column: the server finds a marker's map
        /// as MissionMarkerProjection.ResolveMap does (the scene's MapContextId, else the mission's areas, spawn groups or
        /// pools) and sends a player only the markers of the map the player is on. <see cref="Enable"/> writes each marker
        /// off that map into the scene's IndicatorMaps, as upstream's MarkerMapScopes does.
        /// </summary>
        public void Indicator(uint objective, uint id, double x, double y, double z, double radius = 8, uint map = MapContextId)
        {
            Insert("mission_indicator",
                ("objective_id", objective), ("indicator_id", MissionIndicator.UnnamedFrom + id), ("pos_x", x), ("pos_y", y), ("pos_z", z),
                ("radius", radius), ("show_3d_effect", false), ("comment", "Crucible navigation"));
            _markerMaps.Add((objective, MissionIndicator.UnnamedFrom + id, map));
        }

        /// <summary>
        /// An indicator keyed by the client's own name for it, <paramref name="clientName"/> (a missionobjectiveindicatorlanguage id
        /// below <see cref="MissionIndicator.UnnamedFrom"/>): the client shows that name on the marker (QUEST-RULES).
        /// </summary>
        public void NamedIndicator(uint objective, uint clientName, double x, double y, double z, double radius = 8, uint map = MapContextId)
        {
            Insert("mission_indicator",
                ("objective_id", objective), ("indicator_id", clientName), ("pos_x", x), ("pos_y", y), ("pos_z", z),
                ("radius", radius), ("show_3d_effect", false), ("comment", "Crucible navigation, client name"));
            _markerMaps.Add((objective, clientName, map));
        }

        /// <summary>
        /// An upright cylinder on map <paramref name="map"/> (a squad instance's too: its copies keep the map's
        /// context id) that <see cref="EnterArea"/> waits for, as WildernessMissionDataV1.Area. <paramref name="height"/>
        /// is a half-height: the cylinder reaches that far above and below <paramref name="y"/> (MissionAreaService).
        /// </summary>
        public void Area(uint id, uint map, double x, double y, double z, double radius, double height = 8) =>
            Insert("mission_area",
                ("area_id", id), ("map_context_id", map), ("shape", (byte)MissionAreaShape.Cylinder),
                ("pos_x", x), ("pos_y", y), ("pos_z", z), ("radius", radius),
                ("extent_x", null), ("extent_y", height), ("extent_z", null),
                ("comment", "Crucible mission volume"));

        /// <summary>
        /// The whole of map <paramref name="map"/> (MissionAreaShape.Map), entered by arriving on it (a squad
        /// instance's copy too), as TargetsOfOpportunityOneOffs' operations: no position or extent is read.
        /// </summary>
        public void MapArea(uint id, uint map) =>
            Insert("mission_area",
                ("area_id", id), ("map_context_id", map), ("shape", (byte)MissionAreaShape.Map),
                ("pos_x", 0.0), ("pos_y", 0.0), ("pos_z", 0.0), ("radius", null),
                ("extent_x", null), ("extent_y", null), ("extent_z", null),
                ("comment", "Crucible mission map"));

        /// <summary>Completes <paramref name="objective"/> once the player walks into area <paramref name="area"/>.</summary>
        public void EnterArea(uint objective, uint area) =>
            Trigger(objective, MissionTriggerKind.AreaEntered, ("area_id", area));

        /// <summary>
        /// One source of the mission. Confidence: <paramref name="confidence"/>, else 1.0 for the
        /// client's data and 0.5 for anything else; lower it where little is known.
        /// </summary>
        public void Evidence(uint id, MissionEvidenceSourceKind kind, string source, string note, double? confidence = null) =>
            CrucibleSql.Insert(_migration, "mission_evidence",
                new[]
                {
                    "mission_id", "content_revision", "evidence_id", "owner_kind", "owner_id", "source_kind",
                    "source_uri", "local_client_path", "confidence", "reconstruction_note"
                },
                new object[]
                {
                    _missionId, _revision, id, (byte)MissionEvidenceOwnerKind.Mission, _missionId, (byte)kind,
                    source, null, confidence ?? (kind == MissionEvidenceSourceKind.Client ? 1.0 : 0.5), note
                });

        /// <summary>
        /// Enables the mission; a talk-only mission has no scene, as the Wilderness ones. Each marker not on the scene's map
        /// (MissionSceneDefinition.MapContextId; with none, the Crucible) goes into the scene's IndicatorMaps, so the server
        /// shows it on its own map (MissionMarkerProjection.ResolveMap); a mission with such a marker and no scene gets a scene
        /// that holds only them. A marker on the scene's map, or on the Crucible in a scene with no map, is found by the scene,
        /// the areas or the pools.
        /// </summary>
        public void Enable(MissionSceneDefinition scene = null)
        {
            var sceneMap = scene?.MapContextId > 0 ? scene.MapContextId : MapContextId;
            foreach (var (objective, indicator, map) in _markerMaps.Where(marker => marker.Map != sceneMap))
            {
                scene ??= new MissionSceneDefinition();
                scene.IndicatorMaps ??= new Dictionary<uint, Dictionary<uint, uint>>();
                if (!scene.IndicatorMaps.TryGetValue(objective, out var maps))
                    scene.IndicatorMaps[objective] = maps = new Dictionary<uint, uint>();
                maps[indicator] = map;
            }
            if (scene != null)
                CrucibleSql.Insert(_migration, "mission_scene_binding",
                    new[] { "mission_id", "content_revision", "script_key", "state_version", "bindings" },
                    new object[] { _missionId, _revision, scene.Script, scene.StateVersion,
                        JsonSerializer.Serialize(scene, MissionContentCodec.Options) });
            CrucibleSql.Update(_migration, "mission_content_definition", _missionId, _revision, "enabled", true);
        }

        public static void Remove(MigrationBuilder migration, uint missionId)
        {
            foreach (var table in MissionTables)
                migration.Sql($"DELETE FROM {table} WHERE mission_id = {missionId} AND content_revision = '{Revision}';");
        }

        private void Trigger(uint objective, MissionTriggerKind kind, params (string Column, object Value)[] values) =>
            Trigger(objective, kind, 1, values);

        private void Trigger(uint objective, MissionTriggerKind kind, uint trigger, params (string Column, object Value)[] values) =>
            Trigger(objective, 1, kind, trigger, values);

        private void Trigger(uint objective, uint transition, MissionTriggerKind kind, uint trigger, params (string Column, object Value)[] values) =>
            Insert("mission_trigger",
                new[]
                {
                    ("objective_id", (object)objective), ("transition_id", transition), ("trigger_id", trigger),
                    ("kind", (byte)kind), ("sequence", trigger), ("comment", $"Objective {objective} trigger {trigger}")
                }.Concat(values).ToArray());

        private void Insert(string table, params (string Column, object Value)[] values)
        {
            var columns = new[] { "mission_id", "content_revision", "requirement" }
                .Concat(values.Select(value => value.Column)).ToArray();
            var row = new object[] { _missionId, _revision, (byte)MissionContentRequirement.Required }
                .Concat(values.Select(value => value.Value)).ToArray();
            CrucibleSql.Insert(_migration, table, columns, row);
        }
    }
}
