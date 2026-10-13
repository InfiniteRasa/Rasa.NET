using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Bane Hunters given Weapon_Creature_Hunter, their shield, in the weapon slot, as Bane
    /// Hunter Boss 81 has it (HunterShields). Data only, in plain SQL: no model of its own.
    ///
    /// It was 20261218000000, the timestamp Wilderness_mortar_launchers has. A World that ran it
    /// under that id runs it again under this one, which the guarded insert leaves as it is, and
    /// its history row for the old id is removed.
    ///
    /// Down takes the shield away.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261219000000_Hunter_shields")]
    public class Hunter_shields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(HunterShields.Up);
            migrationBuilder.Sql(HunterShields.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(HunterShields.Down);
        }
    }
}
