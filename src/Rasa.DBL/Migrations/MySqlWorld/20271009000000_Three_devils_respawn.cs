using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Archfiend's and Old Scratch's pools respawn in 50 s, as Horntail's does (ThreeDevilsRespawn).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down puts back 500 s where this put 50 s.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271009000000_Three_devils_respawn")]
    public class Three_devils_respawn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThreeDevilsRespawn.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThreeDevilsRespawn.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
