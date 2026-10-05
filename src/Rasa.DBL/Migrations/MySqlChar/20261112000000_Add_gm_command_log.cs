using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// The game master audit log (the game server's GmAudit): gm_command_log, one row a command
    /// entered by a game master, in chat, as a slash command, through the client's GM windows or
    /// at the server's console, with who entered it, when, from where, the command as it was
    /// entered and what came of it.
    /// </summary>
    public partial class Add_gm_command_log : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_command_log",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    source = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    result = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    account_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    account_level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    family_name = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    address = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    instance_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    coord_x = table.Column<double>(type: "double", nullable: false),
                    coord_y = table.Column<double>(type: "double", nullable: false),
                    coord_z = table.Column<double>(type: "double", nullable: false),
                    target_character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    target = table.Column<string>(type: "varchar(96)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    command = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    text = table.Column<string>(type: "varchar(512)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_command_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "gm_command_log_index_account_id",
                table: "gm_command_log",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "gm_command_log_index_created_at",
                table: "gm_command_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "gm_command_log_index_target_character_id",
                table: "gm_command_log",
                column: "target_character_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "gm_command_log");
        }
    }
}
