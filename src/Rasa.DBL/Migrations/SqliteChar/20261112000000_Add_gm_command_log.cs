using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
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
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    source = table.Column<byte>(type: "INTEGER", nullable: false),
                    result = table.Column<byte>(type: "INTEGER", nullable: false),
                    account_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    account_level = table.Column<byte>(type: "INTEGER", nullable: false),
                    required_level = table.Column<byte>(type: "INTEGER", nullable: false),
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    family_name = table.Column<string>(type: "varchar(64)", nullable: false),
                    address = table.Column<string>(type: "varchar(64)", nullable: false),
                    map_context_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    instance_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    coord_x = table.Column<double>(type: "double", nullable: false),
                    coord_y = table.Column<double>(type: "double", nullable: false),
                    coord_z = table.Column<double>(type: "double", nullable: false),
                    target_character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    target = table.Column<string>(type: "varchar(96)", nullable: false),
                    command = table.Column<string>(type: "varchar(64)", nullable: false),
                    text = table.Column<string>(type: "varchar(512)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_command_log", x => x.id);
                });

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
