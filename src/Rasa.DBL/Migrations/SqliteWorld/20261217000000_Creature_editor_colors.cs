using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261217000000_Creature_editor_colors")]
    public sealed class Creature_editor_colors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(name: "body_hue", table: "creature", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<uint>(name: "body_hue2", table: "creature", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<uint>(name: "hue2", table: "creature_appearance", type: "INTEGER", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // EF Core's SQLite migration generator does not translate DropColumnOperation.
            // Use native SQLite DDL instead. These new nullable columns have no indexes
            // or constraints referencing them, so DROP COLUMN preserves existing rows.
            migrationBuilder.Sql("ALTER TABLE \"creature_appearance\" DROP COLUMN \"hue2\";");
            migrationBuilder.Sql("ALTER TABLE \"creature\" DROP COLUMN \"body_hue2\";");
            migrationBuilder.Sql("ALTER TABLE \"creature\" DROP COLUMN \"body_hue\";");
        }
    }
}
