using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270910000000_Pierre_personal_escort")]
    public class Pierre_personal_escort : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PierrePersonalEscort.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PierrePersonalEscort.Down()) migrationBuilder.Sql(sql);
        }
    }
}