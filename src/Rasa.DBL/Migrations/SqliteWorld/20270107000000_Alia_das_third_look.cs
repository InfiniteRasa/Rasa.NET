using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Alia Das after a third look round: Engineer Salter in the whole Mech 1 armour, Dr. Elise
    /// Corman's Corman set as painted and her hair paler, the Field Medic in the Officer 2
    /// uniform alone (AliaDasThirdLook). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270107000000_Alia_das_third_look")]
    public class Alia_das_third_look : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasThirdLook.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasThirdLook.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
