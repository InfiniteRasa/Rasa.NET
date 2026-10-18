using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Corporal Van Valkenberg in the Stealth 6 armour, with greeting 1648 for once the player has
    /// checked in with him (BootcampVanValkenberg). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the olive officer's uniform back and takes the greeting out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270114000000_Bootcamp_van_valkenberg")]
    public class Bootcamp_van_valkenberg : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampVanValkenberg.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampVanValkenberg.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
