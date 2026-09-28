using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class ConsolidatedWorldSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "action",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    module = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_charged = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "action_cost",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<uint>(type: "int unsigned", nullable: false),
                    attribute_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    cost = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_cost", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "action_item_requirement",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    quantity = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_item_requirement", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "action_level",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<uint>(type: "int unsigned", nullable: false),
                    windup_ms = table.Column<int>(type: "int", nullable: false),
                    windup_anim_family_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    recovery_ms = table.Column<int>(type: "int", nullable: false),
                    recovery_anim_family_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    max_range = table.Column<int>(type: "int", nullable: false),
                    reuse_ms = table.Column<int>(type: "int", nullable: false),
                    preload = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    start_reuse_on_perform = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_level", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "action_property",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<uint>(type: "int unsigned", nullable: false),
                    property_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_property", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "creature_class_flag",
                columns: table => new
                {
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    flag_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_class_flag", x => new { x.class_id, x.flag_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_template_action",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_template_action", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kraftwerks",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kraftwerks", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "map_link",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: false),
                    dest_map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    dest_pos_x = table.Column<double>(type: "double", nullable: false),
                    dest_pos_y = table.Column<double>(type: "double", nullable: false),
                    dest_pos_z = table.Column<double>(type: "double", nullable: false),
                    dest_rotation = table.Column<double>(type: "double", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    enabled = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_link", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "map_marker",
                columns: table => new
                {
                    marker_entity_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    marker_type = table.Column<uint>(type: "int unsigned", nullable: false),
                    object_kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    object_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    match_distance = table.Column<double>(type: "double", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_marker", x => new { x.marker_entity_id, x.map_context_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "map_region",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    region_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    shape = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: false),
                    half_x = table.Column<double>(type: "double", nullable: false),
                    half_z = table.Column<double>(type: "double", nullable: false),
                    min_y = table.Column<double>(type: "double", nullable: false),
                    max_y = table.Column<double>(type: "double", nullable: false),
                    underground = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    enabled = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(96)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_region", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_content_definition",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    abandonment_policy = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    client_name_text_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    giver_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    receiver_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    level = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    group_type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    category_id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    shareable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    radio_completeable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_content_definition", x => new { x.mission_id, x.content_revision });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_experience_binding",
                columns: table => new
                {
                    experience_key = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    bindings = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_experience_binding", x => x.experience_key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recipe",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    energy_cost = table.Column<uint>(type: "int unsigned", nullable: false),
                    kraftwerks_seconds = table.Column<uint>(type: "int unsigned", nullable: false),
                    result_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    result_amount = table.Column<uint>(type: "int unsigned", nullable: false),
                    min_level = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recipe_input",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    recipe_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    input_class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    quantity = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_input", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skill_character",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    required_level = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_character", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_area",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    area_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    shape = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: true),
                    extent_x = table.Column<double>(type: "double", nullable: true),
                    extent_y = table.Column<double>(type: "double", nullable: true),
                    extent_z = table.Column<double>(type: "double", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_area", x => new { x.mission_id, x.content_revision, x.area_id });
                    table.ForeignKey(
                        name: "FK_mission_area_mission_content_definition_mission_id_content_r~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_channel_policy",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    acceptance_channel = table.Column<int>(type: "int", nullable: false),
                    completion_channel = table.Column<int>(type: "int", nullable: false),
                    radio_sources = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_channel_policy", x => new { x.mission_id, x.content_revision });
                    table.CheckConstraint("CK_mission_channel_policy_channels", "acceptance_channel IN (1, 2, 3) AND completion_channel IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_mission_channel_policy_mission_content_definition_mission_id~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_evidence",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    evidence_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    owner_kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    owner_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    source_kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    source_uri = table.Column<string>(type: "varchar(256)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    local_client_path = table.Column<string>(type: "varchar(256)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    confidence = table.Column<double>(type: "double unsigned", nullable: false),
                    reconstruction_note = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_evidence", x => new { x.mission_id, x.content_revision, x.evidence_id });
                    table.CheckConstraint("CK_mission_evidence_source_location", "source_uri IS NOT NULL OR local_client_path IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_mission_evidence_mission_content_definition_mission_id_conte~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_objective_definition",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    client_name_text_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    client_body_text_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    client_counter_0_text_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    client_counter_1_text_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    client_counter_2_text_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    ordinal = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    initial_state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    is_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_objective_definition", x => new { x.mission_id, x.content_revision, x.objective_id });
                    table.ForeignKey(
                        name: "FK_mission_objective_definition_mission_content_definition_miss~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_prerequisite",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prerequisite_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    required_mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    required_mission_state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    required_level = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    player_flag_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    player_flag_value = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_prerequisite", x => new { x.mission_id, x.content_revision, x.prerequisite_id });
                    table.ForeignKey(
                        name: "FK_mission_prerequisite_mission_content_definition_mission_id_c~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_repeat_policy",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    repeat_kind = table.Column<int>(type: "int", nullable: false),
                    cooldown_seconds = table.Column<uint>(type: "int unsigned", nullable: true),
                    reset_second_utc = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_repeat_policy", x => new { x.mission_id, x.content_revision });
                    table.CheckConstraint("CK_mission_repeat_policy_parameters", "(repeat_kind IN (0, 1) AND cooldown_seconds IS NULL AND reset_second_utc IS NULL) OR (repeat_kind = 2 AND cooldown_seconds IS NOT NULL AND cooldown_seconds > 0 AND reset_second_utc IS NULL) OR (repeat_kind = 3 AND cooldown_seconds IS NULL AND reset_second_utc IS NOT NULL AND reset_second_utc BETWEEN 0 AND 86399)");
                    table.ForeignKey(
                        name: "FK_mission_repeat_policy_mission_content_definition_mission_id_~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_reward_definition",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reward_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    experience = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    credits = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    prestige = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    selection_count = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_reward_definition", x => new { x.mission_id, x.content_revision, x.reward_id });
                    table.CheckConstraint("CK_mission_reward_definition_selection_count", "selection_count IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_mission_reward_definition_mission_content_definition_mission~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_scenario",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scenario_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    start_policy = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)1),
                    name = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_scenario", x => new { x.mission_id, x.content_revision, x.scenario_id });
                    table.ForeignKey(
                        name: "FK_mission_scenario_mission_content_definition_mission_id_conte~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_scene_binding",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    script_key = table.Column<string>(type: "varchar(64)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state_version = table.Column<int>(type: "int", nullable: false),
                    bindings = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_scene_binding", x => new { x.mission_id, x.content_revision });
                    table.ForeignKey(
                        name: "FK_mission_scene_binding_mission_content_definition_mission_id_~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_spawn_group",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    spawn_group_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    area_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    respawn_seconds = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    spawn_policy = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_spawn_group", x => new { x.mission_id, x.content_revision, x.spawn_group_id });
                    table.ForeignKey(
                        name: "FK_mission_spawn_group_mission_area_mission_id_content_revision~",
                        columns: x => new { x.mission_id, x.content_revision, x.area_id },
                        principalTable: "mission_area",
                        principalColumns: new[] { "mission_id", "content_revision", "area_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_spawn_group_mission_content_definition_mission_id_co~",
                        columns: x => new { x.mission_id, x.content_revision },
                        principalTable: "mission_content_definition",
                        principalColumns: new[] { "mission_id", "content_revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_indicator",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    indicator_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: false),
                    show_3d_effect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_indicator", x => new { x.mission_id, x.content_revision, x.objective_id, x.indicator_id });
                    table.ForeignKey(
                        name: "FK_mission_indicator_mission_objective_definition_mission_id_co~",
                        columns: x => new { x.mission_id, x.content_revision, x.objective_id },
                        principalTable: "mission_objective_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_objective_transition",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    transition_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    sequence = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    from_state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    to_state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_objective_transition", x => new { x.mission_id, x.content_revision, x.objective_id, x.transition_id });
                    table.ForeignKey(
                        name: "FK_mission_objective_transition_mission_objective_definition_mi~",
                        columns: x => new { x.mission_id, x.content_revision, x.objective_id },
                        principalTable: "mission_objective_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_reward_item",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reward_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    item_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    quantity = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_reward_item", x => new { x.mission_id, x.content_revision, x.reward_id, x.item_id });
                    table.CheckConstraint("CK_mission_reward_item_kind", "kind IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_mission_reward_item_mission_reward_definition_mission_id_con~",
                        columns: x => new { x.mission_id, x.content_revision, x.reward_id },
                        principalTable: "mission_reward_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "reward_id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_spawn",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    spawn_group_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    spawn_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    creature_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    quantity = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_spawn", x => new { x.mission_id, x.content_revision, x.spawn_group_id, x.spawn_id });
                    table.ForeignKey(
                        name: "FK_mission_spawn_mission_spawn_group_mission_id_content_revisio~",
                        columns: x => new { x.mission_id, x.content_revision, x.spawn_group_id },
                        principalTable: "mission_spawn_group",
                        principalColumns: new[] { "mission_id", "content_revision", "spawn_group_id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_action",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    transition_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    action_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    sequence = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    target_objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    objective_state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    reward_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    spawn_group_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    scenario_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    indicator_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    player_flag_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    player_flag_value = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    npc_package_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_intent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_action", x => new { x.mission_id, x.content_revision, x.objective_id, x.transition_id, x.action_id });
                    table.CheckConstraint("CK_mission_action_kind_parameter_set", "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)) AND (kind >= 10 OR item_intent IS NULL) AND (kind < 10 OR (item_intent IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 1 OR (target_objective_id IS NOT NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 2 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 3 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 4 OR (reward_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 5 OR (scenario_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 6 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 7 OR (indicator_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL AND npc_package_id IS NULL)) AND (kind <> 8 OR (player_flag_id IS NOT NULL AND player_flag_value IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND npc_package_id IS NULL)) AND (kind <> 9 OR (npc_package_id IS NOT NULL AND player_flag_id IS NOT NULL AND target_objective_id IS NULL AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_value IS NULL))");
                    table.ForeignKey(
                        name: "FK_mission_action_mission_indicator_mission_id_content_revision~",
                        columns: x => new { x.mission_id, x.content_revision, x.objective_id, x.indicator_id },
                        principalTable: "mission_indicator",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id", "indicator_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_action_mission_objective_definition_mission_id_conte~",
                        columns: x => new { x.mission_id, x.content_revision, x.target_objective_id },
                        principalTable: "mission_objective_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_action_mission_objective_transition_mission_id_conte~",
                        columns: x => new { x.mission_id, x.content_revision, x.objective_id, x.transition_id },
                        principalTable: "mission_objective_transition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id", "transition_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_action_mission_reward_definition_mission_id_content_~",
                        columns: x => new { x.mission_id, x.content_revision, x.reward_id },
                        principalTable: "mission_reward_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "reward_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_action_mission_scenario_mission_id_content_revision_~",
                        columns: x => new { x.mission_id, x.content_revision, x.scenario_id },
                        principalTable: "mission_scenario",
                        principalColumns: new[] { "mission_id", "content_revision", "scenario_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_action_mission_spawn_group_mission_id_content_revisi~",
                        columns: x => new { x.mission_id, x.content_revision, x.spawn_group_id },
                        principalTable: "mission_spawn_group",
                        principalColumns: new[] { "mission_id", "content_revision", "spawn_group_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_trigger",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    transition_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    trigger_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    sequence = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    related_objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    related_state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    event_kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    subject_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    counter_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    initial_value = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    target_value = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    area_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    duration_seconds = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    npc_package_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    player_flag_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    source_spawn_resolved = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_trigger", x => new { x.mission_id, x.content_revision, x.objective_id, x.transition_id, x.trigger_id });
                    table.CheckConstraint("CK_mission_trigger_kind_parameter_set", "(kind IN (1, 2, 3, 4, 5)) AND (kind <> 1 OR (npc_package_id IS NOT NULL AND player_flag_id IS NOT NULL AND related_objective_id IS NULL AND related_state IS NULL AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL AND initial_value IS NULL AND target_value IS NULL AND area_id IS NULL AND duration_seconds IS NULL AND source_spawn_resolved IS NULL)) AND (kind <> 2 OR (event_kind IS NOT NULL AND subject_id IS NOT NULL AND related_objective_id IS NULL AND related_state IS NULL AND area_id IS NULL AND duration_seconds IS NULL AND npc_package_id IS NULL AND player_flag_id IS NULL)) AND (kind <> 3 OR (related_objective_id IS NOT NULL AND related_state IS NOT NULL AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL AND initial_value IS NULL AND target_value IS NULL AND area_id IS NULL AND duration_seconds IS NULL AND npc_package_id IS NULL AND player_flag_id IS NULL AND source_spawn_resolved IS NULL)) AND (kind <> 4 OR (area_id IS NOT NULL AND related_objective_id IS NULL AND related_state IS NULL AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL AND initial_value IS NULL AND target_value IS NULL AND duration_seconds IS NULL AND npc_package_id IS NULL AND player_flag_id IS NULL AND source_spawn_resolved IS NULL)) AND (kind <> 5 OR (duration_seconds IS NOT NULL AND related_objective_id IS NULL AND related_state IS NULL AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL AND initial_value IS NULL AND target_value IS NULL AND area_id IS NULL AND npc_package_id IS NULL AND player_flag_id IS NULL AND source_spawn_resolved IS NULL))");
                    table.ForeignKey(
                        name: "FK_mission_trigger_mission_area_mission_id_content_revision_are~",
                        columns: x => new { x.mission_id, x.content_revision, x.area_id },
                        principalTable: "mission_area",
                        principalColumns: new[] { "mission_id", "content_revision", "area_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_trigger_mission_objective_definition_mission_id_cont~",
                        columns: x => new { x.mission_id, x.content_revision, x.related_objective_id },
                        principalTable: "mission_objective_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_trigger_mission_objective_transition_mission_id_cont~",
                        columns: x => new { x.mission_id, x.content_revision, x.objective_id, x.transition_id },
                        principalTable: "mission_objective_transition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id", "transition_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mission_scenario_step",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scenario_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    step_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    sequence = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    target_objective_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    reward_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    spawn_group_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    spawn_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    dynamic_object_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_class_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    target_scenario_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    delay_milliseconds = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    skill_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    ability_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    skill_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    ability_slot = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    tutorial_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    audio_set_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    attempt_key = table.Column<string>(type: "varchar(64)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scenario_event_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    pos_x = table.Column<double>(type: "double", nullable: true),
                    pos_y = table.Column<double>(type: "double", nullable: true),
                    pos_z = table.Column<double>(type: "double", nullable: true),
                    orientation = table.Column<double>(type: "double", nullable: true),
                    initial_interaction_enabled = table.Column<ulong>(type: "bit", nullable: true),
                    qualification_key = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    qualification_value = table.Column<byte>(type: "tinyint(3) unsigned", nullable: true),
                    account_skip_entitlement = table.Column<ulong>(type: "bit", nullable: true),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_scenario_step", x => new { x.mission_id, x.content_revision, x.scenario_id, x.step_id });
                    table.CheckConstraint("CK_mission_scenario_step_kind_parameter_set", "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23)) AND (kind <> 1 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 2 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 3 OR ((((entity_class_id IS NOT NULL AND spawn_group_id IS NULL AND spawn_id IS NULL) OR (entity_class_id IS NULL AND spawn_group_id IS NOT NULL AND spawn_id IS NOT NULL)) AND target_objective_id IS NULL AND reward_id IS NULL AND dynamic_object_key IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL))) AND (kind <> 4 OR ((((entity_class_id IS NOT NULL AND spawn_group_id IS NULL AND spawn_id IS NULL) OR (entity_class_id IS NULL AND spawn_group_id IS NOT NULL AND spawn_id IS NOT NULL)) AND target_objective_id IS NULL AND reward_id IS NULL AND dynamic_object_key IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL))) AND (kind <> 5 OR (target_objective_id IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 6 OR (target_objective_id IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 7 OR (target_objective_id IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 8 OR (target_objective_id IS NOT NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 9 OR (delay_milliseconds IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 10 OR (target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 11 OR (reward_id IS NOT NULL AND target_objective_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 12 OR (skill_id IS NOT NULL AND ability_id IS NOT NULL AND skill_level IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 13 OR (tutorial_id IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 14 OR (target_scenario_id IS NOT NULL AND delay_milliseconds IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 15 OR ((((target_scenario_id IS NOT NULL AND attempt_key IS NULL) OR (target_scenario_id IS NULL AND attempt_key IS NOT NULL AND attempt_key <> '')) AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL))) AND (kind <> 16 OR (scenario_event_id IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 17 OR (map_context_id IS NOT NULL AND pos_x IS NOT NULL AND pos_y IS NOT NULL AND pos_z IS NOT NULL AND orientation IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 18 OR (qualification_key IS NOT NULL AND qualification_value IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 19 OR (account_skip_entitlement IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL)) AND (kind <> 20 OR (dynamic_object_key IS NOT NULL AND dynamic_object_key <> '' AND entity_class_id IS NOT NULL AND pos_x IS NOT NULL AND pos_y IS NOT NULL AND pos_z IS NOT NULL AND orientation IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND target_scenario_id IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 21 OR (dynamic_object_key IS NOT NULL AND dynamic_object_key <> '' AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 22 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL AND reward_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL)) AND (kind <> 23 OR (target_objective_id IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL AND spawn_id IS NULL AND dynamic_object_key IS NULL AND entity_class_id IS NULL AND target_scenario_id IS NULL AND delay_milliseconds IS NULL AND skill_id IS NULL AND ability_id IS NULL AND skill_level IS NULL AND ability_slot IS NULL AND tutorial_id IS NULL AND audio_set_id IS NULL AND attempt_key IS NULL AND scenario_event_id IS NULL AND map_context_id IS NULL AND pos_x IS NULL AND pos_y IS NULL AND pos_z IS NULL AND orientation IS NULL AND initial_interaction_enabled IS NULL AND qualification_key IS NULL AND qualification_value IS NULL AND account_skip_entitlement IS NULL))");
                    table.CheckConstraint("CK_mission_scenario_step_numeric_bounds", "(target_objective_id IS NULL OR target_objective_id > 0) AND (reward_id IS NULL OR reward_id > 0) AND (spawn_group_id IS NULL OR spawn_group_id > 0) AND (spawn_id IS NULL OR spawn_id > 0) AND (entity_class_id IS NULL OR entity_class_id > 0) AND (target_scenario_id IS NULL OR target_scenario_id > 0) AND (delay_milliseconds IS NULL OR (delay_milliseconds >= 1 AND delay_milliseconds <= 86400000)) AND (skill_id IS NULL OR skill_id > 0) AND (ability_id IS NULL OR (ability_id >= 1 AND ability_id <= 2147483647)) AND (skill_level IS NULL OR (skill_level >= 1 AND skill_level <= 5)) AND (ability_slot IS NULL OR ability_slot <= 24) AND (tutorial_id IS NULL OR tutorial_id > 0) AND (audio_set_id IS NULL OR audio_set_id > 0) AND (attempt_key IS NULL OR attempt_key <> '') AND (dynamic_object_key IS NULL OR dynamic_object_key <> '') AND (scenario_event_id IS NULL OR scenario_event_id > 0) AND (map_context_id IS NULL OR map_context_id > 0) AND (qualification_key IS NULL OR (qualification_key >= 1 AND qualification_key <= 255)) AND (qualification_value IS NULL OR (qualification_value >= 0 AND qualification_value <= 1))");
                    table.ForeignKey(
                        name: "FK_mission_scenario_step_mission_objective_definition_mission_i~",
                        columns: x => new { x.mission_id, x.content_revision, x.target_objective_id },
                        principalTable: "mission_objective_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "objective_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_scenario_step_mission_reward_definition_mission_id_c~",
                        columns: x => new { x.mission_id, x.content_revision, x.reward_id },
                        principalTable: "mission_reward_definition",
                        principalColumns: new[] { "mission_id", "content_revision", "reward_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_scenario_step_mission_scenario_mission_id_content_re~",
                        columns: x => new { x.mission_id, x.content_revision, x.scenario_id },
                        principalTable: "mission_scenario",
                        principalColumns: new[] { "mission_id", "content_revision", "scenario_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_scenario_step_mission_scenario_mission_id_content_r~1",
                        columns: x => new { x.mission_id, x.content_revision, x.target_scenario_id },
                        principalTable: "mission_scenario",
                        principalColumns: new[] { "mission_id", "content_revision", "scenario_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_scenario_step_mission_spawn_group_mission_id_content~",
                        columns: x => new { x.mission_id, x.content_revision, x.spawn_group_id },
                        principalTable: "mission_spawn_group",
                        principalColumns: new[] { "mission_id", "content_revision", "spawn_group_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_scenario_step_mission_spawn_mission_id_content_revis~",
                        columns: x => new { x.mission_id, x.content_revision, x.spawn_group_id, x.spawn_id },
                        principalTable: "mission_spawn",
                        principalColumns: new[] { "mission_id", "content_revision", "spawn_group_id", "spawn_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "map_marker_index_map_context_id",
                table: "map_marker",
                column: "map_context_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_mission_id_content_revision_objective_id_indi~",
                table: "mission_action",
                columns: new[] { "mission_id", "content_revision", "objective_id", "indicator_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_mission_id_content_revision_reward_id",
                table: "mission_action",
                columns: new[] { "mission_id", "content_revision", "reward_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_mission_id_content_revision_scenario_id",
                table: "mission_action",
                columns: new[] { "mission_id", "content_revision", "scenario_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_mission_id_content_revision_spawn_group_id",
                table: "mission_action",
                columns: new[] { "mission_id", "content_revision", "spawn_group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_mission_id_content_revision_target_objective_~",
                table: "mission_action",
                columns: new[] { "mission_id", "content_revision", "target_objective_id" });

            migrationBuilder.CreateIndex(
                name: "mission_content_definition_index_content_revision",
                table: "mission_content_definition",
                column: "content_revision");

            migrationBuilder.CreateIndex(
                name: "IX_mission_scenario_step_mission_id_content_revision_reward_id",
                table: "mission_scenario_step",
                columns: new[] { "mission_id", "content_revision", "reward_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_scenario_step_mission_id_content_revision_spawn_grou~",
                table: "mission_scenario_step",
                columns: new[] { "mission_id", "content_revision", "spawn_group_id", "spawn_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_scenario_step_mission_id_content_revision_target_obj~",
                table: "mission_scenario_step",
                columns: new[] { "mission_id", "content_revision", "target_objective_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_scenario_step_mission_id_content_revision_target_sce~",
                table: "mission_scenario_step",
                columns: new[] { "mission_id", "content_revision", "target_scenario_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_spawn_group_mission_id_content_revision_area_id",
                table: "mission_spawn_group",
                columns: new[] { "mission_id", "content_revision", "area_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_trigger_mission_id_content_revision_area_id",
                table: "mission_trigger",
                columns: new[] { "mission_id", "content_revision", "area_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_trigger_mission_id_content_revision_related_objectiv~",
                table: "mission_trigger",
                columns: new[] { "mission_id", "content_revision", "related_objective_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "action");

            migrationBuilder.DropTable(
                name: "action_cost");

            migrationBuilder.DropTable(
                name: "action_item_requirement");

            migrationBuilder.DropTable(
                name: "action_level");

            migrationBuilder.DropTable(
                name: "action_property");

            migrationBuilder.DropTable(
                name: "creature_class_flag");

            migrationBuilder.DropTable(
                name: "item_template_action");

            migrationBuilder.DropTable(
                name: "kraftwerks");

            migrationBuilder.DropTable(
                name: "map_link");

            migrationBuilder.DropTable(
                name: "map_marker");

            migrationBuilder.DropTable(
                name: "map_region");

            migrationBuilder.DropTable(
                name: "mission_action");

            migrationBuilder.DropTable(
                name: "mission_channel_policy");

            migrationBuilder.DropTable(
                name: "mission_evidence");

            migrationBuilder.DropTable(
                name: "mission_experience_binding");

            migrationBuilder.DropTable(
                name: "mission_prerequisite");

            migrationBuilder.DropTable(
                name: "mission_repeat_policy");

            migrationBuilder.DropTable(
                name: "mission_reward_item");

            migrationBuilder.DropTable(
                name: "mission_scenario_step");

            migrationBuilder.DropTable(
                name: "mission_scene_binding");

            migrationBuilder.DropTable(
                name: "mission_trigger");

            migrationBuilder.DropTable(
                name: "recipe");

            migrationBuilder.DropTable(
                name: "recipe_input");

            migrationBuilder.DropTable(
                name: "skill_character");

            migrationBuilder.DropTable(
                name: "mission_indicator");

            migrationBuilder.DropTable(
                name: "mission_reward_definition");

            migrationBuilder.DropTable(
                name: "mission_scenario");

            migrationBuilder.DropTable(
                name: "mission_spawn");

            migrationBuilder.DropTable(
                name: "mission_objective_transition");

            migrationBuilder.DropTable(
                name: "mission_spawn_group");

            migrationBuilder.DropTable(
                name: "mission_objective_definition");

            migrationBuilder.DropTable(
                name: "mission_area");

            migrationBuilder.DropTable(
                name: "mission_content_definition");
        }
    }
}
