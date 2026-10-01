using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public sealed class WildernessMissionDataV1
    {
        public const string Revision = "wilderness_1_16_5";
        public const uint MapContextId = 1220;
        public const uint CategoryId = 10000044;
        private readonly MigrationBuilder _migration;
        private readonly uint _missionId;

        public WildernessMissionDataV1(MigrationBuilder migration, uint missionId, string name,
            uint nameTextId, uint? giverId, uint? receiverId, uint level, bool shareable = false)
        {
            _migration = migration;
            _missionId = missionId;
            Insert("mission_content_definition",
                ("client_name_text_id", nameTextId), ("giver_id", giverId), ("receiver_id", receiverId),
                ("level", level), ("group_type", (byte)1), ("category_id", CategoryId),
                ("shareable", shareable), ("radio_completeable", false), ("comment", name),
                ("abandonment_policy", (byte)MissionAbandonmentPolicy.Allowed));
        }

        public void Objective(uint id, uint nameTextId, uint bodyTextId, uint ordinal,
            MissionObjectiveState initial = MissionObjectiveState.Inactive, bool required = true,
            uint? counterTextId = null)
        {
            Insert("mission_objective_definition",
                ("objective_id", id), ("client_name_text_id", nameTextId), ("client_body_text_id", bodyTextId),
                ("client_counter_0_text_id", counterTextId), ("client_counter_1_text_id", null),
                ("client_counter_2_text_id", null), ("ordinal", ordinal), ("initial_state", (byte)initial),
                ("is_required", required), ("comment", $"Native objective {_missionId}.{id}"));
        }

        public void Transition(uint objective, uint transition = 1,
            MissionObjectiveState result = MissionObjectiveState.Completed, uint sequence = 1)
        {
            Insert("mission_objective_transition",
                ("objective_id", objective), ("transition_id", transition), ("sequence", sequence),
                ("from_state", (byte)MissionObjectiveState.Incomplete), ("to_state", (byte)result),
                ("comment", $"Objective {objective} transition {transition}"));
        }

        public void Conversation(uint objective, uint package, uint flag = 1, uint transition = 1)
        {
            Trigger(objective, transition, 1, MissionTriggerKind.Conversation,
                ("npc_package_id", package), ("player_flag_id", flag));
        }

        public void Progress(uint objective, MissionProgressEventKind kind, uint subject,
            uint? counter = null, uint? target = null, uint transition = 1, uint trigger = 1)
        {
            Trigger(objective, transition, trigger, MissionTriggerKind.ProgressEvent,
                ("event_kind", (byte)kind), ("subject_id", subject), ("counter_id", counter),
                ("initial_value", target.HasValue ? 0U : null), ("target_value", target));
        }

        public void EnterArea(uint objective, uint area, uint transition = 1) =>
            Trigger(objective, transition, 1, MissionTriggerKind.AreaEntered, ("area_id", area));

        public void Action(uint objective, uint id, MissionActionKind kind, uint? targetObjective = null,
            uint? reward = null, uint? scenario = null, uint transition = 1,
            MissionObjectiveState? state = null, CharacterIntent itemIntent = null,
            uint? playerFlagId = null, uint? playerFlagValue = null)
        {
            Insert("mission_action",
                ("objective_id", objective), ("transition_id", transition), ("action_id", id),
                ("kind", (byte)kind), ("sequence", id), ("target_objective_id", targetObjective),
                ("objective_state", state.HasValue ? (byte)state.Value : null), ("reward_id", reward),
                ("spawn_group_id", null), ("scenario_id", scenario), ("indicator_id", null),
                ("player_flag_id", playerFlagId), ("player_flag_value", playerFlagValue), ("npc_package_id", null),
                ("item_intent", itemIntent == null ? null : JsonSerializer.Serialize(itemIntent, MissionContentCodec.Options)),
                ("comment", $"Objective {objective} action {id}"));
        }

        public void RevealAndActivate(uint objective, uint next, uint firstAction = 1, uint transition = 1)
        {
            Action(objective, firstAction, MissionActionKind.RevealObjective, next, transition: transition);
            Action(objective, firstAction + 1, MissionActionKind.ActivateObjective, next,
                transition: transition, state: MissionObjectiveState.Incomplete);
        }

        public void Prerequisite(uint mission, uint id = 1)
        {
            Insert("mission_prerequisite",
                ("prerequisite_id", id), ("kind", (byte)MissionPrerequisiteKind.MissionCompleted),
                ("required_mission_id", mission), ("required_mission_state", (byte)MissionState.Completed),
                ("required_level", null), ("player_flag_id", null), ("player_flag_value", null),
                ("comment", $"Requires completed mission {mission}"));
        }

        public void Reward(uint experience, uint credits, IReadOnlyList<(uint TemplateId, uint Quantity)> fixedItems = null,
            IReadOnlyList<(uint TemplateId, uint Quantity)> selectableItems = null)
        {
            Insert("mission_reward_definition",
                ("reward_id", 1U), ("experience", experience), ("credits", credits), ("prestige", 0U),
                ("selection_count", (byte)(selectableItems?.Count > 0 ? 1 : 0)),
                ("comment", "Wilderness turn-in reward"));
            uint id = 0;
            foreach (var item in fixedItems ?? System.Array.Empty<(uint, uint)>())
                RewardItem(++id, MissionRewardItemKind.Fixed, item);
            foreach (var item in selectableItems ?? System.Array.Empty<(uint, uint)>())
                RewardItem(++id, MissionRewardItemKind.Selectable, item);
        }

        public void Indicator(uint objective, uint id, double x, double y, double z, double radius = 8)
        {
            Insert("mission_indicator",
                ("objective_id", objective), ("indicator_id", id), ("pos_x", x), ("pos_y", y), ("pos_z", z),
                ("radius", radius), ("show_3d_effect", false), ("comment", "Wilderness navigation"));
        }

        public void Area(uint id, double x, double y, double z, double radius = 8, double height = 4)
        {
            Insert("mission_area",
                ("area_id", id), ("map_context_id", MapContextId), ("shape", (byte)MissionAreaShape.Cylinder),
                ("pos_x", x), ("pos_y", y), ("pos_z", z), ("radius", radius),
                ("extent_x", null), ("extent_y", height), ("extent_z", null),
                ("comment", "Grounded Wilderness mission volume"));
        }

        public void Scenario(uint id, string name, uint? activatesObjective = null)
        {
            Insert("mission_scenario", ("scenario_id", id), ("start_policy", (byte)MissionScenarioStartPolicy.Automatic),
                ("name", name), ("comment", name));
            if (activatesObjective.HasValue)
                Insert("mission_scenario_step",
                    ("scenario_id", id), ("step_id", 1U), ("kind", (byte)MissionScenarioStepKind.ActivateObjective),
                    ("sequence", 1U), ("target_objective_id", activatesObjective), ("comment", "Activate scene destination"));
        }

        public void Evidence(uint id, MissionEvidenceSourceKind kind, string source, string note)
        {
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
        }

        public void Enable(MissionSceneDefinition scene = null)
        {
            if (scene != null)
                MissionDataMigration.InsertScene(_migration, _missionId, Revision, scene);
            MissionDataMigration.EnableMission(_migration, _missionId, Revision);
        }

        public static void Remove(MigrationBuilder migration, params uint[] missionIds)
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
                    migration.Sql($"DELETE FROM {table} WHERE mission_id = {missionId} AND content_revision = '{Revision}';");
        }

        private void RewardItem(uint id, MissionRewardItemKind kind, (uint TemplateId, uint Quantity) item)
        {
            _migration.InsertData("mission_reward_item",
                new[] { "mission_id", "content_revision", "reward_id", "item_id", "kind", "item_template_id", "quantity" },
                new object[] { _missionId, Revision, 1U, id, (byte)kind, item.TemplateId, item.Quantity });
        }

        private void Trigger(uint objective, uint transition, uint id, MissionTriggerKind kind,
            params (string Column, object Value)[] values) =>
            Insert("mission_trigger",
                new[]
                {
                    ("objective_id", (object)objective), ("transition_id", transition), ("trigger_id", id),
                    ("kind", (byte)kind), ("sequence", id), ("comment", $"Objective {objective} trigger {id}")
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
