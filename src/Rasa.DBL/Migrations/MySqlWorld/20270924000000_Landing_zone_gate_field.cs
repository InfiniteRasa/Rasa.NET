using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Landing Zone gate's force field linked to its control point, so that it is the
    /// holder's: red while the Bane hold the point (LandingZoneGateField). Data only, in plain
    /// SQL: no model of its own.
    ///
    /// Down takes out this link alone.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270924000000_Landing_zone_gate_field")]
    public class Landing_zone_gate_field : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneGateField.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneGateField.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
