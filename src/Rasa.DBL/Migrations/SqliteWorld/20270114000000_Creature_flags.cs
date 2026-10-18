using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Every creature class's flags as the monster flag editor sets them (CreatureFlagEdits): the
    /// difference from the seed, 3612 flags in over 340 classes and 14 out. Data only, in plain
    /// SQL: no model of its own.
    ///
    /// Down takes out the flags Up put in and puts back the ones it took out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270114000000_Creature_flags")]
    public class Creature_flags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CreatureFlagEdits.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CreatureFlagEdits.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
