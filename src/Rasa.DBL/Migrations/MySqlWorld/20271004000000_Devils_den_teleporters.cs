using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Devil's Den's three local teleporters linked: Fithik Hatchery and the Bane Detention
    /// Center both ways, the Bane Comm Tower to the detention center (DevilsDenTeleporters).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down takes out these links alone.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271004000000_Devils_den_teleporters")]
    public class Devils_den_teleporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DevilsDenTeleporters.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DevilsDenTeleporters.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
