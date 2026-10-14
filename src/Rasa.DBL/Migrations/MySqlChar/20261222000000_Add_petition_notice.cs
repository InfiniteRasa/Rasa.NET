using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// petition.notice_pending: an answer or a close the player who filed the petition has yet
    /// to be told of, because they were not in the world when it was made. They are told when
    /// they next arrive in it. Every petition there is starts with nothing to tell.
    /// </summary>
    public partial class Add_petition_notice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "notice_pending",
                table: "petition",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "notice_pending", table: "petition");
        }
    }
}
