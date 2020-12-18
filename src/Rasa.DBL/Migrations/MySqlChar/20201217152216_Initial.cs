using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    email = table.Column<string>(type: "varchar(255)", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    family_name = table.Column<string>(type: "varchar(64)", nullable: false, defaultValue: ""),
                    selected_slot = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    can_skip_bootcamp = table.Column<ulong>(type: "bit", nullable: false, defaultValue: 0ul),
                    last_ip = table.Column<string>(type: "varchar(15)", nullable: false, defaultValue: "0.0.0.0"),
                    last_login = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clan",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan", x => x.id);
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
                    gender = table.Column<ulong>(type: "bit", nullable: false),
                    scale = table.Column<double>(type: "double unsigned", nullable: false),
                    experience = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValue: 0u),
                    level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)1),
                    body = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    mind = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    spirit = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    clone_credits = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValue: 0u),
                    map_context_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    coord_x = table.Column<double>(type: "double", nullable: false),
                    coord_y = table.Column<double>(type: "double", nullable: false),
                    coord_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    num_logins = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValue: 0u),
                    last_login = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    total_time_played = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValue: 0u),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character", x => x.id);
                    table.ForeignKey(
                        name: "FK_character_account_account_id",
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
                    table.PrimaryKey("PK_character_appearance", x => new { x.character_id, x.slot });
                    table.ForeignKey(
                        name: "FK_character_appearance_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clan_member",
                columns: table => new
                {
                    clan_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    character_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rank = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_member", x => new { x.clan_id, x.character_id });
                    table.ForeignKey(
                        name: "FK_clan_member_character_character_id",
                        column: x => x.character_id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clan_member_clan_clan_id",
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
                name: "clan_member_index_character",
                table: "clan_member",
                column: "character_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_appearance");

            migrationBuilder.DropTable(
                name: "clan_member");

            migrationBuilder.DropTable(
                name: "character");

            migrationBuilder.DropTable(
                name: "clan");

            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
