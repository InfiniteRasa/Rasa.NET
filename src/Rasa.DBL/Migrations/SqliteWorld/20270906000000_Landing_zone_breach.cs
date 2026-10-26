using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270906000000_Landing_zone_breach")]
    public class Landing_zone_breach : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneBreachContent.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneBreachContent.Down()) migrationBuilder.Sql(sql);
        }
    }
}
