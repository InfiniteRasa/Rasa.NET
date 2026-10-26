using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// SQLite counterpart of the MySQL-only capacity change for creature.comment.
    /// SQLite text has no varchar(N) length constraint to widen. Keep the same
    /// migration ID on both providers so that their histories stay aligned.
    /// </summary>
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270110000200_WidenCreatureCommentBeforeFrontSquads")]
    public class WidenCreatureCommentBeforeFrontSquads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) { }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
