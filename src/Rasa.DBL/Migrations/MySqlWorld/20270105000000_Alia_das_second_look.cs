using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Alia Das after a second look round: the CELLAR Arena link's pads the smaller human
    /// teleporter, Engineer Salter in Motor Assist 11 with a silver metal helmet, Dr. Elise
    /// Corman's hair red, Captain Spaulding's green helmet (AliaDasSecondLook). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down undoes what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270105000000_Alia_das_second_look")]
    public class Alia_das_second_look : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasSecondLook.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasSecondLook.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
