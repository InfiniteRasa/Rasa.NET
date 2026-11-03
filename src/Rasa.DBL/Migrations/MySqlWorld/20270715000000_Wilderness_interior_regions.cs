using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Concordia Wilderness's unnamed regions put on the Ranja Gorge buildings, the Eloh
    /// corridors, the cave tunnels to the map's links out, Alia Das's AFS tents and Forean
    /// village, Alia Caverns' Eloh shrine and the west end of Daghda's Urn's cave
    /// (WildernessInteriorRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270715000000_Wilderness_interior_regions")]
    public class Wilderness_interior_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessInteriorRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessInteriorRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
