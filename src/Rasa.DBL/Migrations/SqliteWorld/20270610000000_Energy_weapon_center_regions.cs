using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Energy Weapon Center's rooms given their volumes
    /// (EnergyWeaponCenterRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270610000000_Energy_weapon_center_regions")]
    public class Energy_weapon_center_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in EnergyWeaponCenterRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in EnergyWeaponCenterRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
