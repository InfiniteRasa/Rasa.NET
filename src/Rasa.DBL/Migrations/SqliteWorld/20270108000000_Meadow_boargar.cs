using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Young Forest Boargar pack west of Alia Das (spawnpool 580064) moves 23 m west, out of
    /// the meadow tripod turret's scan (MeadowBoargar). Data only, in plain SQL: no model of its
    /// own.
    ///
    /// Down puts it back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270108000000_Meadow_boargar")]
    public class Meadow_boargar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MeadowBoargar.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MeadowBoargar.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
