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

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Rows of one Plateau mission (revision plateau_1_16_5, category "Battlefield (Plateau)"), as
    /// WildernessMissionDataV1 writes the Wilderness ones: EF data operations, every row Required.
    /// Fixed once a migration uses it: a later change to a mission goes in a new migration.
    /// </summary>
    public sealed class PlateauMissionDataV1
    {
        public const string Revision = "plateau_1_16_5";
        public const uint MapContextId = 1497;
        public const uint CategoryId = 10000056;        // "Battlefield (Plateau)", missioncategorylanguage

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

        private PlateauMissionDataV1(MigrationBuilder migration, uint missionId)
        {
            _migration = migration;
            _missionId = missionId;
        }

        /// <summary>Rows added to a mission an earlier migration put in: no new definition row.</summary>
        public static PlateauMissionDataV1 Existing(MigrationBuilder migration, uint missionId) => new(migration, missionId);

        public PlateauMissionDataV1(MigrationBuilder migration, uint missionId, string name,
            uint nameTextId, uint giverId, uint? receiverId, uint level, bool shareable = false)
            : this(migration, missionId)
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
                ("selection_count", (byte)(selectableItems.Count > 0 ? 1 : 0)), ("comment", "Plateau turn-in reward"));
            var items = selectableItems.Select(item => (Kind: MissionRewardItemKind.Selectable, item.Template, item.Quantity))
                .Concat(fixedItems.Select(item => (Kind: MissionRewardItemKind.Fixed, item.Template, item.Quantity))).ToArray();
            for (var i = 0; i < items.Length; i++)
                _migration.InsertData("mission_reward_item",
                    new[] { "mission_id", "content_revision", "reward_id", "item_id", "kind", "item_template_id", "quantity" },
                    new object[] { _missionId, Revision, 1U, (uint)i + 1, (byte)items[i].Kind, items[i].Template, items[i].Quantity });
        }

        /// <summary>
        /// Turned in by radio, not at an NPC: accepted at the giver (channel Npc), completed from the
        /// mission log (channel Radio, mission_channel_policy). Construct the mission with no receiver.
        /// </summary>
        public void RadioCompletion()
        {
            _migration.UpdateData("mission_content_definition", new[] { "mission_id", "content_revision" },
                new object[] { _missionId, Revision }, "radio_completeable", true);
            _migration.InsertData("mission_channel_policy",
                new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
                new object[] { _missionId, Revision, (int)MissionChannel.Npc, (int)MissionChannel.Radio, "[]" });
        }

        public void Prerequisite(uint mission, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.MissionCompleted),
                ("required_mission_id", mission), ("required_mission_state", (byte)MissionState.Completed),
                ("required_level", null), ("player_flag_id", null), ("player_flag_value", null),
                ("comment", $"Requires completed mission {mission}"));

        /// <summary>Offered only to a player whose mission flag <paramref name="flag"/> holds <paramref name="value"/>.</summary>
        public void FlagPrerequisite(uint flag, uint value, string comment, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.PlayerFlagValue),
                ("required_mission_id", null), ("required_mission_state", null),
                ("required_level", null), ("player_flag_id", flag), ("player_flag_value", value),
                ("comment", comment));

        public void Objective(uint id, uint nameTextId, uint bodyTextId, uint ordinal, MissionObjectiveState initial,
            uint? counterTextId = null) =>
            Insert("mission_objective_definition",
                ("objective_id", id), ("client_name_text_id", nameTextId), ("client_body_text_id", bodyTextId),
                ("client_counter_0_text_id", counterTextId), ("client_counter_1_text_id", null),
                ("client_counter_2_text_id", null), ("ordinal", ordinal), ("initial_state", (byte)initial),
                ("is_required", true), ("comment", $"Native objective {_missionId}.{id}"));

        public void Transition(uint objective) =>
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", 1U), ("sequence", 1U),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)MissionObjectiveState.Completed),
                ("comment", $"Objective {objective} transition 1"));

        /// <summary>Completes <paramref name="objective"/> once objective <paramref name="related"/> reaches <paramref name="state"/>.</summary>
        public void ObjectiveState(uint objective, uint related, MissionObjectiveState state) =>
            Trigger(objective, MissionTriggerKind.ObjectiveState, ("related_objective_id", related), ("related_state", (byte)state));

        public void Conversation(uint objective, uint package, uint flag = 1) =>
            Trigger(objective, MissionTriggerKind.Conversation, ("npc_package_id", package), ("player_flag_id", flag));

        /// <summary>
        /// A progress rule. Rules that name the same <paramref name="counter"/> add to one count,
        /// counter 0 for a kill count, 1 for a scene result set (MissionProgressRuleAuthoring); each
        /// rule needs a trigger id of its own. No target for a one-off event (a mission completed).
        /// </summary>
        public void Progress(uint objective, MissionProgressEventKind kind, uint subject, uint? target,
            uint? counter = null, uint trigger = 1) =>
            Trigger(objective, MissionTriggerKind.ProgressEvent, trigger,
                ("event_kind", (byte)kind), ("subject_id", subject),
                ("counter_id", counter), ("initial_value", target.HasValue ? 0U : null), ("target_value", target));

        public void Action(uint objective, uint id, MissionActionKind kind, uint? targetObjective = null,
            uint? reward = null, MissionObjectiveState? state = null, CharacterIntent itemIntent = null,
            uint? playerFlag = null, uint? playerFlagValue = null, string comment = null) =>
            Insert("mission_action",
                ("objective_id", objective), ("transition_id", 1U), ("action_id", id),
                ("kind", (byte)kind), ("sequence", id), ("target_objective_id", targetObjective),
                ("objective_state", state.HasValue ? (byte)state.Value : null), ("reward_id", reward),
                ("spawn_group_id", null), ("scenario_id", null), ("indicator_id", null),
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
                    ("sequence", step), ("scenario_event_id", step), ("comment", "Plateau scene result"));
        }

        /// <summary>
        /// An indicator with no client name: its id is <paramref name="id"/> above
        /// <see cref="MissionIndicator.UnnamedFrom"/>, as WildernessUnnamedIndicatorsV1 keys the
        /// Wilderness ones. A lower id is a missionobjectiveindicatorlanguage name of another mission.
        /// </summary>
        public void Indicator(uint objective, uint id, double x, double y, double z, double radius = 8) =>
            Insert("mission_indicator",
                ("objective_id", objective), ("indicator_id", MissionIndicator.UnnamedFrom + id), ("pos_x", x), ("pos_y", y), ("pos_z", z),
                ("radius", radius), ("show_3d_effect", false), ("comment", "Plateau navigation"));

        /// <summary>
        /// One source of the mission. Confidence: <paramref name="confidence"/>, else 1.0 for the
        /// client's data and 0.5 for anything else; lower it where little is known.
        /// </summary>
        public void Evidence(uint id, MissionEvidenceSourceKind kind, string source, string note, double? confidence = null) =>
            _migration.InsertData("mission_evidence",
                new[]
                {
                    "mission_id", "content_revision", "evidence_id", "owner_kind", "owner_id", "source_kind",
                    "source_uri", "local_client_path", "confidence", "reconstruction_note"
                },
                new object[]
                {
                    _missionId, Revision, id, (byte)MissionEvidenceOwnerKind.Mission, _missionId, (byte)kind,
                    source, null, confidence ?? (kind == MissionEvidenceSourceKind.Client ? 1.0 : 0.5), note
                });

        /// <summary>Enables the mission; a talk-only mission has no scene, as the Wilderness ones.</summary>
        public void Enable(MissionSceneDefinition scene = null)
        {
            if (scene != null)
                MissionDataMigration.InsertScene(_migration, _missionId, Revision, scene);
            MissionDataMigration.EnableMission(_migration, _missionId, Revision);
        }

        public static void Remove(MigrationBuilder migration, uint missionId)
        {
            foreach (var table in MissionTables)
                migration.Sql($"DELETE FROM {table} WHERE mission_id = {missionId} AND content_revision = '{Revision}';");
        }

        private void Trigger(uint objective, MissionTriggerKind kind, params (string Column, object Value)[] values) =>
            Trigger(objective, kind, 1, values);

        private void Trigger(uint objective, MissionTriggerKind kind, uint trigger, params (string Column, object Value)[] values) =>
            Insert("mission_trigger",
                new[]
                {
                    ("objective_id", (object)objective), ("transition_id", 1U), ("trigger_id", trigger),
                    ("kind", (byte)kind), ("sequence", trigger), ("comment", $"Objective {objective} trigger {trigger}")
                }.Concat(values).ToArray());

        private void Insert(string table, params (string Column, object Value)[] values)
        {
            var columns = new[] { "mission_id", "content_revision", "requirement" }
                .Concat(values.Select(value => value.Column)).ToArray();
            var row = new object[] { _missionId, Revision, (byte)MissionContentRequirement.Required }
                .Concat(values.Select(value => value.Value)).ToArray();
            _migration.InsertData(table, columns, row);
        }
    }
}
