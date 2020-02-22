using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Database.Migrations.Char
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    email = table.Column<string>(type: "varchar(255)", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    selected_slot = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    family_name = table.Column<string>(type: "varchar(64)", nullable: false, defaultValueSql: "''"),
                    can_skip_bootcamp = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    last_ip = table.Column<string>(type: "varchar(15)", nullable: false, defaultValueSql: "'0.0.0.0'"),
                    last_login = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()"),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "current_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_abilitydrawer",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    characterSlot = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    abilitySlotId = table.Column<int>(type: "int(11)", nullable: false),
                    abilityId = table.Column<int>(type: "int(10)", nullable: false),
                    abilityLevel = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_abilitydrawer", x => new { x.accountId, x.characterSlot, x.abilitySlotId });
                });

            migrationBuilder.CreateTable(
                name: "character_inventory",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    characterSlot = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    itemId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    inventoryType = table.Column<int>(type: "int(11)", nullable: false),
                    slotId = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_inventory", x => new { x.accountId, x.characterSlot, x.itemId });
                });

            migrationBuilder.CreateTable(
                name: "character_lockbox",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    credits = table.Column<int>(type: "int(10)", nullable: false),
                    purashedTabs = table.Column<int>(type: "int(10)", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.accountId);
                });

            migrationBuilder.CreateTable(
                name: "character_logos",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    characterSlot = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    logosId = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_logos", x => new { x.accountId, x.characterSlot });
                });

            migrationBuilder.CreateTable(
                name: "character_missions",
                columns: table => new
                {
                    characterSlot = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    accountId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    missionId = table.Column<int>(type: "int(11)", nullable: false),
                    missionState = table.Column<sbyte>(type: "tinyint(4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_missions", x => new { x.accountId, x.characterSlot, x.missionId });
                });

            migrationBuilder.CreateTable(
                name: "character_options",
                columns: table => new
                {
                    character_id = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'NULL'"),
                    option_id = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'NULL'"),
                    value = table.Column<string>(type: "varchar(50)", nullable: true, defaultValueSql: "'NULL'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_options", x => new { x.character_id, x.option_id });
                });

            migrationBuilder.CreateTable(
                name: "character_skills",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    characterSlot = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    skillId = table.Column<int>(type: "int(11)", nullable: false),
                    abilityId = table.Column<int>(type: "int(11)", nullable: false),
                    skillLevel = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_skills", x => new { x.accountId, x.characterSlot, x.skillId, x.abilityId });
                });

            migrationBuilder.CreateTable(
                name: "character_teleporters",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "'NULL'"),
                    waypoint_id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "'NULL'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_teleporters", x => new { x.character_id, x.waypoint_id });
                },
                comment: "holds data about all waypoints and wormholes that character gained");

            migrationBuilder.CreateTable(
                name: "character_titles",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    characterSlot = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    titleId = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_titles", x => new { x.accountId, x.characterSlot, x.titleId });
                });

            migrationBuilder.CreateTable(
                name: "clan",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "'current_timestamp()'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    itemId = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    stackSize = table.Column<int>(type: "int(11)", nullable: false),
                    currentHitPoints = table.Column<int>(type: "int(11)", nullable: false),
                    color = table.Column<int>(type: "int(11)", nullable: false),
                    ammoCount = table.Column<int>(type: "int(11)", nullable: false),
                    crafterName = table.Column<string>(type: "varchar(64)", nullable: false, defaultValueSql: "''''''"),
                    createdAt = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "'current_timestamp()'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.itemId);
                });

            migrationBuilder.CreateTable(
                name: "user_options",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'NULL'"),
                    option_id = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'NULL'"),
                    value = table.Column<string>(type: "varchar(50)", nullable: true, defaultValueSql: "'NULL'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_options", x => new { x.account_id, x.option_id });
                });

            migrationBuilder.CreateTable(
                name: "character",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    account_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    slot = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    race = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    @class = table.Column<uint>(name: "class", type: "int(11) unsigned", nullable: false),
                    gender = table.Column<ulong>(type: "bit(1)", nullable: false),
                    scale = table.Column<double>(type: "double unsigned", nullable: false),
                    experience = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    body = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    mind = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    spirit = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    clone_credits = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    coord_x = table.Column<float>(nullable: false),
                    coord_y = table.Column<float>(nullable: false),
                    coord_z = table.Column<float>(nullable: false),
                    orientation = table.Column<float>(nullable: false),
                    credits = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'0'"),
                    prestige = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'0'"),
                    active_weapon = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    num_logins = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "'NULL'"),
                    total_time_played = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "'current_timestamp()'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character", x => x.id);
                    table.ForeignKey(
                        name: "character_FK_account",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "character_appearance",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    slot = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    @class = table.Column<uint>(name: "class", type: "int(11) unsigned", nullable: false),
                    color = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.character_id, x.slot });
                    table.ForeignKey(
                        name: "character_appearance_FK_character",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clan_member",
                columns: table => new
                {
                    clan_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    character_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rank = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.clan_id, x.character_id });
                    table.ForeignKey(
                        name: "clan_member_FK_character",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "clan_member_FK_clan",
                        column: x => x.clan_id,
                        principalTable: "clan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "character_index_account",
                table: "character",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "itemId",
                table: "character_inventory",
                column: "itemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "clan_member_index_character",
                table: "clan_member",
                column: "character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "id",
                table: "items",
                column: "itemId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_abilitydrawer");

            migrationBuilder.DropTable(
                name: "character_appearance");

            migrationBuilder.DropTable(
                name: "character_inventory");

            migrationBuilder.DropTable(
                name: "character_lockbox");

            migrationBuilder.DropTable(
                name: "character_logos");

            migrationBuilder.DropTable(
                name: "character_missions");

            migrationBuilder.DropTable(
                name: "character_options");

            migrationBuilder.DropTable(
                name: "character_skills");

            migrationBuilder.DropTable(
                name: "character_teleporters");

            migrationBuilder.DropTable(
                name: "character_titles");

            migrationBuilder.DropTable(
                name: "clan_member");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "user_options");

            migrationBuilder.DropTable(
                name: "character");

            migrationBuilder.DropTable(
                name: "clan");

            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
