using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Proving Grounds front squads include a Field Gunner whose authored creature comment
    /// is 57 characters, longer than the historical creature.comment varchar(50). Widen the
    /// MySQL column before inserting those creatures, preserving the complete comment.
    /// The rollback deliberately does not shrink it, which would risk truncating saved text.
    /// </summary>
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270110000200_WidenCreatureCommentBeforeFrontSquads")]
    public class WidenCreatureCommentBeforeFrontSquads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "creature",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep TEXT: reverting to varchar(50) could truncate persisted creature comments.
        }
    }
}
