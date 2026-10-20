using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Shahrbaraz, the Fithik Hive Monarch and Hive Master, and the Mox Alpha Male swing their
    /// family's weapon for a boss's damage, in place of the placeholder rows they shared
    /// (BossWeapons): creature_action 77001-77004 in slot 1. Data only, in plain SQL: no model of
    /// its own.
    ///
    /// Down puts the old rows back and deletes 77001-77004.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261222000000_Boss_weapons")]
    public class Boss_weapons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BossWeapons.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BossWeapons.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
