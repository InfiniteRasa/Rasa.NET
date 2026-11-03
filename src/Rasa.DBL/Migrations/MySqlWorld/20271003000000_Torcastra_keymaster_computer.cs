using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Keymaster of Torcastra's computer banks, and their objective marker, moved from behind the
    /// pipe in the service room's west alcove to the alcove's floor in front of it, where a player
    /// can use them (TorcastraKeymasterComputer). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271003000000_Torcastra_keymaster_computer")]
    public class Torcastra_keymaster_computer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TorcastraKeymasterComputer.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TorcastraKeymasterComputer.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
