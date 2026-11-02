using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Linker Commander Casso's channel, and the Timora Mines and Crater Lake Hunters' shields
    /// (InstanceCreatureKits). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes off only what Up put on.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271021000000_Instance_creature_kits")]
    public class Instance_creature_kits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in InstanceCreatureKits.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in InstanceCreatureKits.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
