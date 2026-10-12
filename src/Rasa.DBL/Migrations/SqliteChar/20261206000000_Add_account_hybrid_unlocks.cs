using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    /// <summary>
    /// The hybrid races an account has unlocked by completing their missions (hybrid_unlocks: 1
    /// Forean, 2 Brann, 4 Thrax). Every existing account starts with none; with
    /// GameDataConfig.AlwaysUnlockHybrids on, as it is by default, every race is offered anyway.
    /// </summary>
    public partial class Add_account_hybrid_unlocks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "hybrid_unlocks",
                table: "account",
                type: "tinyint(3)",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "hybrid_unlocks", table: "account");
        }
    }
}
