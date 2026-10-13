using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Bane Hunters given Weapon_Creature_Hunter, their shield, in the weapon slot, as Bane
    /// Hunter Boss 81 has it (HunterShields). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the shield away.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261218000000_Hunter_shields")]
    public class Hunter_shields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(HunterShields.Up);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(HunterShields.Down);
        }
    }
}
