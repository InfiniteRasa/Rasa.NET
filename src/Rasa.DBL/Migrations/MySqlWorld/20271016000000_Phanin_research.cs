using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Phanin Research Facility's Brann, bosses and seven missions, Greloran outside it, and
    /// Sergeant Phenix and Colonel Whitaker where they stand (PhaninResearchContent). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down takes the missions out, then the rows, and puts back the pools and package it changed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271016000000_Phanin_research")]
    public class Phanin_research : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PhaninResearchContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PhaninResearchContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
