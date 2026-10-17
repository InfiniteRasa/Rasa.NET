using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The six Thrax through the Proving Grounds' cave-in take the sandbag post before the bridge
    /// (BootcampBreachAssaultV12): a route each, to a line before the post held and fought from,
    /// then onto the bridge, held and fought from too. Data only: no model of its own.
    ///
    /// Down puts their one route back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270110000100_Bootcamp_breach_assault")]
    public class Bootcamp_breach_assault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => BootcampBreachAssaultV12.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) => BootcampBreachAssaultV12.Down(migrationBuilder);
    }
}
