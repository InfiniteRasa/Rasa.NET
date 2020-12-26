using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlAuth
{
    public partial class Initial : Migration
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
                    username = table.Column<string>(type: "varchar(64)", nullable: false),
                    password = table.Column<string>(type: "varchar(64)", nullable: false),
                    salt = table.Column<string>(type: "varchar(40)", nullable: false),
                    level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    last_ip = table.Column<string>(type: "varchar(45)", nullable: false, defaultValue: "0.0.0.0"),
                    last_server_id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    last_login = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    join_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    locked = table.Column<ulong>(type: "bit", nullable: false, defaultValue: 0ul),
                    validated = table.Column<ulong>(type: "bit", nullable: false, defaultValue: 0ul),
                    validation_token = table.Column<string>(type: "varchar(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "email_UNIQUE",
                table: "account",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "username_UNIQUE",
                table: "account",
                column: "username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
