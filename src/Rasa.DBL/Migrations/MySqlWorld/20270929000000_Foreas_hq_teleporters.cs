using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Human teleporter pads under Foreas Base on Concordia Divide: from the headquarters' alcove
    /// down to the lower levels, and from there to the Awake logos room, each pair linked both
    /// ways (ForeasHqTeleporters). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270929000000_Foreas_hq_teleporters")]
    public class Foreas_hq_teleporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ForeasHqTeleporters.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ForeasHqTeleporters.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
