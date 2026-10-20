using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Omega Missile on its pad in Omega Labs' silo, idle (OmegaLabsMissile): world_usable row
    /// 10501. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the row out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270202000000_Omega_labs_missile")]
    public class Omega_labs_missile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in OmegaLabsMissile.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in OmegaLabsMissile.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
