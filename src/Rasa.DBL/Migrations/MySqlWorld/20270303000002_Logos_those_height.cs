using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Logos stone "Those" of Minos Caverns lifted from 2 m under its pedestal's floor to 2 m
    /// over it, as every other stone stands (LogosThoseHeightSeed). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down puts the old height back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270303000002_Logos_those_height")]
    public class Logos_those_height : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LogosThoseHeightSeed.UpStatements)
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LogosThoseHeightSeed.DownStatements)
                migrationBuilder.Sql(sql);
        }
    }
}
