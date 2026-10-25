using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270907000000_Defender_reinforcement_timers")]
    public class Defender_reinforcement_timers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach(var sql in DefenderRespawns.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach(var sql in DefenderRespawns.Down()) migrationBuilder.Sql(sql);
        }
    }
}
