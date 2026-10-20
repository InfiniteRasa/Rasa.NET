using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Seymour, the Maw boss, bites with CR_MAW_MELEE as every Maw does, for a boss's damage, in
    /// place of a Maw weapon swing for 5-12 (MawBossBite). Data only, in plain SQL: no model of
    /// its own.
    ///
    /// Down puts the swing back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261220000000_Maw_boss_bite")]
    public class Maw_boss_bite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MawBossBite.Up)
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MawBossBite.Down)
                migrationBuilder.Sql(sql);
        }
    }
}
