using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// A human teleporter pad behind the Few logos in Howling Maw that takes players up out of
    /// the Dead Zone Power Station's mine shaft to the base floor, one way (FewShaftTeleporter).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270927000000_Few_shaft_teleporter")]
    public class Few_shaft_teleporter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FewShaftTeleporter.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FewShaftTeleporter.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
