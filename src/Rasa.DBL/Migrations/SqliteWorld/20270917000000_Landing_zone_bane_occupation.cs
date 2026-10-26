using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270917000000_Landing_zone_bane_occupation")]
    public class Landing_zone_bane_occupation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneOccupationContent.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneOccupationContent.Down()) migrationBuilder.Sql(sql);
        }
    }
}
