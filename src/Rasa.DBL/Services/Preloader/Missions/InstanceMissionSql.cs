using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// One mission's rows as plain SQL statements, valid on SQLite and MySQL alike, for a data-only
    /// migration that has no model of its own (the Wilderness missions' WildernessMissionDataV1
    /// writes the same rows through MigrationBuilder.InsertData, which needs one). The rows are
    /// the same tables, with the content revision and category given rather than fixed.
    /// </summary>
    public sealed class InstanceMissionSql
    {
        private readonly List<string> _sql;
        private readonly string _revision;
        private readonly uint _missionId;

        public InstanceMissionSql(List<string> sql, string revision, uint missionId, string name, uint nameTextId,
            uint? giverId, uint? receiverId, uint level, uint categoryId, bool shareable = false, bool radioCompleteable = false)
        {
            _sql = sql;
            _revision = revision;
            _missionId = missionId;
            Insert("mission_content_definition",
                ("client_name_text_id", nameTextId), ("giver_id", giverId), ("receiver_id", receiverId),
                ("level", level), ("group_type", (byte)1), ("category_id", categoryId),
                ("shareable", shareable), ("radio_completeable", radioCompleteable), ("comment", name),
                ("abandonment_policy", (byte)MissionAbandonmentPolicy.Allowed), ("enabled", false));
        }

        public void Objective(uint id, uint nameTextId, uint bodyTextId, uint ordinal,
            MissionObjectiveState initial = MissionObjectiveState.Inactive, bool required = true, uint? counterTextId = null)
        {
            Insert("mission_objective_definition",
                ("objective_id", id), ("client_name_text_id", nameTextId), ("client_body_text_id", bodyTextId),
                ("client_counter_0_text_id", counterTextId), ("client_counter_1_text_id", null), ("client_counter_2_text_id", null),
                ("ordinal", ordinal), ("initial_state", (byte)initial), ("is_required", required),
                ("comment", $"Native objective {_missionId}.{id}"));
        }

        public void Transition(uint objective, uint transition = 1,
            MissionObjectiveState result = MissionObjectiveState.Completed, uint sequence = 1)
        {
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", transition), ("sequence", sequence),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)result),
                ("comment", $"Objective {objective} transition {transition}"));
        }

        public void Conversation(uint objective, uint package, uint flag = 1, uint transition = 1) =>
            Trigger(objective, transition, 1, MissionTriggerKind.Conversation,
                ("npc_package_id", package), ("player_flag_id", flag));

        public void Progress(uint objective, MissionProgressEventKind kind, uint subject,
            uint? counter = null, uint? target = null, uint transition = 1, uint trigger = 1) =>
            Trigger(objective, transition, trigger, MissionTriggerKind.ProgressEvent,
                ("event_kind", (byte)kind), ("subject_id", subject), ("counter_id", counter),
                ("initial_value", target.HasValue ? 0U : null), ("target_value", target));

        /// <summary>The objective's deadline: <paramref name="seconds"/> after it is taken up, the transition.</summary>
        public void Timer(uint objective, uint seconds, uint transition) =>
            Trigger(objective, transition, 1, MissionTriggerKind.TimerElapsed, ("duration_seconds", seconds));

        public void Action(uint objective, uint id, MissionActionKind kind, uint? targetObjective = null,
            uint? reward = null, uint transition = 1, MissionObjectiveState? state = null,
            CharacterIntent itemIntent = null)
        {
            Insert("mission_action",
                ("objective_id", objective), ("transition_id", transition), ("action_id", id),
                ("kind", (byte)kind), ("sequence", id), ("target_objective_id", targetObjective),
                ("objective_state", state.HasValue ? (byte)state.Value : null), ("reward_id", reward),
                ("item_intent", itemIntent == null ? null : JsonSerializer.Serialize(itemIntent, MissionContentCodec.Options)),
                ("comment", $"Objective {objective} action {id}"));
        }

        public void RevealAndActivate(uint objective, uint next, uint firstAction = 1, uint transition = 1)
        {
            Action(objective, firstAction, MissionActionKind.RevealObjective, next, transition: transition);
            Action(objective, firstAction + 1, MissionActionKind.ActivateObjective, next,
                transition: transition, state: MissionObjectiveState.Incomplete);
        }

        /// <summary>
        /// Taken from its giver, turned in by radio: the mission's channels (acceptance by NPC,
        /// completion by radio, no radio offer) and the client told it can be radioed in. The
        /// mission is to have no receiver.
        /// </summary>
        public void RadioTurnIn()
        {
            _sql.Add(Statement("mission_channel_policy", new (string, object)[]
            {
                ("mission_id", _missionId), ("content_revision", _revision),
                ("acceptance_channel", (int)MissionChannel.Npc), ("completion_channel", (int)MissionChannel.Radio),
                ("radio_sources", "[]")
            }));
            _sql.Add($"UPDATE mission_content_definition SET radio_completeable = 1 WHERE mission_id = {_missionId} AND content_revision = {S(_revision)};");
        }

        public void Prerequisite(uint mission, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.MissionCompleted),
                ("required_mission_id", mission), ("required_mission_state", (byte)MissionState.Completed),
                ("comment", $"Requires completed mission {mission}"));

        /// <summary>A reward item: its template, how many, and the module in its first slot (0 for none).</summary>
        public sealed record RewardItem(uint TemplateId, uint Quantity, uint Module = 0);

        public void Reward(uint experience, uint credits, IReadOnlyList<RewardItem> fixedItems = null,
            IReadOnlyList<RewardItem> selectableItems = null)
        {
            Insert("mission_reward_definition",
                ("reward_id", 1U), ("experience", experience), ("credits", credits), ("prestige", 0U),
                ("selection_count", (byte)(selectableItems?.Count > 0 ? 1 : 0)),
                ("comment", "Turn-in reward"));
            uint id = 0;
            foreach (var item in fixedItems ?? Array.Empty<RewardItem>())
                RewardRow(++id, MissionRewardItemKind.Fixed, item);
            foreach (var item in selectableItems ?? Array.Empty<RewardItem>())
                RewardRow(++id, MissionRewardItemKind.Selectable, item);
        }

        public void Indicator(uint objective, uint id, double x, double y, double z, double radius = 8) =>
            Insert("mission_indicator",
                ("objective_id", objective), ("indicator_id", id), ("pos_x", x), ("pos_y", y), ("pos_z", z),
                ("radius", radius), ("show_3d_effect", false), ("comment", "Objective marker"));

        /// <summary>
        /// A scenario that is only the events an instance's own script records (OjasaHive), one
        /// step each: never run as a scenario, so it is the player's to trigger and nobody does.
        /// </summary>
        public void Scenario(uint id, string name, params uint[] events)
        {
            Insert("mission_scenario", ("scenario_id", id), ("start_policy", (byte)MissionScenarioStartPolicy.PlayerTriggered),
                ("name", name), ("comment", name));
            foreach (var eventId in events)
                Insert("mission_scenario_step",
                    ("scenario_id", id), ("step_id", eventId), ("kind", (byte)MissionScenarioStepKind.EmitScenarioEvent),
                    ("sequence", eventId), ("scenario_event_id", eventId), ("comment", $"{name}: event {eventId}"));
        }

        public void Evidence(uint id, MissionEvidenceSourceKind kind, string source, string note) =>
            _sql.Add(Statement("mission_evidence", new (string, object)[]
            {
                ("mission_id", _missionId), ("content_revision", _revision), ("evidence_id", id),
                ("owner_kind", (byte)MissionEvidenceOwnerKind.Mission), ("owner_id", _missionId), ("source_kind", (byte)kind),
                ("source_uri", source), ("local_client_path", null),
                ("confidence", kind == MissionEvidenceSourceKind.Client ? 1.0 : 0.5), ("reconstruction_note", note)
            }));

        /// <summary>
        /// How the mission is taken on and handed in, and the server events that may offer it by
        /// radio (MissionOfferSourceDefinition, in the loader's default JSON contract).
        /// </summary>
        public void Channels(MissionChannel acceptance, MissionChannel completion, params MissionOfferSourceDefinition[] sources) =>
            _sql.Add(Statement("mission_channel_policy", new (string, object)[]
            {
                ("mission_id", _missionId), ("content_revision", _revision), ("acceptance_channel", (int)acceptance),
                ("completion_channel", (int)completion), ("radio_sources", JsonSerializer.Serialize(sources))
            }));

        /// <summary>The scene binding, if there is one, and the mission enabled - or, with <paramref name="enabled"/> false, left as it was put in, disabled.</summary>
        public void Enable(MissionSceneDefinition scene, bool enabled = true)
        {
            if (scene != null)
                _sql.Add(Statement("mission_scene_binding", new (string, object)[]
                {
                    ("mission_id", _missionId), ("content_revision", _revision), ("script_key", scene.Script),
                    ("state_version", scene.StateVersion), ("bindings", JsonSerializer.Serialize(scene, MissionContentCodec.Options))
                }));

            if (enabled)
                _sql.Add($"UPDATE mission_content_definition SET enabled = 1 WHERE mission_id = {_missionId} AND content_revision = {S(_revision)};");
        }

        /// <summary>Every row of the missions under the revision, child tables first.</summary>
        public static IEnumerable<string> Remove(string revision, params uint[] missionIds)
        {
            foreach (var missionId in missionIds)
                foreach (var table in new[]
                {
                    "mission_channel_policy", "mission_repeat_policy", "mission_scene_binding", "mission_evidence",
                    "mission_action", "mission_trigger", "mission_scenario_step", "mission_scenario",
                    "mission_spawn", "mission_spawn_group", "mission_indicator", "mission_area",
                    "mission_reward_item", "mission_reward_definition", "mission_objective_transition",
                    "mission_objective_definition", "mission_prerequisite", "mission_content_definition"
                })
                    yield return $"DELETE FROM {table} WHERE mission_id = {missionId} AND content_revision = {S(revision)};";
        }

        private void RewardRow(uint id, MissionRewardItemKind kind, RewardItem item) =>
            _sql.Add(Statement("mission_reward_item", new (string, object)[]
            {
                ("mission_id", _missionId), ("content_revision", _revision), ("reward_id", 1U), ("item_id", id),
                ("kind", (byte)kind), ("item_template_id", item.TemplateId), ("quantity", item.Quantity),
                ("module_1", item.Module), ("module_2", 0U), ("module_3", 0U), ("module_4", 0U)
            }));

        private void Trigger(uint objective, uint transition, uint id, MissionTriggerKind kind,
            params (string Column, object Value)[] values) =>
            Insert("mission_trigger",
                new[]
                {
                    ("objective_id", (object)objective), ("transition_id", transition), ("trigger_id", id),
                    ("kind", (byte)kind), ("sequence", id), ("comment", $"Objective {objective} trigger {id}")
                }.Concat(values).ToArray());

        private void Insert(string table, params (string Column, object Value)[] values) =>
            _sql.Add(Statement(table, new (string, object)[]
                {
                    ("mission_id", _missionId), ("content_revision", _revision),
                    ("requirement", (byte)MissionContentRequirement.Required)
                }.Concat(values).ToArray()));

        private static string Statement(string table, (string Column, object Value)[] values) =>
            $"INSERT INTO {table} ({string.Join(", ", values.Select(value => value.Column))}) " +
            $"VALUES ({string.Join(", ", values.Select(value => Literal(value.Value)))});";

        internal static string Literal(object value) => value switch
        {
            null => "NULL",
            bool flag => flag ? "1" : "0",
            string text => S(text),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"No literal for {value.GetType().Name}.")
        };

        /// <summary>
        /// A string literal as both providers read it. MySQL reads a backslash in one as an
        /// escape and SQLite does not, so a value with one has no literal that means the same
        /// to both: it is refused rather than written differently on each.
        /// </summary>
        internal static string S(string value) => value.Contains('\\')
            ? throw new System.ArgumentException("A literal with a backslash reads differently on SQLite and MySQL.", nameof(value))
            : "'" + value.Replace("'", "''") + "'";
    }
}
