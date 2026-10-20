using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The fighting west of the Proving Grounds' fort once it is taken back
    /// (ProvingGroundsFrontSquads, BootcampFrontSquadsV13): spawnpool_draw, a table of the pools
    /// that send a drawn number of creatures; five creature rows and the Field Gunner's laser
    /// attack; six scripted squad pools, three of AFS infantry drawing three soldiers each and
    /// three of Foreans; and Capture the Flag's scene binding, which wakes them when Youngblood
    /// comes in. Rows in plain SQL, the migration having no model of its own.
    ///
    /// Down takes the rows out, puts the scene binding back as Bootcamp_breach_assault wrote it
    /// and drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270111000000_Proving_grounds_front_squads")]
    public class Proving_grounds_front_squads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: SpawnPoolDrawEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false),
                    count = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpool_draw", x => x.id);
                });

            foreach (var sql in ProvingGroundsFrontSquads.Up())
                migrationBuilder.Sql(sql);

            BootcampFrontSquadsV13.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampFrontSquadsV13.Down(migrationBuilder);

            foreach (var sql in ProvingGroundsFrontSquads.Down())
                migrationBuilder.Sql(sql);

            migrationBuilder.DropTable(name: SpawnPoolDrawEntry.TableName);
        }
    }
}
