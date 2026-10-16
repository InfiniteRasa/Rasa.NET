using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// local_teleporter_link: where each local teleporter takes whoever steps onto it, one row a
    /// way. A local teleporter with no row takes nobody anywhere.
    ///
    /// And the eleven pairs the client's map markers give away, linked both ways, with Ashoka
    /// Upper moved to its marker (LocalTeleporterLinks).
    ///
    /// Down puts Ashoka Upper back and drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270106000000_Local_teleporter_links")]
    public class Local_teleporter_links : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: LocalTeleporterLinkEntry.TableName,
                columns: table => new
                {
                    from_teleporter_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    to_teleporter_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_teleporter_link", x => x.from_teleporter_id);
                });

            foreach (var sql in LocalTeleporterLinks.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LocalTeleporterLinks.Down())
                migrationBuilder.Sql(sql);

            migrationBuilder.DropTable(name: LocalTeleporterLinkEntry.TableName);
        }
    }
}
