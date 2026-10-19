using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlChar
{
    using Context.Char;

    /// <summary>
    /// Indexes for queries the game server runs while it is up that read their whole table:
    ///  - character_inventory (character_id, invenotry_type): the wager inventories of a list of
    ///    characters (CharacterInventoryRepository.GetByType); the table's other index begins
    ///    with account_id, so every item on the server was read.
    ///  - mission_scene (assignment_id) and mission_scene_participant (assignment_id): an
    ///    assignment's scene and its participations (MissionRuntimeRepository.AssignmentScene,
    ///    Participations). Scenes are kept, so these grow with play.
    ///  - petition (account_id, notice_pending): the notices waiting for an account
    ///    (PetitionRepository.ListNoticesPending).
    ///  - pvp_match (kind, ended_at): the matches still running, the clan feuds
    ///    (PvpRecordRepository.GetOpenMatches); the table is the whole match history.
    ///  - squad_instance (owner_character_id, map_context_id) and squad_instance_visitor
    ///    (instance_id): a squad's instance of a map and who was last in one
    ///    (SquadInstanceRepository).
    /// No data changes.
    ///
    /// Down drops them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20270119000000_Add_runtime_indexes")]
    public partial class Add_runtime_indexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "character_inventory_index_character_type",
                table: "character_inventory",
                columns: new[] { "character_id", "invenotry_type" });

            migrationBuilder.CreateIndex(
                name: "mission_scene_index_assignment",
                table: "mission_scene",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "mission_scene_participant_index_assignment",
                table: "mission_scene_participant",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "petition_index_account_notice",
                table: "petition",
                columns: new[] { "account_id", "notice_pending" });

            migrationBuilder.CreateIndex(
                name: "pvp_match_index_kind_ended",
                table: "pvp_match",
                columns: new[] { "kind", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "squad_instance_index_owner_map",
                table: "squad_instance",
                columns: new[] { "owner_character_id", "map_context_id" });

            migrationBuilder.CreateIndex(
                name: "squad_instance_visitor_index_instance",
                table: "squad_instance_visitor",
                column: "instance_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "squad_instance_visitor_index_instance", table: "squad_instance_visitor");
            migrationBuilder.DropIndex(name: "squad_instance_index_owner_map", table: "squad_instance");
            migrationBuilder.DropIndex(name: "pvp_match_index_kind_ended", table: "pvp_match");
            migrationBuilder.DropIndex(name: "petition_index_account_notice", table: "petition");
            migrationBuilder.DropIndex(name: "mission_scene_participant_index_assignment", table: "mission_scene_participant");
            migrationBuilder.DropIndex(name: "mission_scene_index_assignment", table: "mission_scene");
            migrationBuilder.DropIndex(name: "character_inventory_index_character_type", table: "character_inventory");
        }
    }
}
