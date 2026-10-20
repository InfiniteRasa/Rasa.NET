using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// A Bane Mortar on every empty mortar base of the maps with a zone (BaneMortars): a creature
    /// row, attack, stats and launcher for each zone, a pool of one on each base, and a base by a
    /// control point in that point's Bane garrison. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes them all out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270122000000_Bane_mortars")]
    public class Bane_mortars : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => BaneMortars.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) => BaneMortars.Down(migrationBuilder);
    }
}
