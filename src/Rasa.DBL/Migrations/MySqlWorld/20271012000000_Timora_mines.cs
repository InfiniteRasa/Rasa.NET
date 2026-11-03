using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Services.Preloader.Missions.Timora;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Timora Mines' people, Bane, drills, pumps, fuel lakes, consoles, transporter and missions,
    /// and Stolen History switched on (TimoraMinesContent, TimoraMinesMissionsV1). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down takes the missions out and Stolen History off, then the rows, and puts back the pools
    /// it changed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271012000000_Timora_mines")]
    public class Timora_mines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TimoraMinesContent.Up())
                migrationBuilder.Sql(sql);

            foreach (var sql in TimoraMinesMissionsV1.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TimoraMinesMissionsV1.Down())
                migrationBuilder.Sql(sql);

            foreach (var sql in TimoraMinesContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
