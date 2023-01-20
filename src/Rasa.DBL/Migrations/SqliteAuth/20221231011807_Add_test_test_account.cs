using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Rasa.Migrations.SqliteAuth
{
    public partial class Add_test_test_account : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "email", "username", "password", "salt", "level", "join_date" },
                values: new object[] {
                    "test@test",
                    "test",
                    "cda57527d7a63e9e6abe3dc5be01e4926757c70c1c8b179ed11741097e28990f",
                    "7e1180813d268491653f5a259f6d44f1ba77df78",
                    1,
                    DateTime.UtcNow
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
               table: "account",
               keyColumn: "username",
               keyValue: "test");
        }
    }
}
