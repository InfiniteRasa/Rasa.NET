using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Boss_weapons inserts a Fithik Hive Master description longer than the historical
    /// creature_action.description varchar(50). Widen it before the insert on MySQL.
    /// A data-only rollback cannot safely shrink this column without truncating notes.
    /// </summary>
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261221050000_WidenCreatureActionDescriptionBeforeBossWeapons")]
    public class WidenCreatureActionDescriptionBeforeBossWeapons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "creature_action",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately keep TEXT: shrinking would truncate persisted action descriptions.
        }
    }
}
