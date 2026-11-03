using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Torden Mires' dark falling sky given the drop below its southern cliffs
    /// (TordenMiresDarkFallRegion). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the row out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270708000000_Torden_mires_dark_fall_region")]
    public class Torden_mires_dark_fall_region : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TordenMiresDarkFallRegion.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TordenMiresDarkFallRegion.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
