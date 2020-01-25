using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Database.Migrations.Auth
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
                    username = table.Column<string>(type: "varchar(64)", nullable: false),
                    password = table.Column<string>(type: "varchar(64)", nullable: false),
                    salt = table.Column<string>(type: "varchar(40)", nullable: false),
                    level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "0"),
                    last_ip = table.Column<string>(type: "varchar(45)", nullable: false, defaultValueSql: "'0.0.0.0'"),
                    last_server_id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "0"),
                    last_login = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    join_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()"),
                    locked = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    validated = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    validation_token = table.Column<string>(type: "varchar(40)", nullable: true, defaultValueSql: "NULL")
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

            migrationBuilder.Sql("ALTER TABLE `account` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
