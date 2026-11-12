using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The retail body tints of Valverde Plateau, Pools and Marshes creatures (ValverdeCreatureTints).
    /// Data only, plain SQL; Down deletes only the rows that still have the values Up wrote.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271107000100_Valverde_creature_tints")]
    public class Valverde_creature_tints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ValverdeCreatureTints.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ValverdeCreatureTints.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
