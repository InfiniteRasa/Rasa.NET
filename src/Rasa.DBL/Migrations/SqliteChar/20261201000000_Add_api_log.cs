using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The REST API's log (the game server's Api.ApiAudit): api_log, one row a request the API
    /// was sent - when, from which address, what was asked for, the status of the answer, the
    /// body of a POST with its passwords, codes and keys written over, and the start of the
    /// answer. No rows are added here.
    /// </summary>
    public partial class Add_api_log : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_log",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    address = table.Column<string>(type: "varchar(64)", nullable: false),
                    method = table.Column<string>(type: "varchar(16)", nullable: false),
                    path = table.Column<string>(type: "varchar(256)", nullable: false),
                    query = table.Column<string>(type: "varchar(512)", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    body_length = table.Column<int>(type: "INTEGER", nullable: false),
                    response = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "api_log_index_address",
                table: "api_log",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "api_log_index_created_at",
                table: "api_log",
                column: "created_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "api_log");
        }
    }
}
