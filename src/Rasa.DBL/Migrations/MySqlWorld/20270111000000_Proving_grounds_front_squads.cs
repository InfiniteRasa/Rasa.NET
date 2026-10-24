using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
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
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270111000000_Proving_grounds_front_squads")]
    public class Proving_grounds_front_squads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL DDL commits independently from the seed DML. If a prior attempt failed
            // while inserting a creature, the table can already exist even though the
            // migration is absent from __EFMigrationsHistory. Retry without dropping its rows.
            migrationBuilder.Sql(
                "CREATE TABLE IF NOT EXISTS `spawnpool_draw` (" +
                "`id` int unsigned NOT NULL, " +
                "`count` tinyint unsigned NOT NULL, " +
                "CONSTRAINT `PK_spawnpool_draw` PRIMARY KEY (`id`)" +
                ") CHARACTER SET utf8mb4;");

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
