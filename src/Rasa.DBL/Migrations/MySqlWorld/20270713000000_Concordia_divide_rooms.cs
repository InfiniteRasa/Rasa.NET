using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Foreas Base's garage, prison and barracks given their interiors, and the Bane Forward
    /// Command's rectangle its rooms' height (ConcordiaDivideRooms). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down takes the new rows out and gives the rectangle its full height again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270713000000_Concordia_divide_rooms")]
    public class Concordia_divide_rooms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ConcordiaDivideRooms.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ConcordiaDivideRooms.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
