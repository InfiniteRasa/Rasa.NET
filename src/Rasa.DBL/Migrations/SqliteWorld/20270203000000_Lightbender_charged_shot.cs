using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Lightbenders' shot (1, 149) gets the client's 4433 ms cycle - a 1400 ms charge and a
    /// 3033 ms recovery - as its cooldown in place of the 800 placeholder (LightbenderChargedShot).
    /// Data only; rows a game master has changed from 800 are left alone.
    ///
    /// Down puts 800 back on the rows that have 4433.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270203000000_Lightbender_charged_shot")]
    public class Lightbender_charged_shot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LightbenderChargedShot.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LightbenderChargedShot.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
