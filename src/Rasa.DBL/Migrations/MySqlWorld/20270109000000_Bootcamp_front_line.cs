using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// A Rifleman with his rifle ready and a Forean Shaman Initiate at the Proving Grounds' front,
    /// nearest the rockfall the Thrax come through (BootcampFrontLine). Data only, in plain SQL:
    /// no model of its own.
    ///
    /// Down takes out only the rows that are still these.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270109000000_Bootcamp_front_line")]
    public class Bootcamp_front_line : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampFrontLine.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampFrontLine.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
