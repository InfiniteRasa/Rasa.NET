using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The control-point turrets put on the outpost fence's own gun bases, the ones over nothing
    /// taken away and the empty bases given turrets (FenceTurretBases). Data only, in plain SQL:
    /// no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271002000000_Fence_turret_bases")]
    public class Fence_turret_bases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FenceTurretBases.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FenceTurretBases.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
