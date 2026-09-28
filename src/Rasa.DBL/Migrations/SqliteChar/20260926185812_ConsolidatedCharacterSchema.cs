using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.SqliteChar
{
    /// <inheritdoc />
    public partial class ConsolidatedCharacterSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ignored",
                table: "ignored");

            migrationBuilder.DropPrimaryKey(
                name: "PK_friend",
                table: "friend");

            migrationBuilder.DropPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission");

            migrationBuilder.AlterColumn<uint>(
                name: "account_id",
                table: "ignored",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<uint>(
                name: "account_id",
                table: "friend",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<uint>(
                name: "character_id",
                table: "character_mission",
                type: "integer",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "assignment_id",
                table: "character_mission",
                type: "varchar(32)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "completeable",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "content_revision",
                table: "character_mission",
                type: "varchar(32)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "generation",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte>(
                name: "current_ability_slot",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ignored",
                table: "ignored",
                columns: new[] { "account_id", "ignored_account_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_friend",
                table: "friend",
                columns: new[] { "account_id", "friend_account_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission",
                columns: new[] { "character_id", "mission_id" });

            migrationBuilder.CreateTable(
                name: "auction",
                columns: table => new
                {
                    item_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seller_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    seller_name = table.Column<string>(type: "varchar(64)", nullable: false),
                    price = table.Column<uint>(type: "INTEGER", nullable: false),
                    deposit = table.Column<uint>(type: "INTEGER", nullable: false),
                    duration_hours = table.Column<uint>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "character_flag",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    flag_id = table.Column<uint>(type: "int(11)", nullable: false),
                    value = table.Column<uint>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_flag", x => new { x.character_id, x.flag_id });
                    table.CheckConstraint("CK_character_flag_id", "flag_id BETWEEN 1 AND 4294967295");
                    table.CheckConstraint("CK_character_flag_value", "value BETWEEN 0 AND 4294967295");
                    table.ForeignKey(
                        name: "FK_character_flag_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_deadline",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    due_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    state = table.Column<byte>(type: "tinyint(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_deadline", x => new { x.character_id, x.mission_id });
                    table.CheckConstraint("CK_character_mission_deadline_state", "state IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_character_mission_deadline_character_mission_character_id_mission_id",
                        columns: x => new { x.character_id, x.mission_id },
                        principalTable: "character_mission",
                        principalColumns: new[] { "character_id", "mission_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_history",
                columns: table => new
                {
                    assignment_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    assignment_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    rewarded = table.Column<bool>(type: "INTEGER", nullable: false),
                    rewarded_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reward_window_start_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    outcome = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_history", x => x.assignment_id);
                    table.ForeignKey(
                        name: "FK_character_mission_history_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_item",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    assignment_id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    item_key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    item_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    quantity = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_item", x => new { x.character_id, x.mission_id, x.assignment_id, x.item_key, x.item_id });
                    table.CheckConstraint("CK_mission_item_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_character_mission_item_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_item_quarantine",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    assignment_id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_item_quarantine", x => new { x.character_id, x.assignment_id });
                    table.ForeignKey(
                        name: "FK_character_mission_item_quarantine_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_item_receipt",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    assignment_id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    operation_key = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_item_receipt", x => new { x.character_id, x.assignment_id, x.operation_key });
                    table.ForeignKey(
                        name: "FK_character_mission_item_receipt_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_objective",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_state = table.Column<byte>(type: "tinyint(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_objective", x => new { x.character_id, x.mission_id, x.objective_id });
                    table.ForeignKey(
                        name: "FK_character_mission_objective_character_mission_character_id_mission_id",
                        columns: x => new { x.character_id, x.mission_id },
                        principalTable: "character_mission",
                        principalColumns: new[] { "character_id", "mission_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_offer",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    offer_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false),
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    player_entity_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    session_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    player_epoch = table.Column<Guid>(type: "char(36)", nullable: false),
                    map_epoch = table.Column<Guid>(type: "char(36)", nullable: false),
                    source_kind = table.Column<int>(type: "INTEGER", nullable: false),
                    source_key = table.Column<string>(type: "varchar(96)", nullable: false),
                    source_instance_id = table.Column<string>(type: "varchar(96)", nullable: false),
                    source_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    source_assignment_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    source_assignment_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    party_source = table.Column<string>(type: "text", nullable: true),
                    prior_assignment_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    prior_assignment_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    prior_assignment_revision = table.Column<string>(type: "varchar(32)", nullable: true),
                    prior_history_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    consumed_assignment_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    consumed_assignment_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_offer", x => new { x.character_id, x.mission_id });
                    table.ForeignKey(
                        name: "FK_character_mission_offer_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_scenario_step",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    step_key = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_scenario_step", x => new { x.character_id, x.mission_id, x.step_key });
                    table.ForeignKey(
                        name: "FK_character_mission_scenario_step_character_mission_character_id_mission_id",
                        columns: x => new { x.character_id, x.mission_id },
                        principalTable: "character_mission",
                        principalColumns: new[] { "character_id", "mission_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_starting_experience",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false),
                    state = table.Column<byte>(type: "tinyint(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_starting_experience", x => x.character_id);
                    table.CheckConstraint("CK_character_starting_experience_state", "state IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_character_starting_experience_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clan_lockbox_log",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    transaction_type = table.Column<byte>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    character_name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    credit_type = table.Column<byte>(type: "INTEGER", nullable: false),
                    amount = table.Column<long>(type: "INTEGER", nullable: false),
                    item_template_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    quantity = table.Column<uint>(type: "INTEGER", nullable: false),
                    transaction_time = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_lockbox_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mission_outcome",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    run_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_outcome", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "mission_receipt",
                columns: table => new
                {
                    owner_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    operation_key = table.Column<string>(type: "varchar(96)", nullable: false),
                    kind = table.Column<string>(type: "varchar(16)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_receipt", x => new { x.owner_id, x.generation, x.operation_key });
                });

            migrationBuilder.CreateTable(
                name: "mission_scene",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    release = table.Column<string>(type: "varchar(32)", nullable: true),
                    script_key = table.Column<string>(type: "varchar(64)", nullable: true),
                    state_version = table.Column<int>(type: "INTEGER", nullable: false),
                    owner_character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    map_key = table.Column<string>(type: "varchar(64)", nullable: true),
                    assignment_id = table.Column<string>(type: "varchar(32)", nullable: false, defaultValue: ""),
                    checkpoint = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(16)", nullable: true),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    version = table.Column<long>(type: "INTEGER", nullable: false),
                    fault = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_scene", x => x.run_id);
                });

            migrationBuilder.CreateTable(
                name: "petition",
                columns: table => new
                {
                    id = table.Column<uint>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<uint>(type: "integer", nullable: false),
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    summary = table.Column<string>(type: "varchar(255)", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    map_context_id = table.Column<uint>(type: "int(11)", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    status = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    resolution = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_petition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_objective_counter",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    counter_id = table.Column<uint>(type: "int(11)", nullable: false),
                    counter_value = table.Column<uint>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_objective_counter", x => new { x.character_id, x.mission_id, x.objective_id, x.counter_id });
                    table.ForeignKey(
                        name: "FK_character_mission_objective_counter_character_mission_objective_character_id_mission_id_objective_id",
                        columns: x => new { x.character_id, x.mission_id, x.objective_id },
                        principalTable: "character_mission_objective",
                        principalColumns: new[] { "character_id", "mission_id", "objective_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mission_objective_item_counter",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "integer", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    item_class_id = table.Column<uint>(type: "int(11)", nullable: false),
                    counter_value = table.Column<uint>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_objective_item_counter", x => new { x.character_id, x.mission_id, x.objective_id, x.item_class_id });
                    table.ForeignKey(
                        name: "FK_character_mission_objective_item_counter_character_mission_objective_character_id_mission_id_objective_id",
                        columns: x => new { x.character_id, x.mission_id, x.objective_id },
                        principalTable: "character_mission_objective",
                        principalColumns: new[] { "character_id", "mission_id", "objective_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_credit_delivery",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    assignment_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    assignment_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(16)", nullable: true),
                    version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_credit_delivery", x => new { x.event_id, x.assignment_id });
                    table.ForeignKey(
                        name: "FK_mission_credit_delivery_mission_outcome_event_id",
                        column: x => x.event_id,
                        principalTable: "mission_outcome",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mission_actor_lease",
                columns: table => new
                {
                    map_key = table.Column<string>(type: "varchar(64)", nullable: false),
                    spawn_key = table.Column<string>(type: "varchar(64)", nullable: false),
                    run_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    actor_role = table.Column<string>(type: "varchar(64)", nullable: true),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    state = table.Column<string>(type: "varchar(16)", nullable: true),
                    version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_actor_lease", x => new { x.map_key, x.spawn_key });
                    table.ForeignKey(
                        name: "FK_mission_actor_lease_mission_scene_run_id",
                        column: x => x.run_id,
                        principalTable: "mission_scene",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mission_actor_state",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    actor_role = table.Column<string>(type: "varchar(64)", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    owner_character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    shared_key = table.Column<string>(type: "varchar(64)", nullable: true),
                    outcome = table.Column<string>(type: "varchar(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_actor_state", x => new { x.run_id, x.actor_role, x.generation });
                    table.ForeignKey(
                        name: "FK_mission_actor_state_mission_scene_run_id",
                        column: x => x.run_id,
                        principalTable: "mission_scene",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_scene_message",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    run_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    operation_key = table.Column<string>(type: "varchar(96)", nullable: false),
                    sequence_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", nullable: false),
                    version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_scene_message", x => x.id);
                    table.ForeignKey(
                        name: "FK_mission_scene_message_mission_scene_run_id",
                        column: x => x.run_id,
                        principalTable: "mission_scene",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_scene_participant",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    assignment_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    assignment_generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_scene_participant", x => new { x.run_id, x.character_id });
                    table.ForeignKey(
                        name: "FK_mission_scene_participant_mission_scene_run_id",
                        column: x => x.run_id,
                        principalTable: "mission_scene",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_timer",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: true),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: true),
                    sequence_id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    clock_policy = table.Column<string>(type: "varchar(16)", nullable: true),
                    due_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    remaining_ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    disposition = table.Column<string>(type: "varchar(16)", nullable: true),
                    version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_timer", x => new { x.run_id, x.name });
                    table.ForeignKey(
                        name: "FK_mission_timer_mission_scene_run_id",
                        column: x => x.run_id,
                        principalTable: "mission_scene",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_world_effect",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "varchar(32)", nullable: false),
                    generation = table.Column<uint>(type: "INTEGER", nullable: false),
                    operation_key = table.Column<string>(type: "varchar(96)", nullable: false),
                    source_run_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    source_generation = table.Column<uint>(type: "INTEGER", nullable: true),
                    source_assignment_id = table.Column<string>(type: "varchar(32)", nullable: true),
                    source_assignment_generation = table.Column<uint>(type: "INTEGER", nullable: true),
                    payload = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(16)", nullable: true),
                    failure = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_world_effect", x => new { x.run_id, x.generation, x.operation_key });
                    table.ForeignKey(
                        name: "FK_mission_world_effect_mission_scene_run_id",
                        column: x => x.run_id,
                        principalTable: "mission_scene",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_mission_assignment_id",
                table: "character_mission",
                column: "assignment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "character_index_account_slot",
                table: "character",
                columns: new[] { "account_id", "slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "auction_index_seller_id",
                table: "auction",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_character_mission_history_character_id_mission_id_assignment_generation",
                table: "character_mission_history",
                columns: new[] { "character_id", "mission_id", "assignment_generation" });

            migrationBuilder.CreateIndex(
                name: "IX_character_mission_history_character_id_mission_id_reward_window_start_utc",
                table: "character_mission_history",
                columns: new[] { "character_id", "mission_id", "reward_window_start_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_character_mission_item_item_id",
                table: "character_mission_item",
                column: "item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_character_mission_offer_offer_id",
                table: "character_mission_offer",
                column: "offer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "clan_lockbox_log_index_clan_id_time",
                table: "clan_lockbox_log",
                columns: new[] { "clan_id", "transaction_time" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_actor_lease_run_id",
                table: "mission_actor_lease",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_credit_delivery_character_id_status",
                table: "mission_credit_delivery",
                columns: new[] { "character_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_scene_map_key_status",
                table: "mission_scene",
                columns: new[] { "map_key", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_scene_owner_character_id_mission_id",
                table: "mission_scene",
                columns: new[] { "owner_character_id", "mission_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_scene_owner_character_id_mission_id_script_key_assignment_id",
                table: "mission_scene",
                columns: new[] { "owner_character_id", "mission_id", "script_key", "assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mission_scene_message_run_id_generation_operation_key",
                table: "mission_scene_message",
                columns: new[] { "run_id", "generation", "operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mission_timer_disposition_due_at_utc",
                table: "mission_timer",
                columns: new[] { "disposition", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_world_effect_source_run_id",
                table: "mission_world_effect",
                column: "source_run_id");

            migrationBuilder.AddForeignKey(
                name: "FK_character_mission_character_character_id",
                table: "character_mission",
                column: "character_id",
                principalTable: "character",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_character_mission_character_character_id",
                table: "character_mission");

            migrationBuilder.DropTable(
                name: "auction");

            migrationBuilder.DropTable(
                name: "character_flag");

            migrationBuilder.DropTable(
                name: "character_mission_deadline");

            migrationBuilder.DropTable(
                name: "character_mission_history");

            migrationBuilder.DropTable(
                name: "character_mission_item");

            migrationBuilder.DropTable(
                name: "character_mission_item_quarantine");

            migrationBuilder.DropTable(
                name: "character_mission_item_receipt");

            migrationBuilder.DropTable(
                name: "character_mission_objective_counter");

            migrationBuilder.DropTable(
                name: "character_mission_objective_item_counter");

            migrationBuilder.DropTable(
                name: "character_mission_offer");

            migrationBuilder.DropTable(
                name: "character_mission_scenario_step");

            migrationBuilder.DropTable(
                name: "character_starting_experience");

            migrationBuilder.DropTable(
                name: "clan_lockbox_log");

            migrationBuilder.DropTable(
                name: "mission_actor_lease");

            migrationBuilder.DropTable(
                name: "mission_actor_state");

            migrationBuilder.DropTable(
                name: "mission_credit_delivery");

            migrationBuilder.DropTable(
                name: "mission_receipt");

            migrationBuilder.DropTable(
                name: "mission_scene_message");

            migrationBuilder.DropTable(
                name: "mission_scene_participant");

            migrationBuilder.DropTable(
                name: "mission_timer");

            migrationBuilder.DropTable(
                name: "mission_world_effect");

            migrationBuilder.DropTable(
                name: "petition");

            migrationBuilder.DropTable(
                name: "character_mission_objective");

            migrationBuilder.DropTable(
                name: "mission_outcome");

            migrationBuilder.DropTable(
                name: "mission_scene");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ignored",
                table: "ignored");

            migrationBuilder.DropPrimaryKey(
                name: "PK_friend",
                table: "friend");

            migrationBuilder.DropPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission");

            migrationBuilder.DropIndex(
                name: "IX_character_mission_assignment_id",
                table: "character_mission");

            migrationBuilder.DropIndex(
                name: "character_index_account_slot",
                table: "character");

            migrationBuilder.DropColumn(
                name: "assignment_id",
                table: "character_mission");

            migrationBuilder.DropColumn(
                name: "completeable",
                table: "character_mission");

            migrationBuilder.DropColumn(
                name: "content_revision",
                table: "character_mission");

            migrationBuilder.DropColumn(
                name: "generation",
                table: "character_mission");

            migrationBuilder.DropColumn(
                name: "version",
                table: "character_mission");

            migrationBuilder.DropColumn(
                name: "current_ability_slot",
                table: "character");

            migrationBuilder.AlterColumn<uint>(
                name: "account_id",
                table: "ignored",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<uint>(
                name: "account_id",
                table: "friend",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<uint>(
                name: "character_id",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "integer")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ignored",
                table: "ignored",
                column: "account_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_friend",
                table: "friend",
                column: "account_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission",
                column: "character_id");
        }
    }
}
