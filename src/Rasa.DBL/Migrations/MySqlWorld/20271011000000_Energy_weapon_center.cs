using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Energy Weapon Center's people, Bane and missions (EnergyWeaponCenterContent).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again, and sets the seed's Thrax pools there going again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271011000000_Energy_weapon_center")]
    public class Energy_weapon_center : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in EnergyWeaponCenterContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in EnergyWeaponCenterContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
