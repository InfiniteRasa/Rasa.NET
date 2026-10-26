using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Services.Preloader.Missions.Torcastra;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Torcastra Prison's people, Bane, usable objects and missions (TorcastraPrisonContent,
    /// TorcastraMissionsV1). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the missions out, then the rows, and puts back the pools it changed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270307000000_Torcastra_prison")]
    public class Torcastra_prison : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TorcastraPrisonContent.Up())
                migrationBuilder.Sql(sql);

            foreach (var sql in TorcastraMissionsV1.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TorcastraMissionsV1.Down())
                migrationBuilder.Sql(sql);

            foreach (var sql in TorcastraPrisonContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
