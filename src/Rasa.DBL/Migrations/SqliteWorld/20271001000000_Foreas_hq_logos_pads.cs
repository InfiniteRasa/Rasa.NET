using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Foreas Base pad to the Awake logos moved to the corridor ring's east side, its partner
    /// to the logos room's hall, and the pad up to the headquarters turned to face it
    /// (ForeasHqLogosPads). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271001000000_Foreas_hq_logos_pads")]
    public class Foreas_hq_logos_pads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ForeasHqLogosPads.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ForeasHqLogosPads.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
