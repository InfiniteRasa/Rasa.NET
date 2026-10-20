using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The CELLAR Arena link in the Alia Das hospital room where a player stands in front of its
    /// sign, and the hospital's revive point and the arrival from the arena moved clear of it
    /// (AliaDasCellarLink). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts all three back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261230000000_Alia_Das_cellar_link")]
    public class Alia_Das_cellar_link : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasCellarLink.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasCellarLink.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
