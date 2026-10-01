using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// Until when a game master has silenced an account's chat (.mute): Unix milliseconds, UTC,
    /// 0 for never. Every existing account starts unsilenced.
    /// </summary>
    public partial class Add_account_muted_until : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "muted_until",
                table: "account",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "muted_until", table: "account");
        }
    }
}
