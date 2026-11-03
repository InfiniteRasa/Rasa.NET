using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Valverde Marshes shrine of class 21271 teaches the Logos it shows, Logos Element (215),
    /// not Element (405); Crucible's Everyone shrine is Everyone's class (MarshesLogosElement).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271007000000_Marshes_logos_element")]
    public class Marshes_logos_element : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesLogosElement.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesLogosElement.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
