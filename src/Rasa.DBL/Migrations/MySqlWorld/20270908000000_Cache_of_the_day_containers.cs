using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.MySqlWorld
{
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270908000000_Cache_of_the_day_containers")]
    public class Cache_of_the_day_containers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach(var sql in CacheOfTheDayContainers.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach(var sql in CacheOfTheDayContainers.Down()) migrationBuilder.Sql(sql);
        }
    }
}
