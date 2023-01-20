using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    public partial class Add_all_tables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "clan_member",
                type: "varchar(45)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "credits",
                table: "clan",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "is_pvp",
                table: "clan",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "prestige",
                table: "clan",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "purashed_tabs",
                table: "clan",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "rank_title_0",
                table: "clan",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rank_title_1",
                table: "clan",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rank_title_2",
                table: "clan",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rank_title_3",
                table: "clan",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "active_weapon",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "credit",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_pvp_clan",
                table: "character",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "prestige",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "censor_words",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    word = table.Column<string>(type: "varchar(45)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_censor_words", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_ability_drawer",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    abilitiy_slot = table.Column<int>(type: "INTEGER", nullable: false),
                    ability_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ability_level = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_ability_drawer", x => new { x.character_id, x.abilitiy_slot });
                });

            migrationBuilder.CreateTable(
                name: "character_inventory",
                columns: table => new
                {
                    item_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    invenotry_type = table.Column<uint>(type: "INTEGER", nullable: false),
                    slot_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_inventory", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "character_lockbox",
                columns: table => new
                {
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    credits = table.Column<int>(type: "INTEGER", nullable: false),
                    purashed_tabs = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_lockbox", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "character_logos",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    logos_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_logos", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "character_mission",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    mission_state = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "character_option",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    option_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    value = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_option", x => new { x.character_id, x.option_id });
                });

            migrationBuilder.CreateTable(
                name: "character_skills",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    skill_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    ability_id = table.Column<int>(type: "INTEGER", nullable: false),
                    skill_level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_skills", x => new { x.character_id, x.skill_id });
                });

            migrationBuilder.CreateTable(
                name: "character_teleporter",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    waypointId = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_teleporter", x => new { x.character_id, x.waypointId });
                });

            migrationBuilder.CreateTable(
                name: "character_title",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    title_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_title", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "clan_inventory",
                columns: table => new
                {
                    item_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    clan_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    slot_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_inventory", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "friend",
                columns: table => new
                {
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    friend_account_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "ignored",
                columns: table => new
                {
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ignored_account_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ignored", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    item_id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    item_template_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    stack_size = table.Column<uint>(type: "INTEGER", nullable: false),
                    current_hp = table.Column<int>(type: "INTEGER", nullable: false),
                    color = table.Column<uint>(type: "INTEGER", nullable: false),
                    ammo_count = table.Column<uint>(type: "INTEGER", nullable: false),
                    crafter_name = table.Column<string>(type: "varchar(64)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "user_option",
                columns: table => new
                {
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    option_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    value = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_option", x => new { x.account_id, x.option_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "censor_words");

            migrationBuilder.DropTable(
                name: "character_ability_drawer");

            migrationBuilder.DropTable(
                name: "character_inventory");

            migrationBuilder.DropTable(
                name: "character_lockbox");

            migrationBuilder.DropTable(
                name: "character_logos");

            migrationBuilder.DropTable(
                name: "character_mission");

            migrationBuilder.DropTable(
                name: "character_option");

            migrationBuilder.DropTable(
                name: "character_skills");

            migrationBuilder.DropTable(
                name: "character_teleporter");

            migrationBuilder.DropTable(
                name: "character_title");

            migrationBuilder.DropTable(
                name: "clan_inventory");

            migrationBuilder.DropTable(
                name: "friend");

            migrationBuilder.DropTable(
                name: "ignored");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "user_option");

            migrationBuilder.DropColumn(
                name: "note",
                table: "clan_member");

            migrationBuilder.DropColumn(
                name: "credits",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "is_pvp",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "prestige",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "purashed_tabs",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "rank_title_0",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "rank_title_1",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "rank_title_2",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "rank_title_3",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "active_weapon",
                table: "character");

            migrationBuilder.DropColumn(
                name: "credit",
                table: "character");

            migrationBuilder.DropColumn(
                name: "last_pvp_clan",
                table: "character");

            migrationBuilder.DropColumn(
                name: "prestige",
                table: "character");
        }
    }
}
