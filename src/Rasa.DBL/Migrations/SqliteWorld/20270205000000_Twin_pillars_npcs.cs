using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Twin Pillars and its neighbours placed and dressed from footage: force fields in the gates,
    /// new NPCs and a squad, the infirmary and its analyzer, the towers' teleporters, Bane drop-offs,
    /// Pierre after her rescue, fire pits, Slater, a spotlight, Lt. Wood and the supplies crate
    /// (TwinPillarsNpcs). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270205000000_Twin_pillars_npcs")]
    public class Twin_pillars_npcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TwinPillarsNpcs.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TwinPillarsNpcs.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
