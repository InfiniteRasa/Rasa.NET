using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ashen Desert's unnamed interior regions put on Shadow's Edge Post's buildings and the cave
    /// tunnels to the map's links out (AshenDesertInteriorRegions). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270707000000_Ashen_desert_interior_regions")]
    public class Ashen_desert_interior_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenDesertInteriorRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenDesertInteriorRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
