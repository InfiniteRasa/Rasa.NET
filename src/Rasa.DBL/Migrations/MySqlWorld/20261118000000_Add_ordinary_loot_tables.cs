using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class Add_ordinary_loot_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loot_map_policy",
                columns: table => new
                {
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    require_profiles = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_map_policy", x => x.map_context_id);
                    table.ForeignKey(
                        name: "FK_loot_map_policy_map_info_map_context_id",
                        column: x => x.map_context_id,
                        principalTable: "map_info",
                        principalColumn: "map_context_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "loot_profile",
                columns: table => new
                {
                    profile_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    content_revision = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provenance = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    evidence_notes = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_profile", x => x.profile_key);
                    table.CheckConstraint("CK_loot_profile_key_length", "LENGTH(profile_key) BETWEEN 1 AND 64");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "creature_loot_binding",
                columns: table => new
                {
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    creature_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    profile_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_loot_binding", x => new { x.map_context_id, x.creature_id });
                    table.ForeignKey(
                        name: "FK_creature_loot_binding_creature_creature_id",
                        column: x => x.creature_id,
                        principalTable: "creature",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_creature_loot_binding_loot_profile_profile_key",
                        column: x => x.profile_key,
                        principalTable: "loot_profile",
                        principalColumn: "profile_key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_creature_loot_binding_map_info_map_context_id",
                        column: x => x.map_context_id,
                        principalTable: "map_info",
                        principalColumn: "map_context_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "loot_group",
                columns: table => new
                {
                    profile_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    group_key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    chance_basis_points = table.Column<int>(type: "int", nullable: false),
                    minimum_rolls = table.Column<int>(type: "int", nullable: false),
                    maximum_rolls = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_group", x => new { x.profile_key, x.group_key });
                    table.CheckConstraint("CK_loot_group_chance", "chance_basis_points BETWEEN 0 AND 10000");
                    table.CheckConstraint("CK_loot_group_key_length", "LENGTH(group_key) BETWEEN 1 AND 32");
                    table.CheckConstraint("CK_loot_group_kind", "kind IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_loot_group_rolls", "minimum_rolls >= 0 AND maximum_rolls >= minimum_rolls");
                    table.ForeignKey(
                        name: "FK_loot_group_loot_profile_profile_key",
                        column: x => x.profile_key,
                        principalTable: "loot_profile",
                        principalColumn: "profile_key",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "loot_item",
                columns: table => new
                {
                    profile_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    group_key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_template_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    weight = table.Column<int>(type: "int", nullable: false),
                    minimum_quantity = table.Column<int>(type: "int", nullable: false),
                    maximum_quantity = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_item", x => new { x.profile_key, x.group_key, x.item_template_id });
                    table.CheckConstraint("CK_loot_item_quantities", "minimum_quantity > 0 AND maximum_quantity >= minimum_quantity");
                    table.CheckConstraint("CK_loot_item_weight", "weight > 0");
                    table.ForeignKey(
                        name: "FK_loot_item_itemtemplate_item_template_id",
                        column: x => x.item_template_id,
                        principalTable: "itemtemplate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loot_item_loot_group_profile_key_group_key",
                        columns: x => new { x.profile_key, x.group_key },
                        principalTable: "loot_group",
                        principalColumns: new[] { "profile_key", "group_key" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_creature_loot_binding_creature_id",
                table: "creature_loot_binding",
                column: "creature_id");

            migrationBuilder.CreateIndex(
                name: "IX_creature_loot_binding_profile_key",
                table: "creature_loot_binding",
                column: "profile_key");

            migrationBuilder.CreateIndex(
                name: "IX_loot_item_item_template_id",
                table: "loot_item",
                column: "item_template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creature_loot_binding");

            migrationBuilder.DropTable(
                name: "loot_item");

            migrationBuilder.DropTable(
                name: "loot_map_policy");

            migrationBuilder.DropTable(
                name: "loot_group");

            migrationBuilder.DropTable(
                name: "loot_profile");
        }
    }
}
