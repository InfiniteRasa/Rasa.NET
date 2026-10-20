using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
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

        public PlateauMissionDataV1(MigrationBuilder migration, uint missionId, string name,
            uint nameTextId, uint giverId, uint receiverId, uint level, bool shareable = false)
        {
            _migration = migration;
            _missionId = missionId;
            Insert("mission_content_definition",
                ("client_name_text_id", nameTextId), ("giver_id", giverId), ("receiver_id", receiverId),
                ("level", level), ("group_type", (byte)1), ("category_id", CategoryId),
                ("shareable", shareable), ("radio_completeable", false), ("comment", name),
                ("abandonment_policy", (byte)MissionAbandonmentPolicy.Allowed));
        }

        /// <summary>The turn-in reward, reward 1. Insert it before the GrantReward action that names it.</summary>
        public void Reward(uint experience, uint credits, IReadOnlyList<uint> selectableTemplates)
        {
            Insert("mission_reward_definition",
                ("reward_id", 1U), ("experience", experience), ("credits", credits), ("prestige", 0U),
                ("selection_count", (byte)(selectableTemplates.Count > 0 ? 1 : 0)), ("comment", "Plateau turn-in reward"));
            for (var i = 0; i < selectableTemplates.Count; i++)
                _migration.InsertData("mission_reward_item",
                    new[] { "mission_id", "content_revision", "reward_id", "item_id", "kind", "item_template_id", "quantity" },
                    new object[] { _missionId, Revision, 1U, (uint)i + 1, (byte)MissionRewardItemKind.Selectable, selectableTemplates[i], 1U });
        }

        public void Prerequisite(uint mission, uint id = 1) =>
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.MissionCompleted),
                ("required_mission_id", mission), ("required_mission_state", (byte)MissionState.Completed),
                ("required_level", null), ("player_flag_id", null), ("player_flag_value", null),
                ("comment", $"Requires completed mission {mission}"));

        public void Objective(uint id, uint nameTextId, uint bodyTextId, uint ordinal, MissionObjectiveState initial) =>
            Insert("mission_objective_definition",
                ("objective_id", id), ("client_name_text_id", nameTextId), ("client_body_text_id", bodyTextId),
                ("client_counter_0_text_id", null), ("client_counter_1_text_id", null),
                ("client_counter_2_text_id", null), ("ordinal", ordinal), ("initial_state", (byte)initial),
                ("is_required", true), ("comment", $"Native objective {_missionId}.{id}"));

        public void Transition(uint objective) =>
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", 1U), ("sequence", 1U),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)MissionObjectiveState.Completed),
                ("comment", $"Objective {objective} transition 1"));

        public void Conversation(uint objective, uint package, uint flag = 1) =>
            Trigger(objective, MissionTriggerKind.Conversation, ("npc_package_id", package), ("player_flag_id", flag));

        public void Progress(uint objective, MissionProgressEventKind kind, uint subject, uint target) =>
            Trigger(objective, MissionTriggerKind.ProgressEvent,
                ("event_kind", (byte)kind), ("subject_id", subject),
                ("counter_id", null), ("initial_value", 0U), ("target_value", target));

        public void Action(uint objective, uint id, MissionActionKind kind, uint? targetObjective = null,
            uint? reward = null, MissionObjectiveState? state = null, CharacterIntent itemIntent = null) =>
            Insert("mission_action",
                ("objective_id", objective), ("transition_id", 1U), ("action_id", id),
                ("kind", (byte)kind), ("sequence", id), ("target_objective_id", targetObjective),
                ("objective_state", state.HasValue ? (byte)state.Value : null), ("reward_id", reward),
                ("spawn_group_id", null), ("scenario_id", null), ("indicator_id", null),
                ("player_flag_id", null), ("player_flag_value", null), ("npc_package_id", null),
                ("item_intent", itemIntent == null ? null : JsonSerializer.Serialize(itemIntent, MissionContentCodec.Options)),
                ("comment", $"Objective {objective} action {id}"));

        /// <summary>
        /// An indicator with no client name: its id is <paramref name="id"/> above
        /// <see cref="MissionIndicator.UnnamedFrom"/>, as WildernessUnnamedIndicatorsV1 keys the
        /// Wilderness ones. A lower id is a missionobjectiveindicatorlanguage name of another mission.
        /// </summary>
        public void Indicator(uint objective, uint id, double x, double y, double z, double radius = 8) =>
            Insert("mission_indicator",
                ("objective_id", objective), ("indicator_id", MissionIndicator.UnnamedFrom + id), ("pos_x", x), ("pos_y", y), ("pos_z", z),
                ("radius", radius), ("show_3d_effect", false), ("comment", "Plateau navigation"));

        public void Evidence(uint id, MissionEvidenceSourceKind kind, string source, string note) =>
            _migration.InsertData("mission_evidence",
                new[]
                {
                    "mission_id", "content_revision", "evidence_id", "owner_kind", "owner_id", "source_kind",
                    "source_uri", "local_client_path", "confidence", "reconstruction_note"
                },
                new object[]
                {
                    _missionId, Revision, id, (byte)MissionEvidenceOwnerKind.Mission, _missionId, (byte)kind,
                    source, null, kind == MissionEvidenceSourceKind.Client ? 1.0 : 0.5, note
                });

        public void Enable(MissionSceneDefinition scene)
        {
            MissionDataMigration.InsertScene(_migration, _missionId, Revision, scene);
            MissionDataMigration.EnableMission(_migration, _missionId, Revision);
        }

        public static void Remove(MigrationBuilder migration, uint missionId)
        {
            foreach (var table in MissionTables)
                migration.Sql($"DELETE FROM {table} WHERE mission_id = {missionId} AND content_revision = '{Revision}';");
        }

        private void Trigger(uint objective, MissionTriggerKind kind, params (string Column, object Value)[] values) =>
            Insert("mission_trigger",
                new[]
                {
                    ("objective_id", (object)objective), ("transition_id", 1U), ("trigger_id", 1U),
                    ("kind", (byte)kind), ("sequence", 1U), ("comment", $"Objective {objective} trigger 1")
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
