using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// The chat log (the game server's ChatAudit): chat_log, one row a line of chat a player
    /// sent - said, shouted, emoted, whispered, or sent to a squad, a clan, a clan's leaders or
    /// a channel - with who said it, when, where, to whom, the line and what came of it.
    /// </summary>
    public partial class Add_chat_log : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_log",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    result = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    account_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    account_level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    family_name = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    instance_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    coord_x = table.Column<double>(type: "double", nullable: false),
                    coord_y = table.Column<double>(type: "double", nullable: false),
                    coord_z = table.Column<double>(type: "double", nullable: false),
                    group_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    target_account_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    target_character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    target = table.Column<string>(type: "varchar(96)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    heard_by = table.Column<uint>(type: "int unsigned", nullable: false),
                    text = table.Column<string>(type: "varchar(512)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "chat_log_index_account_id",
                table: "chat_log",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "chat_log_index_created_at",
                table: "chat_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "chat_log_index_target_account_id",
                table: "chat_log",
                column: "target_account_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chat_log");
        }
    }
}
