using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// SQLite counterpart of the MySQL teleporter description widening. SQLite does not
    /// enforce varchar(64) lengths; keep the migration IDs aligned across providers.
    /// </summary>
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270211000000_WidenTeleporterDescriptionBeforeOutpostTowers")]
    public class WidenTeleporterDescriptionBeforeOutpostTowers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) { }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
