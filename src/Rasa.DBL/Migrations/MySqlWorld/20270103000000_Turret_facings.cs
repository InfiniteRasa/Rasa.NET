using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The AFS turrets on the maps' mounts turned to face as their mounts do: Add_afs_turrets read
    /// the mounts' quaternions in the wrong order (TurretFacings). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down turns back each turret still facing its mount's way.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270103000000_Turret_facings")]
    public class Turret_facings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TurretFacings.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TurretFacings.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
