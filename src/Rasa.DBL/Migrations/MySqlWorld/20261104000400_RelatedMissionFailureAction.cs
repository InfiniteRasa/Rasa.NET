using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class RelatedMissionFailureAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_mission_action_kind_parameter_set",
                table: "mission_action");

            migrationBuilder.AddCheckConstraint(
                name: "CK_mission_action_kind_parameter_set",
                table: "mission_action",
                sql: "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)) AND (kind >= 10 OR item_intent IS NULL) AND (kind < 10 OR (item_intent IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 1 OR (target_objective_id IS NOT NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 2 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 3 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 4 OR (reward_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 5 OR (scenario_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 6 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 7 OR (indicator_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 8 OR (player_flag_id IS NOT NULL AND player_flag_value IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND npc_package_id IS NULL)) AND (kind <> 9 OR (npc_package_id IS NOT NULL AND player_flag_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_value IS NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_mission_action_kind_parameter_set",
                table: "mission_action");

            migrationBuilder.AddCheckConstraint(
                name: "CK_mission_action_kind_parameter_set",
                table: "mission_action",
                sql: "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)) AND (kind >= 10 OR item_intent IS NULL) AND (kind < 10 OR (item_intent IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 1 OR (target_objective_id IS NOT NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 2 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 3 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 4 OR (reward_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 5 OR (scenario_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 6 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 7 OR (indicator_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 8 OR (player_flag_id IS NOT NULL AND player_flag_value IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND npc_package_id IS NULL)) AND (kind <> 9 OR (npc_package_id IS NOT NULL AND player_flag_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_value IS NULL))");
        }
    }
}
