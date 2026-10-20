using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Archfiend and Old Scratch back beside Horntail on the Lake Tinctuan landing pad, where
    /// Herbal Remedy sends the player for all three (ThreeDevils). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down puts the two pools back at the Lower Eloh Creek camp and outpost.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261229000000_Three_devils")]
    public class Three_devils : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThreeDevils.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThreeDevils.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
