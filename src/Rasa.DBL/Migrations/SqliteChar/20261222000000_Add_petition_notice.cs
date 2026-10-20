using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
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
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <summary>
        /// In SQL of its own: EF rebuilds the table to drop a column on SQLite, from the model of
        /// the migration before, and 20261214000000_Clan_name_unique has none. SQLite drops a
        /// column itself since 3.35.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE petition DROP COLUMN notice_pending;");
        }
    }
}
