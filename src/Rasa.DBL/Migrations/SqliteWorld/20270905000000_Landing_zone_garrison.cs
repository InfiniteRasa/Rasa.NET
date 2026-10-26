using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270905000000_Landing_zone_garrison")]
    public class Landing_zone_garrison : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneGarrisonContent.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneGarrisonContent.Down()) migrationBuilder.Sql(sql);
        }
    }
}
